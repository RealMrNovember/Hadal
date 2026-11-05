using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Events
{
    /// <summary>A due Abyss Siege wave: what the world fights. <paramref name="Members"/> are the bases still in the siege.</summary>
    public sealed record SiegeWaveOrder(string EventId, string AllianceId, int Wave, int Difficulty, int MinReactor, IReadOnlyList<string> Members, string Kingdom);

    /// <summary>A fought wave: points by player, the bases breached, and whether the hub held (null: no hub wave, or no hub).</summary>
    public sealed record SiegeWaveResult(SiegeWaveOrder Order, Dictionary<string, long> Points, List<string> Breached, bool? HubHeld);

    /// <summary>
    /// The Abyss Siege (docs/29 §4.2, AbyssSiegeRules): the leader or a war-duty officer sets the start and the difficulty;
    /// each due wave is handed to the world (GameWorld.RunSieges, WorldService.FightSiegeWave) and its result comes back
    /// here, as the members' points, their breaches and the hub. The siege lives in the event's alliance state, so a
    /// restart resumes at the next wave.
    /// </summary>
    public sealed partial class EventService
    {
        /// <summary>Every member of an alliance; set by the world to SocialHub.MembersOf.</summary>
        public Func<string, IReadOnlyList<string>>? AllianceMembers { get; set; }

        private void HandleSiege(PlayerSession session, AbyssSiegeCommand command, DateTimeOffset now)
        {
            string? reject;
            lock (_gate)
            {
                reject = TrySiege(session, command, now);
                if (reject == null)
                {
                    Flush();
                    PushTo(session, now);
                }
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, reject, now);
        }

        private string? TrySiege(PlayerSession session, AbyssSiegeCommand command, DateTimeOffset now)
        {
            if (!_events.TryGetValue(command.EventId ?? string.Empty, out var live) || !Sees(live.Def, session) || live.Def.Template != EventTemplates.AbyssSiege)
                return "UnknownEvent";
            if (command.Action == AbyssSiegeActions.View)
                return null;
            if (PhaseOf(live.Def, now) != EventPhase.Live)
                return "NotOpen";
            var allianceId = AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
            if (allianceId.Length == 0)
                return "NotInAlliance";
            var (rank, role) = AllianceRole?.Invoke(session.PublicId) ?? (0, string.Empty);
            if (!AlliancePermissions.CanRunEvents(rank, role))
                return "NoPermission";

            var siege = StateOf(live, allianceId).Siege;
            // Once the first wave has struck the siege runs to its end.
            if (siege.StartsAt is { } started && started <= now)
                return "SiegeStarted";
            switch (command.Action)
            {
                case AbyssSiegeActions.Schedule:
                    if ((MemberCount?.Invoke(allianceId) ?? 0) < Math.Max(1, live.Def.AbyssSiege.MinMembers))
                        return "TooFewMembers";
                    if (!AbyssSiegeRules.IsDifficulty(command.Difficulty))
                        return "UnknownDifficulty";
                    if (!AbyssSiegeRules.CanStart(command.StartsAtMs, now.ToUnixTimeMilliseconds(), live.Def.EndsAt.ToUnixTimeMilliseconds()))
                        return "BadTime";
                    siege.StartsAt = DateTimeOffset.FromUnixTimeMilliseconds(command.StartsAtMs);
                    siege.Difficulty = command.Difficulty;
                    siege.Kingdom = Kingdom(session.PublicId);
                    break;

                case AbyssSiegeActions.Cancel:
                    if (siege.StartsAt == null)
                        return "NotScheduled";
                    siege.StartsAt = null;
                    siege.Difficulty = 0;
                    break;

                default:
                    return "MalformedCommand";
            }

            StateChanged(live, allianceId);
            live.Dirty = true;
            return null;
        }

        /// <summary>The next wave of every siege whose time has come (the world fights them; one wave per siege per call).</summary>
        public List<SiegeWaveOrder> TakeSiegeWaves(DateTimeOffset now)
        {
            var due = new List<SiegeWaveOrder>();
            var nowMs = now.ToUnixTimeMilliseconds();
            lock (_gate)
            {
                foreach (var live in _events.Values.Where(e => e.Def.Template == EventTemplates.AbyssSiege && PhaseOf(e.Def, now) == EventPhase.Live))
                    foreach (var (allianceId, state) in live.States)
                    {
                        var siege = state.Siege;
                        if (siege.StartsAt is not { } start || siege.WavesDone >= AbyssSiegeRules.Waves)
                            continue;
                        var wave = siege.WavesDone + 1;
                        if (nowMs < AbyssSiegeRules.WaveAtMs(start.ToUnixTimeMilliseconds(), wave))
                            continue;
                        var members = (AllianceMembers?.Invoke(allianceId) ?? Array.Empty<string>())
                            .Where(p => !AbyssSiegeRules.DroppedOut(siege.Breaches.TryGetValue(p, out var b) ? b : 0)).ToList();
                        due.Add(new SiegeWaveOrder(live.Def.Id, allianceId, wave, siege.Difficulty, live.Def.AbyssSiege.MinReactor, members,
                            siege.Kingdom.Length > 0 ? siege.Kingdom : AllianceLandRules.DefaultWorld));
                    }
            }

            return due;
        }

        /// <summary>A fought wave: points, breaches and the hub go on record, once (a repeated wave number changes nothing).</summary>
        public void CompleteSiegeWave(SiegeWaveResult result, DateTimeOffset now)
        {
            var order = result.Order;
            lock (_gate)
            {
                if (!_events.TryGetValue(order.EventId, out var live) || !live.States.TryGetValue(order.AllianceId, out var state)
                    || state.Siege.WavesDone != order.Wave - 1)
                    return;
                var siege = state.Siege;
                siege.WavesDone = order.Wave;
                foreach (var (playerId, points) in result.Points.Where(p => p.Value > 0))
                    AddPoints(live, ScoreOf(live, playerId, create: true)!, order.AllianceId, points, now);
                foreach (var playerId in result.Breached)
                    siege.Breaches[playerId] = (siege.Breaches.TryGetValue(playerId, out var b) ? b : 0) + 1;
                if (result.HubHeld == true)
                {
                    siege.HubHeld++;
                    live.Alliances[order.AllianceId] = (live.Alliances.TryGetValue(order.AllianceId, out var total) ? total : 0)
                                                       + live.Def.AbyssSiege.HubHoldPoints * order.Difficulty;
                    _dirtyAlliances.Add((live.Def.Id, order.AllianceId));
                }
                else if (result.HubHeld == false)
                {
                    siege.HubLost++;
                }

                StateChanged(live, order.AllianceId);
                live.Dirty = true;
                Flush();
                foreach (var session in _online.Values.Where(s => (AllianceIdOf?.Invoke(s.PublicId) ?? string.Empty) == order.AllianceId).ToList())
                    PushTo(session, now);
            }
        }

        private AbyssSiegeDto SiegeView(Live live, string playerId, DateTimeOffset now)
        {
            var allianceId = AllianceIdOf?.Invoke(playerId) ?? string.Empty;
            var (rank, role) = AllianceRole?.Invoke(playerId) ?? (0, string.Empty);
            var config = live.Def.AbyssSiege;
            var dto = new AbyssSiegeDto
            {
                InAlliance = allianceId.Length > 0,
                CanSchedule = allianceId.Length > 0 && AlliancePermissions.CanRunEvents(rank, role),
                MinMembers = config.MinMembers,
                MinReactor = config.MinReactor,
                Members = allianceId.Length > 0 ? MemberCount?.Invoke(allianceId) ?? 0 : 0
            };
            if (allianceId.Length == 0 || !live.States.TryGetValue(allianceId, out var state))
                return dto;
            var siege = state.Siege;
            dto.WavesDone = siege.WavesDone;
            dto.HubHeld = siege.HubHeld;
            dto.HubLost = siege.HubLost;
            dto.MyBreaches = siege.Breaches.TryGetValue(playerId, out var mine) ? mine : 0;
            dto.DroppedOut = siege.Breaches.Values.Count(AbyssSiegeRules.DroppedOut);
            if (siege.StartsAt is { } start)
            {
                dto.StartsAtMs = start.ToUnixTimeMilliseconds();
                dto.Difficulty = siege.Difficulty;
                if (siege.WavesDone < AbyssSiegeRules.Waves && PhaseOf(live.Def, now) == EventPhase.Live)
                    dto.NextWaveAtMs = AbyssSiegeRules.WaveAtMs(dto.StartsAtMs, siege.WavesDone + 1);
            }

            return dto;
        }
    }
}
