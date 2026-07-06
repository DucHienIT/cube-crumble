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

        public static BallView Spawn(Transform parent, BallRoute route, Vector3 from, Vector3 to,
            float delay, Action<BallView> onArrive)
        {
            var go = new GameObject("Ball", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.34f;

            go.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.Sphere();
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = CubeMeshFactory.BallMaterialFor(route.Color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 500;

            // comet trail behind the flying ball (width is in the ball's local
            // scale, so ~0.5 reads as a tail a bit thinner than the ball)
            var trail = go.AddComponent<TrailRenderer>();
            trail.material = CubeMeshFactory.TrailMaterial();
            trail.time = 0.16f;
            trail.startWidth = 0.55f;
            trail.endWidth = 0f;
            trail.numCapVertices = 3;
            trail.minVertexDistance = 0.03f;
            trail.alignment = LineAlignment.View;
            trail.sortingOrder = 499;
            trail.autodestruct = false;
            trail.emitting = false; // starts once the ball leaves the spawn point
            var tint = Palette.Of(route.Color);
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;

            var ball = go.AddComponent<BallView>();
            ball._trail = trail;
            ball.Route = route;
            ball._from = from;
            ball._to = to;
            var mid = (from + to) * 0.5f;
            ball._ctrl = mid + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 1.3f, 0f);
            ball._delay = delay;
            ball._onArrive = onArrive;
            return ball;
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
