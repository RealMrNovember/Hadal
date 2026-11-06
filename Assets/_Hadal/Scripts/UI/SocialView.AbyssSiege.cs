using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The Abyss Siege page of the event centre (docs/29 §4.2): where the alliance's siege stands (not set, counting down,
    /// the next wave and what kind it is, over), how the player's base is holding, the hub waves, the officer's picks,
    /// then the personal chests and the rank rewards.
    /// </summary>
    public sealed partial class SocialView
    {
        private int _siegeDifficulty = 1;
        private int _siegeDelay = 2;

        /// <summary>Part of the events page key: the officer's unsent picks.</summary>
        private string SiegeKey => $"{_siegeDifficulty}/{_siegeDelay}";

        private void SiegeMove(string eventId, string action, long startsAtMs = 0, int difficulty = 0) =>
            _commands.Dispatch(new AbyssSiegeCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), EventId = eventId, Action = action, StartsAtMs = startsAtMs, Difficulty = difficulty
            });

        private void SiegeBody(EventDto e)
        {
            var s = e.AbyssSiege;
            var live = EventRules.Phase(e, NowMs) == EventPhase.Live;
            EventStats(e);
            if (!s.InAlliance)
            {
                InkNote(Loc.T("siege.noAlliance"), 96);
                EventStages(e);
                return;
            }

            var started = s.StartsAtMs > 0 && s.StartsAtMs <= NowMs;
            var over = s.StartsAtMs > 0 && s.NextWaveAtMs == 0;

            // The siege: wave progress and what comes next.
            var card = InkCard(170, s.NextWaveAtMs > 0 && started ? UiFrames.Highlight : (Color?)null);
            Ink(card.transform, Loc.F("siege.wave", s.WavesDone, AbyssSiegeRules.Waves), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.04f, 0.68f, 0.6f, 0.95f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            if (s.Difficulty > 0)
                Ink(card.transform, Loc.F("lure.difficulty", s.Difficulty), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.6f, 0.68f, 0.96f, 0.95f, TextAnchor.MiddleRight);
            Ink(card.transform, SiegeState(s, over), UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.42f, 0.96f, 0.66f);
            Bar(card.transform, (float)s.WavesDone / AbyssSiegeRules.Waves, 0.04f, 0.1f, 0.96f, 0.24f);

            // The player's own base.
            var dropped = AbyssSiegeRules.DroppedOut(s.MyBreaches);
            var mine = InkCard(96, UiFrames.CardAlt);
            Ink(mine.transform, dropped ? Loc.T("siege.out") : Loc.F("siege.breaches", s.MyBreaches, AbyssSiegeRules.BreachLimit), UiKit.TypeScale.Body,
                dropped ? UiFrames.InkBad : UiFrames.Ink, 0.04f, 0.1f, 0.96f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);

            if (s.WavesDone > 0)
                UiFrames.TableRow(_list, new[] { Loc.F("siege.hub", s.HubHeld, s.HubLost), Loc.F("siege.dropped", s.DroppedOut) }).Row(66);
            if (s.Members < s.MinMembers)
                InkNote(Loc.F("lure.fewMembers", s.Members, s.MinMembers), 96);

            if (s.CanSchedule && live && !started)
                SiegePicks(e, s);
            InkNote(Loc.F("siege.rules", s.MinReactor), 150);
            EventStages(e);
            EventRankRewards(e);
        }

        private string SiegeState(AbyssSiegeDto s, bool over)
        {
            if (over)
                return Loc.T("siege.over");
            if (s.StartsAtMs == 0)
                return Loc.T("siege.unset");
            if (s.StartsAtMs > NowMs)
                return Loc.F("lure.at", LocalTime(s.StartsAtMs), Span(s.StartsAtMs - NowMs));
            var next = s.WavesDone + 1;
            var kind = AbyssSiegeRules.IsHubWave(next) ? "siege.next.hub" : AbyssSiegeRules.IsEliteWave(next) ? "siege.next.elite" : "siege.next";
            return Loc.F(kind, next, Span(System.Math.Max(0, s.NextWaveAtMs - NowMs)));
        }

        /// <summary>The officer's picks: difficulty and start, then set (or move) the siege, or call it off.</summary>
        private void SiegePicks(EventDto e, AbyssSiegeDto s)
        {
            var id = e.Id;
            var card = InkCard(130);
            Stepper(card.transform, Loc.F("lure.difficulty", _siegeDifficulty), 0.04f, 0.56f, 0.48f, 0.92f,
                () => _siegeDifficulty = Mathf.Max(1, _siegeDifficulty - 1), () => _siegeDifficulty = Mathf.Min(AbyssSiegeRules.MaxDifficulty, _siegeDifficulty + 1));
            Stepper(card.transform, Loc.F("lure.in", Span(LureDelays[_siegeDelay] * 60_000L)), 0.5f, 0.56f, 0.96f, 0.92f,
                () => _siegeDelay = Mathf.Max(0, _siegeDelay - 1), () => _siegeDelay = Mathf.Min(LureDelays.Length - 1, _siegeDelay + 1));
            CardButton(card.transform, Loc.T(s.StartsAtMs > 0 ? "lure.move" : "siege.set"), s.Members >= s.MinMembers, () =>
            {
                // Whole five minutes, so the time reads well on every member's page.
                var at = NowMs + LureDelays[_siegeDelay] * 60_000L;
                at = (at + 299_999L) / 300_000L * 300_000L;
                SiegeMove(id, AbyssSiegeActions.Schedule, at, _siegeDifficulty);
            }, UiKit.Ok, 0.04f, 0.1f, s.StartsAtMs > 0 ? 0.48f : 0.96f, 0.46f);
            if (s.StartsAtMs > 0)
                CardButton(card.transform, Loc.T("lure.cancel"), true, () => SiegeMove(id, AbyssSiegeActions.Cancel), UiKit.Danger, 0.52f, 0.1f, 0.96f, 0.46f);
        }
    }
}
