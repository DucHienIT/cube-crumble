using CubeBurst.Core;
using UnityEngine;

namespace CubeBurst.Systems
{
    public static class Palette
    {
        public static readonly Color Background = Hex("#D8E2EF");
        public static readonly Color Outline = Hex("#1E3A8A");
        public static readonly Color UIDark = Hex("#1B2A55");
        public static readonly Color UIAccent = Hex("#3D6BE5");
        public static readonly Color UILight = Hex("#F4F8FC");
        public static readonly Color Gold = Hex("#F9C233");

        // reference-art styling colors
        public static readonly Color ShapeOutline = Hex("#3F63E0");   // blue silhouette line around the polycube
        public static readonly Color PanelBg = Hex("#C6D2F0");        // bottom panel
        public static readonly Color PillarBg = Hex("#DFE7FB");       // side pillars
        public static readonly Color TrayRim = Hex("#F2DFE3");        // basin rim (soft pink-white)
        public static readonly Color TrayRimDanger = Hex("#F07A70");
        public static readonly Color CounterGray = Hex("#B4BFD6");

        /// Display hues follow the reference art; enum names are historical
        /// (level JSON stores indices, so the enum can't change).
        static readonly Color[] ColorMap =
        {
            Hex("#F6392B"), // Red    -> red
            Hex("#D91A6B"), // Purple -> magenta/crimson
            Hex("#45464F"), // Orange -> dark gray (the "black" cubes)
            Hex("#FFD62B"), // Yellow -> yellow
            Hex("#A8DC26"), // Green  -> lime
            Hex("#2FDDE5"), // Blue   -> cyan
        };

        public static Color Of(GameColor c) => ColorMap[(int)c];

        static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
