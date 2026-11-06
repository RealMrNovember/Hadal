using System.Collections.Concurrent;
using System.Threading.Channels;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Events
{
    /// <summary>
    /// The event engine (docs/29 §2.2): Overseer-defined events, scoring from what players do, rankings, stage and
    /// rank rewards. Everything lives in memory under one lock; scores are written behind in batches. Lock order:
    /// events, then social hub, then session — sessions never call in here (their actions are taken each tick).
    /// </summary>
    public sealed partial class EventService
    {
        private sealed class Live
        {
            public EventDefinition Def = null!;
            public EventDto Shape = null!;
            public readonly Dictionary<string, EventScore> Scores = new(StringComparer.Ordinal);
            public readonly Dictionary<string, long> Alliances = new(StringComparer.Ordinal);
            public Dictionary<string, int>? PlayerRanks;
            public Dictionary<string, int>? AllianceRanks;

            /// <summary>Each scoring alliance's kingdom (that of the members who scored for it).</summary>
            public Dictionary<string, string>? AllianceKingdoms;
            public List<EventScore>? Sorted;
            public List<KeyValuePair<string, long>>? SortedAlliances;
            public bool Dirty = true;
            public DateTimeOffset SortedAt;
            public EventPhase Phase;

            /// <summary>Per alliance: the Mobilization pool and the duel's day points (EventAllianceState).</summary>
            public readonly Dictionary<string, EventAllianceState> States = new(StringComparer.Ordinal);

            /// <summary>The end-of-event payout is on its way to the mailbox (EventService.Payout).</summary>
            public bool Paying;
        }

        private static readonly TimeSpan HousekeepingEvery = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ResortEvery = TimeSpan.FromSeconds(3);

        private readonly object _gate = new();
        private readonly IEventStore _store;
        private readonly CatalogRuntime _runtime;
        private readonly INetworkSerializer _serializer;
        private readonly ILogger _logger;
        private readonly Dictionary<string, Live> _events = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerSession> _online = new(StringComparer.Ordinal);
        private readonly HashSet<EventScore> _dirtyScores = new();
        private readonly HashSet<(string EventId, string AllianceId)> _dirtyAlliances = new();
        private readonly HashSet<(string EventId, string AllianceId)> _dirtyStates = new();
        private readonly Channel<Func<CancellationToken, Task>> _writes = Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions { SingleReader = true });
        private DateTimeOffset _nextHousekeeping;

        /// <summary>Base names, avatars and alliance data for rankings (set by the world).</summary>
        public Func<string, string?>? NameLookup { get; set; }
        public Func<string, string?>? AvatarLookup { get; set; }
        public Func<string, string>? AllianceIdOf { get; set; }

        /// <summary>A player's kingdom (docs/39): rankings and rank rewards are per kingdom.</summary>
        public Func<string, string>? KingdomOf { get; set; }

        private string Kingdom(string playerId) => KingdomOf?.Invoke(playerId) ?? HADAL.Shared.Simulation.AllianceLandRules.DefaultWorld;
        public Func<string, string>? TagOf { get; set; }
        public Func<string, (string Tag, string Name, string Emblem)?>? AllianceInfo { get; set; }

        /// <summary>Drops an alliance gift for a player (docs/31 step 5): set by the world to SocialHub.DropGift.</summary>
        public Action<string, string, long, DateTimeOffset>? DropGift { get; set; }

        public EventService(IEventStore store, CatalogRuntime runtime, INetworkSerializer serializer, ILogger logger)
        {
            _store = store;
            _runtime = runtime;
            _serializer = serializer;
            _logger = logger;
            _ = Task.Run(WriteLoopAsync);
        }

        private GameCatalog Catalog => _runtime.Current.Catalog;

        // --- Event bosses (WorldService.Bosses) ---------------------------------------------------

        private volatile EventBossSpawn[] _liveBosses = Array.Empty<EventBossSpawn>();

        /// <summary>The bosses of published, live events; a snapshot the world map reads without taking this lock.</summary>
        public IReadOnlyList<EventBossSpawn> LiveBosses() => _liveBosses;

        /// <summary>Rebuilds the boss snapshot. Caller holds the lock.</summary>
        private void RefreshBosses(DateTimeOffset now) =>
            _liveBosses = _events.Values
                .Where(e => e.Def.Status == EventStatus.Published && PhaseOf(e.Def, now) == EventPhase.Live)
                .SelectMany(e => e.Def.Bosses.Select((b, i) =>
                    {
                        var progress = e.Def.BossProgress.FirstOrDefault(p => p.Index == i);
                        return new EventBossSpawn(e.Def.Id, i, b.Kind, b.Level, b.X, b.Y, e.Def.EndsAt, progress?.Left ?? -1,
                            progress == null ? null : new Dictionary<string, long>(progress.Damage, StringComparer.Ordinal),
                            e.Def.Kingdoms.FirstOrDefault() ?? HADAL.Shared.Simulation.AllianceLandRules.DefaultWorld);
                    })
                    .Where(b => !e.Def.PaidBosses.Contains(b.Index))
                    .Concat(e.Def.Template == EventTemplates.LureTrap ? LureSpawns(e) : Enumerable.Empty<EventBossSpawn>()))
                .ToArray();

        private readonly ConcurrentQueue<(string EventId, int Index, string PlayerId, long Delta, long Left)> _bossHits = new();

        /// <summary>
        /// A hit on an event boss (a negative delta takes a recalled hit back) with the body left after it. Lock-free: the
        /// world calls it under its own lock; the hits are folded into the stored definition on the next housekeeping.
        /// </summary>
        public void RecordBossHit(string eventId, int index, string playerId, long delta, long left) =>
            _bossHits.Enqueue((eventId, index, playerId, delta, left));

        /// <summary>Writes the queued boss hits into the definitions (write-behind, one save per event). Caller holds the lock.</summary>
        private void FlushBossHits()
        {
            var changed = new HashSet<Live>();
            while (_bossHits.TryDequeue(out var hit))
            {
                if (!_events.TryGetValue(hit.EventId, out var live) || live.Def.PaidBosses.Contains(hit.Index))
                    continue;
                var progress = live.Def.BossProgress.FirstOrDefault(p => p.Index == hit.Index);
                if (progress == null)
                    live.Def.BossProgress.Add(progress = new EventBossProgress { Index = hit.Index });
                progress.Left = Math.Max(0, hit.Left);
                if (live.Def.Template == EventTemplates.LureTrap)
                    RecordLureHit(live, hit.Index, hit.PlayerId, hit.Delta, DateTimeOffset.UtcNow);
                var total = (progress.Damage.TryGetValue(hit.PlayerId, out var had) ? had : 0) + hit.Delta;
                if (total > 0)
                    progress.Damage[hit.PlayerId] = total;
                else
                    progress.Damage.Remove(hit.PlayerId);
                changed.Add(live);
            }

            foreach (var live in changed)
            {
                var copy = live.Def.Clone();
                Write(ct => _store.SaveEventAsync(copy, ct));
            }
        }

        /// <summary>
        /// The prizes of a boss that died or whose event ended (EventBossRewards.Plan), once: the boss is marked paid in the
        /// stored definition first, so a second call, a duplicate outcome or a restart pays nothing more.
        /// </summary>
        public List<BossPrize> ClaimBossPrizes(BossOutcome outcome, DateTimeOffset now)
        {
            lock (_gate)
            {
                FlushBossHits();
                if (!_events.TryGetValue(outcome.EventId, out var live) || live.Def.PaidBosses.Contains(outcome.Index))
                    return new List<BossPrize>();
                var def = live.Def;
                def.PaidBosses.Add(outcome.Index);
                def.BossProgress.RemoveAll(p => p.Index == outcome.Index);
                RefreshBosses(now);
                var copy = def.Clone();
                Write(ct => _store.SaveEventAsync(copy, ct));
                // A Gulper pays nothing by itself: its damage became points, and the tier mail goes out at the end.
                if (def.Template == EventTemplates.LureTrap && outcome.Index >= LureIndexBase)
                {
                    FinishLureTrap(live, outcome.Index);
                    Flush();
                    return new List<BossPrize>();
                }

                var prizes = EventBossRewards.Plan(outcome, def.BossRanks, def.BossParticipation);
                _logger.LogInformation("Event {Id} boss {Index} ({Kind}) {How}: {Count} players rewarded.", def.Id, outcome.Index, outcome.Kind,
                    outcome.Killed ? "killed" : "left", prizes.Count);
                return prizes;
            }
        }

        public static bool Handles(CommandType type) => type is CommandType.ClaimEventStage or CommandType.ClaimEventRank or CommandType.ViewEventRanking
            or CommandType.ViewEventCalendar or CommandType.MobilizationTask or CommandType.TrenchConquest or CommandType.LureTrap or CommandType.AbyssSiege;

        public async Task InitializeAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            await _store.InitializeAsync(cancellationToken);
            var definitions = await _store.LoadEventsAsync(cancellationToken);
            // Progress is kept in memory for events that can still change or pay out.
            var open = definitions.Where(d => d.ClaimUntil > now.AddDays(-1)).Select(d => d.Id).ToList();
            var (scores, alliances) = await _store.LoadProgressAsync(open, cancellationToken);
            var states = await _store.LoadAllianceStatesAsync(open, cancellationToken);

            lock (_gate)
            {
                foreach (var definition in definitions)
                    _events[definition.Id] = new Live { Def = definition, Shape = Shape(definition), Phase = PhaseOf(definition, now) };
                foreach (var score in scores)
                {
                    if (_events.TryGetValue(score.EventId, out var live))
                        live.Scores[score.PlayerId] = score;
                }

                foreach (var (eventId, allianceId, points) in alliances)
                {
                    if (_events.TryGetValue(eventId, out var live))
                        live.Alliances[allianceId] = points;
                }

                foreach (var (eventId, allianceId, json) in states)
                {
                    if (_events.TryGetValue(eventId, out var live))
                        live.States[allianceId] = EventAllianceState.FromJson(json);
                }

                RefreshBosses(now);
            }

            _logger.LogInformation("Events: {Events} defined, {Scores} scores loaded.", definitions.Count, scores.Count);
            await InitializeCalendarAsync(now, cancellationToken);
        }

        // --- Sessions -----------------------------------------------------------------------------

        public void OnConnected(PlayerSession session, DateTimeOffset now)
        {
            lock (_gate)
            {
                _online[session.PublicId] = session;
                CountLogins(session, now);
                PushTo(session, now);
            }
        }

        public void OnDisconnected(PlayerSession session)
        {
            lock (_gate)
            {
                if (_online.TryGetValue(session.PublicId, out var current) && ReferenceEquals(current, session))
                    _online.Remove(session.PublicId);
            }
        }

        /// <summary>Turns a session's queued actions into points in every live event it takes part in.</summary>
        public void Record(PlayerSession session, List<(string Action, long Amount)> actions, DateTimeOffset now)
        {
            if (actions.Count == 0)
                return;
            var allianceId = AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
            var nowMs = now.ToUnixTimeMilliseconds();
            lock (_gate)
            {
                var changed = false;
                foreach (var live in _events.Values)
                {
                    if (live.Def.Template == EventTemplates.Login || PhaseOf(live.Def, now) != EventPhase.Live || !Sees(live.Def, session))
                        continue;
                    // Mobilization scores tasks, the duel scores days for paired alliances (their own files).
                    if (live.Def.Template == EventTemplates.Mobilization)
                    {
                        changed |= RecordMobilization(live, session.PublicId, allianceId, actions, now);
                        continue;
                    }

                    if (live.Def.Template == EventTemplates.AllianceDuel)
                    {
                        changed |= RecordDuel(live, session.PublicId, allianceId, actions, now);
                        continue;
                    }

                    // The Lure Trap scores Gulper damage (RecordLureHit); monster kills only charge the bait.
                    if (live.Def.Template == EventTemplates.LureTrap)
                    {
                        changed |= RecordLureBait(live, allianceId, actions, now);
                        continue;
                    }

                    var active = EventRules.ActiveActions(live.Shape, nowMs);
                    if (active.Length == 0)
                        continue;

                    var score = ScoreOf(live, session.PublicId, create: false);
                    long gained = 0;
                    foreach (var (action, amount) in actions)
                    {
                        if (Array.Find(active, a => a.Action == action) == null)
                            continue;
                        score ??= ScoreOf(live, session.PublicId, create: true)!;
                        var carry = score.Carry.TryGetValue(action, out var c) ? c : 0;
                        gained += EventRules.Score(active, action, amount, ref carry);
                        score.Carry[action] = carry;
                        _dirtyScores.Add(score);
                    }

                    if (score == null || gained <= 0)
                        continue;
                    AddPoints(live, score, allianceId, gained, now);
                    changed = true;
                }

                if (changed)
                    PushTo(session, now);
            }
        }

        /// <summary>
        /// Adds points to a player and, in alliance templates, to the alliance they are in when they earn them. Caller
        /// holds the lock.
        /// </summary>
        private void AddPoints(Live live, EventScore score, string allianceId, long gained, DateTimeOffset now)
        {
            score.Points += gained;
            score.UpdatedAt = now;
            _dirtyScores.Add(score);
            if (EventTemplates.ForAlliances(live.Def.Template))
            {
                score.AllianceId = allianceId;
                if (allianceId.Length > 0)
                {
                    live.Alliances[allianceId] = (live.Alliances.TryGetValue(allianceId, out var total) ? total : 0) + gained;
                    _dirtyAlliances.Add((live.Def.Id, allianceId));
                }
            }

            live.Dirty = true;
        }

        /// <summary>The alliance's state in an event, made on first use. Caller holds the lock.</summary>
        private EventAllianceState StateOf(Live live, string allianceId)
        {
            if (!live.States.TryGetValue(allianceId, out var state))
                live.States[allianceId] = state = new EventAllianceState();
            return state;
        }

        private void StateChanged(Live live, string allianceId) => _dirtyStates.Add((live.Def.Id, allianceId));

        /// <summary>Every tick: phase changes, daily sign-ins and batched writes, a few times a minute.</summary>
        public void Tick(DateTimeOffset now)
        {
            if (now < _nextHousekeeping)
                return;
            _nextHousekeeping = now + HousekeepingEvery;
            lock (_gate)
            {
                var phaseChanged = false;
                foreach (var live in _events.Values)
                {
                    var phase = PhaseOf(live.Def, now);
                    if (phase == live.Phase)
                        continue;
                    live.Phase = phase;
                    live.Dirty = true;
                    phaseChanged = true;
                    _logger.LogInformation("Event {Id} is now {Phase}.", live.Def.Id, phase);
                }

                // The weekly calendar creates the occurrences that are due (EventService.Calendar).
                if (now >= _nextCalendarRun)
                {
                    _nextCalendarRun = now + CalendarEvery;
                    phaseChanged |= RunCalendar(now) > 0;
                }

                // Duels that went live are paired (EventService.Duel); leagues draw their groups and fight due rounds (EventService.Trench).
                phaseChanged |= PairDuels(now) > 0;
                phaseChanged |= RunTrench(now);

                FlushBossHits();
                RefreshBosses(now);

                // Rankings moved since the last round: re-sort and show the new ranks to those taking part.
                var moved = _events.Values.Where(e => e.Dirty && e.Sorted != null).ToList();
                foreach (var live in moved)
                    Sort(live, now, force: true);

                foreach (var session in _online.Values)
                {
                    if (CountLogins(session, now) || phaseChanged || moved.Any(e => e.Scores.ContainsKey(session.PublicId)))
                        PushTo(session, now);
                }

                Flush();
            }
        }

        /// <summary>Counts today's sign-in in every live login event; true when one was counted.</summary>
        private bool CountLogins(PlayerSession session, DateTimeOffset now)
        {
            var day = now.ToUnixTimeSeconds() / 86_400;
            var counted = false;
            foreach (var live in _events.Values)
            {
                if (live.Def.Template != EventTemplates.Login || PhaseOf(live.Def, now) != EventPhase.Live || !Sees(live.Def, session))
                    continue;
                var score = ScoreOf(live, session.PublicId, create: true)!;
                if (score.LastLoginDay == day || score.LoginDays >= live.Def.Stages.Count)
                    continue;
                score.LastLoginDay = day;
                score.LoginDays++;
                score.UpdatedAt = now;
                _dirtyScores.Add(score);
                live.Dirty = true;
                counted = true;
            }

            return counted;
        }

        // --- Commands -----------------------------------------------------------------------------

        public void Handle(PlayerSession session, CommandEnvelope envelope, DateTimeOffset now)
        {
            switch (envelope.Type)
            {
                case CommandType.ClaimEventStage when TryDeserialize(envelope.Payload, out ClaimEventStageCommand? stage):
                    ClaimStage(session, stage!, now);
                    break;
                case CommandType.ClaimEventRank when TryDeserialize(envelope.Payload, out ClaimEventRankCommand? rank):
                    ClaimRank(session, rank!, now);
                    break;
                case CommandType.ViewEventRanking when TryDeserialize(envelope.Payload, out ViewEventRankingCommand? view):
                    var ranking = Ranking(view!.EventId, 50, session.PublicId);
                    session.Acknowledge(view.CommandId, view.ClientSequence, ranking == null ? "UnknownEvent" : null, now, ranking == null ? null : new List<EntityChangeDto>
                    {
                        new() { EntityId = EntityIds.EventRanking + view.EventId, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(ranking) }
                    });
                    break;
                case CommandType.ViewEventCalendar when TryDeserialize(envelope.Payload, out ViewEventCalendarCommand? calendar):
                    EventCalendarDto dto;
                    lock (_gate)
                        dto = CalendarFor(session, calendar!.Lang, now);
                    session.Acknowledge(calendar.CommandId, calendar.ClientSequence, null, now, new List<EntityChangeDto>
                    {
                        new() { EntityId = EntityIds.EventCalendar, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(dto) }
                    });
                    break;
                case CommandType.MobilizationTask when TryDeserialize(envelope.Payload, out MobilizationTaskCommand? task):
                    HandleMobilization(session, task!, now);
                    break;
                case CommandType.TrenchConquest when TryDeserialize(envelope.Payload, out TrenchCommand? trench):
                    HandleTrench(session, trench!, now);
                    break;
                case CommandType.LureTrap when TryDeserialize(envelope.Payload, out LureTrapCommand? lure):
                    HandleLure(session, lure!, now);
                    break;
                case CommandType.AbyssSiege when TryDeserialize(envelope.Payload, out AbyssSiegeCommand? siege):
                    HandleSiege(session, siege!, now);
                    break;
                default:
                    session.Acknowledge(envelope.CommandId, envelope.ClientSequence, "MalformedCommand", now);
                    break;
            }
        }

        private void ClaimStage(PlayerSession session, ClaimEventStageCommand command, DateTimeOffset now)
        {
            List<RewardData>? rewards = null;
            string? reject;
            lock (_gate)
            {
                reject = TryClaimStage(session, command.EventId, command.Index, command.Day, now, out rewards);
                if (reject == null)
                    PushTo(session, now);
            }

            if (reject != null)
                session.Acknowledge(command.CommandId, command.ClientSequence, reject, now);
            else
                session.GrantEventRewards(command.CommandId, command.ClientSequence, rewards!, now);
        }

        private string? TryClaimStage(PlayerSession session, string eventId, int index, int day, DateTimeOffset now, out List<RewardData>? rewards)
        {
            rewards = null;
            if (!_events.TryGetValue(eventId ?? string.Empty, out var live) || !Sees(live.Def, session))
                return "UnknownEvent";
            var phase = PhaseOf(live.Def, now);
            if (phase is EventPhase.Upcoming or EventPhase.Closed)
                return "NotOpen";
            // After the end, whatever was still waiting goes out by mail (EventService.Payout).
            if (phase == EventPhase.Ended || live.Def.PaidOutAt != null || live.Paying)
                return "SentByMail";
            if (index < 0 || index >= live.Def.Stages.Count)
                return "UnknownStage";
            var score = ScoreOf(live, session.PublicId, create: false);
            if (score == null)
                return "NotReached";

            // Duel chests count the points of one day: today's, or an earlier day of the week not yet taken.
            var key = index;
            bool reached;
            if (live.Def.Template == EventTemplates.AllianceDuel)
            {
                var today = AllianceDuelRules.DayAt(live.Shape, now.ToUnixTimeMilliseconds());
                var d = day > 0 ? day - 1 : today;
                if (d < 0 || d >= AllianceDuelRules.Days || d > today)
                    return "UnknownStage";
                key = AllianceDuelRules.ChestKey(d, index);
                reached = score.DayPoints[d] >= live.Def.Stages[index].Points;
            }
            else
            {
                reached = live.Def.Template == EventTemplates.Login ? index < score.LoginDays : score.Points >= live.Def.Stages[index].Points;
            }

            if (score.Claimed.Contains(key))
                return "AlreadyClaimed";
            if (!reached)
                return "NotReached";

            score.Claimed.Add(key);
            score.UpdatedAt = now;
            _dirtyScores.Add(score);
            Flush();
            rewards = Rewards(live.Def.Stages[index]);
            return null;
        }

        private void ClaimRank(PlayerSession session, ClaimEventRankCommand command, DateTimeOffset now)
        {
            List<RewardData>? rewards = null;
            string? reject;
            lock (_gate)
            {
                reject = TryClaimRank(session, command.EventId, now, out rewards);
                if (reject == null)
                    PushTo(session, now);
            }

            if (reject != null)
                session.Acknowledge(command.CommandId, command.ClientSequence, reject, now);
            else
            {
                session.GrantEventRewards(command.CommandId, command.ClientSequence, rewards!, now);
                // An event rank drops an alliance gift for the player's mates.
                DropGift?.Invoke(session.PublicId, AllianceGiftRules.Event, 0, now);
            }
        }

        /// <summary>
        /// Rank rewards are no longer claimed on the page: the payout mails them when the event ends (docs/29 §2.2). The
        /// command stays for older clients and only explains why nothing happens.
        /// </summary>
        private string? TryClaimRank(PlayerSession session, string eventId, DateTimeOffset now, out List<RewardData>? rewards)
        {
            rewards = null;
            if (!_events.TryGetValue(eventId ?? string.Empty, out var live) || !Sees(live.Def, session) || live.Def.Template == EventTemplates.Login)
                return "UnknownEvent";
            if (PhaseOf(live.Def, now) is EventPhase.Upcoming or EventPhase.Live && live.Def.PaidOutAt == null)
                return "NotEnded";
            var score = ScoreOf(live, session.PublicId, create: false);
            if (score is { RankClaimed: true })
                return "AlreadyClaimed";
            if (score == null || score.Points <= 0 || score.Points < live.Def.MinRankPoints)
                return "NoRank";
            Sort(live, now, force: false);
            return BandFor(live.Def, RankOf(live, score)) == null ? "NoRank" : "SentByMail";
        }

        private static List<RewardData> Rewards(EventTierConfig tier) =>
            tier.Rewards.Select(r => new RewardData { type = r.Type, id = r.Id, amount = r.Amount, rarity = r.Rarity ?? string.Empty }).ToList();

        // --- Player views -------------------------------------------------------------------------

        private void PushTo(PlayerSession session, DateTimeOffset now)
        {
            var list = ListFor(session, now);
            session.Push(new List<EntityChangeDto>
            {
                new() { EntityId = EntityIds.Events, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(list) }
            }, now);
        }

        /// <summary>What one player sees: published events in their range, upcoming, live, or ended and still paying out.</summary>
        internal EventListDto ListFor(PlayerSession session, DateTimeOffset now)
        {
            var nowMs = now.ToUnixTimeMilliseconds();
            var allianceId = string.Empty;
            var events = new List<EventDto>();
            // Texts in the player's language only once it is known (every language, as before, until then).
            var lang = session.Language;
            foreach (var live in _events.Values.OrderBy(e => e.Def.StartsAt))
            {
                var phase = PhaseOf(live.Def, now);
                if (phase == EventPhase.Closed || !Sees(live.Def, session))
                    continue;
                // Upcoming events show from a week before they start.
                if (phase == EventPhase.Upcoming && live.Def.StartsAt - now > TimeSpan.FromDays(7))
                    continue;
                Sort(live, now, force: false);
                var dto = lang.Length > 0 ? InLanguage(live.Shape, lang) : Copy(live.Shape);
                var score = ScoreOf(live, session.PublicId, create: false);
                dto.Participants = live.Scores.Count;
                if (score != null)
                {
                    dto.MyPoints = score.Points;
                    dto.MyRank = RankOf(live, score);
                    dto.Claimed = score.Claimed.OrderBy(i => i).ToArray();
                    dto.RankClaimed = score.RankClaimed;
                    dto.LoginDays = score.LoginDays;
                    allianceId = score.AllianceId;
                }

                if (EventTemplates.ForAlliances(live.Def.Template))
                {
                    // Before they score, show the alliance they are in now.
                    var alliance = score != null && score.AllianceId.Length > 0 ? score.AllianceId : AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
                    if (alliance.Length > 0)
                    {
                        dto.AlliancePoints = live.Alliances.TryGetValue(alliance, out var total) ? total : 0;
                        dto.AllianceRank = live.AllianceRanks != null && live.AllianceRanks.TryGetValue(alliance, out var r) ? r : 0;
                    }
                }

                dto.PaidOut = live.Def.PaidOutAt != null;
                if (live.Def.Template == EventTemplates.Mobilization)
                    dto.Mobilization = MobilizationView(live, session.PublicId, score, now);
                else if (live.Def.Template == EventTemplates.AllianceDuel)
                    dto.Duel = DuelView(live, session.PublicId, score, now);
                else if (live.Def.Template == EventTemplates.TrenchConquest)
                    dto.Trench = TrenchView(live, session.PublicId, now);
                else if (live.Def.Template == EventTemplates.LureTrap)
                    dto.LureTrap = LureView(live, session.PublicId, now);
                else if (live.Def.Template == EventTemplates.AbyssSiege)
                    dto.AbyssSiege = SiegeView(live, session.PublicId, now);

                events.Add(dto);
            }

            return new EventListDto { Events = events.ToArray(), ServerNowMs = nowMs };
        }

        /// <summary>The top <paramref name="top"/> and the asker's own line; null for an unknown event.</summary>
        public EventRankingDto? Ranking(string eventId, int top, string? playerId = null)
        {
            var now = DateTimeOffset.UtcNow;
            lock (_gate)
            {
                if (!_events.TryGetValue(eventId ?? string.Empty, out var live))
                    return null;
                Sort(live, now, force: false);
                var result = new EventRankingDto { EventId = live.Def.Id, Alliance = EventTemplates.RanksAlliances(live.Def.Template) };
                // Players see their own kingdom's ranking (docs/39); the Overseer (no player) sees every kingdom.
                var kingdom = playerId == null ? null : Kingdom(playerId);
                if (result.Alliance)
                {
                    result.Entries = live.SortedAlliances!.Where(p => kingdom == null || live.AllianceKingdoms!.GetValueOrDefault(p.Key) == kingdom).Take(top)
                        .Select(p => AllianceEntry(live.AllianceRanks![p.Key], p.Key, p.Value)).ToArray();
                    var mine = playerId == null ? null : ScoreOf(live, playerId, create: false);
                    var allianceId = mine?.AllianceId.Length > 0 ? mine.AllianceId : playerId == null ? string.Empty : AllianceIdOf?.Invoke(playerId) ?? string.Empty;
                    if (allianceId.Length > 0 && live.AllianceRanks!.TryGetValue(allianceId, out var rank))
                        result.Mine = AllianceEntry(rank, allianceId, live.Alliances[allianceId]);
                }
                else
                {
                    result.Entries = live.Sorted!.Where(s => kingdom == null || Kingdom(s.PlayerId) == kingdom).Take(top)
                        .Select(s => PlayerEntry(live.PlayerRanks![s.PlayerId], s)).ToArray();
                    var mine = playerId == null ? null : ScoreOf(live, playerId, create: false);
                    if (mine != null && mine.Points > 0)
                        result.Mine = PlayerEntry(RankOf(live, mine), mine);
                }

                return result;
            }
        }

        private EventRankEntryDto PlayerEntry(int rank, EventScore s) => new()
        {
            Rank = rank, Id = s.PlayerId, Name = NameLookup?.Invoke(s.PlayerId) ?? "?", Tag = TagOf?.Invoke(s.PlayerId) ?? string.Empty,
            Avatar = AvatarLookup?.Invoke(s.PlayerId) ?? string.Empty, Points = s.Points
        };

        private EventRankEntryDto AllianceEntry(int rank, string allianceId, long points)
        {
            var info = AllianceInfo?.Invoke(allianceId);
            return new EventRankEntryDto
            {
                Rank = rank, Id = allianceId, Name = info?.Name ?? "?", Tag = info?.Tag ?? string.Empty, Avatar = info?.Emblem ?? string.Empty, Points = points
            };
        }

        // --- Rankings -----------------------------------------------------------------------------

        private void Sort(Live live, DateTimeOffset now, bool force)
        {
            if (!live.Dirty && live.Sorted != null)
                return;
            if (!force && live.Sorted != null && now - live.SortedAt < ResortEvery)
                return;
            // Ties go to whoever got there first.
            live.Sorted = live.Scores.Values.Where(s => s.Points > 0).OrderByDescending(s => s.Points).ThenBy(s => s.UpdatedAt).ThenBy(s => s.PlayerId, StringComparer.Ordinal).ToList();
            // Ranks count within each kingdom (docs/39): every kingdom has its own first place.
            var places = new Dictionary<string, int>(StringComparer.Ordinal);
            live.PlayerRanks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in live.Sorted)
            {
                var kingdom = Kingdom(s.PlayerId);
                live.PlayerRanks[s.PlayerId] = places[kingdom] = places.GetValueOrDefault(kingdom) + 1;
            }

            live.AllianceKingdoms = live.Scores.Values.Where(s => s.AllianceId.Length > 0).GroupBy(s => s.AllianceId)
                .ToDictionary(g => g.Key, g => Kingdom(g.First().PlayerId), StringComparer.Ordinal);
            live.SortedAlliances = live.Alliances.Where(p => p.Value > 0).OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).ToList();
            live.AllianceRanks = new Dictionary<string, int>(StringComparer.Ordinal);
            places.Clear();
            foreach (var pair in live.SortedAlliances)
            {
                var kingdom = live.AllianceKingdoms.GetValueOrDefault(pair.Key) ?? HADAL.Shared.Simulation.AllianceLandRules.DefaultWorld;
                live.AllianceRanks[pair.Key] = places[kingdom] = places.GetValueOrDefault(kingdom) + 1;
            }
            live.Dirty = false;
            live.SortedAt = now;
        }

        /// <summary>The player's rank, or their alliance's in an alliance contest (0 = unranked).</summary>
        private static int RankOf(Live live, EventScore score)
        {
            if (EventTemplates.RanksAlliances(live.Def.Template))
                return score.AllianceId.Length > 0 && live.AllianceRanks != null && live.AllianceRanks.TryGetValue(score.AllianceId, out var a) ? a : 0;
            return live.PlayerRanks != null && live.PlayerRanks.TryGetValue(score.PlayerId, out var r) ? r : 0;
        }

        private static EventTierConfig? BandFor(EventDefinition def, int rank) =>
            rank <= 0 ? null : def.RankBands.FirstOrDefault(b => rank >= b.From && rank <= b.To);

        // --- Helpers ------------------------------------------------------------------------------

        private EventScore? ScoreOf(Live live, string playerId, bool create)
        {
            if (live.Scores.TryGetValue(playerId, out var score))
                return score;
            if (!create)
                return null;
            score = new EventScore { EventId = live.Def.Id, PlayerId = playerId };
            live.Scores[playerId] = score;
            return score;
        }

        /// <summary>Published, in the player's level range and, for a test run, on its list.</summary>
        private static bool Sees(EventDefinition def, PlayerSession session)
        {
            if (def.Status != EventStatus.Published)
                return false;
            if (def.TestAccounts.Count > 0 && !def.TestAccounts.Contains(session.IdentityKey, StringComparer.OrdinalIgnoreCase))
                return false;
            if (def.Kingdoms.Count > 0 && !def.Kingdoms.Contains(session.KingdomId, StringComparer.Ordinal))
                return false;
            if (def.MinLevel > 0 || def.MaxLevel > 0)
            {
                var level = session.BaseLevel;
                if (def.MinLevel > 0 && level < def.MinLevel)
                    return false;
                if (def.MaxLevel > 0 && level > def.MaxLevel)
                    return false;
            }

            return true;
        }

        private static EventPhase PhaseOf(EventDefinition def, DateTimeOffset now) =>
            now < def.StartsAt ? EventPhase.Upcoming : now < def.EndsAt ? EventPhase.Live : now < def.ClaimUntil ? EventPhase.Ended : EventPhase.Closed;

        /// <summary>The player-independent part of the event DTO, built once per definition change.</summary>
        internal static EventDto Shape(EventDefinition def)
        {
            static LocalTextDto[] Texts(Dictionary<string, string> texts) =>
                texts.Where(p => !string.IsNullOrWhiteSpace(p.Value)).Select(p => new LocalTextDto { Lang = p.Key, Text = p.Value }).ToArray();
            static EventActionDto[] Actions(List<EventActionConfig> actions) =>
                actions.Select(a => new EventActionDto { Action = a.Action, Points = a.Points, Per = Math.Max(1, a.Per) }).ToArray();
            static EventTierDto Tier(EventTierConfig t) => new()
            {
                Points = t.Points, From = t.From, To = t.To,
                Rewards = t.Rewards.Select(r => new RewardEntryDto { Type = r.Type, Id = r.Type == "shards" && string.IsNullOrEmpty(r.Id) ? "rarity:" + r.Rarity : r.Id, Amount = r.Amount }).ToArray()
            };

            var start = def.StartsAt.ToUnixTimeMilliseconds();
            var end = def.EndsAt.ToUnixTimeMilliseconds();
            var phases = new List<EventPhaseDto>();
            if (def.Phases.Count > 0)
            {
                var cycle = def.CyclePhases ? def.Phases.Max(p => p.StartHour + p.Hours) : 0;
                for (var round = 0; round < 1000; round++)
                {
                    var offset = round * cycle * 3_600_000d;
                    var added = false;
                    foreach (var p in def.Phases.OrderBy(p => p.StartHour))
                    {
                        var from = start + (long)(offset + p.StartHour * 3_600_000d);
                        if (from >= end)
                            continue;
                        phases.Add(new EventPhaseDto
                        {
                            Title = Texts(p.Title), StartMs = from, EndMs = Math.Min(end, from + (long)(p.Hours * 3_600_000d)), Actions = Actions(p.Actions)
                        });
                        added = true;
                    }

                    if (!def.CyclePhases || cycle <= 0 || !added)
                        break;
                }
            }

            return new EventDto
            {
                Id = def.Id, Template = def.Template, Title = Texts(def.Title), Description = Texts(def.Description), Banner = def.Banner,
                StartsAtMs = start, EndsAtMs = end, ClaimUntilMs = def.ClaimUntil.ToUnixTimeMilliseconds(),
                Actions = Actions(def.Actions), Phases = phases.ToArray(), Stages = def.Stages.Select(Tier).ToArray(),
                RankBands = def.RankBands.Select(Tier).ToArray(), MinRankPoints = def.MinRankPoints, ScheduleId = def.ScheduleId ?? string.Empty
            };
        }

        private static EventDto Copy(EventDto shape) => new()
        {
            Id = shape.Id, Template = shape.Template, Title = shape.Title, Description = shape.Description, Banner = shape.Banner,
            StartsAtMs = shape.StartsAtMs, EndsAtMs = shape.EndsAtMs, ClaimUntilMs = shape.ClaimUntilMs, Actions = shape.Actions,
            Phases = shape.Phases, Stages = shape.Stages, RankBands = shape.RankBands, MinRankPoints = shape.MinRankPoints, ScheduleId = shape.ScheduleId
        };

        // --- Overseer -----------------------------------------------------------------------------

        public sealed record EventSummary(EventDefinition Definition, string Phase, int Participants, long TopPoints);

        public List<EventSummary> List(DateTimeOffset now)
        {
            lock (_gate)
                return _events.Values.OrderByDescending(e => e.Def.StartsAt).Select(e => Summary(e, now)).ToList();
        }

        public EventSummary? Get(string id, DateTimeOffset now)
        {
            lock (_gate)
                return _events.TryGetValue(id, out var live) ? Summary(live, now) : null;
        }

        private static EventSummary Summary(Live live, DateTimeOffset now) => new(
            live.Def.Clone(),
            live.Def.Status != EventStatus.Published ? live.Def.Status : PhaseOf(live.Def, now).ToString().ToLowerInvariant(),
            live.Scores.Count,
            EventTemplates.RanksAlliances(live.Def.Template) ? live.Alliances.Values.DefaultIfEmpty(0).Max() : live.Scores.Values.Select(s => s.Points).DefaultIfEmpty(0).Max());

        /// <summary>Creates or replaces an event. Returns an error code, or the saved definition.</summary>
        public (string Error, EventDefinition? Saved) Save(EventDefinition input, string by, DateTimeOffset now)
        {
            var error = Validate(input, Catalog);
            if (error.Length > 0)
                return (error, null);

            lock (_gate)
            {
                var def = input.Clone();
                Normalise(def);
                if (string.IsNullOrWhiteSpace(def.Id) || !_events.TryGetValue(def.Id, out var live))
                {
                    def.Id = "ev" + Guid.NewGuid().ToString("N")[..12];
                    def.Status = EventStatus.Draft;
                    def.CreatedBy = by;
                    def.CreatedAt = now;
                    ResetRuntime(def);
                    live = new Live();
                    _events[def.Id] = live;
                }
                else
                {
                    // A running event keeps its kind, start and status; scores already earned stay.
                    if (live.Def.Status == EventStatus.Published && PhaseOf(live.Def, now) != EventPhase.Upcoming)
                    {
                        def.Template = live.Def.Template;
                        def.StartsAt = live.Def.StartsAt;
                    }

                    def.Status = live.Def.Status;
                    def.CreatedBy = live.Def.CreatedBy;
                    def.CreatedAt = live.Def.CreatedAt;
                    def.PaidBosses = live.Def.PaidBosses.ToList(); // the Overseer cannot pay a boss twice
                    def.BossProgress = live.Def.BossProgress; // nor reset a boss's body or its damage
                    def.DuelState = live.Def.DuelState; // nor re-pair a duel
                    def.PaidOutAt = live.Def.PaidOutAt; // nor pay an event out twice
                    def.PaidOutMails = live.Def.PaidOutMails;
                }

                def.UpdatedAt = now;
                Replace(live, def, now);
                return (string.Empty, def.Clone());
            }
        }

        public string Publish(string id, DateTimeOffset now) => Change(id, now, def =>
        {
            if (def.Status == EventStatus.Published)
                return "AlreadyPublished";
            if (def.EndsAt <= now)
                return "AlreadyOver";
            def.Status = EventStatus.Published;
            return string.Empty;
        });

        public string Cancel(string id, DateTimeOffset now) => Change(id, now, def =>
        {
            def.Status = EventStatus.Cancelled;
            return string.Empty;
        });

        /// <summary>Ends a running event now; rank rewards open at once.</summary>
        public string EndNow(string id, DateTimeOffset now) => Change(id, now, def =>
        {
            if (PhaseOf(def, now) != EventPhase.Live)
                return "NotLive";
            def.EndsAt = now;
            return string.Empty;
        });

        public string Extend(string id, double hours, DateTimeOffset now) => Change(id, now, def =>
        {
            if (hours is <= 0 or > 24 * 30)
                return "InvalidHours";
            if (PhaseOf(def, now) is EventPhase.Closed)
                return "AlreadyOver";
            // Once the payout went out (or is on its way) the result stands.
            if (def.PaidOutAt != null || (_events.TryGetValue(def.Id, out var running) && running.Paying))
                return "AlreadyPaid";
            def.EndsAt = def.EndsAt.AddHours(hours);
            return string.Empty;
        });

        public (string Error, EventDefinition? Copy) Duplicate(string id, string by, DateTimeOffset now)
        {
            EventDefinition source;
            lock (_gate)
            {
                if (!_events.TryGetValue(id, out var live))
                    return ("UnknownEvent", null);
                source = live.Def.Clone();
            }

            // An event ended early keeps at least a day's length in its copy.
            var length = source.EndsAt - source.StartsAt;
            if (length < TimeSpan.FromHours(1))
                length = TimeSpan.FromDays(1);
            source.Id = string.Empty;
            source.StartsAt = now.AddDays(1);
            source.EndsAt = source.StartsAt + length;
            foreach (var key in source.Title.Keys.ToList())
                source.Title[key] = source.Title[key] + " (2)";
            return Save(source, by, now);
        }

        /// <summary>Removes a draft or cancelled event with its scores.</summary>
        public string Delete(string id)
        {
            lock (_gate)
            {
                if (!_events.TryGetValue(id, out var live))
                    return "UnknownEvent";
                if (live.Def.Status == EventStatus.Published)
                    return "CancelFirst";
                _events.Remove(id);
                _dirtyScores.RemoveWhere(s => s.EventId == id);
                Write(ct => _store.DeleteEventAsync(id, ct));
                return string.Empty;
            }
        }

        private string Change(string id, DateTimeOffset now, Func<EventDefinition, string> change)
        {
            lock (_gate)
            {
                if (!_events.TryGetValue(id ?? string.Empty, out var live))
                    return "UnknownEvent";
                var def = live.Def.Clone();
                var error = change(def);
                if (error.Length > 0)
                    return error;
                def.UpdatedAt = now;
                Replace(live, def, now);
                return string.Empty;
            }
        }

        /// <summary>Installs a new definition, writes it and shows the change to everyone online. Caller holds the lock.</summary>
        private void Replace(Live live, EventDefinition def, DateTimeOffset now)
        {
            live.Def = def;
            live.Shape = Shape(def);
            live.Phase = PhaseOf(def, now);
            live.Dirty = true;
            RefreshBosses(now);
            var copy = def.Clone();
            Write(ct => _store.SaveEventAsync(copy, ct));
            foreach (var session in _online.Values)
                PushTo(session, now);
        }

        private static void Normalise(EventDefinition def)
        {
            def.Title = def.Title.Where(p => !string.IsNullOrWhiteSpace(p.Value)).ToDictionary(p => p.Key, p => p.Value.Trim());
            def.Description = def.Description.Where(p => !string.IsNullOrWhiteSpace(p.Value)).ToDictionary(p => p.Key, p => p.Value.Trim());
            def.TestAccounts = def.TestAccounts.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            def.Stages = def.Template == EventTemplates.Login ? def.Stages : def.Stages.OrderBy(s => s.Points).ToList();
            def.RankBands = def.RankBands.OrderBy(b => b.From).ToList();
            def.Bosses ??= new List<EventBossConfig>();
            def.BossRanks = (def.BossRanks ?? new List<EventTierConfig>()).OrderBy(b => b.From).ToList();
            def.BossParticipation ??= new List<EventRewardConfig>();
            def.PaidBosses ??= new List<int>();
            def.BossProgress ??= new List<EventBossProgress>();
            def.Mobilization ??= new MobilizationConfig();
            def.Mobilization.Kinds = (def.Mobilization.Kinds ?? new List<string>()).Distinct(StringComparer.Ordinal).ToList();
            def.Duel ??= new DuelConfig();
            def.Duel.Win ??= new List<EventRewardConfig>();
            def.Duel.Loss ??= new List<EventRewardConfig>();
            def.Duel.Draw ??= new List<EventRewardConfig>();
            def.DuelState ??= new DuelState();
            def.AbyssSiege ??= new AbyssSiegeConfig();
            def.LureTrap ??= new LureTrapConfig();
            def.LureTrap.Tiers = (def.LureTrap.Tiers ?? new List<EventTierConfig>()).OrderBy(t => t.Points).ToList();
            foreach (var tier in def.LureTrap.Tiers)
                tier.Rewards ??= new List<EventRewardConfig>();
            foreach (var a in def.Actions.Concat(def.Phases.SelectMany(p => p.Actions)))
                a.Per = Math.Max(1, a.Per);
        }

        /// <summary>What the server writes into a definition while it runs: a new event or a copy starts without any.</summary>
        internal static void ResetRuntime(EventDefinition def)
        {
            def.PaidBosses = new List<int>();
            def.BossProgress = new List<EventBossProgress>();
            def.DuelState = new DuelState();
            def.PaidOutAt = null;
            def.PaidOutMails = 0;
        }

        /// <summary>Checks an event definition; returns an error code ("" when fine).</summary>
        public static string Validate(EventDefinition def, GameCatalog catalog)
        {
            if (!EventTemplates.IsKnown(def.Template))
                return "UnknownTemplate";
            if (!def.Title.Values.Any(t => !string.IsNullOrWhiteSpace(t)) || def.Title.Values.Any(t => t != null && t.Length > 80))
                return "InvalidTitle";
            if (def.Description.Values.Any(t => t != null && t.Length > 2000))
                return "InvalidDescription";
            if (def.EndsAt <= def.StartsAt || def.EndsAt - def.StartsAt > TimeSpan.FromDays(62))
                return "InvalidWindow";
            if (def.ClaimDays is < 0 or > 14 || def.MinLevel < 0 || def.MaxLevel < 0 || (def.MaxLevel > 0 && def.MaxLevel < def.MinLevel) || def.MinRankPoints < 0)
                return "InvalidRange";
            if (string.IsNullOrWhiteSpace(def.Banner) || def.Banner.Length > 32)
                return "InvalidBanner";
            def.Kingdoms = (def.Kingdoms ?? new List<string>()).Select(k => k.Trim()).Where(k => k.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            if (def.Kingdoms.Any(k => k.Length > 12) || (def.Bosses.Count > 0 && def.Kingdoms.Count > 1))
                return "InvalidKingdoms";

            var length = (def.EndsAt - def.StartsAt).TotalHours;
            if (def.Template == EventTemplates.Login)
            {
                if (def.Stages.Count is < 1 or > 31 || def.Stages.Any(s => s.Rewards.Count == 0))
                    return "InvalidDays";
                if (def.Stages.Count > Math.Ceiling(length / 24) + 0.0001)
                    return "TooManyDays";
            }
            else
            {
                var phases = def.Phases ?? new List<EventPhaseConfig>();
                // Mobilization scores its tasks and Trench Conquest its fights, not an actions table.
                if (def.Actions.Count == 0 && phases.Count == 0 && def.Template != EventTemplates.Mobilization && def.Template != EventTemplates.TrenchConquest
                    && def.Template != EventTemplates.LureTrap && def.Template != EventTemplates.AbyssSiege)
                    return "NoActions";
                if (def.Template == EventTemplates.TrenchConquest)
                {
                    var t = def.Trench ?? new TrenchConfig();
                    if (t.MinReactor is < 1 or > 60 || t.MinRegistered is < 1 or > 200 || t.FightsPerTeam is < 1 or > 5 || t.RegisterHours is < 1 or > 336
                        || t.FirstRoundAfterHours is < 0 or > 168 || t.RoundEveryHours is < 1 or > 168 || t.PointsPerWin is < 0 or > 100_000
                        || def.StartsAt.AddHours(t.RegisterHours + t.FirstRoundAfterHours) >= def.EndsAt)
                        return "InvalidTrench";
                }
                if (def.Template == EventTemplates.AbyssSiege)
                {
                    var a = def.AbyssSiege ?? new AbyssSiegeConfig();
                    if (a.MinMembers is < 1 or > 200 || a.MinReactor is < 1 or > 60 || a.HubHoldPoints is < 0 or > 10_000_000
                        || (def.EndsAt - def.StartsAt).TotalMilliseconds <= AbyssSiegeRules.LengthMs + AbyssSiegeRules.MinLeadMinutes * 60_000L)
                        return "InvalidAbyssSiege";
                }

                if (def.Template == EventTemplates.LureTrap)
                {
                    var l = def.LureTrap ?? new LureTrapConfig();
                    var tiers = l.Tiers ?? new List<EventTierConfig>();
                    if (l.MinMembers is < 1 or > 200 || l.FightMinutes is < 5 or > 240 || l.BaitPerKill is < 0 or > 1000 || l.TopHitterMultiplier is < 1 or > 10
                        || tiers.Count > 10 || tiers.Any(t => t.Points <= 0 || (t.Rewards?.Count ?? 0) > 10)
                        || tiers.Select(t => t.Points).Distinct().Count() != tiers.Count
                        || def.StartsAt.AddMinutes(LureTrapRules.MinLeadMinutes + l.FightMinutes) >= def.EndsAt)
                        return "InvalidLureTrap";
                }

                if (def.Template == EventTemplates.Mobilization)
                {
                    var m = def.Mobilization ?? new MobilizationConfig();
                    if (m.MinMembers is < 1 or > 200 || m.Slots is < 1 or > MobilizationRules.MaxSlots || m.RefillMinutes is < 0 or > 1440
                        || m.TasksPerDay is < 1 or > 100 || m.RefreshesPerDay is < 0 or > 50 || m.RefreshCooldownMinutes is < 0 or > 1440
                        || m.TargetPercent is < 10 or > 1000 || (m.Kinds ?? new List<string>()).Any(k => !MobilizationRules.IsKind(k)))
                        return "InvalidMobilization";
                }

                if (def.Template == EventTemplates.AllianceDuel)
                {
                    // Six themed days, back to back from the start.
                    var days = phases.OrderBy(p => p.StartHour).ToList();
                    if (days.Count != AllianceDuelRules.Days || def.CyclePhases || days[0].StartHour != 0
                        || days.Zip(days.Skip(1), (a, b) => Math.Abs(a.StartHour + a.Hours - b.StartHour) > 0.0001).Any(gap => gap))
                        return "InvalidDuelDays";
                    var duel = def.Duel ?? new DuelConfig();
                    if (duel.MinMembers is < 1 or > 200
                        || (duel.Win ?? new()).Concat(duel.Loss ?? new()).Concat(duel.Draw ?? new()).Any(r => !ValidReward(r, catalog))
                        || (duel.Win?.Count ?? 0) > 10 || (duel.Loss?.Count ?? 0) > 10 || (duel.Draw?.Count ?? 0) > 10)
                        return "InvalidDuelRewards";
                }
                foreach (var a in def.Actions.Concat(phases.SelectMany(p => p.Actions)))
                {
                    if (!EventActions.IsKnown(a.Action) || a.Points <= 0 || a.Per < 1)
                        return "InvalidAction";
                }

                foreach (var p in phases)
                {
                    if (p.Hours <= 0 || p.StartHour < 0 || p.StartHour >= length || p.Actions.Count == 0 || !p.Title.Values.Any(t => !string.IsNullOrWhiteSpace(t)))
                        return "InvalidPhase";
                }

                if (def.Stages.Count > 30 || def.Stages.Any(s => s.Points <= 0 || s.Rewards.Count == 0) || def.Stages.Select(s => s.Points).Distinct().Count() != def.Stages.Count)
                    return "InvalidStages";
                if (def.RankBands.Count > 20 || def.RankBands.Any(b => b.From < 1 || b.To < b.From || b.Rewards.Count == 0))
                    return "InvalidBands";
                var bands = def.RankBands.OrderBy(b => b.From).ToList();
                for (var i = 1; i < bands.Count; i++)
                {
                    if (bands[i].From <= bands[i - 1].To)
                        return "OverlappingBands";
                }
            }

            foreach (var r in def.Stages.Concat(def.RankBands).SelectMany(t => t.Rewards))
            {
                if (!ValidReward(r, catalog))
                    return "InvalidReward";
            }

            // Bosses: event-only kinds, within the kind's levels, on the map.
            var size = Math.Max(10, catalog.World.size);
            if ((def.Bosses?.Count ?? 0) > 5 || (def.Bosses ?? new List<EventBossConfig>()).Any(b =>
                    MonsterRules.Kind(b.Kind) is not { EventOnly: true } kind || b.Level < kind.MinLevel || b.Level > kind.MaxLevel
                    || b.X < 0 || b.Y < 0 || b.X >= size || b.Y >= size))
                return "InvalidBoss";

            // Boss prizes: rank bands inside an alliance and a participation gift, mailed (resources and items only).
            var ranks = def.BossRanks ?? new List<EventTierConfig>();
            if (ranks.Count > 10 || ranks.Any(b => b.From < 1 || b.To < b.From || b.Rewards.Count == 0)
                || ranks.OrderBy(b => b.From).Zip(ranks.OrderBy(b => b.From).Skip(1), (a, b) => b.From <= a.To).Any(x => x)
                || (def.BossParticipation?.Count ?? 0) > 5
                || ranks.SelectMany(b => b.Rewards).Concat(def.BossParticipation ?? new List<EventRewardConfig>())
                    .Any(r => !EventBossRewards.Mailable(r) || !ValidReward(r, catalog)))
                return "InvalidBossRewards";

            return string.Empty;
        }

        public static bool ValidReward(EventRewardConfig r, GameCatalog catalog)
        {
            if (r == null || r.Amount <= 0 || r.Amount > 1_000_000_000)
                return false;
            return r.Type switch
            {
                "resource" => GameCatalog.TryParseResource(r.Id, out _),
                "item" => catalog.TryGetItem(r.Id, out _) || Speedups.TryParse(r.Id, out _, out _, out _) || Capsules.TryParse(r.Id, out _, out _) || GiftItems.IsValid(catalog, r.Id) || VipRules.TryParseItem(r.Id, out _) || TeleportRules.IsItem(r.Id) || RenameRules.IsItem(r.Id),
                "shards" => string.IsNullOrEmpty(r.Id) ? catalog.Heroes.Any(h => h.Rarity.id == r.Rarity) : catalog.TryGetHero(r.Id, out _),
                _ => false
            };
        }

        // --- Writes -------------------------------------------------------------------------------

        /// <summary>Writes changed scores and alliance totals. Caller holds the lock.</summary>
        private void Flush()
        {
            if (_dirtyScores.Count > 0)
            {
                var scores = _dirtyScores.Where(s => _events.ContainsKey(s.EventId)).Select(s => s.Copy()).ToList();
                _dirtyScores.Clear();
                Write(ct => _store.SaveScoresAsync(scores, ct));
            }

            if (_dirtyStates.Count > 0)
            {
                var states = _dirtyStates.Where(k => _events.TryGetValue(k.EventId, out var e) && e.States.ContainsKey(k.AllianceId))
                    .Select(k => (k.EventId, k.AllianceId, _events[k.EventId].States[k.AllianceId].ToJson())).ToList();
                _dirtyStates.Clear();
                Write(ct => _store.SaveAllianceStatesAsync(states, ct));
            }

            if (_dirtyAlliances.Count > 0)
            {
                var totals = _dirtyAlliances.Where(k => _events.ContainsKey(k.EventId))
                    .Select(k => (k.EventId, k.AllianceId, _events[k.EventId].Alliances.TryGetValue(k.AllianceId, out var p) ? p : 0)).ToList();
                _dirtyAlliances.Clear();
                Write(ct => _store.SaveAlliancePointsAsync(totals, ct));
            }
        }

        /// <summary>Waits until every queued write is done (tests, shutdown).</summary>
        public async Task DrainAsync()
        {
            var done = new TaskCompletionSource();
            lock (_gate)
            {
                FlushBossHits();
                Flush();
            }
            Write(_ =>
            {
                done.SetResult();
                return Task.CompletedTask;
            });
            await done.Task;
        }

        private void Write(Func<CancellationToken, Task> write) => _writes.Writer.TryWrite(write);

        private async Task WriteLoopAsync()
        {
            await foreach (var write in _writes.Reader.ReadAllAsync())
            {
                try
                {
                    await write(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Event store write failed.");
                }
            }
        }

        private bool TryDeserialize<T>(byte[] payload, out T? value) where T : class
        {
            value = null;
            return payload is { Length: > 0 } && _serializer.TryDeserialize(payload, out value) && value != null;
        }
    }
}
