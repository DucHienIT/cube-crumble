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

            // decorative cube trio above the title
            var mid = new Vector2(0.5f, 0.78f);
            UIFactory.CreateImage(root, "CubeL", SpriteFactory.IsoCube(), mid, new Vector2(-150f, -30f), new Vector2(190f, 190f), Palette.Of(Core.GameColor.Red));
            UIFactory.CreateImage(root, "CubeR", SpriteFactory.IsoCube(), mid, new Vector2(150f, -30f), new Vector2(190f, 190f), Palette.Of(Core.GameColor.Yellow));
            UIFactory.CreateImage(root, "CubeM", SpriteFactory.IsoCube(), mid, new Vector2(0f, 40f), new Vector2(230f, 230f), Palette.Of(Core.GameColor.Purple));

            UIFactory.CreateText(root, "Title", "CUBE BURST", 128, Palette.Outline,
                new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(900f, 160f));
            UIFactory.CreateText(root, "Subtitle", "Tap. Crumble. Sort.", 48, new Color(0.25f, 0.35f, 0.55f),
                new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(900f, 80f));

            var play = UIFactory.CreateButton(root, "Play", "PLAY", new Vector2(0.5f, 0.42f), Vector2.zero,
                new Vector2(560f, 150f), Palette.UIAccent, Color.white, 64,
                () => gm.StartLevel(Mathf.Min(SaveSystem.UnlockedLevel, GameManager.TotalLevels)));
            _playLabel = UIFactory.ButtonLabel(play);

            UIFactory.CreateButton(root, "Levels", "LEVELS", new Vector2(0.5f, 0.31f), Vector2.zero,
                new Vector2(560f, 130f), Palette.UIDark, Color.white, 54,
                ui.ShowLevelSelect);

            var sound = UIFactory.CreateButton(root, "Sound", "SOUND: ON", new Vector2(0.5f, 0.21f), Vector2.zero,
                new Vector2(560f, 110f), new Color(0.55f, 0.62f, 0.75f), Color.white, 44,
                () =>
                {
                    AudioManager.Instance.SoundOn = !AudioManager.Instance.SoundOn;
                    Refresh();
                });
            _soundLabel = UIFactory.ButtonLabel(sound);
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
