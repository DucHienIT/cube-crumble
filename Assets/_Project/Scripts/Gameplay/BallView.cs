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

        // Jelly squash on landing: a decaying cosine that flattens the ball on
        // impact and springs it back to round. base scale is captured when the
        // ball parks so we can multiply the squash on top of the tray's scale.
        Vector3 _baseScale;
        float _squashT;
        bool _squashing;

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
            _arrived = true;
            _collider.enabled = true;
            _body.isKinematic = false;
            // Interpolation is OFF during flight (the ball is a kinematic body
            // driven straight from transform.position each frame — interpolation
            // would fight that and render it lagging/floating mid-air). Turn it
            // on now so the piling physics ball moves smoothly in the tray.
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.maxDepenetrationVelocity = 0.6f; // don't catapult stacked balls out
            // more solver iterations keep the soft (overlapping) pile from
            // jittering apart as balls stack up
            _body.solverIterations = 16;
            _body.solverVelocityIterations = 4;
            _body.velocity = velocity;

            // kick off the jelly squash; Update stays enabled to run it, then
            // disables itself once the ball settles round again
            _baseScale = transform.localScale;
            _squashT = 0f;
            _squashing = true;
        }

        void Update()
        {
            if (_arrived)
            {
                if (_squashing) UpdateSquash();
                return;
            }
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

        // Decaying-cosine squash: flatten vertically on impact, spring back to
        // round. Volume-ish preserved (widen x/z as y compresses) for a soft
        // jelly wobble. Runs ~0.55s then turns the component off.
        void UpdateSquash()
        {
            _squashT += Time.deltaTime;
            const float amp = 0.32f, decay = 9f, freq = 26f, life = 0.55f;
            float s = amp * Mathf.Exp(-decay * _squashT) * Mathf.Cos(freq * _squashT);
            transform.localScale = new Vector3(
                _baseScale.x * (1f + s * 0.5f),
                _baseScale.y * (1f - s),
                _baseScale.z * (1f + s * 0.5f));
            if (_squashT >= life)
            {
                transform.localScale = _baseScale;
                _squashing = false;
                enabled = false; // settled — no more per-frame work
            }
        }
    }
}
