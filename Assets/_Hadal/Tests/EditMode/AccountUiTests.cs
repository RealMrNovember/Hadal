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
        public void Every_language_has_every_english_string()
        {
            var english = Loc.Parse(Resources.Load<TextAsset>("Localization/en").text);
            Assert.Greater(english.Count, 150);
            Assert.AreEqual(18, Loc.Languages.Length);

            foreach (var (code, _) in Loc.Languages)
            {
                var asset = Resources.Load<TextAsset>("Localization/" + code);
                Assert.IsNotNull(asset, $"Localization/{code}.txt missing");
                var strings = Loc.Parse(asset.text);
                foreach (var pair in english)
                {
                    Assert.IsTrue(strings.TryGetValue(pair.Key, out var value) && value.Length > 0, $"{code}: missing {pair.Key}");
                    for (var n = 0; n < 3; n++)
                        if (pair.Value.Contains("{" + n + "}"))
                            StringAssert.Contains("{" + n + "}", value, $"{code}: {pair.Key} lost {{{n}}}");
                }
            }
        }

        [Test]
        public void Language_choice_follows_the_phone_and_falls_back_to_english()
        {
            Assert.AreEqual("tr", Loc.FromSystem(SystemLanguage.Turkish));
            Assert.AreEqual("zh-Hant", Loc.FromSystem(SystemLanguage.ChineseTraditional));
            Assert.AreEqual("en", Loc.FromSystem(SystemLanguage.Finnish));

            var previous = Loc.Current;
            try
            {
                Loc.Use("tr");
                Assert.AreEqual("Ayarlar", Loc.T("settings.title"));
                Loc.Use("xx");
                Assert.AreEqual("en", Loc.Current);
                Assert.AreEqual("Settings", Loc.T("settings.title"));
            }
            finally
            {
                Loc.Use(previous);
            }
        }

        [Test]
        public void Arabic_is_joined_and_laid_out_right_to_left()
        {
            // Seen joins into the lam-alef ligature, meem stands alone; the Latin run (with its placeholder)
            // keeps its order and sits on the left of an RTL line.
            var shaped = ArabicText.Shape("سلام HADAL {0}");
            Assert.AreEqual("HADAL {0} ﻡﻼﺳ", shaped);
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
