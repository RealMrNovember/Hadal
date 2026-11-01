using Hadal.Game.Mailbox;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Game.World;

namespace Hadal.Game.Events
{
    /// <summary>
    /// The Lure Trap (docs/29 §4.1, LureTrapRules): members' monster kills charge their alliance's bait while the event is
    /// live; the leader or a war-duty officer schedules each of the two traps. A scheduled trap is an event boss of its
    /// own (index <see cref="LureIndexBase"/> and up) that the world raises at the alliance's rally point when its time
    /// comes, for the alliance only. Its hits go through the boss path (RecordBossHit, BossProgress), and here into the
    /// trap's damage and the hitter's points. At the end every member who hit a Gulper gets the alliance's tier reward.
    /// </summary>
    public sealed partial class EventService
    {
        /// <summary>Gulper boss indices start here, two per alliance (EventAllianceState.LureIndex), clear of the Overseer's bosses.</summary>
        public const int LureIndexBase = 100_000;

        /// <summary>A member's rank and duty (the leader or a war-duty officer schedules); set by the world.</summary>
        public Func<string, (int Rank, string Role)>? AllianceRole { get; set; }

        private static TimeSpan Fight(EventDefinition def) => TimeSpan.FromMinutes(Math.Clamp(def.LureTrap.FightMinutes, 5, 240));

        private void HandleLure(PlayerSession session, LureTrapCommand command, DateTimeOffset now)
        {
            string? reject;
            lock (_gate)
            {
                reject = TryLure(session, command, now);
                if (reject == null)
                {
                    Flush();
                    PushTo(session, now);
                }
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, reject, now);
        }

        private string? TryLure(PlayerSession session, LureTrapCommand command, DateTimeOffset now)
        {
            if (!_events.TryGetValue(command.EventId ?? string.Empty, out var live) || !Sees(live.Def, session) || live.Def.Template != EventTemplates.LureTrap)
                return "UnknownEvent";
            if (command.Action == LureTrapActions.View)
                return null;
            if (PhaseOf(live.Def, now) != EventPhase.Live)
                return "NotOpen";
            var allianceId = AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
            if (allianceId.Length == 0)
                return "NotInAlliance";
            var (rank, role) = AllianceRole?.Invoke(session.PublicId) ?? (0, string.Empty);
            if (!AlliancePermissions.CanRunEvents(rank, role))
                return "NoPermission";
            if (command.Trap < 0 || command.Trap >= LureTrapRules.Traps)
                return "UnknownTrap";

            var def = live.Def;
            var state = StateOf(live, allianceId);
            var trap = state.Traps[command.Trap];
            // A trap that has risen (or is up now) is fixed; one still ahead can be moved or called off.
            if (trap.Done || trap.StartsAt is { } started && started <= now)
                return "TrapStarted";
            switch (command.Action)
            {
                case LureTrapActions.Schedule:
                {
                    if ((MemberCount?.Invoke(allianceId) ?? 0) < Math.Max(1, def.LureTrap.MinMembers))
                        return "TooFewMembers";
                    if (!LureTrapRules.IsDifficulty(command.Difficulty))
                        return "UnknownDifficulty";
                    var fight = Fight(def);
                    var nowMs = now.ToUnixTimeMilliseconds();
                    if (!LureTrapRules.CanStart(command.StartsAtMs, nowMs, def.EndsAt.ToUnixTimeMilliseconds(), (int)fight.TotalMinutes))
                        return "BadTime";
                    var start = DateTimeOffset.FromUnixTimeMilliseconds(command.StartsAtMs);
                    // One Gulper at a time: the other trap's fight may not overlap this one.
                    var other = state.Traps[1 - command.Trap];
                    if (other.StartsAt is { } o && start < o + fight && o < start + fight)
                        return "TrapOverlap";
                    if (state.LureIndex == 0)
                        state.LureIndex = LureIndexBase + live.States.Values.Count(s => s.LureIndex > 0) * LureTrapRules.Traps;
                    trap.StartsAt = start;
                    trap.Difficulty = command.Difficulty;
                    trap.ScheduledBy = session.PublicId;
                    trap.Kingdom = Kingdom(session.PublicId);
                    break;
                }

                case LureTrapActions.Cancel:
                    if (trap.StartsAt == null)
                        return "NotScheduled";
                    trap.StartsAt = null;
                    trap.Difficulty = 0;
                    break;

                default:
                    return "MalformedCommand";
            }

            StateChanged(live, allianceId);
            live.Dirty = true;
            RefreshBosses(now);
            return null;
        }

        /// <summary>Monster kills charge the alliance's bait (a new trap level also strengthens a Gulper that is up). Caller holds the lock.</summary>
        private bool RecordLureBait(Live live, string allianceId, List<(string Action, long Amount)> actions, DateTimeOffset now)
        {
            if (allianceId.Length == 0)
                return false;
            var kills = actions.Where(a => a.Action == EventActions.MonsterKill).Sum(a => a.Amount);
            if (kills <= 0 || live.Def.LureTrap.BaitPerKill <= 0)
                return false;
            var state = StateOf(live, allianceId);
            var before = LureTrapRules.Level(state.Bait);
            state.Bait += kills * live.Def.LureTrap.BaitPerKill;
            StateChanged(live, allianceId);
            live.Dirty = true;
            if (LureTrapRules.Level(state.Bait) != before)
                RefreshBosses(now);
            return true;
        }

        /// <summary>The scheduled Gulpers of live Lure Trap events, as boss spawns (the world raises each at its start). Caller holds the lock.</summary>
        private IEnumerable<EventBossSpawn> LureSpawns(Live live)
        {
            var def = live.Def;
            var fight = Fight(def);
            foreach (var (allianceId, state) in live.States)
            {
                if (state.LureIndex == 0)
                    continue;
                var bonus = LureTrapRules.AttackBonus(LureTrapRules.Level(state.Bait));
                for (var t = 0; t < state.Traps.Count; t++)
                {
                    var trap = state.Traps[t];
                    var index = state.LureIndex + t;
                    if (trap.StartsAt is not { } start || trap.Done || def.PaidBosses.Contains(index))
                        continue;
                    var progress = def.BossProgress.FirstOrDefault(p => p.Index == index);
                    yield return new EventBossSpawn(def.Id, index, LureTrapRules.GulperKind, LureTrapRules.GulperLevel(trap.Difficulty), -1, -1,
                        Min(start + fight, def.EndsAt), progress?.Left ?? -1,
                        progress == null ? null : new Dictionary<string, long>(progress.Damage, StringComparer.Ordinal),
                        trap.Kingdom.Length > 0 ? trap.Kingdom : AllianceLandRules.DefaultWorld, allianceId, start, bonus, trap.ScheduledBy);
                }
            }
        }

        private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

        /// <summary>The trap (alliance and slot) a Gulper index belongs to. Caller holds the lock.</summary>
        private static (string AllianceId, EventAllianceState State, LureTrapSlot Trap)? LureTrapOf(Live live, int index)
        {
            if (index < LureIndexBase)
                return null;
            foreach (var (allianceId, state) in live.States)
            {
                if (state.LureIndex > 0 && index >= state.LureIndex && index < state.LureIndex + state.Traps.Count)
                    return (allianceId, state, state.Traps[index - state.LureIndex]);
            }

            return null;
        }

        /// <summary>A hit on a Gulper: the trap's damage and the hitter's points (a recalled hit takes both back). Caller holds the lock.</summary>
        private void RecordLureHit(Live live, int index, string playerId, long delta, DateTimeOffset now)
        {
            if (LureTrapOf(live, index) is not { } found)
                return;
            var (allianceId, _, trap) = found;
            var total = (trap.Damage.TryGetValue(playerId, out var had) ? had : 0) + delta;
            if (total > 0)
                trap.Damage[playerId] = total;
            else
                trap.Damage.Remove(playerId);
            StateChanged(live, allianceId);
            var score = ScoreOf(live, playerId, create: true)!;
            AddPoints(live, score, allianceId, delta, now);
        }

        /// <summary>A Gulper came and went: its trap is done (its damage final). Caller holds the lock.</summary>
        private void FinishLureTrap(Live live, int index)
        {
            if (LureTrapOf(live, index) is not { } found)
                return;
            var (allianceId, _, trap) = found;
            trap.Done = true;
            StateChanged(live, allianceId);
            live.Dirty = true;
        }

        /// <summary>
        /// The tier mails of an ended Lure Trap: every member who hit one of the alliance's Gulpers gets the tier the
        /// alliance reached over both traps; each trap's top hitter gets it TopHitterMultiplier times. Caller holds the lock.
        /// </summary>
        private void PlanLurePayout(Live live, string name, List<KeyedMail> mails)
        {
            var def = live.Def;
            var tiers = def.LureTrap.Tiers.OrderBy(t => t.Points).ToList();
            if (tiers.Count == 0)
                return;
            var thresholds = tiers.Select(t => t.Points).ToList();
            foreach (var (allianceId, state) in live.States.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var total = state.Traps.Sum(t => t.Damage.Values.Sum());
                var tier = LureTrapRules.Tier(total, thresholds);
                if (tier == 0)
                    continue;
                var rewards = tiers[tier - 1].Rewards;
                var tops = state.Traps.Where(t => t.Damage.Count > 0)
                    .Select(t => t.Damage.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First().Key)
                    .ToHashSet(StringComparer.Ordinal);
                var hitters = state.Traps.SelectMany(t => t.Damage.Where(p => p.Value > 0).Select(p => p.Key)).Distinct().OrderBy(p => p, StringComparer.Ordinal);
                foreach (var playerId in hitters)
                {
                    var times = tops.Contains(playerId) ? Math.Clamp(def.LureTrap.TopHitterMultiplier, 1, 10) : 1;
                    var scaled = rewards.Select(r => new EventRewardConfig { Type = r.Type, Id = r.Id, Rarity = r.Rarity, Amount = r.Amount * times });
                    Add(mails, $"ev:{def.Id}:{playerId}:lure", playerId, $"loc:mail.lure.tier.title|{name}",
                        $"loc:mail.lure.tier.body|{name}|{tier}|{total}|{times}", scaled);
                }
            }
        }

        private LureTrapDto LureView(Live live, string playerId, DateTimeOffset now)
        {
            var def = live.Def;
            var allianceId = AllianceIdOf?.Invoke(playerId) ?? string.Empty;
            var (rank, role) = AllianceRole?.Invoke(playerId) ?? (0, string.Empty);
            var thresholds = def.LureTrap.Tiers.Select(t => t.Points).OrderBy(p => p).ToArray();
            var dto = new LureTrapDto
            {
                InAlliance = allianceId.Length > 0,
                CanSchedule = allianceId.Length > 0 && AlliancePermissions.CanRunEvents(rank, role),
                MinMembers = def.LureTrap.MinMembers,
                Members = allianceId.Length > 0 ? MemberCount?.Invoke(allianceId) ?? 0 : 0,
                FightMinutes = (int)Fight(def).TotalMinutes,
                TierThresholds = thresholds
            };
            if (allianceId.Length == 0)
                return dto;
            var state = live.States.TryGetValue(allianceId, out var s) ? s : EventAllianceState.FromJson(null);
            dto.Bait = state.Bait;
            dto.Level = LureTrapRules.Level(state.Bait);
            dto.AllianceDamage = state.Traps.Sum(t => t.Damage.Values.Sum());
            dto.Tier = LureTrapRules.Tier(dto.AllianceDamage, thresholds);
            var fight = Fight(def);
            dto.Traps = state.Traps.Select((trap, t) =>
            {
                var slot = new LureTrapSlotDto
                {
                    Difficulty = trap.Difficulty,
                    Damage = trap.Damage.Values.Sum(),
                    MyDamage = trap.Damage.TryGetValue(playerId, out var mine) ? mine : 0,
                    Done = trap.Done,
                    Board = trap.Damage.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Take(10)
                        .Select((p, i) => new BossBoardEntryDto { PlayerId = p.Key, Name = NameLookup?.Invoke(p.Key) ?? string.Empty, Damage = p.Value, Rank = i + 1 })
                        .ToArray()
                };
                if (trap.StartsAt is { } start)
                {
                    slot.StartsAtMs = start.ToUnixTimeMilliseconds();
                    slot.EndsAtMs = Min(start + fight, def.EndsAt).ToUnixTimeMilliseconds();
                    if (!trap.Done && start <= now && now.ToUnixTimeMilliseconds() < slot.EndsAtMs)
                        slot.MonsterId = WorldService.BossId(def.Id, state.LureIndex + t);
                }

                return slot;
            }).ToArray();
            return dto;
        }
    }
}
