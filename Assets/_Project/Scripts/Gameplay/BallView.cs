using System;
using CubeBurst.Core;
using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// A flying ball: quadratic bezier arc from cube (or tray) to its target.
    /// A real 3D sphere; the lit look is baked into its matcap-style texture.
    public class BallView : MonoBehaviour
    {
        public BallRoute Route { get; private set; }

        Vector3 _from, _ctrl, _to;
        float _t, _delay;
        static float Duration => Systems.GameConfig.Active.ballFlightDuration;
        Action<BallView> _onArrive;
        bool _arrived;
        TrailRenderer _trail;

        /// The prefab carries the components and their tunables (scale,
        /// shadow flags, trail time/width — edit them on Ball.prefab); only
        /// the procedural assets (sphere mesh, matcap material, trail tint)
        /// are assigned here.
        public void Launch(BallRoute route, Vector3 from, Vector3 to,
            float delay, Action<BallView> onArrive)
        {
            transform.position = from;

            GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.Sphere();
            GetComponent<MeshRenderer>().sharedMaterial = CubeMeshFactory.BallMaterialFor(route.Color);

            _trail = GetComponent<TrailRenderer>();
            _trail.material = CubeMeshFactory.TrailMaterial();
            _trail.emitting = false; // starts once the ball leaves the spawn point
            var tint = Palette.Of(route.Color);
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = grad;

            Route = route;
            _from = from;
            _to = to;
            var mid = (from + to) * 0.5f;
            _ctrl = mid + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 1.3f, 0f);
            _delay = delay;
            _onArrive = onArrive;
        }

        /// Stops and removes the trail so a parked tray ball doesn't drag one.
        public void DetachTrail()
        {
            if (_trail == null) return;
            _trail.emitting = false;
            Destroy(_trail);
            _trail = null;
        }

        void Update()
        {
            if (_arrived) return;
            if (_delay > 0f)
            {
                _delay -= Time.deltaTime;
                return;
            }
            if (_trail != null && !_trail.emitting) _trail.emitting = true;
            _t += Time.deltaTime / Duration;
            if (_t >= 1f)
            {
                _arrived = true;
                transform.position = _to;
                _onArrive?.Invoke(this);
                return;
            }
            float e = _t * _t * (3f - 2f * _t); // smoothstep along the arc
            var a = Vector3.Lerp(_from, _ctrl, e);
            var b = Vector3.Lerp(_ctrl, _to, e);
            transform.position = Vector3.Lerp(a, b, e);
        }
    }
}
