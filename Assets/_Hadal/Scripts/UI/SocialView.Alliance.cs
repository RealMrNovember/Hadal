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
    /// Alliance page (docs/25, docs/rehber 10): invitations, find or found one; once in, members with ranks and
    /// a manage card per member, help, and an admin tab (applications, join mode, description).
    /// </summary>
    public sealed partial class SocialView
    {
        private int _allianceTab; // in an alliance: 0 members, 1 help, 2 admin; without: 0 join, 1 found
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
                    Emblem = _newEmblem
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

            var waiting = View.HelpRequests.Count(r => !r.HelpedByMe);
            var applicants = alliance.Applicants?.Length ?? 0;
            var admin = AlliancePermissions.CanApprove(myRank);
            var tabs = new[]
            {
                Loc.T("alliance.tab.members"),
                waiting > 0 ? $"{Loc.T("alliance.tab.help")} ({waiting})" : Loc.T("alliance.tab.help"),
                applicants > 0 ? $"{Loc.T("alliance.tab.admin")} ({applicants})" : Loc.T("alliance.tab.admin")
            };
            if (!admin && _allianceTab == 2)
                _allianceTab = 0;
            UiKit.Choice("Tabs", _list, admin ? tabs : tabs.Take(2).ToArray(), () => _allianceTab, i => { _allianceTab = i; Render(force: true); }).Row(90);

            if (_allianceTab == 1)
            {
                RenderHelp();
                return;
            }

            if (_allianceTab == 2)
            {
                RenderAdmin(alliance, myRank);
                return;
            }

            var banner = Card(150);
            AvatarDisc.Emblem(UiKit.Rect("Banner", banner.transform).Place(0.02f, 0.08f, 0.18f, 0.92f), alliance.Emblem);
            CardText(banner.transform, $"[{alliance.Tag}] {alliance.Name}", 36, UiKit.Text, 0.21f, 0.5f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            CardText(banner.transform, string.IsNullOrEmpty(alliance.Description) ? Loc.F("alliance.leader", alliance.Members.FirstOrDefault(m => m.IsLeader)?.Name ?? "-")
                : alliance.Description, 24, UiKit.Muted, 0.21f, 0.05f, 0.97f, 0.5f, TextAnchor.UpperLeft);

            var lastRank = -1;
            foreach (var member in alliance.Members)
            {
                // Rank groups R5 → R1 with a count, like the genre's member lists.
                var memberRank = member.Rank > 0 ? member.Rank : member.IsLeader ? 5 : 2;
                if (memberRank != lastRank)
                {
                    lastRank = memberRank;
                    Header($"R{memberRank} · {RankName(memberRank)}  ({alliance.Members.Count(m => (m.Rank > 0 ? m.Rank : m.IsLeader ? 5 : 2) == memberRank)})");
                }

                var card = Card(150);
                var dot = member.Online ? "<color=#38D6F2>●</color>" : "<color=#3A5560>●</color>";
                var rank = member.Rank > 0 ? member.Rank : member.IsLeader ? 5 : 2;
                var role = string.IsNullOrEmpty(member.Role) ? string.Empty : " · " + Loc.T("alliance.duty." + member.Role);
                var face = UiKit.Rect("Face", card.transform).Place(0.015f, 0.1f, 0.15f, 0.9f);
                AvatarDisc.Draw(face, member.AvatarId, member.Name, member.PlayerId);
                face.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                var faceId = member.PlayerId;
                var faceName = member.Name;
                var faceAvatar = member.AvatarId;
                face.gameObject.AddComponent<Button>().onClick.AddListener(() => ProfileCard.Instance?.Show(faceId, faceName, faceAvatar));
                CardText(card.transform, $"{dot} {member.Name}", 34, member.PlayerId == me ? UiKit.Accent : UiKit.Text,
                    0.17f, 0.5f, 0.78f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"<color=#FFB74D>R{rank} {RankName(rank)}{role}</color>   {Loc.F("hero.power", member.Power)}", 24, UiKit.Muted,
                    0.17f, 0.08f, 0.78f, 0.5f);

                if (member.PlayerId == me)
                    continue;
                var id = member.PlayerId;
                CardButton(card.transform, _manage == id ? "▲" : "⋯", true, () =>
                {
                    _manage = _manage == id ? string.Empty : id;
                    _kickConfirm = _transferConfirm = string.Empty;
                    Render(force: false);
                }, UiKit.Field, 0.8f, 0.2f, 0.97f, 0.8f);

                if (_manage == id)
                    RenderManage(member, rank, myRank, alliance);
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

        /// <summary>What the viewer may do to one member: message, rank up or down, duty, hand over, remove.</summary>
        private void RenderManage(AllianceMemberDto member, int rank, int myRank, AllianceDto alliance)
        {
            var id = member.PlayerId;
            var actions = Card(120);
            actions.color = new Color(0.03f, 0.1f, 0.14f, 1f);
            var slots = new System.Collections.Generic.List<(string Label, bool Enabled, Action Click, Color? Fill)>
            {
                (Loc.T("alliance.message"), true, () => OpenPrivateChat(id, member.Name), UiKit.Field)
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
        private void RenderAdmin(AllianceDto alliance, int myRank)
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
            var open = View.HelpRequests.Where(r => !r.HelpedByMe).ToList();
            var all = Card(150);
            CardText(all.transform, open.Count > 0 ? Loc.F("alliance.help.waiting", open.Count) : Loc.T("alliance.help.none"), 30, UiKit.Text,
                0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft);
            CardButton(all.transform, Loc.T("alliance.help.all"), open.Count > 0, () => _commands.Dispatch(new HelpAllCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence()
            }));

            Note(Loc.T("alliance.help.how"));

            foreach (var request in View.HelpRequests)
            {
                var card = Card(130);
                CardText(card.transform, $"{request.OwnerName}: {Loc.Building(request.BuildingDefinitionId)} → {Loc.F("base.level", request.TargetLevel)}", 30,
                    UiKit.Text, 0.04f, 0.5f, 0.96f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"{Loc.F("alliance.help.count", request.Helps, request.MaxHelps)}{(request.HelpedByMe ? "  ✓" : string.Empty)}", 26,
                    request.HelpedByMe ? UiKit.Accent : UiKit.Muted, 0.04f, 0.05f, 0.96f, 0.5f);
            }
        }

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
