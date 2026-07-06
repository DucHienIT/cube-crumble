using CubeBurst.Core;
using UnityEngine;

namespace CubeBurst.Systems
{
    /// Every color the game renders, in one designer-editable asset.
    /// The instance lives at Resources/Config/PaletteConfig.asset and is also
    /// wired to GameBootstrap in the scene. Code reads it through the static
    /// Palette facade, so call sites stay `Palette.Background` etc.
    [CreateAssetMenu(fileName = "PaletteConfig", menuName = "Cube Burst/Palette Config")]
    public class PaletteConfig : ScriptableObject
    {
        [Header("Core")]
        public Color background = Hex("#D8E2EF");
        public Color outline = Hex("#1E3A8A");
        public Color uiDark = Hex("#1B2A55");
        public Color uiAccent = Hex("#3D6BE5");
        public Color uiLight = Hex("#F4F8FC");
        public Color gold = Hex("#F9C233");

        [Header("Candy UI (menus, buttons, popups)")]
        public Color skyBg = Hex("#A9CFFF");
        public Color btnGreen = Hex("#57C43D");
        public Color btnBlue = Hex("#3D8BFF");
        public Color btnOrange = Hex("#FFA132");
        public Color btnRed = Hex("#F4574A");
        public Color btnSlate = Hex("#8FA2C7");
        public Color cardWhite = Hex("#FDFEFF");
        public Color shadowInk = new Color(0.07f, 0.11f, 0.28f, 0.35f);

        [Header("Reference-art styling")]
        public Color shapeOutline = Hex("#3F63E0");
        public Color panelBg = Hex("#C6D2F0");
        public Color pillarBg = Hex("#DFE7FB");
        public Color trayRim = Hex("#F2DFE3");
        public Color trayRimDanger = Hex("#F07A70");
        public Color counterGray = Hex("#B4BFD6");

        [Header("Ball/cube colors — indexed by GameColor")]
        [Tooltip("Display hues follow the reference art; enum names are historical (level JSON stores indices, so the enum can't change). Order: Red, Purple, Orange, Yellow, Green, Blue.")]
        public Color[] gameColors =
        {
            Hex("#F6392B"), // Red    -> red
            Hex("#D91A6B"), // Purple -> magenta/crimson
            Hex("#45464F"), // Orange -> dark gray (the "black" cubes)
            Hex("#FFD62B"), // Yellow -> yellow
            Hex("#A8DC26"), // Green  -> lime
            Hex("#2FDDE5"), // Blue   -> cyan
        };

        public Color Of(GameColor c)
        {
            int i = (int)c;
            return gameColors != null && i >= 0 && i < gameColors.Length
                ? gameColors[i]
                : Color.magenta;
        }

        static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        // ---- static access ----

        static PaletteConfig _active;

        /// Never null: serialized asset if assigned/loadable, else code defaults.
        public static PaletteConfig Active
        {
            get
            {
                if (_active == null)
                    _active = Resources.Load<PaletteConfig>("Config/PaletteConfig");
                if (_active == null)
                    _active = CreateInstance<PaletteConfig>();
                return _active;
            }
        }

        public static void SetActive(PaletteConfig config) => _active = config;
    }
}
