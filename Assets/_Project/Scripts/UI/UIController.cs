using CubeBurst.Core;
using CubeBurst.Gameplay;
using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.UI
{
    /// Owns the canvas and toggles the plain-class screens.
    public class UIController : MonoBehaviour
    {
        GameManager _gm;
        Transform _canvas;

        MainMenuScreen _menu;
        LevelSelectScreen _levels;
        HUDScreen _hud;
        PausePopup _pause;
        ResultPopup _result;

        public HUDScreen Hud => _hud;

        public static UIController Create()
        {
            var canvas = UIFactory.CreateCanvas("UICanvas");
            var ui = canvas.gameObject.AddComponent<UIController>();
            ui._canvas = canvas.transform;
            return ui;
        }

        public void Init(GameManager gm)
        {
            _gm = gm;
            _menu = new MainMenuScreen();
            _menu.Build(_canvas, gm, this);
            _levels = new LevelSelectScreen();
            _levels.Build(_canvas, gm, this);
            _hud = new HUDScreen();
            _hud.Build(_canvas, gm, this);
            _pause = new PausePopup();
            _pause.Build(_canvas, gm, this);
            _result = new ResultPopup();
            _result.Build(_canvas, gm, this);
            HideAll();
        }

        void HideAll()
        {
            _menu.Root.SetActive(false);
            _levels.Root.SetActive(false);
            _hud.Root.SetActive(false);
            _pause.Root.SetActive(false);
            _result.Root.SetActive(false);
        }

        public void ShowMainMenu()
        {
            HideAll();
            _menu.Refresh();
            _menu.Root.SetActive(true);
        }

        public void ShowLevelSelect()
        {
            HideAll();
            _levels.Rebuild();
            _levels.Root.SetActive(true);
        }

        public void ShowHUD()
        {
            HideAll();
            _hud.Bind();
            _hud.Root.SetActive(true);
        }

        public void ShowPause()
        {
            if (!_gm.IsPlaying) return;
            _gm.SetPaused(true);
            _pause.Show();
        }

        public void HidePause()
        {
            _pause.Root.SetActive(false);
            _gm.SetPaused(false);
        }

        public void ShowResult(bool win, int stars, GameStatus reason)
        {
            _pause.Root.SetActive(false);
            _result.Show(win, stars, reason);
        }

        public void HideResult() => _result.Root.SetActive(false);
    }
}
