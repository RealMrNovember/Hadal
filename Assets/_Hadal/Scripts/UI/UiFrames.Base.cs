using System;
using System.Collections.Generic;
using HADAL.Shared.Enums;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The base screens' parts (building details, build list, research, training, speed-ups), after Last Asylum: a
    /// dialog that sizes itself to its content with a pinned footer for the call to action, light action cards, job
    /// cards with a timer bar, grid tiles, cost chips in dark ink, requirement rows and a horizontal strip of tiles.
    /// </summary>
    public static partial class UiFrames
    {
        /// <summary>What <see cref="SizedDialog"/> hands back.</summary>
        public sealed class Sheet
        {
            public RectTransform Root;
            public RectTransform Card;
            public Parts Parts;

            /// <summary>The scroll content: rows go here (with <c>Row</c>).</summary>
            public RectTransform List;

            /// <summary>The pinned call-to-action column under the list (null when not asked for); hidden while empty.</summary>
            public RectTransform Footer;
        }

        /// <summary>
        /// The genre's centred dialog over the blurred game, as tall as its content up to <paramref name="max"/> of the
        /// screen (then the list scrolls), with an optional footer pinned to the bottom for the main button.
        /// </summary>
        public static Sheet SizedDialog(Transform canvas, Action close, float max = 0.84f, bool footer = false)
        {
            var root = Dim(canvas, close);
            var card = UiKit.Rect("Card", root);
            card.anchorMin = new Vector2(0.03f, 0.5f);
            card.anchorMax = new Vector2(0.97f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(0, 600);
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().From = 0.94f;
            var parts = Dialog(card, close);

            var list = UiKit.ScrollList("List", parts.Body, 14f);
            list.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(18, 18, 16, 18);
            var viewport = ((RectTransform)list.parent).Place(0, 0, 1, 1);

            RectTransform foot = null;
            if (footer)
            {
                foot = UiKit.Column("Footer", parts.Body, 8f);
                foot.anchorMin = Vector2.zero;
                foot.anchorMax = new Vector2(1, 0);
                foot.pivot = new Vector2(0.5f, 0);
                foot.sizeDelta = Vector2.zero;
                foot.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(22, 22, 12, 16);
                foot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var band = foot.gameObject.AddComponent<Image>();
                band.sprite = UiKit.Rounded;
                band.type = Image.Type.Sliced;
                band.color = CardAlt;
                band.raycastTarget = false;
            }

            var fit = card.gameObject.AddComponent<DialogFit>();
            fit.Content = list;
            fit.Viewport = viewport;
            fit.Footer = foot;
            fit.Subtitle = parts.Subtitle;
            fit.Max = max;
            return new Sheet { Root = root, Card = card, Parts = parts, List = list, Footer = foot };
        }

        /// <summary>Removes every child at once (hidden now, destroyed at the end of the frame) for a refill.</summary>
        public static void Clear(Transform parent)
        {
            if (parent == null)
                return;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        private static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        /// <summary>
        /// A light action card: the title, a soft line (the blocking reason in red after it), the price as dark chips
        /// with the time, and the button on the right (boost, overdrive, salvage, expand…).
        /// </summary>
        public static RectTransform ActionCard(Transform parent, string title, string body, string reason, string action, bool enabled, Action click,
            Color? fill = null, IReadOnlyList<KeyValuePair<ResourceType, long>> costs = null, IReadOnlyDictionary<ResourceType, long> wallet = null,
            float seconds = -1f, string costText = null)
        {
            var hasBody = !string.IsNullOrEmpty(body) || !string.IsNullOrEmpty(reason);
            var hasCost = costs != null || !string.IsNullOrEmpty(costText);
            var height = 108f + (hasBody ? 92f : 0f) + (hasCost ? 64f : 0f);
            var card = UiKit.Box("Card", parent, Card);
            card.sprite = UiKit.Rounded;
            card.raycastTarget = false;
            card.Row(height);
            var s = card.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0.1f, 0.16f, 0.28f, 0.25f);
            s.effectDistance = new Vector2(0, -3);

            var top = 1f - 70f / height;
            UiKit.Label("Title", card.transform, title, UiKit.TypeScale.Body + 2, Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                .Fit(18, UiKit.TypeScale.Body + 2).rectTransform.Place(0.04f, top, 0.68f, 0.97f);
            if (hasBody)
            {
                var text = string.IsNullOrEmpty(reason) ? body : $"{body}{(string.IsNullOrEmpty(body) ? string.Empty : "\n")}<color={Hex(InkBad)}>{reason}</color>";
                UiKit.Label("Body", card.transform, text, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.UpperLeft)
                    .Fit(14, UiKit.TypeScale.Caption + 2).rectTransform.Place(0.04f, hasCost ? 64f / height + 0.02f : 0.06f, 0.68f, top);
            }

            if (costs != null)
                Costs(card.transform, costs, wallet, seconds).Place(0.04f, 0.04f, 0.7f, 0.04f + 54f / height);
            else if (!string.IsNullOrEmpty(costText))
                UiKit.Label("Price", card.transform, costText, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleLeft)
                    .rectTransform.Place(0.04f, 0.04f, 0.7f, 0.04f + 54f / height);

            var button = UiKit.Button("Action", card.transform, action, () => click?.Invoke(), enabled ? fill ?? UiKit.Primary : UiKit.Panel, 32);
            button.interactable = enabled;
            var half = Mathf.Min(0.32f, 46f / height);
            ((RectTransform)button.transform).Place(0.71f, 0.5f - half, 0.97f, 0.5f + half);
            return card.rectTransform;
        }

        /// <summary>Resource chips in dark ink for light surfaces: icon and amount (red when short), then the time.</summary>
        public static RectTransform Costs(Transform parent, IReadOnlyList<KeyValuePair<ResourceType, long>> cost,
            IReadOnlyDictionary<ResourceType, long> wallet, float seconds = -1f)
        {
            var row = UiKit.Rect("Price", parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            foreach (var c in cost)
            {
                long have = 0;
                wallet?.TryGetValue(c.Key, out have);
                Chip(row, Hud.HudIcons.Resource(c.Key), CostChips.Compact(c.Value), wallet == null || have >= c.Value ? Ink : InkBad);
            }

            if (seconds >= 0f)
                Chip(row, "queue_build", Loc.Duration(seconds), InkSoft);
            return row;
        }

        private static void Chip(Transform row, string icon, string text, Color color)
        {
            var chip = UiKit.Rect("Chip", row);
            var h = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            var image = UiKit.Rect("Icon", chip).gameObject.AddComponent<Image>();
            image.sprite = Hud.HudIcons.Get(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = image.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 40f;
            var label = UiKit.Label("Value", chip, text, UiKit.TypeScale.Body, color, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>One price line as an inset chip: the resource icon and "have / need" (have in red while short).</summary>
        public static RectTransform CostChip(Transform parent, ResourceType resource, long have, long need)
        {
            var chip = UiKit.Box("Chip", parent, CardAlt);
            chip.sprite = UiKit.Rounded;
            chip.raycastTarget = false;
            Hud.HudIcons.Image(chip.transform, Hud.HudIcons.Resource(resource), 0.03f, 0.1f, 0.2f, 0.9f);
            var amount = have >= need ? $"{CostChips.Compact(have)} / {CostChips.Compact(need)}"
                : $"<color={Hex(InkBad)}>{CostChips.Compact(have)}</color> / {CostChips.Compact(need)}";
            UiKit.Label("Amount", chip.transform, amount, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                .Fit(16, UiKit.TypeScale.Body).rectTransform.Place(0.24f, 0, 0.97f, 1);
            return chip.rectTransform;
        }

        /// <summary>A timer bar with its text on it (time left, "12 / 40"); <paramref name="fill"/> is green, gold or blue.</summary>
        public static RectTransform Progress(Transform parent, float value, string text, string fill = "green")
        {
            var root = UiKit.Rect("Progress", parent);
            Skin.Bar(root, value, fill).Place(0, 0, 1, 1);
            var label = UiKit.Label("Text", root, text, UiKit.TypeScale.Caption + 2, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(0, 0, 1, 1);
            UiText.Outline(label, new Color(0.03f, 0.05f, 0.1f, 0.95f));
            return root;
        }

        /// <summary>Moves a <see cref="Progress"/> bar on (the speed-up window's live timer).</summary>
        public static void SetProgress(RectTransform progress, float value, string text)
        {
            if (progress == null)
                return;
            var fill = progress.Find("Bar/Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.rectTransform.Place(0, 0, Mathf.Clamp01(value), 1, 2);
                fill.enabled = value > 0.005f;
            }

            var label = progress.Find("Text")?.GetComponent<TMP_Text>();
            if (label != null)
                label.text = text;
        }

        /// <summary>What <see cref="Job"/> hands back: the bar to move on and the strip its buttons go into.</summary>
        public sealed class JobParts
        {
            public RectTransform Card;
            public RectTransform Bar;
            public RectTransform Buttons;
        }

        /// <summary>
        /// A running job (construction, training, healing, research, a salvage run): its name, the timer bar, and a
        /// strip for its buttons (Ask help, Speed up, Finish, Cancel) that shows only when one is added.
        /// </summary>
        public static JobParts Job(Transform parent, string title, float progress, string left, string fill = "green", bool buttons = true)
        {
            var card = UiKit.Column("Job", parent, 10f);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(22, 22, 14, 18);
            var bg = card.gameObject.AddComponent<Image>();
            bg.sprite = UiKit.Rounded;
            bg.type = Image.Type.Sliced;
            bg.color = Card;
            bg.raycastTarget = false;
            UiKit.Label("Title", card, title, UiKit.TypeScale.Body + 2, Ink, TextAnchor.MiddleLeft, FontStyle.Bold).Fit(18, UiKit.TypeScale.Body + 2).Row(52);
            var bar = Progress(card, progress, left, fill).Row(46);
            RectTransform strip = null;
            if (buttons)
            {
                strip = ButtonStrip(card);
                strip.Row(96);
            }

            return new JobParts { Card = card, Bar = bar, Buttons = strip };
        }

        /// <summary>A row that shares its width among the buttons put in it.</summary>
        public static RectTransform ButtonStrip(Transform parent, float spacing = 18f)
        {
            var strip = UiKit.Rect("Buttons", parent);
            var row = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            row.childAlignment = TextAnchor.MiddleCenter;
            return strip;
        }

        /// <summary>
        /// A grid card (build list, research nodes): the icon, the name, a short line under it (time, level, why it is
        /// locked) and an optional tag in the top right; the chosen one is gold, locked ones are faded.
        /// </summary>
        public static RectTransform Tile(Transform parent, string icon, string title, string line, Color lineColor, Action click, bool chosen = false,
            bool locked = false, string tag = null, float progress = -1f)
        {
            var tile = UiKit.Box("Tile", parent, chosen ? Highlight : Card);
            tile.sprite = UiKit.Rounded;
            var s = tile.gameObject.AddComponent<Shadow>();
            s.effectColor = chosen ? new Color(0.55f, 0.4f, 0.08f, 0.6f) : new Color(0.1f, 0.16f, 0.28f, 0.3f);
            s.effectDistance = new Vector2(0, -4);
            if (click != null)
            {
                tile.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    click();
                });
                tile.gameObject.AddComponent<PressFeedback>();
            }

            var art = Hud.HudIcons.Image(tile.transform, icon, 0.2f, 0.42f, 0.8f, 0.93f);
            if (locked)
                art.color = new Color(1f, 1f, 1f, 0.35f);
            UiKit.Label("Name", tile.transform, title, UiKit.TypeScale.Body, locked ? InkSoft : Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .Fit(14, UiKit.TypeScale.Body).rectTransform.Place(0.04f, progress >= 0f ? 0.26f : 0.2f, 0.96f, 0.42f);
            UiKit.Label("Line", tile.transform, line, UiKit.TypeScale.Caption, lineColor, TextAnchor.MiddleCenter, FontStyle.Bold)
                .Fit(12, UiKit.TypeScale.Caption).rectTransform.Place(0.04f, progress >= 0f ? 0.12f : 0.03f, 0.96f, progress >= 0f ? 0.27f : 0.21f);
            if (progress >= 0f)
                Skin.Bar(tile.transform, progress, "blue").Place(0.1f, 0.04f, 0.9f, 0.1f);
            if (!string.IsNullOrEmpty(tag))
                UiKit.Label("Tag", tile.transform, tag, UiKit.TypeScale.Caption, InkSoft, TextAnchor.UpperRight, FontStyle.Bold)
                    .rectTransform.Place(0.5f, 0.82f, 0.95f, 0.97f);
            return tile.rectTransform;
        }

        /// <summary>
        /// A shop-style item card (the speed-up window, as Last Asylum's shield cards): the name on a band, the rarity
        /// tile with its tag, "Owned: n" and the button.
        /// </summary>
        public static RectTransform ItemCard(Transform parent, string name, string icon, string rarity, string tag, string owned, string action, Action click,
            Color? fill = null)
        {
            var card = UiKit.Box("ItemCard", parent, Card);
            card.sprite = UiKit.Rounded;
            card.raycastTarget = false;
            var band = UiKit.Box("Band", card.transform, Band);
            band.sprite = UiKit.Rounded;
            band.raycastTarget = false;
            band.rectTransform.Place(0, 0.79f, 1, 1);
            UiKit.Label("Name", band.transform, name, UiKit.TypeScale.Caption, Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .Fit(12, UiKit.TypeScale.Caption).rectTransform.Place(0.04f, 0.04f, 0.96f, 0.96f);
            ItemTile(card.transform, icon, rarity, null, null, tag).Place(0.12f, 0.37f, 0.88f, 0.77f);
            UiKit.Label("Owned", card.transform, owned, UiKit.TypeScale.Caption, InkSoft, TextAnchor.MiddleCenter)
                .Fit(12, UiKit.TypeScale.Caption).rectTransform.Place(0.04f, 0.24f, 0.96f, 0.36f);
            var button = UiKit.Button("Action", card.transform, action, () => click?.Invoke(), fill ?? UiKit.Primary, 28);
            ((RectTransform)button.transform).Place(0.06f, 0.02f, 0.94f, 0.23f);
            return card.rectTransform;
        }

        /// <summary>A requirement line on a light card: icon, what is needed (red while not met) and a tick or a cross.</summary>
        public static RectTransform NeedRow(Transform parent, string icon, string text, bool met)
        {
            var row = UiKit.Box("Need", parent, Card);
            row.sprite = UiKit.Rounded;
            row.raycastTarget = false;
            Hud.HudIcons.Image(row.transform, icon, 0.02f, 0.12f, 0.12f, 0.88f);
            UiKit.Label("Text", row.transform, text, UiKit.TypeScale.Body, met ? Ink : InkBad, TextAnchor.MiddleLeft, FontStyle.Bold)
                .Fit(16, UiKit.TypeScale.Body).rectTransform.Place(0.15f, 0, 0.86f, 1);
            UiKit.Label("Mark", row.transform, met ? "✓" : "✗", UiKit.TypeScale.Title, met ? InkGood : InkBad, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0.86f, 0, 0.98f, 1);
            return row.rectTransform;
        }

        /// <summary>
        /// A horizontal strip of tiles (troop tiers) that scrolls sideways; returns the content to add cells to (give
        /// each a <see cref="LayoutElement"/> width) and the scroll for its position.
        /// </summary>
        public static (RectTransform Content, ScrollRect Scroll) Strip(Transform parent, float spacing = 12f)
        {
            var viewport = UiKit.Box("Strip", parent, new Color(0, 0, 0, 0.001f));
            viewport.sprite = null;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.decelerationRate = 0.12f;
            var content = UiKit.Rect("Content", viewport.transform);
            content.anchorMin = Vector2.zero;
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.padding = new RectOffset(4, 4, 6, 6);
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            return (content, scroll);
        }

        /// <summary>Item-frame tone of a troop tier (as in the bag): 1–3 common, 4–6 rare, 7–8 epic, 9+ legendary.</summary>
        public static string TierRarity(int tier) => tier >= 9 ? "legendary" : tier >= 7 ? "epic" : tier >= 4 ? "rare" : "common";
    }

    /// <summary>
    /// Sizes a <see cref="UiFrames.SizedDialog"/> card to its list (plus the pinned footer and the subtitle) each frame,
    /// capped at <see cref="Max"/> of the screen, past which the list scrolls.
    /// </summary>
    public sealed class DialogFit : MonoBehaviour
    {
        public RectTransform Content;
        public RectTransform Viewport;
        public RectTransform Footer;
        public TMP_Text Subtitle;
        public float Max = 0.84f;
        public float Min = 300f;

        private const float SubtitleHeight = 50f;
        private const float Chrome = 104f + 24f;

        private void LateUpdate()
        {
            var card = (RectTransform)transform;
            var parent = card.parent as RectTransform;
            if (parent == null || Content == null || Viewport == null)
                return;

            var foot = 0f;
            if (Footer != null)
            {
                var any = false;
                for (var i = 0; i < Footer.childCount && !any; i++)
                    any = Footer.GetChild(i).gameObject.activeSelf;
                if (Footer.gameObject.activeSelf != any)
                    Footer.gameObject.SetActive(any);
                foot = any ? LayoutUtility.GetPreferredHeight(Footer) : 0f;
            }

            var top = Subtitle != null && !string.IsNullOrEmpty(Subtitle.text) ? SubtitleHeight : 0f;
            Viewport.offsetMin = new Vector2(0, foot);
            Viewport.offsetMax = new Vector2(0, -top);
            var want = LayoutUtility.GetPreferredHeight(Content) + foot + top + Chrome;
            var height = Mathf.Clamp(want, Min, Mathf.Max(Min, parent.rect.height * Max));
            if (Mathf.Abs(card.sizeDelta.y - height) > 0.5f)
                card.sizeDelta = new Vector2(card.sizeDelta.x, height);
        }
    }
}
