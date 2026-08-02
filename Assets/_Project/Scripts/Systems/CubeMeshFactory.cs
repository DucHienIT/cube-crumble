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
        // brightness per submesh: 0 = top, 1 = left (z faces), 2 = right (x faces).
        // Stronger face-to-face contrast so the polycube reads as a solid 3D
        // volume rather than flat isometric art.
        static readonly float[] Shades = { 1f, 0.78f, 0.6f };
        static readonly Color SeamInk = new Color(0.12f, 0.12f, 0.16f);

        static Mesh _cube;
        static Mesh _invertedCube;
        static Mesh _sphere;
        static Mesh _pill;
        static Material _trail;
        static readonly Dictionary<Color, Material[]> TintCache = new Dictionary<Color, Material[]>();
        static readonly Dictionary<Color, Material[]> PillCache = new Dictionary<Color, Material[]>();
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

        /// Rounded-rectangle loaf: a real 3D container body (front cap + side
        /// wall + back cap) so the pills read as solid objects, not flat art.
        /// submesh 0 = front cap (glossy top), 1 = side wall (dark thickness),
        /// 2 = back cap.
        public static Mesh PillMesh()
        {
            if (_pill == null) _pill = LoadOrBuild("CubeBurstPill", BuildPill);
            return _pill;
        }

        public static Mesh BuildPillMeshAsset() => BuildPill();

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

        /// Shaded 3D cube face: a solid color lit by a directional gradient
        /// (brighter toward the top) plus soft ambient-occlusion darkening
        /// toward the outer border, so each face reads as a lit volume rather
        /// than flat art. Thin near-black seam line at the edges (reference).
        static Texture2D BakeFace(Color c)
        {
            const int S = 96;
            var tex = NewTexture(S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float fx = x / (float)(S - 1);
                float fy = y / (float)(S - 1);
                float edge = Mathf.Min(Mathf.Min(x, S - 1 - x), Mathf.Min(y, S - 1 - y));
                // directional light: brighter toward the top, gentle diagonal
                float grad = 0.90f + 0.16f * fy + 0.05f * (1f - fx);
                // soft AO: the face darkens toward its outer rim, giving depth
                float ao = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 24f));
                grad *= 0.86f + 0.14f * ao;
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

        /// Soft matte ball texture sampled through the sphere's matcap UVs:
        /// a gentle light falloff for volume and a broad, low-intensity sheen
        /// instead of a tight steel-ball specular — reads soft, not hard.
        static Texture2D BakeBall(Color c)
        {
            const int S = 128; // hi-res so the gradient + highlight read smooth, no banding
            var tex = NewTexture(S);
            var px = new Color[S * S];
            // soft light from the upper-left, matching the reference balls
            var light = new Vector2(0.38f, 0.62f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var uv = new Vector2(x / (float)(S - 1), y / (float)(S - 1));
                float d = Vector2.Distance(uv, light);
                // rich matte body: bright near the light, gently darker toward the
                // far (lower-right) rim so the ball reads round and volumetric
                float k = 1.10f - 0.42f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / 1.05f));
                var col = new Color(
                    Mathf.Min(1f, c.r * k),
                    Mathf.Min(1f, c.g * k),
                    Mathf.Min(1f, c.b * k), 1f);
                // broad, soft glossy highlight (no hard specular dot) — a wide
                // white bloom near the light, feathered out
                float sheen = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / 0.46f));
                px[y * S + x] = Color.Lerp(col, Color.white, sheen * sheen * 0.4f);
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

        public static Material[] PillMaterialsFor(GameColor color) => PillMaterials(Palette.Of(color));

        /// Materials for the 3D pill: submesh 0 = glossy top cap (baked
        /// rounded-rect gloss + dark rim texture), 1/2 = dark side/back walls
        /// that read as the container's thickness.
        public static Material[] PillMaterials(Color c)
        {
            if (PillCache.TryGetValue(c, out var cached) && cached != null && cached[0] != null)
                return cached;

            var shader = UnlitShader();
            var cap = new Material(shader) { mainTexture = BakePillCap(c) };

            var wallTex = NewTexture(4);
            var wc = new Color(c.r * 0.34f, c.g * 0.34f, c.b * 0.30f, 1f);
            var wp = new Color[16];
            for (int i = 0; i < wp.Length; i++) wp[i] = wc;
            wallTex.SetPixels(wp);
            wallTex.Apply();
            var wall = new Material(shader) { mainTexture = wallTex };

            var mats = new[] { cap, wall, wall };
            PillCache[c] = mats;
            return mats;
        }

        /// Glossy rounded-rect top texture for the pill cap: vertical gloss,
        /// bright top band, dark bottom lip and a thick near-black rim — the
        /// candy look, now on a real 3D cap.
        static Texture2D BakePillCap(Color c)
        {
            const int W = 176, H = 96;
            const float radius = 34f;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color[W * H];
            var c0 = new Vector2(W / 2f, H / 2f);
            var half = new Vector2(W / 2f - 1f, H / 2f - 1f);
            var ink = new Color(0.06f, 0.06f, 0.09f, 1f);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dist = RoundedRectSdf(new Vector2(x + 0.5f, y + 0.5f) - c0, half, radius);
                float g = 0.82f + 0.20f * (y / (float)(H - 1));
                if (y < 18) g *= 0.60f + 0.40f * (y / 18f);
                if (y > H - 26 && dist < -9f)
                    g = Mathf.Min(1.12f, g + 0.18f * Mathf.Clamp01((y - (H - 26)) / 18f));
                var col = new Color(Mathf.Min(1f, c.r * g), Mathf.Min(1f, c.g * g), Mathf.Min(1f, c.b * g), 1f);
                float border = Mathf.Clamp01((dist + 7f) / 6.5f);
                if (dist > -0.5f) border = 1f; // fully dark outside the rounded rect
                px[y * W + x] = Color.Lerp(col, ink, border);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static float RoundedRectSdf(Vector2 p, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                   + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// A rounded-rectangle loaf (front cap toward -z, side wall, back cap).
        /// Proportioned to match the old pill sprite so the container layout is
        /// unchanged, plus real depth along z.
        static Mesh BuildPill()
        {
            const float hw = 0.781f, hh = 0.406f, hd = 0.22f, r = 0.34f;
            const int arc = 5;

            var outline = new List<Vector2>();
            Vector2[] cc =
            {
                new Vector2(hw - r, hh - r),
                new Vector2(-(hw - r), hh - r),
                new Vector2(-(hw - r), -(hh - r)),
                new Vector2(hw - r, -(hh - r)),
            };
            float[] startDeg = { 0f, 90f, 180f, 270f };
            for (int ci = 0; ci < 4; ci++)
            for (int s = 0; s <= arc; s++)
            {
                float a = Mathf.Deg2Rad * (startDeg[ci] + 90f * s / arc);
                outline.Add(cc[ci] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            int n = outline.Count;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var capF = new List<int>();
            var wall = new List<int>();
            var capB = new List<int>();

            Vector2 Uv(Vector2 o) => new Vector2(o.x / (2f * hw) + 0.5f, o.y / (2f * hh) + 0.5f);

            int fc = verts.Count; verts.Add(new Vector3(0f, 0f, -hd)); uvs.Add(new Vector2(0.5f, 0.5f));
            int fs = verts.Count;
            for (int i = 0; i < n; i++) { verts.Add(new Vector3(outline[i].x, outline[i].y, -hd)); uvs.Add(Uv(outline[i])); }
            for (int i = 0; i < n; i++) { int a = fs + i, b = fs + (i + 1) % n; capF.Add(fc); capF.Add(b); capF.Add(a); }

            int bc = verts.Count; verts.Add(new Vector3(0f, 0f, hd)); uvs.Add(new Vector2(0.5f, 0.5f));
            int bs = verts.Count;
            for (int i = 0; i < n; i++) { verts.Add(new Vector3(outline[i].x, outline[i].y, hd)); uvs.Add(new Vector2(0.5f, 0.5f)); }
            for (int i = 0; i < n; i++) { int a = bs + i, b = bs + (i + 1) % n; capB.Add(bc); capB.Add(a); capB.Add(b); }

            for (int i = 0; i < n; i++)
            {
                int f0 = fs + i, f1 = fs + (i + 1) % n, b0 = bs + i, b1 = bs + (i + 1) % n;
                wall.Add(f0); wall.Add(f1); wall.Add(b1);
                wall.Add(f0); wall.Add(b1); wall.Add(b0);
            }

            var mesh = new Mesh { name = "CubeBurstPill" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(capF, 0);
            mesh.SetTriangles(wall, 1);
            mesh.SetTriangles(capB, 2);
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
