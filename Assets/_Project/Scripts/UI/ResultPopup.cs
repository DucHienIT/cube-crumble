using CubeBurst.Core;
using CubeBurst.Gameplay;
using CubeBurst.Systems;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class ResultPopup
    {
        public GameObject Root { get; private set; }

        GameManager _gm;
        RectTransform _panel;
        Text _title;
        Text _reason;
        readonly Image[] _stars = new Image[3];
        Button _next;
        Button _retry;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            _gm = gm;
            var root = UIFactory.CreateScreen(canvas, "ResultPopup", new Color(0f, 0f, 0f, 0.55f));
            Root = root.gameObject;

            var panelImg = UIFactory.CreatePanel(root, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(800f, 900f), Palette.UILight);
            _panel = (RectTransform)panelImg.transform;

            _title = UIFactory.CreateText(_panel, "Title", "LEVEL CLEAR!", 84, Palette.Outline,
                new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(760f, 120f));

            for (int i = 0; i < 3; i++)
                _stars[i] = UIFactory.CreateImage(_panel, "Star" + i, SpriteFactory.Star(),
                    new Vector2(0.5f, 1f), new Vector2((i - 1) * 170f, -280f + (i == 1 ? 30f : 0f)),
                    new Vector2(i == 1 ? 170f : 140f, i == 1 ? 170f : 140f), Palette.Gold);

            _reason = UIFactory.CreateText(_panel, "Reason", "", 48, new Color(0.45f, 0.3f, 0.3f),
                new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(700f, 90f));

            _next = UIFactory.CreateButton(_panel, "Next", "NEXT LEVEL", new Vector2(0.5f, 1f), new Vector2(0f, -470f),
                new Vector2(560f, 130f), Palette.UIAccent, Color.white, 54,
                () =>
                {
                    ui.HideResult();
                    gm.NextLevel();
                });

            _retry = UIFactory.CreateButton(_panel, "Retry", "REPLAY", new Vector2(0.5f, 1f), new Vector2(0f, -630f),
                new Vector2(560f, 130f), Palette.UIDark, Color.white, 54,
                () =>
                {
                    ui.HideResult();
                    gm.RestartLevel();
                });

            UIFactory.CreateButton(_panel, "Menu", "MAIN MENU", new Vector2(0.5f, 1f), new Vector2(0f, -790f),
                new Vector2(560f, 130f), new Color(0.55f, 0.62f, 0.75f), Color.white, 50,
                () =>
                {
                    ui.HideResult();
                    gm.QuitToMenu();
                });
        }

        public void Show(bool win, int stars, GameStatus reason)
        {
            _title.text = win ? "LEVEL CLEAR!" : "LEVEL FAILED";
            _reason.text = win ? "" : reason == GameStatus.LostTime ? "Time's up!" : "The tray overflowed!";

            for (int i = 0; i < 3; i++)
            {
                _stars[i].gameObject.SetActive(win);
                _stars[i].color = i < stars ? Palette.Gold : new Color(0f, 0f, 0f, 0.12f);
            }

            bool hasNext = win && _gm.LevelIndex < GameManager.TotalLevels;
            _next.gameObject.SetActive(hasNext);
            UIFactory.ButtonLabel(_retry).text = win ? "REPLAY" : "RETRY";

            Root.SetActive(true);
            _panel.localScale = Vector3.one * 0.7f;
            _panel.DOKill();
            _panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }
}
