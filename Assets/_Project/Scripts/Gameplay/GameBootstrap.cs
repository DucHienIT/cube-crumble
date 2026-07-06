using CubeBurst.Systems;
using CubeBurst.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CubeBurst.Gameplay
{
    /// The only component that lives in the scene. Everything else —
    /// camera config, event system, audio, UI, game state — is built here.
    /// Designer-facing tunables live in the two ScriptableObject assets below
    /// (Resources/Config/); if the fields are left empty they are loaded from
    /// Resources, so the scene still works with zero setup.
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

            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            AudioManager.Create();
            var ui = UIController.Create();
            var gm = GameManager.Create(ui);
            ui.Init(gm);
            ui.ShowMainMenu();
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            var cfg = GameConfig.Active;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(cfg.cameraMinOrthoSize, cfg.cameraHalfWidth / cam.aspect);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
