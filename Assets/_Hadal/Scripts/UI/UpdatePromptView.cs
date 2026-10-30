using System;
using System.Threading.Tasks;
using Hadal.Core.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// "A new version is available" card over everything else. Optional updates can be skipped
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

        private void Build(Transform root)
        {
            var dim = UiKit.Box("Dim", root, new Color(0f, 0.01f, 0.02f, 0.75f));
            dim.rectTransform.Place(0, 0, 1, 1);
            _root = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.07f, 0.32f, 0.93f, 0.68f);
            var column = UiKit.Column("Column", card.transform, 20f);
            column.Place(0.07f, 0.06f, 0.93f, 0.94f);

            UiKit.Label("Title", column, "HADAL", 64, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).Row(90);
            _message = UiKit.Label("Message", column, string.Empty, 38, UiKit.Text, TextAnchor.MiddleCenter).Row(150);
            _status = UiKit.Label("Status", column, string.Empty, 32, UiKit.Muted, TextAnchor.MiddleCenter).Row(60);
            _update = UiKit.Button("Update", column, Loc.T("update.button"), () => _ = RunUpdate()).Row(110);
            _later = UiKit.Button("Later", column, Loc.T("update.later"), Hide, UiKit.Field).Row(100);
            _later.GetComponentInChildren<TMP_Text>().color = UiKit.Text;

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
            _status.color = UiKit.Muted;
            // Synchronous on purpose: Progress<T> posts to the next frame and could overwrite the final status.
            var progress = new Immediate(p => _status.text = Loc.F("update.downloading", Mathf.RoundToInt(p * 100f)));
            var outcome = await Updater(_url, progress);

            _status.color = outcome == UpdateOutcome.Failed ? UiKit.Danger : UiKit.Muted;
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
