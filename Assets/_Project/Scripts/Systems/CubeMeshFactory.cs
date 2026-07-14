using System.Collections.Generic;
using CubeBurst.Core;
using UnityEngine;

namespace CubeBurst.Systems
{
    /// Real-3D rendering primitives: one shared unit-cube mesh with three
    /// submeshes (top/bottom, z faces, x faces), an inside-out cube for
    /// silhouette outlines, a sphere for balls, and per-color unlit materials
    /// with the shading baked into small textures. Unlit legacy shaders have
    /// no LightMode tag, so the URP 2D renderer draws them (SRPDefaultUnlit)
    /// exactly like it draws sprites — no lights or renderer changes needed.
    public static class CubeMeshFactory
    {
        // brightness per submesh: 0 = top, 1 = left (z faces), 2 = right (x faces)
        static readonly float[] Shades = { 1f, 0.85f, 0.7f };
        static readonly Color SeamInk = new Color(0.12f, 0.12f, 0.16f);

        static Mesh _cube;
        static Mesh _invertedCube;
        static Mesh _sphere;
        static Material _trail;
        static readonly Dictionary<Color, Material[]> TintCache = new Dictionary<Color, Material[]>();
        static readonly Dictionary<Color, Material> BallCache = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> SolidCache = new Dictionary<Color, Material>();

        // Baked mesh assets live under Resources/Meshes/ (see MeshTools "Bake
        // Meshes"). The accessors load them first and only fall back to building
        // at runtime when the assets are missing, so behavior is identical even
        // before the meshes have been baked.
        const string MeshDir = "Meshes/";

        public static Mesh UnitCube()
        {
            if (_cube == null) _cube = LoadOrBuild("CubeBurstCube", () => BuildCube(false));
            return _cube;
        }

        /// Inside-out unit cube: only its far side renders, so a scaled-up
        /// copy behind a cube reads as a silhouette outline (inverted hull).
        public static Mesh InvertedCube()
        {
            if (_invertedCube == null) _invertedCube = LoadOrBuild("CubeBurstCubeHull", () => BuildCube(true));
            return _invertedCube;
        }

        /// Unit-diameter sphere with matcap-style UVs (uv from the normal's
        /// x/y), so the ball texture bakes a fake-lit look with zero lights.
        public static Mesh Sphere()
        {
            if (_sphere == null) _sphere = LoadOrBuild("CubeBurstBall", BuildSphere);
            return _sphere;
        }

        static Mesh LoadOrBuild(string name, System.Func<Mesh> build)
        {
            var asset = Resources.Load<Mesh>(MeshDir + name);
            return asset != null ? asset : build();
        }

        // Editor baking entry points (MeshTools). Each returns a fresh mesh
        // whose name matches its Resources asset file, so re-baking overwrites
        // the right asset.
        public static Mesh BuildUnitCubeMesh() => BuildCube(false);
        public static Mesh BuildInvertedCubeMesh() => BuildCube(true);
        public static Mesh BuildSphereMesh() => BuildSphere();

        public static Material[] MaterialsFor(GameColor color) => MaterialsForTint(Palette.Of(color));

        public static Material[] MaterialsForTint(Color baseColor)
        {
            if (TintCache.TryGetValue(baseColor, out var cached) && cached != null && cached[0] != null)
                return cached;

            var shader = UnlitShader();
            var mats = new Material[Shades.Length];
            for (int i = 0; i < Shades.Length; i++)
            {
                var c = new Color(baseColor.r * Shades[i], baseColor.g * Shades[i], baseColor.b * Shades[i], 1f);
                mats[i] = new Material(shader) { mainTexture = BakeFace(c) };
            }
            TintCache[baseColor] = mats;
            return mats;
        }

        public static Material BallMaterialFor(GameColor color) => BallMaterial(Palette.Of(color));

        public static Material BallMaterial(Color baseColor)
        {
            if (BallCache.TryGetValue(baseColor, out var cached) && cached != null)
                return cached;
            var mat = new Material(UnlitShader()) { mainTexture = BakeBall(baseColor) };
            BallCache[baseColor] = mat;
            return mat;
        }

        /// Flat single-color unlit material (silhouette outline hulls).
        public static Material SolidMaterial(Color color)
        {
            if (SolidCache.TryGetValue(color, out var cached) && cached != null)
                return cached;
            var tex = NewTexture(4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            var mat = new Material(UnlitShader()) { mainTexture = tex };
            SolidCache[color] = mat;
            return mat;
        }

        static Shader UnlitShader()
        {
            // Custom opaque unlit with explicit Cull/ZWrite/ZTest — the legacy
            // built-in "Unlit/Texture" cross-compiles unreliably for GLES/WebGL
            // under the 2D renderer, dropping the cubes' camera-facing faces in
            // WebGL builds. Lives in Resources/ so Shader.Find resolves it in a
            // build (Resources assets are always included).
            var shader = Shader.Find("CubeBurst/UnlitCube");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader;
        }

        /// Transparent, vertex-colored material for ball flight trails. Uses
        /// the sprite shader (alpha blend + per-vertex tint) with a plain white
        /// texture, so a TrailRenderer's colorGradient shows through.
        public static Material TrailMaterial()
        {
            if (_trail != null) return _trail;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = UnlitShader();
            var tex = NewTexture(4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _trail = new Material(shader) { mainTexture = tex };
            return _trail;
        }

        /// Flat toy-block face: solid color, a whisper of vertical gradient,
        /// and a thin near-black seam line at the edges (reference style).
        static Texture2D BakeFace(Color c)
        {
            const int S = 96;
            var tex = NewTexture(S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float edge = Mathf.Min(Mathf.Min(x, S - 1 - x), Mathf.Min(y, S - 1 - y));
                float grad = 0.97f + 0.05f * (y / (float)(S - 1));
                var col = new Color(
                    Mathf.Min(1f, c.r * grad),
                    Mathf.Min(1f, c.g * grad),
                    Mathf.Min(1f, c.b * grad), 1f);
                if (edge < 2.2f)
                    col = Color.Lerp(SeamInk, col, Mathf.Clamp01((edge - 1f) / 1.2f));
                px[y * S + x] = col;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// Radial-lit ball texture sampled through the sphere's matcap UVs:
        /// bright toward an upper-left light with a small white specular.
        static Texture2D BakeBall(Color c)
        {
            const int S = 64;
            var tex = NewTexture(S);
            var px = new Color[S * S];
            var light = new Vector2(0.36f, 0.64f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var uv = new Vector2(x / (float)(S - 1), y / (float)(S - 1));
                float d = Vector2.Distance(uv, light);
                float k = 1.1f - 0.6f * Mathf.Clamp01(d / 0.78f);
                float spec = Mathf.Clamp01(1f - d / 0.15f);
                var col = new Color(
                    Mathf.Min(1f, c.r * k),
                    Mathf.Min(1f, c.g * k),
                    Mathf.Min(1f, c.b * k), 1f);
                px[y * S + x] = Color.Lerp(col, Color.white, spec * 0.65f);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D NewTexture(int size) => new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        static Mesh BuildCube(bool inverted)
        {
            var verts = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var subs = new[] { new List<int>(), new List<int>(), new List<int>() };

            void Face(int sub, Vector3 n, Vector3 up)
            {
                var right = Vector3.Cross(n, up);
                var center = n * 0.5f;
                int i = verts.Count;
                verts.Add(center - right * 0.5f - up * 0.5f);
                verts.Add(center - right * 0.5f + up * 0.5f);
                verts.Add(center + right * 0.5f + up * 0.5f);
                verts.Add(center + right * 0.5f - up * 0.5f);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                if (inverted)
                    subs[sub].AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                else
                    subs[sub].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            Face(0, Vector3.up, Vector3.forward);
            Face(0, Vector3.down, Vector3.forward);
            Face(1, Vector3.forward, Vector3.up);
            Face(1, Vector3.back, Vector3.up);
            Face(2, Vector3.right, Vector3.up);
            Face(2, Vector3.left, Vector3.up);

            var mesh = new Mesh { name = inverted ? "CubeBurstCubeHull" : "CubeBurstCube" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            if (inverted)
            {
                var all = new List<int>(36);
                foreach (var s in subs) all.AddRange(s);
                mesh.SetTriangles(all, 0);
            }
            else
            {
                mesh.subMeshCount = 3;
                for (int s = 0; s < subs.Length; s++) mesh.SetTriangles(subs[s], s);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildSphere()
        {
            const int Rings = 12, Segs = 20;
            var verts = new List<Vector3>((Rings + 1) * (Segs + 1));
            var norms = new List<Vector3>(verts.Capacity);
            var uvs = new List<Vector2>(verts.Capacity);
            for (int r = 0; r <= Rings; r++)
            {
                float theta = Mathf.PI * r / Rings;
                for (int s = 0; s <= Segs; s++)
                {
                    float phi = 2f * Mathf.PI * s / Segs;
                    var n = new Vector3(
                        Mathf.Sin(theta) * Mathf.Cos(phi),
                        Mathf.Cos(theta),
                        Mathf.Sin(theta) * Mathf.Sin(phi));
                    verts.Add(n * 0.5f);
                    norms.Add(n);
                    // matcap-style UV: front hemisphere maps onto the full texture
                    uvs.Add(new Vector2(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f));
                }
            }
            var tris = new List<int>(Rings * Segs * 6);
            for (int r = 0; r < Rings; r++)
            for (int s = 0; s < Segs; s++)
            {
                int a = r * (Segs + 1) + s, b = a + Segs + 1;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
            }

            var mesh = new Mesh { name = "CubeBurstBall" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
