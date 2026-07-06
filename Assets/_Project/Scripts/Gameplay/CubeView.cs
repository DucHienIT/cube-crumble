using CubeBurst.Core;
using CubeBurst.Systems;
using DG.Tweening;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// One tappable cube of the polycube — a real 3D mesh with a BoxCollider
    /// for Physics.Raycast picking.
    public class CubeView : MonoBehaviour
    {
        public int CubeId { get; private set; }
        public GameColor GameColor { get; private set; }
        public Color BaseColor { get; private set; }
        public int GX { get; private set; }
        public int GY { get; private set; }
        public int GZ { get; private set; }

        Vector3 _homePos;
        MeshRenderer _renderer;

        /// The prefab carries MeshFilter/MeshRenderer/BoxCollider (collider
        /// size is a prefab tunable); the procedural mesh and per-color
        /// materials are assigned here.
        public void Init(CubeUnit cube, Vector3 localPos)
        {
            name = $"Cube_{cube.Id}";
            transform.localPosition = localPos;

            GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.UnitCube();
            _renderer = GetComponent<MeshRenderer>();
            _renderer.sharedMaterials = CubeMeshFactory.MaterialsFor(cube.Color);

            CubeId = cube.Id;
            GameColor = cube.Color;
            BaseColor = Palette.Of(cube.Color);
            GX = cube.X;
            GY = cube.Y;
            GZ = cube.Z;
            _homePos = localPos;
        }

        /// Painter-order fallback: cubes closer to the camera draw later, in
        /// case the renderer ignores the depth buffer. Recomputed per
        /// orientation by CubeShapeView.
        public void SetDepthOrder(int sx, int sz)
        {
            _renderer.sortingOrder = 100 + (sx * GX + GY + sz * GZ) * 4;
        }

        /// Feedback for tapping a covered cube.
        public void Shake()
        {
            transform.DOKill(true);
            transform.localPosition = _homePos;
            transform.DOShakePosition(0.18f, 0.07f, 24)
                .OnComplete(() => transform.localPosition = _homePos);
        }
    }
}
