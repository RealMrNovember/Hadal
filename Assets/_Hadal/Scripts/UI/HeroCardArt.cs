using System.Collections.Generic;
using HADAL.Shared.Catalog;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Last Asylum style hero art: the tall painted full-body cut-outs in Resources/HeroArt (art/gpu/cards.py,
    /// hero_art.json) over a rarity-tinted backdrop drawn here, and the idle motion of the hero card.
    /// </summary>
    public static class HeroCardArt
    {
        private static readonly Dictionary<string, Texture2D> Art = new();
        private static readonly Dictionary<string, Texture2D> Backdrops = new();

        /// <summary>The hero's full-body cut-out (3:5, transparent), or null when it has none.</summary>
        public static Texture2D FullBody(string heroId)
        {
            if (Art.TryGetValue(heroId, out var texture))
                return texture;
            texture = Resources.Load<Texture2D>("HeroArt/" + heroId);
            if (texture != null)
                texture.wrapMode = TextureWrapMode.Clamp;
            Art[heroId] = texture;
            return texture;
        }

        /// <summary>Gold for legendary, purple for epic, blue for rare, steel for common.</summary>
        public static Color Tint(string rarity) => rarity switch
        {
            "legendary" => new Color(1f, 0.68f, 0.2f),
            "epic" => new Color(0.68f, 0.38f, 0.95f),
            "rare" => new Color(0.25f, 0.6f, 1f),
            _ => new Color(0.5f, 0.62f, 0.68f)
        };

        /// <summary>Gacha grade letters, as on Last Asylum's cards.</summary>
        public static string Letter(string rarity) => rarity switch
        {
            "legendary" => "UR",
            "epic" => "SSR",
            "rare" => "SR",
            _ => "R"
        };

        /// <summary>A vertical rarity gradient with a soft glow behind the figure and faint light shafts from above.</summary>
        public static Texture2D Backdrop(string rarity)
        {
            if (Backdrops.TryGetValue(rarity, out var texture))
                return texture;

            const int w = 96, h = 160;
            var tint = Tint(rarity);
            var deep = new Color(0.02f, 0.05f, 0.09f);
            texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            for (var y = 0; y < h; y++)
            {
                var v = y / (h - 1f);
                for (var x = 0; x < w; x++)
                {
                    var u = x / (w - 1f);
                    var c = Color.Lerp(deep, tint * 0.75f, Mathf.SmoothStep(0f, 1f, v * 0.9f + 0.1f));
                    // Glow behind the chest of the figure.
                    var dx = (u - 0.5f) * 1.4f;
                    var dy = v - 0.58f;
                    c += tint * 0.45f * Mathf.Exp(-(dx * dx + dy * dy) * 9f);
                    // Light shafts slanting down from the surface.
                    var shaft = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((u * 3.2f + v * 0.9f) * Mathf.PI)), 8f) * v * v;
                    c += new Color(0.6f, 0.9f, 1f) * 0.12f * shaft;
                    // Dark vignette at the bottom for the level band.
                    c *= Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 0.35f, v));
                    c.a = 1f;
                    pixels[y * w + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Backdrops[rarity] = texture;
            return texture;
        }

        /// <summary>
        /// Backdrop plus figure filling <paramref name="parent"/>. <paramref name="uv"/> picks the part of the 3:5
        /// cut-out shown (the list shows the upper body, the card the whole figure). Returns the figure, or null when
        /// the hero has no art yet (then the old square bust is used, if there is one).
        /// </summary>
        public static RawImage Stage(Transform parent, HeroDefinition hero, bool owned, Rect uv)
        {
            var back = UiKit.Rect("Backdrop", parent).gameObject.AddComponent<RawImage>();
            back.texture = Backdrop(hero.Rarity.id);
            back.color = owned ? Color.white : new Color(0.45f, 0.48f, 0.52f, 1f);
            back.raycastTarget = false;
            ((RectTransform)back.transform).Place(0, 0, 1, 1);

            var art = FullBody(hero.Id);
            var bust = art == null ? Hadal.Core.Catalog.CharacterLibrary.Instance?.Portrait(hero.Portrait) : null;
            if (art == null && bust == null)
                return null;

            var figure = UiKit.Rect("Figure", parent).gameObject.AddComponent<RawImage>();
            figure.texture = art != null ? art : bust;
            figure.uvRect = art != null ? uv : new Rect(0, 0, 1, 1);
            // Not found yet: a dark silhouette, as in Last Asylum's locked heroes.
            figure.color = owned ? Color.white : new Color(0.08f, 0.1f, 0.13f, 0.95f);
            figure.raycastTarget = false;
            var rect = (RectTransform)figure.transform;
            rect.Place(0, 0, 1, 1);
            rect.pivot = new Vector2(0.5f, 0f);
            return figure;
        }
    }

    /// <summary>
    /// The hero card's idle life (Last Asylum animates its heroes with Spine): the figure breathes and sways from its
    /// feet, bubbles drift up through the backdrop and a light band sweeps across every few seconds.
    /// </summary>
    public sealed class HeroIdleMotion : MonoBehaviour
    {
        private RectTransform _figure;
        private RectTransform _sweep;
        private Image _sweepImage;
        private readonly List<(RectTransform Rect, Image Image, float Speed, float Phase, float X, float Size)> _bubbles = new();
        private float _seed;

        public static HeroIdleMotion Attach(RectTransform area, RawImage figure, Color tint, int bubbles = 14)
        {
            var motion = area.gameObject.AddComponent<HeroIdleMotion>();
            motion._figure = figure != null ? (RectTransform)figure.transform : null;
            motion._seed = Random.value * 10f;
            if (area.GetComponent<RectMask2D>() == null)
                area.gameObject.AddComponent<RectMask2D>();

            for (var i = 0; i < bubbles; i++)
            {
                var bubble = UiKit.Box("Bubble", area, new Color(0.75f, 0.95f, 1f, 0.3f));
                bubble.sprite = UiKit.CircleRing;
                bubble.type = Image.Type.Simple;
                bubble.raycastTarget = false;
                var size = Random.Range(10f, 30f); // reference pixels
                motion._bubbles.Add((bubble.rectTransform, bubble, Random.Range(0.035f, 0.08f), Random.value, Random.value, size));
                // Bubbles go behind the figure.
                bubble.transform.SetSiblingIndex(1);
            }

            var sweep = UiKit.Box("Sweep", area, new Color(1f, 1f, 1f, 0f));
            sweep.raycastTarget = false;
            sweep.rectTransform.localRotation = Quaternion.Euler(0, 0, -18f);
            motion._sweep = sweep.rectTransform;
            motion._sweepImage = sweep;
            motion._sweepImage.color = new Color(tint.r, tint.g, tint.b, 0f);
            return motion;
        }

        private void Update()
        {
            var t = Time.time + _seed;
            if (_figure != null)
            {
                // Breathing: a slow lift of the chest (more height than width), and a small sway around the feet.
                var breath = Mathf.Sin(t * 1.6f);
                _figure.localScale = new Vector3(1f + 0.006f * breath, 1f + 0.014f * breath, 1f);
                _figure.localRotation = Quaternion.Euler(0, 0, 0.7f * Mathf.Sin(t * 0.55f));
                _figure.anchoredPosition = new Vector2(4f * Mathf.Sin(t * 0.37f), 0f);
            }

            foreach (var (rect, image, speed, phase, x, size) in _bubbles)
            {
                var p = Mathf.Repeat(phase + t * speed, 1f);
                var bx = x + 0.03f * Mathf.Sin(t * 1.3f + phase * 20f);
                rect.anchorMin = rect.anchorMax = new Vector2(bx, p);
                rect.sizeDelta = Vector2.one * size;
                var c = image.color;
                c.a = 0.35f * Mathf.Sin(p * Mathf.PI);
                image.color = c;
            }

            // The light band: every ~6 s it crosses the card in a second and a half.
            var phaseSweep = Mathf.Repeat(t / 6f, 1f);
            var s = Mathf.Clamp01(phaseSweep * 4f);
            var sx = Mathf.Lerp(-0.5f, 1.3f, s);
            _sweep.Place(sx, -0.3f, sx + 0.18f, 1.3f);
            var a = phaseSweep < 0.25f ? 0.18f * Mathf.Sin(s * Mathf.PI) : 0f;
            _sweepImage.color = new Color(1f, 1f, 1f, a);
        }
    }
}
