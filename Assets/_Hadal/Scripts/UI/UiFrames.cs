using System;
using System.Collections;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The game's window language, after Last Asylum (docs/41, references in docs/la_ref): two frames and the parts every
    /// screen is built from, so no screen draws its own chrome.
    ///
    ///   Page    full screen: a navy top bar with a big white back arrow and the title (bag, heroes, alliance, quests…)
    ///   Dialog  centred card over the blurred game: navy header, title on the left, a plain white cross (details,
    ///           boosts, settings…); <see cref="UiWindow"/> is the content-sized dialog built on the same look
    ///
    /// Parts: segmented <see cref="Tabs"/>, rarity <see cref="ItemTile"/>s in a <see cref="Grid"/>, alliance-style
    /// <see cref="Feature"/> buttons, <see cref="QuestRow"/>s with reward tiles and a Claim/Go button,
    /// <see cref="BonusRow"/>s, <see cref="Banner"/>s and table rows with a highlighted current row.
    /// </summary>
    public static partial class UiFrames
    {
        // Palette (sampled from the references).
        public static readonly Color Navy = new(0.15f, 0.21f, 0.32f, 1f);
        public static readonly Color NavyDeep = new(0.1f, 0.14f, 0.22f, 1f);
        public static readonly Color Body = new(0.8f, 0.85f, 0.91f, 1f);
        public static readonly Color Card = new(0.89f, 0.92f, 0.96f, 1f);
        public static readonly Color CardAlt = new(0.84f, 0.88f, 0.93f, 1f);
        public static readonly Color Band = new(0.68f, 0.76f, 0.86f, 1f);
        public static readonly Color Highlight = new(0.91f, 0.84f, 0.62f, 1f);
        public static readonly Color Ink = new(0.12f, 0.17f, 0.26f, 1f);
        public static readonly Color InkSoft = new(0.36f, 0.43f, 0.53f, 1f);
        public static readonly Color InkGood = new(0.13f, 0.56f, 0.22f, 1f);
        public static readonly Color InkBad = new(0.84f, 0.22f, 0.18f, 1f);

        public const float TopBar = 124f;

        /// <summary>What a frame hands back: where the title goes, an optional subtitle, and the body to fill.</summary>
        public sealed class Parts
        {
            public TMP_Text Title;
            public TMP_Text Subtitle;
            public RectTransform Bar;
            public RectTransform Body;
        }

        // --- Frames -----------------------------------------------------------------------------

        /// <summary>
        /// Full-screen page chrome on <paramref name="root"/> (which fills the screen): the navy bar with the back arrow
        /// and title on top, the light body under it. The bar's right side (<see cref="Parts.Bar"/>) can take resource
        /// chips or a help button.
        /// </summary>
        public static Parts Page(RectTransform root, Action back)
        {
            var bg = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            bg.sprite = null;
            bg.type = Image.Type.Simple;
            bg.color = Body;

            var bar = UiKit.Box("TopBar", root, Navy);
            bar.sprite = null;
            bar.rectTransform.anchorMin = new Vector2(0, 1);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.sizeDelta = new Vector2(0, TopBar);
            var shade = UiKit.Box("Shade", bar.transform, new Color(0, 0, 0, 0.25f));
            shade.sprite = null;
            shade.raycastTarget = false;
            shade.rectTransform.Place(0, 0, 1, 0);
            shade.rectTransform.offsetMax = new Vector2(0, 5);

            var arrow = UiKit.Box("Back", bar.transform, new Color(0, 0, 0, 0.001f));
            arrow.sprite = null;
            arrow.rectTransform.anchorMin = new Vector2(0, 0);
            arrow.rectTransform.anchorMax = new Vector2(0, 1);
            arrow.rectTransform.pivot = new Vector2(0, 0.5f);
            arrow.rectTransform.sizeDelta = new Vector2(TopBar * 1.05f, 0);
            arrow.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.PanelClose);
                back?.Invoke();
            });
            arrow.gameObject.AddComponent<PressFeedback>();
            Hud.HudIcons.Image(arrow.transform, "g_back", 0.2f, 0.2f, 0.82f, 0.8f);

            var title = UiKit.Label("Title", bar.transform, string.Empty, UiKit.TypeScale.Title, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0, 0);
            title.rectTransform.anchorMax = new Vector2(0.62f, 1);
            title.rectTransform.offsetMin = new Vector2(TopBar * 1.05f, 0);
            title.rectTransform.offsetMax = Vector2.zero;
            UiText.Outline(title, new Color(0.04f, 0.07f, 0.12f, 0.9f));

            var right = UiKit.Rect("Right", bar.transform);
            right.anchorMin = new Vector2(0.6f, 0);
            right.anchorMax = new Vector2(1, 1);
            right.offsetMin = right.offsetMax = Vector2.zero;

            var body = UiKit.Rect("Body", root);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = Vector2.zero;
            body.offsetMax = new Vector2(0, -TopBar);
            var subtitle = UiKit.Label("Subtitle", body, string.Empty, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0, 1);
            subtitle.rectTransform.anchorMax = new Vector2(1, 1);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1);
            subtitle.rectTransform.sizeDelta = new Vector2(-40, 52);
            subtitle.rectTransform.anchoredPosition = new Vector2(0, -4);
            return new Parts { Title = title, Subtitle = subtitle, Bar = right, Body = body };
        }

        /// <summary>
        /// Dialog chrome on <paramref name="card"/>: the navy frame with the title on the left and the white cross, the
        /// light body inset under it. Pair with <see cref="Dim"/> for the backdrop.
        /// </summary>
        public static Parts Dialog(RectTransform card, Action close, float header = 104f)
        {
            var frame = card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>();
            frame.sprite = UiKit.Rounded;
            frame.type = Image.Type.Sliced;
            frame.color = Navy;
            var ink = card.GetComponent<InkSurface>();
            if (ink != null)
                UnityEngine.Object.Destroy(ink);
            if (card.GetComponent<Button>() == null)
                card.gameObject.AddComponent<Button>(); // taps on the card do not reach the backdrop
            var shadow = card.GetComponent<Shadow>() ?? card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0.02f, 0.06f, 0.5f);
            shadow.effectDistance = new Vector2(0, -8);

            var title = UiKit.Label("Title", card, string.Empty, UiKit.TypeScale.Subtitle + 2, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0, 1);
            title.rectTransform.offsetMin = new Vector2(36, -header);
            title.rectTransform.offsetMax = new Vector2(-130, 0);
            UiText.Outline(title, new Color(0.04f, 0.07f, 0.12f, 0.9f));
            Cross(card, close);

            var body = UiKit.Box("Body", card, Body);
            body.sprite = UiKit.Rounded;
            body.raycastTarget = false;
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.offsetMin = new Vector2(12, 12);
            body.rectTransform.offsetMax = new Vector2(-12, -header);
            var subtitle = UiKit.Label("Subtitle", body.transform, string.Empty, UiKit.TypeScale.Caption + 2, InkSoft, TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0, 1);
            subtitle.rectTransform.anchorMax = new Vector2(1, 1);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1);
            subtitle.rectTransform.sizeDelta = new Vector2(-40, 50);
            return new Parts { Title = title, Subtitle = subtitle, Bar = null, Body = body.rectTransform };
        }

        /// <summary>The full-screen backdrop under a dialog: the game blurred and darkened; a tap on it closes.</summary>
        public static RectTransform Dim(Transform canvas, Action close)
        {
            var root = UiKit.Rect("Dim", canvas).Place(0, 0, 1, 1);
            var image = root.gameObject.AddComponent<RawImage>();
            image.color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            image.texture = Texture2D.whiteTexture;
            root.gameObject.AddComponent<Button>().onClick.AddListener(() => close?.Invoke());
            root.gameObject.AddComponent<BlurBackdrop>();
            return root;
        }

        /// <summary>A plain white cross in the top right corner (no coloured disc), 120 px to hit.</summary>
        public static RectTransform Cross(Transform parent, Action close)
        {
            var hit = UiKit.Box("Close", parent, new Color(0, 0, 0, 0.001f));
            hit.sprite = null;
            hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(1, 1);
            hit.rectTransform.pivot = new Vector2(1, 1);
            hit.rectTransform.sizeDelta = new Vector2(112, 104);
            hit.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.PanelClose);
                close?.Invoke();
            });
            hit.gameObject.AddComponent<PressFeedback>();
            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = UiKit.Box("Bar", hit.transform, Color.white);
                bar.sprite = UiKit.Rounded;
                bar.raycastTarget = false;
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bar.rectTransform.sizeDelta = new Vector2(56, 12);
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                var shadow = bar.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.02f, 0.05f, 0.1f, 0.7f);
                shadow.effectDistance = new Vector2(2, -3);
            }

            return hit.rectTransform;
        }

        // --- Parts ------------------------------------------------------------------------------

        /// <summary>
        /// Segmented tabs (the genre's light strip with the chosen tab raised and white). Returns the strip; give it a
        /// height with <c>Row</c> or <c>Place</c>.
        /// </summary>
        public static RectTransform Tabs(Transform parent, string[] labels, int active, Action<int> select, bool[] dots = null)
        {
            var strip = UiKit.Box("Tabs", parent, new Color(0.62f, 0.69f, 0.79f, 1f));
            strip.sprite = UiKit.Rounded;
            var row = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(6, 6, 6, 6);
            row.spacing = 4;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var on = i == active;
                var tab = UiKit.Box("Tab", strip.transform, on ? new Color(0.95f, 0.97f, 1f, 1f) : new Color(0, 0, 0, 0.001f));
                tab.sprite = UiKit.Rounded;
                if (on)
                {
                    var s = tab.gameObject.AddComponent<Shadow>();
                    s.effectColor = new Color(0.1f, 0.18f, 0.32f, 0.45f);
                    s.effectDistance = new Vector2(0, -3);
                }

                tab.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    select?.Invoke(index);
                });
                var label = UiKit.Label("Label", tab.transform, labels[i], UiKit.TypeScale.Body, on ? Ink : InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.Place(0, 0, 1, 1, 4);
                label.Fit(16, UiKit.TypeScale.Body);
                if (dots != null && i < dots.Length && dots[i])
                    Dot(tab.transform);
            }

            return strip.rectTransform;
        }

        /// <summary>
        /// A rarity-framed square (bag items, rewards, heroes' gear): the icon, a count in the bottom right and an optional
        /// tag in the top left (a level, "12h"). <paramref name="rarity"/> is common / rare / epic / legendary.
        /// </summary>
        public static RectTransform ItemTile(Transform parent, string icon, string rarity, string count, Action click = null, string tag = null,
            bool dot = false)
        {
            var root = UiKit.Rect("Item", parent);
            var frame = Skin.Slot(root, string.IsNullOrEmpty(rarity) ? "common" : rarity);
            frame.rectTransform.Place(0, 0, 1, 1);
            frame.raycastTarget = click != null;
            if (click != null)
            {
                root.gameObject.AddComponent<Button>().targetGraphic = frame;
                root.GetComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    click();
                });
                root.gameObject.AddComponent<PressFeedback>();
            }

            Hud.HudIcons.Image(root, icon, 0.12f, 0.12f, 0.88f, 0.88f);
            if (!string.IsNullOrEmpty(count))
            {
                var n = UiKit.Label("Count", root, count, UiKit.TypeScale.Caption + 2, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                n.rectTransform.Place(0.04f, 0.03f, 0.95f, 0.45f);
                UiText.Outline(n, new Color(0.03f, 0.05f, 0.1f, 0.95f));
            }

            if (!string.IsNullOrEmpty(tag))
            {
                var t = UiKit.Label("Tag", root, tag, UiKit.TypeScale.Caption, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                t.rectTransform.Place(0.07f, 0.55f, 0.95f, 0.95f);
                UiText.Outline(t, new Color(0.03f, 0.05f, 0.1f, 0.95f));
            }

            if (dot)
                Dot(root);
            return root;
        }

        /// <summary>A grid that keeps <paramref name="columns"/> square-ish cells across whatever width it gets.</summary>
        public static RectTransform Grid(Transform parent, int columns, float aspect = 1f, float spacing = 14f)
        {
            var rect = UiKit.Rect("Grid", parent);
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset(16, 16, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            var flex = rect.gameObject.AddComponent<FlexGrid>();
            flex.Aspect = aspect;
            rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        /// <summary>The alliance hub's tiles: an icon on the left, the name in a light pill, a red dot when something waits.</summary>
        public static RectTransform Feature(Transform parent, string icon, string label, Action click, bool dot = false)
        {
            var tile = UiKit.Box("Feature", parent, Card);
            tile.sprite = UiKit.Rounded;
            var s = tile.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0.1f, 0.16f, 0.28f, 0.35f);
            s.effectDistance = new Vector2(0, -4);
            tile.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                click?.Invoke();
            });
            tile.gameObject.AddComponent<PressFeedback>();
            Hud.HudIcons.Image(tile.transform, icon, 0.03f, 0.08f, 0.3f, 0.92f);
            var name = UiKit.Label("Label", tile.transform, label, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Place(0.3f, 0.05f, 0.97f, 0.95f);
            name.Fit(16, UiKit.TypeScale.Body);
            if (dot)
                Dot(tile.transform);
            return tile.rectTransform;
        }

        /// <summary>
        /// A task line (daily quests, achievements, event goals): title and progress, a row of reward tiles, and the
        /// green Claim or blue Go button on the right.
        /// </summary>
        public static RectTransform QuestRow(Transform parent, string title, (string Icon, string Rarity, string Count)[] rewards,
            string action, bool claim, Action click, bool enabled = true)
        {
            var row = UiKit.Box("Quest", parent, Card);
            row.sprite = UiKit.Rounded;
            row.raycastTarget = false;
            UiKit.Label("Title", row.transform, title, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.03f, 0.64f, 0.97f, 0.97f);
            var n = rewards?.Length ?? 0;
            for (var i = 0; i < n; i++)
            {
                var (icon, rarity, count) = rewards[i];
                var x = 0.03f + i * 0.125f;
                ItemTile(row.transform, icon, rarity, count).Place(x, 0.08f, x + 0.115f, 0.6f);
            }

            if (!string.IsNullOrEmpty(action))
            {
                var button = UiKit.Button("Action", row.transform, action, () => click?.Invoke(),
                    enabled ? claim ? UiKit.Ok : UiKit.Primary : UiKit.Panel, 32);
                button.interactable = enabled;
                ((RectTransform)button.transform).Place(0.7f, 0.14f, 0.97f, 0.58f);
            }

            return row.rectTransform;
        }

        /// <summary>A bonus line (the boost window's war and economy tabs): the name, the value right-aligned, a small "+".</summary>
        public static RectTransform BonusRow(Transform parent, string label, string value, Action more = null)
        {
            var row = UiKit.Box("Bonus", parent, Card);
            row.sprite = UiKit.Rounded;
            UiKit.Label("Label", row.transform, label, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0, 0.66f, 1);
            UiKit.Label("Value", row.transform, value, UiKit.TypeScale.Body, Ink, TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Place(0.6f, 0, 0.86f, 1);
            if (more != null)
            {
                var plus = UiKit.Box("More", row.transform, new Color(0.24f, 0.52f, 0.9f, 1f));
                plus.sprite = UiKit.Rounded;
                plus.rectTransform.Place(0.885f, 0.18f, 0.965f, 0.82f);
                plus.gameObject.AddComponent<Button>().onClick.AddListener(() => more());
                Hud.HudIcons.Image(plus.transform, "g_up", 0.18f, 0.15f, 0.82f, 0.85f);
            }

            return row.rectTransform;
        }

        /// <summary>A table row: cells across, zebra or the gold highlight for the current level.</summary>
        public static RectTransform TableRow(Transform parent, string[] cells, bool header = false, bool highlight = false, int index = 0)
        {
            var color = header ? Band : highlight ? Highlight : index % 2 == 0 ? Card : CardAlt;
            var row = UiKit.Box(header ? "Head" : "Row", parent, color);
            row.sprite = null;
            row.raycastTarget = false;
            var w = 1f / cells.Length;
            for (var i = 0; i < cells.Length; i++)
                UiKit.Label("Cell", row.transform, cells[i], header ? UiKit.TypeScale.Caption + 2 : UiKit.TypeScale.Body, header ? InkSoft : Ink, TextAnchor.MiddleCenter,
                    header ? FontStyle.Normal : FontStyle.Bold).rectTransform.Place(i * w, 0, (i + 1) * w, 1, 4);
            return row.rectTransform;
        }

        /// <summary>A light card with text (descriptions at the top of a dialog, empty states).</summary>
        public static RectTransform Note(Transform parent, string text, Color? color = null)
        {
            var box = UiKit.Box("Note", parent, Card);
            box.sprite = UiKit.Rounded;
            box.raycastTarget = false;
            UiKit.Label("Text", box.transform, text, UiKit.TypeScale.Body, color ?? InkSoft, TextAnchor.MiddleLeft).rectTransform.Place(0, 0, 1, 1, 22);
            return box.rectTransform;
        }

        /// <summary>A section heading band (pale blue, centred), as inside the genre's dialogs.</summary>
        public static RectTransform Section(Transform parent, string title)
        {
            var band = UiKit.Box("Band", parent, Band);
            band.sprite = null;
            band.raycastTarget = false;
            UiKit.Label("Text", band.transform, title, UiKit.TypeScale.Body + 2, Ink, TextAnchor.MiddleCenter).rectTransform.Place(0, 0, 1, 1);
            return band.rectTransform;
        }

        /// <summary>The picture block of a dialog (the shielded base, a hero, a pack): art, its name and a line under it.</summary>
        public static RectTransform Banner(Transform parent, Sprite art, string title, string text)
        {
            var root = UiKit.Rect("Banner", parent);
            if (art != null)
            {
                var image = UiKit.Box("Art", root, Color.white);
                image.sprite = art;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.rectTransform.Place(0.2f, 0.3f, 0.8f, 1f);
            }

            var name = UiKit.Label("Title", root, title, UiKit.TypeScale.Title, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Place(0, 0.2f, 1, 0.36f);
            UiText.Outline(name, new Color(0.04f, 0.07f, 0.12f, 0.9f));
            UiKit.Label("Text", root, text, UiKit.TypeScale.Body, InkSoft, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0, 0.96f, 0.2f);
            return root;
        }

        /// <summary>The red notice dot in a tile's top right corner.</summary>
        public static GameObject Dot(Transform parent)
        {
            var dot = UiKit.Box("Dot", parent, new Color(0.95f, 0.18f, 0.15f, 1f));
            dot.sprite = Skin.Get("hud_badge") ?? UiKit.Circle;
            if (Skin.Get("hud_badge") != null)
                dot.color = Color.white;
            dot.type = Image.Type.Simple;
            dot.raycastTarget = false;
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(1, 1);
            dot.rectTransform.sizeDelta = new Vector2(30, 30);
            dot.rectTransform.anchoredPosition = new Vector2(-8, -8);
            return dot.gameObject;
        }
    }

    /// <summary>Keeps a GridLayoutGroup at a fixed column count by sizing its cells from the width it is given.</summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class FlexGrid : MonoBehaviour
    {
        public float Aspect = 1f;
        private GridLayoutGroup _grid;
        private float _width = -1f;

        private void Awake() => _grid = GetComponent<GridLayoutGroup>();

        private void OnRectTransformDimensionsChange() => Apply();

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_grid == null)
                _grid = GetComponent<GridLayoutGroup>();
            var width = ((RectTransform)transform).rect.width;
            if (width <= 0f || Mathf.Approximately(width, _width))
                return;
            _width = width;
            var columns = Mathf.Max(1, _grid.constraintCount);
            var cell = (width - _grid.padding.left - _grid.padding.right - _grid.spacing.x * (columns - 1)) / columns;
            _grid.cellSize = new Vector2(cell, cell / Mathf.Max(0.1f, Aspect));
        }
    }

    /// <summary>
    /// The genre's soft background: at the end of the frame it is created in, the screen (without it, still clear) is
    /// captured, shrunk to a sixteenth in bilinear steps and shown darkened under the dialog.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class BlurBackdrop : MonoBehaviour
    {
        private RawImage _image;
        private RenderTexture _blur;
        private float _t;
        private bool _ready;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _image.canvasRenderer.SetAlpha(0f);
        }

        // Re-captured each time it shows (dialogs that are hidden and shown again blur what is behind them now).
        private void OnEnable()
        {
            _ready = false;
            _t = 0f;
            _image.canvasRenderer.SetAlpha(0f);
            StartCoroutine(CaptureAtEndOfFrame());
        }

        private IEnumerator CaptureAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            if (_blur != null)
                _blur.Release();
            try
            {
                _blur = Capture();
                _image.texture = _blur;
                _image.color = new Color(0.5f, 0.55f, 0.62f, 1f);
                if (SystemInfo.graphicsUVStartsAtTop)
                    _image.uvRect = new Rect(0, 1, 1, -1);
            }
            catch (Exception)
            {
                _image.texture = Texture2D.whiteTexture;
                _image.color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            }

            _ready = true;
        }

        /// <summary>The blurred copy of the current screen (caller owns it).</summary>
        public static RenderTexture Capture()
        {
            var full = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(full);
            var quarter = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width / 4), Mathf.Max(1, Screen.height / 4), 0);
            Graphics.Blit(full, quarter);
            var eighth = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width / 8), Mathf.Max(1, Screen.height / 8), 0);
            Graphics.Blit(quarter, eighth);
            var sixteenth = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width / 16), Mathf.Max(1, Screen.height / 16), 0);
            Graphics.Blit(eighth, sixteenth);
            var blur = new RenderTexture(eighth.width, eighth.height, 0) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Graphics.Blit(sixteenth, blur);
            RenderTexture.ReleaseTemporary(full);
            RenderTexture.ReleaseTemporary(quarter);
            RenderTexture.ReleaseTemporary(eighth);
            RenderTexture.ReleaseTemporary(sixteenth);
            return blur;
        }

        private void Update()
        {
            if (!_ready || _t >= 1f)
                return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / 0.2f);
            _image.canvasRenderer.SetAlpha(UiKit.EaseOut(_t));
        }

        private void OnDestroy()
        {
            if (_blur != null)
                _blur.Release();
        }
    }
}
