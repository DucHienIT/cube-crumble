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
        Image _ribbon;
        Text _title;
        Text _reason;
        readonly Image[] _stars = new Image[3];
        Button _next;
        Button _retry;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            _gm = gm;
            var root = UIFactory.CreateScreen(canvas, "ResultPopup", new Color(0.05f, 0.08f, 0.18f, 0.6f));
            Root = root.gameObject;

            var panelRt = UIFactory.CreateRect(root, "Panel");
            UIFactory.Place(panelRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 960f));
            _panel = panelRt;

            UIFactory.CreateSoftShadow(panelRt, new Vector2(850f, 1010f), new Vector2(0f, -16f));
            var body = UIFactory.CreateImage(panelRt, "Body", SpriteFactory.UIRounded(),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 960f), Palette.CardWhite);
            body.type = Image.Type.Sliced;
            body.raycastTarget = true;

            _ribbon = UIFactory.CreateImage(panelRt, "Ribbon", SpriteFactory.UIGloss(),
                new Vector2(0.5f, 1f), new Vector2(0f, 60f), new Vector2(560f, 130f), Palette.BtnGreen);
            _ribbon.type = Image.Type.Sliced;
            _title = UIFactory.CreateText(_ribbon.transform, "Title", "LEVEL CLEAR!", 60, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(540f, 120f));
            UIFactory.AddOutline(_title, new Color(0.10f, 0.14f, 0.22f));

            for (int i = 0; i < 3; i++)
                _stars[i] = UIFactory.CreateImage(panelRt, "Star" + i, SpriteFactory.Star(),
                    new Vector2(0.5f, 1f), new Vector2((i - 1) * 180f, -190f + (i == 1 ? 30f : 0f)),
                    new Vector2(i == 1 ? 185f : 150f, i == 1 ? 185f : 150f), Palette.Gold);

            _reason = UIFactory.CreateText(panelRt, "Reason", "", 48, new Color(0.45f, 0.3f, 0.3f),
                new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(700f, 90f));

            _next = UIFactory.CreateCandyButton(panelRt, "Next", "NEXT LEVEL", new Vector2(0.5f, 1f), new Vector2(0f, -430f),
                new Vector2(560f, 140f), Palette.BtnGreen, 54,
                () =>
                {
                    ui.HideResult();
                    gm.NextLevel();
                });

            _retry = UIFactory.CreateCandyButton(panelRt, "Retry", "REPLAY", new Vector2(0.5f, 1f), new Vector2(0f, -600f),
                new Vector2(560f, 140f), Palette.BtnOrange, 54,
                () =>
                {
                    ui.HideResult();
                    gm.RestartLevel();
                });

            UIFactory.CreateCandyButton(panelRt, "Menu", "MAIN MENU", new Vector2(0.5f, 1f), new Vector2(0f, -770f),
                new Vector2(560f, 140f), Palette.BtnSlate, 50,
                () =>
                {
                    ui.HideResult();
                    gm.QuitToMenu();
                });
        }

        public void Show(bool win, int stars, GameStatus reason)
        {
            _title.text = win ? "LEVEL CLEAR!" : "LEVEL FAILED";
            _ribbon.color = win ? Palette.BtnGreen : Palette.BtnRed;
            _reason.text = win ? "" : reason == GameStatus.LostTime ? "Time's up!" : "The tray overflowed!";

            bool hasNext = win && _gm.LevelIndex < GameManager.TotalLevels;
            _next.gameObject.SetActive(hasNext);
            UIFactory.ButtonLabel(_retry).text = win ? "REPLAY" : "RETRY";

            Root.SetActive(true);
            _panel.localScale = Vector3.one * 0.7f;
            _panel.DOKill();
            _panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);

            for (int i = 0; i < 3; i++)
            {
                var star = _stars[i];
                star.gameObject.SetActive(win);
                if (!win) continue;
                star.color = i < stars ? Palette.Gold : new Color(0f, 0f, 0f, 0.10f);
                star.transform.DOKill();
                star.transform.localScale = Vector3.zero;
                star.transform.DOScale(1f, 0.4f)
                    .SetEase(Ease.OutBack, 2.2f)
                    .SetDelay(0.25f + i * 0.15f)
                    .SetUpdate(true);
            }
        }
    }
}
