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

        // Every component the ball needs is authored on Ball.prefab and wired
        // here in the Inspector — nothing is AddComponent'd or GetComponent'd at
        // runtime, so code stripping can never drop the physics types in a build
        // (the bug that made tray balls throw NullReferenceException on WebGL).
        [SerializeField] MeshFilter _meshFilter;
        [SerializeField] MeshRenderer _meshRenderer;
        [SerializeField] TrailRenderer _trail;
        [SerializeField] SphereCollider _collider;
        [SerializeField] Rigidbody _body;

        Vector3 _from, _ctrl, _to;
        float _t, _delay;
        static float Duration => Systems.GameConfig.Active.ballFlightDuration;
        Action<BallView> _onArrive;
        bool _arrived;

        /// The parked ball's rigidbody, so the tray's out-of-bounds safety net
        /// can zero its velocity without a GetComponent.
        public Rigidbody Body => _body;

        /// The prefab carries the components and their tunables (scale, shadow
        /// flags, trail time/width, and the physics config on the disabled
        /// SphereCollider + kinematic Rigidbody — edit them on Ball.prefab);
        /// only the procedural assets (sphere mesh, matcap material, trail tint)
        /// are assigned here.
        public void Launch(BallRoute route, Vector3 from, Vector3 to,
            float delay, Action<BallView> onArrive)
        {
            transform.position = from;

            // physics stays inert during flight — BallView drives the transform
            _collider.enabled = false;
            _body.isKinematic = true;

            _meshFilter.sharedMesh = CubeMeshFactory.Sphere();
            _meshRenderer.sharedMaterial = CubeMeshFactory.BallMaterialFor(route.Color);

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

        /// Turns the just-landed ball into a live physics ball in the tray: the
        /// flight loop stops and the prefab's Rigidbody/SphereCollider (mass,
        /// drag, constraints, etc. authored on Ball.prefab) wake up. Only the
        /// dynamic bits are set here — no component is created.
        public void EnablePhysics(Vector3 velocity)
        {
            enabled = false; // stop the flight Update
            _arrived = true;
            _collider.enabled = true;
            _body.isKinematic = false;
            // Interpolation is OFF during flight (the ball is a kinematic body
            // driven straight from transform.position each frame — interpolation
            // would fight that and render it lagging/floating mid-air). Turn it
            // on now so the piling physics ball moves smoothly in the tray.
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.maxDepenetrationVelocity = 1f; // don't catapult stacked balls out
            _body.velocity = velocity;
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
