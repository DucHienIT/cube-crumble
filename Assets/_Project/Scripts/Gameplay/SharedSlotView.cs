using System.Collections.Generic;
using CubeBurst.Core;
using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Marks a parked ball in the tray so transfers can find it by color.
    public class BallTag : MonoBehaviour
    {
        public GameColor Color;
    }

    /// The shared overflow tray between the cube shape and the containers —
    /// a U-shaped basin (reference style) where landed balls drop in and pile
    /// up with real physics (z-locked so they behave like a 2D pile).
    public class SharedSlotView : MonoBehaviour
    {
        // tray position is authored on the SharedSlot prefab root

        // basin interior derived from the BasinRim sprite bake (416x224 @128ppu)
        const float FloorY = -0.65f;        // inner floor surface
        const float InnerHalfWidth = 1.36f; // inner wall faces
        // one full row of Capacity(8) balls spans the whole interior, so a
        // "full" tray is visually unambiguous
        const float BallScale = 0.33f;

        static PhysicsMaterial _ballPhysics;

        GameSession _session;
        SpriteRenderer _rim;
        readonly List<GameObject> _balls = new List<GameObject>();

        public void Init(GameSession session)
        {
            _session = session;
            Build();
        }

        void Build()
        {
            // polka-dot interior behind the balls
            var interior = NewSprite("Interior", SpriteFactory.PolkaPanel(), 41);
            interior.transform.localPosition = new Vector3(0f, -0.1f, 0.1f);
            interior.transform.localScale = new Vector3(1.35f, 0.7f, 1f);

            // dashed fill-limit line above the basin opening
            var dashes = NewSprite("Dashes", SpriteFactory.Dashes(), 42);
            dashes.transform.localPosition = new Vector3(0f, 0.5f, -0.05f);
            dashes.transform.localScale = new Vector3(1.25f, 1f, 1f);
            dashes.color = new Color(1f, 1f, 1f, 0.9f);

            // white straps hanging the basin from the panel's top edge
            for (int i = 0; i < 2; i++)
            {
                var strap = NewSprite(i == 0 ? "StrapL" : "StrapR", SpriteFactory.BigRounded(), 40);
                strap.transform.localPosition = new Vector3(i == 0 ? -1.47f : 1.47f, 0.55f, 0.2f);
                strap.drawMode = SpriteDrawMode.Sliced;
                strap.size = new Vector2(0.26f, 0.8f);
                strap.color = new Color(0.99f, 0.97f, 0.97f, 1f);
            }

            // rim in front so parked balls sit "inside" the basin
            _rim = NewSprite("Rim", SpriteFactory.BasinRim(), 46);
            _rim.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            _rim.color = Palette.TrayRim;

            // static colliders: floor + two walls (balls are z-frozen, so no
            // front/back walls needed)
            AddWall(new Vector3(0f, FloorY - 0.1f, 0f), new Vector3(3f, 0.2f, 1f));
            AddWall(new Vector3(-(InnerHalfWidth + 0.1f), 0.8f, 0f), new Vector3(0.2f, 3.1f, 1f));
            AddWall(new Vector3(InnerHalfWidth + 0.1f, 0.8f, 0f), new Vector3(0.2f, 3.1f, 1f));
        }

        SpriteRenderer NewSprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        void AddWall(Vector3 center, Vector3 size)
        {
            var go = new GameObject("TrayCollider", typeof(BoxCollider));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = center;
            go.GetComponent<BoxCollider>().size = size;
        }

        static PhysicsMaterial BallPhysics()
        {
            if (_ballPhysics == null)
                _ballPhysics = new PhysicsMaterial("TrayBall")
                {
                    bounciness = 0.25f,
                    dynamicFriction = 0.2f,
                    staticFriction = 0.2f,
                    bounceCombine = PhysicsMaterialCombine.Average,
                };
            return _ballPhysics;
        }

        /// Where flying balls aim: just above the basin opening, with jitter
        /// so the pile builds naturally.
        public Vector3 GetDropPoint()
        {
            if (_balls.Count >= _session.Shared.Capacity)
                return transform.position + new Vector3(0f, 0.8f, 0f); // overflow ball, game over incoming
            return transform.position + new Vector3(Random.Range(-1f, 1f), 0.7f, 0f);
        }

        /// Converts a landed ball into a physics ball inside the basin.
        public void AcceptBall(BallView ball)
        {
            var go = ball.gameObject;
            var tag = go.AddComponent<BallTag>();
            tag.Color = ball.Route.Color;
            ball.DetachTrail();
            Destroy(ball);

            go.transform.SetParent(transform, true);
            var p = go.transform.localPosition;
            p.x = Mathf.Clamp(p.x, -InnerHalfWidth + 0.16f, InnerHalfWidth - 0.16f);
            p.z = 0f;
            go.transform.localPosition = p;
            go.transform.localScale = Vector3.one * BallScale;

            go.AddComponent<SphereCollider>().sharedMaterial = BallPhysics();
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.linearDamping = 0.1f;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // two balls landing on the same spot must not catapult each other
            // out of the basin (default depenetration velocity is 10!)
            rb.maxDepenetrationVelocity = 1f;
            rb.linearVelocity = new Vector3(Random.Range(-0.4f, 0.4f), -2.5f, 0f);

            _balls.Add(go);
            UpdateDangerTint();
        }

        /// Removes the most recent ball of a color (for transfers out); the
        /// rest of the pile resettles on its own.
        public GameObject TakeBall(GameColor color)
        {
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var tag = _balls[i].GetComponent<BallTag>();
                if (tag == null || tag.Color != color) continue;
                var go = _balls[i];
                _balls.RemoveAt(i);
                UpdateDangerTint();
                return go;
            }
            return null;
        }

        void UpdateDangerTint()
        {
            _rim.color = _balls.Count >= _session.Shared.Capacity ? Palette.TrayRimDanger : Palette.TrayRim;
        }

        /// Safety net: if a ball somehow gets knocked out of the basin, drop
        /// it back in from the top — the model counts every parked ball, so a
        /// lost visual ball would make the tray look emptier than it is.
        void FixedUpdate()
        {
            for (int i = 0; i < _balls.Count; i++)
            {
                var b = _balls[i];
                if (b == null) continue;
                var p = b.transform.localPosition;
                if (p.y >= FloorY - 0.3f && p.y <= 2.5f && Mathf.Abs(p.x) <= InnerHalfWidth + 0.25f)
                    continue;
                b.transform.localPosition = new Vector3(Mathf.Clamp(p.x, -1f, 1f), 0.6f, 0f);
                var rb = b.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
