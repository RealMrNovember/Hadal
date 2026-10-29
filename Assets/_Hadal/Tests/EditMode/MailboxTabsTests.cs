using System.Linq;
using HADAL.Shared.Simulation;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>The Last Asylum style mailbox (docs/37): tabs, their names and the report card's texts in every language.</summary>
    public sealed class MailboxTabsTests
    {
        private static readonly string[] CardKeys =
        {
            "mail.readAll", "mail.emptyTab", "mail.head.won", "mail.head.lost", "mail.head.shielded", "mail.head.scout",
            "mail.head.scouted", "mail.report.you", "mail.report.place", "mail.report.overview", "mail.report.ourLosses",
            "mail.report.troops", "mail.report.start", "mail.report.lost", "mail.report.left", "mail.report.count",
            "mail.report.noTroops", "mail.report.scoutSide", "mail.report.scoutedSide", "mail.report.scoutedText",
            "mail.report.moved", "mail.report.gone", "mail.report.goThere"
        };

        [Test]
        public void Every_language_names_the_tabs_and_the_report_card()
        {
            foreach (var (code, _) in Loc.Languages)
            {
                var asset = Resources.Load<TextAsset>("Localization/" + code);
                Assert.IsNotNull(asset, code);
                var table = Loc.Parse(asset.text);
                foreach (var key in MailRules.Tabs.Select(t => "mail.tab." + t).Concat(CardKeys))
                    Assert.IsTrue(table.ContainsKey(key) && table[key].Length > 0, $"{code}: {key}");
                StringAssert.Contains("{0}", table["mail.report.scoutedText"], code);
                StringAssert.Contains("{1}", table["mail.report.ourLosses"], code);
            }
        }

        [Test]
        public void Mails_sort_into_the_tabs_by_kind()
        {
            Assert.AreEqual(MailRules.TabBattle, MailRules.TabOf("battle"));
            Assert.AreEqual(MailRules.TabAlliance, MailRules.TabOf("alliance"));
            Assert.AreEqual(MailRules.TabEvent, MailRules.TabOf("event"));
            Assert.AreEqual(MailRules.TabSystem, MailRules.TabOf("system"));
            Assert.AreEqual(MailRules.TabSystem, MailRules.TabOf("admin"));
            Assert.AreEqual(MailRules.TabPersonal, MailRules.TabOf("player"));
            Assert.AreEqual(MailRules.TabSystem, MailRules.TabOf(null));
        }
    }
}
