using System;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Dialog parts for the war, map and settings screens (Last Asylum's boost, settings, profile and result dialogs):
    /// an opened <see cref="Sheet"/> that sizes itself to its content and refills in place, LA's ON/OFF switch and
    /// check rows, icon tiles for a settings grid, stat pills, the navy icon bar under a card, and the blue-red
    /// versus banner of a battle result.
    /// </summary>
    public static partial class UiFrames
    {
        /// <summary>An open dialog: the backdrop (destroy it to close), the card, its chrome and the column to fill.</summary>
        public sealed class Sheet
        {
            public RectTransform Root;
            public RectTransform Card;
            public Parts Parts;

            /// <summary>The scrolling column rows go in (add them with <c>Row</c>).</summary>
            public RectTransform Content;

            /// <summary>A fixed strip under the scroll for the dialog's buttons; null unless asked for.</summary>
            public RectTransform Footer;

            /// <summary>A fixed strip over the scroll (the tabs); null unless asked for.</summary>
            public RectTransform Top;

            internal float Height;
            internal float FooterHeight;
            internal float TopHeight;
            internal float MaxShare;

            public bool IsOpen => Root != null;

            public void Close()
            {
                if (Root != null)
                    UnityEngine.Object.Destroy(Root.gameObject);
                Root = null;
            }
        }

        private const float SheetHeader = 104f;

        /// <summary>
        /// Opens a dialog on <paramref name="canvas"/> over the blurred game. <paramref name="height"/> &gt; 0 gives it a
        /// fixed share of the screen (tabbed dialogs keep their size between tabs); 0 sizes it to its content on
        /// <see cref="FitSheet"/>, up to <paramref name="maxShare"/> of the screen, past which it scrolls.
        /// <paramref name="footer"/> reserves a strip (reference px) for buttons that stay put, <paramref name="top"/> one
        /// over the scroll for tabs.
        /// </summary>
        public static Sheet OpenSheet(Transform canvas, string title, Action close, float height = 0f, float footer = 0f, float maxShare = 0.82f,
            float width = 0.94f, float top = 0f)
        {
            var sheet = new Sheet { Height = height, FooterHeight = footer * UiKit.Scale, TopHeight = top * UiKit.Scale, MaxShare = maxShare };
            sheet.Root = Dim(canvas, close);
            var image = UiKit.Box("Card", sheet.Root, Navy);
            var card = image.rectTransform;
            var x0 = (1f - width) / 2f;
            if (height > 0f)
            {
                card.anchorMin = new Vector2(x0, 0.5f - height / 2f);
                card.anchorMax = new Vector2(1f - x0, 0.5f + height / 2f);
                card.offsetMin = card.offsetMax = Vector2.zero;
            }
            else
            {
                card.anchorMin = new Vector2(x0, 0.5f);
                card.anchorMax = new Vector2(1f - x0, 0.5f);
                card.sizeDelta = new Vector2(0f, 900f);
                card.anchoredPosition = Vector2.zero;
            }

            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().From = 0.94f;
            sheet.Card = card;
            sheet.Parts = Dialog(card, close);
            sheet.Parts.Title.text = title ?? string.Empty;

            var scroll = UiKit.ScrollList("Scroll", sheet.Parts.Body, 12f);
            var viewport = (RectTransform)scroll.parent;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(14f, 14f + sheet.FooterHeight);
            viewport.offsetMax = new Vector2(-14f, -14f - sheet.TopHeight);
            sheet.Content = scroll;

            if (top > 0f)
            {
                var strip = UiKit.Rect("Top", sheet.Parts.Body);
                strip.anchorMin = new Vector2(0f, 1f);
                strip.anchorMax = new Vector2(1f, 1f);
                strip.pivot = new Vector2(0.5f, 1f);
                strip.offsetMin = new Vector2(16f, -12f - sheet.TopHeight);
                strip.offsetMax = new Vector2(-16f, -12f);
                sheet.Top = strip;
            }

            if (footer > 0f)
            {
                var strip = UiKit.Rect("Footer", sheet.Parts.Body);
                strip.anchorMin = new Vector2(0f, 0f);
                strip.anchorMax = new Vector2(1f, 0f);
                strip.pivot = new Vector2(0.5f, 0f);
                strip.offsetMin = new Vector2(18f, 12f);
                strip.offsetMax = new Vector2(-18f, 12f + sheet.FooterHeight - 8f);
                sheet.Footer = strip;
            }

            return sheet;
        }

        /// <summary>Empties the sheet for a refill: no new backdrop capture, no pop-in, the scroll stays where it was.</summary>
        public static void ClearSheet(Sheet sheet)
        {
            EmptyRect(sheet.Content);
            if (sheet.Footer != null)
                EmptyRect(sheet.Footer);
            if (sheet.Top != null)
                EmptyRect(sheet.Top);
        }

        /// <summary>Destroys a rect's children, hidden first so this frame's layout already ignores them.</summary>
        public static void EmptyRect(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        /// <summary>Sizes a content-sized sheet to its rows (capped; beyond the cap it scrolls).</summary>
        public static void FitSheet(Sheet sheet)
        {
            if (sheet.Height > 0f || sheet.Card == null)
                return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(sheet.Content);
            var content = LayoutUtility.GetPreferredHeight(sheet.Content);
            var screen = ((RectTransform)sheet.Root.parent).rect.height;
            if (screen <= 1f)
                screen = 1920f;
            var total = content + SheetHeader + 12f + 30f + sheet.FooterHeight + sheet.TopHeight;
            sheet.Card.sizeDelta = new Vector2(0f, Mathf.Min(total, screen * sheet.MaxShare));
        }

        // --- Rows --------------------------------------------------------------------------------

        /// <summary>
        /// An icon from Resources/Icons, or when there is no such icon the text itself drawn large (an emoji such as
        /// "⚙" or "🔔" for the parts the icon set lacks). Returns the graphic so callers can tint it.
        /// </summary>
        public static Graphic Glyph(Transform parent, string icon, float x0, float y0, float x1, float y1, Color tint)
        {
            if (Hud.HudIcons.Get(icon ?? string.Empty) != null)
                return Hud.HudIcons.Image(parent, icon, x0, y0, x1, y1);
            var glyph = UiKit.Label("Glyph", parent, icon ?? string.Empty, UiKit.TypeScale.Display, tint, TextAnchor.MiddleCenter, FontStyle.Bold);
            glyph.rectTransform.Place(x0, y0, x1, y1);
            glyph.Fit(14, 96);
            return glyph;
        }

        /// <summary>A light rounded block inside a dialog (a card of rows, a picture frame, a summary).</summary>
        public static Image SheetBlock(Transform parent, Color? color = null, string name = "Block")
        {
            var box = UiKit.Box(name, parent, color ?? Card);
            box.sprite = UiKit.Rounded;
            box.type = Image.Type.Sliced;
            box.raycastTarget = false;
            return box;
        }

        /// <summary>Plain dark text on the light body (explanations, captions), wrapped.</summary>
        public static TMP_Text SheetText(Transform parent, string text, int size = UiKit.TypeScale.Body, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal)
        {
            var label = UiKit.Label("Text", parent, text ?? string.Empty, size, color ?? InkSoft, anchor, style);
            return label;
        }

        /// <summary>
        /// Last Asylum's settings line: an optional icon, the name, and the ON/OFF switch on the right (blue with the
        /// knob right when on, grey with the knob left when off). The whole row flips it.
        /// </summary>
        public static Button ToggleRow(Transform parent, string icon, string label, Func<bool> get, Action<bool> set)
        {
            var row = UiKit.Rect("Toggle", parent);
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            var button = row.gameObject.AddComponent<Button>();
            var x = 0.03f;
            if (!string.IsNullOrEmpty(icon))
            {
                Glyph(row, icon, 0.02f, 0.14f, 0.11f, 0.86f, Ink);
                x = 0.13f;
            }

            var name = UiKit.Label("Label", row, label, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft);
            name.rectTransform.Place(x, 0f, 0.7f, 1f);
            name.Fit(16, UiKit.TypeScale.Body);

            var track = UiKit.Box("Switch", row, Color.white);
            track.sprite = UiKit.Rounded;
            track.raycastTarget = false;
            track.rectTransform.Place(0.74f, 0.2f, 0.97f, 0.8f);
            var knob = UiKit.Box("Knob", track.transform, Color.white);
            knob.sprite = UiKit.Circle;
            knob.type = Image.Type.Simple;
            knob.preserveAspect = true;
            knob.raycastTarget = false;
            var state = UiKit.Label("State", track.transform, string.Empty, UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            void Render()
            {
                var on = get();
                track.color = on ? new Color(0.22f, 0.5f, 0.88f, 1f) : new Color(0.5f, 0.55f, 0.62f, 1f);
                knob.color = on ? new Color(0.92f, 0.95f, 1f, 1f) : new Color(0.82f, 0.85f, 0.9f, 1f);
                knob.rectTransform.Place(on ? 0.64f : 0.03f, 0.08f, on ? 0.97f : 0.36f, 0.92f);
                state.rectTransform.Place(on ? 0.04f : 0.38f, 0f, on ? 0.62f : 0.96f, 1f);
                state.text = on ? "ON" : "OFF";
            }

            button.onClick.AddListener(() =>
            {
                set(!get());
                Render();
                GameAudio.Play(Sfx.UiToggle);
            });
            Render();
            return button;
        }

        /// <summary>
        /// Mutually exclusive choices as Last Asylum's check boxes in one line (graphics quality, refresh rate): a grey
        /// square that shows a green tick when chosen, the option's name beside it.
        /// </summary>
        public static RectTransform CheckRow(Transform parent, string[] labels, Func<int> get, Action<int> set)
        {
            var row = UiKit.Rect("Checks", parent);
            var ticks = new GameObject[labels.Length];

            void Render()
            {
                var chosen = get();
                for (var i = 0; i < ticks.Length; i++)
                    ticks[i].SetActive(i == chosen);
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var cell = UiKit.Rect("Option" + i, row).Place((float)i / labels.Length, 0f, (float)(i + 1) / labels.Length, 1f);
                cell.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    set(index);
                    Render();
                    GameAudio.Play(Sfx.UiToggle);
                });
                var box = UiKit.Box("Box", cell, new Color(0.6f, 0.67f, 0.77f, 1f));
                box.sprite = UiKit.Rounded;
                box.raycastTarget = false;
                box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                box.rectTransform.pivot = new Vector2(0f, 0.5f);
                box.rectTransform.sizeDelta = new Vector2(46f, 46f);
                box.rectTransform.anchoredPosition = new Vector2(10f, 0f);
                var tick = UiKit.Label("Tick", box.transform, "✓", UiKit.TypeScale.Title, InkGood, TextAnchor.MiddleCenter, FontStyle.Bold);
                tick.rectTransform.Place(-0.2f, -0.1f, 1.3f, 1.3f);
                ticks[i] = tick.gameObject;
                var name = UiKit.Label("Label", cell, labels[i], UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft);
                name.rectTransform.anchorMin = Vector2.zero;
                name.rectTransform.anchorMax = Vector2.one;
                name.rectTransform.offsetMin = new Vector2(66f, 0f);
                name.rectTransform.offsetMax = new Vector2(-4f, 0f);
                name.Fit(14, UiKit.TypeScale.Body);
            }

            Render();
            return row;
        }

        /// <summary>A settings-grid tile: a pale square with the icon on top and its name under it.</summary>
        public static RectTransform IconTile(Transform parent, string icon, string label, Action click, bool dot = false)
        {
            var tile = UiKit.Box("Tile", parent, CardAlt);
            tile.sprite = UiKit.Rounded;
            var shadow = tile.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.16f, 0.28f, 0.3f);
            shadow.effectDistance = new Vector2(0, -3);
            tile.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                click?.Invoke();
            });
            tile.gameObject.AddComponent<PressFeedback>();
            Glyph(tile.transform, icon, 0.24f, 0.4f, 0.76f, 0.92f, Ink);
            var name = UiKit.Label("Label", tile.transform, label, UiKit.TypeScale.Caption + 2, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Place(0.04f, 0.03f, 0.96f, 0.4f);
            name.Fit(13, UiKit.TypeScale.Caption + 2);
            if (dot)
                Dot(tile.transform);
            return tile.rectTransform;
        }

        /// <summary>A stat pill (profile power, level): the icon on the left, the value right-aligned in bold.</summary>
        public static RectTransform StatPill(Transform parent, string icon, string value)
        {
            var pill = UiKit.Box("Pill", parent, CardAlt);
            pill.sprite = UiKit.Rounded;
            pill.raycastTarget = false;
            Glyph(pill.transform, icon, 0.02f, 0.1f, 0.2f, 0.9f, Ink);
            var text = UiKit.Label("Value", pill.transform, value, UiKit.TypeScale.Subtitle, Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            text.rectTransform.Place(0.2f, 0f, 0.95f, 1f);
            text.Fit(16, UiKit.TypeScale.Subtitle);
            return pill.rectTransform;
        }

        /// <summary>
        /// A pale line with an icon, text and an optional action on the right (the profile's name, alliance and kingdom
        /// rows): the action is a small round button with its own icon.
        /// </summary>
        public static RectTransform InfoRow(Transform parent, string icon, string text, string actionIcon = null, Action action = null,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var row = UiKit.Box("Info", parent, CardAlt);
            row.sprite = UiKit.Rounded;
            row.raycastTarget = false;
            var x = 0.03f;
            if (!string.IsNullOrEmpty(icon))
            {
                Glyph(row.transform, icon, 0.015f, 0.1f, 0.11f, 0.9f, Ink);
                x = 0.13f;
            }

            var label = UiKit.Label("Text", row.transform, text, UiKit.TypeScale.Body, Ink, anchor, FontStyle.Bold);
            label.rectTransform.Place(x, 0f, action != null ? 0.86f : 0.97f, 1f);
            label.Fit(16, UiKit.TypeScale.Body);
            if (action != null)
            {
                var hit = UiKit.Box("Action", row.transform, new Color(0, 0, 0, 0.001f));
                hit.rectTransform.Place(0.86f, 0f, 1f, 1f);
                hit.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    action();
                });
                hit.gameObject.AddComponent<PressFeedback>();
                Glyph(hit.transform, actionIcon ?? "›", 0.2f, 0.16f, 0.8f, 0.84f, InkSoft);
            }

            return row.rectTransform;
        }

        /// <summary>
        /// The navy bar of icon buttons under a card (Last Asylum's profile: Record · Account · Settings · Ranking): each
        /// an icon over its white name, greyed when not available.
        /// </summary>
        public static RectTransform IconBar(Transform parent, (string Icon, string Label, Action Click, bool Enabled)[] buttons)
        {
            var bar = UiKit.Box("IconBar", parent, NavyDeep);
            bar.sprite = UiKit.Rounded;
            for (var i = 0; i < buttons.Length; i++)
            {
                var (icon, label, click, enabled) = buttons[i];
                var cell = UiKit.Box("Button" + i, bar.transform, new Color(0, 0, 0, 0.001f));
                cell.rectTransform.Place((float)i / buttons.Length, 0f, (float)(i + 1) / buttons.Length, 1f);
                var button = cell.gameObject.AddComponent<Button>();
                button.interactable = enabled;
                button.onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    click?.Invoke();
                });
                cell.gameObject.AddComponent<PressFeedback>();
                var image = Glyph(cell.transform, icon, 0.28f, 0.38f, 0.72f, 0.94f, Color.white);
                if (!enabled)
                    image.color = new Color(image.color.r, image.color.g, image.color.b, 0.35f);
                var name = UiKit.Label("Label", cell.transform, label, UiKit.TypeScale.Caption + 2, enabled ? Color.white : new Color(0.6f, 0.65f, 0.72f),
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.Place(0.02f, 0.02f, 0.98f, 0.38f);
                name.Fit(13, UiKit.TypeScale.Caption + 2);
                UiText.Outline(name, new Color(0.03f, 0.05f, 0.1f, 0.9f));
            }

            return bar.rectTransform;
        }

        /// <summary>A footer button filling its share of a row (x0..x1 of the parent).</summary>
        public static Button FooterButton(Transform parent, string text, Action click, Color fill, float x0, float x1, bool enabled = true)
        {
            var button = UiKit.Button("Action", parent, text, () => click?.Invoke(), enabled ? fill : UiKit.Panel, UiKit.TypeScale.Body + 4);
            button.interactable = enabled;
            ((RectTransform)button.transform).Place(x0, 0f, x1, 1f, 6f);
            return button;
        }

        /// <summary>
        /// The two sides of a battle face to face (Last Asylum's report head): the attacker's half blue, the
        /// defender's red, "Victory"/"Defeat" over each name, VS (or a centre icon) between them.
        /// </summary>
        public static RectTransform Versus(Transform parent, (string Name, string Result, bool Won, string Line) left,
            (string Name, string Result, bool Won, string Line) right, string centreIcon = null)
        {
            var banner = UiKit.Rect("Versus", parent);
            Half(0f, 0.5f, new Color(0.3f, 0.52f, 0.82f, 1f), left);
            Half(0.5f, 1f, new Color(0.82f, 0.36f, 0.34f, 1f), right);
            if (!string.IsNullOrEmpty(centreIcon))
            {
                var ring = UiKit.Box("Mid", banner, NavyDeep);
                ring.sprite = UiKit.Circle;
                ring.type = Image.Type.Simple;
                ring.preserveAspect = true;
                ring.rectTransform.Place(0.42f, 0.3f, 0.58f, 0.78f);
                Hud.HudIcons.Image(ring.transform, centreIcon, 0.15f, 0.15f, 0.85f, 0.85f);
            }
            else
            {
                var vs = UiKit.Label("Vs", banner, "VS", UiKit.TypeScale.Display + 8, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                vs.rectTransform.Place(0.38f, 0.28f, 0.62f, 0.82f);
                UiText.Outline(vs, new Color(0.35f, 0.18f, 0.02f, 0.95f));
            }

            return banner;

            void Half(float x0, float x1, Color color, (string Name, string Result, bool Won, string Line) side)
            {
                var half = UiKit.Box("Side", banner, color);
                half.sprite = null;
                half.raycastTarget = false;
                half.rectTransform.Place(x0, 0f, x1, 1f);
                var inner = x0 < 0.5f ? (0.04f, 0.8f) : (0.2f, 0.96f);
                if (!string.IsNullOrEmpty(side.Result))
                {
                    var result = UiKit.Label("Result", half.transform, side.Result, UiKit.TypeScale.Title, side.Won ? UiKit.Gold : new Color(0.9f, 0.92f, 0.96f),
                        TextAnchor.MiddleCenter, FontStyle.Bold);
                    result.rectTransform.Place(inner.Item1, 0.6f, inner.Item2, 0.96f);
                    result.Fit(18, UiKit.TypeScale.Title);
                    UiText.Outline(result, new Color(0.08f, 0.06f, 0.1f, 0.9f));
                }

                var name = UiKit.Label("Name", half.transform, side.Name, UiKit.TypeScale.Body, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.Place(0.04f, 0.3f, 0.96f, 0.6f);
                name.richText = false;
                name.Fit(14, UiKit.TypeScale.Body);
                UiText.Outline(name, new Color(0.04f, 0.07f, 0.12f, 0.85f));
                if (!string.IsNullOrEmpty(side.Line))
                {
                    var line = UiKit.Label("Line", half.transform, side.Line, UiKit.TypeScale.Caption, new Color(1f, 0.92f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    line.rectTransform.Place(0.04f, 0.04f, 0.96f, 0.3f);
                    line.Fit(12, UiKit.TypeScale.Caption);
                    UiText.Outline(line, new Color(0.04f, 0.07f, 0.12f, 0.85f));
                }
            }
        }
    }
}
