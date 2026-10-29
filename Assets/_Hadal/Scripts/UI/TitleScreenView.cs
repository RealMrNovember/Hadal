using System.Collections;
using Hadal.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// First screen, laid out like the genre's openers (Last Asylum, docs/rehber 19 §6.1): the logo on a deep-sea
    /// backdrop with rising bubbles, a depth gauge that fills while the session connects and the base loads, the
    /// status under it. When the base is ready the logo swells and fades and the base opens underneath — on its own,
    /// unless an announcement is showing, then a tap. It sits over the already-rendering world, so no scene change.
    /// </summary>
    public sealed class TitleScreenView : MonoBehaviour
    {
        public enum Status
        {
            Connecting,
            Reconnecting,
            Maintenance,
            UpdateRequired,
            Banned,
            Ready
        }

        /// <summary>The trench's depth in metres: the gauge counts down to it while loading.</summary>
        private const float Depth = 10994f;

        private CanvasGroup _group;
        private RectTransform _logo;
        private Image _logoImage;
        private Text _status;
        private Text _tap;
        private Text _announcement;
        private Text _depth;
        private Image _gaugeFill;
        private RectTransform _bubbles;
        private string _detail = string.Empty;
        private Button _tapArea;
        private Status _current = Status.Connecting;
        private bool _dismissed;
        private float _progress;
        private float _readyAt = -1f;

        public bool IsDismissed => _dismissed;
        public Status Current => _current;

        private void OnEnable()
        {
            AudioDirector.TitleShowing = true;
            GameAudio.SetMusic(MusicTrack.Menu);
        }

        private void OnDisable() => AudioDirector.TitleShowing = false;

        public static TitleScreenView Create(Transform parent)
        {
            var canvas = UiKit.Canvas("TitleScreen", 100);
            canvas.transform.SetParent(parent, false);
            var view = canvas.gameObject.AddComponent<TitleScreenView>();
            view.Build(canvas.transform);
            return view;
        }

        /// <summary>The logo as a sprite (Resources/Branding), or null in an old build without it.</summary>
        public static Sprite LogoSprite()
        {
            var texture = Resources.Load<Texture2D>("Branding/hadal_logo");
            return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void Build(Transform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();

            // Deep water: near black at the bottom, a faint teal light from the surface above.
            var backdrop = UiKit.Rect("Backdrop", root).Place(0, 0, 1, 1);
            var shade = backdrop.gameObject.AddComponent<RawImage>();
            shade.texture = Gradient(new Color(0.08f, 0.26f, 0.33f), new Color(0.004f, 0.02f, 0.035f));
            _tapArea = backdrop.gameObject.AddComponent<Button>();
            _tapArea.transition = Selectable.Transition.None;
            _tapArea.onClick.AddListener(Dismiss);

            _bubbles = UiKit.Rect("Bubbles", root).Place(0, 0, 1, 1);
            for (var i = 0; i < 26; i++)
                Bubble(i);

            _logo = UiKit.Rect("Logo", root).Place(0.06f, 0.55f, 0.94f, 0.75f);
            _logoImage = _logo.gameObject.AddComponent<Image>();
            _logoImage.preserveAspect = true;
            _logoImage.raycastTarget = false;
            _logoImage.sprite = LogoSprite();
            if (_logoImage.sprite == null)
            {
                _logoImage.enabled = false;
                UiKit.Label("Title", _logo, "HADAL", 190, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
            }

            var tagline = UiKit.Label("Tagline", root, Loc.T("title.tagline"), 36, new Color(0.7f, 0.86f, 0.92f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Italic);
            tagline.rectTransform.Place(0.08f, 0.5f, 0.92f, 0.55f);

            _announcement = UiKit.Label("Announcement", root, string.Empty, 36, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Italic);
            _announcement.rectTransform.Place(0.08f, 0.32f, 0.92f, 0.44f);

            _tap = UiKit.Label("Tap", root, Loc.T("title.tap"), 50, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _tap.rectTransform.Place(0.1f, 0.24f, 0.9f, 0.3f);

            // The depth gauge: an amber bar in a dark rounded track, the depth reached at its right end.
            var track = UiKit.Box("Gauge", root, new Color(0f, 0f, 0f, 0.55f));
            track.rectTransform.Place(0.1f, 0.135f, 0.9f, 0.152f);
            track.raycastTarget = false;
            _gaugeFill = UiKit.Box("Fill", track.transform, new Color(1f, 0.7f, 0.24f, 1f));
            _gaugeFill.rectTransform.Place(0, 0, 0.02f, 1);
            _gaugeFill.raycastTarget = false;
            _depth = UiKit.Label("Depth", root, "0 m", 26, new Color(1f, 0.82f, 0.5f, 1f), TextAnchor.MiddleRight, FontStyle.Bold);
            _depth.rectTransform.Place(0.5f, 0.155f, 0.9f, 0.18f);

            _status = UiKit.Label("Status", root, Loc.T("title.connecting"), 30, UiKit.Muted, TextAnchor.MiddleCenter);
            _status.rectTransform.Place(0.08f, 0.08f, 0.92f, 0.13f);

            var version = UiKit.Label("Version", root, "v" + Application.version, 26, new Color(1, 1, 1, 0.3f), TextAnchor.LowerRight);
            version.rectTransform.Place(0.5f, 0.01f, 0.98f, 0.05f);

            SetStatus(Status.Connecting);
        }

        private static Texture2D Gradient(Color top, Color bottom)
        {
            var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (var y = 0; y < 64; y++)
                texture.SetPixel(0, y, Color.Lerp(bottom, top, Mathf.Pow(y / 63f, 1.6f)));
            texture.Apply(false, true);
            return texture;
        }

        private void Bubble(int index)
        {
            var bubble = UiKit.Rect("Bubble", _bubbles);
            var size = Random.Range(8f, 26f);
            bubble.sizeDelta = new Vector2(size, size);
            bubble.anchorMin = bubble.anchorMax = new Vector2(Random.value, Random.value);
            var image = bubble.gameObject.AddComponent<Image>();
            image.sprite = UiKit.CircleRing;
            image.color = new Color(0.6f, 0.9f, 1f, Random.Range(0.12f, 0.35f));
            image.raycastTarget = false;
        }

        /// <summary>Overseer announcement (version manifest); empty hides it.</summary>
        public void SetAnnouncement(string text) => _announcement.text = text ?? string.Empty;

        public string Announcement => _announcement.text;

        /// <param name="detail">Server-written text that replaces the stock line (maintenance message).</param>
        public void SetStatus(Status status, string detail = null)
        {
            // Once the world is loaded a later hiccup is the in-game HUD's business, not the title's.
            if (_current == Status.Ready && status == Status.Reconnecting)
                return;

            // A ban outranks everything that may still arrive from the connection loop.
            if (_current == Status.Banned)
                return;

            _current = status;
            _detail = detail ?? string.Empty;
            _status.text = _detail.Length > 0 ? _detail : status switch
            {
                Status.Reconnecting => Loc.T("title.reconnecting"),
                Status.Maintenance => Loc.T("title.maintenance"),
                Status.UpdateRequired => Loc.T("title.update"),
                Status.Banned => Loc.T("title.banned"),
                Status.Ready => string.Empty,
                _ => Loc.T("title.connecting")
            };
            _status.color = status == Status.Banned ? UiKit.Danger
                : status is Status.Maintenance or Status.UpdateRequired ? UiKit.Amber : UiKit.Muted;
            _tapArea.interactable = status == Status.Ready;
            if (status == Status.Ready && _readyAt < 0f)
                _readyAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (_dismissed)
                return;

            // The gauge eases towards 90% while connecting and runs out to the bottom once the base is ready.
            var target = _current == Status.Ready ? 1f : _current is Status.Connecting or Status.Reconnecting ? 0.9f : _progress;
            var speed = _current == Status.Ready ? 2.5f : 0.35f;
            _progress = Mathf.MoveTowards(_progress, target, speed * Time.unscaledDeltaTime * (target - _progress + 0.05f));
            _gaugeFill.rectTransform.Place(0, 0, Mathf.Max(0.02f, _progress), 1);
            _depth.text = $"{Mathf.RoundToInt(_progress * Depth):N0} m";

            // The logo breathes; bubbles rise and wrap round.
            var pulse = 1f + 0.015f * Mathf.Sin(Time.unscaledTime * 1.6f);
            _logo.localScale = new Vector3(pulse, pulse, 1f);
            foreach (RectTransform bubble in _bubbles)
            {
                var a = bubble.anchorMin;
                a.y += Time.unscaledDeltaTime * (0.02f + bubble.sizeDelta.x * 0.0015f);
                a.x += Mathf.Sin(Time.unscaledTime * 1.3f + bubble.GetInstanceID()) * 0.0004f;
                if (a.y > 1.05f)
                    a = new Vector2(Random.value, -0.05f);
                bubble.anchorMin = bubble.anchorMax = a;
            }

            // Ready: the tap prompt only when an announcement wants reading; otherwise the base opens by itself.
            var waitForTap = _current == Status.Ready && _announcement.text.Length > 0;
            _tap.enabled = waitForTap;
            if (waitForTap)
                _tap.color = new Color(UiKit.Text.r, UiKit.Text.g, UiKit.Text.b, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f));
            else if (_current == Status.Ready && _progress >= 0.999f && Time.unscaledTime - _readyAt > 0.6f)
                Dismiss();
        }

        public void Dismiss()
        {
            if (_dismissed || _current != Status.Ready)
                return;

            _dismissed = true;
            StartCoroutine(Reveal());
        }

        /// <summary>Last Asylum's hand-over: the logo swells and fades while the curtain lifts off the base.</summary>
        private IEnumerator Reveal()
        {
            _tapArea.interactable = false;
            Hadal.Core.State.CameraFocus.Settle();
            for (var t = 0f; t < 0.7f; t += Time.unscaledDeltaTime)
            {
                var k = t / 0.7f;
                var eased = 1f - (1f - k) * (1f - k);
                _logo.localScale = Vector3.one * (1f + 0.35f * eased);
                _group.alpha = 1f - eased;
                yield return null;
            }

            _group.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
