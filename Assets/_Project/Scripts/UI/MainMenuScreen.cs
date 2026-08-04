using CubeBurst.Core;
using CubeBurst.Gameplay;
using CubeBurst.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class MainMenuScreen
    {
        public GameObject Root { get; private set; }

        Text _playLabel;
        Text _soundLabel;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            var root = UIFactory.CreateScreen(canvas, "MainMenu", Palette.Background);
            Root = root.gameObject;

            UIFactory.CreateGradientBG(root, Palette.SkyBg);
            AddStripes(root);
            AddFloatingCubes(root);

            // title spelled out on tilted color tiles, like puzzle blocks
            BuildTileWord(root, "CUBE", new Vector2(0.5f, 0.76f), 168f,
                new[] { GameColor.Red, GameColor.Yellow, GameColor.Blue, GameColor.Green });
            BuildTileWord(root, "BURST", new Vector2(0.5f, 0.655f), 138f,
                new[] { GameColor.Purple, GameColor.Blue, GameColor.Yellow, GameColor.Red, GameColor.Green });

            var subtitle = UIFactory.CreateText(root, "Subtitle", "Tap. Crumble. Sort.", 46, Color.white,
                new Vector2(0.5f, 0.575f), Vector2.zero, new Vector2(900f, 80f));
            UIFactory.AddOutline(subtitle, new Color(0.11f, 0.17f, 0.34f, 0.8f), 2f);

            var play = UIFactory.CreateCandyButton(root, "Play", "PLAY", new Vector2(0.5f, 0.44f), Vector2.zero,
                new Vector2(560f, 165f), Palette.BtnGreen, 68,
                () => gm.StartLevel(Mathf.Min(SaveSystem.UnlockedLevel, GameManager.TotalLevels)));
            _playLabel = UIFactory.ButtonLabel(play);

            UIFactory.CreateCandyButton(root, "Levels", "LEVELS", new Vector2(0.5f, 0.325f), Vector2.zero,
                new Vector2(560f, 135f), Palette.BtnBlue, 56,
                ui.ShowLevelSelect);

            var sound = UIFactory.CreateCandyButton(root, "Sound", "SOUND: ON", new Vector2(0.5f, 0.225f), Vector2.zero,
                new Vector2(430f, 105f), Palette.BtnSlate, 42,
                () =>
                {
                    AudioManager.Instance.SoundOn = !AudioManager.Instance.SoundOn;
                    Refresh();
                });
            _soundLabel = UIFactory.ButtonLabel(sound);
        }

        static void AddStripes(RectTransform root)
        {
            var rt = UIFactory.CreateRect(root, "Stripes");
            UIFactory.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.Stripes();
            img.type = Image.Type.Tiled;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        static void AddFloatingCubes(RectTransform root)
        {
            // (anchor, size, color, alpha, phase) — scattered around the edges
            var deco = new (Vector2 anchor, Vector2 pos, float size, GameColor color, float alpha, float phase)[]
            {
                (new Vector2(0.10f, 0.90f), Vector2.zero, 120f, GameColor.Red, 0.85f, 0.0f),
                (new Vector2(0.90f, 0.87f), Vector2.zero, 100f, GameColor.Blue, 0.85f, 1.3f),
                (new Vector2(0.06f, 0.52f), Vector2.zero, 84f, GameColor.Green, 0.6f, 2.1f),
                (new Vector2(0.94f, 0.48f), Vector2.zero, 92f, GameColor.Yellow, 0.6f, 3.0f),
                (new Vector2(0.12f, 0.13f), Vector2.zero, 110f, GameColor.Purple, 0.75f, 4.2f),
                (new Vector2(0.88f, 0.10f), Vector2.zero, 96f, GameColor.Green, 0.75f, 5.1f),
            };
            foreach (var d in deco)
            {
                var c = Palette.Of(d.color);
                c.a = d.alpha;
                var img = UIFactory.CreateImage(root, "Deco", SpriteFactory.IsoCube(),
                    d.anchor, d.pos, new Vector2(d.size, d.size), c);
                var motion = img.gameObject.AddComponent<UIMotion>();
                motion.BobAmp = 12f;
                motion.BobSpeed = 0.9f;
                motion.RotAmp = 6f;
                motion.Phase = d.phase;
            }
        }

        static void BuildTileWord(RectTransform root, string word, Vector2 anchor, float tileSize, GameColor[] colors)
        {
            float spacing = tileSize + 14f;
            float x0 = -(word.Length - 1) * spacing * 0.5f;
            for (int i = 0; i < word.Length; i++)
            {
                var rt = UIFactory.CreateRect(root, $"Tile{word}{i}");
                UIFactory.Place(rt, anchor, new Vector2(x0 + i * spacing, 0f), new Vector2(tileSize, tileSize));
                rt.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 5f);

                UIFactory.CreateSoftShadow(rt, new Vector2(tileSize + 26f, tileSize + 24f), new Vector2(0f, -9f));
                var body = UIFactory.CreateImage(rt, "Body", SpriteFactory.UIGloss(),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(tileSize, tileSize), Palette.Of(colors[i]));
                body.type = Image.Type.Sliced;

                var letter = UIFactory.CreateText(body.transform, "Letter", word[i].ToString(),
                    Mathf.RoundToInt(tileSize * 0.62f), Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(tileSize, tileSize));
                UIFactory.AddOutline(letter, Color.Lerp(Palette.Of(colors[i]), Color.black, 0.55f));

                var motion = rt.gameObject.AddComponent<UIMotion>();
                motion.BobAmp = 6f;
                motion.BobSpeed = 1.4f;
                motion.RotAmp = 2.5f;
                motion.Phase = i * 0.65f + (word.Length > 4 ? 0.3f : 0f);
            }
        }

        public void Refresh()
        {
            int level = Mathf.Min(SaveSystem.UnlockedLevel, GameManager.TotalLevels);
            _playLabel.text = $"PLAY - LVL {level}";
            if (AudioManager.Instance != null)
                _soundLabel.text = AudioManager.Instance.SoundOn ? "SOUND: ON" : "SOUND: OFF";
        }
    }
}
