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

            UIFactory.CreateButton(root, "Pause", "II", new Vector2(0f, 1f), new Vector2(40f, -40f),
                new Vector2(110f, 110f), Palette.UIDark, Color.white, 48,
                ui.ShowPause);

            // dark pill with a stopwatch icon and white time (reference style)
            UIFactory.CreatePanel(root, "TimerPill", new Vector2(0.5f, 1f), new Vector2(0f, -46f),
                new Vector2(330f, 100f), new Color(0.13f, 0.15f, 0.22f, 0.96f));
            UIFactory.CreateImage(root, "TimerIcon", SpriteFactory.Stopwatch(),
                new Vector2(0.5f, 1f), new Vector2(-110f, -66f), new Vector2(84f, 84f), Color.white);
            _timer = UIFactory.CreateText(root, "Timer", "0:00", 58, Color.white,
                new Vector2(0.5f, 1f), new Vector2(24f, -46f), new Vector2(240f, 100f));

            // "Lvl" over the big number, white with a soft shadow (top-right)
            var lvlLabel = UIFactory.CreateText(root, "LevelLabel", "Lvl", 46, Color.white,
                new Vector2(1f, 1f), new Vector2(-56f, -30f), new Vector2(200f, 56f), TextAnchor.MiddleRight);
            AddShadow(lvlLabel);
            _levelNumber = UIFactory.CreateText(root, "LevelNumber", "1", 80, Color.white,
                new Vector2(1f, 1f), new Vector2(-56f, -92f), new Vector2(200f, 84f), TextAnchor.MiddleRight);
            AddShadow(_levelNumber);

            // big gray delivery counter over the bottom panel's top edge
            _progress = UIFactory.CreateText(root, "Progress", "0/0", 88, Palette.CounterGray,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(500f, 110f));
        }

        static void AddShadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.16f, 0.35f, 0.4f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        public void Bind()
        {
            _levelNumber.text = _gm.LevelIndex.ToString();
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
