using CubeBurst.Core;
using CubeBurst.Gameplay;
using CubeBurst.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class LevelSelectScreen
    {
        const int Columns = 5;

        static readonly GameColor[] RowColors =
            { GameColor.Red, GameColor.Yellow, GameColor.Green, GameColor.Blue, GameColor.Purple };

        public GameObject Root { get; private set; }

        GameManager _gm;
        RectTransform _grid;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            _gm = gm;
            var root = UIFactory.CreateScreen(canvas, "LevelSelect", Palette.Background);
            Root = root.gameObject;

            UIFactory.CreateGradientBG(root, Palette.SkyBg);

            var title = UIFactory.CreateText(root, "Title", "SELECT LEVEL", 84, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 120f));
            UIFactory.AddOutline(title, new Color(0.11f, 0.17f, 0.34f), 4f);

            UIFactory.CreateCandyButton(root, "Back", "BACK", new Vector2(0f, 1f), new Vector2(40f, -40f),
                new Vector2(210f, 105f), Palette.BtnSlate, 44,
                ui.ShowMainMenu);

            _grid = UIFactory.CreateRect(root, "Grid");
            UIFactory.Place(_grid, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1000f, 1400f));
        }

        /// Star/lock state changes between visits, so the grid is rebuilt on show.
        public void Rebuild()
        {
            for (int i = _grid.childCount - 1; i >= 0; i--)
                Object.Destroy(_grid.GetChild(i).gameObject);

            int unlocked = SaveSystem.UnlockedLevel;
            for (int level = 1; level <= GameManager.TotalLevels; level++)
            {
                int row = (level - 1) / Columns;
                int col = (level - 1) % Columns;
                var pos = new Vector2((col - (Columns - 1) * 0.5f) * 195f, -60f - row * 210f);
                var accent = Palette.Of(RowColors[row % RowColors.Length]);

                if (level <= unlocked)
                    BuildUnlockedCard(level, pos, accent);
                else
                    BuildLockedCard(level, pos);
            }
        }

        void BuildUnlockedCard(int level, Vector2 pos, Color accent)
        {
            var size = new Vector2(178f, 190f);
            var rt = UIFactory.CreateRect(_grid, $"Level{level}");
            UIFactory.Place(rt, new Vector2(0.5f, 1f), pos, size);

            UIFactory.CreateSoftShadow(rt, size + new Vector2(30f, 26f), new Vector2(0f, -10f));
            var body = UIFactory.CreateImage(rt, "Body", SpriteFactory.UIRounded(),
                new Vector2(0.5f, 0.5f), Vector2.zero, size, Palette.CardWhite);
            body.type = Image.Type.Sliced;
            body.raycastTarget = true;

            // colored accent bar tucked under the card's top edge
            var bar = UIFactory.CreateImage(body.transform, "Accent", SpriteFactory.UIRounded(),
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(size.x - 26f, 34f), accent);
            bar.type = Image.Type.Sliced;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = body;
            rt.gameObject.AddComponent<UIPressEffect>();
            int captured = level;
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                _gm.StartLevel(captured);
            });

            UIFactory.CreateText(body.transform, "Num", level.ToString(), 66, Palette.UIDark,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(170f, 80f));

            int stars = SaveSystem.GetStars(level);
            for (int s = 0; s < 3; s++)
                UIFactory.CreateImage(body.transform, "Star", SpriteFactory.Star(),
                    new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 46f, -56f), new Vector2(44f, 44f),
                    s < stars ? Palette.Gold : new Color(0f, 0f, 0f, 0.10f));
        }

        void BuildLockedCard(int level, Vector2 pos)
        {
            var size = new Vector2(178f, 190f);
            var rt = UIFactory.CreateRect(_grid, $"Locked{level}");
            UIFactory.Place(rt, new Vector2(0.5f, 1f), pos, size);

            UIFactory.CreateSoftShadow(rt, size + new Vector2(30f, 26f), new Vector2(0f, -10f));
            var body = UIFactory.CreateImage(rt, "Body", SpriteFactory.UIRounded(),
                new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(0.70f, 0.75f, 0.85f));
            body.type = Image.Type.Sliced;

            UIFactory.CreateImage(body.transform, "Lock", SpriteFactory.Padlock(),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(78f, 78f),
                new Color(1f, 1f, 1f, 0.85f));
            UIFactory.CreateText(body.transform, "Num", level.ToString(), 44, new Color(1f, 1f, 1f, 0.7f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(170f, 60f));
        }
    }
}
