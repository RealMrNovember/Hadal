using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The Depth Map campaign (docs/rehber 08 §5) as a Last Asylum page: power, oxygen and today's rewarded dives in the
    /// bar, the chapters as rows that open onto their ten stages, and the dive result with its reward tiles.
    /// </summary>
    public sealed partial class SocialView
    {
        private int _chapter;
        private DiveReportDto _dive;

        public void OpenExpedition()
        {
            _chapter = 0; // the current chapter
            Open(Page.Expedition);
        }

        private string ExpeditionKey() =>
            $"{View.IncomeVersion}|{_chapter}|{ReactorLevel()}|{(Wallet().TryGetValue(ResourceType.Oxygen, out var o2) ? o2 / 50 : 0)}";

        private void RenderExpedition()
        {
            BeginPage();
            var expedition = _catalog.Expedition;
            var state = View.Expedition ?? new ExpeditionStateDto { DailyRewarded = expedition.dailyRewarded };
            var reactor = ReactorLevel();
            var oxygen = Wallet().TryGetValue(ResourceType.Oxygen, out var o2) ? o2 : 0;
            var current = Math.Min(state.Cleared / Math.Max(1, expedition.stages) + 1, expedition.chapters.Length);
            if (_chapter <= 0)
                _chapter = Math.Max(1, current);

            _title.text = Loc.T("exp.title");
            BarChips(new[]
            {
                ("power", (Func<string>)(() => CostChips.Compact(state.Power))),
                (Hud.HudIcons.Resource(ResourceType.Oxygen), WalletChip(ResourceType.Oxygen)),
                ("event_gift", (Func<string>)(() => $"{Math.Max(0, state.DailyRewarded - state.RewardedToday)}/{state.DailyRewarded}"))
            });

            if (expedition.chapters.Length == 0)
            {
                NoteRow(Loc.T("exp.none"));
                return;
            }

            foreach (var chapter in expedition.chapters)
            {
                var open = reactor >= chapter.reactor && chapter.chapter <= current;
                var done = Mathf.Clamp(state.Cleared - (chapter.chapter - 1) * expedition.stages, 0, expedition.stages);
                var chosen = chapter.chapter == _chapter;
                var row = UiFrames.Plate(_list, chosen ? UiFrames.Highlight : UiFrames.Card).Row(150);
                UiKit.Label("Name", row.transform, Loc.F("exp.chapter", chapter.chapter, Loc.T("exp.ch." + chapter.id)), UiKit.TypeScale.Body + 2,
                    open ? UiFrames.Ink : UiFrames.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0.5f, 0.78f, 0.94f);
                var status = reactor < chapter.reactor ? Loc.F("exp.locked.reactor", chapter.reactor)
                    : !open ? Loc.T("exp.locked")
                    : Loc.F("exp.progress", done, expedition.stages);
                UiKit.Label("Status", row.transform, status, UiKit.TypeScale.Caption + 2, done == expedition.stages ? UiFrames.InkGood : !open ? UiFrames.InkBad : UiFrames.InkSoft,
                    TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.08f, 0.78f, 0.5f);
                if (open)
                    UiFrames.Progress(row.transform, done, expedition.stages, done == expedition.stages ? "gold" : "green").Place(0.42f, 0.16f, 0.78f, 0.4f);

                var number = chapter.chapter;
                var toggle = UiKit.Button("Open", row.transform, chosen ? "▼" : "▶", () =>
                {
                    _chapter = number;
                    Render(force: false);
                }, open ? UiKit.Primary : UiKit.Panel, UiKit.TypeScale.Body);
                toggle.interactable = open;
                ((RectTransform)toggle.transform).Place(0.82f, 0.2f, 0.97f, 0.8f);

                if (chosen && open)
                    RenderStages(chapter, state, oxygen);
            }
        }

        /// <summary>The chosen chapter's stages: name, recommended power (green when the team is strong enough), O₂ and Dive.</summary>
        private void RenderStages(HADAL.Shared.Catalog.ExpeditionChapterData chapter, ExpeditionStateDto state, long oxygen)
        {
            var expedition = _catalog.Expedition;
            for (var stage = 1; stage <= expedition.stages; stage++)
            {
                var index = ExpeditionRules.Index(expedition, chapter.chapter, stage);
                var cleared = index < state.Cleared;
                var next = index == state.Cleared;
                var recommended = ExpeditionRules.RecommendedPower(expedition, chapter, stage);
                var boss = ExpeditionRules.IsBoss(expedition, stage);

                var row = UiFrames.Plate(_list, next ? UiFrames.Card : UiFrames.CardAlt).Row(130);
                Hud.HudIcons.Image(row.transform, boss ? "event_war" : cleared ? "g_star" : "nav_world", 0.04f, 0.18f, 0.12f, 0.82f).color =
                    cleared || next ? Color.white : new Color(1, 1, 1, 0.45f);
                var name = boss ? Loc.T("exp.boss") : Loc.F("exp.stage", stage);
                UiKit.Label("Name", row.transform, (cleared ? "✓ " : string.Empty) + name, UiKit.TypeScale.Body,
                    boss ? UiFrames.InkBad : cleared ? UiFrames.InkGood : UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.15f, 0.5f, 0.7f, 0.95f);
                var strong = state.Power >= recommended;
                var info = $"<color=#{ColorUtility.ToHtmlStringRGB(strong ? UiFrames.InkGood : UiFrames.InkBad)}>{Loc.F("exp.recommended", recommended.ToString("N0"))}</color>   " +
                           Loc.F("exp.oxygen", chapter.oxygen.ToString("N0"));
                UiKit.Label("Info", row.transform, info, UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.MiddleLeft).rectTransform.Place(0.15f, 0.06f, 0.7f, 0.5f);

                var s = stage;
                var can = (cleared || next) && oxygen >= chapter.oxygen;
                var button = UiKit.Button("Dive", row.transform, cleared ? Loc.T("exp.replay") : next ? Loc.T("exp.dive") : Loc.T("exp.locked"),
                    () => Dive(chapter.chapter, s), can ? cleared ? UiKit.Ok : UiKit.Primary : UiKit.Panel, UiKit.TypeScale.Body);
                button.interactable = can;
                ((RectTransform)button.transform).Place(0.72f, 0.16f, 0.97f, 0.84f);
            }
        }

        private void Dive(int chapter, int stage) =>
            _commands.Dispatch(new DiveStageCommand { CommandId = NewId(), ClientSequence = NextSequence(), Chapter = chapter, Stage = stage });

        /// <summary>The dive's outcome: a Victory or Defeat banner, the numbers, a hint, the rewards as tiles, and Done.</summary>
        private void RenderDiveResult()
        {
            BeginPage();
            var dive = _dive ?? new DiveReportDto();
            var chapter = ExpeditionRules.Chapter(_catalog.Expedition, dive.Chapter);
            _title.text = dive.Won ? Loc.T("exp.won") : Loc.T("exp.lost");

            var banner = UiKit.Box("Result", _list, dive.Won ? new Color(0.13f, 0.42f, 0.24f, 1f) : new Color(0.5f, 0.14f, 0.13f, 1f)).Row(220);
            banner.sprite = UiKit.Rounded;
            banner.raycastTarget = false;
            ArtText(banner.transform, dive.Won ? Loc.T("exp.won") : Loc.T("exp.lost"), UiKit.TypeScale.Display, dive.Won ? UiKit.Gold : Color.white,
                0.04f, 0.45f, 0.96f, 0.95f);
            if (chapter != null)
                ArtText(banner.transform, $"{Loc.F("exp.chapter", dive.Chapter, Loc.T("exp.ch." + chapter.id))} · " +
                                          (ExpeditionRules.IsBoss(_catalog.Expedition, dive.Stage) ? Loc.T("exp.boss") : Loc.F("exp.stage", dive.Stage)),
                    UiKit.TypeScale.Body, Color.white, 0.04f, 0.08f, 0.96f, 0.45f, TextAnchor.MiddleCenter, FontStyle.Normal);

            NoteRow(Loc.F("exp.result", dive.Power.ToString("N0"), dive.Recommended.ToString("N0"), dive.OxygenSpent.ToString("N0")), 120);
            if (dive.FirstClear)
                SectionRow(Loc.T("exp.first"));
            if (dive.Won && !dive.Rewarded)
                NoteRow(Loc.T("exp.norewards"), 130);
            if (!dive.Won)
                NoteRow(Loc.T("exp.lost.hint"), 150);

            var rewards = (dive.Rewards ?? Array.Empty<RewardEntryDto>())
                .GroupBy(r => (r.Type, r.Id))
                .Select(g => (g.Key.Type, g.Key.Id, Amount: g.Sum(r => r.Amount)))
                .ToArray();
            if (rewards.Length > 0)
                RewardGrid(_list, rewards);

            BottomButton(Loc.T("hero.pod.done"), UiKit.Primary, () =>
            {
                _title.color = Color.white;
                Open(Page.Expedition);
            });
        }
    }
}
