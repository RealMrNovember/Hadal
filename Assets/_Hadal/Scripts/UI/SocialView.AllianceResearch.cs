using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance research (docs/31 step 6): the focus card with its bar, today's donations and Donate, then Development
    /// and War tabs and a row per node with its level, bonus and progress, and for officers "Make focus".
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
            var card = InkCard(270, UiFrames.Highlight);
            HudIconImage(card.transform, "left_research", 0.02f, 0.55f, 0.15f, 0.95f);
            Ink(card.transform, Loc.F("alliance.research.focus", Loc.T("alliance.research.node." + focus.Id), focusState.Level, AllianceResearchRules.MaxLevel),
                UiKit.TypeScale.Body + 2, UiFrames.Ink, 0.17f, 0.72f, 0.97f, 0.97f, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (focusState.Needed > 0)
                ProgressBar(card.transform, focusState.Progress, focusState.Needed, 0.17f, 0.56f, 0.97f, 0.7f);
            var cost = AllianceResearchRules.DonationCost(focusState.Level);
            Ink(card.transform, Loc.F("alliance.research.donate.info", cost.ToString("N0"), Loc.Resource(focus.Resource.ToString()),
                    AllianceResearchRules.PointsPerDonation, research.DonationsLeft, AllianceResearchRules.DonationsPerDay),
                UiKit.TypeScale.Caption, UiFrames.Ink, 0.04f, 0.3f, 0.97f, 0.54f);
            Ink(card.transform, Loc.F("alliance.research.mine", research.MyDonated.ToString("N0")), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.04f, 0.04f, 0.6f, 0.28f);
            CardButton(card.transform, Loc.T("alliance.research.donate"), research.DonationsLeft > 0 && focusState.Needed > 0,
                () => _commands.Dispatch(new DonateAllianceResearchCommand { CommandId = NewId(), ClientSequence = NextSequence() }),
                UiKit.Ok, 0.62f, 0.04f, 0.97f, 0.28f);

            UiFrames.Tabs(_list, new[] { Loc.T("alliance.research.tab.development"), Loc.T("alliance.research.tab.war") }, Mathf.Clamp(_researchTab, 0, 1),
                i => { _researchTab = i; Render(force: true); }).Row(96);
            var tab = _researchTab == 1 ? AllianceResearchRules.War : AllianceResearchRules.Development;
            var officer = AlliancePermissions.CanSetResearchFocus(myRank);
            foreach (var node in AllianceResearchRules.Nodes.Where(n => n.Tab == tab))
            {
                var state = nodes.FirstOrDefault(n => n.Id == node.Id) ?? new AllianceResearchNodeDto { Id = node.Id };
                var isFocus = node.Id == focus.Id;
                var row = InkCard(170, isFocus ? UiFrames.RankMine : (Color?)null);
                Ink(row.transform, $"{Loc.T("alliance.research.node." + node.Id)}  {state.Level}/{AllianceResearchRules.MaxLevel}", UiKit.TypeScale.Body, UiFrames.Ink,
                    0.03f, 0.62f, 0.7f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ink(row.transform, Loc.F("alliance.research.effect." + node.Id, AllianceResearchRules.Bonus(node, state.Level),
                        AllianceResearchRules.Bonus(node, Math.Min(AllianceResearchRules.MaxLevel, state.Level + 1))),
                    UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.03f, 0.36f, 0.7f, 0.62f);
                if (state.Needed > 0)
                    ProgressBar(row.transform, state.Progress, state.Needed, 0.03f, 0.1f, 0.7f, 0.3f);
                else
                    Ink(row.transform, Loc.T("research.max"), UiKit.TypeScale.Caption, UiFrames.InkGood, 0.03f, 0.08f, 0.7f, 0.32f, TextAnchor.MiddleLeft, FontStyle.Bold);

                if (isFocus)
                    Ink(row.transform, Loc.T("alliance.research.isfocus"), UiKit.TypeScale.Body, UiFrames.InkGood, 0.72f, 0.2f, 0.97f, 0.8f, TextAnchor.MiddleCenter, FontStyle.Bold);
                else if (officer && state.Needed > 0)
                {
                    var id = node.Id;
                    CardButton(row.transform, Loc.T("alliance.research.makefocus"), true,
                        () => _commands.Dispatch(new SetResearchFocusCommand { CommandId = NewId(), ClientSequence = NextSequence(), NodeId = id }),
                        UiKit.Primary, 0.72f, 0.22f, 0.97f, 0.78f);
                }
            }
        }
    }
}
