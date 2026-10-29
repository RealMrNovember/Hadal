using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The quest panel's part of the first-session guide (docs/34): the current guide step with its reward, the switch
    /// that brings a closed guide back, and "Coming up": what the next reactor levels open.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>The chat page on the alliance channel (the guide's alliance-chat step).</summary>
        public void OpenAllianceChat()
        {
            _chatChannel = View.MyAlliance != null ? ChatChannel.Alliance : ChatChannel.World;
            Open(Page.Chat);
        }

        private void RenderGuide(QuestStateDto state)
        {
            var guide = state.Guide;
            if (guide != null && _catalog.TryGetOnboarding(guide.Id, out var step, out _))
            {
                Header(Loc.F("guide.title", state.GuideNumber, state.GuideTotal));
                var done = guide.Progress >= guide.Target;
                var card = Card(330);
                HudIconImage(card.transform, "quest", 0.02f, 0.55f, 0.18f, 0.95f);
                CardText(card.transform, QuestText.Task(step), 36, UiKit.Text, 0.21f, 0.6f, 0.97f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                ProgressBar(card.transform, guide.Progress, guide.Target, 0.21f, 0.47f, 0.97f, 0.57f);
                CardText(card.transform, $"{Loc.T("quest.reward")}: {QuestText.Rewards(step.rewards)}", 26, UiKit.Amber, 0.04f, 0.05f, 0.68f, 0.42f, TextAnchor.MiddleLeft);
                var id = step.id;
                if (done)
                {
                    CardButton(card.transform, Loc.T("quest.claim"), true, () => _commands.Dispatch(new ClaimQuestCommand
                    {
                        CommandId = NewId(), ClientSequence = NextSequence(), QuestId = id
                    }), UiKit.Amber, 0.71f, 0.08f, 0.97f, 0.42f);
                }
                else
                {
                    CardButton(card.transform, Loc.T("guide.go"), true, () =>
                    {
                        Close();
                        OnboardingGuide.Instance?.Go();
                    }, UiKit.Primary, 0.71f, 0.08f, 0.97f, 0.42f);
                }

                // A guide the player closed comes back from here; it picks up at the server's step.
                var guideView = OnboardingGuide.Instance;
                if (guideView != null)
                {
                    var toggle = Card(110);
                    CardText(toggle.transform, Loc.T(guideView.Off ? "guide.off.note" : "guide.on.note"), 26, UiKit.Muted, 0.04f, 0.1f, 0.68f, 0.9f, TextAnchor.MiddleLeft);
                    CardButton(toggle.transform, Loc.T(guideView.Off ? "guide.show" : "guide.off"), true, () =>
                    {
                        guideView.Off = !guideView.Off;
                        Render(force: true);
                    }, UiKit.Field, 0.71f, 0.15f, 0.97f, 0.85f);
                }
            }

            RenderUpcoming();
        }

        /// <summary>"Coming up": features the next reactor levels open, so locked things have a date (docs/34 §3).</summary>
        private void RenderUpcoming()
        {
            var reactor = ReactorLevel();
            var upcoming = OnboardingRules.Upcoming(_catalog, reactor, 4);
            if (upcoming.Count == 0)
                return;

            Header(Loc.T("unlock.title"));
            foreach (var feature in upcoming)
            {
                var row = Card(100);
                var name = feature.Key switch
                {
                    "building" => Loc.Building(feature.Id),
                    "ring" => Loc.F("unlock.ring", feature.Id),
                    _ => Loc.T("unlock." + feature.Key)
                };
                CardText(row.transform, name, 30, UiKit.Text, 0.04f, 0.1f, 0.6f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
                CardText(row.transform, Loc.F("unlock.at", feature.ReactorLevel), 26, UiKit.Amber, 0.6f, 0.1f, 0.97f, 0.9f, TextAnchor.MiddleRight);
            }
        }
    }
}
