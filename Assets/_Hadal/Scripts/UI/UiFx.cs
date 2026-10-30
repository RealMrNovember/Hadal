using System.Collections.Generic;
using HADAL.Shared.Enums;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>Buttons dip to 94% while pressed and spring back with a small overshoot (genre feel, docs/26).</summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private float _target = 1f;
        private float _velocity;
        private Selectable _selectable;

        private void Awake() => _selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable == null || _selectable.interactable)
                _target = 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData) => _target = 1f;

        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void OnDisable()
        {
            _target = 1f;
            _velocity = 0f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            var scale = transform.localScale.x;
            if (Mathf.Abs(scale - _target) < 0.0005f && Mathf.Abs(_velocity) < 0.001f)
                return;
            // A light spring: quick dip, a little bounce on release.
            _velocity += (_target - scale) * 900f * Time.unscaledDeltaTime;
            _velocity *= Mathf.Exp(-28f * Time.unscaledDeltaTime);
            scale += _velocity * Time.unscaledDeltaTime;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Panels fade and scale in (0.9 → 1, ease-out-back, 0.2 s) every time they are shown.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopIn : MonoBehaviour
    {
        public float From = 0.9f;
        public float Duration = 0.2f;

        /// <summary>Slide up from below instead of scaling (bottom sheets).</summary>
        public bool Slide;

        private CanvasGroup _group;
        private float _t = 1f;
        private Vector2 _restPosition;
        private bool _hasRest;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            _t = 0f;
            GameAudio.Play(Sfx.PanelOpen);
            var rect = (RectTransform)transform;
            if (!_hasRest)
            {
                _restPosition = rect.anchoredPosition;
                _hasRest = true;
            }

            Apply(0f);
        }

        private void OnDisable()
        {
            _t = 1f;
            Apply(1f);
        }

        private void Update()
        {
            if (_t >= 1f)
                return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Mathf.Max(0.01f, Duration));
            Apply(_t);
        }

        private void Apply(float t)
        {
            // Ease-out-back: overshoots by a few percent, then settles.
            const float c1 = 1.4f;
            var u = t - 1f;
            var eased = 1f + (c1 + 1f) * u * u * u + c1 * u * u;
            _group.alpha = Mathf.Clamp01(t * 1.6f);
            var rect = (RectTransform)transform;
            if (Slide)
            {
                rect.anchoredPosition = _restPosition + new Vector2(0f, -140f * (1f - eased));
                transform.localScale = Vector3.one;
            }
            else
            {
                var s = Mathf.LerpUnclamped(From, 1f, eased);
                transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    /// <summary>
    /// One place for short notices: dark pills near the top-centre of the screen, newest at the bottom of a stack of
    /// at most three, shown for two seconds and faded upwards (genre pattern, see docs/26).
    /// </summary>
    public static class Toaster
    {
        private sealed class Entry
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public float Born;
        }

        private const float Life = 2.2f;
        private const float Fade = 0.25f;
        private static readonly List<Entry> Entries = new();
        private static RectTransform _root;
        private static ToasterRunner _runner;

        public static void Show(string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;
            EnsureRoot();
            if (color == UiKit.Danger)
                GameAudio.Play(Sfx.Error);

            // The same notice twice in a row just refreshes the first one.
            if (Entries.Count > 0 && Entries[^1].Rect.GetComponentInChildren<TMP_Text>().text == text)
            {
                Entries[^1].Born = Time.unscaledTime;
                return;
            }

            var pill = UiKit.Box("Toast", _root, new Color(0.02f, 0.06f, 0.09f, 0.92f));
            pill.raycastTarget = false;
            var rect = pill.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var edge = UiKit.Box("Edge", pill.transform, color);
            edge.raycastTarget = false;
            edge.rectTransform.Place(0f, 0f, 0f, 1f);
            edge.rectTransform.offsetMax = new Vector2(10f, 0f);
            var label = UiKit.Label("Text", pill.transform, text, 32, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(0f, 0f, 1f, 1f);
            label.rectTransform.offsetMin = new Vector2(34f, 10f);
            label.rectTransform.offsetMax = new Vector2(-26f, -10f);
            label.textWrappingMode = TextWrappingModes.Normal;

            // Width follows the text, up to 88% of the reference width.
            var width = Mathf.Clamp(label.preferredWidth + 70f, 320f, 950f);
            var lines = Mathf.Clamp(Mathf.CeilToInt((label.preferredWidth + 70f) / 950f), 1, 3);
            rect.sizeDelta = new Vector2(width, 44f + lines * label.fontSize * 1.25f);

            Entries.Add(new Entry { Rect = rect, Group = pill.gameObject.AddComponent<CanvasGroup>(), Born = Time.unscaledTime });
            while (Entries.Count > 3)
            {
                Object.Destroy(Entries[0].Rect.gameObject);
                Entries.RemoveAt(0);
            }
        }

        private static void EnsureRoot()
        {
            if (_root != null)
                return;
            var canvas = UiKit.Canvas("Toasts", 300);
            Object.DontDestroyOnLoad(canvas.gameObject);
            _root = UiKit.Rect("Stack", canvas.transform).Place(0f, 0f, 1f, 0.8f);
            _runner = canvas.gameObject.AddComponent<ToasterRunner>();
        }

        internal static void Tick()
        {
            var now = Time.unscaledTime;
            var y = 0f;
            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];
                if (e.Rect == null)
                {
                    Entries.RemoveAt(i);
                    continue;
                }

                var age = now - e.Born;
                if (age > Life + Fade)
                {
                    Object.Destroy(e.Rect.gameObject);
                    Entries.RemoveAt(i);
                    continue;
                }

                var fadeIn = Mathf.Clamp01(age / 0.15f);
                var fadeOut = age > Life ? 1f - (age - Life) / Fade : 1f;
                e.Group.alpha = fadeIn * fadeOut;
                var rise = age > Life ? (age - Life) / Fade * 30f : (1f - fadeIn) * -20f;
                e.Rect.anchoredPosition = Vector2.Lerp(e.Rect.anchoredPosition, new Vector2(0f, -y + rise), UiKit.Damp(24f));
                y += e.Rect.sizeDelta.y + 12f;
            }
        }
    }

    internal sealed class ToasterRunner : MonoBehaviour
    {
        private void Update() => Toaster.Tick();
    }

    /// <summary>Resource cost chips: icon + amount, red when the wallet is short (have/need on hover-less phones).</summary>
    public static class CostChips
    {
        public static RectTransform Row(Transform parent, IReadOnlyList<KeyValuePair<ResourceType, long>> cost,
            IReadOnlyDictionary<ResourceType, long> wallet, float seconds = -1f)
        {
            var row = UiKit.Rect("Cost", parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            foreach (var c in cost)
            {
                wallet.TryGetValue(c.Key, out var have);
                Chip(row, Hud.HudIcons.Resource(c.Key), Compact(c.Value), have >= c.Value ? UiKit.Text : UiKit.Danger);
            }

            if (seconds >= 0f)
                Chip(row, "queue_build", Loc.Duration(seconds), UiKit.Accent);
            return row;
        }

        private static void Chip(Transform row, string icon, string text, Color color)
        {
            var chip = UiKit.Rect("Chip", row);
            var h = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var image = UiKit.Rect("Icon", chip).gameObject.AddComponent<Image>();
            image.sprite = Hud.HudIcons.Get(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = image.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 40f;

            var label = UiKit.Label("Value", chip, text, 30, color, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        public static string Compact(long value) =>
            value >= 1_000_000 ? (value / 1_000_000f).ToString(value >= 10_000_000 ? "0" : "0.#", System.Globalization.CultureInfo.InvariantCulture) + "M"
            : value >= 10_000 ? (value / 1000f).ToString(value >= 100_000 ? "0" : "0.#", System.Globalization.CultureInfo.InvariantCulture) + "K"
            : value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
