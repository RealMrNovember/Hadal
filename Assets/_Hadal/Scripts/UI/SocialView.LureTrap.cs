using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The Lure Trap page of the event centre (docs/29 §4.1): the alliance's bait and trap level, its two traps (when,
    /// how hard, how it went; the leader or a war officer sets them here), the way to a Gulper that is up, the best
    /// hitters, the alliance's damage against the tier thresholds, then the personal chests and the rank rewards.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>Start offsets an officer picks from, in minutes from now.</summary>
        private static readonly int[] LureDelays = { 15, 30, 60, 120, 240, 480, 720, 1440 };

        private readonly int[] _lureDifficulty = { 1, 1 };
        private readonly int[] _lureDelay = { 2, 2 };

        /// <summary>Part of the events page key: the officer's unsent picks.</summary>
        private string LureKey => string.Join(",", _lureDifficulty) + "/" + string.Join(",", _lureDelay);

        private void LureMove(string eventId, string action, int trap = 0, long startsAtMs = 0, int difficulty = 0) =>
            _commands.Dispatch(new LureTrapCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), EventId = eventId, Action = action, Trap = trap, StartsAtMs = startsAtMs, Difficulty = difficulty
            });

        private void LureBody(EventDto e)
        {
            var t = e.LureTrap;
            var live = EventRules.Phase(e, NowMs) == EventPhase.Live;
            EventStats(e);
            if (!t.InAlliance)
            {
                InkNote(Loc.T("lure.noAlliance"), 96);
                EventStages(e);
                return;
            }

            // The trap: level, bait, and what the next level needs.
            var trapCard = InkCard(130);
            Ink(trapCard.transform, Loc.F("lure.level", t.Level, LureTrapRules.MaxLevel), UiKit.TypeScale.Subtitle, UiFrames.Ink,
                0.04f, 0.56f, 0.6f, 0.94f, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ink(trapCard.transform, Loc.F("lure.bonus", Mathf.RoundToInt((float)(LureTrapRules.AttackBonus(t.Level) * 100))), UiKit.TypeScale.Body, UiFrames.InkGood,
                0.6f, 0.56f, 0.96f, 0.94f, TextAnchor.MiddleRight, FontStyle.Bold);
            var toNext = LureTrapRules.BaitToNext(t.Bait);
            Ink(trapCard.transform, toNext > 0 ? Loc.F("lure.bait", t.Bait.ToString("N0"), toNext.ToString("N0")) : Loc.F("lure.baitMax", t.Bait.ToString("N0")),
                UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.04f, 0.3f, 0.96f, 0.56f);
            var from = LureTrapRules.BaitForLevel[t.Level];
            var to = t.Level >= LureTrapRules.MaxLevel ? from : LureTrapRules.BaitForLevel[t.Level + 1];
            Bar(trapCard.transform, to > from ? (float)(t.Bait - from) / (to - from) : 1f, 0.04f, 0.1f, 0.96f, 0.24f);

            if (t.Members < t.MinMembers)
                InkNote(Loc.F("lure.fewMembers", t.Members, t.MinMembers), 96);

            Band(Loc.T("lure.traps"));
            for (var i = 0; i < t.Traps.Length; i++)
                TrapCard(e, t, i, live);

            // The best hitters of the trap that is up, else of the last one that saw a fight.
            var shown = t.Traps.FirstOrDefault(s => s.MonsterId.Length > 0) ?? t.Traps.LastOrDefault(s => s.Board.Length > 0);
            if (shown != null && shown.Board.Length > 0)
            {
                Band(Loc.T("lure.board"));
                UiFrames.TableRow(_list, new[] { Loc.T("rank.col.rank"), Loc.T("rank.col.name"), Loc.T("lure.col.damage") }, header: true).Row(58);
                for (var i = 0; i < shown.Board.Length; i++)
                {
                    var b = shown.Board[i];
                    UiFrames.TableRow(_list, new[] { b.Rank.ToString(), b.Name, b.Damage.ToString("N0") }, highlight: b.PlayerId == View.PlayerId, index: i).Row(66);
                }
            }

            // The alliance tier over both traps.
            if (t.TierThresholds.Length > 0)
            {
                Band(Loc.T("lure.tiers"));
                var next = t.Tier < t.TierThresholds.Length ? t.TierThresholds[t.Tier] : 0;
                var tierCard = InkCard(120);
                Ink(tierCard.transform, Loc.F("lure.tier", t.Tier, t.TierThresholds.Length), UiKit.TypeScale.Body, UiFrames.Ink,
                    0.04f, 0.55f, 0.6f, 0.92f, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ink(tierCard.transform, Loc.F("lure.allianceDamage", t.AllianceDamage.ToString("N0")), UiKit.TypeScale.Caption, UiFrames.InkSoft,
                    0.5f, 0.55f, 0.96f, 0.92f, TextAnchor.MiddleRight);
                var floor = t.Tier > 0 ? t.TierThresholds[t.Tier - 1] : 0;
                Bar(tierCard.transform, next > floor ? (float)(t.AllianceDamage - floor) / (next - floor) : 1f, 0.04f, 0.14f, 0.96f, 0.4f);
                InkNote(Loc.T("lure.tierNote"), 80);
            }

            EventStages(e);
            EventRankRewards(e);
        }

        private void TrapCard(EventDto e, LureTrapDto t, int i, bool live)
        {
            var s = t.Traps[i];
            var up = s.MonsterId.Length > 0;
            var waiting = s.StartsAtMs > NowMs && !s.Done;
            var canSet = t.CanSchedule && live && !s.Done && (s.StartsAtMs == 0 || waiting);
            var card = InkCard(canSet ? 250 : 150, up ? UiFrames.Highlight : (Color?)null);
            Ink(card.transform, Loc.F("lure.trap", i + 1), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.04f, canSet ? 0.78f : 0.62f, 0.5f, 0.96f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            if (s.Difficulty > 0)
                Ink(card.transform, Loc.F("lure.difficulty", s.Difficulty), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.5f, canSet ? 0.78f : 0.62f, 0.96f, 0.96f,
                    TextAnchor.MiddleRight);

            var state = up ? Loc.F("lure.up", Span(s.EndsAtMs - NowMs))
                : s.Done ? Loc.T("lure.done")
                : waiting ? Loc.F("lure.at", LocalTime(s.StartsAtMs), Span(s.StartsAtMs - NowMs))
                : Loc.T("lure.unset");
            var top = canSet ? 0.56f : 0.34f;
            Ink(card.transform, state, UiKit.TypeScale.Body, up ? UiFrames.InkGood : UiFrames.Ink, 0.04f, top, up ? 0.6f : 0.96f, top + 0.2f);
            if (s.Damage > 0)
                Ink(card.transform, Loc.F("lure.damage", s.Damage.ToString("N0"), s.MyDamage.ToString("N0")), UiKit.TypeScale.Caption, UiFrames.InkSoft,
                    0.04f, top - 0.2f, 0.96f, top);
            if (up && ShowOnMap != null)
            {
                var (x, y) = (s.X, s.Y);
                CardButton(card.transform, Loc.T("lure.go"), true, () => ShowOnMap(x, y), UiKit.Ok, 0.62f, top - 0.02f, 0.97f, top + 0.24f);
            }

            if (!canSet)
                return;

            // The officer's picks: difficulty and start, then set (or move) the trap, or call it off.
            var id = e.Id;
            Stepper(card.transform, Loc.F("lure.difficulty", _lureDifficulty[i]), 0.04f, 0.04f, 0.48f, 0.3f,
                () => _lureDifficulty[i] = Math.Max(1, _lureDifficulty[i] - 1), () => _lureDifficulty[i] = Math.Min(LureTrapRules.MaxDifficulty, _lureDifficulty[i] + 1));
            Stepper(card.transform, Loc.F("lure.in", Span(LureDelays[_lureDelay[i]] * 60_000L)), 0.5f, 0.04f, 0.96f, 0.3f,
                () => _lureDelay[i] = Math.Max(0, _lureDelay[i] - 1), () => _lureDelay[i] = Math.Min(LureDelays.Length - 1, _lureDelay[i] + 1));
            var enough = t.Members >= t.MinMembers;
            CardButton(card.transform, Loc.T(s.StartsAtMs > 0 ? "lure.move" : "lure.set"), enough, () =>
            {
                // Whole five minutes, so the time reads well on every member's page.
                var at = NowMs + LureDelays[_lureDelay[i]] * 60_000L;
                at = (at + 299_999L) / 300_000L * 300_000L;
                LureMove(id, LureTrapActions.Schedule, i, at, _lureDifficulty[i]);
            }, UiKit.Ok, 0.04f, 0.33f, s.StartsAtMs > 0 ? 0.48f : 0.96f, 0.52f);
            if (s.StartsAtMs > 0)
                CardButton(card.transform, Loc.T("lure.cancel"), true, () => LureMove(id, LureTrapActions.Cancel, i), UiKit.Danger, 0.52f, 0.33f, 0.96f, 0.52f);
        }

        /// <summary>A "−  label  +" row; a tap changes the pick (the page key changes, so the page redraws).</summary>
        private void Stepper(Transform card, string label, float x0, float y0, float x1, float y1, Action down, Action up)
        {
            var w = (x1 - x0) * 0.22f;
            CardButton(card, "−", true, down, UiKit.Field, x0, y0, x0 + w, y1);
            Ink(card, label, UiKit.TypeScale.Caption, UiFrames.Ink, x0 + w, y0, x1 - w, y1, TextAnchor.MiddleCenter, FontStyle.Bold);
            CardButton(card, "+", true, up, UiKit.Field, x1 - w, y0, x1, y1);
        }

        /// <summary>A thin progress bar on a card.</summary>
        private static void Bar(Transform card, float fill, float x0, float y0, float x1, float y1)
        {
            var back = UiKit.Box("Bar", card, UiFrames.Band);
            back.raycastTarget = false;
            back.rectTransform.Place(x0, y0, x1, y1);
            var front = UiKit.Box("Fill", back.transform, UiFrames.InkGood);
            front.raycastTarget = false;
            front.rectTransform.Place(0, 0, Mathf.Clamp01(fill), 1);
        }

        private static string LocalTime(long ms)
        {
            var at = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime();
            return at.Date == DateTimeOffset.Now.Date ? at.ToString("HH:mm") : at.ToString("dd.MM HH:mm");
        }
    }
}
