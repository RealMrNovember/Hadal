using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The genre's "tap something in the world" menu (Last Asylum, docs/26): a dark selection ring under the
    /// target, a small info bubble above it and round action buttons in an arc below, following the target while
    /// the camera glides. Tapping anywhere else closes it. One menu at a time, shared by the base and the world map.
    /// </summary>
    public sealed class TargetMenu : MonoBehaviour
    {
        public readonly struct Item
        {
            public Item(string icon, string label, Action click, bool enabled = true)
            {
                Icon = icon;
                Label = label;
                Click = click;
                Enabled = enabled;
            }

            public string Icon { get; }
            public string Label { get; }
            public Action Click { get; }
            public bool Enabled { get; }
        }

        private static TargetMenu _instance;

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _ring;
        private RectTransform _bubble;
        private Text _title;
        private Text _info;
        private RectTransform _bar;
        private Image _barFill;
        private Text _barText;
        private readonly List<RectTransform> _buttons = new();

        private Func<Camera> _camera;
        private Func<Vector3> _target;
        private Action _closed;
        private float _openedAt;

        public static bool IsOpen => _instance != null && _instance._root.gameObject.activeSelf;

        public static void Show(Func<Camera> camera, Func<Vector3> target, string title, string info, IReadOnlyList<Item> items,
            float bar = -1f, string barText = "", Action closed = null)
        {
            var menu = Instance();
            menu.Open(camera, target, title, info, items, bar, barText, closed);
        }

        public static void Hide()
        {
            if (_instance == null || !IsOpen)
                return;
            _instance._root.gameObject.SetActive(false);
            var closed = _instance._closed;
            _instance._closed = null;
            closed?.Invoke();
        }

        private static TargetMenu Instance()
        {
            if (_instance != null)
                return _instance;
            var go = new GameObject("TargetMenu");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TargetMenu>();
            _instance.Build();
            return _instance;
        }

        private void Build()
        {
            _canvas = UiKit.Canvas("TargetMenuUi", 38);
            _canvas.transform.SetParent(transform, false);
            _root = UiKit.Rect("Root", _canvas.transform).Place(0, 0, 1, 1);

            // Anywhere outside the menu closes it (the genre behaviour).
            var catcher = _root.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0.001f);
            _root.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                // The tap that opened the menu must not close it on the same frame.
                if (Time.unscaledTime - _openedAt > 0.2f)
                    Hide();
            });

            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(_root, false);
            _ring = (RectTransform)ring.transform;
            var ringImage = ring.GetComponent<Image>();
            ringImage.sprite = UiKit.CircleRing;
            ringImage.color = new Color(0f, 0.02f, 0.04f, 0.6f);
            ringImage.raycastTarget = false;
            _ring.sizeDelta = new Vector2(420f, 250f);

            var bubble = UiKit.Box("Bubble", _root, UiKit.Panel);
            _bubble = bubble.rectTransform;
            _bubble.sizeDelta = new Vector2(600f, 170f);
            _bubble.pivot = new Vector2(0.5f, 0f);
            bubble.gameObject.AddComponent<Button>(); // taps on the bubble do not close the menu
            _title = UiKit.Label("Title", _bubble, string.Empty, 36, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            _title.rectTransform.Place(0.05f, 0.62f, 0.95f, 0.95f);
            _info = UiKit.Label("Info", _bubble, string.Empty, 26, UiKit.Text, TextAnchor.UpperLeft);
            _info.rectTransform.Place(0.05f, 0.08f, 0.95f, 0.6f);

            var back = UiKit.Box("Bar", _bubble, new Color(0, 0, 0, 0.5f));
            _bar = back.rectTransform;
            _bar.Place(0.05f, 0.1f, 0.95f, 0.26f);
            _barFill = UiKit.Box("Fill", _bar, new Color(0.36f, 0.85f, 0.45f, 1f));
            _barText = UiKit.Label("Text", _bar, string.Empty, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _barText.rectTransform.Place(0, 0, 1, 1);

            _root.gameObject.SetActive(false);
        }

        private void Open(Func<Camera> camera, Func<Vector3> target, string title, string info, IReadOnlyList<Item> items,
            float bar, string barText, Action closed)
        {
            var previous = _closed;
            _closed = null;
            if (IsOpen && previous != null)
                previous();

            _camera = camera;
            _target = target;
            _closed = closed;
            _openedAt = Time.unscaledTime;
            _title.text = title;
            _info.text = info;
            _bar.gameObject.SetActive(bar >= 0f);
            if (bar >= 0f)
            {
                _barFill.rectTransform.Place(0, 0, Mathf.Clamp(bar, 0.02f, 1f), 1);
                _barText.text = barText;
                _info.rectTransform.Place(0.05f, 0.3f, 0.95f, 0.6f);
            }
            else
            {
                _info.rectTransform.Place(0.05f, 0.08f, 0.95f, 0.6f);
            }

            foreach (var b in _buttons)
                Destroy(b.gameObject);
            _buttons.Clear();
            foreach (var item in items)
                _buttons.Add(BuildButton(item));

            _root.gameObject.SetActive(true);
            Follow();
        }

        private RectTransform BuildButton(Item item)
        {
            var rect = UiKit.Rect("Action", _root);
            rect.sizeDelta = new Vector2(150f, 150f);
            var disc = rect.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.color = item.Enabled ? new Color(0.13f, 0.52f, 0.86f, 1f) : new Color(0.25f, 0.32f, 0.38f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.interactable = item.Enabled;
            var click = item.Click;
            button.onClick.AddListener(() =>
            {
                Hide();
                click?.Invoke();
            });
            rect.gameObject.AddComponent<PressFeedback>();
            rect.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -5);

            var icon = Hud.HudIcons.Image(rect, item.Icon, 0.18f, 0.18f, 0.82f, 0.82f);
            icon.raycastTarget = false;
            if (!item.Enabled)
                icon.color = new Color(1f, 1f, 1f, 0.45f);
            var label = UiKit.Label("Label", rect, item.Label, 28, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            label.rectTransform.Place(-0.4f, -0.42f, 1.4f, 0f);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(2, -2);
            return rect;
        }

        private void LateUpdate()
        {
            if (IsOpen)
                Follow();
        }

        private void Follow()
        {
            var camera = _camera?.Invoke();
            if (camera == null || _target == null)
            {
                Hide();
                return;
            }

            var screen = camera.WorldToScreenPoint(_target());
            if (screen.z <= 0f)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
            var half = _root.rect.size / 2f;

            _ring.anchoredPosition = local;
            _ring.anchorMin = _ring.anchorMax = new Vector2(0.5f, 0.5f);

            // The bubble sits above the target and stays on screen.
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0.5f, 0.5f);
            var bx = Mathf.Clamp(local.x, -half.x + _bubble.sizeDelta.x / 2f + 16f, half.x - _bubble.sizeDelta.x / 2f - 16f);
            var by = Mathf.Min(local.y + 150f, half.y - _bubble.sizeDelta.y - 260f);
            _bubble.anchoredPosition = new Vector2(bx, by);

            // Round actions in an arc under the target.
            var n = _buttons.Count;
            const float radius = 230f;
            for (var i = 0; i < n; i++)
            {
                var t = n == 1 ? 0.5f : i / (float)(n - 1);
                var angle = Mathf.Lerp(-150f, -30f, t) * Mathf.Deg2Rad;
                if (n == 2)
                    angle = Mathf.Lerp(-125f, -55f, t) * Mathf.Deg2Rad;
                var b = _buttons[i];
                b.anchorMin = b.anchorMax = new Vector2(0.5f, 0.5f);
                var p = local + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.75f) * radius;
                p.x = Mathf.Clamp(p.x, -half.x + 90f, half.x - 90f);
                b.anchoredPosition = p;
            }
        }
    }
}
