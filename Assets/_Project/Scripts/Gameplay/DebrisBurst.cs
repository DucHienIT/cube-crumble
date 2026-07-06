using CubeBurst.Core;
using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Small shard burst when a cube crumbles — tumbling mini 3D cubes that
    /// shrink out instead of alpha-fading (the unlit materials are opaque).
    public static class DebrisBurst
    {
        public static void Spawn(Transform parent, Vector3 pos, GameColor color)
        {
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("Debris", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                go.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f);
                go.transform.rotation = Random.rotation;

                go.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.UnitCube();
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterials = CubeMeshFactory.MaterialsFor(color);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.sortingOrder = 450;

                var piece = go.AddComponent<DebrisPiece>();
                piece.Velocity = new Vector2(Random.Range(-2.4f, 2.4f), Random.Range(1.5f, 4.2f));
                piece.TumbleAxis = Random.onUnitSphere;
                piece.TumbleSpeed = Random.Range(180f, 480f);
                piece.BaseScale = Random.Range(0.14f, 0.24f);
                go.transform.localScale = Vector3.one * piece.BaseScale;
            }
        }
    }

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
