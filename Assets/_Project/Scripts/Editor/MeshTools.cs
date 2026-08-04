using System.IO;
using CubeBurst.Systems;
using UnityEditor;
using UnityEngine;

namespace CubeBurst.EditorTools
{
    /// <summary>
    /// Bakes the procedurally-built gameplay meshes (unit cube, inverted-hull
    /// cube for silhouette outlines, matcap ball sphere) into .asset files under
    /// Resources/Meshes/, so CubeMeshFactory loads them instead of regenerating
    /// them every run. Re-baking overwrites in place (stable GUIDs).
    /// </summary>
    public static class MeshTools
    {
        const string Dir = "Assets/_Project/Resources/Meshes";

        [MenuItem("Tools/Cube Burst/Bake Meshes")]
        public static void BakeMeshes()
        {
            Directory.CreateDirectory(Dir);
            Save(CubeMeshFactory.BuildUnitCubeMesh(), "CubeBurstCube");
            Save(CubeMeshFactory.BuildInvertedCubeMesh(), "CubeBurstCubeHull");
            Save(CubeMeshFactory.BuildSphereMesh(), "CubeBurstBall");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CubeBurst] Baked 3 meshes → {Dir}");
        }

        static void Save(Mesh mesh, string name)
        {
            var path = $"{Dir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                // Overwrite the existing asset's data to keep its GUID stable.
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
        }
    }
}
