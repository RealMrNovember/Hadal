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
    /// The alliance event pages of the event centre, in Last Asylum's layout:
    /// <para>
    /// Mobilization (docs/29 §4.5): the player's held task as a coloured card with its progress bar and Drop / Refresh,
    /// the day's limits, the alliance's task pool as a two-column grid of colour-coded cards with Take (a slot that is
    /// refilling counts down), then the personal point chests and the alliance rank rewards.
    /// </para>
    /// <para>
    /// Alliance Duel (§4.3): both alliances' banners face to face with the week score between them, the six days as
    /// cells (won, lost, tie, today), today's theme with both sides' points, how to score, the player's daily chests
    /// (today's and earlier days' still waiting), and the alliance's last duels.
    /// </para>
    /// Both pages ask the server for a fresh view on opening and every <see cref="AllianceEventRefreshSeconds"/> while
    /// shown (MobilizationActions.View): mates' takes and the rival's points move without the player doing anything.
    /// </summary>
    public sealed partial class SocialView
    {
        private const float AllianceEventRefreshSeconds = 20f;
        private float _allianceEventAskedAt = -1000f;
        private string _allianceEventAskedFor = string.Empty;

        /// <summary>Asks for a fresh view of a Mobilization or Duel page now and then while it is open.</summary>
        private void TickAllianceEvent(EventDto e)
        {
            if (e == null || (e.Template != EventTemplates.Mobilization && e.Template != EventTemplates.AllianceDuel
                    && e.Template != EventTemplates.TrenchConquest && e.Template != EventTemplates.LureTrap)
                || EventRules.Phase(e, NowMs) != EventPhase.Live)
                return;
            if (_allianceEventAskedFor == e.Id && Time.unscaledTime - _allianceEventAskedAt < AllianceEventRefreshSeconds)
                return;
            _allianceEventAskedFor = e.Id;
            _allianceEventAskedAt = Time.unscaledTime;
            if (e.Template == EventTemplates.TrenchConquest)
                TrenchMove(e.Id, TrenchActions.View);
            else if (e.Template == EventTemplates.LureTrap)
                LureMove(e.Id, LureTrapActions.View);
            else
                _commands.Dispatch(new MobilizationTaskCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = e.Id, Action = MobilizationActions.View });
        }

        /// <summary>Opens the event centre on the running event of a template (the alliance hub's tiles); else the calendar.</summary>
        public void OpenAllianceEvent(string template)
        {
            var e = TabEvents().FirstOrDefault(x => x.Template == template && EventRules.Phase(x, NowMs) == EventPhase.Live)
                    ?? TabEvents().FirstOrDefault(x => x.Template == template);
            _afterReward = Page.None;
            if (e != null)
            {
                _eventId = e.Id;
                _previewId = string.Empty;
                Open(Page.Events);
                return;
            }

            _eventId = CalendarTabId;
            _previewId = string.Empty;
            _calendarDay = 0;
            Open(Page.Events);
            AskCalendar(force: true);
            Toast(Loc.T("event.notRunning"), UiKit.Amber);
        }

        /// <summary>Whether an alliance event of the template has something to claim (the hub tile's red dot).</summary>
        private bool AllianceEventWaiting(string template) =>
            (View.Events.Events ?? Array.Empty<EventDto>()).Any(x => x.Template == template && EventRules.HasClaim(x, NowMs));

        private void MobilizationMove(string eventId, string action, int slot = 0) =>
            _commands.Dispatch(new MobilizationTaskCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = eventId, Action = action, Slot = slot });

        // --- Mobilization ---------------------------------------------------------------------

        private void MobilizationBody(EventDto e)
        {
            var m = e.Mobilization;
            var live = EventRules.Phase(e, NowMs) == EventPhase.Live;
            EventStats(e);

            // My task: the card in hand with its bar, or why the player cannot take one.
            Band(Loc.T("mob.myTask"));
            var block = AllianceEventText.TakeBlock(m);
            if (m.Mine != null && !string.IsNullOrEmpty(m.Mine.Kind))
                HeldTask(e, m, live);
            else
                InkNote(block.Length > 0 && block != Loc.T("mob.holding") ? block : Loc.T("mob.takeOne"));

            var limits = InkCard(96, UiFrames.CardAlt);
            Ink(limits.transform, Loc.F("mob.left", m.TasksLeftToday, m.TasksPerDay), UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.1f, 0.5f, 0.9f);
            Ink(limits.transform, Loc.F("mob.done", m.Completed), UiKit.TypeScale.Body, UiFrames.InkGood, 0.5f, 0.1f, 0.96f, 0.9f, TextAnchor.MiddleRight, FontStyle.Bold);

            // The pool: the alliance's cards, two a row.
            if (m.Pool.Length > 0)
            {
                Band(Loc.T("mob.pool"));
                var canTake = live && block.Length == 0;
                var grid = UiFrames.Grid(_list, 2, 1.25f, 16f);
                grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 6, 6);
                foreach (var card in m.Pool)
                    PoolCard(grid, e.Id, card, canTake);
            }

            EventStages(e);
            EventRankRewards(e);
        }

        private void HeldTask(EventDto e, MobilizationDto m, bool live)
        {
            var task = m.Mine;
            var card = InkCard(300);
            var stripe = UiKit.Box("Tier", card.transform, AllianceEventText.TierColor(task.Tier));
            stripe.sprite = UiKit.Rounded;
            stripe.raycastTarget = false;
            stripe.rectTransform.Place(0, 0, 0.022f, 1);
            UiFrames.ItemTile(card.transform, AllianceEventText.KindIcon(task.Kind), AllianceEventText.TierFrame(task.Tier), null).Place(0.04f, 0.42f, 0.2f, 0.94f);
            Ink(card.transform, AllianceEventText.TaskText(task), UiKit.TypeScale.Body, UiFrames.Ink, 0.23f, 0.68f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold)
                .Fit(16, UiKit.TypeScale.Body);
            Ink(card.transform, Loc.T("mob.tier." + MobilizationRules.TierName(task.Tier)) + "  ·  " + Loc.F("mob.points", task.Points.ToString("N0")),
                UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.23f, 0.46f, 0.97f, 0.68f);
            var bar = UiKit.Rect("Bar", card.transform).Place(0.23f, 0.3f, 0.97f, 0.44f);
            UiFrames.Progress(bar, MobilizationRules.Share(task.Progress, task.Target), $"{task.Progress:N0} / {task.Target:N0}").Place(0, 0, 1, 1);

            var id = e.Id;
            CardButton(card.transform, Loc.T("mob.drop"), live, () => MobilizationMove(id, MobilizationActions.Drop), UiKit.Danger, 0.23f, 0.04f, 0.56f, 0.26f);
            var cooldown = m.RefreshReadyAtMs > NowMs;
            var label = cooldown ? Loc.F("mob.refreshIn", Span(m.RefreshReadyAtMs - NowMs)) : Loc.F("mob.refresh", m.RefreshesLeftToday);
            CardButton(card.transform, label, live && !cooldown && m.RefreshesLeftToday > 0, () => MobilizationMove(id, MobilizationActions.Refresh),
                UiKit.Primary, 0.6f, 0.04f, 0.97f, 0.26f);
        }

        private void PoolCard(Transform grid, string eventId, MobilizationTaskDto card, bool canTake)
        {
            var cell = UiKit.Rect("Card", grid);
            var ready = !string.IsNullOrEmpty(card.Kind);
            var box = UiFrames.CardBox(cell, ready ? UiFrames.Card : UiFrames.CardAlt);
            box.rectTransform.Place(0, 0, 1, 1);
            var head = UiKit.Box("Tier", box.transform, ready ? AllianceEventText.TierColor(card.Tier) : UiFrames.Band);
            head.sprite = UiKit.Rounded;
            head.raycastTarget = false;
            head.rectTransform.Place(0, 0.82f, 1, 1);
            if (!ready)
            {
                Ink(box.transform, Loc.F("mob.refilling", Span(card.ReadyAtMs - NowMs)), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.06f, 0.2f, 0.94f, 0.78f,
                    TextAnchor.MiddleCenter);
                return;
            }

            UiKit.Stroked(UiKit.Label("Colour", head.transform, Loc.T("mob.tier." + MobilizationRules.TierName(card.Tier)), UiKit.TypeScale.Caption, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0, 1, 1);
            UiFrames.ItemTile(box.transform, AllianceEventText.KindIcon(card.Kind), AllianceEventText.TierFrame(card.Tier), null).Place(0.05f, 0.4f, 0.3f, 0.78f);
            Ink(box.transform, AllianceEventText.TaskText(card), UiKit.TypeScale.Caption, UiFrames.Ink, 0.33f, 0.4f, 0.96f, 0.8f, TextAnchor.MiddleLeft, FontStyle.Bold)
                .Fit(11, UiKit.TypeScale.Caption);
            Ink(box.transform, Loc.F("mob.points", card.Points.ToString("N0")), UiKit.TypeScale.Caption, UiFrames.InkGood, 0.05f, 0.04f, 0.45f, 0.36f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var slot = card.Slot;
            CardButton(box.transform, Loc.T("mob.take"), canTake, () => MobilizationMove(eventId, MobilizationActions.Take, slot), UiKit.Ok, 0.5f, 0.05f, 0.95f, 0.36f);
        }

        // --- Alliance Duel --------------------------------------------------------------------

        private void DuelBody(EventDto e)
        {
            var duel = e.Duel;
            var now = NowMs;
            var today = AllianceDuelRules.DayAt(e, now);
            EventStats(e);

            if (!duel.InDuel)
            {
                InkNote(Loc.T(EventRules.Phase(e, now) == EventPhase.Upcoming ? "duel.pairingSoon" : "duel.notIn"), 150f);
            }
            else
            {
                DuelBanner(duel);
                DuelDays(e, duel, today);
                if (duel.Result != AllianceDuelRules.Running)
                    InkNote(Loc.T(AllianceEventText.ResultKey(duel.Result)) + "  ·  " + Loc.T("duel.resultMail"), 120f);
                else if (today >= 0)
                    DuelToday(e, duel, today);
            }

            EventThemes(e);
            EventActionsList(e);
            if (duel.InDuel)
                DuelChests(e, duel, today);
            DuelHistory(duel);
        }

        /// <summary>The two banners face to face: emblem, [TAG] Name and power on each side, the week score between them.</summary>
        private void DuelBanner(DuelDto duel)
        {
            var row = UiKit.Rect("Duel", _list).Row(320);
            Side(row, duel.Mine, 0f, 0.5f, new Color(0.3f, 0.52f, 0.82f, 1f), true);
            Side(row, duel.Rival, 0.5f, 1f, new Color(0.82f, 0.36f, 0.34f, 1f), false);

            var mid = UiKit.Box("Score", row, UiFrames.NavyDeep);
            mid.sprite = UiKit.Rounded;
            mid.raycastTarget = false;
            mid.rectTransform.Place(0.37f, 0.28f, 0.63f, 0.74f);
            UiKit.Stroked(UiKit.Label("Wins", mid.transform, $"{duel.Mine?.Wins ?? 0} : {duel.Rival?.Wins ?? 0}", UiKit.TypeScale.Display, UiKit.Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0.3f, 1, 1);
            UiKit.Label("Goal", mid.transform, Loc.F("duel.firstTo", AllianceDuelRules.WinTarget), UiKit.TypeScale.Micro, Color.white, TextAnchor.MiddleCenter)
                .rectTransform.Place(0, 0.02f, 1, 0.3f);

            void Side(RectTransform parent, DuelSideDto side, float x0, float x1, Color color, bool left)
            {
                var half = UiKit.Box("Side", parent, color);
                half.sprite = UiKit.Rounded;
                half.raycastTarget = false;
                half.rectTransform.Place(x0 + 0.004f, 0f, x1 - 0.004f, 1f);
                var inner = left ? (0.04f, 0.7f) : (0.3f, 0.96f);
                var disc = UiKit.Rect("Emblem", half.transform).Place(inner.Item1 + 0.18f, 0.4f, inner.Item2 - 0.18f, 0.94f);
                if (side != null && side.AllianceId != AllianceDuelRules.Echo)
                    AvatarDisc.Emblem(disc, side.Emblem);
                else
                    Hud.HudIcons.Image(disc, "event_war", 0.1f, 0.1f, 0.9f, 0.9f);
                var name = UiKit.Stroked(UiKit.Label("Name", half.transform, AllianceEventText.SideName(side), UiKit.TypeScale.Body, Color.white,
                    TextAnchor.MiddleCenter, FontStyle.Bold));
                name.richText = false;
                name.rectTransform.Place(inner.Item1, 0.18f, inner.Item2, 0.4f);
                name.Fit(12, UiKit.TypeScale.Body);
                if (side != null && side.Power > 0)
                    UiKit.Stroked(UiKit.Label("Power", half.transform, $"⚡ {side.Power:N0}", UiKit.TypeScale.Caption, new Color(1f, 0.92f, 0.62f),
                        TextAnchor.MiddleCenter)).rectTransform.Place(inner.Item1, 0.02f, inner.Item2, 0.18f);
            }
        }

        /// <summary>The six days as cells: day number, theme, worth; won green, lost red, tie grey, today gold.</summary>
        private void DuelDays(EventDto e, DuelDto duel, int today)
        {
            var strip = UiKit.Rect("Days", _list).Row(190);
            for (var d = 0; d < AllianceDuelRules.Days; d++)
            {
                var winner = d < duel.DayWinners.Length ? duel.DayWinners[d] : AllianceDuelRules.Pending;
                var x0 = d / (float)AllianceDuelRules.Days + 0.004f;
                var x1 = (d + 1) / (float)AllianceDuelRules.Days - 0.004f;
                var cell = UiKit.Box("Day", strip, AllianceEventText.DayColor(winner, d == today));
                cell.sprite = UiKit.Rounded;
                cell.raycastTarget = false;
                cell.rectTransform.Place(x0, 0.04f, x1, 0.96f);
                var ink = winner is AllianceDuelRules.First or AllianceDuelRules.Second ? Color.white : UiFrames.Ink;
                UiKit.Label("Day", cell.transform, Loc.F("event.day", d + 1), UiKit.TypeScale.Caption, ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Place(0.02f, 0.7f, 0.98f, 0.96f);
                var theme = d < e.Phases.Length ? EventRules.Text(e.Phases[d].Title, Loc.Current) : Loc.T("duel.theme." + AllianceDuelRules.Themes[d]);
                var themeLabel = UiKit.Label("Theme", cell.transform, theme, UiKit.TypeScale.Micro, ink, TextAnchor.MiddleCenter);
                themeLabel.rectTransform.Place(0.03f, 0.3f, 0.97f, 0.7f);
                themeLabel.Fit(9, UiKit.TypeScale.Micro);
                var mark = winner switch
                {
                    AllianceDuelRules.First => "✓",
                    AllianceDuelRules.Second => "✗",
                    AllianceDuelRules.Tie => "=",
                    _ => "+" + AllianceDuelRules.DayWorth(d)
                };
                UiKit.Label("Worth", cell.transform, mark, UiKit.TypeScale.Body, ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0.02f, 1, 0.32f);
            }
        }

        /// <summary>Today: both sides' points on one bar (mine blue from the left, theirs red from the right).</summary>
        private void DuelToday(EventDto e, DuelDto duel, int today)
        {
            var mine = duel.Mine != null && today < duel.Mine.DayPoints.Length ? duel.Mine.DayPoints[today] : 0;
            var theirs = duel.Rival != null && today < duel.Rival.DayPoints.Length ? duel.Rival.DayPoints[today] : 0;
            Band(Loc.F("duel.today", today + 1, AllianceDuelRules.DayWorth(today)));
            var card = InkCard(170);
            Ink(card.transform, mine.ToString("N0"), UiKit.TypeScale.Subtitle, new Color(0.2f, 0.42f, 0.78f, 1f), 0.04f, 0.55f, 0.5f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ink(card.transform, theirs.ToString("N0"), UiKit.TypeScale.Subtitle, new Color(0.76f, 0.26f, 0.24f, 1f), 0.5f, 0.55f, 0.96f, 0.95f, TextAnchor.MiddleRight, FontStyle.Bold);
            var total = Math.Max(1, mine + theirs);
            var share = mine + theirs == 0 ? 0.5f : (float)mine / total;
            var left = UiKit.Box("Mine", card.transform, new Color(0.3f, 0.52f, 0.82f, 1f));
            left.raycastTarget = false;
            left.rectTransform.Place(0.04f, 0.3f, 0.04f + 0.92f * share, 0.5f);
            var right = UiKit.Box("Theirs", card.transform, new Color(0.82f, 0.36f, 0.34f, 1f));
            right.raycastTarget = false;
            right.rectTransform.Place(0.04f + 0.92f * share, 0.3f, 0.96f, 0.5f);
            Ink(card.transform, Loc.F("duel.myToday", (today < duel.MyDayPoints.Length ? duel.MyDayPoints[today] : 0).ToString("N0")), UiKit.TypeScale.Caption,
                UiFrames.InkSoft, 0.04f, 0.04f, 0.96f, 0.28f, TextAnchor.MiddleCenter);
        }

        /// <summary>Daily chests: today's stages by the player's own points today, then earlier days' chests still waiting.</summary>
        private void DuelChests(EventDto e, DuelDto duel, int today)
        {
            if (e.Stages.Length == 0)
                return;
            var live = EventRules.Phase(e, NowMs) == EventPhase.Live;
            var claimed = new System.Collections.Generic.HashSet<int>(duel.ClaimedChests ?? Array.Empty<int>());
            Band(Loc.T("duel.chests"));
            foreach (var (day, stage) in AllianceEventText.Chests(duel, e.Stages, today))
            {
                var points = day < duel.MyDayPoints.Length ? duel.MyDayPoints[day] : 0;
                var need = e.Stages[stage].Points;
                var done = claimed.Contains(AllianceDuelRules.ChestKey(day, stage));
                var reached = points >= need;
                var title = $"{Loc.F("event.day", day + 1)} · {Loc.F("event.stage", stage + 1)}  ({Math.Min(points, need):N0}/{need:N0})";
                var action = done ? Loc.T("event.claimed") : reached ? Loc.T(live ? "quest.claim" : "event.byMail") : Loc.F("event.pointsLeft", (need - points).ToString("N0"));
                var id = e.Id;
                var d = day;
                var s = stage;
                var row = UiFrames.QuestRow(_list, title, RewardTiles(e.Stages[stage].Rewards), action, true,
                    () => _commands.Dispatch(new ClaimEventStageCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = id, Index = s, Day = d + 1 }),
                    live && reached && !done).Row(200);
                if (done)
                    row.GetComponent<Image>().color = UiFrames.CardAlt;
                RewardsHint(row, e.Stages[stage].Rewards);
            }
        }

        /// <summary>The alliance's last duels: week, rival, score and result.</summary>
        private void DuelHistory(DuelDto duel)
        {
            if (duel.History == null || duel.History.Length == 0)
                return;
            Band(Loc.T("duel.history"));
            var offset = EventCalendarText.LocalOffsetMs(NowMs);
            for (var i = 0; i < duel.History.Length; i++)
            {
                var h = duel.History[i];
                var card = InkCard(110, i % 2 == 0 ? UiFrames.Card : UiFrames.CardAlt);
                var rival = string.IsNullOrEmpty(h.RivalTag) && string.IsNullOrEmpty(h.RivalName) ? Loc.T("duel.echo") : $"[{h.RivalTag}] {h.RivalName}";
                var week = Loc.F("duel.week", DateTimeOffset.FromUnixTimeMilliseconds(h.StartsAtMs + offset).UtcDateTime.ToString("dd.MM"));
                var name = Ink(card.transform, week + "  ·  " + rival, UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.1f, 0.62f, 0.9f);
                name.richText = false;
                name.Fit(12, UiKit.TypeScale.Body);
                var color = h.Result == AllianceDuelRules.Won ? UiFrames.InkGood : h.Result == AllianceDuelRules.Lost ? UiFrames.InkBad : UiFrames.InkSoft;
                Ink(card.transform, $"{h.MyWins} : {h.RivalWins}  {Loc.T(AllianceEventText.ResultKey(h.Result))}", UiKit.TypeScale.Body, color, 0.6f, 0.1f, 0.96f, 0.9f,
                    TextAnchor.MiddleRight, FontStyle.Bold);
            }
        }
    }
}
