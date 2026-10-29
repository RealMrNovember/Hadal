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
    /// leads to members with ranks and a manage card per member, help, applications and settings.
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

        private static string RankName(int rank) => Loc.T("alliance.rank." + Mathf.Clamp(rank, 1, 5));

        private void RenderAlliance()
        {
            var alliance = View.MyAlliance;
            if (alliance == null)
                RenderNoAlliance();
            else
                RenderMyAlliance(alliance);
        }

        private void RenderNoAlliance()
        {
            _title.text = Loc.T("social.alliance");
            _subtitle.text = Loc.T("alliance.none");

            // Invitations come first: one tap and the player is in.
            foreach (var invite in View.AllianceInvites.Invites ?? Array.Empty<AllianceInviteDto>())
            {
                var card = Card(170);
                card.color = new Color(0.05f, 0.22f, 0.3f, 1f);
                AvatarDisc.Emblem(UiKit.Rect("Banner", card.transform).Place(0.02f, 0.12f, 0.15f, 0.88f), invite.Emblem);
                CardText(card.transform, $"[{invite.Tag}] {invite.Name}", 34, UiKit.Accent, 0.17f, 0.55f, 0.6f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"{Loc.F("alliance.invite.from", invite.InviterName)}\n{Loc.F("alliance.members", invite.Members, invite.MaxMembers)}", 24,
                    UiKit.Muted, 0.17f, 0.05f, 0.6f, 0.55f);
                var id = invite.AllianceId;
                CardButton(card.transform, Loc.T("alliance.invite.accept"), true, () => AnswerInvite(id, true), null, 0.62f, 0.52f, 0.97f, 0.92f);
                CardButton(card.transform, Loc.T("alliance.invite.decline"), true, () => AnswerInvite(id, false), UiKit.Field, 0.62f, 0.08f, 0.97f, 0.48f);
            }

            UiKit.Choice("Tabs", _list, new[] { Loc.T("alliance.tab.join"), Loc.T("alliance.tab.found") },
                () => _allianceTab, i => { _allianceTab = i; Render(force: true); }).Row(90);

            if (_allianceTab == 1)
            {
                RenderFound();
                return;
            }

            var searchRow = UiKit.Rect("Search", _list).Row(100);
            var search = UiKit.Input("Query", searchRow, Loc.T("alliance.search"), characterLimit: 20);
            ((RectTransform)search.transform).Place(0, 0.05f, 0.7f, 0.95f);
            search.text = _search;
            search.onValueChanged.AddListener(v => _search = v);
            var go = UiKit.Button("Go", searchRow, Loc.T("alliance.search.go"), () => Browse(_search), UiKit.Accent, 34);
            ((RectTransform)go.transform).Place(0.72f, 0.05f, 1f, 0.95f);

            if (View.AllianceBrowse.Length == 0)
            {
                Note(Loc.T("alliance.browse.empty"));
                return;
            }

            var applied = View.AllianceInvites.Applied ?? Array.Empty<string>();
            foreach (var entry in View.AllianceBrowse)
            {
                var card = Card(220);
                AvatarDisc.Emblem(UiKit.Rect("Banner", card.transform).Place(0.02f, 0.5f, 0.15f, 0.94f), entry.Emblem);
                CardText(card.transform, $"[{entry.Tag}] {entry.Name}", 36, UiKit.Text, 0.17f, 0.68f, 0.7f, 0.96f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"{Loc.F("alliance.members", entry.Members, entry.MaxMembers)}   {Loc.F("hero.power", entry.Power)}\n" +
                                         $"{Loc.F("alliance.leader", entry.LeaderName)}   {Loc.T("alliance.mode." + entry.JoinMode)}",
                    26, UiKit.Muted, 0.17f, 0.32f, 0.7f, 0.68f);
                if (!string.IsNullOrEmpty(entry.Description))
                    CardText(card.transform, entry.Description, 24, UiKit.Text, 0.04f, 0.03f, 0.7f, 0.32f);
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
                }));
            }
        }

        private void RenderFound()
        {
            var rules = _catalog.Alliance;
            UiKit.Label("NameLabel", _list, Loc.F("alliance.name.hint", rules.nameMin, rules.nameMax), 28, UiKit.Muted).Row(50);
            var name = UiKit.Input("Name", _list, Loc.T("alliance.name"), characterLimit: rules.nameMax).Row(100);
            name.text = _newName;
            name.onValueChanged.AddListener(v => _newName = v);

            UiKit.Label("TagLabel", _list, Loc.F("alliance.tag.hint", rules.tagMin, rules.tagMax), 28, UiKit.Muted).Row(50);
            var tag = UiKit.Input("Tag", _list, Loc.T("alliance.tag"), characterLimit: rules.tagMax).Row(100);
            tag.text = _newTag;
            tag.onValueChanged.AddListener(v => _newTag = v);

            UiKit.Label("DescLabel", _list, Loc.T("alliance.description"), 28, UiKit.Muted).Row(50);
            var description = UiKit.Input("Description", _list, Loc.T("alliance.description.hint"), characterLimit: 200).Row(100);
            description.text = _newDescription;
            description.onValueChanged.AddListener(v => _newDescription = v);

            UiKit.Label("EmblemLabel", _list, Loc.T("alliance.emblem"), 28, UiKit.Muted).Row(50);
            EmblemPicker(() => _newEmblem, e => _newEmblem = e);

            // The price and, before the tap, what is wrong: short name or tag, or resources missing.
            var wallet = Wallet();
            var missing = _catalog.AllianceCreateCost.FirstOrDefault(c => (wallet.TryGetValue(c.Key, out var have) ? have : 0) < c.Value);
            var problem = !SocialRules.IsValidName(_newName.Trim(), rules) ? Loc.F("alliance.name.hint", rules.nameMin, rules.nameMax)
                : !SocialRules.IsValidTag(SocialRules.NormalizeTag(_newTag), rules) ? Loc.F("alliance.tag.hint", rules.tagMin, rules.tagMax)
                : missing.Value > 0 ? Loc.F("alliance.found.short", Loc.Resource(missing.Key.ToString()))
                : string.Empty;
            var card = Card(200);
            CardText(card.transform, $"{Loc.T("base.cost")}: {Cost(_catalog.AllianceCreateCost, wallet)}", 30, UiKit.Amber, 0.04f, 0.5f, 0.68f, 0.95f, TextAnchor.MiddleLeft);
            CardText(card.transform, problem.Length > 0 ? problem : Loc.T("alliance.found.ready"), 22, problem.Length > 0 ? UiKit.Danger : UiKit.Ok,
                0.04f, 0.05f, 0.68f, 0.5f, TextAnchor.MiddleLeft);
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
            });
        }

        private void RenderMyAlliance(AllianceDto alliance)
        {
            var me = View.PlayerId;
            var myRank = View.MyRank();
            _title.text = $"[{alliance.Tag}] {alliance.Name}";
            _subtitle.text = $"{Loc.F("alliance.members", alliance.Members.Length, alliance.MaxMembers)}   {Loc.F("hero.power", alliance.Members.Sum(m => m.Power))}   " +
                             $"{RankName(myRank)}";

            switch (_allianceTab)
            {
                case AllianceTabHelp:
                    BackToHub();
                    RenderHelp();
                    return;
                case AllianceTabMembers:
                    BackToHub();
                    RenderMembers(alliance, me, myRank);
                    return;
                case AllianceTabApplications when AlliancePermissions.CanApprove(myRank):
                    BackToHub();
                    RenderApplications(alliance);
                    return;
                case AllianceTabResearch:
                    BackToHub();
                    RenderAllianceResearch(myRank);
                    return;
                case AllianceTabGifts:
                    BackToHub();
                    RenderAllianceGifts();
                    return;
                case AllianceTabShop:
                    BackToHub();
                    RenderAllianceShop();
                    return;
                case AllianceTabLog:
                    BackToHub();
                    RenderAllianceLog(AllianceLogKinds.Log);
                    return;
                case AllianceTabBattleLog:
                    BackToHub();
                    RenderAllianceLog(AllianceLogKinds.Battle);
                    return;
                case AllianceTabSettings when AlliancePermissions.CanEditAnnouncement(myRank):
                    BackToHub();
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
                return $"<color=#5BE37A>{Loc.T("world.online")}</color>";
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
        /// collapsible rank groups R4 → R1 with counts (R4 against its seats), each member with portrait, level, power and
        /// online / last seen, and the manage card (message, mail, rank, duty, hand over, remove) under the row.
        /// </summary>
        private void RenderMembers(AllianceDto alliance, string me, int myRank)
        {
            var leader = alliance.Members.FirstOrDefault(m => m.IsLeader);
            if (leader != null)
            {
                Header(Loc.T("alliance.leader.title"));
                MemberRow(leader, alliance, me, myRank, 190, new Color(0.18f, 0.14f, 0.05f, 1f));
            }

            // Officer duties: one titled slot each, the officer holding it or empty.
            Header(Loc.T("alliance.titles"));
            var duties = AlliancePermissions.Roles.Where(r => r.Length > 0).ToArray();
            var row = UiKit.Rect("Titles", _list).Row(230);
            for (var i = 0; i < duties.Length; i++)
            {
                var duty = duties[i];
                var holder = alliance.Members.FirstOrDefault(m => m.Role == duty && RankOfMember(m) == AlliancePermissions.Officer);
                var slot = UiKit.Box("Title", row, UiKit.Field);
                slot.rectTransform.Place(i / (float)duties.Length + 0.005f, 0.02f, (i + 1) / (float)duties.Length - 0.005f, 0.98f);
                var face = UiKit.Rect("Face", slot.transform).Place(0.2f, 0.36f, 0.8f, 0.94f);
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
                    UiKit.Label("Empty", face, "+", 60, UiKit.Muted, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
                }

                CardText(slot.transform, Loc.T("alliance.duty." + duty), 24, UiKit.Amber, 0.02f, 0.18f, 0.98f, 0.36f, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .raycastTarget = false;
                var label = CardText(slot.transform, holder?.Name ?? Loc.T("alliance.duty.empty"), 20, holder != null ? UiKit.Text : UiKit.Muted,
                    0.02f, 0.01f, 0.98f, 0.18f, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                label.supportRichText = false;
            }

            // Rank groups R4 → R1; a tap folds or opens one.
            for (var rank = AlliancePermissions.Officer; rank >= AlliancePermissions.Recruit; rank--)
            {
                var group = alliance.Members.Where(m => !m.IsLeader && RankOfMember(m) == rank).ToList();
                var open = !_collapsedRanks.Contains(rank);
                var count = rank == AlliancePermissions.Officer ? $"{group.Count}/{AlliancePermissions.MaxOfficers}" : group.Count.ToString();
                var head = Card(90);
                head.color = new Color(0.04f, 0.1f, 0.15f, 1f);
                var groupRank = rank;
                var toggle = head.gameObject.AddComponent<Button>();
                toggle.targetGraphic = head;
                toggle.onClick.AddListener(() =>
                {
                    if (!_collapsedRanks.Remove(groupRank))
                        _collapsedRanks.Add(groupRank);
                    Render(force: false);
                });
                CardText(head.transform, $"{(open ? "▼" : "▶")}  R{rank} · {RankName(rank)}", 30, UiKit.Accent, 0.03f, 0.05f, 0.7f, 0.95f,
                    TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
                CardText(head.transform, count, 30, UiKit.Text, 0.7f, 0.05f, 0.97f, 0.95f, TextAnchor.MiddleRight, FontStyle.Bold).raycastTarget = false;
                if (!open)
                    continue;
                foreach (var member in group)
                    MemberRow(member, alliance, me, myRank, 150, null);
            }

            var leave = Card(150);
            CardText(leave.transform, _confirmLeave ? Loc.T("alliance.leave.confirm") : Loc.T("alliance.leave.title"), 30,
                _confirmLeave ? UiKit.Danger : UiKit.Text, 0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft);
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
            var card = Card(height);
            if (fill.HasValue)
                card.color = fill.Value;
            var rank = RankOfMember(member);
            var face = UiKit.Rect("Face", card.transform).Place(0.015f, 0.1f, 0.15f, 0.9f);
            AvatarDisc.Draw(face, member.AvatarId, member.Name, member.PlayerId);
            face.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var faceId = member.PlayerId;
            var faceName = member.Name;
            var faceAvatar = member.AvatarId;
            face.gameObject.AddComponent<Button>().onClick.AddListener(() => ProfileCard.Instance?.Show(faceId, faceName, faceAvatar));

            var name = CardText(card.transform, member.Name, 32, member.PlayerId == me ? UiKit.Accent : UiKit.Text, 0.17f, 0.52f, 0.62f, 0.95f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            name.supportRichText = false;
            var role = string.IsNullOrEmpty(member.Role) ? string.Empty : " · " + Loc.T("alliance.duty." + member.Role);
            var level = member.Level > 0 ? Loc.F("world.level", member.Level) + "   " : string.Empty;
            CardText(card.transform, $"<color=#FFB74D>R{rank} {RankName(rank)}{role}</color>\n{level}{Loc.F("hero.power", member.Power.ToString("N0"))}", 22,
                UiKit.Muted, 0.17f, 0.04f, 0.62f, 0.54f, TextAnchor.MiddleLeft);
            CardText(card.transform, LastSeen(member), 22, UiKit.Muted, 0.6f, 0.52f, 0.79f, 0.95f, TextAnchor.MiddleRight);

            if (member.PlayerId == me)
                return;
            var id = member.PlayerId;
            CardButton(card.transform, _manage == id ? "▲" : "⋯", true, () =>
            {
                _manage = _manage == id ? string.Empty : id;
                _kickConfirm = _transferConfirm = string.Empty;
                Render(force: false);
            }, UiKit.Field, 0.81f, 0.2f, 0.97f, 0.8f);

            if (_manage == id)
                RenderManage(member, rank, myRank, alliance);
        }

        /// <summary>What the viewer may do to one member: message, rank up or down, duty, hand over, remove.</summary>
        private void RenderManage(AllianceMemberDto member, int rank, int myRank, AllianceDto alliance)
        {
            var id = member.PlayerId;
            var actions = Card(120);
            actions.color = new Color(0.03f, 0.1f, 0.14f, 1f);
            var slots = new System.Collections.Generic.List<(string Label, bool Enabled, Action Click, Color? Fill)>
            {
                (Loc.T("alliance.message"), true, () => OpenPrivateChat(id, member.Name), UiKit.Field),
                (Loc.T("mail.send"), true, () => OpenMailCompose(id, member.Name), UiKit.Field)
            };

            var officers = alliance.Members.Count(m => m.Rank == AlliancePermissions.Officer);
            if (AlliancePermissions.CanSetRank(myRank, rank, rank + 1))
                slots.Add((Loc.T("alliance.promote"), rank + 1 != AlliancePermissions.Officer || officers < AlliancePermissions.MaxOfficers,
                    () => SetRank(id, rank + 1, null), null));
            if (rank > AlliancePermissions.Recruit && AlliancePermissions.CanSetRank(myRank, rank, rank - 1))
                slots.Add((Loc.T("alliance.demote"), true, () => SetRank(id, rank - 1, null), UiKit.Field));
            if (rank == AlliancePermissions.Officer && AlliancePermissions.CanSetRole(myRank))
            {
                var roles = AlliancePermissions.Roles;
                var next = roles[(Array.IndexOf(roles, member.Role ?? string.Empty) + 1) % roles.Length];
                slots.Add((Loc.T("alliance.duty"), true, () => SetRank(id, rank, next), UiKit.Field));
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
            var danger = Card(110);
            danger.color = new Color(0.03f, 0.1f, 0.14f, 1f);
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
                }, UiKit.Amber, 0.02f, 0.12f, 0.48f, 0.88f);
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

        /// <summary>Applications (R3+) and, for the leader, who may join and the description.</summary>
        private void RenderApplications(AllianceDto alliance)
        {
            Header(Loc.T("alliance.applications"));
            var applicants = alliance.Applicants ?? Array.Empty<AllianceApplicantDto>();
            if (applicants.Length == 0)
                Note(Loc.T("alliance.applications.none"));
            foreach (var a in applicants)
            {
                var card = Card(140);
                CardText(card.transform, a.Name, 32, UiKit.Text, 0.04f, 0.5f, 0.6f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, Loc.F("hero.power", a.Power), 24, UiKit.Muted, 0.04f, 0.05f, 0.6f, 0.5f);
                var id = a.PlayerId;
                CardButton(card.transform, Loc.T("alliance.invite.accept"), true, () => AnswerApplication(id, true), null, 0.62f, 0.52f, 0.97f, 0.92f);
                CardButton(card.transform, Loc.T("alliance.invite.decline"), true, () => AnswerApplication(id, false), UiKit.Field, 0.62f, 0.08f, 0.97f, 0.48f);
            }

            Note(Loc.T("alliance.invite.how"));
        }

        /// <summary>Officers write the announcement (the leader also the language); the leader edits the rest.</summary>
        private void RenderSettings(AllianceDto alliance, int myRank)
        {
            RenderAnnouncementEditor(alliance, myRank);
            if (!AlliancePermissions.CanEditSettings(myRank))
                return;

            Header(Loc.T("alliance.settings"));
            if (_editMode < 0)
                _editMode = alliance.JoinMode;
            _editDescription ??= alliance.Description ?? string.Empty;
            UiKit.Choice("Mode", _list, new[] { Loc.T("alliance.mode.0"), Loc.T("alliance.mode.1"), Loc.T("alliance.mode.2") },
                () => _editMode, i => { _editMode = i; Render(force: false); }).Row(90);
            UiKit.Label("ModeHint", _list, Loc.T("alliance.mode.hint." + _editMode), 24, UiKit.Muted).Row(70);
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
                UiKit.Label("NameLabel", _list, Loc.F("alliance.name.hint", rules.nameMin, rules.nameMax), 24, UiKit.Muted).Row(44);
                var name = UiKit.Input("Name", _list, Loc.T("alliance.name"), characterLimit: rules.nameMax).Row(100);
                name.text = _editName;
                name.onValueChanged.AddListener(v => _editName = v);
                UiKit.Label("TagLabel", _list, Loc.F("alliance.tag.hint", rules.tagMin, rules.tagMax), 24, UiKit.Muted).Row(44);
                var tag = UiKit.Input("Tag", _list, Loc.T("alliance.tag"), characterLimit: rules.tagMax).Row(100);
                tag.text = _editTag;
                tag.onValueChanged.AddListener(v => _editTag = v);
                UiKit.Label("EmblemLabel", _list, Loc.T("alliance.emblem"), 24, UiKit.Muted).Row(44);
                EmblemPicker(() => _editEmblem, e => _editEmblem = e);
            }

            var save = Card(110);
            CardButton(save.transform, Loc.T("alliance.save"), true, () =>
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
            }, null, 0.25f, 0.1f, 0.75f, 0.9f);
        }

        /// <summary>Banner picker: the icons in a row, the colours in a row, a preview on the left.</summary>
        private void EmblemPicker(Func<string> current, Action<string> set)
        {
            var (icon, color) = AvatarDisc.EmblemParts(current());
            var card = Card(230);
            AvatarDisc.Emblem(UiKit.Rect("Preview", card.transform).Place(0.02f, 0.15f, 0.2f, 0.85f), $"{icon}:{color}");
            var icons = SocialRules.EmblemIcons.Length;
            for (var i = 0; i < icons; i++)
            {
                var pick = i;
                var cell = UiKit.Box("Icon", card.transform, i == icon ? UiKit.Accent : UiKit.Field);
                cell.rectTransform.Place(0.23f + i * (0.76f / icons), 0.53f, 0.23f + (i + 1) * (0.76f / icons) - 0.008f, 0.95f);
                Hud.HudIcons.Image(cell.transform, SocialRules.EmblemIcons[i], 0.15f, 0.15f, 0.85f, 0.85f).raycastTarget = false;
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { set($"{pick}:{AvatarDisc.EmblemParts(current()).Color}"); Render(force: true); });
            }

            var colors = SocialRules.EmblemColors.Length;
            for (var i = 0; i < colors; i++)
            {
                var pick = i;
                ColorUtility.TryParseHtmlString("#" + SocialRules.EmblemColors[i], out var tint);
                var cell = UiKit.Box("Colour", card.transform, tint);
                cell.rectTransform.Place(0.23f + i * (0.76f / colors), 0.05f, 0.23f + (i + 1) * (0.76f / colors) - 0.008f, 0.45f);
                if (i == color)
                {
                    var outline = cell.gameObject.AddComponent<Outline>();
                    outline.effectColor = Color.white;
                    outline.effectDistance = new Vector2(4, -4);
                }

                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { set($"{AvatarDisc.EmblemParts(current()).Icon}:{pick}"); Render(force: true); });
            }
        }

        private void RenderHelp()
        {
            // Today's alliance coins from helping, like the genre's bar on top of the help list.
            var coins = View.AllianceCoins ?? new AllianceCoinsDto();
            var cap = Math.Max(1, coins.DailyHelpCoins > 0 ? coins.DailyHelpCoins : AllianceShopRules.DailyHelpCoins);
            var bar = Card(120);
            CardText(bar.transform, Loc.F("alliance.coins.help", AllianceShopRules.CoinsPerHelp), 24, UiKit.Text, 0.04f, 0.55f, 0.96f, 0.95f, TextAnchor.MiddleLeft);
            ProgressBar(bar.transform, coins.HelpCoinsToday, cap, 0.04f, 0.12f, 0.96f, 0.45f);

            var open = View.HelpRequests.Where(r => !r.HelpedByMe).ToList();
            var all = Card(150);
            CardText(all.transform, open.Count > 0 ? Loc.F("alliance.help.open", open.Count) : Loc.T("alliance.help.none"), 30, UiKit.Text,
                0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft);
            CardButton(all.transform, Loc.T("alliance.help.all"), open.Count > 0, () => _commands.Dispatch(new HelpAllCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence()
            }));

            Note(Loc.T("alliance.help.rule"));

            foreach (var request in View.HelpRequests)
            {
                var card = Card(130);
                CardText(card.transform, $"{request.OwnerName}: {HelpJobText(request)}", 30,
                    UiKit.Text, 0.04f, 0.5f, 0.96f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"{Loc.F("alliance.help.count", request.Helps, request.MaxHelps)}{(request.HelpedByMe ? "  ✓" : string.Empty)}", 26,
                    request.HelpedByMe ? UiKit.Accent : UiKit.Muted, 0.04f, 0.05f, 0.96f, 0.5f);
            }
        }

        /// <summary>What a help request is for: a building's next level, a healing batch or a research.</summary>
        private static string HelpJobText(HelpRequestDto request) => HelpKinds.Normalize(request.Kind) switch
        {
            HelpKinds.Heal => Loc.F("help.kind.heal", request.TargetLevel.ToString("N0")),
            HelpKinds.Research => Loc.F("help.kind.research", Loc.T("research.node." + request.BuildingDefinitionId), request.TargetLevel),
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
