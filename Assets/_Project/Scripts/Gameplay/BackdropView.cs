using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Decorative gameplay backdrop matching the reference art: faint vertical
    /// stripes behind the cube shape, a big rounded bottom panel, and two
    /// side pillars framing the tray + containers.
    public static class BackdropView
    {
        public static void Create(Transform parent)
        {
            var root = new GameObject("Backdrop").transform;
            root.SetParent(parent, false);

            // striped upper background
            var stripes = NewSprite(root, "Stripes", SpriteFactory.Stripes(), 0);
            stripes.transform.localPosition = new Vector3(0f, 1.5f, 2f);
            stripes.drawMode = SpriteDrawMode.Tiled;
            stripes.size = new Vector2(12f, 15f);

            // bottom panel with rounded top corners (bottom extends off-screen)
            var panel = NewSprite(root, "Panel", SpriteFactory.BigRounded(), 2);
            panel.transform.localPosition = new Vector3(0f, -4.75f, 2f);
            panel.drawMode = SpriteDrawMode.Sliced;
            panel.size = new Vector2(8.4f, 9f);
            panel.color = Palette.PanelBg;

            // side pillars
            for (int i = 0; i < 2; i++)
            {
                var pillar = NewSprite(root, i == 0 ? "PillarL" : "PillarR", SpriteFactory.BigRounded(), 3);
                pillar.transform.localPosition = new Vector3(i == 0 ? -3.65f : 3.65f, -4.6f, 1.9f);
                pillar.drawMode = SpriteDrawMode.Sliced;
                pillar.size = new Vector2(0.9f, 8f);
                pillar.color = Palette.PillarBg;
            }
        }

        static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
