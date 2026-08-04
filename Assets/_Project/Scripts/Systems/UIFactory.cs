using System;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.Systems
{
    /// Builds all uGUI by code. Legacy Text + builtin font — no TMP dependency.
    public static class UIFactory
    {
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// Full-screen container; bg alpha 0 = transparent, no Image at all.
        public static RectTransform CreateScreen(Transform parent, string name, Color bg)
        {
            var rt = CreateRect(parent, name);
            Stretch(rt);
            if (bg.a > 0f)
            {
                var img = rt.gameObject.AddComponent<Image>();
                img.color = bg;
            }
            return rt;
        }

        public static Image CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var rt = CreateRect(parent, name);
            Place(rt, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.UIRounded();
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var rt = CreateRect(parent, name);
            Place(rt, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            Vector2 anchor, Vector2 pos, Vector2 size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = CreateRect(parent, name);
            Place(rt, anchor, pos, size);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.text = content;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos,
            Vector2 size, Color bg, Color textColor, int fontSize, Action onClick)
        {
            var img = CreatePanel(parent, name, anchor, pos, size, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var labelText = CreateText(img.transform, "Label", label, fontSize, textColor,
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Stretch((RectTransform)labelText.transform);
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                onClick?.Invoke();
            });
            return btn;
        }

        public static Text ButtonLabel(Button btn) => btn.GetComponentInChildren<Text>();

        // ---- candy-UI helpers ----

        /// Chunky puzzle-style text: thick dark outline + soft drop shadow.
        public static void AddOutline(Text text, Color color, float thickness = 3f)
        {
            var o = text.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(thickness, -thickness);
            var o2 = text.gameObject.AddComponent<Outline>();
            o2.effectColor = color;
            o2.effectDistance = new Vector2(-thickness, thickness);
            var s = text.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(color.r, color.g, color.b, 0.45f);
            s.effectDistance = new Vector2(0f, -Mathf.Max(4f, thickness + 2f));
        }

        /// Full-screen vertical gradient background (bright top), tinted with the given hue.
        public static Image CreateGradientBG(Transform parent, Color tint)
        {
            var rt = CreateRect(parent, "BG");
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.UIGradient();
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        /// Blurry drop shadow behind a panel/button.
        public static Image CreateSoftShadow(Transform parent, Vector2 size, Vector2 offset)
        {
            var img = CreateImage(parent, "Shadow", SpriteFactory.UISoftShadow(),
                new Vector2(0.5f, 0.5f), offset, size, Palette.ShadowInk);
            img.type = Image.Type.Sliced;
            return img;
        }

        /// White card with drop shadow (level tiles, popup panels). Returns the body image.
        public static Image CreateCard(Transform parent, string name, Vector2 anchor, Vector2 pos,
            Vector2 size, Color color)
        {
            var rt = CreateRect(parent, name);
            Place(rt, anchor, pos, size);
            CreateSoftShadow(rt, size + new Vector2(34f, 30f), new Vector2(0f, -12f));
            var body = CreateImage(rt, "Body", SpriteFactory.UIRounded(),
                new Vector2(0.5f, 0.5f), Vector2.zero, size, color);
            body.type = Image.Type.Sliced;
            return body;
        }

        /// Glossy candy button: drop shadow + baked-gloss body + outlined label + press squash.
        public static Button CreateCandyButton(Transform parent, string name, string label,
            Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize, Action onClick)
        {
            var rt = CreateRect(parent, name);
            Place(rt, anchor, pos, size);

            CreateSoftShadow(rt, size + new Vector2(32f, 28f), new Vector2(0f, -12f));

            var body = CreateImage(rt, "Body", SpriteFactory.UIGloss(),
                new Vector2(0.5f, 0.5f), Vector2.zero, size, color);
            body.type = Image.Type.Sliced;
            body.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = body;
            rt.gameObject.AddComponent<UIPressEffect>();

            if (!string.IsNullOrEmpty(label))
            {
                var text = CreateText(body.transform, "Label", label, fontSize, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), size);
                AddOutline(text, Color.Lerp(color, Color.black, 0.5f));
            }

            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                onClick?.Invoke();
            });
            return btn;
        }
    }
}
