using System;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The social screens' parts (alliance, events, chat, mail), after Last Asylum: the light card every row sits on, the
    /// label · value info line of the alliance banner, the ranking table (gold, silver and bronze rows, the viewer's own
    /// row green), the mail home's category rows, the alliance hub's bottom icon bar and the event centre's icon tabs.
    /// </summary>
    public static partial class UiFrames
    {
        /// <summary>The podium rows of a ranking and the viewer's own (pinned) row.</summary>
        public static readonly Color RankGold = new(0.98f, 0.87f, 0.55f, 1f);
        public static readonly Color RankSilver = new(0.86f, 0.89f, 0.94f, 1f);
        public static readonly Color RankBronze = new(0.94f, 0.8f, 0.66f, 1f);
        public static readonly Color RankMine = new(0.71f, 0.89f, 0.62f, 1f);

        /// <summary>A light rounded card with a soft shadow (rows, banners, forms), drawn in dark ink.</summary>
        public static Image CardBox(Transform parent, Color? color = null, string name = "Card")
        {
            var card = UiKit.Box(name, parent, color ?? Card);
            card.sprite = UiKit.Rounded;
            card.type = Image.Type.Sliced;
            var s = card.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0.1f, 0.16f, 0.28f, 0.28f);
            s.effectDistance = new Vector2(0, -4);
            return card;
        }

        /// <summary>One "Leader:  Name" line of a banner card: the label soft on the left, the value bold on the right.</summary>
        public static void InfoLine(Transform parent, string label, string value, float x0, float y0, float x1, float y1)
        {
            UiKit.Label("Label", parent, label, UiKit.TypeScale.Body, InkSoft, TextAnchor.MiddleLeft).rectTransform.Place(x0, y0, x0 + (x1 - x0) * 0.45f, y1);
            var v = UiKit.Label("Value", parent, value, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            v.rectTransform.Place(x0 + (x1 - x0) * 0.35f, y0, x1, y1);
            v.richText = false;
            v.Fit(16, UiKit.TypeScale.Body);
        }

        // --- Ranking table ------------------------------------------------------------------

        private const float RankX = 0.15f;
        private const float NameX = 0.17f;
        private const float ValueX = 0.66f;

        /// <summary>The ranking table's header band: rank · name · value.</summary>
        public static RectTransform RankHead(Transform parent, string rank, string name, string value)
        {
            var band = UiKit.Box("Head", parent, Band);
            band.sprite = UiKit.Rounded;
            band.raycastTarget = false;
            UiKit.Label("Rank", band.transform, rank, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, RankX, 1);
            UiKit.Label("Name", band.transform, name, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(NameX, 0, ValueX, 1);
            UiKit.Label("Value", band.transform, value, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Place(ValueX, 0, 0.96f, 1);
            return band.rectTransform;
        }

        /// <summary>
        /// One ranking line: the place (a gold, silver or bronze medal for the podium, whose rows are tinted to match), a
        /// portrait slot (<paramref name="face"/>, filled by the caller), the name and the value. The viewer's own row
        /// (<paramref name="mine"/>) is green, as Last Asylum pins it under the table.
        /// </summary>
        public static RectTransform RankRow(Transform parent, int rank, string name, string value, bool mine, int index, out RectTransform face)
        {
            var color = mine ? RankMine : rank switch
            {
                1 => RankGold,
                2 => RankSilver,
                3 => RankBronze,
                _ => index % 2 == 0 ? Card : CardAlt
            };
            var row = UiKit.Box("Rank", parent, color);
            row.sprite = UiKit.Rounded;
            row.raycastTarget = false;

            if (rank is >= 1 and <= 3)
            {
                var medal = UiKit.Box("Medal", row.transform, rank == 1 ? new Color(0.96f, 0.7f, 0.12f) : rank == 2 ? new Color(0.62f, 0.68f, 0.76f) : new Color(0.78f, 0.47f, 0.24f));
                medal.sprite = UiKit.Circle;
                medal.type = Image.Type.Simple;
                medal.preserveAspect = true;
                medal.raycastTarget = false;
                medal.rectTransform.Place(0.02f, 0.14f, RankX - 0.01f, 0.86f);
                var n = UiKit.Label("Place", medal.transform, rank.ToString(), UiKit.TypeScale.Subtitle, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                n.rectTransform.Place(0, 0, 1, 1);
                UiText.Outline(n, new Color(0.25f, 0.15f, 0.03f, 0.9f));
            }
            else
            {
                var n = UiKit.Label("Place", row.transform, rank > 0 ? rank.ToString("N0") : "-", UiKit.TypeScale.Body, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                n.rectTransform.Place(0, 0, RankX, 1);
                n.Fit(14, UiKit.TypeScale.Body);
            }

            face = UiKit.Rect("Face", row.transform).Place(NameX, 0.1f, NameX + 0.1f, 0.9f);
            var who = UiKit.Label("Name", row.transform, name, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            who.rectTransform.Place(NameX + 0.12f, 0.05f, ValueX, 0.95f);
            who.richText = false;
            who.Fit(16, UiKit.TypeScale.Body);
            var v = UiKit.Label("Value", row.transform, value, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            v.rectTransform.Place(ValueX, 0.05f, 0.96f, 0.95f);
            v.Fit(14, UiKit.TypeScale.Body);
            return row.rectTransform;
        }

        // --- Lists and bars -----------------------------------------------------------------

        /// <summary>
        /// A category line (the mail home): the icon on a pale disc, the name, an optional badge (an unread count) and the
        /// chevron; a red dot in the corner while something waits.
        /// </summary>
        public static RectTransform CategoryRow(Transform parent, string icon, string label, string badge, bool dot, Action click)
        {
            var row = CardBox(parent, null, "Category");
            row.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                click?.Invoke();
            });
            row.gameObject.AddComponent<PressFeedback>();
            var disc = UiKit.Box("Disc", row.transform, Band);
            disc.sprite = UiKit.Circle;
            disc.type = Image.Type.Simple;
            disc.preserveAspect = true;
            disc.raycastTarget = false;
            disc.rectTransform.Place(0.02f, 0.08f, 0.2f, 0.92f);
            Hud.HudIcons.Image(disc.transform, icon, 0.16f, 0.16f, 0.84f, 0.84f);
            var name = UiKit.Label("Label", row.transform, label, UiKit.TypeScale.Subtitle, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.Place(0.25f, 0, 0.72f, 1);
            name.Fit(18, UiKit.TypeScale.Subtitle);
            if (!string.IsNullOrEmpty(badge))
            {
                var pill = UiKit.Box("Badge", row.transform, new Color(0.95f, 0.18f, 0.15f, 1f));
                pill.sprite = UiKit.Rounded;
                pill.raycastTarget = false;
                pill.rectTransform.Place(0.74f, 0.3f, 0.88f, 0.7f);
                var count = UiKit.Label("Count", pill.transform, badge, UiKit.TypeScale.Caption + 2, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                count.rectTransform.Place(0, 0, 1, 1);
            }

            UiKit.Label("Chevron", row.transform, "›", UiKit.TypeScale.Display, InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.89f, 0, 0.98f, 1);
            if (dot)
                Dot(row.transform);
            return row.rectTransform;
        }

        /// <summary>
        /// The navy icon bar along a page's bottom edge (the alliance hub's war log, log, applications, settings): each an
        /// icon on a raised tile with its name under it, a red dot where something waits. Anchored to the bottom of
        /// <paramref name="parent"/>, <paramref name="height"/> reference pixels tall.
        /// </summary>
        public static RectTransform IconBar(Transform parent, (string Icon, string Label, bool Dot, Action Click)[] items, float height = 170f)
        {
            var bar = UiKit.Box("IconBar", parent, Navy);
            bar.sprite = null;
            var rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, height);
            rect.anchoredPosition = Vector2.zero;
            var w = 1f / Math.Max(1, items.Length);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var cell = UiKit.Rect("Item", rect).Place(i * w, 0, (i + 1) * w, 1);
                var hit = cell.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0.001f);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    item.Click?.Invoke();
                });
                cell.gameObject.AddComponent<PressFeedback>();
                var tile = UiKit.Box("Tile", cell, new Color(0.3f, 0.38f, 0.5f, 1f));
                tile.sprite = UiKit.Rounded;
                tile.raycastTarget = false;
                tile.preserveAspect = false;
                tile.rectTransform.anchorMin = tile.rectTransform.anchorMax = new Vector2(0.5f, 0.64f);
                tile.rectTransform.sizeDelta = new Vector2(height * 0.56f, height * 0.56f);
                var s = tile.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0.02f, 0.06f, 0.6f);
                s.effectDistance = new Vector2(0, -4);
                Hud.HudIcons.Image(tile.transform, item.Icon, 0.14f, 0.14f, 0.86f, 0.86f);
                var label = UiKit.Label("Label", cell, item.Label, UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.Place(0.02f, 0.02f, 0.98f, 0.3f);
                label.Fit(12, UiKit.TypeScale.Caption);
                UiText.Outline(label, new Color(0.04f, 0.07f, 0.12f, 0.9f));
                if (item.Dot)
                    Dot(tile.transform);
            }

            return rect;
        }

        /// <summary>
        /// A horizontal, scrolling strip of picture tabs (the event centre): returns the content to add
        /// <see cref="IconTab"/>s to; <paramref name="viewport"/> is the strip to size.
        /// </summary>
        public static RectTransform IconStrip(Transform parent, out RectTransform viewport)
        {
            var view = UiKit.Box("Strip", parent, new Color(0, 0, 0, 0.001f));
            view.sprite = null;
            view.gameObject.AddComponent<RectMask2D>();
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            var content = UiKit.Rect("Content", view.transform);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12;
            row.padding = new RectOffset(8, 8, 4, 4);
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view.rectTransform;
            scroll.content = content;
            viewport = view.rectTransform;
            return content;
        }

        /// <summary>
        /// One picture tab: the event's art (or an icon) in a rounded square, its name under it; the chosen one framed in
        /// gold and the rest dimmed, a red dot when a reward waits.
        /// </summary>
        public static RectTransform IconTab(Transform parent, Texture art, string icon, string label, bool on, Action click, bool dot, float width = 196f)
        {
            var root = UiKit.Rect("Tab", parent);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = layout.minWidth = width;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            root.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                click?.Invoke();
            });
            root.gameObject.AddComponent<PressFeedback>();

            var frame = UiKit.Box("Frame", root, on ? new Color(0.98f, 0.78f, 0.25f, 1f) : new Color(0.5f, 0.58f, 0.7f, 1f));
            frame.sprite = UiKit.Rounded;
            frame.raycastTarget = false;
            frame.rectTransform.Place(0.04f, 0.3f, 0.96f, 1f);
            var picture = UiKit.Box("Picture", frame.transform, Color.white);
            picture.sprite = UiKit.Rounded;
            picture.raycastTarget = false;
            picture.rectTransform.Place(0, 0, 1, 1, 6);
            picture.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (art != null)
            {
                var image = UiKit.Rect("Art", picture.transform).Place(0, 0, 1, 1).gameObject.AddComponent<RawImage>();
                image.texture = art;
                image.uvRect = new Rect(0.25f, 0f, 0.5f, 1f); // the art is 2:1; the middle square
                image.raycastTarget = false;
                image.color = on ? Color.white : new Color(0.7f, 0.74f, 0.8f, 1f);
            }
            else
            {
                picture.color = Navy;
                Hud.HudIcons.Image(picture.transform, icon, 0.15f, 0.15f, 0.85f, 0.85f);
            }

            var name = UiKit.Label("Label", root, label, UiKit.TypeScale.Caption, on ? Ink : InkSoft, TextAnchor.UpperCenter, FontStyle.Bold);
            name.rectTransform.Place(0, 0, 1, 0.29f);
            name.Fit(12, UiKit.TypeScale.Caption);
            if (dot)
                Dot(frame.transform);
            return root;
        }
    }
}
