using System;
using System.Collections;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The game's one window frame (docs/41), after Last Asylum's: the game behind goes soft and dark, a navy frame
    /// with the title centred and a subtitle under it, a light body sized to its content (it scrolls only past
    /// ~60% of the screen), pale section bands, zebra rows, and a footer with a note and one or two buttons.
    /// It opens with a short scale-and-fade and closes the same way; content can be refilled without a re-open.
    /// </summary>
    public sealed class UiWindow : MonoBehaviour
    {
        public static readonly Color Ink = new(0.1f, 0.15f, 0.24f, 1f);
        public static readonly Color InkMuted = new(0.33f, 0.4f, 0.5f, 1f);
        public static readonly Color InkGood = new(0.13f, 0.56f, 0.22f, 1f);
        public static readonly Color InkBad = new(0.84f, 0.22f, 0.18f, 1f);

        private static readonly Color BandColor = new(0.68f, 0.76f, 0.86f, 1f);
        private static readonly Color ZebraA = new(0.87f, 0.9f, 0.95f, 1f);
        private static readonly Color ZebraB = new(0.82f, 0.86f, 0.92f, 1f);

        private const float OpenSeconds = 0.22f;
        private const float CloseSeconds = 0.14f;

        /// <summary>Rows go here (a top-down column).</summary>
        public RectTransform Body { get; private set; }

        private RectTransform _card;
        private CanvasGroup _cardGroup;
        private RawImage _backdrop;
        private RenderTexture _blur;
        private LayoutElement _viewport;
        private RectTransform _footer;
        private RectTransform _buttons;
        private TMP_Text _note;
        private TMP_Text _title;
        private TMP_Text _subtitle;
        private Action _onClosed;
        private float _t;
        private bool _shown;
        private bool _closing;
        private int _rows;

        public bool IsClosing => _closing;

        /// <summary>Opens a window on <paramref name="canvas"/>; <paramref name="onClosed"/> runs once it has gone.</summary>
        public static UiWindow Open(Transform canvas, string title, string subtitle, Action onClosed)
        {
            var root = UiKit.Rect("Window", canvas).Place(0, 0, 1, 1);
            var window = root.gameObject.AddComponent<UiWindow>();
            window._onClosed = onClosed;
            window.Build(title, subtitle);
            return window;
        }

        private void Build(string title, string subtitle)
        {
            // The soft game behind: a blurred copy of the screen, captured before the window shows (Backdrop below).
            _backdrop = UiKit.Rect("Backdrop", transform).Place(0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            _backdrop.color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            _backdrop.texture = Texture2D.whiteTexture;
            _backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _backdrop.canvasRenderer.SetAlpha(0f);

            // The card: navy frame, title block, then the light body with its footer.
            var frame = UiKit.Box("Card", transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            frame.sprite = UiKit.Rounded;
            frame.color = new Color(0.16f, 0.23f, 0.35f, 1f);
            frame.gameObject.AddComponent<Button>(); // taps on the card do not reach the backdrop
            _card = frame.rectTransform;
            _card.anchorMin = new Vector2(0, 0.5f);
            _card.anchorMax = new Vector2(1, 0.5f);
            _card.pivot = new Vector2(0.5f, 0.5f);
            _card.offsetMin = new Vector2(22, 0);
            _card.offsetMax = new Vector2(-22, 0);
            var cardLayout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(12, 12, 0, 12);
            cardLayout.childControlWidth = cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            frame.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _cardGroup = frame.gameObject.AddComponent<CanvasGroup>();
            _cardGroup.alpha = 0f;

            var head = UiKit.Rect("Head", _card).Row(150 / UiKit.Scale);
            _title = UiKit.Stroked(UiKit.Label("Title", head, title, UiKit.TypeScale.Display, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            _title.rectTransform.Place(0.14f, 0.38f, 0.86f, 0.98f);
            _subtitle = UiKit.Label("Subtitle", head, subtitle ?? string.Empty, UiKit.TypeScale.Subtitle, new Color(0.78f, 0.88f, 0.98f, 1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            _subtitle.rectTransform.Place(0.14f, 0.06f, 0.86f, 0.4f);
            CloseCross(head);

            var panel = UiKit.Rect("Panel", _card).gameObject.AddComponent<Image>();
            panel.sprite = Skin.Get("panel") ?? UiKit.Rounded;
            panel.type = Image.Type.Sliced;
            panel.color = Skin.Get("panel") != null ? Color.white : new Color(0.9f, 0.93f, 0.97f, 1f);
            var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(0, 0, 10, 0);
            panelLayout.childControlWidth = panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Body: a scroll view whose height follows its content up to a cap (Fit).
            var viewport = UiKit.Rect("Viewport", panel.transform);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _viewport = viewport.gameObject.AddComponent<LayoutElement>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.decelerationRate = 0.12f;
            Body = UiKit.Column("Body", viewport, 0f);
            Body.anchorMin = new Vector2(0, 1);
            Body.anchorMax = new Vector2(1, 1);
            Body.pivot = new Vector2(0.5f, 1);
            Body.offsetMin = Body.offsetMax = Vector2.zero;
            Body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bodyLayout = Body.GetComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(16, 16, 0, 8);
            scroll.viewport = viewport;
            scroll.content = Body;

            // Footer: an optional note (right-aligned, like the genre's "Original time") over the buttons.
            _footer = UiKit.Column("Footer", panel.transform, 6f);
            _footer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(28, 28, 6, 26);
            _note = UiKit.Label("Note", _footer, string.Empty, UiKit.TypeScale.Caption, InkMuted, TextAnchor.MiddleRight);
            _note.Row(UiKit.TypeScale.Caption * 1.7f);
            _buttons = UiKit.Rect("Buttons", _footer).Row(124 / UiKit.Scale);
            var row = _buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 26;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            row.childAlignment = TextAnchor.MiddleCenter;

            StartCoroutine(Backdrop());
        }

        /// <summary>A plain white cross in the frame's top right (no coloured disc: the genre's quiet close).</summary>
        private void CloseCross(Transform head)
        {
            var hit = UiKit.Box("Close", head, new Color(0, 0, 0, 0.001f));
            hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(1, 1);
            hit.rectTransform.pivot = new Vector2(1, 1);
            hit.rectTransform.sizeDelta = new Vector2(120, 120);
            hit.rectTransform.anchoredPosition = new Vector2(-4, -10);
            hit.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            hit.gameObject.AddComponent<PressFeedback>();
            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = UiKit.Box("Bar", hit.transform, Color.white);
                bar.sprite = UiKit.Rounded;
                bar.raycastTarget = false;
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bar.rectTransform.sizeDelta = new Vector2(62, 13);
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                var shadow = bar.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.02f, 0.05f, 0.1f, 0.7f);
                shadow.effectDistance = new Vector2(2, -3);
            }
        }

        public void SetTitle(string title, string subtitle)
        {
            _title.text = title;
            _subtitle.text = subtitle ?? string.Empty;
        }

        // --- Content ----------------------------------------------------------------------------

        /// <summary>Empties the body and footer for a refill (the window stays open, nothing re-animates).</summary>
        public void Clear()
        {
            for (var i = Body.childCount - 1; i >= 0; i--)
                Destroy(Body.GetChild(i).gameObject);
            for (var i = _buttons.childCount - 1; i >= 0; i--)
                Destroy(_buttons.GetChild(i).gameObject);
            _note.text = string.Empty;
            _rows = 0;
        }

        /// <summary>A pale full-width band with a centred heading; restarts the zebra.</summary>
        public void Section(string title)
        {
            var band = UiKit.Box("Band", Body, BandColor);
            band.sprite = null;
            band.Row(62);
            UiKit.Label("Text", band.transform, title, UiKit.TypeScale.Subtitle, Ink, TextAnchor.MiddleCenter).rectTransform.Place(0, 0, 1, 1);
            _rows = 0;
        }

        /// <summary>A zebra row: what on the left, the value right-aligned (rich text for a green "+delta").</summary>
        public void Row(string label, string value, Color? valueColor = null)
        {
            var row = Zebra(78);
            UiKit.Label("Label", row, label, UiKit.TypeScale.Body + 2, Ink, TextAnchor.MiddleLeft).rectTransform.Place(0.03f, 0, 0.55f, 1);
            UiKit.Label("Value", row, value, UiKit.TypeScale.Body + 2, valueColor ?? Ink, TextAnchor.MiddleRight, FontStyle.Bold)
                .rectTransform.Place(0.4f, 0, 0.97f, 1);
        }

        /// <summary>
        /// A requirement or cost line: an icon, the text (red while not met) and, when <paramref name="action"/> is given,
        /// a small green button that goes and fixes it ("Go", "Get").
        /// </summary>
        public void Need(string icon, string text, bool met, string actionLabel = null, Action action = null, bool enabled = true, Color? fill = null)
        {
            var row = Zebra(96);
            Hud.HudIcons.Image(row, icon, 0.02f, 0.1f, 0.13f, 0.9f);
            UiKit.Label("Text", row, text, UiKit.TypeScale.Body + 2, met ? Ink : InkBad, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.15f, 0, action != null ? 0.72f : 0.86f, 1);
            if (action != null)
            {
                var go = UiKit.Button("Go", row, actionLabel, () => action(), enabled ? fill ?? UiKit.Ok : UiKit.Panel, 30);
                go.interactable = enabled;
                ((RectTransform)go.transform).Place(0.74f, 0.14f, 0.98f, 0.86f);
            }
            else
            {
                UiKit.Label("Mark", row, met ? "✓" : "✗", 40, met ? InkGood : InkBad, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Place(0.86f, 0, 0.98f, 1);
            }
        }

        /// <summary>A line of plain text in the body (explanations, empty states).</summary>
        public void Paragraph(string text, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var label = UiKit.Label("Text", Body, text, UiKit.TypeScale.Body, color ?? InkMuted, anchor);
            label.Row(UiKit.TypeScale.Body * 2.4f);
        }

        private RectTransform Zebra(float height)
        {
            var row = UiKit.Box("Row", Body, _rows++ % 2 == 0 ? ZebraA : ZebraB);
            row.sprite = null;
            row.raycastTarget = false;
            row.Row(height);
            return row.rectTransform;
        }

        public void Note(string text) => _note.text = text ?? string.Empty;

        /// <summary>
        /// A footer button: the label with an optional second line (a time, a price). Disabled ones are grey and show
        /// why in the note above if the caller sets it.
        /// </summary>
        public Button AddButton(string label, string detail, Color fill, Action onClick, bool enabled = true)
        {
            var text = string.IsNullOrEmpty(detail) ? label : $"{label}\n<size={Mathf.RoundToInt(UiKit.TypeScale.Body * UiKit.Scale)}>{detail}</size>";
            var button = UiKit.Button("Action", _buttons, text, () => onClick?.Invoke(), enabled ? fill : UiKit.Panel, 38);
            button.interactable = enabled;
            return button;
        }

        /// <summary>Sizes the body to its content (capped at ~60% of the screen) once the rows are in.</summary>
        public void Fit()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Body);
            var canvasHeight = ((RectTransform)transform.parent).rect.height;
            var cap = Mathf.Max(400f, canvasHeight * 0.6f);
            _viewport.preferredHeight = Mathf.Min(LayoutUtility.GetPreferredHeight(Body), cap);
            _note.gameObject.SetActive(_note.text.Length > 0);
            _buttons.gameObject.SetActive(_buttons.childCount > 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_card);
        }

        // --- Open / close -----------------------------------------------------------------------

        public void Close()
        {
            if (_closing)
                return;
            _closing = true;
            _t = 1f;
        }

        /// <summary>
        /// Captures the screen at the end of the opening frame (the window is still invisible then), shrinks it to a
        /// sixteenth through two bilinear steps, and shows that soft copy under a dark tint: the genre's blurred game.
        /// </summary>
        private IEnumerator Backdrop()
        {
            yield return new WaitForEndOfFrame();
            try
            {
                var full = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
                ScreenCapture.CaptureScreenshotIntoRenderTexture(full);
                var quarter = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width / 4), Mathf.Max(1, Screen.height / 4), 0);
                Graphics.Blit(full, quarter);
                var eighth = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width / 8), Mathf.Max(1, Screen.height / 8), 0);
                Graphics.Blit(quarter, eighth);
                var sixteenth = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width / 16), Mathf.Max(1, Screen.height / 16), 0);
                Graphics.Blit(eighth, sixteenth);
                _blur = new RenderTexture(eighth.width, eighth.height, 0) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                Graphics.Blit(sixteenth, _blur); // back up a step: bilinear on both ways smooths the blocks
                RenderTexture.ReleaseTemporary(full);
                RenderTexture.ReleaseTemporary(quarter);
                RenderTexture.ReleaseTemporary(eighth);
                RenderTexture.ReleaseTemporary(sixteenth);

                _backdrop.texture = _blur;
                _backdrop.color = new Color(0.5f, 0.55f, 0.62f, 1f);
                // The captured image comes upside down where the GPU's texture origin is at the top (Vulkan, Metal, D3D).
                if (SystemInfo.graphicsUVStartsAtTop)
                    _backdrop.uvRect = new Rect(0, 1, 1, -1);
            }
            catch (Exception)
            {
                // No capture on this device: a plain dark dim does the job.
                _backdrop.texture = Texture2D.whiteTexture;
                _backdrop.color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            }

            _shown = true;
            GameAudio.Play(Sfx.PanelOpen);
        }

        private void Update()
        {
            if (!_shown)
                return;

            if (_closing)
            {
                _t -= Time.unscaledDeltaTime / CloseSeconds;
                if (_t <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }

                Apply(UiKit.EaseInOut(_t), _t);
                return;
            }

            if (_t >= 1f)
                return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / OpenSeconds);
            Apply(UiKit.EaseOut(_t), BackOut(_t));
        }

        private void Apply(float fade, float grow)
        {
            _backdrop.canvasRenderer.SetAlpha(fade);
            _cardGroup.alpha = fade;
            var s = Mathf.LerpUnclamped(0.92f, 1f, grow);
            _card.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Ease-out-back: overshoots a touch past 1 and settles (the card's arrival).</summary>
        private static float BackOut(float t)
        {
            const float c = 1.4f;
            var u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }

        private void OnDestroy()
        {
            if (_blur != null)
                _blur.Release();
            _onClosed?.Invoke();
        }
    }
}
