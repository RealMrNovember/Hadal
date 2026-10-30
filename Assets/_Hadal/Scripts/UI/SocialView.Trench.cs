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
    /// The Trench Conquest page of the event centre (docs/29 §4.6), in Last Asylum's layout: where the league stands
    /// (registration countdown, next round, or why the alliance is out), the three lanes as tiles with the alliance's
    /// teams in each (tap to register there, or to move the team while the league runs), the alliance's registered
    /// teams, the group table and the rounds played, then the personal chests and the alliance rank rewards.
    /// </summary>
    public sealed partial class SocialView
    {
        private void TrenchMove(string eventId, string action, int lane = 0) =>
            _commands.Dispatch(new TrenchCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = eventId, Action = action, Lane = lane });

        private void TrenchBody(EventDto e)
        {
            var t = e.Trench;
            var live = EventRules.Phase(e, NowMs) == EventPhase.Live;
            var registering = live && NowMs < t.RegisterUntilMs;
            EventStats(e);

            // Where the league stands.
            var state = registering ? Loc.F("trench.registerOpen", Span(t.RegisterUntilMs - NowMs))
                : !t.InLeague && t.RoundsDone == 0 && live ? Loc.F("trench.notInLeague", t.MinRegistered)
                : t.NextRoundAtMs > NowMs ? Loc.F("trench.nextRound", Span(t.NextRoundAtMs - NowMs))
                : Loc.T("trench.over");
            InkNote(state, 96);

            // The lanes: how many of the alliance's teams stand in each; a tap registers or moves the player's team.
            Band(Loc.T("trench.myTeam"));
            var lanes = UiFrames.Grid(_list, TrenchRules.Lanes, 1.1f, 16f);
            lanes.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 6, 6);
            for (var lane = 0; lane < TrenchRules.Lanes; lane++)
                LaneTile(lanes, e.Id, t, lane, registering, live);

            var info = InkCard(96, UiFrames.CardAlt);
            Ink(info.transform, t.Registered ? Loc.F("trench.power", t.MyPower.ToString("N0")) : Loc.T("trench.notRegistered"),
                UiKit.TypeScale.Body, t.Registered ? UiFrames.Ink : UiFrames.InkSoft, 0.04f, 0.1f, 0.62f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (t.Registered && registering)
            {
                var id = e.Id;
                CardButton(info.transform, Loc.T("trench.withdraw"), true, () => TrenchMove(id, TrenchActions.Withdraw), UiKit.Danger, 0.64f, 0.16f, 0.97f, 0.84f);
            }
            else
            {
                Ink(info.transform, Loc.F("trench.minReactor", t.MinReactor), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.62f, 0.1f, 0.96f, 0.9f, TextAnchor.MiddleRight);
            }

            // The alliance's teams, lane by lane.
            if (t.Members.Length > 0)
            {
                Band(Loc.F("trench.teams", t.Members.Length));
                for (var i = 0; i < t.Members.Length; i++)
                {
                    var m = t.Members[i];
                    UiFrames.TableRow(_list, new[] { Loc.F("trench.lane", m.Lane + 1), m.Name, m.Power.ToString("N0") }, highlight: m.Mine, index: i).Row(70);
                }
            }

            // The group table and the rounds once the league is drawn.
            if (t.InLeague && t.Group.Length > 0)
            {
                Band(Loc.T("trench.group"));
                UiFrames.TableRow(_list, new[] { Loc.T("rank.col.rank"), Loc.T("rank.col.name"), Loc.T("rank.col.points"), Loc.T("trench.col.flags") }, header: true).Row(58);
                for (var i = 0; i < t.Group.Length; i++)
                {
                    var g = t.Group[i];
                    var mine = View.MyAlliance != null && g.AllianceId == View.MyAlliance.Id;
                    UiFrames.TableRow(_list, new[] { (i + 1).ToString(), $"[{g.Tag}] {g.Name}", g.Points.ToString(), g.Flags.ToString() }, highlight: mine, index: i).Row(70);
                }
            }

            if (t.Rounds.Length > 0)
            {
                Band(Loc.T("trench.rounds"));
                foreach (var r in t.Rounds)
                    RoundCard(r);
            }

            EventStages(e);
            EventRankRewards(e);
        }

        private void LaneTile(Transform grid, string eventId, TrenchDto t, int lane, bool registering, bool live)
        {
            var mine = t.Registered && t.MyLane == lane;
            var count = t.Members.Count(m => m.Lane == lane);
            var cell = UiKit.Rect("Lane", grid);
            var box = UiFrames.CardBox(cell, mine ? UiFrames.Highlight : UiFrames.Card);
            box.rectTransform.Place(0, 0, 1, 1);
            Ink(box.transform, Loc.F("trench.lane", lane + 1), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.05f, 0.62f, 0.95f, 0.95f, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ink(box.transform, Loc.F("trench.laneTeams", count), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.05f, 0.42f, 0.95f, 0.62f, TextAnchor.MiddleCenter);
            if (mine)
            {
                Ink(box.transform, "✓", UiKit.TypeScale.Title, UiFrames.InkGood, 0.05f, 0.05f, 0.95f, 0.4f, TextAnchor.MiddleCenter, FontStyle.Bold);
                return;
            }

            // Registration takes a fresh snapshot of the team; in the league only the lane changes.
            var action = registering ? TrenchActions.Register : TrenchActions.Move;
            var label = registering ? Loc.T(t.Registered ? "trench.move" : "trench.register") : Loc.T("trench.move");
            var enabled = live && (registering || t.Registered);
            CardButton(box.transform, label, enabled, () => TrenchMove(eventId, action, lane), UiKit.Ok, 0.08f, 0.06f, 0.92f, 0.36f);
        }

        private void RoundCard(TrenchRoundDto r)
        {
            var card = InkCard(110);
            Ink(card.transform, Loc.F("trench.round", r.Round), UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.52f, 0.3f, 0.94f, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ink(card.transform, r.Bye ? Loc.T("trench.bye") : $"[{r.RivalTag}] {r.RivalName}", UiKit.TypeScale.Caption, UiFrames.InkSoft,
                0.04f, 0.08f, 0.6f, 0.5f, TextAnchor.MiddleLeft);
            // One mark per lane: ours, theirs, or nobody's.
            for (var i = 0; i < r.Lanes.Length && i < TrenchRules.Lanes; i++)
            {
                var flag = r.Lanes[i];
                var mark = UiKit.Box("Flag", card.transform, flag == 1 ? UiFrames.InkGood : flag == 2 ? UiFrames.InkBad : UiFrames.Band);
                mark.sprite = UiKit.Circle;
                mark.raycastTarget = false;
                var x = 0.62f + i * 0.09f;
                mark.rectTransform.Place(x, 0.3f, x + 0.065f, 0.7f);
            }

            var points = r.Points > 0 ? "+" + r.Points : "0";
            Ink(card.transform, points, UiKit.TypeScale.Title, r.Points >= TrenchRules.WinPoints ? UiFrames.InkGood : r.Points > 0 ? UiFrames.Ink : UiFrames.InkBad,
                0.88f, 0.1f, 0.98f, 0.9f, TextAnchor.MiddleRight, FontStyle.Bold);
        }
    }
}
