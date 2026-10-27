using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The drawn UI skin in Resources/UiSkin (art/ui/make_skin.py): light panels, the navy brass-trimmed header,
    /// glossy buttons, tabs, rarity slots, bars and pills, as 9-sliced sprites. A missing sprite yields null and the
    /// caller falls back to the procedural shapes.
    /// </summary>
    public static class Skin
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        // Slice borders (left, bottom, right, top) in sprite pixels; mirrors skin.json.
        private static Vector4 Border(string name) => name switch
        {
            "panel" => new Vector4(36, 36, 36, 36),
            "header" => new Vector4(40, 24, 40, 24),
            "inset" => new Vector4(24, 24, 24, 24),
            "tab_on" or "tab_off" => new Vector4(24, 20, 24, 20),
            "pill" => new Vector4(28, 26, 28, 26),
            _ when name.StartsWith("btn_") => new Vector4(30, 32, 30, 26),
            _ when name.StartsWith("slot_") => new Vector4(28, 28, 28, 28),
            _ when name.StartsWith("bar_") => new Vector4(16, 14, 16, 14),
            _ => Vector4.zero
        };

        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out var sprite))
                return sprite;

            var texture = Resources.Load<Texture2D>("UiSkin/" + name);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, Border(name));
                sprite.name = name;
            }

            Cache[name] = sprite;
            return sprite;
        }

        /// <summary>A rarity-framed slot for an item or hero ("common", "rare", "epic", "legendary").</summary>
        public static Image Slot(Transform parent, string rarity)
        {
            var image = UiKit.Rect("Slot", parent).gameObject.AddComponent<Image>();
            image.sprite = Get("slot_" + rarity) ?? UiKit.Soft;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A progress bar: the dark track and a fill (green, gold or blue) cut to <paramref name="value"/>.</summary>
        public static RectTransform Bar(Transform parent, float value, string fill = "green")
        {
            var track = UiKit.Rect("Bar", parent).gameObject.AddComponent<Image>();
            track.sprite = Get("bar_track") ?? UiKit.Rounded;
            track.type = Image.Type.Sliced;
            track.color = Get("bar_track") != null ? Color.white : new Color(0.02f, 0.05f, 0.08f, 1f);
            track.raycastTarget = false;

            var bar = track.rectTransform;
            var image = UiKit.Rect("Fill", bar).gameObject.AddComponent<Image>();
            image.sprite = Get("bar_" + fill) ?? UiKit.Rounded;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            image.rectTransform.Place(0, 0, Mathf.Clamp01(value), 1, 2);
            image.enabled = value > 0.005f;
            return bar;
        }
    }

    /// <summary>
    /// Sits on a light panel or inset: text drawn straight on it (not on a button, tile or dark card inside it) that
    /// was written in the old light-on-dark colours is turned to dark ink of the same hue, rich-text colours included.
    /// Runs a few times a second, so text a screen recolours later is caught too.
    /// </summary>
    public sealed class InkSurface : MonoBehaviour
    {
        public static readonly Color Ink = new(0.1f, 0.15f, 0.24f, 1f);
        public static readonly Color InkMuted = new(0.34f, 0.41f, 0.51f, 1f);

        private static readonly Regex Tag = new("<color=#([0-9A-Fa-f]{6})([0-9A-Fa-f]{2})?>", RegexOptions.Compiled);
        private static readonly List<Text> Texts = new();

        private readonly Dictionary<Text, string> _done = new();
        private Image _image;
        private float _next;

        private void Awake() => _image = GetComponent<Image>();

        private void OnEnable() => _next = 0f;

        private bool IsLight => _image == null || 0.299f * _image.color.r + 0.587f * _image.color.g + 0.114f * _image.color.b > 0.6f;

        private void LateUpdate()
        {
            if (Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + 0.15f;

            // The outermost light surface does the work for the ones nested in it.
            var parent = transform.parent;
            if (parent != null && parent.GetComponentInParent<InkSurface>() is { IsLight: true })
                return;

            if (_done.Count > 400)
                _done.Clear();
            GetComponentsInChildren(true, Texts);
            foreach (var text in Texts)
            {
                if (text.GetComponent<Shadow>() != null || !OnLight(text.transform))
                    continue;
                var c = text.color;
                if (Luma(c) > 0.5f)
                    text.color = ToInk(c);
                if (text.supportRichText && text.text.IndexOf("<color=#", System.StringComparison.Ordinal) >= 0
                    && (!_done.TryGetValue(text, out var was) || was != text.text))
                {
                    text.text = Tag.Replace(text.text, m =>
                    {
                        if (!ColorUtility.TryParseHtmlString("#" + m.Groups[1].Value, out var tag) || Luma(tag) <= 0.5f)
                            return m.Value;
                        return $"<color=#{ColorUtility.ToHtmlStringRGB(ToInk(tag))}{m.Groups[2].Value}>";
                    });
                    _done[text] = text.text;
                }
            }

            Texts.Clear();
        }

        /// <summary>Whether the first surface under the text is a light skin surface (not a button, tile or dark card).</summary>
        private static bool OnLight(Transform t)
        {
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (p.TryGetComponent<InkSurface>(out var surface))
                    return surface.IsLight;
                if (p.TryGetComponent<Button>(out _))
                    return false;
                if (p.TryGetComponent<Image>(out var image) && image.type == Image.Type.Sliced && image.color.a > 0.3f && image.sprite != null)
                    return false;
            }

            return false;
        }

        private static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        private static Color ToInk(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            if (s < 0.3f)
            {
                var grey = v > 0.75f ? Ink : InkMuted;
                return new Color(grey.r, grey.g, grey.b, c.a);
            }

            // Yellows and oranges read better a little brighter; everything else as a deep shade of its hue.
            var ink = Color.HSVToRGB(h, Mathf.Max(s, 0.75f), h > 0.06f && h < 0.17f ? 0.68f : 0.52f);
            ink.a = c.a;
            return ink;
        }
    }
}
