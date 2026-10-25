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
        public static readonly Color Panel = new(0.03f, 0.10f, 0.14f, 0.97f);
        public static readonly Color Field = new(0.06f, 0.16f, 0.21f, 1f);
        public static readonly Color Accent = new(0.22f, 0.84f, 0.95f, 1f);
        public static readonly Color Amber = new(1f, 0.64f, 0.22f, 1f);
        public static readonly Color Text = new(0.86f, 0.94f, 0.97f, 1f);
        public static readonly Color Muted = new(0.55f, 0.68f, 0.74f, 1f);
        public static readonly Color Danger = new(1f, 0.45f, 0.42f, 1f);

        private static Font _font;

        public static Font Font => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

        public static Image Box(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal)
        {
            var rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
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
            var image = Box(name, parent, fill ?? Accent);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var label = Label("Label", image.transform, text, fontSize, new Color(0.02f, 0.07f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(0, 0, 1, 1, 8);
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
            element.minHeight = height;
            element.preferredHeight = height;
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
