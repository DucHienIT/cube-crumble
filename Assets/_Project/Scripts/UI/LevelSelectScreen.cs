using CubeBurst.Gameplay;
using CubeBurst.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBurst.UI
{
    public class LevelSelectScreen
    {
        const int Columns = 5;

        public GameObject Root { get; private set; }

        GameManager _gm;
        RectTransform _grid;

        public void Build(Transform canvas, GameManager gm, UIController ui)
        {
            _gm = gm;
            var root = UIFactory.CreateScreen(canvas, "LevelSelect", Palette.Background);
            Root = root.gameObject;

            UIFactory.CreateText(root, "Title", "SELECT LEVEL", 84, Palette.Outline,
                new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 120f));
            UIFactory.CreateButton(root, "Back", "BACK", new Vector2(0f, 1f), new Vector2(40f, -40f),
                new Vector2(200f, 100f), Palette.UIDark, Color.white, 44,
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
                bool isUnlocked = level <= unlocked;

                if (isUnlocked)
                {
                    int captured = level;
                    var btn = UIFactory.CreateButton(_grid, $"Level{level}", "", new Vector2(0.5f, 1f), pos,
                        new Vector2(175f, 185f), Color.white, Palette.Outline, 60,
                        () => _gm.StartLevel(captured));
                    UIFactory.CreateText(btn.transform, "Num", level.ToString(), 64, Palette.Outline,
                        new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(170f, 80f));

                    int stars = SaveSystem.GetStars(level);
                    for (int s = 0; s < 3; s++)
                        UIFactory.CreateImage(btn.transform, "Star", SpriteFactory.Star(),
                            new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 44f, -48f), new Vector2(42f, 42f),
                            s < stars ? Palette.Gold : new Color(0f, 0f, 0f, 0.12f));
                }
                else
                {
                    var panel = UIFactory.CreatePanel(_grid, $"Locked{level}", new Vector2(0.5f, 1f), pos,
                        new Vector2(175f, 185f), new Color(0.72f, 0.77f, 0.85f));
                    UIFactory.CreateText(panel.transform, "Num", level.ToString(), 64, new Color(1f, 1f, 1f, 0.6f),
                        new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 80f));
                }
            }
        }
    }
}
