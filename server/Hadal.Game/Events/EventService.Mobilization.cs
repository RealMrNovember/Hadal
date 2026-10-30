using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Events
{
    /// <summary>
    /// Mobilization (docs/29 §4.5, MobilizationRules): each alliance's task pool, the members' held tasks and how playing
    /// finishes them. The pool lives in the event's alliance state (EventAllianceState), the held task in the player's
    /// score, so both survive restarts with the rest of the event.
    /// </summary>
    public sealed partial class EventService
    {
        /// <summary>How many members an alliance has (set by the world to SocialHub.MemberCount).</summary>
        public Func<string, int>? MemberCount { get; set; }

        private static long UtcDay(DateTimeOffset now) => now.ToUnixTimeSeconds() / 86_400;

        /// <summary>Starts a new UTC day's counters for a player. Caller holds the lock.</summary>
        private static void RollMobilizationDay(EventScore score, DateTimeOffset now)
        {
            var day = UtcDay(now);
            if (score.TaskDay == day)
                return;
            score.TaskDay = day;
            score.TakenToday = 0;
            score.RefreshesToday = 0;
        }

        /// <summary>
        /// The alliance's pool with every slot whose refill time has come dealt a new card (made on first use). Returns
        /// whether anything changed. Caller holds the lock.
        /// </summary>
        private EventAllianceState Pool(Live live, string allianceId, DateTimeOffset now)
        {
            var state = StateOf(live, allianceId);
            var config = live.Def.Mobilization;
            var slots = Math.Clamp(config.Slots, 1, MobilizationRules.MaxSlots);
            var changed = false;
            while (state.Pool.Count < slots)
            {
                state.Pool.Add(null);
                changed = true;
            }

            while (state.Draws.Count < state.Pool.Count)
                state.Draws.Add(0);
            while (state.ReadyAt.Count < state.Pool.Count)
                state.ReadyAt.Add(DateTimeOffset.MinValue);

            for (var i = 0; i < state.Pool.Count; i++)
            {
                if (state.Pool[i] != null || state.ReadyAt[i] > now)
                    continue;
                state.Draws[i]++;
                var card = MobilizationRules.Deal(live.Def.Id, allianceId, i, state.Draws[i], config.Kinds, config.TargetPercent);
                state.Pool[i] = new MobilizationCard { Kind = card.Kind, Tier = card.Tier, Target = card.Target, Points = card.Points };
                changed = true;
            }

            if (changed)
                StateChanged(live, allianceId);
            return state;
        }

        private void HandleMobilization(PlayerSession session, MobilizationTaskCommand command, DateTimeOffset now)
        {
            string? reject;
            lock (_gate)
            {
                reject = TryMobilization(session, command, now, out var allianceId);
                if (reject == null)
                {
                    Flush();
                    // The pool changed for the whole alliance: everyone in it who is online sees the new table.
                    foreach (var online in _online.Values)
                    {
                        if (online.PublicId == session.PublicId || (allianceId.Length > 0 && command.Action == MobilizationActions.Take
                                && AllianceIdOf?.Invoke(online.PublicId) == allianceId))
                            PushTo(online, now);
                    }
                }
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, reject, now);
        }

        private string? TryMobilization(PlayerSession session, MobilizationTaskCommand command, DateTimeOffset now, out string allianceId)
        {
            allianceId = AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
            if (!_events.TryGetValue(command.EventId ?? string.Empty, out var live) || !Sees(live.Def, session))
                return "UnknownEvent";
            // An open alliance-event page asks for a fresh view now and then (mates' cards, the rival's points).
            if (command.Action == MobilizationActions.View)
                return null;
            if (live.Def.Template != EventTemplates.Mobilization)
                return "UnknownEvent";
            if (PhaseOf(live.Def, now) != EventPhase.Live)
                return "NotOpen";
            var config = live.Def.Mobilization;

            switch (command.Action)
            {
                case MobilizationActions.Take:
                {
                    if (allianceId.Length == 0)
                        return "NotInAlliance";
                    if ((MemberCount?.Invoke(allianceId) ?? 0) < config.MinMembers)
                        return "TooFewForEvent";
                    var score = ScoreOf(live, session.PublicId, create: true)!;
                    RollMobilizationDay(score, now);
                    if (score.Task != null)
                        return "TaskHeld";
                    if (score.TakenToday >= config.TasksPerDay)
                        return "NoTasksLeft";
                    var pool = Pool(live, allianceId, now);
                    if (command.Slot < 0 || command.Slot >= pool.Pool.Count || pool.Pool[command.Slot] == null)
                        return "TaskGone";

                    score.Task = pool.Pool[command.Slot];
                    score.TaskProgress = 0;
                    score.TakenToday++;
                    score.AllianceId = allianceId;
                    score.UpdatedAt = now;
                    pool.Pool[command.Slot] = null;
                    pool.ReadyAt[command.Slot] = now.AddMinutes(Math.Max(0, config.RefillMinutes));
                    StateChanged(live, allianceId);
                    if (config.RefillMinutes <= 0)
                        Pool(live, allianceId, now);
                    _dirtyScores.Add(score);
                    return null;
                }

                case MobilizationActions.Drop:
                {
                    var score = ScoreOf(live, session.PublicId, create: false);
                    if (score?.Task == null)
                        return "NoTask";
                    score.Task = null;
                    score.TaskProgress = 0;
                    score.UpdatedAt = now;
                    _dirtyScores.Add(score);
                    return null;
                }

                case MobilizationActions.Refresh:
                {
                    var score = ScoreOf(live, session.PublicId, create: false);
                    if (score?.Task == null)
                        return "NoTask";
                    RollMobilizationDay(score, now);
                    if (score.RefreshesToday >= config.RefreshesPerDay)
                        return "NoRefreshesLeft";
                    if (score.LastRefresh is { } last && now < last.AddMinutes(config.RefreshCooldownMinutes))
                        return "RefreshCooldown";

                    score.Refreshes++;
                    score.RefreshesToday++;
                    score.LastRefresh = now;
                    var card = MobilizationRules.Reroll(live.Def.Id, session.PublicId, score.Refreshes, config.Kinds, config.TargetPercent);
                    score.Task = new MobilizationCard { Kind = card.Kind, Tier = card.Tier, Target = card.Target, Points = card.Points };
                    score.TaskProgress = 0;
                    score.UpdatedAt = now;
                    _dirtyScores.Add(score);
                    return null;
                }

                default:
                    return "MalformedCommand";
            }
        }

        /// <summary>
        /// Moves the player's held task with what they just did; a finished task pays its points to the player and the
        /// alliance they are in now. Returns whether anything changed. Caller holds the lock.
        /// </summary>
        private bool RecordMobilization(Live live, string playerId, string allianceId, List<(string Action, long Amount)> actions, DateTimeOffset now)
        {
            var score = ScoreOf(live, playerId, create: false);
            var task = score?.Task;
            if (score == null || task == null)
                return false;
            var moved = 0L;
            foreach (var (action, amount) in actions)
            {
                if (amount > 0 && MobilizationRules.Moves(task.Kind, action))
                    moved += amount;
            }

            if (moved <= 0)
                return false;
            score.TaskProgress = Math.Min(task.Target, score.TaskProgress + moved);
            score.UpdatedAt = now;
            _dirtyScores.Add(score);
            if (score.TaskProgress < task.Target)
                return true;

            // Done: points to the player and their alliance; the hand is free for the next card.
            score.Task = null;
            score.TaskProgress = 0;
            score.Completed++;
            AddPoints(live, score, allianceId, task.Points, now);
            return true;
        }

        /// <summary>The player's view: their alliance's pool, their task and limits. Caller holds the lock.</summary>
        private MobilizationDto MobilizationView(Live live, string playerId, EventScore? score, DateTimeOffset now)
        {
            var config = live.Def.Mobilization;
            var allianceId = AllianceIdOf?.Invoke(playerId) ?? string.Empty;
            var day = UtcDay(now);
            var sameDay = score != null && score.TaskDay == day;
            var dto = new MobilizationDto
            {
                InAlliance = allianceId.Length > 0,
                Members = allianceId.Length > 0 ? MemberCount?.Invoke(allianceId) ?? 0 : 0,
                MinMembers = config.MinMembers,
                TasksPerDay = config.TasksPerDay,
                TasksLeftToday = Math.Max(0, config.TasksPerDay - (sameDay ? score!.TakenToday : 0)),
                RefreshesLeftToday = Math.Max(0, config.RefreshesPerDay - (sameDay ? score!.RefreshesToday : 0)),
                RefreshReadyAtMs = score?.LastRefresh is { } last && last.AddMinutes(config.RefreshCooldownMinutes) > now
                    ? last.AddMinutes(config.RefreshCooldownMinutes).ToUnixTimeMilliseconds() : 0,
                Completed = score?.Completed ?? 0
            };
            if (score?.Task is { } task)
                dto.Mine = new MobilizationTaskDto { Kind = task.Kind, Tier = task.Tier, Target = task.Target, Points = task.Points, Slot = -1, Progress = score.TaskProgress };

            // The table is dealt once the alliance may play (and while the event runs).
            if (allianceId.Length > 0 && PhaseOf(live.Def, now) == EventPhase.Live && dto.Members >= config.MinMembers)
            {
                var pool = Pool(live, allianceId, now);
                dto.Pool = pool.Pool.Select((card, i) => card == null
                    ? new MobilizationTaskDto { Slot = i, ReadyAtMs = pool.ReadyAt[i].ToUnixTimeMilliseconds() }
                    : new MobilizationTaskDto { Kind = card.Kind, Tier = card.Tier, Target = card.Target, Points = card.Points, Slot = i }).ToArray();
            }

            return dto;
        }

        /// <summary>The Overseer's live view of one Mobilization: every alliance's table and points.</summary>
        public sealed record MobilizationPoolView(string AllianceId, string Tag, string Name, long Points, int Members, List<MobilizationTaskDto> Cards, int Holding);

        public List<MobilizationPoolView> MobilizationPools(string eventId, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!_events.TryGetValue(eventId ?? string.Empty, out var live) || live.Def.Template != EventTemplates.Mobilization)
                    return new List<MobilizationPoolView>();
                var holding = live.Scores.Values.Where(s => s.Task != null && s.AllianceId.Length > 0).GroupBy(s => s.AllianceId)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
                return live.States.Where(p => p.Value.Pool.Count > 0).Select(p =>
                {
                    var info = AllianceInfo?.Invoke(p.Key);
                    var cards = p.Value.Pool.Select((card, i) => card == null
                        ? new MobilizationTaskDto { Slot = i, ReadyAtMs = i < p.Value.ReadyAt.Count ? p.Value.ReadyAt[i].ToUnixTimeMilliseconds() : 0 }
                        : new MobilizationTaskDto { Kind = card.Kind, Tier = card.Tier, Target = card.Target, Points = card.Points, Slot = i }).ToList();
                    return new MobilizationPoolView(p.Key, info?.Tag ?? string.Empty, info?.Name ?? "?", live.Alliances.GetValueOrDefault(p.Key),
                        MemberCount?.Invoke(p.Key) ?? 0, cards, holding.GetValueOrDefault(p.Key));
                }).OrderByDescending(v => v.Points).ThenBy(v => v.AllianceId, StringComparer.Ordinal).ToList();
            }
        }
    }
}
