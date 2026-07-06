using UnityEngine;

namespace CubeBurst.Systems
{
    /// All gameplay/feel tunables in one designer-editable asset.
    /// The instance lives at Resources/Config/GameConfig.asset and is also
    /// wired to GameBootstrap in the scene; edit it in the Inspector, no code
    /// changes needed. Code reads it through GameConfig.Active.
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Cube Burst/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Application")]
        public int targetFrameRate = 60;

        [Header("Camera")]
        [Tooltip("Ortho size = max(minOrthoSize, halfWidth / aspect) — keeps the play field fully visible on narrow screens.")]
        public float cameraMinOrthoSize = 6f;
        public float cameraHalfWidth = 3.9f;

        [Header("Levels & stars")]
        public int totalLevels = 30;
        [Range(0f, 1f), Tooltip("Remaining-time fraction needed for 3 stars.")]
        public float threeStarTimeFraction = 0.5f;
        [Range(0f, 1f), Tooltip("Remaining-time fraction needed for 2 stars.")]
        public float twoStarTimeFraction = 0.25f;

        [Header("Input")]
        [Tooltip("Press that moves less than this (px) counts as a tap; more = rotate drag.")]
        public float dragThresholdPx = 16f;
        [Tooltip("Degrees of shape yaw per full screen width of horizontal drag.")]
        public float dragDegreesPerScreenWidth = 270f;

        [Header("Ball flight")]
        public float ballFlightDuration = 0.55f;
        [Tooltip("Delay between consecutive balls of one crumbled cube.")]
        public float ballSpawnStagger = 0.035f;
        [Tooltip("Random spawn offset (x, y) around the crumbled cube.")]
        public Vector2 ballSpawnJitter = new Vector2(0.2f, 0.15f);
        [Tooltip("Delay before the first tray->container transfer ball departs.")]
        public float transferBaseDelay = 0.12f;
        [Tooltip("Extra delay per additional transfer ball in the same frame.")]
        public float transferStagger = 0.08f;

        [Header("Flow")]
        [Tooltip("Seconds between level end and the result popup.")]
        public float resultPopupDelay = 0.9f;

        [Header("Audio")]
        [Range(0f, 1f)] public float musicVolume = 0.3f;

        // ---- static access ----

        static GameConfig _active;

        /// Never null: serialized asset if assigned/loadable, else code defaults.
        public static GameConfig Active
        {
            get
            {
                if (_active == null)
                    _active = Resources.Load<GameConfig>("Config/GameConfig");
                if (_active == null)
                    _active = CreateInstance<GameConfig>();
                return _active;
            }
        }

        /// GameBootstrap passes its serialized reference here (null is fine —
        /// Active then falls back to Resources, then to defaults).
        public static void SetActive(GameConfig config) => _active = config;
    }
}
