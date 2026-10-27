using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The event centre (docs/29): one tab per event the Overseer runs. A banner with the countdown, the player's points
    /// and rank, the running theme, the ways to score, stage chests, rank rewards, the ranking, and for sign-in events
    /// a calendar of daily rewards.
    /// </summary>
    public sealed partial class SocialView
    {
        private string _eventId = string.Empty;
        private Text _eventClock;
        private EventDto _eventClockFor;
        private Page _afterReward = Page.None;

        public void OpenEvents()
        {
            _afterReward = Page.None;
            Open(Page.Events);
        }

        private string EventsKey() => $"{View.EventsVersion}|{View.EventRankingVersion}|{_eventId}";

        private long NowMs => View.ServerNowMs;

        private static string EventTitle(EventDto e) => EventRules.Text(e.Title, Loc.Current);

        /// <summary>The selected event, or the first one asking for a claim, or the first one.</summary>
        private EventDto SelectedEvent()
        {
            var events = View.Events.Events ?? Array.Empty<EventDto>();
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

        // --- Page --------------------------------------------------------------------------

        private void RenderEvents()
        {
            _title.text = Loc.T("event.title");
            _subtitle.text = string.Empty;
            _eventClock = null;
            _eventClockFor = null;

            var events = View.Events.Events ?? Array.Empty<EventDto>();
            if (events.Length == 0)
            {
                Note(Loc.T("event.none"));
                return;
            }

            var e = SelectedEvent();
            if (events.Length > 1)
                EventTabs(events, e);
            EventBanner(e);

            if (e.Template == EventTemplates.Login)
            {
                LoginCalendar(e);
            }
            else
            {
                EventStats(e);
                EventThemes(e);
                EventActionsList(e);
                EventStages(e);
                EventRankRewards(e);
            }

            var description = EventRules.Text(e.Description, Loc.Current);
            if (description.Length > 0)
            {
                Header(Loc.T("event.about"));
                var card = Card(Mathf.Clamp(70 + description.Length * 0.9f, 120, 520));
                CardText(card.transform, description, 28, UiKit.Text, 0.04f, 0.06f, 0.96f, 0.94f);
            }
        }

        private void EventTabs(EventDto[] events, EventDto selected)
        {
            // Up to four tabs a row; more events wrap onto another row.
            for (var start = 0; start < events.Length; start += 4)
            {
                var row = UiKit.Rect("Tabs", _list).Row(96);
                var count = Math.Min(4, events.Length - start);
                for (var i = 0; i < count; i++)
                {
                    var e = events[start + i];
                    var on = e.Id == selected.Id;
                    var id = e.Id;
                    var tab = UiKit.Button("Tab", row, EventTitle(e), () =>
                    {
                        _eventId = id;
                        Render(force: true);
                    }, on ? UiKit.TabOn : UiKit.TabOff, 26);
                    ((RectTransform)tab.transform).Place((float)i / 4, 0, (float)(i + 1) / 4, 1, 4);
                    if (EventRules.HasClaim(e, NowMs))
                        RedDot(tab.transform);
                }
            }
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
            art.texture = Resources.Load<Texture2D>("Events/banner_" + e.Banner) ?? Resources.Load<Texture2D>("Events/banner_abyss");
            art.color = art.texture == null ? new Color(0.07f, 0.2f, 0.36f, 1f) : Color.white;
            // The art is 2:1; the card is wider, so show its middle band.
            art.uvRect = new Rect(0f, 0.15f, 1f, 0.7f);
            art.raycastTarget = false;

            var shade = UiKit.Box("Shade", frame.transform, new Color(0.02f, 0.05f, 0.1f, 0.55f));
            shade.sprite = null;
            shade.rectTransform.Place(0, 0, 1, 0.42f);
            shade.raycastTarget = false;

            UiKit.Stroked(UiKit.Label("Title", frame.transform, EventTitle(e), 50, Color.white, TextAnchor.LowerLeft, FontStyle.Bold))
                .rectTransform.Place(0.04f, 0.2f, 0.96f, 0.42f);
            _eventClock = UiKit.Stroked(UiKit.Label("Clock", frame.transform, ClockText(e), 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold));
            _eventClock.rectTransform.Place(0.04f, 0.03f, 0.7f, 0.2f);
            _eventClockFor = e;

            var kind = UiKit.Box("Kind", frame.transform, new Color(0.04f, 0.1f, 0.18f, 0.85f));
            kind.rectTransform.Place(0.62f, 0.8f, 0.97f, 0.95f);
            kind.raycastTarget = false;
            UiKit.Label("Text", kind.transform, Loc.T("event.template." + e.Template), 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0, 0, 1, 1);
        }

        private void EventStats(EventDto e)
        {
            var alliance = e.Template == EventTemplates.Alliance;
            var card = Card(150);
            Stat(card.transform, 0.02f, 0.3f, Loc.T("event.myPoints"), e.MyPoints.ToString("N0"));
            if (alliance)
            {
                Stat(card.transform, 0.3f, 0.58f, Loc.T("event.alliancePoints"), e.AllianceRank > 0 ? $"{e.AlliancePoints:N0}  ·  #{e.AllianceRank}" : Loc.T("event.noAlliance"));
            }
            else
            {
                Stat(card.transform, 0.3f, 0.58f, Loc.T("event.myRank"), e.MyRank > 0 ? "#" + e.MyRank.ToString("N0") : "—");
            }

            var id = e.Id;
            CardButton(card.transform, Loc.T("event.ranking"), true, () =>
            {
                _eventId = id;
                _commands.Dispatch(new ViewEventRankingCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = id });
                Open(Page.EventRanking);
            }, UiKit.Primary, 0.62f, 0.2f, 0.97f, 0.8f);
        }

        private static void Stat(Transform card, float x0, float x1, string label, string value)
        {
            CardText(card, label, 22, UiKit.Muted, x0 + 0.02f, 0.55f, x1, 0.92f, TextAnchor.LowerLeft);
            CardText(card, value, 36, UiKit.Text, x0 + 0.02f, 0.08f, x1, 0.55f, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        /// <summary>The running theme of a themed event and the next ones.</summary>
        private void EventThemes(EventDto e)
        {
            if (e.Phases.Length == 0)
                return;
            var now = NowMs;
            var current = EventRules.CurrentPhase(e, now);
            var next = current >= 0 ? current + 1 : Array.FindIndex(e.Phases, p => p.StartMs > now);
            Header(Loc.T("event.theme"));
            var card = Card(150);
            if (current >= 0)
            {
                var phase = e.Phases[current];
                CardText(card.transform, EventRules.Text(phase.Title, Loc.Current), 36, UiKit.Text, 0.04f, 0.45f, 0.7f, 0.92f, TextAnchor.MiddleLeft, FontStyle.Bold);
                CardText(card.transform, Loc.F("event.themeEnds", Span(phase.EndMs - now)), 24, UiKit.Muted, 0.04f, 0.08f, 0.7f, 0.45f, TextAnchor.MiddleLeft);
            }
            else
            {
                CardText(card.transform, Loc.T("event.themeNone"), 28, UiKit.Muted, 0.04f, 0.1f, 0.7f, 0.9f, TextAnchor.MiddleLeft);
            }

            if (next >= 0 && next < e.Phases.Length)
            {
                CardText(card.transform, Loc.T("event.themeNext"), 20, UiKit.Muted, 0.7f, 0.5f, 0.97f, 0.9f, TextAnchor.LowerRight);
                CardText(card.transform, EventRules.Text(e.Phases[next].Title, Loc.Current), 24, UiKit.Amber, 0.6f, 0.1f, 0.97f, 0.5f, TextAnchor.UpperRight, FontStyle.Bold);
            }
        }

        private void EventActionsList(EventDto e)
        {
            var actions = EventRules.Phase(e, NowMs) == EventPhase.Live ? EventRules.ActiveActions(e, NowMs)
                : e.Phases.Length > 0 ? e.Phases[0].Actions : e.Actions;
            if (actions.Length == 0)
                return;
            Header(Loc.T("event.howToScore"));
            foreach (var a in actions)
            {
                var card = Card(90);
                HudIconImage(card.transform, ActionIcon(a.Action), 0.02f, 0.1f, 0.12f, 0.9f);
                CardText(card.transform, Loc.T("event.action." + a.Action), 28, UiKit.Text, 0.15f, 0.05f, 0.66f, 0.95f, TextAnchor.MiddleLeft);
                var points = a.Per > 1 ? Loc.F("event.pointsPer", a.Points.ToString("N0"), a.Per.ToString("N0")) : Loc.F("event.points", a.Points.ToString("N0"));
                CardText(card.transform, points, 28, UiKit.Ok, 0.6f, 0.05f, 0.97f, 0.95f, TextAnchor.MiddleRight, FontStyle.Bold);
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

        private void EventStages(EventDto e)
        {
            if (e.Stages.Length == 0)
                return;
            Header(Loc.T("event.stages"));
            var claimed = new HashSet<int>(e.Claimed);
            var open = EventRules.Phase(e, NowMs) is EventPhase.Live or EventPhase.Ended;
            for (var i = 0; i < e.Stages.Length; i++)
            {
                var stage = e.Stages[i];
                var card = Card(170);
                CardText(card.transform, Loc.F("event.stage", i + 1), 24, UiKit.Muted, 0.03f, 0.66f, 0.4f, 0.94f, TextAnchor.MiddleLeft, FontStyle.Bold);
                ProgressBar(card.transform, Math.Min(e.MyPoints, stage.Points), stage.Points, 0.03f, 0.42f, 0.4f, 0.62f);
                RewardStrip(card.transform, stage.Rewards, 0.43f, 0.1f, 0.74f, 0.9f, 170);

                var index = i;
                var done = claimed.Contains(i);
                var reached = e.MyPoints >= stage.Points;
                var button = CardButton(card.transform, done ? Loc.T("event.claimed") : Loc.T("quest.claim"), open && reached && !done,
                    () => ClaimStage(e.Id, index), UiKit.Gold, 0.76f, 0.24f, 0.97f, 0.76f);
                if (!reached && !done)
                    button.GetComponentInChildren<Text>().text = Loc.F("event.pointsLeft", (stage.Points - e.MyPoints).ToString("N0"));
            }
        }

        private void EventRankRewards(EventDto e)
        {
            if (e.RankBands.Length == 0)
                return;
            var alliance = e.Template == EventTemplates.Alliance;
            Header(Loc.T(alliance ? "event.rankRewards.alliance" : "event.rankRewards"));
            var rank = alliance ? e.AllianceRank : e.MyRank;
            var mine = EventRules.Band(e, rank);
            foreach (var band in e.RankBands)
            {
                var card = Card(130);
                if (band == mine)
                    card.color = new Color(1f, 0.93f, 0.7f, 1f);
                var label = band.From == band.To ? $"#{band.From}" : $"#{band.From}–{band.To}";
                CardText(card.transform, label, 34, UiKit.Text, 0.03f, 0.1f, 0.19f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
                RewardStrip(card.transform, band.Rewards, 0.2f, 0.1f, 0.97f, 0.9f, 130);
            }

            var phase = EventRules.Phase(e, NowMs);
            var eligible = mine != null && e.MyPoints > 0 && e.MyPoints >= e.MinRankPoints;
            var claim = Card(120);
            if (phase == EventPhase.Ended && eligible && !e.RankClaimed)
            {
                var id = e.Id;
                CardButton(claim.transform, Loc.T("event.claimRank"), true, () =>
                    _commands.Dispatch(new ClaimEventRankCommand { CommandId = NewId(), ClientSequence = NextSequence(), EventId = id }),
                    UiKit.Gold, 0.2f, 0.12f, 0.8f, 0.88f);
            }
            else
            {
                var note = e.RankClaimed ? Loc.T("event.rankClaimed")
                    : phase == EventPhase.Ended ? Loc.T("event.rankNone")
                    : e.MinRankPoints > 0 ? Loc.F("event.rankAtEndMin", e.MinRankPoints.ToString("N0"))
                    : Loc.T("event.rankAtEnd");
                CardText(claim.transform, note, 26, UiKit.Muted, 0.04f, 0.1f, 0.96f, 0.9f, TextAnchor.MiddleCenter);
            }
        }

        /// <summary>Sign-in rewards: four days a row; today's unclaimed day glows and claims on tap.</summary>
        private void LoginCalendar(EventDto e)
        {
            var card = Card(120);
            CardText(card.transform, Loc.F("event.loginDays", e.LoginDays, e.Stages.Length), 32, UiKit.Text, 0.04f, 0.1f, 0.96f, 0.9f, TextAnchor.MiddleCenter, FontStyle.Bold);

            var claimed = new HashSet<int>(e.Claimed);
            var open = EventRules.Phase(e, NowMs) is EventPhase.Live or EventPhase.Ended;
            for (var start = 0; start < e.Stages.Length; start += 4)
            {
                var row = UiKit.Rect("Days", _list).Row(250);
                for (var c = 0; c < 4 && start + c < e.Stages.Length; c++)
                {
                    var index = start + c;
                    var done = claimed.Contains(index);
                    var ready = open && !done && index < e.LoginDays;
                    var tile = Skin.Slot(row, done ? "common" : ready ? "legendary" : "rare");
                    tile.rectTransform.Place(c * 0.25f + 0.01f, 0.02f, c * 0.25f + 0.24f, 0.98f);
                    tile.color = done || ready ? Color.white : new Color(0.72f, 0.76f, 0.84f, 1f);
                    tile.raycastTarget = true;
                    UiKit.Stroked(UiKit.Label("Day", tile.transform, Loc.F("event.day", index + 1), 24, Color.white, TextAnchor.UpperCenter, FontStyle.Bold))
                        .rectTransform.Place(0, 0.78f, 1, 0.97f);
                    var reward = e.Stages[index].Rewards.FirstOrDefault();
                    if (reward != null)
                    {
                        RewardIcon(tile.transform, reward, 0.2f, 0.28f, 0.8f, 0.78f);
                        UiKit.Stroked(UiKit.Label("Amount", tile.transform, RewardAmount(reward), 24, Color.white, TextAnchor.LowerCenter, FontStyle.Bold))
                            .rectTransform.Place(0, 0.04f, 1, 0.3f);
                    }

                    if (done)
                        UiKit.Stroked(UiKit.Label("Done", tile.transform, "✓", 64, UiKit.Ok, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0.2f, 1, 0.8f);

                    var id = e.Id;
                    var rewards = e.Stages[index].Rewards;
                    var button = tile.gameObject.AddComponent<Button>();
                    button.onClick.AddListener(() =>
                    {
                        if (ready)
                            ClaimStage(id, index);
                        else
                            Toast(string.Join("  ·  ", rewards.Select(RewardText)), UiKit.Accent);
                    });
                    tile.gameObject.AddComponent<PressFeedback>();
                }
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

            var icon = r.Type == "resource" && GameCatalog.TryParseResource(r.Id, out var resource) ? Hud.HudIcons.Resource(resource)
                : r.Type == "item" ? QuestText.ItemIcon(r.Id, _catalog.TryGetItem(r.Id, out var item) ? item.kind : string.Empty)
                : "nav_heroes";
            HudIconImage(parent, icon, x0, y0, x1, y1);
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

        private void RenderEventRanking()
        {
            var e = SelectedEvent();
            _title.text = e != null ? EventTitle(e) : Loc.T("event.title");
            _subtitle.text = Loc.T("event.ranking");

            var back = Card(96);
            CardButton(back.transform, "‹ " + Loc.T("hero.back"), true, () => Open(Page.Events), UiKit.Field, 0.01f, 0.08f, 0.35f, 0.92f);

            var ranking = View.EventRanking(_eventId);
            if (ranking == null)
            {
                Note(Loc.T("event.loading"));
                return;
            }

            if (ranking.Mine != null)
            {
                Header(Loc.T("event.you"));
                RankRow(ranking.Mine, ranking.Alliance, highlight: true);
            }

            Header(Loc.T(ranking.Alliance ? "event.topAlliances" : "event.topPlayers"));
            if (ranking.Entries.Length == 0)
                Note(Loc.T("event.rankingEmpty"));
            foreach (var entry in ranking.Entries)
                RankRow(entry, ranking.Alliance, highlight: false);
        }

        private void RankRow(EventRankEntryDto entry, bool alliance, bool highlight)
        {
            var card = Card(110);
            if (highlight)
                card.color = new Color(1f, 0.93f, 0.7f, 1f);

            // Medals for the podium, a plain number after it.
            var medal = entry.Rank switch
            {
                1 => new Color(1f, 0.78f, 0.25f, 1f),
                2 => new Color(0.78f, 0.82f, 0.88f, 1f),
                3 => new Color(0.86f, 0.55f, 0.3f, 1f),
                _ => new Color(0, 0, 0, 0)
            };
            var badge = UiKit.Box("Medal", card.transform, medal);
            badge.sprite = UiKit.Circle;
            badge.type = Image.Type.Simple;
            badge.preserveAspect = true;
            badge.rectTransform.Place(0.01f, 0.1f, 0.13f, 0.9f);
            badge.raycastTarget = false;
            var number = UiKit.Label("Rank", badge.transform, entry.Rank.ToString("N0"), entry.Rank <= 3 ? 32 : 28, entry.Rank <= 3 ? Color.white : UiKit.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            number.rectTransform.Place(0, 0, 1, 1);
            if (entry.Rank <= 3)
                UiKit.Stroked(number);

            var face = UiKit.Rect("Face", card.transform).Place(0.14f, 0.08f, 0.26f, 0.92f);
            if (alliance)
                AvatarDisc.Emblem(face, entry.Avatar);
            else
                AvatarDisc.Draw(face, entry.Avatar, entry.Name, entry.Id);

            var name = alliance ? $"[{entry.Tag}] {entry.Name}" : string.IsNullOrEmpty(entry.Tag) ? entry.Name : $"[{entry.Tag}] {entry.Name}";
            var label = CardText(card.transform, name, 30, UiKit.Text, 0.28f, 0.1f, 0.72f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.supportRichText = false;
            CardText(card.transform, entry.Points.ToString("N0"), 30, UiKit.Ok, 0.7f, 0.1f, 0.97f, 0.9f, TextAnchor.MiddleRight, FontStyle.Bold);
        }
    }
}
