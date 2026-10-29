using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The mailbox (docs/29 §2.1): system, Overseer, event and alliance mails, newest first, with an unread dot, the
    /// sender, title, age and attachment icons. Opening a mail shows its text and attachments with a claim button;
    /// "Claim all" and "Delete read" act on the whole box. Mails go after 30 days, unclaimed attachments with them.
    /// </summary>
    public sealed partial class SocialView
    {
        private string _mailId = string.Empty;

        public void OpenMail()
        {
            _afterReward = Page.None;
            _mailId = string.Empty;
            Open(Page.Mail);
        }

        private string MailKey() => $"{View.MailVersion}|{_mailId}";

        private MailDto[] Mails() => View.Mail?.Mails ?? Array.Empty<MailDto>();

        private static string MailSender(MailDto m) =>
            m.Sender.Length > 0 ? m.Sender : Loc.Has("mail.sender." + m.Kind) ? Loc.T("mail.sender." + m.Kind) : Loc.T("mail.sender.system");

        private static string MailIcon(MailDto m) => m.Kind switch
        {
            "event" => "event_gift",
            "alliance" => "side_alliance",
            _ => "side_mail"
        };

        private static bool Claimable(MailDto m) => m.Attachments.Length > 0 && !m.Claimed;

        /// <summary>"Just now", "12 min ago", "3 h ago", "2 d ago".</summary>
        private string MailAge(MailDto m)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, NowMs - m.SentAtMs));
            return t.TotalDays >= 1 ? Loc.F("mail.age.day", (int)t.TotalDays)
                : t.TotalHours >= 1 ? Loc.F("mail.age.hour", (int)t.TotalHours)
                : t.TotalMinutes >= 1 ? Loc.F("mail.age.min", (int)t.TotalMinutes)
                : Loc.T("mail.age.now");
        }

        private void OpenMailItem(MailDto m)
        {
            _mailId = m.Id;
            if (!m.Read)
                _commands.Dispatch(new ReadMailCommand { CommandId = NewId(), ClientSequence = NextSequence(), MailId = m.Id });
            Open(Page.MailDetail);
        }

        private void ClaimMail(string mailId) =>
            _commands.Dispatch(new ClaimMailCommand { CommandId = NewId(), ClientSequence = NextSequence(), MailId = mailId });

        private void DeleteMail(string mailId) =>
            _commands.Dispatch(new DeleteMailCommand { CommandId = NewId(), ClientSequence = NextSequence(), MailId = mailId });

        // --- List ---------------------------------------------------------------------------

        private void RenderMail()
        {
            _title.text = Loc.T("mail.title");
            var box = View.Mail ?? new MailListDto();
            _subtitle.text = box.Unread > 0 ? Loc.F("mail.unread", box.Unread) : string.Empty;
            var mails = Mails();

            var bar = UiKit.Rect("MailBar", _list).Row(100);
            var claimAll = CardButton(bar, Loc.T("mail.claimAll"), box.Unclaimed > 0, () => ClaimMail(string.Empty), UiKit.Gold, 0.0f, 0.08f, 0.49f, 0.92f);
            if (box.Unclaimed > 0)
                RedDot(claimAll.transform);
            CardButton(bar, Loc.T("mail.deleteRead"), mails.Any(m => m.Read && !Claimable(m)), () => DeleteMail(string.Empty), UiKit.Field, 0.51f, 0.08f, 1f, 0.92f);

            if (mails.Length == 0)
            {
                Note(Loc.T("mail.empty"));
                return;
            }

            foreach (var m in mails)
            {
                var mail = m;
                var card = Card(170);
                if (!m.Read)
                    card.color = new Color(0.16f, 0.24f, 0.34f, 1f);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.onClick.AddListener(() => OpenMailItem(mail));

                HudIconImage(card.transform, MailIcon(m), 0.02f, 0.18f, 0.14f, 0.82f);
                if (!m.Read)
                {
                    // The unread dot sits on the icon's corner.
                    var dot = UiKit.Box("Unread", card.transform, new Color(0.95f, 0.18f, 0.15f, 1f));
                    dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.13f, 0.8f);
                    dot.rectTransform.sizeDelta = new Vector2(22, 22);
                    dot.raycastTarget = false;
                }

                CardText(card.transform, MailSender(m), 24, m.Read ? UiKit.Muted : UiKit.Accent, 0.17f, 0.62f, 0.7f, 0.94f, TextAnchor.MiddleLeft, FontStyle.Bold)
                    .raycastTarget = false;
                CardText(card.transform, MailAge(m), 22, UiKit.Muted, 0.7f, 0.62f, 0.97f, 0.94f, TextAnchor.MiddleRight).raycastTarget = false;
                CardText(card.transform, m.Title, 30, UiKit.Text, 0.17f, 0.3f, 0.97f, 0.64f, TextAnchor.MiddleLeft, m.Read ? FontStyle.Normal : FontStyle.Bold)
                    .raycastTarget = false;

                // Up to five attachment icons; a tick once taken.
                var shown = Math.Min(5, m.Attachments.Length);
                for (var i = 0; i < shown; i++)
                {
                    var x = 0.17f + i * 0.065f;
                    RewardIcon(card.transform, m.Attachments[i], x, 0.04f, x + 0.06f, 0.3f);
                }

                if (m.Attachments.Length > shown)
                    CardText(card.transform, "+" + (m.Attachments.Length - shown), 22, UiKit.Muted, 0.17f + shown * 0.065f, 0.04f, 0.6f, 0.3f, TextAnchor.MiddleLeft)
                        .raycastTarget = false;
                if (m.Attachments.Length > 0)
                    CardText(card.transform, m.Claimed ? Loc.T("mail.claimed") : Loc.T("mail.claim"), 22, m.Claimed ? UiKit.Muted : UiKit.Gold,
                        0.7f, 0.04f, 0.97f, 0.3f, TextAnchor.MiddleRight, FontStyle.Bold).raycastTarget = false;
            }

            Note(Loc.T("mail.note"));
        }

        // --- One mail -----------------------------------------------------------------------

        private void RenderMailDetail()
        {
            var m = Array.Find(Mails(), x => x.Id == _mailId);
            if (m == null)
            {
                // Deleted or expired meanwhile: back to the list.
                _page = Page.Mail;
                RenderMail();
                return;
            }

            _title.text = Loc.T("mail.title");
            _subtitle.text = MailSender(m);

            var head = Card(170);
            HudIconImage(head.transform, MailIcon(m), 0.02f, 0.15f, 0.14f, 0.85f);
            CardText(head.transform, m.Title, 34, UiKit.Text, 0.17f, 0.42f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            var daysLeft = (int)Math.Floor(TimeSpan.FromMilliseconds(Math.Max(0, m.ExpiresAtMs - NowMs)).TotalDays);
            var expiry = daysLeft >= 1 ? Loc.F("mail.expires", daysLeft) : Loc.T("mail.expiresToday");
            CardText(head.transform, $"{MailSender(m)} · {MailAge(m)} · {expiry}", 22, UiKit.Muted, 0.17f, 0.06f, 0.97f, 0.42f, TextAnchor.MiddleLeft);

            if (m.Body.Length > 0)
            {
                var lines = m.Body.Split('\n').Sum(l => 1 + l.Length / 40);
                var body = Card(Mathf.Clamp(50 + lines * 40, 120, 1400));
                CardText(body.transform, m.Body, 28, UiKit.Text, 0.04f, 0.04f, 0.96f, 0.96f);
            }

            if (m.Attachments.Length > 0)
            {
                Header(Loc.T("mail.attachments"));
                var gifts = Card(190);
                RewardStrip(gifts.transform, m.Attachments, 0.03f, 0.1f, 0.64f, 0.9f, 190);
                var claimable = Claimable(m);
                CardButton(gifts.transform, claimable ? Loc.T("mail.claim") : Loc.T("mail.claimed"), claimable, () => ClaimMail(m.Id), UiKit.Gold, 0.66f, 0.24f, 0.97f, 0.76f);
            }

            var actions = UiKit.Rect("MailActions", _list).Row(110);
            CardButton(actions, Loc.T("mail.back"), true, () =>
            {
                _mailId = string.Empty;
                Open(Page.Mail);
            }, UiKit.Field, 0.0f, 0.1f, 0.49f, 0.9f);
            var id = m.Id;
            CardButton(actions, Loc.T("mail.delete"), !Claimable(m), () =>
            {
                _mailId = string.Empty;
                DeleteMail(id);
                Open(Page.Mail);
            }, UiKit.Danger, 0.51f, 0.1f, 1f, 0.9f);
        }
    }
}
