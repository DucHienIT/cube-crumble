using CubeBurst.Gameplay;
using CubeBurst.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class HUDScreen
    {
        public GameObject Root { get; private set; }

        GameManager _gm;
        Text _timer;
        Text _progress;
        Text _levelNumber;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            _gm = gm;
            var root = UIFactory.CreateScreen(canvas, "HUD", Color.clear);
            Root = root.gameObject;

            // pause: light frosted square with soft slate bars (reference style)
            var pause = UIFactory.CreateCandyButton(root, "Pause", "", new Vector2(0f, 1f), new Vector2(40f, -40f),
                new Vector2(112f, 112f), Palette.CardWhite, 48,
                ui.ShowPause);
            UIFactory.CreateImage(pause.transform, "Icon", SpriteFactory.PauseIcon(),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(60f, 60f),
                new Color(0.42f, 0.49f, 0.65f, 1f));

            // dark pill with a stopwatch icon and white time (reference style)
            var timerRt = UIFactory.CreateRect(root, "TimerPill");
            UIFactory.Place(timerRt, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(340f, 104f));
            UIFactory.CreateSoftShadow(timerRt, new Vector2(370f, 132f), new Vector2(0f, -10f));
            var timerBody = UIFactory.CreateImage(timerRt, "Body", SpriteFactory.UIGloss(),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 104f),
                new Color(0.16f, 0.19f, 0.28f, 0.98f));
            timerBody.type = Image.Type.Sliced;
            UIFactory.CreateImage(timerBody.transform, "TimerIcon", SpriteFactory.Stopwatch(),
                new Vector2(0.5f, 0.5f), new Vector2(-110f, -4f), new Vector2(86f, 86f), Color.white);
            _timer = UIFactory.CreateText(timerBody.transform, "Timer", "0:00", 58, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(28f, 2f), new Vector2(240f, 100f));

            // level label (top-right): small "Lvl" caption over a big number,
            // dark navy text with a white outline, no background pill (ref style)
            var levelGroup = UIFactory.CreateRect(root, "LevelLabel");
            UIFactory.Place(levelGroup, new Vector2(1f, 1f), new Vector2(-56f, -30f), new Vector2(170f, 150f));
            var caption = UIFactory.CreateText(levelGroup, "Caption", "Lvl", 40, Palette.UIDark,
                new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(170f, 48f));
            UIFactory.AddOutline(caption, Color.white, 2f);
            _levelNumber = UIFactory.CreateText(levelGroup, "LevelNumber", "1", 84, Palette.UIDark,
                new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(170f, 100f));
            UIFactory.AddOutline(_levelNumber, Color.white, 3f);

            // big gray tray-danger counter over the bottom panel's top edge
            _progress = UIFactory.CreateText(root, "Progress", "0/0", 88, Palette.CounterGray,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(500f, 110f));
            var shadow = _progress.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 1f, 1f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        public void Bind()
        {
            _levelNumber.text = $"{_gm.LevelIndex}";
            Refresh();
        }

        public void Refresh()
        {
            var s = _gm.Session;
            if (s == null) return;
            int t = Mathf.CeilToInt(s.TimeRemaining);
            _timer.text = $"{t / 60:00}:{t % 60:00}";
            _timer.color = t <= 10 ? new Color(1f, 0.4f, 0.35f) : Color.white;

            // tray danger meter: balls piled in the shared tray vs its capacity;
            // going over the capacity is an instant loss
            int tray = s.Shared.Count;
            int cap = s.Shared.Capacity;
            _progress.text = $"{tray}/{cap}";
            float frac = cap > 0 ? tray / (float)cap : 0f;
            _progress.color = frac >= 0.85f ? new Color(0.9f, 0.25f, 0.22f)
                : frac >= 0.6f ? new Color(0.95f, 0.55f, 0.2f)
                : Palette.CounterGray;
        }
    }
}
