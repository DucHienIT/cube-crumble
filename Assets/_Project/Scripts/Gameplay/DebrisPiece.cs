using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// One tumbling shard of a DebrisBurst. Lives on DebrisPiece.prefab —
    /// it must be in its own file (class name == file name) so Unity can
    /// serialize the prefab's script reference.
    public class DebrisPiece : MonoBehaviour
    {
        public Vector2 Velocity;
        public Vector3 TumbleAxis;
        public float TumbleSpeed;
        public float BaseScale;

        const float MaxLife = 0.55f;
        float _life;

        void Update()
        {
            _life += Time.deltaTime;
            if (_life >= MaxLife)
            {
                Destroy(gameObject);
                return;
            }
            Velocity += Vector2.down * (14f * Time.deltaTime);
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            transform.Rotate(TumbleAxis, TumbleSpeed * Time.deltaTime, Space.World);
            transform.localScale = Vector3.one * (BaseScale * (1f - _life / MaxLife));
        }
    }
}
