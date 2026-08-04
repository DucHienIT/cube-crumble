using CubeBurst.Systems;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// Decorative gameplay backdrop matching the reference art: faint vertical
    /// stripes behind the cube shape, a big rounded bottom panel, and two
    /// side pillars framing the tray + containers. Lives on Backdrop.prefab;
    /// the sprites are procedural, so it builds its children on Start.
    public class BackdropView : MonoBehaviour
    {
        void Start()
        {
            var root = transform;

            // striped upper background
            var stripes = NewSprite(root, "Stripes", SpriteFactory.Stripes(), 0);
            stripes.transform.localPosition = new Vector3(0f, 1.5f, 2f);
            stripes.drawMode = SpriteDrawMode.Tiled;
            stripes.size = new Vector2(12f, 15f);

            // soft white glow behind the shape — the airy vignette of the ref art
            var glow = NewSprite(root, "Glow", SpriteFactory.RadialGlow(), 1);
            glow.transform.localPosition = new Vector3(0f, 3.2f, 1.95f);
            glow.transform.localScale = new Vector3(5f, 4.3f, 1f);
            glow.color = new Color(1f, 1f, 1f, 0.85f);

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
