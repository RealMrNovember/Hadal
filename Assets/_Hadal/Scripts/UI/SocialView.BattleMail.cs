using System.Linq;
using HADAL.Shared.Simulation;

namespace Hadal.UI
{
    /// <summary>
    /// The war log's way into the mailbox: a battle opens as its mail's report card (the game's one report screen,
    /// Last Asylum's), so the War Room does not draw a second kind of report while the mail is still in the box.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>Opens the battle's report mail; false when no mail carries it (expired, or the box is not loaded).</summary>
        public bool OpenBattleMail(string battleId, string reportKind)
        {
            if (string.IsNullOrEmpty(battleId))
                return false;
            var mails = Mails().Where(m => m.Report != null && m.Report.Id == battleId).ToList();
            var mail = mails.FirstOrDefault(m => m.Report.Kind == reportKind) ?? mails.FirstOrDefault();
            if (mail == null)
                return false;
            _afterReward = Page.None;
            _mailTab = MailRules.TabOf(mail.Kind);
            OpenMailItem(mail);
            return true;
        }
    }
}
