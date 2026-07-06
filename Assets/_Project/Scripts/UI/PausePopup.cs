using CubeBurst.Gameplay;
using CubeBurst.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class PausePopup
    {
        public GameObject Root { get; private set; }

        Text _soundLabel;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            var root = UIFactory.CreateScreen(canvas, "PausePopup", new Color(0f, 0f, 0f, 0.55f));
            Root = root.gameObject;

            var panel = UIFactory.CreatePanel(root, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(760f, 940f), Palette.UILight);

            UIFactory.CreateText(panel.transform, "Title", "PAUSED", 90, Palette.Outline,
                new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(700f, 120f));

            UIFactory.CreateButton(panel.transform, "Resume", "RESUME", new Vector2(0.5f, 1f), new Vector2(0f, -260f),
                new Vector2(560f, 130f), Palette.UIAccent, Color.white, 54,
                ui.HidePause);

            UIFactory.CreateButton(panel.transform, "Restart", "RESTART", new Vector2(0.5f, 1f), new Vector2(0f, -430f),
                new Vector2(560f, 130f), Palette.UIDark, Color.white, 54,
                () =>
                {
                    ui.HidePause();
                    gm.RestartLevel();
                });

            var sound = UIFactory.CreateButton(panel.transform, "Sound", "SOUND: ON", new Vector2(0.5f, 1f), new Vector2(0f, -600f),
                new Vector2(560f, 130f), new Color(0.55f, 0.62f, 0.75f), Color.white, 50,
                () =>
                {
                    AudioManager.Instance.SoundOn = !AudioManager.Instance.SoundOn;
                    Refresh();
                });
            _soundLabel = UIFactory.ButtonLabel(sound);

            UIFactory.CreateButton(panel.transform, "Menu", "MAIN MENU", new Vector2(0.5f, 1f), new Vector2(0f, -770f),
                new Vector2(560f, 130f), new Color(0.8f, 0.35f, 0.3f), Color.white, 50,
                () =>
                {
                    ui.HidePause();
                    gm.QuitToMenu();
                });
        }

        public void Refresh()
        {
            if (AudioManager.Instance != null)
                _soundLabel.text = AudioManager.Instance.SoundOn ? "SOUND: ON" : "SOUND: OFF";
        }
    }
}
