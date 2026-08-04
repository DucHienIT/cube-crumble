using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Applies the config assets and camera fit before anything else runs.
    /// All game objects (EventSystem, AudioManager, UICanvas, GameManager)
    /// are authored in the scene; per-level content comes from prefabs
    /// referenced by GameManager — this component creates nothing.
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameConfig gameConfig;
        [SerializeField] PaletteConfig palette;

        void Awake()
        {
            GameConfig.SetActive(gameConfig);
            PaletteConfig.SetActive(palette);

            Application.targetFrameRate = GameConfig.Active.targetFrameRate;
            SetupCamera();
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var cfg = GameConfig.Active;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(cfg.cameraMinOrthoSize, cfg.cameraHalfWidth / cam.aspect);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
        }
    }
}
