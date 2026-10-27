using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>The Depth Map campaign (docs/rehber 08 §5): chapters, their ten stages and the dive result.</summary>
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
            var expedition = _catalog.Expedition;
            var state = View.Expedition ?? new ExpeditionStateDto { DailyRewarded = expedition.dailyRewarded };
            var reactor = ReactorLevel();
            var oxygen = Wallet().TryGetValue(ResourceType.Oxygen, out var o2) ? o2 : 0;
            var current = Math.Min(state.Cleared / Math.Max(1, expedition.stages) + 1, expedition.chapters.Length);
            if (_chapter <= 0)
                _chapter = Math.Max(1, current);

            _title.text = Loc.T("exp.title");
            _subtitle.text = Loc.F("exp.subtitle", state.Power.ToString("N0"),
                Math.Max(0, state.DailyRewarded - state.RewardedToday), state.DailyRewarded, oxygen.ToString("N0"));

            if (expedition.chapters.Length == 0)
            {
                Note(Loc.T("exp.none"));
                return;
            }

            foreach (var chapter in expedition.chapters)
            {
                var open = reactor >= chapter.reactor && chapter.chapter <= current;
                var done = Mathf.Clamp(state.Cleared - (chapter.chapter - 1) * expedition.stages, 0, expedition.stages);
                var card = Card(130);
                card.color = chapter.chapter == _chapter ? new Color(0.62f, 0.8f, 1f, 1f) : Color.white;
                CardText(card.transform, Loc.F("exp.chapter", chapter.chapter, Loc.T("exp.ch." + chapter.id)), 34,
                    open ? UiKit.Text : UiKit.Muted, 0.04f, 0.5f, 0.75f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                var status = reactor < chapter.reactor ? Loc.F("exp.locked.reactor", chapter.reactor)
                    : !open ? Loc.T("exp.locked")
                    : Loc.F("exp.progress", done, expedition.stages);
                CardText(card.transform, status, 26, done == expedition.stages ? UiKit.Accent : UiKit.Muted, 0.04f, 0.05f, 0.75f, 0.5f,
                    TextAnchor.MiddleLeft);
                var number = chapter.chapter;
                CardButton(card.transform, chapter.chapter == _chapter ? "▼" : "▶", open, () =>
                {
                    _chapter = number;
                    Render(force: false);
                }, UiKit.Field, 0.8f, 0.2f, 0.97f, 0.8f);

                if (chapter.chapter == _chapter && open)
                    RenderStages(chapter, state, oxygen);
            }
        }

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

                var card = Card(120);
                card.color = next ? new Color(0.04f, 0.16f, 0.2f, 1f) : new Color(0.03f, 0.08f, 0.11f, 1f);
                var name = boss ? Loc.T("exp.boss") : Loc.F("exp.stage", stage);
                CardText(card.transform, (cleared ? "✓ " : string.Empty) + name, 30, boss ? UiKit.Amber : cleared ? UiKit.Accent : UiKit.Text,
                    0.06f, 0.5f, 0.68f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                var strong = state.Power >= recommended;
                var info = $"<color={(strong ? "#5BE37A" : "#FF7369")}>{Loc.F("exp.recommended", recommended.ToString("N0"))}</color>   " +
                           Loc.F("exp.oxygen", chapter.oxygen.ToString("N0"));
                CardText(card.transform, info, 24, UiKit.Muted, 0.06f, 0.05f, 0.7f, 0.5f, TextAnchor.MiddleLeft);

                var s = stage;
                var can = (cleared || next) && oxygen >= chapter.oxygen;
                CardButton(card.transform, cleared ? Loc.T("exp.replay") : next ? Loc.T("exp.dive") : Loc.T("exp.locked"), can,
                    () => Dive(chapter.chapter, s), cleared ? UiKit.Field : (Color?)null, 0.71f, 0.18f, 0.97f, 0.82f);
            }
        }

        private void Dive(int chapter, int stage) =>
            _commands.Dispatch(new DiveStageCommand { CommandId = NewId(), ClientSequence = NextSequence(), Chapter = chapter, Stage = stage });

        private void RenderDiveResult()
        {
            var dive = _dive ?? new DiveReportDto();
            var chapter = ExpeditionRules.Chapter(_catalog.Expedition, dive.Chapter);
            _title.text = dive.Won ? Loc.T("exp.won") : Loc.T("exp.lost");
            _title.color = dive.Won ? Color.white : UiKit.Danger;
            _subtitle.text = chapter == null ? string.Empty
                : $"{Loc.F("exp.chapter", dive.Chapter, Loc.T("exp.ch." + chapter.id))} · " +
                  (ExpeditionRules.IsBoss(_catalog.Expedition, dive.Stage) ? Loc.T("exp.boss") : Loc.F("exp.stage", dive.Stage));

            var card = Card(150);
            CardText(card.transform, Loc.F("exp.result", dive.Power.ToString("N0"), dive.Recommended.ToString("N0"), dive.OxygenSpent.ToString("N0")),
                30, UiKit.Text, 0.05f, 0.1f, 0.95f, 0.9f, TextAnchor.MiddleLeft);
            if (dive.FirstClear)
                Header(Loc.T("exp.first"));
            if (dive.Won && !dive.Rewarded)
                Note(Loc.T("exp.norewards"));
            if (!dive.Won)
                Note(Loc.T("exp.lost.hint"));

            foreach (var group in (dive.Rewards ?? Array.Empty<RewardEntryDto>()).GroupBy(r => (r.Type, r.Id)))
            {
                var row = Card(120);
                var (type, id) = group.Key;
                var total = group.Sum(r => r.Amount);
                if (type == "shards" && _catalog.TryGetHero(id, out var hero))
                    Portrait(row.transform, hero, true, 0.01f, 0.04f, 0.15f, 0.96f);
                else
                    HudIconImage(row.transform, type == "item" ? QuestText.ItemIcon(id, string.Empty)
                        : HADAL.Shared.Catalog.GameCatalog.TryParseResource(id, out var resource) ? Hud.HudIcons.Resource(resource) : "event_gift",
                        0.02f, 0.1f, 0.15f, 0.9f);
                CardText(row.transform, QuestText.Reward(type, id, total), 30, UiKit.Text, 0.2f, 0.1f, 0.97f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            var done = Card(110);
            CardButton(done.transform, Loc.T("hero.pod.done"), true, () =>
            {
                _title.color = Color.white;
                Open(Page.Expedition);
            }, null, 0.25f, 0.1f, 0.75f, 0.9f);
        }
    }
}
