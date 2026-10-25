using HADAL.Shared.Auth;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    public sealed class AccountUiTests
    {
        [TestCase("Kaptan Deniz", true)]
        [TestCase("Şükrü_Öz-1", true)]
        [TestCase("ab", false)]
        [TestCase("a<b>c", false)]
        [TestCase(" lead", false)]
        public void Client_uses_the_server_name_rules(string name, bool valid)
        {
            Assert.AreEqual(valid, AccountRules.IsValidName(name));
        }

        [Test]
        public void Transfer_codes_are_accepted_as_typed()
        {
            Assert.AreEqual("ABCDEFGHJKMN", AccountRules.NormaliseTransferCode(" abcd-efgh-jkmn "));
        }

        [Test]
        public void Every_string_exists_in_both_languages()
        {
            foreach (var turkish in new[] { true, false })
            {
                Loc.Turkish = turkish;
                foreach (var key in new[] { "title.tap", "account.protect", "account.unprotected", "error.InvalidCredentials" })
                    Assert.AreNotEqual(key, Loc.T(key), $"missing {key} (tr={turkish})");
            }

            Loc.Turkish = Application.systemLanguage == SystemLanguage.Turkish;
        }

        [Test]
        public void Unknown_error_codes_still_produce_a_message()
        {
            StringAssert.Contains("Http502", Loc.Error("Http502"));
        }

        [Test]
        public void Title_can_only_be_dismissed_once_the_world_is_loaded()
        {
            var title = TitleScreenView.Create(null);
            try
            {
                title.Dismiss();
                Assert.IsFalse(title.IsDismissed, "dismissed while still connecting");

                title.SetStatus(TitleScreenView.Status.Ready);
                title.SetStatus(TitleScreenView.Status.Reconnecting);
                Assert.AreEqual(TitleScreenView.Status.Ready, title.Current, "a reconnect after load must not re-block the title");
            }
            finally
            {
                Object.DestroyImmediate(title.gameObject);
            }
        }
    }
}
