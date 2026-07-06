using CubeBurst.Core;
using UnityEngine;

namespace CubeBurst.Systems
{
    /// Static facade over PaletteConfig so call sites stay short
    /// (`Palette.Background`). Edit colors in Resources/Config/PaletteConfig.asset,
    /// not here — this class holds no color values of its own.
    public static class Palette
    {
        static PaletteConfig Cfg => PaletteConfig.Active;

        public static Color Background => Cfg.background;
        public static Color Outline => Cfg.outline;
        public static Color UIDark => Cfg.uiDark;
        public static Color UIAccent => Cfg.uiAccent;
        public static Color UILight => Cfg.uiLight;
        public static Color Gold => Cfg.gold;

        // candy-UI colors (menus, buttons, popups)
        public static Color SkyBg => Cfg.skyBg;
        public static Color BtnGreen => Cfg.btnGreen;
        public static Color BtnBlue => Cfg.btnBlue;
        public static Color BtnOrange => Cfg.btnOrange;
        public static Color BtnRed => Cfg.btnRed;
        public static Color BtnSlate => Cfg.btnSlate;
        public static Color CardWhite => Cfg.cardWhite;
        public static Color ShadowInk => Cfg.shadowInk;

        // reference-art styling colors
        public static Color ShapeOutline => Cfg.shapeOutline;
        public static Color PanelBg => Cfg.panelBg;
        public static Color PillarBg => Cfg.pillarBg;
        public static Color TrayRim => Cfg.trayRim;
        public static Color TrayRimDanger => Cfg.trayRimDanger;
        public static Color CounterGray => Cfg.counterGray;

        public static Color Of(GameColor c) => Cfg.Of(c);
    }
}
