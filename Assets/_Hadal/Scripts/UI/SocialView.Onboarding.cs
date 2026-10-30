using System;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The quest page's part of the first-session guide (docs/34): the current guide step as a task row with its reward,
    /// the switch that brings a closed guide back, and "Coming up": what the next reactor levels open.
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
            if (guide == null || !_catalog.TryGetOnboarding(guide.Id, out var step, out _))
                return;

            SectionRow(Loc.F("guide.title", state.GuideNumber, state.GuideTotal));
            var done = guide.Progress >= guide.Target;
            var id = step.id;
            Action click = done
                ? () => ClaimQuest(id)
                : () =>
                {
                    Close();
                    OnboardingGuide.Instance?.Go();
                };
            UiFrames.QuestRow(_list, TaskTitle(step, guide.Progress, guide.Target), RewardTiles(step.rewards).Take(5).ToArray(),
                done ? Loc.T("quest.claim") : Loc.T("guide.go"), done, click).Row(QuestRowHeight);

            // A guide the player closed comes back from here; it picks up at the server's step.
            var guideView = OnboardingGuide.Instance;
            if (guideView == null)
                return;
            var row = PlateRow(120);
            UiKit.Label("Text", row.transform, Loc.T(guideView.Off ? "guide.off.note" : "guide.on.note"), UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.MiddleLeft)
                .rectTransform.Place(0.04f, 0.06f, 0.68f, 0.94f);
            var toggle = UiKit.Button("Toggle", row.transform, Loc.T(guideView.Off ? "guide.show" : "guide.off"), () =>
            {
                guideView.Off = !guideView.Off;
                Render(force: true);
            }, UiKit.Primary, UiKit.TypeScale.Body);
            ((RectTransform)toggle.transform).Place(0.71f, 0.14f, 0.97f, 0.86f);
        }

        /// <summary>"Coming up": features the next reactor levels open, so locked things have a date (docs/34 §3).</summary>
        private void RenderUpcoming()
        {
            var upcoming = OnboardingRules.Upcoming(_catalog, ReactorLevel(), 4);
            if (upcoming.Count == 0)
                return;

            SectionRow(Loc.T("unlock.title"));
            foreach (var feature in upcoming)
            {
                var name = feature.Key switch
                {
                    "building" => Loc.Building(feature.Id),
                    "ring" => Loc.F("unlock.ring", feature.Id),
                    _ => Loc.T("unlock." + feature.Key)
                };
                UiFrames.BonusRow(_list, name, Loc.F("unlock.at", feature.ReactorLevel)).Row(96);
            }
        }
    }
}
