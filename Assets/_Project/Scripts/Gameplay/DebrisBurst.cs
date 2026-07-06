using CubeBurst.Core;
using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Small shard burst when a cube crumbles — tumbling mini 3D cubes that
    /// shrink out instead of alpha-fading (the unlit materials are opaque).
    public static class DebrisBurst
    {
        /// The prefab carries MeshFilter/MeshRenderer/DebrisPiece; the
        /// procedural mesh/materials and randomized motion are set here.
        public static void Spawn(DebrisPiece prefab, Transform parent, Vector3 pos, GameColor color)
        {
            for (int i = 0; i < 8; i++)
            {
                var piece = Object.Instantiate(prefab, parent);
                piece.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f);
                piece.transform.rotation = Random.rotation;

                piece.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.UnitCube();
                piece.GetComponent<MeshRenderer>().sharedMaterials = CubeMeshFactory.MaterialsFor(color);

                piece.Velocity = new Vector2(Random.Range(-2.4f, 2.4f), Random.Range(1.5f, 4.2f));
                piece.TumbleAxis = Random.onUnitSphere;
                piece.TumbleSpeed = Random.Range(180f, 480f);
                piece.BaseScale = Random.Range(0.14f, 0.24f);
                piece.transform.localScale = Vector3.one * piece.BaseScale;
            }
        }
    }
}
