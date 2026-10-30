using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The event centre (docs/29) in Last Asylum's layout: the running events as a strip of picture tabs fixed under the
    /// top bar; under it the event's banner with its title and countdown, the player's points and rank with the ranking
    /// button, the running theme, the ways to score, the stage goals as quest rows (reward tiles, Claim), the rank
    /// rewards as tile rows, and for sign-in events a calendar of daily rewards. The ranking is the genre's table. The
    /// strip's first tab is the week calendar (SocialView.EventCalendar).
    /// </summary>
    public sealed partial class SocialView
    {
        private string _eventId = string.Empty;
        private TMP_Text _eventClock;
        private EventDto _eventClockFor;
        private Page _afterReward = Page.None;

        /// <summary>The event tab strip under the top bar, in canvas units.</summary>
        private const float EventStripHeight = 196f;

        public void OpenEvents()
        {
            _afterReward = Page.None;
            Open(Page.Events);
        }

        private string EventsKey() => $"{View.EventsVersion}|{View.EventRankingVersion}|{_eventId}|{View.EventCalendarVersion}|{_calendarDay}|{_previewId}";

        private long NowMs => View.ServerNowMs;

        private static string EventTitle(EventDto e) => EventRules.Text(e.Title, Loc.Current);

        /// <summary>
        /// The events that get a tab: all the player sees, except recurring ones that have not started (the calendar
        /// shows those, so a daily event does not add a tab a day ahead).
        /// </summary>
        private EventDto[] TabEvents() =>
            (View.Events.Events ?? Array.Empty<EventDto>())
            .Where(e => string.IsNullOrEmpty(e.ScheduleId) || EventRules.Phase(e, NowMs) != EventPhase.Upcoming).ToArray();

        /// <summary>The selected event, or the first one asking for a claim, or the first one.</summary>
        private EventDto SelectedEvent()
        {
            var events = TabEvents();
            var chosen = Array.Find(events, e => e.Id == _eventId)
                         ?? Array.Find(events, e => EventRules.HasClaim(e, NowMs))
                         ?? Array.Find(events, e => EventRules.Phase(e, NowMs) == EventPhase.Live)
                         ?? (events.Length > 0 ? events[0] : null);
            _eventId = chosen?.Id ?? string.Empty;
            return chosen;
        }

        /// <summary>The banner countdown runs every frame without redrawing the page.</summary>
        private void TickEvents()
        {
            if (_page != Page.Events || _eventClock == null || _eventClockFor == null)
                return;
            _eventClock.text = ClockText(_eventClockFor);
            TickAllianceEvent(_eventClockFor);
        }

        private string ClockText(EventDto e)
        {
            var now = NowMs;
            return EventRules.Phase(e, now) switch
            {
                EventPhase.Upcoming => Loc.F("event.startsIn", Span(e.StartsAtMs - now)),
                EventPhase.Live => Loc.F("event.endsIn", Span(e.EndsAtMs - now)),
                EventPhase.Ended => Loc.F("event.claimUntil", Span(e.ClaimUntilMs - now)),
                _ => Loc.T("event.over")
            };
        }

        /// <summary>"2g 4s", "3s 12d", "45d 10sn" — the two largest units.</summary>
        private static string Span(long ms)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
            return t.TotalDays >= 1 ? Loc.F("event.span.dh", (int)t.TotalDays, t.Hours)
                : t.TotalHours >= 1 ? Loc.F("event.span.hm", (int)t.TotalHours, t.Minutes)
                : Loc.F("event.span.ms", t.Minutes, t.Seconds);
        }

        private static Texture2D EventArt(EventDto e) =>
            Resources.Load<Texture2D>("Events/banner_" + e.Banner) ?? Resources.Load<Texture2D>("Events/banner_abyss");

        // --- Page --------------------------------------------------------------------------

        private void RenderEvents()
        {
            PageTitle(Loc.T("event.title"));
            _eventClock = null;
            _eventClockFor = null;

            // With nothing running the calendar shows what is coming.
            var events = TabEvents();
            if (events.Length == 0 && !CalendarShown)
            {
                _eventId = CalendarTabId;
                _calendarDay = 0;
            }

            var e = CalendarShown ? null : SelectedEvent();
            EventTabs(events, e);
            if (e == null)
            {
                if (events.Length == 0)
                    InkNote(Loc.T("event.none"));
                RenderCalendar();
                return;
            }

            EventBanner(e);
            EventBody(e, preview: false);
        }

        /// <summary>
        /// Everything under the banner. A <paramref name="preview"/> (a calendar entry not open yet) shows the rules and
        /// rewards without the player's points, rank or ranking button.
        /// </summary>
        private void EventBody(EventDto e, bool preview)
        {
            if (e.Template == EventTemplates.Login)
            {
                LoginCalendar(e);
            }
            else if (!preview && e.Template == EventTemplates.Mobilization && e.Mobilization != null)
            {
                MobilizationBody(e);
            }
            else if (!preview && e.Template == EventTemplates.AllianceDuel && e.Duel != null)
            {
                DuelBody(e);
            }
            else if (!preview && e.Template == EventTemplates.TrenchConquest && e.Trench != null)
            {
                TrenchBody(e);
            }
            else
            {
                if (!preview)
                    EventStats(e);
                EventThemes(e);
                EventActionsList(e);
                EventStages(e);
                EventRankRewards(e);
            }

            var description = EventRules.Text(e.Description, Loc.Current);
            if (description.Length > 0)
            {
                Band(Loc.T("event.about"));
                var card = InkCard(Mathf.Clamp(70 + description.Length * 0.9f, 120, 520));
                Ink(card.transform, description, UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.06f, 0.96f, 0.94f, TextAnchor.UpperLeft);
            }
        }

        /// <summary>The events as picture tabs in a strip fixed under the top bar (it scrolls sideways when they are many).</summary>
        private void EventTabs(EventDto[] events, EventDto selected)
        {
            var holder = Fixed("EventStrip", () => _page == Page.Events);
            holder.anchorMin = new Vector2(0, 1);
            holder.anchorMax = new Vector2(1, 1);
            holder.pivot = new Vector2(0.5f, 1);
            holder.sizeDelta = new Vector2(0, EventStripHeight);
            holder.anchoredPosition = Vector2.zero;
            var shade = holder.gameObject.AddComponent<Image>();
            shade.color = UiFrames.Band;
            var content = UiFrames.IconStrip(holder, out var strip);
            strip.Place(0, 0, 1, 1, 6);
            UiFrames.IconTab(content, null, "quest", Loc.T("event.calendar"), selected == null, OpenCalendar, false, 186f);
            foreach (var e in events)
            {
                var id = e.Id;
                UiFrames.IconTab(content, EventArt(e), "event_gift", EventTitle(e), e.Id == selected?.Id, () =>
                {
                    _eventId = id;
                    _previewId = string.Empty;
                    Render(force: true);
                }, EventRules.HasClaim(e, NowMs), 186f);
            }

            // The list starts under the strip.
            var rect = (RectTransform)_list.parent;
            rect.anchorMax = new Vector2(rect.anchorMax.x, 1);
            rect.offsetMax = new Vector2(rect.offsetMax.x, -EventStripHeight - 10f);
        }

        private static void RedDot(Transform parent)
        {
            var dot = UiKit.Box("Dot", parent, new Color(0.95f, 0.18f, 0.15f, 1f));
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.94f, 0.86f);
            dot.rectTransform.sizeDelta = new Vector2(22, 22);
            dot.raycastTarget = false;
        }

        private void EventBanner(EventDto e)
        {
            // A rounded mask with the banner art, a dark band for the text, title and countdown on it.
            var frame = UiKit.Box("Banner", _list, Color.white).Row(340);
            frame.sprite = UiKit.Rounded;
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var art = UiKit.Rect("Art", frame.transform).Place(0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            art.texture = EventArt(e);
            art.color = art.texture == null ? new Color(0.07f, 0.2f, 0.36f, 1f) : Color.white;
            // The art is 2:1; the card is wider, so show its middle band.
            art.uvRect = new Rect(0f, 0.15f, 1f, 0.7f);
            art.raycastTarget = false;

            var shade = UiKit.Box("Shade", frame.transform, new Color(0.02f, 0.05f, 0.1f, 0.55f));
            shade.sprite = null;
            shade.rectTransform.Place(0, 0, 1, 0.42f);
            shade.raycastTarget = false;

            UiKit.Stroked(UiKit.Label("Title", frame.transform, EventTitle(e), UiKit.TypeScale.Display, Color.white, TextAnchor.LowerLeft, FontStyle.Bold))
                .rectTransform.Place(0.04f, 0.2f, 0.96f, 0.42f);
            // The countdown on a dark pill with the clock, as Last Asylum's event timers.
            var pill = UiKit.Box("Timer", frame.transform, new Color(0.02f, 0.05f, 0.1f, 0.75f));
            pill.sprite = UiKit.Rounded;
            pill.raycastTarget = false;
            pill.rectTransform.Place(0.03f, 0.03f, 0.62f, 0.19f);
            UiKit.Label("Icon", pill.transform, "⏱", UiKit.TypeScale.Body, Color.white, TextAnchor.MiddleCenter).rectTransform.Place(0, 0, 0.13f, 1);
            _eventClock = UiKit.Stroked(UiKit.Label("Clock", pill.transform, ClockText(e), UiKit.TypeScale.Body, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold));
            _eventClock.rectTransform.Place(0.14f, 0, 0.98f, 1);
            _eventClockFor = e;

            var kind = UiKit.Box("Kind", frame.transform, new Color(0.04f, 0.1f, 0.18f, 0.85f));
            kind.sprite = UiKit.Rounded;
            kind.rectTransform.Place(0.62f, 0.8f, 0.97f, 0.95f);
            kind.raycastTarget = false;
            UiKit.Label("Text", kind.transform, Loc.T("event.template." + e.Template), UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0, 0, 1, 1);
        }

        private void EventStats(EventDto e)
        {
            var alliance = EventTemplates.RanksAlliances(e.Template);
            var card = InkCard(150);
            Stat(card.transform, 0.02f, 0.3f, Loc.T("event.myPoints"), e.MyPoints.ToString("N0"));
            if (alliance)
                Stat(card.transform, 0.3f, 0.62f, Loc.T("event.alliancePoints"), e.AllianceRank > 0 ? $"{e.AlliancePoints:N0}  ·  #{e.AllianceRank}" : Loc.T("event.noAlliance"));
            else
                Stat(card.transform, 0.3f, 0.62f, Loc.T("event.myRank"), e.MyRank > 0 ? "#" + e.MyRank.ToString("N0") : "—");

            var id = e.Id;
            CardButton(card.transform, Loc.T("event.ranking"), true, () =>
            {
                _eventId = id;
                _commands.Dispatch(new ViewEventRankingCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = id });
                Open(Page.EventRanking);
            }, UiKit.Primary, 0.64f, 0.2f, 0.97f, 0.8f);
        }

        private static void Stat(Transform card, float x0, float x1, string label, string value)
        {
            Ink(card, label, UiKit.TypeScale.Caption, UiFrames.InkSoft, x0 + 0.02f, 0.55f, x1, 0.92f, TextAnchor.LowerLeft);
            var v = Ink(card, value, UiKit.TypeScale.Subtitle, UiFrames.Ink, x0 + 0.02f, 0.08f, x1, 0.55f, TextAnchor.UpperLeft, FontStyle.Bold);
            v.Fit(16, UiKit.TypeScale.Subtitle);
        }

        /// <summary>The running theme of a themed event and the next ones.</summary>
        private void EventThemes(EventDto e)
        {
            if (e.Phases.Length == 0)
                return;
            var now = NowMs;
            var current = EventRules.CurrentPhase(e, now);
            var next = current >= 0 ? current + 1 : Array.FindIndex(e.Phases, p => p.StartMs > now);
            Band(Loc.T("event.theme"));
            var card = InkCard(150);
            if (current >= 0)
            {
                var phase = e.Phases[current];
                Ink(card.transform, EventRules.Text(phase.Title, Loc.Current), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.04f, 0.45f, 0.66f, 0.92f, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ink(card.transform, Loc.F("event.themeEnds", Span(phase.EndMs - now)), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.04f, 0.08f, 0.66f, 0.45f);
            }
            else
            {
                Ink(card.transform, Loc.T("event.themeNone"), UiKit.TypeScale.Body, UiFrames.InkSoft, 0.04f, 0.1f, 0.66f, 0.9f);
            }

            if (next >= 0 && next < e.Phases.Length)
            {
                Ink(card.transform, Loc.T("event.themeNext"), UiKit.TypeScale.Micro, UiFrames.InkSoft, 0.68f, 0.5f, 0.97f, 0.9f, TextAnchor.LowerRight);
                Ink(card.transform, EventRules.Text(e.Phases[next].Title, Loc.Current), UiKit.TypeScale.Caption, UiFrames.Ink, 0.6f, 0.1f, 0.97f, 0.5f,
                    TextAnchor.UpperRight, FontStyle.Bold);
            }
        }

        private void EventActionsList(EventDto e)
        {
            var actions = EventRules.Phase(e, NowMs) == EventPhase.Live ? EventRules.ActiveActions(e, NowMs)
                : e.Phases.Length > 0 ? e.Phases[0].Actions : e.Actions;
            if (actions.Length == 0)
                return;
            Band(Loc.T("event.howToScore"));
            for (var i = 0; i < actions.Length; i++)
            {
                var a = actions[i];
                var card = InkCard(96, i % 2 == 0 ? UiFrames.Card : UiFrames.CardAlt);
                HudIconImage(card.transform, ActionIcon(a.Action), 0.02f, 0.1f, 0.12f, 0.9f);
                Ink(card.transform, Loc.T("event.action." + a.Action), UiKit.TypeScale.Body, UiFrames.Ink, 0.15f, 0.05f, 0.64f, 0.95f);
                var points = a.Per > 1 ? Loc.F("event.pointsPer", a.Points.ToString("N0"), a.Per.ToString("N0")) : Loc.F("event.points", a.Points.ToString("N0"));
                Ink(card.transform, points, UiKit.TypeScale.Body, UiFrames.InkGood, 0.6f, 0.05f, 0.97f, 0.95f, TextAnchor.MiddleRight, FontStyle.Bold);
            }
        }

        private static string ActionIcon(string action) => action switch
        {
            EventActions.Gather => "res_titanium",
            EventActions.BuildPower => "queue_build",
            EventActions.Research => "left_research",
            EventActions.Heal => "left_help",
            EventActions.MonsterKill or EventActions.MonsterLevels or EventActions.Dive => "event_war",
            EventActions.HeroPull => "event_pod",
            EventActions.HeroLevel or EventActions.HeroRecruit => "nav_heroes",
            EventActions.SpeedupMinutes => "queue_build",
            EventActions.HadaliteSpent => "res_hadalite",
            EventActions.Help => "side_alliance",
            EventActions.Salvage => "queue_drone",
            EventActions.DailyPoints => "quest",
            _ when action.StartsWith(EventActions.TrainPrefix, StringComparison.Ordinal) => "power",
            _ => "event_gift"
        };

        /// <summary>Stage goals as Last Asylum's quest rows: "Stage 2 (1,200/3,000)", the reward tiles, Claim or what is left.</summary>
        private void EventStages(EventDto e)
        {
            if (e.Stages.Length == 0)
                return;
            Band(Loc.T("event.stages"));
            var claimed = new HashSet<int>(e.Claimed);
            // Claimed on the page while the event runs; what is left at the end comes by mail.
            var open = EventRules.Phase(e, NowMs) == EventPhase.Live;
            for (var i = 0; i < e.Stages.Length; i++)
            {
                var stage = e.Stages[i];
                var index = i;
                var done = claimed.Contains(i);
                var reached = e.MyPoints >= stage.Points;
                var title = $"{Loc.F("event.stage", i + 1)}  ({Math.Min(e.MyPoints, stage.Points):N0}/{stage.Points:N0})";
                var action = done ? Loc.T(e.PaidOut ? "event.byMail" : "event.claimed") : reached ? Loc.T(open ? "quest.claim" : "event.byMail")
                    : Loc.F("event.pointsLeft", (stage.Points - e.MyPoints).ToString("N0"));
                var row = UiFrames.QuestRow(_list, title, RewardTiles(stage.Rewards), action, true, () => ClaimStage(e.Id, index), open && reached && !done).Row(200);
                if (done)
                    row.GetComponent<Image>().color = UiFrames.CardAlt;
                RewardsHint(row, stage.Rewards);
            }
        }

        /// <summary>Rank rewards as tile rows: the place range and its rewards; the band the player is in now is gold.</summary>
        private void EventRankRewards(EventDto e)
        {
            if (e.RankBands.Length == 0)
                return;
            var alliance = EventTemplates.RanksAlliances(e.Template);
            Band(Loc.T(alliance ? "event.rankRewards.alliance" : "event.rankRewards"));
            var rank = alliance ? e.AllianceRank : e.MyRank;
            var mine = EventRules.Band(e, rank);
            foreach (var band in e.RankBands)
            {
                var label = band.From == band.To ? $"#{band.From}" : $"#{band.From}–{band.To}";
                var row = UiFrames.QuestRow(_list, label, RewardTiles(band.Rewards), null, false, null).Row(190);
                if (band == mine)
                    row.GetComponent<Image>().color = UiFrames.Highlight;
                RewardsHint(row, band.Rewards);
            }

            // Rank rewards go out by mail when the event ends (docs/29 §2.2): the page only says so.
            InkNote(AllianceEventText.RankNote(e, EventRules.Phase(e, NowMs)));
        }

        /// <summary>The first five rewards as quest-row tiles (icon, rarity, amount).</summary>
        private (string Icon, string Rarity, string Count)[] RewardTiles(RewardEntryDto[] rewards) =>
            (rewards ?? Array.Empty<RewardEntryDto>()).Take(5).Select(r => (RewardTileIcon(r), RewardTone(r), RewardAmount(r))).ToArray();

        private string RewardTileIcon(RewardEntryDto r) =>
            r.Type == "resource" && GameCatalog.TryParseResource(r.Id, out var resource) ? Hud.HudIcons.Resource(resource)
            : r.Type == "item" ? QuestText.ItemIcon(r.Id, _catalog.TryGetItem(r.Id, out var item) ? item.kind : string.Empty)
            : "nav_heroes";

        /// <summary>Tapping a reward row's tiles names all its rewards (the row shows at most five).</summary>
        private void RewardsHint(RectTransform row, RewardEntryDto[] rewards)
        {
            var all = rewards ?? Array.Empty<RewardEntryDto>();
            var hit = UiKit.Rect("Hint", row).Place(0.02f, 0.05f, 0.68f, 0.62f);
            hit.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            hit.gameObject.AddComponent<Button>().onClick.AddListener(() => Toast(string.Join("  ·  ", all.Select(RewardText)), UiKit.Accent));
        }

        /// <summary>Sign-in rewards: four days a row; today's unclaimed day glows and claims on tap.</summary>
        private void LoginCalendar(EventDto e)
        {
            var card = InkCard(110);
            Ink(card.transform, Loc.F("event.loginDays", e.LoginDays, e.Stages.Length), UiKit.TypeScale.Body + 2, UiFrames.Ink, 0.04f, 0.1f, 0.96f, 0.9f,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            var claimed = new HashSet<int>(e.Claimed);
            var open = EventRules.Phase(e, NowMs) == EventPhase.Live; // the days left at the end come by mail
            var grid = UiFrames.Grid(_list, 4, 0.8f, 12f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 6, 6);
            for (var index = 0; index < e.Stages.Length; index++)
            {
                var done = claimed.Contains(index);
                var ready = open && !done && index < e.LoginDays;
                var cell = UiKit.Rect("Day", grid);
                var tile = Skin.Slot(cell, done ? "common" : ready ? "legendary" : "rare");
                tile.rectTransform.Place(0, 0, 1, 1);
                tile.color = done || ready ? Color.white : new Color(0.72f, 0.76f, 0.84f, 1f);
                tile.raycastTarget = true;
                UiKit.Stroked(UiKit.Label("Day", tile.transform, Loc.F("event.day", index + 1), UiKit.TypeScale.Caption, Color.white, TextAnchor.UpperCenter, FontStyle.Bold))
                    .rectTransform.Place(0, 0.78f, 1, 0.97f);
                var reward = e.Stages[index].Rewards.FirstOrDefault();
                if (reward != null)
                {
                    RewardIcon(tile.transform, reward, 0.2f, 0.28f, 0.8f, 0.78f);
                    UiKit.Stroked(UiKit.Label("Amount", tile.transform, RewardAmount(reward), UiKit.TypeScale.Caption, Color.white, TextAnchor.LowerCenter, FontStyle.Bold))
                        .rectTransform.Place(0, 0.04f, 1, 0.3f);
                }

                if (done)
                    UiKit.Stroked(UiKit.Label("Done", tile.transform, "✓", 64, UiKit.Ok, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0.2f, 1, 0.8f);
                if (ready)
                    UiFrames.Dot(tile.transform);

                var id = e.Id;
                var day = index;
                var rewards = e.Stages[index].Rewards;
                var button = tile.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    if (ready)
                        ClaimStage(id, day);
                    else
                        Toast(string.Join("  ·  ", rewards.Select(RewardText)), UiKit.Accent);
                });
                tile.gameObject.AddComponent<PressFeedback>();
            }
        }

        private void ClaimStage(string eventId, int index) =>
            _commands.Dispatch(new ClaimEventStageCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = eventId, Index = index });

        // --- Rewards -----------------------------------------------------------------------

        /// <summary>
        /// Square reward tiles in a row from the left, as many as fit; tapping the strip names them all.
        /// <paramref name="cardHeight"/> is the card's row height, so the tiles can be sized before layout.
        /// </summary>
        private void RewardStrip(Transform card, RewardEntryDto[] rewards, float x0, float y0, float x1, float y1, float cardHeight)
        {
            var strip = UiKit.Rect("Rewards", card).Place(x0, y0, x1, y1);
            var size = (y1 - y0) * cardHeight * UiKit.Scale;
            var room = (x1 - x0) * 1000f; // the list is about 1000 reference pixels wide
            var shown = Math.Min(rewards.Length, Math.Max(1, (int)((room + 8) / (size + 8))));
            for (var i = 0; i < shown; i++)
            {
                var r = rewards[i];
                var slot = Skin.Slot(strip, RewardTone(r));
                var rect = slot.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(i * (size + 8f), 0f);
                RewardIcon(slot.transform, r, 0.12f, 0.24f, 0.88f, 0.92f);
                UiKit.Stroked(UiKit.Label("Amount", slot.transform, RewardAmount(r), 20, Color.white, TextAnchor.LowerCenter, FontStyle.Bold))
                    .rectTransform.Place(0, 0.02f, 1, 0.34f);
            }

            var all = rewards;
            strip.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            strip.gameObject.AddComponent<Button>().onClick.AddListener(() => Toast(string.Join("  ·  ", all.Select(RewardText)), UiKit.Accent));
        }

        private void RewardIcon(Transform parent, RewardEntryDto r, float x0, float y0, float x1, float y1)
        {
            if ((r.Type == "shards" || r.Type == "hero") && _catalog.TryGetHero(r.Id, out var hero))
            {
                Portrait(parent, hero, true, x0, y0, x1, y1);
                return;
            }

            HudIconImage(parent, RewardTileIcon(r), x0, y0, x1, y1);
        }

        private static string RewardAmount(RewardEntryDto r) =>
            r.Type == "resource" ? CostChips.Compact(r.Amount) : "×" + r.Amount.ToString("N0");

        private static string RewardText(RewardEntryDto r)
        {
            if (r.Type == "shards" && r.Id.StartsWith("rarity:", StringComparison.Ordinal))
                return QuestText.Reward("shards", string.Empty, r.Amount, r.Id.Substring(7));
            return QuestText.Reward(r.Type, r.Id, r.Amount);
        }

        private string RewardTone(RewardEntryDto r)
        {
            if (r.Type == "resource")
                return r.Id == "Hadalite" ? "epic" : r.Amount >= 100_000 ? "legendary" : r.Amount >= 10_000 ? "rare" : "common";
            if (r.Type == "shards")
                return r.Id.StartsWith("rarity:", StringComparison.Ordinal) ? r.Id.Substring(7)
                    : _catalog.TryGetHero(r.Id, out var hero) ? hero.Rarity.id : "rare";
            return _catalog.TryGetItem(r.Id, out var item) && !string.IsNullOrEmpty(item.rarity) ? item.rarity : "rare";
        }

        // --- Ranking -----------------------------------------------------------------------

        /// <summary>The event's ranking as the genre's table; the player's (or alliance's) own row pinned green under it.</summary>
        private void RenderEventRanking()
        {
            var e = SelectedEvent();
            PageTitle(e != null ? EventTitle(e) : Loc.T("event.title"));

            var ranking = View.EventRanking(_eventId);
            if (ranking == null)
            {
                InkNote(Loc.T("event.loading"));
                return;
            }

            Band(Loc.T(ranking.Alliance ? "event.topAlliances" : "event.topPlayers"));
            if (ranking.Entries.Length == 0)
            {
                InkNote(Loc.T("event.rankingEmpty"));
            }
            else
            {
                RankTableHead(Loc.T("rank.col.points"));
                for (var i = 0; i < ranking.Entries.Length; i++)
                    EventRankRow(_list, ranking.Entries[i], ranking.Alliance, false, i).Row(110);
            }

            if (ranking.Mine != null)
            {
                var shownFor = _eventId;
                var pinned = PinnedRow(() => _page == Page.EventRanking && _eventId == shownFor, 110);
                EventRankRow(pinned, ranking.Mine, ranking.Alliance, true, 0).Place(0, 0, 1, 1);
            }
        }

        private RectTransform EventRankRow(Transform parent, EventRankEntryDto entry, bool alliance, bool mine, int index)
        {
            var name = alliance || !string.IsNullOrEmpty(entry.Tag) ? $"[{entry.Tag}] {entry.Name}" : entry.Name;
            var row = UiFrames.RankRow(parent, entry.Rank, name, entry.Points.ToString("N0"), mine, index, out var face);
            if (alliance)
                AvatarDisc.Emblem(face, entry.Avatar);
            else
                AvatarDisc.Draw(face, entry.Avatar, entry.Name, entry.Id);
            return row;
        }
    }
}
