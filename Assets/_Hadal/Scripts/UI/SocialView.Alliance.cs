using System;
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
    /// Alliance page (docs/25, docs/rehber 10): invitations, find or found one; once in, the hub (SocialView.AllianceHub)
    /// leads to members with ranks and a manage card per member, help, applications and settings. Every sub-page is a
    /// full page of its own, titled in the top bar, whose back arrow returns to the hub (Last Asylum).
    /// </summary>
    public sealed partial class SocialView
    {
        private int _allianceTab; // in an alliance: an AllianceTab* page (0 = hub); without: 0 join, 1 found
        private bool _confirmLeave;
        private string _kickConfirm = string.Empty;
        private string _transferConfirm = string.Empty;
        private string _manage = string.Empty;
        private string _search = string.Empty;
        private string _newName = string.Empty;
        private string _newTag = string.Empty;
        private string _newDescription = string.Empty;
        private int _editMode = -1;
        private string _editDescription;
        private string _newEmblem = SocialRules.DefaultEmblem;
        private string _editName;
        private string _editTag;
        private string _editEmblem;

        // Rich-text colours that read on the light cards.
        private const string InkGoodHex = "#1F8F38";
        private const string InkRankHex = "#9A5B00";

        private static string RankName(int rank) => Loc.T("alliance.rank." + Mathf.Clamp(rank, 1, 5));

        private void RenderAlliance()
        {
            var alliance = View.MyAlliance;
            if (alliance == null)
                RenderNoAlliance();
            else
                RenderMyAlliance(alliance);
        }

        /// <summary>
        /// Without an alliance: Join · Found tabs; invitations first (one tap and the player is in), then the search and
        /// the alliance list, a card per alliance with its banner, leader, power and members and the join button.
        /// </summary>
        private void RenderNoAlliance()
        {
            PageTitle(Loc.T("social.alliance"));

            UiFrames.Tabs(_list, new[] { Loc.T("alliance.tab.join"), Loc.T("alliance.tab.found") }, _allianceTab,
                i => { _allianceTab = i; Render(force: true); }).Row(96);

            if (_allianceTab == 1)
            {
                RenderFound();
                return;
            }

            foreach (var invite in View.AllianceInvites.Invites ?? Array.Empty<AllianceInviteDto>())
            {
                var card = InkCard(190, UiFrames.Highlight);
                AvatarDisc.Emblem(UiKit.Rect("Banner", card.transform).Place(0.02f, 0.1f, 0.17f, 0.9f), invite.Emblem);
                var name = Ink(card.transform, $"[{invite.Tag}] {invite.Name}", UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.2f, 0.55f, 0.62f, 0.94f,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                name.richText = false;
                name.Fit(16, UiKit.TypeScale.Subtitle);
                Ink(card.transform, $"{Loc.F("alliance.invite.from", invite.InviterName)}\n{Loc.F("alliance.members", invite.Members, invite.MaxMembers)}",
                    UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.2f, 0.06f, 0.62f, 0.55f);
                var id = invite.AllianceId;
                CardButton(card.transform, Loc.T("alliance.invite.accept"), true, () => AnswerInvite(id, true), UiKit.Ok, 0.64f, 0.52f, 0.97f, 0.92f);
                CardButton(card.transform, Loc.T("alliance.invite.decline"), true, () => AnswerInvite(id, false), UiKit.TabOn, 0.64f, 0.08f, 0.97f, 0.48f);
            }

            var searchRow = UiKit.Rect("Search", _list).Row(100);
            var search = UiKit.Input("Query", searchRow, Loc.T("alliance.search"), characterLimit: 20);
            ((RectTransform)search.transform).Place(0, 0.05f, 0.72f, 0.95f);
            search.text = _search;
            search.onValueChanged.AddListener(v => _search = v);
            var go = UiKit.Button("Go", searchRow, Loc.T("alliance.search.go"), () => Browse(_search), UiKit.Primary, 34);
            ((RectTransform)go.transform).Place(0.74f, 0.05f, 1f, 0.95f);

            if (View.AllianceBrowse.Length == 0)
            {
                InkNote(Loc.T("alliance.browse.empty"));
                return;
            }

            var applied = View.AllianceInvites.Applied ?? Array.Empty<string>();
            foreach (var entry in View.AllianceBrowse)
            {
                var hasText = !string.IsNullOrEmpty(entry.Description);
                var card = InkCard(hasText ? 250 : 200);
                var top = hasText ? 0.3f : 0.04f;
                AvatarDisc.Emblem(UiKit.Rect("Banner", card.transform).Place(0.02f, top + 0.06f, 0.17f, 0.96f), entry.Emblem);
                var name = Ink(card.transform, $"[{entry.Tag}] {entry.Name}", UiKit.TypeScale.Body + 2, UiFrames.Ink, 0.2f, top + (0.96f - top) * 0.66f, 0.7f, 0.97f,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                name.richText = false;
                name.Fit(16, UiKit.TypeScale.Body + 2);
                Ink(card.transform, $"{Loc.F("alliance.leader", entry.LeaderName)}\n{Loc.F("hero.power", entry.Power.ToString("N0"))}   " +
                                    $"{Loc.F("alliance.members", entry.Members, entry.MaxMembers)}",
                    UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.2f, top, 0.7f, top + (0.96f - top) * 0.66f).richText = false;
                if (hasText)
                {
                    var text = Ink(card.transform, entry.Description, UiKit.TypeScale.Caption, UiFrames.Ink, 0.03f, 0.03f, 0.97f, 0.29f);
                    text.richText = false;
                    text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                }

                var id = entry.Id;
                var full = entry.Members >= entry.MaxMembers;
                var waiting = applied.Contains(id);
                var label = full ? Loc.T("alliance.full")
                    : waiting ? Loc.T("alliance.applied")
                    : entry.JoinMode == (int)AllianceJoinMode.Closed ? Loc.T("alliance.inviteonly")
                    : entry.JoinMode == (int)AllianceJoinMode.Approval ? Loc.T("alliance.apply")
                    : Loc.T("alliance.join");
                var can = !full && !waiting && entry.JoinMode != (int)AllianceJoinMode.Closed;
                CardButton(card.transform, label, can, () => _commands.Dispatch(new JoinAllianceCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), AllianceId = id
                }), entry.JoinMode == (int)AllianceJoinMode.Open ? UiKit.Ok : UiKit.Primary, 0.72f, top + (0.96f - top) * 0.2f, 0.97f, top + (0.96f - top) * 0.8f);
            }
        }

        /// <summary>Founding: name, tag, description and banner as a form, then the price with what is missing and Found.</summary>
        private void RenderFound()
        {
            var rules = _catalog.Alliance;
            FieldLabel(Loc.F("alliance.name.hint", rules.nameMin, rules.nameMax));
            var name = UiKit.Input("Name", _list, Loc.T("alliance.name"), characterLimit: rules.nameMax).Row(100);
            name.text = _newName;
            name.onValueChanged.AddListener(v => _newName = v);

            FieldLabel(Loc.F("alliance.tag.hint", rules.tagMin, rules.tagMax));
            var tag = UiKit.Input("Tag", _list, Loc.T("alliance.tag"), characterLimit: rules.tagMax).Row(100);
            tag.text = _newTag;
            tag.onValueChanged.AddListener(v => _newTag = v);

            FieldLabel(Loc.T("alliance.description"));
            var description = UiKit.Input("Description", _list, Loc.T("alliance.description.hint"), characterLimit: 200).Row(100);
            description.text = _newDescription;
            description.onValueChanged.AddListener(v => _newDescription = v);

            FieldLabel(Loc.T("alliance.emblem"));
            EmblemPicker(() => _newEmblem, e => _newEmblem = e);

            // The price and, before the tap, what is wrong: short name or tag, or resources missing.
            var wallet = Wallet();
            var missing = _catalog.AllianceCreateCost.FirstOrDefault(c => (wallet.TryGetValue(c.Key, out var have) ? have : 0) < c.Value);
            var problem = !SocialRules.IsValidName(_newName.Trim(), rules) ? Loc.F("alliance.name.hint", rules.nameMin, rules.nameMax)
                : !SocialRules.IsValidTag(SocialRules.NormalizeTag(_newTag), rules) ? Loc.F("alliance.tag.hint", rules.tagMin, rules.tagMax)
                : missing.Value > 0 ? Loc.F("alliance.found.short", Loc.Resource(missing.Key.ToString()))
                : string.Empty;
            var card = InkCard(200);
            Ink(card.transform, $"{Loc.T("base.cost")}: {Cost(_catalog.AllianceCreateCost, wallet)}", UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.5f, 0.66f, 0.95f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Ink(card.transform, problem.Length > 0 ? problem : Loc.T("alliance.found.ready"), UiKit.TypeScale.Caption, problem.Length > 0 ? UiFrames.InkBad : UiFrames.InkGood,
                0.04f, 0.05f, 0.66f, 0.5f);
            CardButton(card.transform, Loc.T("alliance.found"), true, () =>
            {
                if (problem.Length > 0)
                {
                    Toast(problem, UiKit.Danger);
                    return;
                }

                _commands.Dispatch(new CreateAllianceCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), Name = _newName.Trim(), Tag = _newTag.Trim(), Description = _newDescription.Trim(),
                    Emblem = _newEmblem, Language = Loc.Current
                });
            }, UiKit.Primary, 0.68f, 0.2f, 0.97f, 0.8f);
        }

        /// <summary>A form field's caption above it.</summary>
        private void FieldLabel(string text) =>
            UiKit.Label("Label", _list, text, UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.LowerLeft, FontStyle.Bold).Row(48);

        private void RenderMyAlliance(AllianceDto alliance)
        {
            var me = View.PlayerId;
            var myRank = View.MyRank();

            switch (_allianceTab)
            {
                case AllianceTabHelp:
                    PageTitle(Loc.T("alliance.tile.help"));
                    RenderHelp();
                    return;
                case AllianceTabMembers:
                    PageTitle(Loc.T("alliance.tile.members"));
                    _subtitle.text = $"{Loc.F("alliance.members", alliance.Members.Length, alliance.MaxMembers)}   ·   {RankName(myRank)}";
                    RenderMembers(alliance, me, myRank);
                    return;
                case AllianceTabApplications when AlliancePermissions.CanApprove(myRank):
                    PageTitle(Loc.T("alliance.bar.applications"));
                    RenderApplications(alliance);
                    return;
                case AllianceTabRankings:
                    PageTitle(Loc.T("alliance.tile.rankings"));
                    RenderAllianceRankings();
                    return;
                case AllianceTabResearch:
                    PageTitle(Loc.T("alliance.tile.research"));
                    RenderAllianceResearch(myRank);
                    return;
                case AllianceTabGifts:
                    PageTitle(Loc.T("alliance.tile.gifts"));
                    RenderAllianceGifts();
                    return;
                case AllianceTabShop:
                    PageTitle(Loc.T("alliance.tile.shop"));
                    RenderAllianceShop();
                    return;
                case AllianceTabLog:
                    PageTitle(Loc.T("alliance.bar.log"));
                    RenderAllianceLog(AllianceLogKinds.Log);
                    return;
                case AllianceTabBattleLog:
                    PageTitle(Loc.T("alliance.bar.battlelog"));
                    RenderAllianceLog(AllianceLogKinds.Battle);
                    return;
                case AllianceTabSettings when AlliancePermissions.CanEditAnnouncement(myRank):
                    PageTitle(Loc.T("alliance.bar.settings"));
                    RenderSettings(alliance, myRank);
                    return;
                default:
                    _allianceTab = AllianceTabHub;
                    RenderHub(alliance, myRank);
                    return;
            }
        }

        private readonly System.Collections.Generic.HashSet<int> _collapsedRanks = new System.Collections.Generic.HashSet<int>();

        private static int RankOfMember(AllianceMemberDto m) => m.Rank > 0 ? m.Rank : m.IsLeader ? AlliancePermissions.Leader : AlliancePermissions.Diver;

        /// <summary>"Online", or when the member was last seen ("3 h ago").</summary>
        private string LastSeen(AllianceMemberDto m)
        {
            if (m.Online)
                return $"<color={InkGoodHex}>{Loc.T("world.online")}</color>";
            if (m.LastSeenMs <= 0)
                return Loc.T("world.offline");
            var t = TimeSpan.FromMilliseconds(Math.Max(0, NowMs - m.LastSeenMs));
            return t.TotalDays >= 1 ? Loc.F("mail.age.day", (int)t.TotalDays)
                : t.TotalHours >= 1 ? Loc.F("mail.age.hour", (int)t.TotalHours)
                : t.TotalMinutes >= 1 ? Loc.F("mail.age.min", (int)t.TotalMinutes)
                : Loc.T("mail.age.now");
        }

        /// <summary>
        /// Members, Last Asylum's layout (docs/31 §2): the leader's card, the officers' duty titles as portrait slots, then
        /// collapsible rank bands R4 → R1 with counts (R4 against its seats), each member with portrait, level, power and
        /// online / last seen, and the manage card (message, mail, rank, duty, hand over, remove) under the row.
        /// </summary>
        private void RenderMembers(AllianceDto alliance, string me, int myRank)
        {
            var leader = alliance.Members.FirstOrDefault(m => m.IsLeader);
            if (leader != null)
            {
                Band(Loc.T("alliance.leader.title"));
                MemberRow(leader, alliance, me, myRank, 170, UiFrames.Highlight);
            }

            // Officer duties: one titled slot each, the officer holding it or empty.
            Band(Loc.T("alliance.titles"));
            var duties = AlliancePermissions.Roles.Where(r => r.Length > 0).ToArray();
            var row = UiKit.Rect("Titles", _list).Row(240);
            for (var i = 0; i < duties.Length; i++)
            {
                var duty = duties[i];
                var holder = alliance.Members.FirstOrDefault(m => m.Role == duty && RankOfMember(m) == AlliancePermissions.Officer);
                var slot = UiFrames.CardBox(row, holder != null ? UiFrames.Card : UiFrames.CardAlt, "Title");
                slot.rectTransform.Place(i / (float)duties.Length + 0.008f, 0.03f, (i + 1) / (float)duties.Length - 0.008f, 0.97f);
                var face = UiKit.Rect("Face", slot.transform).Place(0.2f, 0.38f, 0.8f, 0.95f);
                if (holder != null)
                {
                    AvatarDisc.Draw(face, holder.AvatarId, holder.Name, holder.PlayerId);
                    var id = holder.PlayerId;
                    var name = holder.Name;
                    var avatar = holder.AvatarId;
                    var button = slot.gameObject.AddComponent<Button>();
                    button.targetGraphic = slot;
                    button.onClick.AddListener(() => ProfileCard.Instance?.Show(id, name, avatar));
                }
                else
                {
                    var empty = UiKit.Box("Empty", face, UiFrames.Band);
                    empty.sprite = UiKit.Circle;
                    empty.type = Image.Type.Simple;
                    empty.preserveAspect = true;
                    empty.raycastTarget = false;
                    empty.rectTransform.Place(0, 0, 1, 1);
                    UiKit.Label("Plus", empty.transform, "+", UiKit.TypeScale.Display, UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
                }

                Ink(slot.transform, Loc.T("alliance.duty." + duty), UiKit.TypeScale.Caption, UiFrames.Ink, 0.03f, 0.19f, 0.97f, 0.37f, TextAnchor.MiddleCenter, FontStyle.Bold);
                var label = Ink(slot.transform, holder?.Name ?? Loc.T("alliance.duty.empty"), UiKit.TypeScale.Micro, holder != null ? UiFrames.Ink : UiFrames.InkSoft,
                    0.03f, 0.02f, 0.97f, 0.19f, TextAnchor.MiddleCenter);
                label.richText = false;
                label.Fit(12, UiKit.TypeScale.Micro);
            }

            // Rank bands R4 → R1; a tap folds or opens one.
            for (var rank = AlliancePermissions.Officer; rank >= AlliancePermissions.Recruit; rank--)
            {
                var group = alliance.Members.Where(m => !m.IsLeader && RankOfMember(m) == rank).ToList();
                var open = !_collapsedRanks.Contains(rank);
                var count = rank == AlliancePermissions.Officer ? $"{group.Count}/{AlliancePermissions.MaxOfficers}" : group.Count.ToString();
                var head = UiKit.Box("RankBand", _list, UiFrames.Band).Row(84);
                head.sprite = UiKit.Rounded;
                var groupRank = rank;
                var toggle = head.gameObject.AddComponent<Button>();
                toggle.targetGraphic = head;
                toggle.onClick.AddListener(() =>
                {
                    if (!_collapsedRanks.Remove(groupRank))
                        _collapsedRanks.Add(groupRank);
                    Render(force: false);
                });
                Ink(head.transform, $"{(open ? "▼" : "▶")}  R{rank} · {RankName(rank)}", UiKit.TypeScale.Body, UiFrames.Ink, 0.03f, 0.05f, 0.7f, 0.95f,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                Ink(head.transform, count, UiKit.TypeScale.Body, UiFrames.Ink, 0.7f, 0.05f, 0.97f, 0.95f, TextAnchor.MiddleRight, FontStyle.Bold);
                if (!open)
                    continue;
                foreach (var member in group)
                    MemberRow(member, alliance, me, myRank, 150, null);
            }

            var leave = InkCard(140);
            Ink(leave.transform, _confirmLeave ? Loc.T("alliance.leave.confirm") : Loc.T("alliance.leave.title"), UiKit.TypeScale.Body,
                _confirmLeave ? UiFrames.InkBad : UiFrames.Ink, 0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
            CardButton(leave.transform, _confirmLeave ? Loc.T("base.confirm.yes") : Loc.T("alliance.leave"), true, () =>
            {
                if (!_confirmLeave)
                {
                    _confirmLeave = true;
                    Render(force: false);
                    return;
                }

                _confirmLeave = false;
                _allianceTab = 0;
                _commands.Dispatch(new LeaveAllianceCommand { CommandId = NewId(), ClientSequence = NextSequence() });
            }, UiKit.Danger);
        }

        /// <summary>One member: portrait (opens the profile), name, rank and duty, level, power, online / last seen, and "⋯".</summary>
        private void MemberRow(AllianceMemberDto member, AllianceDto alliance, string me, int myRank, float height, Color? fill)
        {
            var card = InkCard(height, fill ?? (member.PlayerId == me ? UiFrames.RankMine : (Color?)null));
            var rank = RankOfMember(member);
            var face = UiKit.Rect("Face", card.transform).Place(0.015f, 0.1f, 0.15f, 0.9f);
            AvatarDisc.Draw(face, member.AvatarId, member.Name, member.PlayerId);
            face.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var faceId = member.PlayerId;
            var faceName = member.Name;
            var faceAvatar = member.AvatarId;
            face.gameObject.AddComponent<Button>().onClick.AddListener(() => ProfileCard.Instance?.Show(faceId, faceName, faceAvatar));

            var name = Ink(card.transform, member.Name, UiKit.TypeScale.Body + 2, UiFrames.Ink, 0.17f, 0.52f, 0.6f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.richText = false;
            name.Fit(16, UiKit.TypeScale.Body + 2);
            var role = string.IsNullOrEmpty(member.Role) ? string.Empty : " · " + Loc.T("alliance.duty." + member.Role);
            var level = member.Level > 0 ? Loc.F("world.level", member.Level) + "   " : string.Empty;
            Ink(card.transform, $"<color={InkRankHex}>R{rank} {RankName(rank)}{role}</color>\n{level}{Loc.F("hero.power", member.Power.ToString("N0"))}",
                UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.17f, 0.04f, 0.62f, 0.54f);
            Ink(card.transform, LastSeen(member), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.58f, 0.52f, 0.79f, 0.95f, TextAnchor.MiddleRight);

            if (member.PlayerId == me)
                return;
            var id = member.PlayerId;
            CardButton(card.transform, _manage == id ? "▲" : "⋯", true, () =>
            {
                _manage = _manage == id ? string.Empty : id;
                _kickConfirm = _transferConfirm = string.Empty;
                Render(force: false);
            }, UiKit.TabOn, 0.81f, 0.2f, 0.97f, 0.8f);

            if (_manage == id)
                RenderManage(member, rank, myRank, alliance);
        }

        /// <summary>What the viewer may do to one member: message, rank up or down, duty, hand over, remove.</summary>
        private void RenderManage(AllianceMemberDto member, int rank, int myRank, AllianceDto alliance)
        {
            var id = member.PlayerId;
            var actions = InkCard(120, UiFrames.CardAlt);
            var slots = new System.Collections.Generic.List<(string Label, bool Enabled, Action Click, Color? Fill)>
            {
                (Loc.T("alliance.message"), true, () => OpenPrivateChat(id, member.Name), UiKit.Primary),
                (Loc.T("mail.send"), true, () => OpenMailCompose(id, member.Name), UiKit.Primary)
            };

            var officers = alliance.Members.Count(m => m.Rank == AlliancePermissions.Officer);
            if (AlliancePermissions.CanSetRank(myRank, rank, rank + 1))
                slots.Add((Loc.T("alliance.promote"), rank + 1 != AlliancePermissions.Officer || officers < AlliancePermissions.MaxOfficers,
                    () => SetRank(id, rank + 1, null), UiKit.Ok));
            if (rank > AlliancePermissions.Recruit && AlliancePermissions.CanSetRank(myRank, rank, rank - 1))
                slots.Add((Loc.T("alliance.demote"), true, () => SetRank(id, rank - 1, null), UiKit.TabOn));
            if (rank == AlliancePermissions.Officer && AlliancePermissions.CanSetRole(myRank))
            {
                var roles = AlliancePermissions.Roles;
                var next = roles[(Array.IndexOf(roles, member.Role ?? string.Empty) + 1) % roles.Length];
                slots.Add((Loc.T("alliance.duty"), true, () => SetRank(id, rank, next), UiKit.TabOn));
            }

            var x = 0.02f;
            var width = 0.96f / Math.Max(1, slots.Count);
            foreach (var (label, enabled, click, fill) in slots)
            {
                CardButton(actions.transform, label, enabled, click, fill, x, 0.15f, x + width - 0.02f, 0.85f);
                x += width;
            }

            // Dangerous actions sit on their own row and need a second tap.
            var canKick = AlliancePermissions.CanKick(myRank, rank);
            var canTransfer = AlliancePermissions.CanTransfer(myRank);
            if (!canKick && !canTransfer)
                return;
            var danger = InkCard(110, UiFrames.CardAlt);
            if (canTransfer)
            {
                CardButton(danger.transform, _transferConfirm == id ? Loc.T("base.confirm.yes") : Loc.T("alliance.transfer"), true, () =>
                {
                    if (_transferConfirm != id)
                    {
                        _transferConfirm = id;
                        Render(force: false);
                        return;
                    }

                    _transferConfirm = string.Empty;
                    _commands.Dispatch(new TransferLeadershipCommand { CommandId = NewId(), ClientSequence = NextSequence(), MemberId = id });
                }, UiKit.Gold, 0.02f, 0.12f, 0.48f, 0.88f);
            }

            if (canKick)
            {
                CardButton(danger.transform, _kickConfirm == id ? Loc.T("base.confirm.yes") : Loc.T("alliance.kick"), true, () =>
                {
                    if (_kickConfirm != id)
                    {
                        _kickConfirm = id;
                        Render(force: false);
                        return;
                    }

                    _kickConfirm = string.Empty;
                    _manage = string.Empty;
                    _commands.Dispatch(new KickMemberCommand { CommandId = NewId(), ClientSequence = NextSequence(), MemberId = id });
                }, UiKit.Danger, 0.52f, 0.12f, 0.98f, 0.88f);
            }
        }

        /// <summary>Applications (R3+): a row per applicant with power, Accept and Decline; how to invite under them.</summary>
        private void RenderApplications(AllianceDto alliance)
        {
            var applicants = alliance.Applicants ?? Array.Empty<AllianceApplicantDto>();
            if (applicants.Length == 0)
                InkNote(Loc.T("alliance.applications.none"));
            foreach (var a in applicants)
            {
                var card = InkCard(150);
                var face = UiKit.Rect("Face", card.transform).Place(0.015f, 0.1f, 0.15f, 0.9f);
                AvatarDisc.Draw(face, string.Empty, a.Name, a.PlayerId);
                var name = Ink(card.transform, a.Name, UiKit.TypeScale.Body + 2, UiFrames.Ink, 0.18f, 0.5f, 0.6f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.richText = false;
                Ink(card.transform, Loc.F("hero.power", a.Power.ToString("N0")), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.18f, 0.05f, 0.6f, 0.5f);
                var id = a.PlayerId;
                CardButton(card.transform, Loc.T("alliance.invite.accept"), true, () => AnswerApplication(id, true), UiKit.Ok, 0.62f, 0.52f, 0.97f, 0.92f);
                CardButton(card.transform, Loc.T("alliance.invite.decline"), true, () => AnswerApplication(id, false), UiKit.TabOn, 0.62f, 0.08f, 0.97f, 0.48f);
            }

            InkNote(Loc.T("alliance.invite.how"));
        }

        /// <summary>Officers write the announcement (the leader also the language); the leader edits the rest.</summary>
        private void RenderSettings(AllianceDto alliance, int myRank)
        {
            RenderAnnouncementEditor(alliance, myRank);
            if (!AlliancePermissions.CanEditSettings(myRank))
                return;

            Band(Loc.T("alliance.settings"));
            if (_editMode < 0)
                _editMode = alliance.JoinMode;
            _editDescription ??= alliance.Description ?? string.Empty;
            UiFrames.Tabs(_list, new[] { Loc.T("alliance.mode.0"), Loc.T("alliance.mode.1"), Loc.T("alliance.mode.2") }, _editMode,
                i => { _editMode = i; Render(force: false); }).Row(96);
            UiKit.Label("ModeHint", _list, Loc.T("alliance.mode.hint." + _editMode), UiKit.TypeScale.Caption, UiFrames.InkSoft, TextAnchor.MiddleCenter).Row(64);
            FieldLabel(Loc.T("alliance.description"));
            var description = UiKit.Input("Description", _list, Loc.T("alliance.description.hint"), characterLimit: 200).Row(100);
            description.text = _editDescription;
            description.onValueChanged.AddListener(v => _editDescription = v);

            // Name, tag and banner: the leader only (the server checks the same and that both stay unique).
            var leader = alliance.LeaderId == View.PlayerId;
            if (leader)
            {
                var rules = _catalog.Alliance;
                _editName ??= alliance.Name;
                _editTag ??= alliance.Tag;
                _editEmblem ??= string.IsNullOrEmpty(alliance.Emblem) ? SocialRules.DefaultEmblem : alliance.Emblem;
                FieldLabel(Loc.F("alliance.name.hint", rules.nameMin, rules.nameMax));
                var name = UiKit.Input("Name", _list, Loc.T("alliance.name"), characterLimit: rules.nameMax).Row(100);
                name.text = _editName;
                name.onValueChanged.AddListener(v => _editName = v);
                FieldLabel(Loc.F("alliance.tag.hint", rules.tagMin, rules.tagMax));
                var tag = UiKit.Input("Tag", _list, Loc.T("alliance.tag"), characterLimit: rules.tagMax).Row(100);
                tag.text = _editTag;
                tag.onValueChanged.AddListener(v => _editTag = v);
                FieldLabel(Loc.T("alliance.emblem"));
                EmblemPicker(() => _editEmblem, e => _editEmblem = e);
            }

            WideButton(Loc.T("alliance.save"), true, () =>
            {
                _commands.Dispatch(new SetAllianceSettingsCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), JoinMode = _editMode, Description = (_editDescription ?? string.Empty).Trim(),
                    Name = leader && _editName != alliance.Name ? (_editName ?? string.Empty).Trim() : string.Empty,
                    Tag = leader && _editTag != alliance.Tag ? (_editTag ?? string.Empty).Trim() : string.Empty,
                    Emblem = leader ? _editEmblem ?? string.Empty : string.Empty
                });
                _editMode = -1;
                _editDescription = _editName = _editTag = _editEmblem = null;
            }, UiKit.Primary);
        }

        /// <summary>Banner picker: a preview on the left, the icons in a row and the colours in a row on a light card.</summary>
        private void EmblemPicker(Func<string> current, Action<string> set)
        {
            var (icon, color) = AvatarDisc.EmblemParts(current());
            var card = InkCard(240);
            AvatarDisc.Emblem(UiKit.Rect("Preview", card.transform).Place(0.02f, 0.12f, 0.2f, 0.88f), $"{icon}:{color}");
            var icons = SocialRules.EmblemIcons.Length;
            for (var i = 0; i < icons; i++)
            {
                var pick = i;
                var cell = UiKit.Box("Icon", card.transform, i == icon ? UiFrames.Highlight : UiFrames.CardAlt);
                cell.sprite = UiKit.Rounded;
                cell.rectTransform.Place(0.23f + i * (0.75f / icons), 0.53f, 0.23f + (i + 1) * (0.75f / icons) - 0.008f, 0.94f);
                Hud.HudIcons.Image(cell.transform, SocialRules.EmblemIcons[i], 0.15f, 0.15f, 0.85f, 0.85f).raycastTarget = false;
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { set($"{pick}:{AvatarDisc.EmblemParts(current()).Color}"); Render(force: true); });
            }

            var colors = SocialRules.EmblemColors.Length;
            for (var i = 0; i < colors; i++)
            {
                var pick = i;
                ColorUtility.TryParseHtmlString("#" + SocialRules.EmblemColors[i], out var tint);
                var cell = UiKit.Box("Colour", card.transform, tint);
                cell.sprite = UiKit.Rounded;
                cell.rectTransform.Place(0.23f + i * (0.75f / colors), 0.06f, 0.23f + (i + 1) * (0.75f / colors) - 0.008f, 0.45f);
                if (i == color)
                {
                    var outline = cell.gameObject.AddComponent<Outline>();
                    outline.effectColor = UiFrames.Ink;
                    outline.effectDistance = new Vector2(4, -4);
                }

                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { set($"{AvatarDisc.EmblemParts(current()).Icon}:{pick}"); Render(force: true); });
            }
        }

        /// <summary>Alliance help: today's coins from helping, Help all, the rule, then every open request with its count.</summary>
        private void RenderHelp()
        {
            var coins = View.AllianceCoins ?? new AllianceCoinsDto();
            var cap = Math.Max(1, coins.DailyHelpCoins > 0 ? coins.DailyHelpCoins : AllianceShopRules.DailyHelpCoins);
            var open = View.HelpRequests.Where(r => !r.HelpedByMe).ToList();
            var head = InkCard(230);
            HudIconImage(head.transform, "left_help", 0.02f, 0.4f, 0.17f, 0.95f);
            Ink(head.transform, open.Count > 0 ? Loc.F("alliance.help.open", open.Count) : Loc.T("alliance.help.none"), UiKit.TypeScale.Body, UiFrames.Ink,
                0.2f, 0.62f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ink(head.transform, Loc.F("alliance.coins.help", AllianceShopRules.CoinsPerHelp), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.2f, 0.42f, 0.97f, 0.62f);
            ProgressBar(head.transform, coins.HelpCoinsToday, cap, 0.2f, 0.28f, 0.97f, 0.42f);
            CardButton(head.transform, Loc.T("alliance.help.all"), open.Count > 0, () => _commands.Dispatch(new HelpAllCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence()
            }), UiKit.Ok, 0.3f, 0.02f, 0.7f, 0.25f);

            InkNote(Loc.T("alliance.help.rule"), 130);

            foreach (var request in View.HelpRequests)
            {
                var card = InkCard(130, request.HelpedByMe ? UiFrames.CardAlt : (Color?)null);
                var text = Ink(card.transform, $"{request.OwnerName}: {HelpJobText(request)}", UiKit.TypeScale.Body, UiFrames.Ink,
                    0.04f, 0.5f, 0.96f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                text.richText = false;
                Ink(card.transform, $"{Loc.F("alliance.help.count", request.Helps, request.MaxHelps)}{(request.HelpedByMe ? "  ✓" : string.Empty)}", UiKit.TypeScale.Caption,
                    request.HelpedByMe ? UiFrames.InkGood : UiFrames.InkSoft, 0.04f, 0.05f, 0.96f, 0.5f);
            }
        }

        /// <summary>What a help request is for: a building's next level, a healing batch or a research.</summary>
        private static string HelpJobText(HelpRequestDto request) => HelpKinds.Normalize(request.Kind) switch
        {
            HelpKinds.Heal => Loc.F("help.kind.heal", request.TargetLevel.ToString("N0")),
            HelpKinds.Research => Loc.F("help.kind.research", Loc.T("research.node." + request.BuildingDefinitionId), request.TargetLevel),
            HelpKinds.Repair => Loc.F("help.kind.repair", Loc.Building(request.BuildingDefinitionId)),
            _ => $"{Loc.Building(request.BuildingDefinitionId)} → {Loc.F("base.level", request.TargetLevel)}"
        };

        private void SetRank(string memberId, int rank, string role) => _commands.Dispatch(new SetMemberRankCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), MemberId = memberId, Rank = rank, Role = role
        });

        private void AnswerInvite(string allianceId, bool accept) => _commands.Dispatch(new AnswerInviteCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), AllianceId = allianceId, Accept = accept
        });

        private void AnswerApplication(string playerId, bool accept) => _commands.Dispatch(new AnswerApplicationCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), PlayerId = playerId, Accept = accept
        });

        private void Browse(string query) => _commands.Dispatch(new BrowseAlliancesCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), Query = query?.Trim() ?? string.Empty
        });
    }
}
