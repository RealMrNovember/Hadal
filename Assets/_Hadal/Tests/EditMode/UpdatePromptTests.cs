using System.Threading.Tasks;
using Hadal.Core.Network;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>OTA update card: optional updates can be skipped, required ones cannot.</summary>
    public sealed class UpdatePromptTests
    {
        private GameObject _root;
        private UpdatePromptView _view;
        private string _language;

        [SetUp]
        public void SetUp()
        {
            _language = Loc.Current;
            Loc.Use("tr");
            _root = new GameObject("UpdatePromptTests");
            _view = UpdatePromptView.Create(_root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Use(_language);
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Optional_update_can_be_skipped_for_the_session()
        {
            _view.ShowAvailable("1.2.0", "https://hadal.cicibyte.com/download/hadal.apk");
            Assert.IsTrue(_view.IsShown);
            Assert.IsTrue(Button("Later").activeSelf);

            Button("Later").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsFalse(_view.IsShown);

            _view.ShowAvailable("1.2.0", "https://hadal.cicibyte.com/download/hadal.apk");
            Assert.IsFalse(_view.IsShown, "skipped update came back in the same session");
        }

        [Test]
        public void Required_update_cannot_be_skipped()
        {
            _view.ShowRequired("https://hadal.cicibyte.com/download/hadal.apk");
            Assert.IsTrue(_view.IsShown);
            Assert.IsTrue(_view.IsRequired);
            Assert.IsFalse(Button("Later").activeSelf);
        }

        [TestCase(UpdateOutcome.Failed, "İndirme başarısız. Lütfen tekrar dene.")]
        [TestCase(UpdateOutcome.InstallerOpened, "Sonraki ekranda Yükle'ye dokun.")]
        public void Update_reports_what_happened(UpdateOutcome outcome, string expected)
        {
            string requested = null;
            _view.Updater = (url, progress) =>
            {
                requested = url;
                progress.Report(0.5f);
                return Task.FromResult(outcome);
            };
            _view.ShowRequired("https://example.org/h.apk");

            var task = _view.RunUpdate();
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual("https://example.org/h.apk", requested);
            Assert.AreEqual(expected, _view.Status);
        }

        private GameObject Button(string name) => _root.transform.Find("UpdatePrompt/Dim/Card/Column/" + name).gameObject;
    }
}
