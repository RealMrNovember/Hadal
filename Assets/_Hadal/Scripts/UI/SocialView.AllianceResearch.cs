using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance research (docs/31 step 6): Development and War tabs; the focus card with today's donations and the
    /// Donate button; every node with its level, bonus and progress, and for officers "Make focus".
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabResearch = 9;

        private int _researchTab; // 0 development, 1 war

        private void RenderAllianceResearch(int myRank)
        {
            var research = View.AllianceResearch ?? new AllianceResearchDto();
            var nodes = research.Nodes ?? Array.Empty<AllianceResearchNodeDto>();
            var focus = AllianceResearchRules.Find(research.Focus) ?? AllianceResearchRules.Nodes[0];
            var focusState = nodes.FirstOrDefault(n => n.Id == focus.Id) ?? new AllianceResearchNodeDto { Id = focus.Id };

            // The focus: what donations go to, today's donations left and the price of one.
            var card = Card(260);
            card.color = new Color(0.06f, 0.16f, 0.24f, 1f);
            HudIconImage(card.transform, "left_research", 0.02f, 0.55f, 0.13f, 0.95f);
            CardText(card.transform, Loc.F("alliance.research.focus", Loc.T("alliance.research.node." + focus.Id), focusState.Level, AllianceResearchRules.MaxLevel),
                30, UiKit.Accent, 0.15f, 0.68f, 0.97f, 0.97f, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (focusState.Needed > 0)
                ProgressBar(card.transform, focusState.Progress, focusState.Needed, 0.15f, 0.54f, 0.97f, 0.66f);
            var cost = AllianceResearchRules.DonationCost(focusState.Level);
            CardText(card.transform, Loc.F("alliance.research.donate.info", cost.ToString("N0"), Loc.Resource(focus.Resource.ToString()),
                    AllianceResearchRules.PointsPerDonation, research.DonationsLeft, AllianceResearchRules.DonationsPerDay),
                22, UiKit.Text, 0.04f, 0.26f, 0.97f, 0.52f, TextAnchor.MiddleLeft);
            CardText(card.transform, Loc.F("alliance.research.mine", research.MyDonated.ToString("N0")), 22, UiKit.Muted, 0.04f, 0.04f, 0.6f, 0.26f, TextAnchor.MiddleLeft);
            CardButton(card.transform, Loc.T("alliance.research.donate"), research.DonationsLeft > 0 && focusState.Needed > 0,
                () => _commands.Dispatch(new DonateAllianceResearchCommand { CommandId = NewId(), ClientSequence = NextSequence() }),
                UiKit.Gold, 0.62f, 0.04f, 0.97f, 0.26f);

            UiKit.Choice("ResearchTabs", _list, new[] { Loc.T("alliance.research.tab.development"), Loc.T("alliance.research.tab.war") },
                () => _researchTab, i => { _researchTab = i; Render(force: true); }).Row(90);
            var tab = _researchTab == 1 ? AllianceResearchRules.War : AllianceResearchRules.Development;
            var officer = AlliancePermissions.CanEditAnnouncement(myRank);
            foreach (var node in AllianceResearchRules.Nodes.Where(n => n.Tab == tab))
            {
                var state = nodes.FirstOrDefault(n => n.Id == node.Id) ?? new AllianceResearchNodeDto { Id = node.Id };
                var row = Card(170);
                if (node.Id == focus.Id)
                    row.color = new Color(0.12f, 0.2f, 0.12f, 1f);
                CardText(row.transform, $"{Loc.T("alliance.research.node." + node.Id)}  {state.Level}/{AllianceResearchRules.MaxLevel}", 28, UiKit.Text,
                    0.03f, 0.6f, 0.7f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                CardText(row.transform, Loc.F("alliance.research.effect." + node.Id, AllianceResearchRules.Bonus(node, state.Level),
                        AllianceResearchRules.Bonus(node, Math.Min(AllianceResearchRules.MaxLevel, state.Level + 1))),
                    22, UiKit.Muted, 0.03f, 0.36f, 0.7f, 0.6f, TextAnchor.MiddleLeft);
                if (state.Needed > 0)
                    ProgressBar(row.transform, state.Progress, state.Needed, 0.03f, 0.1f, 0.7f, 0.3f);
                else
                    CardText(row.transform, Loc.T("research.max"), 22, UiKit.Amber, 0.03f, 0.08f, 0.7f, 0.32f, TextAnchor.MiddleLeft);

                if (node.Id == focus.Id)
                    CardText(row.transform, Loc.T("alliance.research.isfocus"), 24, UiKit.Ok, 0.72f, 0.2f, 0.97f, 0.8f, TextAnchor.MiddleCenter, FontStyle.Bold);
                else if (officer && state.Needed > 0)
                {
                    var id = node.Id;
                    CardButton(row.transform, Loc.T("alliance.research.makefocus"), true,
                        () => _commands.Dispatch(new SetResearchFocusCommand { CommandId = NewId(), ClientSequence = NextSequence(), NodeId = id }),
                        UiKit.Field, 0.72f, 0.22f, 0.97f, 0.78f);
                }
            }
        }
    }
}
