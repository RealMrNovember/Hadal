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
    /// The mailbox in Last Asylum's layout (docs/37): tabs Battle · Alliance · Event · System · Personal, each with its
    /// unread count; rows with a sealed or opened envelope, a bold title, a one-line summary (the enemy in red), the age, a
    /// red dot while unread and the gift while attachments wait. "Claim all", "Mark all read" and "Delete read" act on the
    /// open tab. Battle and scout mails open as a report card drawn from the report itself (two sides, result, troops,
    /// losses, loot); the rest show their text and attachments. Mails go after 30 days, unclaimed attachments with them.
    /// Players write to one player at a time (MailRules); resources go along only to alliance mates, within today's
    /// allowance, which the compose page shows.
    /// </summary>
    public sealed partial class SocialView
    {
        private static readonly Color MailRed = new Color(0.86f, 0.2f, 0.18f, 1f);
        private static readonly Color AttackerSide = new Color(0.13f, 0.3f, 0.52f, 1f);
        private static readonly Color DefenderSide = new Color(0.5f, 0.14f, 0.14f, 1f);

        private string _mailId = string.Empty;
        private string _mailTab = string.Empty;

        /// <summary>Opens the mailbox on the first tab with something waiting (else the last one used, else Battle).</summary>
        public void OpenMail()
        {
            _afterReward = Page.None;
            _mailId = string.Empty;
            var waiting = MailRules.Tabs.FirstOrDefault(t => TabCount(t).Unread > 0 || TabCount(t).Unclaimed > 0);
            _mailTab = waiting ?? (_mailTab.Length > 0 ? _mailTab : MailRules.TabBattle);
            Open(Page.Mail);
        }

        private string MailKey() => $"{View.MailVersion}|{_mailId}|{_mailTab}";

        private MailDto[] Mails() => View.Mail?.Mails ?? Array.Empty<MailDto>();

        private MailDto[] TabMails() => Mails().Where(m => MailRules.TabOf(m.Kind) == _mailTab).ToArray();

        /// <summary>Unread and unclaimed in a tab: the server's count over every live mail, or the shown mails' own.</summary>
        private (int Unread, int Unclaimed) TabCount(string tab)
        {
            var counted = View.Mail?.Tabs?.FirstOrDefault(t => t.Tab == tab);
            if (counted != null)
                return (counted.Unread, counted.Unclaimed);
            var mails = Mails().Where(m => MailRules.TabOf(m.Kind) == tab).ToList();
            return (mails.Count(m => !m.Read), mails.Count(Claimable));
        }

        private static string MailSender(MailDto m) =>
            m.Sender.Length > 0 ? m.Sender : Loc.Has("mail.sender." + m.Kind) ? Loc.T("mail.sender." + m.Kind) : Loc.T("mail.sender.system");

        private static string TabIcon(string tab) => tab switch
        {
            MailRules.TabBattle => "event_war",
            MailRules.TabAlliance => "side_alliance",
            MailRules.TabEvent => "event_gift",
            MailRules.TabPersonal => "chat",
            _ => "side_mail"
        };

        private static string MailIcon(MailDto m)
        {
            if (m.Report != null)
                return m.Report.Kind is "scout" or "scouted" ? "left_search" : m.Report.Outcome == "shielded" ? "item_shield_8h" : "event_war";
            return m.Kind switch
            {
                "event" => "event_gift",
                "alliance" => "side_alliance",
                "battle" => "event_war",
                "player" => "chat",
                _ => "side_mail"
            };
        }

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

        private void ClaimMail(string mailId, string tab = "") =>
            _commands.Dispatch(new ClaimMailCommand { CommandId = NewId(), ClientSequence = NextSequence(), MailId = mailId, Tab = tab });

        private void DeleteMail(string mailId, string tab = "") =>
            _commands.Dispatch(new DeleteMailCommand { CommandId = NewId(), ClientSequence = NextSequence(), MailId = mailId, Tab = tab });

        private void ReadAllMail(string tab) =>
            _commands.Dispatch(new ReadMailCommand { CommandId = NewId(), ClientSequence = NextSequence(), Tab = tab });

        // --- Report wording ---------------------------------------------------------------

        /// <summary>The other side: "[TAG] Name", or the city or alliance building fought over.</summary>
        private static string Opponent(PvpReportDto r)
        {
            if (!string.IsNullOrEmpty(r.StrongholdId))
            {
                var place = AllianceLandRules.IsBuilding(r.StrongholdId) ? Loc.T("bld." + r.StrongholdId)
                    : r.StrongholdId == AllianceLandRules.Beacon ? Loc.T("land.beacon") : Loc.T("city.name");
                return r.StrongholdLevel > 0 ? $"{place} {Loc.F("base.level", r.StrongholdLevel)}" : place;
            }

            return (string.IsNullOrEmpty(r.OpponentTag) ? string.Empty : $"[{r.OpponentTag}] ") + r.OpponentName;
        }

        /// <summary>We sent the army (attack, rally, scout); otherwise it came to us or our team.</summary>
        private static bool WeAttacked(PvpReportDto r) => r.Kind is "attack" or "rally" or "scout";

        private static bool IsScout(PvpReportDto r) => r.Kind is "scout" or "scouted";

        /// <summary>The row's and card's headline: Victory / Defeat / Shield blocked / Scout report / You were scouted.</summary>
        private static string Headline(PvpReportDto r) =>
            r.Kind == "scouted" ? Loc.T("mail.head.scouted")
            : r.Kind == "scout" ? Loc.T("mail.head.scout")
            : r.Outcome is "won" or "lost" or "shielded" ? Loc.T("mail.head." + r.Outcome)
            : Loc.T("pvp.outcome." + r.Outcome);

        private static Color HeadlineColor(PvpReportDto r) =>
            IsScout(r) ? UiKit.Accent : r.Outcome == "won" ? UiKit.Ok : r.Outcome == "lost" ? MailRed : UiKit.Amber;

        /// <summary>"Attack · [TAG] Name" with the enemy in red, as Last Asylum's rows do.</summary>
        private static string ReportSummary(PvpReportDto r) =>
            $"{Loc.T("pvp.kind." + r.Kind)} · <color=#E0453A>{MailEscape(Opponent(r))}</color>";

        private static string MailEscape(string text) => (text ?? string.Empty).Replace("<", "‹").Replace(">", "›");

        /// <summary>The first line of a mail's text, cut short, for its row.</summary>
        private static string FirstLine(MailDto m)
        {
            var text = MailText.Show(m.Body, m.Kind);
            var line = text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? string.Empty;
            return line.Length > 60 ? line.Substring(0, 57) + "…" : line;
        }

        // --- List ---------------------------------------------------------------------------

        private void RenderMail()
        {
            _title.text = Loc.T("mail.title");
            var box = View.Mail ?? new MailListDto();
            _subtitle.text = box.Unread > 0 ? Loc.F("mail.unread", box.Unread) : string.Empty;
            if (!MailRules.Tabs.Contains(_mailTab))
                _mailTab = MailRules.TabBattle;

            MailTabs();

            var mails = TabMails();
            var (unread, unclaimed) = TabCount(_mailTab);
            var tab = _mailTab;
            var bar = UiKit.Rect("MailBar", _list).Row(100);
            var claimAll = CardButton(bar, Loc.T("mail.claimAll"), unclaimed > 0, () => ClaimMail(string.Empty, tab), UiKit.Gold, 0.0f, 0.08f, 0.325f, 0.92f);
            if (unclaimed > 0)
                RedDot(claimAll.transform);
            CardButton(bar, Loc.T("mail.readAll"), unread > 0, () => ReadAllMail(tab), UiKit.Accent, 0.3375f, 0.08f, 0.6625f, 0.92f);
            CardButton(bar, Loc.T("mail.deleteRead"), mails.Any(m => m.Read && !Claimable(m)), () => DeleteMail(string.Empty, tab), UiKit.Field,
                0.675f, 0.08f, 1f, 0.92f);
            foreach (var label in bar.GetComponentsInChildren<Text>())
                label.resizeTextMaxSize = Math.Min(label.resizeTextMaxSize, 28);

            if (mails.Length == 0)
            {
                Note(Loc.T("mail.emptyTab"));
                return;
            }

            foreach (var m in mails)
                MailRow(m);

            Note(Loc.T("mail.note"));
        }

        /// <summary>Five tabs across, each an icon over its name with a red count while something is unread.</summary>
        private void MailTabs()
        {
            var strip = UiKit.Rect("MailTabs", _list).Row(132);
            var tabs = MailRules.Tabs;
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                var on = tab == _mailTab;
                var x0 = (float)i / tabs.Length;
                var button = UiKit.Button("Tab" + i, strip, string.Empty, () =>
                {
                    _mailTab = tab;
                    _mailId = string.Empty;
                    Open(Page.Mail);
                }, on ? UiKit.TabOn : UiKit.TabOff, 22);
                ((RectTransform)button.transform).Place(x0 + 0.004f, 0.04f, x0 + 1f / tabs.Length - 0.004f, 0.96f);
                HudIconImage(button.transform, TabIcon(tab), 0.3f, 0.44f, 0.7f, 0.92f).raycastTarget = false;
                var name = UiKit.Label("Name", button.transform, Loc.T("mail.tab." + tab), 22, on ? new Color(0.06f, 0.1f, 0.14f) : Color.white,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.Place(0.02f, 0.06f, 0.98f, 0.44f);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 14;
                name.resizeTextMaxSize = 22;
                name.raycastTarget = false;
                if (!on)
                    UiKit.Stroked(name);

                var unread = TabCount(tab).Unread;
                if (unread > 0)
                    CountBadge(button.transform, unread);
            }
        }

        /// <summary>A red disc with a number at the top right corner (99+ beyond).</summary>
        private static void CountBadge(Transform parent, int count)
        {
            var badge = UiKit.Box("Badge", parent, MailRed);
            badge.sprite = UiKit.Circle;
            badge.type = Image.Type.Simple;
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.86f, 0.86f);
            badge.rectTransform.sizeDelta = new Vector2(count > 9 ? 50 : 40, 40);
            badge.raycastTarget = false;
            var label = UiKit.Label("Count", badge.transform, count > 99 ? "99+" : count.ToString(), 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(0, 0, 1, 1);
            label.raycastTarget = false;
        }

        /// <summary>
        /// One row: envelope (sealed while unread), bold title, summary, age; unread rows are bright with a red dot and an
        /// airmail stripe, read ones dimmed with a tick; waiting attachments show the gift.
        /// </summary>
        private void MailRow(MailDto m)
        {
            var mail = m;
            var card = Card(156);
            card.color = m.Read ? new Color(0.8f, 0.85f, 0.9f, 1f) : Color.white;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.onClick.AddListener(() => OpenMailItem(mail));

            var disc = UiKit.Box("Disc", card.transform, m.Read ? new Color(0.62f, 0.7f, 0.78f, 1f) : new Color(0.78f, 0.86f, 0.94f, 1f));
            disc.sprite = UiKit.Circle;
            disc.type = Image.Type.Simple;
            disc.preserveAspect = true;
            disc.rectTransform.Place(0.02f, 0.1f, 0.16f, 0.9f);
            disc.raycastTarget = false;
            HudIconImage(disc.transform, MailIcon(m), 0.18f, 0.18f, 0.82f, 0.82f).raycastTarget = false;
            // The envelope: sealed (a red seal) until read, open after.
            HudIconImage(disc.transform, "side_mail", 0.58f, 0.0f, 0.98f, 0.4f).raycastTarget = false;
            if (!m.Read)
            {
                var seal = UiKit.Box("Seal", disc.transform, MailRed);
                seal.sprite = UiKit.Circle;
                seal.type = Image.Type.Simple;
                seal.rectTransform.anchorMin = seal.rectTransform.anchorMax = new Vector2(0.78f, 0.2f);
                seal.rectTransform.sizeDelta = new Vector2(16, 16);
                seal.raycastTarget = false;
            }

            string title, summary;
            Color titleColor = UiKit.Text;
            if (m.Report != null)
            {
                title = Headline(m.Report);
                titleColor = HeadlineColor(m.Report);
                summary = ReportSummary(m.Report);
            }
            else
            {
                title = MailText.Show(m.Title, m.Kind);
                var first = FirstLine(m);
                summary = m.Kind == "player" || first.Length == 0 ? MailSender(m) : first;
            }

            var titleText = Plain(CardText(card.transform, title, 30, titleColor, 0.19f, 0.56f, 0.8f, 0.94f, TextAnchor.MiddleLeft, FontStyle.Bold), m);
            titleText.raycastTarget = false;
            var summaryText = CardText(card.transform, summary, 23, UiKit.Muted, 0.19f, 0.3f, 0.97f, 0.58f, TextAnchor.MiddleLeft);
            summaryText.supportRichText = m.Report != null || m.Kind != "player";
            summaryText.horizontalOverflow = HorizontalWrapMode.Overflow;
            summaryText.raycastTarget = false;
            var ageLine = m.Kind == "player" ? MailAge(m) : $"{MailAge(m)} · {MailSender(m)}";
            CardText(card.transform, ageLine, 20, UiKit.Muted, 0.19f, 0.04f, 0.75f, 0.3f, TextAnchor.MiddleLeft).raycastTarget = false;

            if (!m.Read)
            {
                var dot = UiKit.Box("Unread", card.transform, MailRed);
                dot.sprite = UiKit.Circle;
                dot.type = Image.Type.Simple;
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.965f, 0.84f);
                dot.rectTransform.sizeDelta = new Vector2(26, 26);
                dot.raycastTarget = false;
                // The airmail stripe along the bottom edge.
                for (var i = 0; i < 24; i++)
                {
                    var stripe = UiKit.Box("Stripe", card.transform, i % 2 == 0 ? new Color(0.85f, 0.3f, 0.3f, 0.55f) : new Color(0.3f, 0.45f, 0.8f, 0.55f));
                    stripe.sprite = null;
                    stripe.rectTransform.Place(0.02f + i * 0.04f, 0.0f, 0.02f + (i + 1) * 0.04f - 0.012f, 0.035f);
                    stripe.raycastTarget = false;
                }
            }

            if (Claimable(m))
            {
                HudIconImage(card.transform, "event_gift", 0.86f, 0.06f, 0.96f, 0.5f).raycastTarget = false;
            }
            else if (m.Read)
            {
                var tick = CardText(card.transform, "✓", 64, new Color(0.45f, 0.55f, 0.65f, 0.45f), 0.82f, 0.0f, 0.98f, 0.62f, TextAnchor.MiddleCenter, FontStyle.Bold);
                tick.raycastTarget = false;
            }
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
            _subtitle.text = Loc.T("mail.tab." + MailRules.TabOf(m.Kind));

            if (m.Report != null)
            {
                RenderReportMail(m, m.Report);
                MailActions(m);
                return;
            }

            var head = Card(170);
            HudIconImage(head.transform, MailIcon(m), 0.02f, 0.15f, 0.14f, 0.85f);
            Plain(CardText(head.transform, MailText.Show(m.Title, m.Kind), 34, UiKit.Text, 0.17f, 0.42f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold), m);
            var daysLeft = (int)Math.Floor(TimeSpan.FromMilliseconds(Math.Max(0, m.ExpiresAtMs - NowMs)).TotalDays);
            var expiry = daysLeft >= 1 ? Loc.F("mail.expires", daysLeft) : Loc.T("mail.expiresToday");
            var from = CardText(head.transform, $"{MailSender(m)} · {MailAge(m)} · {expiry}", 22, UiKit.Muted, 0.17f, 0.06f, 0.97f, 0.42f, TextAnchor.MiddleLeft);
            from.supportRichText = false;

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

            MailActions(m);
        }

        /// <summary>Back · Reply (player mails), Go there (reports) or Open report (old battle mails) · Delete.</summary>
        private void MailActions(MailDto m)
        {
            var actions = UiKit.Rect("MailActions", _list).Row(110);
            var canReply = m.Kind == "player" && m.SenderId.Length > 0;
            var canGo = m.Report != null && ShowOnMap != null;
            var canReport = m.Report == null && m.Kind == "battle" && PvpCenter.Instance != null && MailText.BattleRef(m.Body, out _, out _);
            var width = canReply || canGo || canReport ? 1f / 3f : 0.5f;
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
            else if (canGo)
            {
                var x = m.Report.X;
                var y = m.Report.Y;
                CardButton(actions, Loc.T("mail.report.goThere"), true, () =>
                {
                    Close();
                    ShowOnMap?.Invoke(x, y);
                }, UiKit.Accent, width + 0.01f, 0.1f, 2 * width - 0.01f, 0.9f);
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

        // --- Battle and scout reports (docs/37 §3–4) ---------------------------------------

        /// <summary>
        /// Last Asylum's report: place and time, the two sides face to face (attacker blue, defender red) with the result
        /// over each, then the overview, both armies stack by stack (start, lost, left), our losses, the loot or what was
        /// taken with the protected share, and the battle's id with its seed.
        /// </summary>
        private void RenderReportMail(MailDto m, PvpReportDto r)
        {
            // Place (a link to the map) and the time it happened.
            var top = Card(70);
            var at = DateTimeOffset.FromUnixTimeMilliseconds(r.AtMs > 0 ? r.AtMs : m.SentAtMs).ToLocalTime();
            var place = CardText(top.transform, Loc.F("mail.report.place", r.X, r.Y), 26, UiKit.Ok, 0.03f, 0f, 0.6f, 1f, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (ShowOnMap != null)
            {
                var x = r.X;
                var y = r.Y;
                place.raycastTarget = true;
                place.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    Close();
                    ShowOnMap?.Invoke(x, y);
                });
            }

            CardText(top.transform, at.ToString("yyyy-MM-dd HH:mm"), 24, UiKit.Muted, 0.55f, 0f, 0.97f, 1f, TextAnchor.MiddleRight);

            VersusBanner(r);

            if (IsScout(r))
            {
                ScoutBody(r);
                Integrity(r);
                return;
            }

            if (r.Outcome == "shielded")
            {
                Paragraph(WeAttacked(r) ? Loc.T("mail.pvp.bounced") : Loc.F("mail.pvp.blocked", Opponent(r)));
                Integrity(r);
                return;
            }

            if (r.Outcome is "moved" or "gone")
            {
                Paragraph(Loc.T("mail.report." + r.Outcome));
                Integrity(r);
                return;
            }

            // Overview: powers, rounds, our losses and share.
            Header(Loc.T("mail.report.overview"));
            var overview = Card(r.Contribution > 0 && r.Contribution < 100 ? 250 : 200);
            PowerBar(overview.transform, r.AttackerPower, r.DefenderPower, 0.62f, 0.92f);
            CardText(overview.transform, Loc.F("report.rounds", r.Rounds), 24, UiKit.Muted, 0.03f, 0.44f, 0.97f, 0.62f, TextAnchor.MiddleCenter);
            CardText(overview.transform, Loc.F("mail.report.ourLosses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")), 26, UiKit.Text,
                0.03f, 0.22f, 0.97f, 0.44f, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (r.Contribution > 0 && r.Contribution < 100)
                CardText(overview.transform, Loc.F("report.contribution", r.Contribution), 24, UiKit.Muted, 0.03f, 0.02f, 0.97f, 0.22f, TextAnchor.MiddleCenter);

            // Both armies, stack by stack.
            Header(Loc.T("mail.report.troops"));
            ArmyTable(Loc.T("report.attacker"), r.Attacker, AttackerSide, WeAttacked(r));
            ArmyTable(Loc.T("report.defender"), r.Defender, DefenderSide, !WeAttacked(r));

            Spoils(r);

            if (r.PlunderShare < 100 && r.Kind is "attack" or "rally")
                Paragraph(Loc.F("mail.pvp.share", r.PlunderShare));
            if (r.Captured)
                Paragraph(Loc.T("mail.pvp.captured"));
            Integrity(r);
        }

        /// <summary>Blue attacker half, VS, red defender half; each with the result, name and power.</summary>
        private void VersusBanner(PvpReportDto r)
        {
            var banner = UiKit.Rect("Versus", _list).Row(260);
            var we = WeAttacked(r);
            var us = Loc.T("mail.report.you");
            var them = Opponent(r);
            var attackerName = we ? us : them;
            var defenderName = we ? them : us;
            var attackerWon = r.Outcome == "won" ? we : r.Outcome == "lost" ? !we : (bool?)null;
            if (IsScout(r) || r.Outcome is "shielded" or "moved" or "gone")
                attackerWon = null;

            Half(0f, 0.5f, AttackerSide, attackerName, r.FromX, r.FromY, r.AttackerPower,
                IsScout(r) ? Loc.T("mail.report.scoutSide") : attackerWon == null ? string.Empty : Loc.T(attackerWon.Value ? "battle.won" : "battle.lost"),
                attackerWon == true);
            Half(0.5f, 1f, DefenderSide, defenderName, r.X, r.Y, r.Kind == "scouted" ? 0 : r.DefenderPower,
                IsScout(r) ? Loc.T("mail.report.scoutedSide") : attackerWon == null ? string.Empty : Loc.T(attackerWon.Value ? "battle.lost" : "battle.won"),
                attackerWon == false);

            // The middle: VS for a fight, the spyglass for a scout, the shield for a blocked attack.
            if (IsScout(r) || r.Outcome == "shielded")
            {
                var ring = UiKit.Box("Mid", banner, new Color(0.06f, 0.1f, 0.16f, 1f));
                ring.sprite = UiKit.Circle;
                ring.type = Image.Type.Simple;
                ring.preserveAspect = true;
                ring.rectTransform.Place(0.42f, 0.36f, 0.58f, 0.74f);
                HudIconImage(ring.transform, IsScout(r) ? "left_search" : "item_shield_8h", 0.15f, 0.15f, 0.85f, 0.85f);
            }
            else
            {
                var vs = UiKit.Stroked(UiKit.Label("Vs", banner, "VS", 64, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold));
                vs.rectTransform.Place(0.38f, 0.3f, 0.62f, 0.8f);
            }

            void Half(float x0, float x1, Color color, string name, int x, int y, long power, string result, bool won)
            {
                var half = UiKit.Box("Side", banner, color);
                half.rectTransform.Place(x0 + 0.003f, 0f, x1 - 0.003f, 1f);
                var inner = x0 < 0.5f ? (0.04f, 0.8f) : (0.2f, 0.96f);
                var title = UiKit.Stroked(UiKit.Label("Result", half.transform, result, 40, won ? UiKit.Gold : new Color(0.85f, 0.88f, 0.92f),
                    TextAnchor.MiddleCenter, FontStyle.Bold));
                title.rectTransform.Place(inner.Item1, 0.66f, inner.Item2, 0.96f);
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 20;
                title.resizeTextMaxSize = 40;
                var who = UiKit.Label("Name", half.transform, name, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                who.rectTransform.Place(0.04f, 0.38f, 0.96f, 0.64f);
                who.supportRichText = false;
                who.resizeTextForBestFit = true;
                who.resizeTextMinSize = 16;
                who.resizeTextMaxSize = 28;
                var where = x != 0 || y != 0 ? Loc.F("world.coords", x, y) : string.Empty;
                UiKit.Label("Where", half.transform, where, 22, new Color(0.6f, 0.95f, 0.6f), TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.2f, 0.96f, 0.38f);
                if (power > 0)
                    UiKit.Label("Power", half.transform, Loc.F("hero.power", power.ToString("N0")), 22, UiKit.Amber, TextAnchor.MiddleCenter)
                        .rectTransform.Place(0.04f, 0.02f, 0.96f, 0.2f);
            }
        }

        /// <summary>Two bars facing each other: the attacker's power share in blue from the left, the defender's in red.</summary>
        private static void PowerBar(Transform card, long attacker, long defender, float y0, float y1)
        {
            var total = Math.Max(1, attacker + defender);
            var share = Mathf.Clamp((float)attacker / total, 0.04f, 0.96f);
            var track = UiKit.Box("Track", card, new Color(0.2f, 0.25f, 0.3f, 1f));
            track.rectTransform.Place(0.03f, y0 + (y1 - y0) * 0.2f, 0.97f, y1 - (y1 - y0) * 0.2f);
            var left = UiKit.Box("Attacker", track.transform, AttackerSide);
            left.rectTransform.Place(0f, 0f, share, 1f);
            var right = UiKit.Box("Defender", track.transform, DefenderSide);
            right.rectTransform.Place(share, 0f, 1f, 1f);
            UiKit.Stroked(UiKit.Label("A", track.transform, attacker.ToString("N0"), 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold)).rectTransform.Place(0.02f, 0, 0.5f, 1);
            UiKit.Stroked(UiKit.Label("D", track.transform, defender.ToString("N0"), 22, Color.white, TextAnchor.MiddleRight, FontStyle.Bold)).rectTransform.Place(0.5f, 0, 0.98f, 1);
        }

        /// <summary>One side's army: a coloured title bar, then type · start · lost · left per stack.</summary>
        private void ArmyTable(string side, BattleStackDto[] stacks, Color color, bool ours, bool startOnly = false)
        {
            var all = (stacks ?? Array.Empty<BattleStackDto>()).OrderByDescending(s => s.Start).ToList();
            var title = UiKit.Box("SideBar", _list, color).Row(64);
            UiKit.Stroked(UiKit.Label("Side", title.transform, ours ? $"{side} · {Loc.T("mail.report.you")}" : side, 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold))
                .rectTransform.Place(0.03f, 0, 0.6f, 1);
            if (!startOnly)
            {
                Column(title.transform, Loc.T("mail.report.start"), 0.52f, 0.68f);
                Column(title.transform, Loc.T("mail.report.lost"), 0.68f, 0.84f);
                Column(title.transform, Loc.T("mail.report.left"), 0.84f, 0.98f);
            }
            else
            {
                Column(title.transform, Loc.T("mail.report.count"), 0.7f, 0.98f);
            }

            if (all.Count == 0)
            {
                var none = Card(70);
                CardText(none.transform, Loc.T("mail.report.noTroops"), 24, UiKit.Muted, 0.03f, 0, 0.97f, 1, TextAnchor.MiddleCenter);
                return;
            }

            var table = Card(24 + all.Count * 74);
            var step = 1f / all.Count;
            for (var i = 0; i < all.Count; i++)
            {
                var s = all[i];
                var y1 = 1f - i * step;
                var y0 = y1 - step;
                HudIconImage(table.transform, "item_troops_" + s.Type, 0.02f, y0 + step * 0.12f, 0.1f, y1 - step * 0.12f);
                var name = CardText(table.transform, $"T{s.Tier} {Loc.T("troop." + s.Type)}", 24, UiKit.Text, 0.12f, y0, 0.52f, y1, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 16;
                name.resizeTextMaxSize = 24;
                if (startOnly)
                {
                    CardText(table.transform, s.Start.ToString("N0"), 24, UiKit.Text, 0.7f, y0, 0.98f, y1, TextAnchor.MiddleCenter);
                    continue;
                }

                CardText(table.transform, s.Start.ToString("N0"), 24, UiKit.Text, 0.52f, y0, 0.68f, y1, TextAnchor.MiddleCenter);
                CardText(table.transform, s.Lost > 0 ? "−" + s.Lost.ToString("N0") : "0", 24, s.Lost > 0 ? MailRed : UiKit.Muted, 0.68f, y0, 0.84f, y1,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                CardText(table.transform, Math.Max(0, s.Start - s.Lost).ToString("N0"), 24, UiKit.Ok, 0.84f, y0, 0.98f, y1, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            void Column(Transform bar, string text, float x0, float x1)
            {
                var label = UiKit.Label("Col", bar, text, 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.Place(x0, 0, x1, 1);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 20;
            }
        }

        /// <summary>Plunder won or resources lost, and what the storage kept safe, as reward tiles.</summary>
        private void Spoils(PvpReportDto r)
        {
            var we = WeAttacked(r);
            var loot = Entries(r.Plunder);
            if (loot.Length > 0)
            {
                Header(Loc.T(r.Kind == "scout" ? "mail.pvp.loot.scout" : we ? "mail.pvp.loot.won" : "mail.pvp.loot.lost").TrimEnd(':', '：', ' '));
                var card = Card(170);
                RewardStrip(card.transform, loot, 0.03f, 0.08f, 0.97f, 0.92f, 170);
            }
            else if (r.Rounds > 0 && r.Outcome == "won" && r.Kind is "attack" or "rally")
            {
                Paragraph(Loc.T("mail.pvp.loot.none"));
            }

            var kept = Entries(r.Protected);
            if (kept.Length > 0)
            {
                Header(Loc.T("mail.pvp.protected").TrimEnd(':', '：', ' '));
                var card = Card(170);
                RewardStrip(card.transform, kept, 0.03f, 0.08f, 0.97f, 0.92f, 170);
            }

            RewardEntryDto[] Entries(ResourceAmountEntry[] amounts) => (amounts ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0)
                .Select(p => new RewardEntryDto { Type = "resource", Id = p.Key.ToString(), Amount = p.Value }).ToArray();
        }

        /// <summary>Scouting: what we saw of the target (its power, help, army and what could be taken), or who saw us.</summary>
        private void ScoutBody(PvpReportDto r)
        {
            if (r.Kind == "scouted")
            {
                Paragraph(Loc.F("mail.report.scoutedText", Opponent(r)));
                return;
            }

            if (r.Outcome is "shielded" or "moved" or "gone")
            {
                Paragraph(r.Outcome == "shielded" ? Loc.T("mail.pvp.bounced") : Loc.T("mail.report." + r.Outcome));
                return;
            }

            Header(Loc.T("mail.report.overview"));
            var overview = Card(120);
            CardText(overview.transform, Loc.F("mail.pvp.scout", r.DefenderPower.ToString("N0"), r.Reinforcements), 28, UiKit.Text, 0.03f, 0, 0.97f, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            ArmyTable(Loc.T("report.defender"), r.Defender, DefenderSide, false, startOnly: true);
            Spoils(r);
        }

        private void Paragraph(string text)
        {
            var lines = 1 + text.Length / 38;
            var card = Card(40 + lines * 40);
            CardText(card.transform, text, 28, UiKit.Text, 0.04f, 0.05f, 0.96f, 0.95f, TextAnchor.MiddleCenter);
        }

        /// <summary>The battle id, its seed and rule version (docs/33 §4), small at the bottom like Last Asylum's report id.</summary>
        private void Integrity(PvpReportDto r)
        {
            var text = r.RuleVersion > 0 ? Loc.F("report.integrity", r.Id, r.Seed, r.RuleVersion) : Loc.F("mail.pvp.ref", r.Id);
            UiKit.Label("Integrity", _list, text, 20, UiKit.Muted, TextAnchor.MiddleCenter).Row(50);
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
