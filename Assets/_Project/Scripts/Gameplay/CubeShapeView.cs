using System.Collections.Generic;
using CubeBurst.Core;
using CubeBurst.Systems;
using DG.Tweening;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Renders the polycube as real 3D cubes under an isometric rotation,
    /// auto-centered and scaled to fit the play area. The player can drag
    /// horizontally to spin it around the vertical axis; on release it snaps
    /// to the nearest 90° and the model's exposure rule follows.
    public class CubeShapeView : MonoBehaviour
    {
        static readonly Vector3 ShapeCenter = new Vector3(0f, 3.4f, 0f);
        const float Pitch = -33f;
        const float BaseYaw = 45f;
        const float MaxWidth = 5.2f;
        const float MaxHeight = 5.6f;
        /// Balls and debris are spawned at this z so they always pass the
        /// depth test against the cube meshes.
        public const float FrontZ = -2.6f;

        [SerializeField] CubeView cubePrefab;
        [SerializeField] DebrisPiece debrisPrefab;

        GameSession _session;
        readonly Dictionary<int, CubeView> _views = new Dictionary<int, CubeView>();
        readonly Dictionary<int, GameObject> _hullWhite = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> _hullBlue = new Dictionary<int, GameObject>();
        Transform _outlineWhite, _outlineBlue;
        float _yaw;           // free-spinning yaw offset while dragging (deg)
        Tween _snapTween;

        public void Init(GameSession session)
        {
            _session = session;
            Build();
        }

        /// Grid z is mirrored so at orientation 0 the three camera-facing
        /// faces are exactly the +x/+y/+z neighbours the exposure rule checks
        /// — level data and the generator stay valid unchanged.
        static Vector3 GridToLocal(CubeUnit c) => new Vector3(c.X, c.Y, -c.Z);

        void Build()
        {
            // pivot at the grid bounding-box center so drag-rotation spins in place
            var gridMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var gridMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var cube in _session.Shape.Cubes)
            {
                var p = GridToLocal(cube);
                gridMin = Vector3.Min(gridMin, p);
                gridMax = Vector3.Max(gridMax, p);
            }
            var gridCenter = (gridMin + gridMax) * 0.5f;

            // fit the worst case across all four snapped orientations so the
            // shape never spins out of the play area
            float width = 0f, height = 0f;
            Vector3 center0 = Vector3.zero;
            for (int k = 0; k < 4; k++)
            {
                var rot = Quaternion.Euler(Pitch, BaseYaw + 90f * k, 0f);
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var cube in _session.Shape.Cubes)
                {
                    var p = rot * (GridToLocal(cube) - gridCenter);
                    min = Vector3.Min(min, p - Vector3.one * 0.75f);
                    max = Vector3.Max(max, p + Vector3.one * 0.75f);
                }
                width = Mathf.Max(width, max.x - min.x);
                height = Mathf.Max(height, max.y - min.y);
                if (k == 0) center0 = (min + max) * 0.5f;
            }

            float scale = Mathf.Min(1f, Mathf.Min(MaxWidth / width, MaxHeight / height));
            transform.localScale = Vector3.one * scale;
            transform.position = ShapeCenter - center0 * scale;

            // white + blue silhouette outline (reference style): enlarged clones
            // of every cube pushed far behind the shape — the ortho camera keeps
            // their screen footprint identical, so their union reads as a fat
            // outline around the whole silhouette, never over the cubes
            _outlineWhite = new GameObject("OutlineWhite").transform;
            _outlineWhite.SetParent(transform, false);
            _outlineBlue = new GameObject("OutlineBlue").transform;
            _outlineBlue.SetParent(transform, false);

            foreach (var cube in _session.Shape.Cubes)
            {
                var local = GridToLocal(cube) - gridCenter;
                var view = Instantiate(cubePrefab, transform);
                view.Init(cube, local);
                _views[cube.Id] = view;
                _hullWhite[cube.Id] = CreateHull(_outlineWhite, local, 1.2f, Color.white, 28);
                _hullBlue[cube.Id] = CreateHull(_outlineBlue, local, 1.29f, Palette.ShapeOutline, 24);
            }
            ApplyRotation();
            UpdateDepthOrder();
        }

        static GameObject CreateHull(Transform parent, Vector3 localPos, float scale, Color color, int order)
        {
            var go = new GameObject("Hull", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.UnitCube();
            var mr = go.GetComponent<MeshRenderer>();
            var mats = new Material[3];
            for (int i = 0; i < 3; i++) mats[i] = CubeMeshFactory.SolidMaterial(color);
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = order;
            return go;
        }

        void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(Pitch, BaseYaw + _yaw, 0f);
            // keep the outline layers on fixed world-z planes behind everything
            if (_outlineWhite != null) _outlineWhite.position = transform.position + Vector3.forward * 8f;
            if (_outlineBlue != null) _outlineBlue.position = transform.position + Vector3.forward * 10f;
        }

        /// Free rotation while the finger drags; taps are suppressed meanwhile.
        public void DragRotate(float deltaDegrees)
        {
            if (_snapTween != null)
            {
                _snapTween.Kill();
                _snapTween = null;
            }
            _yaw += deltaDegrees;
            ApplyRotation();
        }

        /// Snaps to the nearest 90° step and syncs the exposure rule to it.
        public void EndDrag()
        {
            int k = Mathf.RoundToInt(_yaw / 90f);
            _session.Shape.SetOrientation(k);
            UpdateDepthOrder();
            _snapTween = DOTween.To(() => _yaw, v => { _yaw = v; ApplyRotation(); }, k * 90f, 0.22f)
                .SetEase(Ease.OutCubic);
        }

        void UpdateDepthOrder()
        {
            CubeShapeModel.FacingSigns(_session.Shape.Orientation, out int sx, out int sz);
            foreach (var view in _views.Values)
                view.SetDepthOrder(sx, sz);
        }

        public CubeView GetView(int cubeId) => _views.TryGetValue(cubeId, out var v) ? v : null;

        public void RemoveCube(int cubeId)
        {
            if (!_views.TryGetValue(cubeId, out var view)) return;
            var debrisPos = view.transform.position;
            debrisPos.z = FrontZ;
            DebrisBurst.Spawn(debrisPrefab, transform.parent, debrisPos, view.GameColor);
            _views.Remove(cubeId);
            Destroy(view.gameObject);
            if (_hullWhite.TryGetValue(cubeId, out var hw)) { Destroy(hw); _hullWhite.Remove(cubeId); }
            if (_hullBlue.TryGetValue(cubeId, out var hb)) { Destroy(hb); _hullBlue.Remove(cubeId); }
        }
    }
}
