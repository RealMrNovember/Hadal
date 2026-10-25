using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// First screen: logo, connection status, then "tap to start" once the base has loaded. It sits
    /// over the already-rendering world (dimmed), so tapping simply lifts the curtain — no scene
    /// change and no sign-up step: the guest session is created silently behind it.
    /// </summary>
    public sealed class TitleScreenView : MonoBehaviour
    {
        public enum Status
        {
            Connecting,
            Reconnecting,
            Maintenance,
            UpdateRequired,
            Ready
        }

        private CanvasGroup _group;
        private Text _status;
        private Text _tap;
        private Button _tapArea;
        private Status _current = Status.Connecting;
        private bool _dismissed;

        public bool IsDismissed => _dismissed;
        public Status Current => _current;

        public static TitleScreenView Create(Transform parent)
        {
            var canvas = UiKit.Canvas("TitleScreen", 100);
            canvas.transform.SetParent(parent, false);
            var view = canvas.gameObject.AddComponent<TitleScreenView>();
            view.Build(canvas.transform);
            return view;
        }

        private void Build(Transform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();

            var backdrop = UiKit.Box("Backdrop", root, new Color(0.004f, 0.02f, 0.035f, 0.82f));
            backdrop.rectTransform.Place(0, 0, 1, 1);
            _tapArea = backdrop.gameObject.AddComponent<Button>();
            _tapArea.transition = Selectable.Transition.None;
            _tapArea.onClick.AddListener(Dismiss);

            var title = UiKit.Label("Title", root, "HADAL", 190, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(0.05f, 0.58f, 0.95f, 0.72f);
            var glow = title.gameObject.AddComponent<Shadow>();
            glow.effectColor = new Color(0.1f, 0.8f, 1f, 0.35f);
            glow.effectDistance = new Vector2(0, -6);

            var tagline = UiKit.Label("Tagline", root, Loc.T("title.tagline"), 40, UiKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
            tagline.rectTransform.Place(0.08f, 0.53f, 0.92f, 0.58f);

            _tap = UiKit.Label("Tap", root, Loc.T("title.tap"), 54, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _tap.rectTransform.Place(0.1f, 0.24f, 0.9f, 0.30f);

            _status = UiKit.Label("Status", root, Loc.T("title.connecting"), 36, UiKit.Muted, TextAnchor.MiddleCenter);
            _status.rectTransform.Place(0.08f, 0.17f, 0.92f, 0.23f);

            var version = UiKit.Label("Version", root, "v" + Application.version, 28, new Color(1, 1, 1, 0.3f), TextAnchor.LowerRight);
            version.rectTransform.Place(0.5f, 0.01f, 0.98f, 0.05f);

            SetStatus(Status.Connecting);
        }

        public void SetStatus(Status status)
        {
            // Once the world is loaded a later hiccup is the in-game HUD's business, not the title's.
            if (_current == Status.Ready && status == Status.Reconnecting)
                return;

            _current = status;
            _status.text = status switch
            {
                Status.Reconnecting => Loc.T("title.reconnecting"),
                Status.Maintenance => Loc.T("title.maintenance"),
                Status.UpdateRequired => Loc.T("title.update"),
                Status.Ready => string.Empty,
                _ => Loc.T("title.connecting")
            };
            _status.color = status is Status.Maintenance or Status.UpdateRequired ? UiKit.Amber : UiKit.Muted;
            _tap.enabled = status == Status.Ready;
            _tapArea.interactable = status == Status.Ready;
        }

        private void Update()
        {
            if (_tap.enabled)
                _tap.color = new Color(UiKit.Text.r, UiKit.Text.g, UiKit.Text.b, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f));
        }

        public void Dismiss()
        {
            if (_dismissed || _current != Status.Ready)
                return;

            _dismissed = true;
            StartCoroutine(UiKit.Fade(_group, 0f, 0.6f));
        }
    }
}
