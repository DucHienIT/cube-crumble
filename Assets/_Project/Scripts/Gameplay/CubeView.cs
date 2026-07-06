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

        public static CubeView Create(Transform parent, CubeUnit cube, Vector3 localPos)
        {
            var go = new GameObject($"Cube_{cube.Id}", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            go.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.UnitCube();
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterials = CubeMeshFactory.MaterialsFor(cube.Color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            go.GetComponent<BoxCollider>().size = Vector3.one * 0.98f;

            var view = go.AddComponent<CubeView>();
            view.CubeId = cube.Id;
            view.GameColor = cube.Color;
            view.BaseColor = Palette.Of(cube.Color);
            view.GX = cube.X;
            view.GY = cube.Y;
            view.GZ = cube.Z;
            view._homePos = localPos;
            view._renderer = mr;
            return view;
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
