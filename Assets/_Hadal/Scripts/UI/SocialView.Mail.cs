using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The mailbox (docs/29 §2.1): system, Overseer, event and alliance mails, newest first, with an unread dot, the
    /// sender, title, age and attachment icons. Opening a mail shows its text and attachments with a claim button;
    /// "Claim all" and "Delete read" act on the whole box. Mails go after 30 days, unclaimed attachments with them.
    /// Players write to one player at a time (MailRules); resources go along only to alliance mates, within today's
    /// allowance, which the compose page shows.
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
            "battle" => "event_war",
            _ => "side_mail"
        };

        private static bool Claimable(MailDto m) => m.Attachments.Length > 0 && !m.Claimed;

        /// <summary>Player-written text shows as typed: no rich-text tags.</summary>
        private static Text Plain(Text label, MailDto m)
        {
            label.supportRichText = m.Kind != "player";
            return label;
        }

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
                Plain(CardText(card.transform, MailText.Show(m.Title, m.Kind), 30, UiKit.Text, 0.17f, 0.3f, 0.97f, 0.64f, TextAnchor.MiddleLeft, m.Read ? FontStyle.Normal : FontStyle.Bold), m)
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
            Plain(CardText(head.transform, MailText.Show(m.Title, m.Kind), 34, UiKit.Text, 0.17f, 0.42f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold), m);
            var daysLeft = (int)Math.Floor(TimeSpan.FromMilliseconds(Math.Max(0, m.ExpiresAtMs - NowMs)).TotalDays);
            var expiry = daysLeft >= 1 ? Loc.F("mail.expires", daysLeft) : Loc.T("mail.expiresToday");
            CardText(head.transform, $"{MailSender(m)} · {MailAge(m)} · {expiry}", 22, UiKit.Muted, 0.17f, 0.06f, 0.97f, 0.42f, TextAnchor.MiddleLeft);

            if (m.Body.Length > 0)
            {
                var shown = MailText.Show(m.Body, m.Kind);
                var lines = shown.Split('\n').Sum(l => 1 + l.Length / 40);
                var body = Card(Mathf.Clamp(50 + lines * 40, 120, 2400));
                Plain(CardText(body.transform, shown, 28, UiKit.Text, 0.04f, 0.04f, 0.96f, 0.96f), m);
            }

            if (m.Attachments.Length > 0)
            {
                Header(Loc.T("mail.attachments"));
                var gifts = Card(190);
                RewardStrip(gifts.transform, m.Attachments, 0.03f, 0.1f, 0.64f, 0.9f, 190);
                var claimable = Claimable(m);
                CardButton(gifts.transform, claimable ? Loc.T("mail.claim") : Loc.T("mail.claimed"), claimable, () => ClaimMail(m.Id), UiKit.Gold, 0.66f, 0.24f, 0.97f, 0.76f);
            }

            // Back · Reply (player mails) or Open report (battle mails) · Delete.
            var actions = UiKit.Rect("MailActions", _list).Row(110);
            var canReply = m.Kind == "player" && m.SenderId.Length > 0;
            var canReport = m.Kind == "battle" && PvpCenter.Instance != null && MailText.BattleRef(m.Body, out _, out _);
            var width = canReply || canReport ? 1f / 3f : 0.5f;
            CardButton(actions, Loc.T("mail.back"), true, () =>
            {
                _mailId = string.Empty;
                Open(Page.Mail);
            }, UiKit.Field, 0.0f, 0.1f, width - 0.01f, 0.9f);
            if (canReply)
            {
                var title = m.Title.StartsWith("Re: ", StringComparison.Ordinal) ? m.Title : "Re: " + m.Title;
                CardButton(actions, Loc.T("mail.reply"), true, () => OpenMailCompose(m.SenderId, m.Sender, title), UiKit.Accent,
                    width + 0.01f, 0.1f, 2 * width - 0.01f, 0.9f);
            }
            else if (canReport)
            {
                MailText.BattleRef(m.Body, out var battleId, out var reportKind);
                CardButton(actions, Loc.T("mail.pvp.open"), true, () =>
                {
                    Close();
                    PvpCenter.Instance.OpenReport(battleId, reportKind);
                }, UiKit.Accent, width + 0.01f, 0.1f, 2 * width - 0.01f, 0.9f);
            }

            var id = m.Id;
            CardButton(actions, Loc.T("mail.delete"), !Claimable(m), () =>
            {
                _mailId = string.Empty;
                DeleteMail(id);
                Open(Page.Mail);
            }, UiKit.Danger, 1f - width + 0.01f, 0.1f, 1f, 0.9f);
        }

        // --- Writing ------------------------------------------------------------------------

        private string _composeTo = string.Empty;
        private string _composeName = string.Empty;
        private string _composeTitle = string.Empty;
        private string _composeBody = string.Empty;
        private readonly Dictionary<ResourceType, string> _composeAmounts = new Dictionary<ResourceType, string>();

        /// <summary>Opens the compose page for one player (member lists, profile cards, Reply).</summary>
        public void OpenMailCompose(string playerId, string name, string title = "")
        {
            if (string.IsNullOrEmpty(playerId) || playerId == View.PlayerId)
                return;
            if (_composeTo != playerId)
            {
                _composeBody = string.Empty;
                _composeAmounts.Clear();
                _composeTitle = title ?? string.Empty;
            }
            else if (!string.IsNullOrEmpty(title))
            {
                _composeTitle = title;
            }

            _composeTo = playerId;
            _composeName = string.IsNullOrEmpty(name) ? playerId.Substring(0, Math.Min(8, playerId.Length)) : name;
            _afterReward = Page.None;
            Open(Page.MailCompose);
        }

        private string ComposeKey() => $"{_composeTo}|{View.MailQuotaVersion}|{View.AllianceVersion}";

        private bool ComposeToMate() => View.MyAlliance?.Members?.Any(m => m.PlayerId == _composeTo) == true;

        /// <summary>Today's allowance: the server's numbers for today, or the full ones once a new UTC day began.</summary>
        private long MailAllowanceLeft(ResourceType type)
        {
            var quota = View.MailQuota;
            if (quota != null && quota.Day == MailRules.Day(NowMs))
                return quota.Left?.FirstOrDefault(l => l.Key == type)?.Value ?? 0;
            return MailRules.DailyAllowance(type, ReactorLevel());
        }

        private int MailsLeftToday()
        {
            var quota = View.MailQuota;
            return quota != null && quota.Day == MailRules.Day(NowMs) ? quota.MailsLeft : MailRules.MailsPerDay;
        }

        /// <summary>After a mail went out: the form empties and the mailbox shows again. Returns the toast text.</summary>
        private string MailSent()
        {
            _composeTitle = _composeBody = string.Empty;
            _composeAmounts.Clear();
            if (_page == Page.MailCompose)
                _page = Page.Mail;
            return Loc.T("mail.sent");
        }

        private void RenderMailCompose()
        {
            _title.text = Loc.T("mail.compose");
            _subtitle.text = Loc.F("mail.to", _composeName);
            var mate = ComposeToMate();
            var mailsLeft = MailsLeftToday();

            var to = Card(120);
            HudIconImage(to.transform, mate ? "side_alliance" : "side_mail", 0.02f, 0.15f, 0.12f, 0.85f);
            var name = CardText(to.transform, _composeName, 32, UiKit.Text, 0.15f, 0.45f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.supportRichText = false;
            CardText(to.transform, Loc.F("mail.left", mailsLeft, MailRules.MailsPerDay), 22, mailsLeft > 0 ? UiKit.Muted : UiKit.Danger,
                0.15f, 0.05f, 0.97f, 0.45f, TextAnchor.MiddleLeft);

            var title = UiKit.Input("Title", _list, Loc.T("mail.titleHint"), characterLimit: MailRules.MaxTitle).Row(100);
            title.text = _composeTitle;
            title.onValueChanged.AddListener(v => _composeTitle = v);

            var body = UiKit.Input("Body", _list, Loc.T("mail.bodyHint"), characterLimit: MailRules.MaxPlayerBody).Row(320);
            body.lineType = InputField.LineType.MultiLineNewline;
            body.textComponent.alignment = TextAnchor.UpperLeft;
            body.textComponent.fontSize = 32;
            body.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            ((Text)body.placeholder).alignment = TextAnchor.UpperLeft;
            body.textComponent.rectTransform.Place(0, 0, 1, 1, 20);
            body.text = _composeBody;
            body.onValueChanged.AddListener(v => _composeBody = v);

            // Resources only between alliance mates (the genre's resource help), within today's allowance.
            if (mate)
            {
                Header(Loc.T("mail.resources"));
                var wallet = Wallet();
                foreach (var type in MailRules.Sendable)
                {
                    var row = Card(110);
                    HudIconImage(row.transform, Hud.HudIcons.Resource(type), 0.02f, 0.12f, 0.11f, 0.88f);
                    var left = MailAllowanceLeft(type);
                    var have = wallet.TryGetValue(type, out var h) ? h : 0;
                    CardText(row.transform, Loc.Resource(type.ToString()), 28, UiKit.Text, 0.13f, 0.5f, 0.5f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                    CardText(row.transform, Loc.F("mail.allowance", CostChips.Compact(left), CostChips.Compact(have)), 22, left > 0 ? UiKit.Muted : UiKit.Danger,
                        0.13f, 0.05f, 0.6f, 0.5f, TextAnchor.MiddleLeft);
                    var input = UiKit.Input("Amount", row.transform, "0", characterLimit: 9);
                    ((RectTransform)input.transform).Place(0.62f, 0.12f, 0.97f, 0.88f);
                    input.contentType = InputField.ContentType.IntegerNumber;
                    input.text = _composeAmounts.TryGetValue(type, out var typed) ? typed : string.Empty;
                    var resource = type;
                    input.onValueChanged.AddListener(v => _composeAmounts[resource] = v);
                    input.interactable = left > 0;
                }
            }
            else
            {
                Note(Loc.T("mail.resourcesMatesOnly"));
            }

            var actions = UiKit.Rect("ComposeActions", _list).Row(110);
            CardButton(actions, Loc.T("mail.cancel"), true, () => Open(Page.Mail), UiKit.Field, 0.0f, 0.1f, 0.49f, 0.9f);
            CardButton(actions, Loc.T("mail.send"), mailsLeft > 0, SendComposedMail, UiKit.Accent, 0.51f, 0.1f, 1f, 0.9f);
        }

        private void SendComposedMail()
        {
            var title = MailRules.Clean(_composeTitle, MailRules.MaxTitle, multiline: false);
            if (title.Length == 0)
            {
                Toast(Loc.Reason("InvalidMailTitle"), UiKit.Danger);
                return;
            }

            var resources = new List<ResourceAmountEntry>();
            if (ComposeToMate())
            {
                foreach (var pair in _composeAmounts)
                {
                    if (long.TryParse(pair.Value, out var amount) && amount > 0)
                        resources.Add(new ResourceAmountEntry { Key = pair.Key, Value = amount });
                }
            }

            _commands.Dispatch(new SendMailCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), PlayerId = _composeTo, Title = title,
                Body = MailRules.Clean(_composeBody, MailRules.MaxPlayerBody, multiline: true), Resources = resources.ToArray()
            });
        }
    }
}
