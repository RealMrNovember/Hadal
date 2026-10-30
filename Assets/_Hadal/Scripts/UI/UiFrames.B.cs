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
    /// Parts of the bag, heroes, quests and VIP pages (Last Asylum's bag.jpg, heroes.jpg, daily_quests.jpg, vip.jpg):
    /// resource chips for the page bar, light plates and rows, a progress bar with its value on it, cost lines in ink,
    /// and the parts that pin themselves outside a page's scrolling list.
    /// </summary>
    public static partial class UiFrames
    {
        /// <summary>
        /// A resource chip for the page bar (the genre's dark pill with the icon and a compact amount). The value is
        /// read again twice a second, so the chip follows the wallet while the page stays as it is.
        /// </summary>
        public static RectTransform Chip(Transform parent, string icon, Func<string> value, float width = 150f)
        {
            var chip = UiKit.Box("Chip", parent, new Color(0.03f, 0.06f, 0.11f, 0.55f));
            chip.sprite = UiKit.Rounded;
            chip.raycastTarget = false;
            var element = chip.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = width;
            element.preferredHeight = element.minHeight = 54f;
            Hud.HudIcons.Image(chip.transform, icon, 0.02f, 0.04f, 0.32f, 0.96f).preserveAspect = true;
            var text = UiKit.Label("Value", chip.transform, value(), UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Place(0.3f, 0, 0.98f, 1);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.Fit(14, UiKit.TypeScale.Caption);
            UiText.Outline(text, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            LiveText.Bind(text, value, 0.5f);
            return chip.rectTransform;
        }

        /// <summary>A square light button for the page bar (the bag's overview, the VIP page's shop).</summary>
        public static Button BarButton(Transform parent, string icon, Action click)
        {
            var box = UiKit.Box("BarButton", parent, new Color(0.93f, 0.95f, 0.98f, 1f));
            box.sprite = UiKit.Rounded;
            var element = box.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 70f;
            element.preferredHeight = element.minHeight = 70f;
            var button = box.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                click?.Invoke();
            });
            box.gameObject.AddComponent<PressFeedback>();
            Hud.HudIcons.Image(box.transform, icon, 0.14f, 0.14f, 0.86f, 0.86f).preserveAspect = true;
            return button;
        }

        /// <summary>A light rounded plate (rows, the hero's stat card); its text is dark ink.</summary>
        public static Image Plate(Transform parent, Color? color = null)
        {
            var plate = UiKit.Box("Plate", parent, color ?? Card);
            plate.sprite = UiKit.Rounded;
            plate.type = Image.Type.Sliced;
            plate.raycastTarget = false;
            return plate;
        }

        /// <summary>A progress bar (the skin's track and fill) with "value/max" written across it.</summary>
        public static RectTransform Progress(Transform parent, long value, long max, string fill = null)
        {
            var bar = Skin.Bar(parent, max > 0 ? (float)value / max : 0f, fill ?? (value >= max && max > 0 ? "gold" : "green"));
            var text = UiKit.Label("Value", bar, $"{value:N0}/{max:N0}", UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Place(0, 0, 1, 1);
            UiText.Outline(text, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            return bar;
        }

        /// <summary>A cost line on a light surface: icon and amount per resource, red where the wallet is short.</summary>
        public static RectTransform CostLine(Transform parent, IReadOnlyList<KeyValuePair<ResourceType, long>> cost,
            IReadOnlyDictionary<ResourceType, long> wallet)
        {
            var row = UiKit.Rect("Cost", parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            foreach (var c in cost)
            {
                wallet.TryGetValue(c.Key, out var have);
                var chip = UiKit.Rect("Chip", row);
                var h = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 6f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
                var image = UiKit.Rect("Icon", chip).gameObject.AddComponent<Image>();
                image.sprite = Hud.HudIcons.Get(Hud.HudIcons.Resource(c.Key));
                image.preserveAspect = true;
                image.raycastTarget = false;
                var element = image.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.minWidth = 44f;
                var label = UiKit.Label("Value", chip, $"{CostChips.Compact(have)}/{CostChips.Compact(c.Value)}", UiKit.TypeScale.Body,
                    have >= c.Value ? InkGood : InkBad, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            return row;
        }

        /// <summary>A reward cell: the rarity tile and the reward's name under it (reward and result grids).</summary>
        public static RectTransform RewardCell(Transform parent, string icon, string rarity, string count, string name)
        {
            var cell = UiKit.Rect("Reward", parent);
            ItemTile(cell, icon, rarity, count).Place(0.08f, 0.3f, 0.92f, 1f);
            var label = UiKit.Label("Name", cell, name, UiKit.TypeScale.Caption, Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            label.rectTransform.Place(0, 0, 1, 0.28f);
            label.Fit(14, UiKit.TypeScale.Caption);
            return cell;
        }
    }

    /// <summary>A label that reads its text again every so often (bar chips, timers) without the page redrawing.</summary>
    public sealed class LiveText : MonoBehaviour
    {
        private Func<string> _source;
        private TMP_Text _label;
        private float _every;
        private float _next;

        public static void Bind(TMP_Text label, Func<string> source, float every)
        {
            var live = label.gameObject.AddComponent<LiveText>();
            live._label = label;
            live._source = source;
            live._every = every;
        }

        private void Update()
        {
            if (Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + _every;
            var text = _source?.Invoke() ?? string.Empty;
            if (_label.text != text)
                _label.text = text;
        }
    }

    /// <summary>A gentle up-and-down hop (a chest that can be opened, a reward waiting).</summary>
    public sealed class Bob : MonoBehaviour
    {
        public float Height = 8f;
        private float _phase;

        private void Awake() => _phase = UnityEngine.Random.value * 6f;

        private void Update()
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Height * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f + _phase)));
        }
    }

    /// <summary>Calls back when its object is switched off (a page that closed).</summary>
    public sealed class DisableWatch : MonoBehaviour
    {
        public Action Disabled;

        private void OnDisable() => Disabled?.Invoke();
    }

    /// <summary>
    /// A page part that sits outside the scrolling list (tabs, a bottom action bar, chips in the top bar). It lives as
    /// long as its marker in the list: the next redraw empties the list and the part goes with it, so no page leaves
    /// its parts on another. A newer part of the same name replaces it at once.
    /// </summary>
    public sealed class PinnedPart : MonoBehaviour
    {
        private static readonly List<PinnedPart> All = new();
        private GameObject _marker;

        /// <summary>A part named <paramref name="name"/> on <paramref name="parent"/>, tied to a marker in <paramref name="list"/>.</summary>
        public static RectTransform Create(Transform parent, Transform list, string name)
        {
            for (var i = All.Count - 1; i >= 0; i--)
            {
                if (All[i] != null && All[i].name == name)
                    Destroy(All[i].gameObject);
            }

            var marker = UiKit.Rect("Pin." + name, list);
            marker.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = UiKit.Rect(name, parent);
            var part = rect.gameObject.AddComponent<PinnedPart>();
            part._marker = marker.gameObject;
            All.Add(part);
            return rect;
        }

        private void LateUpdate()
        {
            if (_marker == null)
                Destroy(gameObject);
        }

        private void OnDestroy() => All.Remove(this);
    }
}
