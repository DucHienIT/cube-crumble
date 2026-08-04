using CubeBurst.Gameplay;
using CubeBurst.Systems;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class PausePopup
    {
        public GameObject Root { get; private set; }

        RectTransform _panel;
        Text _soundLabel;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            var root = UIFactory.CreateScreen(canvas, "PausePopup", new Color(0.05f, 0.08f, 0.18f, 0.6f));
            Root = root.gameObject;

            var panelRt = UIFactory.CreateRect(root, "Panel");
            UIFactory.Place(panelRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 900f));
            _panel = panelRt;

            UIFactory.CreateSoftShadow(panelRt, new Vector2(810f, 950f), new Vector2(0f, -16f));
            var body = UIFactory.CreateImage(panelRt, "Body", SpriteFactory.UIRounded(),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 900f), Palette.CardWhite);
            body.type = Image.Type.Sliced;
            body.raycastTarget = true;

            // ribbon header overlapping the panel's top edge
            var ribbon = UIFactory.CreateImage(panelRt, "Ribbon", SpriteFactory.UIGloss(),
                new Vector2(0.5f, 1f), new Vector2(0f, 60f), new Vector2(460f, 130f), Palette.BtnBlue);
            ribbon.type = Image.Type.Sliced;
            var title = UIFactory.CreateText(ribbon.transform, "Title", "PAUSED", 66, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(440f, 120f));
            UIFactory.AddOutline(title, Color.Lerp(Palette.BtnBlue, Color.black, 0.5f));

            UIFactory.CreateCandyButton(panelRt, "Resume", "RESUME", new Vector2(0.5f, 1f), new Vector2(0f, -190f),
                new Vector2(560f, 140f), Palette.BtnGreen, 56,
                ui.HidePause);

            UIFactory.CreateCandyButton(panelRt, "Restart", "RESTART", new Vector2(0.5f, 1f), new Vector2(0f, -370f),
                new Vector2(560f, 140f), Palette.BtnOrange, 56,
                () =>
                {
                    ui.HidePause();
                    gm.RestartLevel();
                });

            var sound = UIFactory.CreateCandyButton(panelRt, "Sound", "SOUND: ON", new Vector2(0.5f, 1f), new Vector2(0f, -550f),
                new Vector2(560f, 140f), Palette.BtnSlate, 50,
                () =>
                {
                    AudioManager.Instance.SoundOn = !AudioManager.Instance.SoundOn;
                    Refresh();
                });
            _soundLabel = UIFactory.ButtonLabel(sound);

            UIFactory.CreateCandyButton(panelRt, "Menu", "MAIN MENU", new Vector2(0.5f, 1f), new Vector2(0f, -730f),
                new Vector2(560f, 140f), Palette.BtnRed, 50,
                () =>
                {
                    ui.HidePause();
                    gm.QuitToMenu();
                });
        }

        public void Show()
        {
            Refresh();
            Root.SetActive(true);
            _panel.localScale = Vector3.one * 0.75f;
            _panel.DOKill();
            _panel.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void Refresh()
        {
            if (AudioManager.Instance != null)
                _soundLabel.text = AudioManager.Instance.SoundOn ? "SOUND: ON" : "SOUND: OFF";
        }
    }
}
