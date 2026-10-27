using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Builds HADAL-styled UGUI at runtime (dark abyss panels, cyan accents) so screens are plain
    /// code instead of hand-wired scene objects. Layout is anchor based on a 1080x1920 reference.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Abyss = new(0.015f, 0.055f, 0.08f, 0.96f);
        public static readonly Color Panel = new(0.02f, 0.07f, 0.10f, 0.93f);
        public static readonly Color Field = new(0.05f, 0.13f, 0.18f, 0.94f);

        /// <summary>
        /// Everything is drawn a little smaller than the first screens were laid out for: text sizes and
        /// list row heights are multiplied by this, so screens read as a modern mobile game, not a form.
        /// </summary>
        public const float Scale = 0.8f;

        /// <summary>Corner radius of boxes, cards and buttons (reference pixels).</summary>
        public const int Radius = 22;

        private static Sprite _rounded;
        private static Sprite _ring;

        /// <summary>A white rounded rectangle for 9-slicing.</summary>
        public static Sprite Rounded => _rounded != null ? _rounded : _rounded = MakeRounded(false);

        /// <summary>The same shape as an outline (thin frame).</summary>
        public static Sprite Ring => _ring != null ? _ring : _ring = MakeRounded(true);

        private static Sprite _gloss;
        private static Sprite _soft;

        /// <summary>
        /// Rounded and shaded for buttons (Last Asylum's look): darker at the bottom, a bright band at the top. Tinting
        /// multiplies, so every button colour gets the same gloss.
        /// </summary>
        public static Sprite Gloss => _gloss != null ? _gloss : _gloss = MakeShaded("HadalGloss", 0.62f, 0.9f, 0.16f);

        /// <summary>Rounded with a gentle top-to-bottom shade for panels and cards.</summary>
        public static Sprite Soft => _soft != null ? _soft : _soft = MakeShaded("HadalSoft", 0.84f, 1f, 0.04f);

        private static Sprite _circle;
        private static Sprite _circleRing;

        /// <summary>A filled disc (round action buttons, badges).</summary>
        public static Sprite Circle => _circle != null ? _circle : _circle = MakeCircle(false);

        /// <summary>A soft ring (selection marker under a tapped target).</summary>
        public static Sprite CircleRing => _circleRing != null ? _circleRing : _circleRing = MakeCircle(true);

        private static Sprite MakeCircle(bool ring)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = ring ? "HadalCircleRing" : "HadalCircle"
            };
            var pixels = new Color32[size * size];
            const float r = size / 2f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    var inside = Mathf.Clamp01(r - 1f - d);
                    // The ring is thick and fades inwards, like a shadowed selection disc.
                    var alpha = ring ? inside * Mathf.Clamp01((d - r * 0.62f) / (r * 0.3f)) : inside;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A rounded rectangle whose brightness runs from <paramref name="bottom"/> to <paramref name="top"/>, plus a highlight band near the top.</summary>
        private static Sprite MakeShaded(string name, float bottom, float top, float highlight)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = name
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var t = (y + 0.5f) / size;
                var shade = Mathf.Lerp(bottom, top, t);
                // The glossy band: brightest just under the top edge, fading out by the middle.
                shade += highlight * Mathf.Clamp01((t - 0.55f) / 0.3f) * Mathf.Clamp01((0.97f - t) / 0.06f);
                var value = (byte)(Mathf.Clamp01(shade) * 255);
                for (var x = 0; x < size; x++)
                {
                    var cx = Mathf.Clamp(x + 0.5f, Radius, size - Radius);
                    var cy = Mathf.Clamp(y + 0.5f, Radius, size - Radius);
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - Radius;
                    pixels[y * size + x] = new Color32(value, value, value, (byte)(Mathf.Clamp01(0.5f - d) * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(Radius, Radius, Radius, Radius));
        }

        private static Sprite MakeRounded(bool ring)
        {
            const int size = 64;
            const float thickness = 2.5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = ring ? "HadalRing" : "HadalRounded"
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Distance from the rounded-rectangle edge (negative inside).
                    var cx = Mathf.Clamp(x + 0.5f, Radius, size - Radius);
                    var cy = Mathf.Clamp(y + 0.5f, Radius, size - Radius);
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - Radius;
                    var inside = Mathf.Clamp01(0.5f - d);
                    var alpha = ring ? inside * Mathf.Clamp01(d + thickness + 0.5f) : inside;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(Radius, Radius, Radius, Radius));
        }
        public static readonly Color Accent = new(0.22f, 0.84f, 0.95f, 1f);

        /// <summary>Tabs: the chosen one pale, the rest deep blue.</summary>
        public static readonly Color TabOn = new(0.88f, 0.93f, 0.97f, 1f);
        public static readonly Color TabOff = new(0.1f, 0.2f, 0.3f, 1f);

        /// <summary>Primary action (Last Asylum's blue) and the gold of purchases and claims.</summary>
        public static readonly Color Primary = new(0.24f, 0.52f, 0.9f, 1f);
        public static readonly Color Gold = new(0.98f, 0.74f, 0.25f, 1f);
        public static readonly Color Amber = new(1f, 0.64f, 0.22f, 1f);
        public static readonly Color Text = new(0.86f, 0.94f, 0.97f, 1f);
        public static readonly Color Muted = new(0.55f, 0.68f, 0.74f, 1f);
        public static readonly Color Danger = new(1f, 0.45f, 0.42f, 1f);

        /// <summary>Met / go (requirements met, the main build button).</summary>
        public static readonly Color Ok = new(0.36f, 0.86f, 0.46f, 1f);

        private static Font _builtin;
        private static Font _world;
        private static Font _roundFont;

        /// <summary>The font for the current language (switching language restarts the scene).</summary>
        public static Font Font => FontFor(Loc.Current);

        /// <summary>
        /// The built-in font draws Latin, Cyrillic and CJK through the phone's fonts but not Arabic
        /// or Thai on Android, so those two use a bundled Noto merge (Latin + Arabic + Thai).
        /// </summary>
        public static Font FontFor(string language)
        {
            if (language is "ar" or "th")
            {
                _world = _world != null ? _world : Resources.Load<Font>("Fonts/HadalSansWorld-Regular");
                if (_world != null)
                    return _world;
            }

            // Latin-script languages get the game's rounded face (art/ui/make_font.py); Cyrillic, CJK and Vietnamese
            // are outside it and stay on the built-in font.
            if (language is not ("ru" or "ja" or "ko" or "zh-Hans" or "zh-Hant" or "vi"))
            {
                _roundFont = _roundFont != null ? _roundFont : Resources.Load<Font>("Fonts/Fredoka-SemiBold");
                if (_roundFont != null)
                    return _roundFont;
            }

            return _builtin = _builtin != null ? _builtin : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Canvas Canvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors in parent-relative 0..1 coordinates (min/max corners), optional pixel inset.</summary>
        public static RectTransform Place(this RectTransform rect, float xMin, float yMin, float xMax, float yMax, float inset = 0f)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>
        /// A rounded box. Panels and cards (the Panel and Field colours) are the skin's light panel and inset: their text
        /// turns to dark ink by itself (<see cref="InkSurface"/>). A box named "Header" is the navy, brass-trimmed bar.
        /// </summary>
        public static Image Box(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = color.a > 0.05f ? Soft : Rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = color;

            // Section bars wear the header too; cost chips are insets.
            var bar = name is "Header" or "Section";
            var skin = color == Panel ? Skin.Get("panel") : color == Field || name == "Cost" ? Skin.Get("inset") : bar ? Skin.Get("header") : null;
            if (skin != null)
            {
                image.sprite = skin;
                image.color = Color.white;
                if (!bar)
                    rect.gameObject.AddComponent<InkSurface>();
            }

            return image;
        }

        /// <summary>
        /// The navy title bar across the top of a panel (from <paramref name="yMin"/> up), drawn behind what is already
        /// there; returns it so the title can sit on it in white.
        /// </summary>
        public static Image TitleBar(Transform panel, float yMin)
        {
            var bar = Box("Header", panel, new Color(0.05f, 0.16f, 0.22f, 1f));
            bar.rectTransform.Place(0, yMin, 1, 1);
            bar.raycastTarget = false;
            bar.transform.SetAsFirstSibling();
            return bar;
        }

        /// <summary>White bold text with the skin's dark navy stroke (titles on bars, buttons).</summary>
        public static Text Stroked(Text label)
        {
            label.color = Color.white;
            var stroke = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
            stroke.effectColor = new Color(0.06f, 0.1f, 0.17f, 0.9f);
            stroke.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        /// <summary>The skin sprite a button of this colour wears (art/ui/make_skin.py).</summary>
        private static string ButtonSkin(Color c)
        {
            if (c == Panel)
                return "btn_grey"; // disabled
            if (c == Field)
                return "btn_navy";
            if (c == TabOn)
                return "tab_on";
            if (c == TabOff)
                return "tab_off";
            Color.RGBToHSV(c, out var h, out var s, out var v);
            if (s < 0.25f || v < 0.3f)
                return v > 0.7f ? "btn_light" : v < 0.45f ? "btn_navy" : "btn_grey";
            if (h < 0.04f || h > 0.9f)
                return "btn_red";
            if (h < 0.15f)
                return "btn_gold";
            if (h < 0.45f)
                return "btn_green";
            if (h < 0.53f)
                return "btn_teal";
            return "btn_blue";
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal)
        {
            var rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = Mathf.RoundToInt(size * Scale);
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(string name, Transform parent, string text, UnityAction onClick, Color? fill = null, int fontSize = 38)
        {
            var face = fill ?? Accent;
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = face;
            image.sprite = Gloss;
            // Skinned: the sprite carries the colour, gloss and lip; a lone "X" is the round red close button.
            var close = text == "X";
            var skin = Skin.Get(close ? "round_red" : ButtonSkin(face));
            if (skin != null)
            {
                image.sprite = skin;
                image.color = Color.white;
                if (close)
                {
                    image.type = UnityEngine.UI.Image.Type.Simple;
                    image.preserveAspect = true;
                }

                face = skin.name is "btn_light" or "tab_on" ? TabOn : face == Panel || face == Field || face == TabOff ? new Color(0.2f, 0.28f, 0.4f) : face;
            }

            var button = image.gameObject.AddComponent<Button>();
            image.gameObject.AddComponent<PressFeedback>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            // Light fills take dark text; dark and coloured fills take white text with an outline (Last Asylum's buttons).
            var light = skin != null ? face == TabOn : 0.299f * face.r + 0.587f * face.g + 0.114f * face.b > 0.62f;
            var label = Label("Label", image.transform, text, fontSize, light ? new Color(0.06f, 0.1f, 0.14f) : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (!light)
                Stroked(label);
            label.rectTransform.Place(0, 0, 1, 1, 8);
            // The skin's lip is part of the sprite: keep the text on the face above it.
            if (skin != null && !close)
                label.rectTransform.offsetMin = new Vector2(8, 14);
            // Long translations shrink to fit instead of spilling out of the button.
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Min(16, label.fontSize);
            label.resizeTextMaxSize = label.fontSize;
            if (skin == null)
            {
                // The lip: a darker copy of the face just below it reads as a pressable slab.
                var lip = image.gameObject.AddComponent<Shadow>();
                lip.effectColor = new Color(face.r * 0.4f, face.g * 0.4f, face.b * 0.4f, Mathf.Max(0.6f, face.a));
                lip.effectDistance = new Vector2(0, -6);
            }

            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.3f);
            shadow.effectDistance = new Vector2(0, -6);
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }

        public static InputField Input(string name, Transform parent, string placeholder, bool password = false, int characterLimit = 64)
        {
            var image = Box(name, parent, Field);
            var field = image.gameObject.AddComponent<InputField>();

            var text = Label("Text", image.transform, string.Empty, 40, Text);
            text.rectTransform.Place(0, 0, 1, 1, 20);
            text.supportRichText = false;
            text.raycastTarget = true;

            var hint = Label("Placeholder", image.transform, placeholder, 36, Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
            hint.rectTransform.Place(0, 0, 1, 1, 20);

            field.textComponent = text;
            field.placeholder = hint;
            field.characterLimit = characterLimit;
            field.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        /// <summary>Stacks children top-down with a fixed row height in reference pixels.</summary>
        public static RectTransform Column(string name, Transform parent, float spacing = 24f)
        {
            var rect = Rect(name, parent);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static T Row<T>(this T component, float height) where T : Component
        {
            var element = component.gameObject.GetComponent<LayoutElement>() ?? component.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height * Scale;
            element.preferredHeight = height * Scale;
            return component;
        }

        /// <summary>Vertical scroll list; returns the content transform to add rows (with <see cref="Row{T}"/>) to.</summary>
        public static RectTransform ScrollList(string name, Transform parent, float spacing = 16f)
        {
            var viewport = Box(name, parent, new Color(0, 0, 0, 0.001f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var content = Column("Content", viewport.transform, spacing);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            return content;
        }

        /// <summary>A settings row: label on the left, an on/off pill on the right that flips on tap.</summary>
        public static Button Switch(string name, Transform parent, string label, Func<bool> get, Action<bool> set)
        {
            var row = Box(name, parent, Field);
            var button = row.gameObject.AddComponent<Button>();
            var text = Label("Label", row.transform, label, 38, Text);
            text.rectTransform.Place(0.04f, 0, 0.7f, 1);

            var pill = Box("Pill", row.transform, Accent);
            pill.rectTransform.Place(0.74f, 0.2f, 0.96f, 0.8f);
            pill.raycastTarget = false;
            var state = Label("State", pill.transform, string.Empty, 32, Abyss, TextAnchor.MiddleCenter, FontStyle.Bold);
            state.rectTransform.Place(0, 0, 1, 1);

            void Render()
            {
                var on = get();
                pill.color = on ? Accent : new Color(0.12f, 0.24f, 0.3f, 1f);
                state.color = on ? Abyss : Muted;
                state.text = on ? Loc.T("settings.on") : Loc.T("settings.off");
            }

            button.onClick.AddListener(() =>
            {
                set(!get());
                Render();
            });
            Render();
            return button;
        }

        /// <summary>A row of mutually exclusive choices; the selected one is filled.</summary>
        public static RectTransform Choice(string name, Transform parent, string[] labels, Func<int> get, Action<int> set)
        {
            var row = Rect(name, parent);
            var buttons = new Button[labels.Length];

            void Render()
            {
                var selected = get();
                for (var i = 0; i < buttons.Length; i++)
                {
                    var tab = Skin.Get(i == selected ? "tab_on" : "tab_off");
                    if (tab != null)
                        buttons[i].image.sprite = tab;
                    else
                        buttons[i].image.color = i == selected ? TabOn : TabOff;
                    var caption = buttons[i].GetComponentInChildren<UnityEngine.UI.Text>();
                    caption.color = i == selected ? new Color(0.08f, 0.13f, 0.2f) : Text;
                    var stroke = caption.GetComponent<Outline>();
                    if (stroke != null)
                        stroke.enabled = i != selected;
                }
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                buttons[i] = Button("Option" + i, row, labels[i], () =>
                {
                    set(index);
                    Render();
                }, TabOff, 34);
                ((RectTransform)buttons[i].transform).Place((float)i / labels.Length, 0, (float)(i + 1) / labels.Length, 1, 4);
            }

            Render();
            return row;
        }

        /// <summary>Horizontal 0..1 slider in the HADAL style.</summary>
        public static UnityEngine.UI.Slider Slider(string name, Transform parent, float value, Action<float> changed)
        {
            var root = Rect(name, parent);
            var track = Box("Track", root, Field);
            track.rectTransform.Place(0, 0.4f, 1, 0.6f);

            var fillArea = Rect("FillArea", root);
            fillArea.Place(0, 0.4f, 1, 0.6f);
            var fill = Box("Fill", fillArea, Accent);
            fill.rectTransform.Place(0, 0, 0, 1);

            var handleArea = Rect("HandleArea", root);
            handleArea.Place(0, 0, 1, 1);
            handleArea.offsetMin = new Vector2(24, 0);
            handleArea.offsetMax = new Vector2(-24, 0);
            var handle = Box("Handle", handleArea, Text);
            handle.rectTransform.sizeDelta = new Vector2(48, 0);
            handle.rectTransform.anchorMin = new Vector2(0, 0.1f);
            handle.rectTransform.anchorMax = new Vector2(0, 0.9f);

            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => changed(v));
            return slider;
        }

        /// <summary>Fades a CanvasGroup on unscaled time; deactivates the object when it reaches 0.</summary>
        public static System.Collections.IEnumerator Fade(CanvasGroup group, float to, float seconds, Action done = null)
        {
            var from = group.alpha;
            for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }

            group.alpha = to;
            group.blocksRaycasts = to > 0.01f;
            if (to <= 0.01f)
                group.gameObject.SetActive(false);
            done?.Invoke();
        }
    }
}
