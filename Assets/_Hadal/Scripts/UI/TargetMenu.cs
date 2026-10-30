using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The genre's "tap something in the world" menu (Last Asylum, docs/41 §5): the HUD steps aside, a light info card
    /// sits above the target (a header strip with the name and small tab buttons, a portrait beside the numbers, and
    /// under a dashed line the social actions), and round blue action buttons stand on a dark crescent under it.
    /// It follows the target while the camera glides; tapping anywhere else closes it. One menu at a time, shared by
    /// the base and the world map.
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

        /// <summary>A player's card: portrait and numbers, header tabs (share, bookmark), social actions under a line.</summary>
        public sealed class Card
        {
            public string Title = string.Empty;
            public string AvatarId = string.Empty;
            public string AvatarName = string.Empty;
            public string AvatarSeed = string.Empty;

            /// <summary>The "!" left of the name and a tap on the portrait: the player's profile.</summary>
            public Action Profile;

            public readonly List<(string Label, string Value)> Stats = new();
            public readonly List<(string Icon, Action Click)> Tabs = new();
            public readonly List<Item> Social = new();

            /// <summary>One line under the numbers (why an attack is closed, the shield's time), rich text.</summary>
            public string Note = string.Empty;
        }

        private static readonly Color CardBody = new(0.9f, 0.93f, 0.97f, 0.98f);
        private static readonly Color CardHead = new(0.78f, 0.84f, 0.91f, 1f);
        private static readonly Color Ink = new(0.12f, 0.17f, 0.26f, 1f);
        private static readonly Color InkSoft = new(0.36f, 0.43f, 0.53f, 1f);

        private const float CardWidth = 660f;
        private const float ArcWidth = 600f;
        private const float ButtonSize = 128f;

        private static TargetMenu _instance;

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _arc;
        private RectTransform _card;
        private readonly List<RectTransform> _buttons = new();

        private Func<Camera> _camera;
        private Func<Vector3> _target;
        private Action _closed;
        private float _openedAt;

        public static bool IsOpen => _instance != null && _instance._root.gameObject.activeSelf;

        /// <summary>The plain form (buildings, nodes, monsters): a title and a few lines, optionally a progress bar.</summary>
        public static void Show(Func<Camera> camera, Func<Vector3> target, string title, string info, IReadOnlyList<Item> items,
            float bar = -1f, string barText = "", Action closed = null)
        {
            var menu = Instance();
            menu.Open(camera, target, items, closed);
            menu.BuildPlainCard(title, info, bar, barText);
        }

        /// <summary>A player's base: the full card (Last Asylum's).</summary>
        public static void Show(Func<Camera> camera, Func<Vector3> target, Card card, IReadOnlyList<Item> items, Action closed = null)
        {
            var menu = Instance();
            menu.Open(camera, target, items, closed);
            menu.BuildPlayerCard(card);
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

            // The dark crescent the action buttons stand on, its flat edge through the target.
            var arc = UiKit.Rect("Arc", _root).gameObject.AddComponent<Image>();
            arc.sprite = Skin.Get("arc");
            arc.color = arc.sprite != null ? Color.white : new Color(0, 0, 0, 0.35f);
            arc.raycastTarget = false;
            _arc = arc.rectTransform;
            _arc.anchorMin = _arc.anchorMax = new Vector2(0.5f, 0.5f);
            _arc.pivot = new Vector2(0.5f, 1f);
            _arc.sizeDelta = new Vector2(ArcWidth, ArcWidth / 2f);

            _root.gameObject.SetActive(false);
        }

        private void Open(Func<Camera> camera, Func<Vector3> target, IReadOnlyList<Item> items, Action closed)
        {
            var previous = _closed;
            _closed = null;
            if (IsOpen && previous != null)
                previous();

            _camera = camera;
            _target = target;
            _closed = closed;
            _openedAt = Time.unscaledTime;

            if (_card != null)
                Destroy(_card.gameObject);
            foreach (var b in _buttons)
                Destroy(b.gameObject);
            _buttons.Clear();
            foreach (var item in items)
                _buttons.Add(ActionButton(item));
            _arc.gameObject.SetActive(items.Count > 0);

            _root.gameObject.SetActive(true);
        }

        // --- Cards ------------------------------------------------------------------------------

        private RectTransform NewCard(out RectTransform column)
        {
            var card = UiKit.Box("Card", _root, CardBody);
            card.sprite = UiKit.Rounded;
            card.gameObject.AddComponent<Button>(); // taps on the card do not close the menu
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0.03f, 0.08f, 0.45f);
            shadow.effectDistance = new Vector2(0, -6);
            _card = card.rectTransform;
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
            _card.pivot = new Vector2(0.5f, 0f);
            _card.sizeDelta = new Vector2(CardWidth, 200f);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 14);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            column = _card;
            return _card;
        }

        /// <summary>The header strip: an optional "!" on the left, the name, and small blue tabs on the right.</summary>
        private void Header(RectTransform card, string title, Action profile, IReadOnlyList<(string Icon, Action Click)> tabs)
        {
            var head = UiKit.Box("Head", card, CardHead);
            head.sprite = UiKit.Rounded;
            head.raycastTarget = false;
            Fixed(head.rectTransform, 86f);

            var left = 0.03f;
            if (profile != null)
            {
                var info = Tap(head.rectTransform, "Info", profile);
                info.Place(0.02f, 0.14f, 0.11f, 0.86f);
                var bg = info.GetComponent<Image>();
                bg.sprite = UiKit.Rounded;
                bg.color = new Color(0.36f, 0.45f, 0.58f, 1f);
                Hud.HudIcons.Image(info, "g_info", 0.15f, 0.12f, 0.85f, 0.88f);
                left = 0.13f;
            }

            var right = 0.97f - tabs.Count * 0.115f;
            var name = UiKit.Label("Title", head.transform, title, UiKit.TypeScale.Subtitle, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.Place(left, 0, right, 1);
            name.textWrappingMode = TextWrappingModes.NoWrap;

            for (var i = 0; i < tabs.Count; i++)
            {
                var (icon, click) = tabs[i];
                var x1 = 0.98f - i * 0.115f;
                var tab = Tap(head.rectTransform, "Tab", click);
                tab.Place(x1 - 0.1f, -0.22f, x1, 1.06f);
                var image = tab.GetComponent<Image>();
                image.sprite = Skin.Get("card_tab") ?? UiKit.Rounded;
                image.color = Skin.Get("card_tab") != null ? Color.white : new Color(0.2f, 0.45f, 0.85f, 1f);
                image.preserveAspect = true;
                Hud.HudIcons.Image(tab, icon, 0.18f, 0.3f, 0.82f, 0.86f);
            }
        }

        private void BuildPlayerCard(Card data)
        {
            var card = NewCard(out _);
            Header(card, data.Title, data.Profile, data.Tabs);

            // Portrait on the left, the numbers right-aligned beside it.
            var body = UiKit.Rect("Body", card);
            var rows = Math.Max(3, data.Stats.Count);
            Fixed(body, 30f + rows * 50f);
            var frame = Tap(body, "Portrait", data.Profile);
            frame.Place(0.035f, 0.1f, 0.235f, 0.92f);
            var frameImage = frame.GetComponent<Image>();
            frameImage.sprite = UiKit.Rounded;
            frameImage.color = new Color(0.22f, 0.3f, 0.42f, 1f);
            var face = UiKit.Rect("Face", frame).Place(0.06f, 0.06f, 0.94f, 0.94f);
            if (!string.IsNullOrEmpty(data.AvatarId) && Hadal.Core.Catalog.CharacterLibrary.Instance?.Portrait(data.AvatarId) != null)
                AvatarDisc.Draw(face, data.AvatarId, data.AvatarName, data.AvatarSeed);
            else
                Hud.HudIcons.Image(face, "avatar", 0, 0, 1, 1);

            for (var i = 0; i < data.Stats.Count; i++)
            {
                var (label, value) = data.Stats[i];
                var y1 = 0.9f - i * (0.8f / rows);
                var y0 = y1 - 0.8f / rows;
                UiKit.Label("Label", body, label, UiKit.TypeScale.Body, InkSoft, TextAnchor.MiddleLeft).rectTransform.Place(0.27f, y0, 0.62f, y1);
                UiKit.Label("Value", body, value, UiKit.TypeScale.Body + 2, Ink, TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Place(0.5f, y0, 0.965f, y1);
            }

            if (!string.IsNullOrEmpty(data.Note))
            {
                var note = UiKit.Label("Note", card, data.Note, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleCenter);
                Fixed(note.rectTransform, 44f);
            }

            if (data.Social.Count == 0)
                return;

            // A dashed line, then the social actions as small round buttons with their names.
            Dashes(card);
            var social = UiKit.Rect("Social", card);
            Fixed(social, 150f);
            var n = data.Social.Count;
            for (var i = 0; i < n; i++)
            {
                var item = data.Social[i];
                var cx = (i + 0.5f) / n;
                var b = SmallAction(social, item);
                b.anchorMin = b.anchorMax = new Vector2(cx, 0.62f);
                b.sizeDelta = new Vector2(92f, 92f);
            }
        }

        private void BuildPlainCard(string title, string info, float bar, string barText)
        {
            var card = NewCard(out _);
            Header(card, title, null, Array.Empty<(string, Action)>());
            if (!string.IsNullOrEmpty(info))
            {
                var lines = info.Split('\n').Length;
                var holder = UiKit.Rect("InfoRow", card);
                Fixed(holder, 22f + lines * 40f);
                UiKit.Label("Info", holder, info, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft).rectTransform.Place(0.045f, 0, 0.96f, 1);
            }

            if (bar >= 0f)
            {
                var holder = UiKit.Rect("BarRow", card);
                Fixed(holder, 46f);
                var track = UiKit.Box("Bar", holder, new Color(0.12f, 0.18f, 0.27f, 0.9f));
                track.sprite = UiKit.Rounded;
                track.rectTransform.Place(0.04f, 0.1f, 0.96f, 0.9f);
                var fill = UiKit.Box("Fill", track.transform, new Color(0.3f, 0.78f, 0.4f, 1f));
                fill.sprite = UiKit.Rounded;
                fill.rectTransform.Place(0, 0, Mathf.Clamp(bar, 0.02f, 1f), 1);
                UiKit.Stroked(UiKit.Label("Text", track.transform, barText, UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold))
                    .rectTransform.Place(0, 0, 1, 1);
            }
        }

        private static void Dashes(RectTransform card)
        {
            var row = UiKit.Rect("Dashes", card);
            Fixed(row, 18f);
            const int count = 34;
            for (var i = 0; i < count; i++)
            {
                var dash = UiKit.Box("Dash", row, new Color(0.3f, 0.37f, 0.47f, 0.8f));
                dash.sprite = null;
                dash.raycastTarget = false;
                var x = 0.04f + i * (0.92f / count);
                dash.rectTransform.Place(x, 0.42f, x + 0.92f / count * 0.6f, 0.58f);
            }
        }

        private static void Fixed(RectTransform rect, float height)
        {
            var element = rect.gameObject.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
        }

        private static RectTransform Tap(RectTransform parent, string name, Action click)
        {
            var image = UiKit.Box(name, parent, new Color(0, 0, 0, 0.001f));
            image.sprite = null;
            if (click != null)
            {
                image.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    Hide();
                    click();
                });
                image.gameObject.AddComponent<PressFeedback>();
            }

            return image.rectTransform;
        }

        // --- Buttons ----------------------------------------------------------------------------

        /// <summary>A round blue action with a white glyph and its name under it, outlined (the arc's buttons).</summary>
        private RectTransform ActionButton(Item item)
        {
            var rect = Disc(_root, item, ButtonSize);
            var label = UiKit.Label("Label", rect, item.Label, UiKit.TypeScale.Body - 2, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            label.rectTransform.Place(-0.5f, -0.36f, 1.5f, 0.02f);
            UiText.Outline(label, new Color(0.02f, 0.05f, 0.1f, 0.9f));
            return rect;
        }

        /// <summary>A smaller one for the card's social row, its name in ink under it.</summary>
        private static RectTransform SmallAction(RectTransform parent, Item item)
        {
            var rect = Disc(parent, item, 92f);
            UiKit.Label("Label", rect, item.Label, UiKit.TypeScale.Caption + 2, Ink, TextAnchor.UpperCenter, FontStyle.Bold)
                .rectTransform.Place(-0.6f, -0.5f, 1.6f, -0.02f);
            return rect;
        }

        private static RectTransform Disc(RectTransform parent, Item item, float size)
        {
            var rect = UiKit.Rect("Action", parent);
            rect.sizeDelta = new Vector2(size, size);
            var disc = rect.gameObject.AddComponent<Image>();
            disc.sprite = Skin.Get("act_disc") ?? UiKit.Circle;
            disc.color = Skin.Get("act_disc") != null
                ? item.Enabled ? Color.white : new Color(0.55f, 0.6f, 0.66f, 1f)
                : item.Enabled ? new Color(0.13f, 0.52f, 0.86f, 1f) : new Color(0.25f, 0.32f, 0.38f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.interactable = item.Enabled;
            var click = item.Click;
            button.onClick.AddListener(() =>
            {
                Hide();
                click?.Invoke();
            });
            rect.gameObject.AddComponent<PressFeedback>();

            var icon = Hud.HudIcons.Image(rect, item.Icon, 0.2f, 0.22f, 0.8f, 0.82f);
            icon.raycastTarget = false;
            if (!item.Enabled)
                icon.color = new Color(1f, 1f, 1f, 0.55f);
            return rect;
        }

        // --- Following --------------------------------------------------------------------------

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

            // The crescent hangs from the target; the buttons stand along its middle.
            var center = local + new Vector2(0f, -30f);
            _arc.anchoredPosition = center;

            var n = _buttons.Count;
            var radius = ArcWidth * 0.39f;
            for (var i = 0; i < n; i++)
            {
                // Spread across the lower half: wide for many buttons, close together for one or two.
                var spread = n switch { 1 => 0f, 2 => 60f, 3 => 110f, _ => 140f };
                var t = n == 1 ? 0.5f : i / (float)(n - 1);
                var angle = (270f - spread / 2f + spread * t) * Mathf.Deg2Rad;
                var p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                p.x = Mathf.Clamp(p.x, -half.x + 80f, half.x - 80f);
                var b = _buttons[i];
                b.anchorMin = b.anchorMax = new Vector2(0.5f, 0.5f);
                b.anchoredPosition = p;
            }

            // The card above the target, kept on screen.
            if (_card != null)
            {
                var height = _card.rect.height;
                var x = Mathf.Clamp(local.x, -half.x + CardWidth / 2f + 16f, half.x - CardWidth / 2f - 16f);
                var y = Mathf.Min(local.y + 190f, half.y - height - 40f);
                _card.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
