using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>Alliance page: find or found one; once in, members, help and leaving (docs/25).</summary>
    public sealed partial class SocialView
    {
        private int _allianceTab; // 0 = members, 1 = help (in an alliance); 0 = join, 1 = found (without)
        private bool _confirmLeave;
        private string _kickConfirm = string.Empty;
        private string _search = string.Empty;
        private string _newName = string.Empty;
        private string _newTag = string.Empty;
        private string _newDescription = string.Empty;

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

            foreach (var entry in View.AllianceBrowse)
            {
                var card = Card(220);
                CardText(card.transform, $"[{entry.Tag}] {entry.Name}", 36, UiKit.Text, 0.04f, 0.68f, 0.7f, 0.96f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"{Loc.F("alliance.members", entry.Members, entry.MaxMembers)}   {Loc.F("hero.power", entry.Power)}\n{Loc.F("alliance.leader", entry.LeaderName)}",
                    26, UiKit.Muted, 0.04f, 0.32f, 0.7f, 0.68f);
                if (!string.IsNullOrEmpty(entry.Description))
                    CardText(card.transform, entry.Description, 24, UiKit.Text, 0.04f, 0.03f, 0.7f, 0.32f);
                var id = entry.Id;
                var full = entry.Members >= entry.MaxMembers;
                CardButton(card.transform, full ? Loc.T("alliance.full") : Loc.T("alliance.join"), !full, () => _commands.Dispatch(new JoinAllianceCommand
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

            var wallet = Wallet();
            var card = Card(170);
            CardText(card.transform, $"{Loc.T("base.cost")}: {Cost(_catalog.AllianceCreateCost, wallet)}", 30, UiKit.Amber, 0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft);
            CardButton(card.transform, Loc.T("alliance.found"), true, () => _commands.Dispatch(new CreateAllianceCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), Name = _newName.Trim(), Tag = _newTag.Trim(), Description = _newDescription.Trim()
            }));
        }

        private void RenderMyAlliance(AllianceDto alliance)
        {
            var me = View.PlayerId;
            var leader = alliance.LeaderId == me;
            _title.text = $"[{alliance.Tag}] {alliance.Name}";
            _subtitle.text = $"{Loc.F("alliance.members", alliance.Members.Length, alliance.MaxMembers)}   {Loc.F("hero.power", alliance.Members.Sum(m => m.Power))}";

            var waiting = View.HelpRequests.Count(r => !r.HelpedByMe);
            UiKit.Choice("Tabs", _list, new[] { Loc.T("alliance.tab.members"), waiting > 0 ? $"{Loc.T("alliance.tab.help")} ({waiting})" : Loc.T("alliance.tab.help") },
                () => _allianceTab, i => { _allianceTab = i; Render(force: true); }).Row(90);

            if (_allianceTab == 1)
            {
                RenderHelp();
                return;
            }

            if (!string.IsNullOrEmpty(alliance.Description))
                UiKit.Label("Description", _list, alliance.Description, 28, UiKit.Text).Row(90);

            foreach (var member in alliance.Members)
            {
                var card = Card(150);
                var dot = member.Online ? "<color=#38D6F2>●</color>" : "<color=#3A5560>●</color>";
                var role = member.IsLeader ? $"  <color=#FFB74D>{Loc.T("alliance.role.leader")}</color>" : string.Empty;
                CardText(card.transform, $"{dot} {member.Name}{role}", 34, member.PlayerId == me ? UiKit.Accent : UiKit.Text,
                    0.04f, 0.5f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, Loc.F("hero.power", member.Power), 26, UiKit.Muted, 0.04f, 0.08f, 0.7f, 0.5f);

                if (leader && member.PlayerId != me)
                {
                    var id = member.PlayerId;
                    var confirming = _kickConfirm == id;
                    CardButton(card.transform, confirming ? Loc.T("base.confirm.yes") : Loc.T("alliance.kick"), true, () =>
                    {
                        if (_kickConfirm != id)
                        {
                            _kickConfirm = id;
                            Render(force: false);
                            return;
                        }

                        _kickConfirm = string.Empty;
                        _commands.Dispatch(new KickMemberCommand { CommandId = NewId(), ClientSequence = NextSequence(), MemberId = id });
                    }, UiKit.Danger);
                }
            }

            var leave = Card(150);
            CardText(leave.transform, _confirmLeave ? Loc.T("alliance.leave.confirm") : Loc.T("alliance.leave.title"), 30,
                _confirmLeave ? UiKit.Danger : UiKit.Text, 0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft);
            CardButton(leave.transform, _confirmLeave ? Loc.T("base.confirm.yes") : Loc.T("alliance.leave"), true, () =>
            {
                if (!_confirmLeave)
                {
                    _confirmLeave = true;
                    return;
                }

                _confirmLeave = false;
                _allianceTab = 0;
                _commands.Dispatch(new LeaveAllianceCommand { CommandId = NewId(), ClientSequence = NextSequence() });
            }, UiKit.Danger);
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

        private void Browse(string query) => _commands.Dispatch(new BrowseAlliancesCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), Query = query?.Trim() ?? string.Empty
        });
    }
}
