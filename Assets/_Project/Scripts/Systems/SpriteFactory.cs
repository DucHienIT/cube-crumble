using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeBurst.Systems
{
    /// Generates every sprite in the game at runtime — no image assets needed.
    /// All sprites are white/grayscale and tinted via renderer color.
    public static class SpriteFactory
    {
        public const int PPU = 128;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Color Ink = new Color(0.16f, 0.2f, 0.38f, 1f); // baked outline tone

        public static Sprite IsoCube() => Cached("isocube", CreateIsoCube);
        public static Sprite Ball() => Cached("ball", CreateBall);
        public static Sprite Dot() => Cached("dot", () => CreateFilledCircle(40));
        public static Sprite Ring() => Cached("ring", CreateRing);
        public static Sprite Box() => Cached("box", () => CreateRoundedRect(176, 176, 30, 7));
        public static Sprite Tray() => Cached("tray", CreateTray);
        public static Sprite Star() => Cached("star", CreateStar);
        public static Sprite UIRounded() => Cached("uiRounded", CreateUIRounded);

        // reference-art sprites
        public static Sprite BigRounded() => Cached("bigRounded", CreateBigRounded);
        public static Sprite Stripes() => Cached("stripes", CreateStripes);
        public static Sprite PolkaPanel() => Cached("polka", CreatePolkaPanel);
        public static Sprite BasinRim() => Cached("basinRim", CreateBasinRim);
        public static Sprite Dashes() => Cached("dashes", CreateDashes);
        public static Sprite Pill() => Cached("pill", CreatePill);
        public static Sprite Socket() => Cached("socket", CreateSocket);
        public static Sprite Stopwatch() => Cached("stopwatch", CreateStopwatch);

        static Sprite Cached(string key, Func<Sprite> create)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = create();
                Cache[key] = s;
            }
            return s;
        }

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            return t;
        }

        static Sprite ToSprite(Texture2D tex, Color[] px, Vector4 border = default)
        {
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, border);
        }

        /// 128x128 isometric cube: top/left/right faces at different brightness
        /// with dark seams, so a flat tint reads as a shaded 3D block.
        static Sprite CreateIsoCube()
        {
            const int S = 128;
            var tex = NewTex(S, S);
            var px = new Color[S * S];

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                // hexagon silhouette: (0,96)(64,128)(128,96)(128,32)(64,0)(0,32)
                float inside = Mathf.Min(
                    Mathf.Min((96f + 0.5f * fx) - fy, (160f - 0.5f * fx) - fy),
                    Mathf.Min(
                        Mathf.Min(fy - (32f - 0.5f * fx), fy - (0.5f * fx - 32f)),
                        Mathf.Min(fx, 128f - fx)));

                if (inside <= 0f)
                {
                    px[y * S + x] = Color.clear;
                    continue;
                }

                // top diamond bottom edges: L(0,96)-B(64,64) and B(64,64)-R(128,96)
                float topEdgeL = fy - (96f - 0.5f * fx);
                float topEdgeR = fy - (32f + 0.5f * fx);
                bool isTop = topEdgeL >= 0f && topEdgeR >= 0f;
                float shade = isTop ? 1f : (fx < 64f ? 0.8f : 0.62f);

                float seam = Mathf.Min(Mathf.Abs(topEdgeL), Mathf.Abs(topEdgeR));
                if (!isTop) seam = Mathf.Min(seam, Mathf.Abs(fx - 64f));
                float edge = Mathf.Min(inside, seam);

                var c = new Color(shade, shade, shade, 1f);
                if (edge < 3.5f) c = Color.Lerp(Ink, c, Mathf.Clamp01((edge - 2f) / 1.5f));
                c.a = Mathf.Clamp01(inside / 1.5f);
                px[y * S + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// 96x96 shaded ball with dark rim (top-left light source baked in).
        static Sprite CreateBall()
        {
            const int S = 96;
            const float r = 44f;
            var tex = NewTex(S, S);
            var px = new Color[S * S];
            Vector2 center = new Vector2(S / 2f, S / 2f);
            Vector2 lightPos = center + new Vector2(-13f, 13f);

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, center);
                if (d > r + 1f)
                {
                    px[y * S + x] = Color.clear;
                    continue;
                }
                float shade = 1f - 0.34f * Mathf.Clamp01(Vector2.Distance(p, lightPos) / (r * 1.6f));
                var c = new Color(shade, shade, shade, 1f);
                if (d > r - 4f) c = Color.Lerp(c, Ink, Mathf.Clamp01((d - (r - 4f)) / 2.5f));
                c.a = Mathf.Clamp01(r + 0.75f - d);
                px[y * S + x] = c;
            }
            return ToSprite(tex, px);
        }

        static Sprite CreateFilledCircle(int size)
        {
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            float r = size / 2f - 1f, c0 = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - c0) * (x + 0.5f - c0) + (y + 0.5f - c0) * (y + 0.5f - c0));
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.75f));
            }
            return ToSprite(tex, px);
        }

        /// Thin circle outline, used as capacity markers in the shared tray.
        static Sprite CreateRing()
        {
            const int S = 64;
            const float r = 27f;
            var tex = NewTex(S, S);
            var px = new Color[S * S];
            float c0 = S / 2f;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - c0) * (x + 0.5f - c0) + (y + 0.5f - c0) * (y + 0.5f - c0));
                float band = 2.5f - Mathf.Abs(d - r);
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(band));
            }
            return ToSprite(tex, px);
        }

        /// Rounded rect with baked dark border — container boxes.
        static Sprite CreateRoundedRect(int w, int h, float radius, float borderW)
        {
            var tex = NewTex(w, h);
            var px = new Color[w * h];
            var half = new Vector2(w / 2f - 1f, h / 2f - 1f);
            var c0 = new Vector2(w / 2f, h / 2f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - c0;
                var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
                float dist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                             + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                if (dist > 0.75f)
                {
                    px[y * w + x] = Color.clear;
                    continue;
                }
                var c = Color.white;
                if (dist > -borderW) c = Color.Lerp(c, Ink, Mathf.Clamp01((dist + borderW) / 2f + 0.6f));
                c.a = Mathf.Clamp01(-dist + 0.75f);
                px[y * w + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// Open-top trapezoid tray (the shared slot).
        static Sprite CreateTray()
        {
            const int W = 480, H = 200;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            float cx = W / 2f;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float t = y / (float)H;
                float hw = Mathf.Lerp(152f, 234f, t);
                float dx = hw - Mathf.Abs(x + 0.5f - cx);
                float dy = y + 0.5f;
                float inside = Mathf.Min(dx, Mathf.Min(dy, H - 0.5f - y));
                if (inside <= 0f)
                {
                    px[y * W + x] = Color.clear;
                    continue;
                }
                var c = Color.white;
                // side + bottom borders only; top edge stays open
                float borderDist = Mathf.Min(dx, dy);
                if (borderDist < 7f) c = Color.Lerp(Ink, c, Mathf.Clamp01((borderDist - 5f) / 2f));
                c.a = Mathf.Clamp01(inside / 1.5f);
                px[y * W + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// Five-point star (result screen / level select), supersampled.
        static Sprite CreateStar()
        {
            const int S = 96;
            var pts = new Vector2[10];
            var c0 = new Vector2(S / 2f, S / 2f);
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 45f : 19f;
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                pts[i] = c0 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }

            var tex = NewTex(S, S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (PointInPolygon(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f), pts))
                        hits++;
                px[y * S + x] = new Color(1f, 1f, 1f, hits / 4f);
            }
            return ToSprite(tex, px);
        }

        static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (poly[i].y > p.y != poly[j].y > p.y &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        static float RoundedRectSdf(Vector2 p, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                   + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// Large-radius 9-sliced rounded rect for world backdrops (panel,
        /// pillars) via SpriteRenderer drawMode Sliced.
        static Sprite CreateBigRounded()
        {
            const int S = 192;
            const float radius = 64f;
            var tex = NewTex(S, S);
            var px = new Color[S * S];
            var c0 = new Vector2(S / 2f, S / 2f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dist = RoundedRectSdf(new Vector2(x + 0.5f, y + 0.5f) - c0,
                    new Vector2(S / 2f - 1f, S / 2f - 1f), radius);
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(-dist + 0.75f));
            }
            return ToSprite(tex, px, new Vector4(72, 72, 72, 72));
        }

        /// Faint vertical stripes for the top background.
        static Sprite CreateStripes()
        {
            const int S = 256;
            var tex = NewTex(S, S);
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                bool on = (x / 32) % 2 == 0;
                px[y * S + x] = new Color(1f, 1f, 1f, on ? 0.06f : 0f);
            }
            return ToSprite(tex, px);
        }

        /// Polka-dot interior panel of the shared basin (colors baked in).
        static Sprite CreatePolkaPanel()
        {
            const int W = 256, H = 192;
            var baseCol = new Color(0.984f, 0.99f, 1f, 1f);
            var dotCol = new Color(0.851f, 0.886f, 0.957f, 1f);
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            var c0 = new Vector2(W / 2f, H / 2f);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dist = RoundedRectSdf(new Vector2(x + 0.5f, y + 0.5f) - c0,
                    new Vector2(W / 2f - 1f, H / 2f - 1f), 20f);
                if (dist > 0.75f)
                {
                    px[y * W + x] = Color.clear;
                    continue;
                }
                // staggered dot grid
                int row = y / 26;
                float ox = row % 2 == 0 ? 0f : 15f;
                float dx = Mathf.Repeat(x + ox, 30f) - 15f;
                float dy = Mathf.Repeat(y, 26f) - 13f;
                float dd = Mathf.Sqrt(dx * dx + dy * dy);
                var c = Color.Lerp(dotCol, baseCol, Mathf.Clamp01(dd - 5.5f));
                c.a = Mathf.Clamp01(-dist + 0.75f);
                px[y * W + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// U-shaped basin rim: a rounded-rect ring open at the top, ends
        /// capped with circles. White — tinted pink (or red when full).
        static Sprite CreateBasinRim()
        {
            const int W = 416, H = 224;
            var center = new Vector2(208f, 112f);
            var half = new Vector2(188f, 96f);
            const float radius = 46f, t = 13f, yOpen = 158f;
            var capL = new Vector2(center.x - half.x, yOpen);
            var capR = new Vector2(center.x + half.x, yOpen);

            var tex = NewTex(W, H);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float ring = Mathf.Abs(RoundedRectSdf(p - center, half, radius)) - t;
                float d = p.y <= yOpen ? ring : float.MaxValue;
                d = Mathf.Min(d, Vector2.Distance(p, capL) - t);
                d = Mathf.Min(d, Vector2.Distance(p, capR) - t);
                px[y * W + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(-d + 0.75f));
            }
            return ToSprite(tex, px);
        }

        /// Horizontal dashed line (the basin's fill limit).
        static Sprite CreateDashes()
        {
            const int W = 256, H = 14;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float inDash = Mathf.Repeat(x, 36f);
                float dx = inDash < 22f ? Mathf.Min(inDash, 22f - inDash) : -1f;
                float dy = Mathf.Min(y, H - 1 - y);
                float a = dx < 0f ? 0f : Mathf.Clamp01(Mathf.Min(dx, dy) + 0.5f);
                px[y * W + x] = new Color(1f, 1f, 1f, a);
            }
            return ToSprite(tex, px);
        }

        /// Glossy "loaf" pill for containers: white-tintable with baked
        /// vertical gradient, dark rim, and bottom shadow.
        static Sprite CreatePill()
        {
            const int W = 200, H = 104;
            const float radius = 44f;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            var c0 = new Vector2(W / 2f, H / 2f);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dist = RoundedRectSdf(new Vector2(x + 0.5f, y + 0.5f) - c0,
                    new Vector2(W / 2f - 1f, H / 2f - 1f), radius);
                if (dist > 0.75f)
                {
                    px[y * W + x] = Color.clear;
                    continue;
                }
                float g = 0.86f + 0.14f * (y / (float)(H - 1));   // top brighter
                if (y < 16) g *= 0.82f + 0.18f * (y / 16f);        // bottom shadow lip
                if (dist > -3f) g *= 0.5f;                         // dark rim
                var c = new Color(g, g, g, Mathf.Clamp01(-dist + 0.75f));
                px[y * W + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// Dark circular hole on the active container (colors baked in).
        static Sprite CreateSocket()
        {
            const int S = 56;
            var hole = new Color(0.15f, 0.17f, 0.24f, 1f);
            var tex = NewTex(S, S);
            var px = new Color[S * S];
            float c0 = S / 2f, r = 25f;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c0, c0));
                var c = hole;
                // subtle inner-shadow: darker toward the top of the hole
                float k = 1f - 0.35f * Mathf.Clamp01((y - c0) / r);
                c = new Color(hole.r * k, hole.g * k, hole.b * k, 1f);
                c.a = Mathf.Clamp01(r - d + 0.75f);
                px[y * S + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// Small yellow stopwatch icon for the timer pill (colors baked in).
        static Sprite CreateStopwatch()
        {
            const int S = 72;
            var body = new Color(1f, 0.79f, 0.23f, 1f);
            var face = new Color(1f, 0.97f, 0.9f, 1f);
            var hand = new Color(0.48f, 0.29f, 0.12f, 1f);
            var tex = NewTex(S, S);
            var px = new Color[S * S];
            var c0 = new Vector2(36f, 32f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, c0);
                Color c = Color.clear;
                if (d < 27f)
                {
                    c = d < 20f ? face : body;
                    // hand: segment from center toward upper-right
                    var dir = new Vector2(0.64f, 0.77f);
                    float along = Vector2.Dot(p - c0, dir);
                    float across = Mathf.Abs((p.x - c0.x) * dir.y - (p.y - c0.y) * dir.x);
                    if (d < 20f && along > -2f && along < 15f && across < 2.5f) c = hand;
                    c.a = Mathf.Clamp01(27f - d + 0.75f);
                }
                else if (Mathf.Abs(p.x - 36f) < 7f && p.y > 56f && p.y < 68f)
                {
                    c = body; // crown button on top
                }
                px[y * S + x] = c;
            }
            return ToSprite(tex, px);
        }

        /// 64x64 9-sliced rounded rect for all UI panels/buttons.
        static Sprite CreateUIRounded()
        {
            const int S = 64;
            const float radius = 20f;
            var tex = NewTex(S, S);
            var px = new Color[S * S];
            var half = new Vector2(S / 2f - 1f, S / 2f - 1f);
            var c0 = new Vector2(S / 2f, S / 2f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - c0;
                var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
                float dist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                             + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(-dist + 0.75f));
            }
            return ToSprite(tex, px, new Vector4(26, 26, 26, 26));
        }
    }
}
