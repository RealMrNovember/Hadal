using System.Threading.Tasks;
using HADAL.Shared.Auth;
using Hadal.Core.Network;
using Hadal.Data.Config;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>Sign in with Google: picker failures stop before the server, every outcome has a message.</summary>
    public sealed class GoogleSignInTests
    {
        private string _language;
        private GameConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _language = Loc.Current;
            Loc.Use("tr");
            _config = ScriptableObject.CreateInstance<GameConfigSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Use(_language);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Web_client_id_is_configured_by_default()
        {
            StringAssert.EndsWith(".apps.googleusercontent.com", _config.GoogleWebClientId);
        }

        [Test]
        public void Editor_reports_google_as_unsupported()
        {
            var result = new GoogleSignIn().SignInAsync(_config.GoogleWebClientId);
            Assert.IsTrue(result.IsCompleted);
            Assert.AreEqual(GoogleSignIn.Unsupported, result.Result.Error);
            Assert.AreEqual(GoogleSignIn.NotConfigured, new GoogleSignIn().SignInAsync(" ").Result.Error);
        }

        [TestCase(GoogleSignIn.Cancelled)]
        [TestCase(GoogleSignIn.NoAccount)]
        [TestCase(GoogleSignIn.Failed)]
        public void Picker_failures_never_reach_the_server(string error)
        {
            // No auth service or token store: touching the network would throw.
            var account = new AccountService(_config, null, null, new FakeGoogle(new GoogleSignInResult(null, null, error)));

            var link = account.LinkGoogleAsync();
            var signIn = account.SignInWithGoogleAsync();

            Assert.IsTrue(link.IsCompleted && signIn.IsCompleted);
            Assert.AreEqual(error, link.Result.Error);
            Assert.AreEqual(error, signIn.Result.Error);
        }

        [TestCase(GoogleSignIn.Cancelled, "Google ile giriş iptal edildi.")]
        [TestCase(AccountErrors.ProviderLinkedElsewhere, "Bu Google hesabı başka bir HADAL hesabına bağlı. O hesaba geçmek için Google ile giriş yap.")]
        [TestCase(AccountErrors.ProviderNotConfigured, "Google ile giriş sunucuda henüz açılmadı.")]
        [TestCase(AccountResult<ProfileResponse>.NetworkError, "Sunucuya ulaşılamadı.")]
        public void Outcomes_read_in_turkish(string error, string expected)
        {
            Assert.AreEqual(expected, Loc.Error(error));
            Assert.AreEqual("Google hesabına bağlandı: x@gmail.com", Loc.F("account.google.linked.email", "x@gmail.com"));
        }

        [Test]
        public void Every_google_outcome_has_its_own_message()
        {
            foreach (var code in new[]
                     {
                         GoogleSignIn.Cancelled, GoogleSignIn.NoAccount, GoogleSignIn.Failed, GoogleSignIn.Unsupported, GoogleSignIn.NotConfigured,
                         AccountErrors.ProviderNotConfigured, AccountErrors.ProviderTokenInvalid, AccountErrors.ProviderLinkedElsewhere,
                         AccountErrors.ProviderNotLinked, AccountErrors.ProviderUnknown
                     })
                Assert.IsTrue(Loc.Has("error." + code), code);
        }

        private sealed class FakeGoogle : IGoogleSignIn
        {
            private readonly GoogleSignInResult _result;

            public FakeGoogle(GoogleSignInResult result) => _result = result;

            public bool IsSupported => true;

            public Task<GoogleSignInResult> SignInAsync(string serverClientId) => Task.FromResult(_result);
        }
    }
}
