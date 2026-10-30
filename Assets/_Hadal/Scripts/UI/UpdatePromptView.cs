using System;
using System.Threading.Tasks;
using Hadal.Core.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// "A new version is available" dialog over everything else. Optional updates can be skipped
    /// ("Later", once per session); a required one (below the server's minimum) cannot.
    /// Update downloads the APK in the app and opens the Android installer (OTA, see AppUpdater).
    /// </summary>
    public sealed class UpdatePromptView : MonoBehaviour
    {
        private GameObject _root;
        private TMP_Text _message;
        private TMP_Text _status;
        private Button _update;
        private Button _later;
        private GameObject _cross;
        private string _url = string.Empty;
        private bool _busy;
        private bool _skipped;

        /// <summary>Replaced by tests; the real one downloads and installs.</summary>
        public Func<string, IProgress<float>, Task<UpdateOutcome>> Updater = AppUpdater.UpdateAsync;

        public bool IsShown => _root != null && _root.activeSelf;
        public bool IsRequired { get; private set; }
        public string Status => _status.text;

        public static UpdatePromptView Create(Transform parent)
        {
            var canvas = UiKit.Canvas("UpdatePrompt", 120);
            canvas.transform.SetParent(parent, false);
            var view = canvas.gameObject.AddComponent<UpdatePromptView>();
            view.Build(canvas.transform);
            return view;
        }

        /// <summary>
        /// Last Asylum's dialog over the blurred game: the navy frame titled HADAL, the message and the download status
        /// on the light body, Update over Later. A required update hides the cross and Later, and a tap outside does
        /// nothing.
        /// </summary>
        private void Build(Transform root)
        {
            var dim = UiFrames.Dim(root, () =>
            {
                if (!IsRequired)
                    Hide();
            });
            _root = dim.gameObject;

            var card = UiKit.Box("Card", dim, UiFrames.Navy);
            card.rectTransform.Place(0.05f, 0.32f, 0.95f, 0.68f);
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().From = 0.94f;
            UiFrames.Dialog(card.rectTransform, Hide).Title.text = "HADAL";
            _cross = card.transform.Find("Close").gameObject;

            var column = UiKit.Column("Column", card.transform, 16f);
            column.anchorMin = Vector2.zero;
            column.anchorMax = Vector2.one;
            column.offsetMin = new Vector2(40f, 36f);
            column.offsetMax = new Vector2(-40f, -130f);

            _message = UiKit.Label("Message", column, string.Empty, UiKit.TypeScale.Subtitle, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Row(160);
            _status = UiKit.Label("Status", column, string.Empty, UiKit.TypeScale.Body, UiFrames.InkSoft, TextAnchor.MiddleCenter).Row(60);
            _update = UiKit.Button("Update", column, Loc.T("update.button"), () => _ = RunUpdate(), UiKit.Ok, UiKit.TypeScale.Subtitle + 4).Row(112);
            _later = UiKit.Button("Later", column, Loc.T("update.later"), Hide, UiKit.Field, UiKit.TypeScale.Subtitle).Row(100);

            _root.SetActive(false);
        }

        public void ShowAvailable(string version, string url)
        {
            if (_skipped || IsRequired)
                return;

            Show(Loc.F("update.available", version), url, required: false);
        }

        public void ShowRequired(string url) => Show(Loc.T("update.required"), url, required: true);

        private void Show(string message, string url, bool required)
        {
            IsRequired = required;
            _url = url ?? string.Empty;
            _message.text = message;
            _status.text = string.Empty;
            _update.gameObject.SetActive(_url.Length > 0);
            _later.gameObject.SetActive(!required);
            _cross.SetActive(!required);
            _root.SetActive(true);
        }

        private void Hide()
        {
            _skipped = true;
            _root.SetActive(false);
        }

        public async Task RunUpdate()
        {
            if (_busy)
                return;

            _busy = true;
            _update.interactable = false;
            _status.color = UiFrames.InkSoft;
            // Synchronous on purpose: Progress<T> posts to the next frame and could overwrite the final status.
            var progress = new Immediate(p => _status.text = Loc.F("update.downloading", Mathf.RoundToInt(p * 100f)));
            var outcome = await Updater(_url, progress);

            _status.color = outcome == UpdateOutcome.Failed ? UiFrames.InkBad : UiFrames.InkSoft;
            _status.text = outcome switch
            {
                UpdateOutcome.Failed => Loc.T("update.failed"),
                UpdateOutcome.InstallerOpened => Loc.T("update.install"),
                _ => string.Empty
            };
            _update.interactable = true;
            _busy = false;
        }

        private sealed class Immediate : IProgress<float>
        {
            private readonly Action<float> _report;

            public Immediate(Action<float> report) => _report = report;

            public void Report(float value) => _report(value);
        }
    }
}
