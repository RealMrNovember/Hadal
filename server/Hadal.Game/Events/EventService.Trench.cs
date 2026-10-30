using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Events
{
    /// <summary>
    /// Trench Conquest (docs/29 §4.6, TrenchRules): members register team snapshots into three lanes while registration
    /// is open; then the groups are drawn per kingdom from the alliances with enough teams, and the rounds are fought on
    /// the clock (one every RoundEveryHours). Teams live in the event's alliance state, the league in the definition, so
    /// a restart resumes where it was. Alliances rank by league points (then flags, then fights won); members earn
    /// personal points per fight won (the event's stages).
    /// </summary>
    public sealed partial class EventService
    {
        private static DateTimeOffset RegisterUntil(EventDefinition def) => def.StartsAt.AddHours(Math.Max(1, def.Trench.RegisterHours));

        /// <summary>When round <paramref name="round"/> (0-based) is fought.</summary>
        private static DateTimeOffset RoundAt(EventDefinition def, int round) =>
            RegisterUntil(def).AddHours(Math.Max(0, def.Trench.FirstRoundAfterHours) + round * (double)Math.Max(1, def.Trench.RoundEveryHours));

        /// <summary>The rounds that fit before the event ends (at most TrenchRules.Rounds).</summary>
        private static int RoundCount(EventDefinition def) =>
            Enumerable.Range(0, TrenchRules.Rounds).Count(r => RoundAt(def, r) < def.EndsAt);

        private void HandleTrench(PlayerSession session, TrenchCommand command, DateTimeOffset now)
        {
            string? reject;
            // The snapshot reads the player's own state under the session's lock, before the events' lock is taken.
            var snapshot = command.Action == TrenchActions.Register ? session.TrenchTeam(command.HeroIds) : default;
            lock (_gate)
            {
                reject = TryTrench(session, command, snapshot, now);
                if (reject == null)
                {
                    Flush();
                    PushTo(session, now);
                }
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, reject, now);
        }

        private string? TryTrench(PlayerSession session, TrenchCommand command, (TrenchRules.Team? Team, int Reactor, string? Reject) snapshot,
            DateTimeOffset now)
        {
            if (!_events.TryGetValue(command.EventId ?? string.Empty, out var live) || !Sees(live.Def, session)
                || live.Def.Template != EventTemplates.TrenchConquest)
                return "UnknownEvent";
            if (command.Action == TrenchActions.View)
                return null;
            if (PhaseOf(live.Def, now) != EventPhase.Live)
                return "NotOpen";
            var allianceId = AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
            if (allianceId.Length == 0)
                return "NotInAlliance";

            var def = live.Def;
            var state = StateOf(live, allianceId);
            var mine = state.Teams.FirstOrDefault(t => t.PlayerId == session.PublicId);
            var registering = now < RegisterUntil(def);
            switch (command.Action)
            {
                case TrenchActions.Register:
                {
                    // A fresh snapshot only while registration is open; in the league teams can only change lanes.
                    if (!registering)
                        return "RegistrationClosed";
                    if (snapshot.Reject != null || snapshot.Team == null)
                        return snapshot.Reject ?? "NoTroops";
                    if (!TrenchRules.CanRegister(command.Lane, snapshot.Reactor, def.Trench.MinReactor))
                        return command.Lane < 0 || command.Lane >= TrenchRules.Lanes ? "UnknownLane" : "ReactorTooLow";
                    var team = snapshot.Team;
                    team.Lane = command.Lane;
                    state.Teams.RemoveAll(t => t.PlayerId == session.PublicId);
                    state.Teams.Add(team);
                    // Registered members are the ones the alliance's rank reward is mailed to (one point marks them).
                    var score = ScoreOf(live, session.PublicId, create: true)!;
                    score.AllianceId = allianceId;
                    score.Points = Math.Max(score.Points, 1);
                    score.UpdatedAt = now;
                    _dirtyScores.Add(score);
                    break;
                }

                case TrenchActions.Move:
                    if (mine == null)
                        return "NotRegistered";
                    if (command.Lane < 0 || command.Lane >= TrenchRules.Lanes)
                        return "UnknownLane";
                    mine.Lane = command.Lane;
                    break;

                case TrenchActions.Withdraw:
                    if (mine == null)
                        return "NotRegistered";
                    if (!registering)
                        return "RegistrationClosed";
                    state.Teams.Remove(mine);
                    break;

                default:
                    return "MalformedCommand";
            }

            StateChanged(live, allianceId);
            live.Dirty = true;
            return null;
        }

        /// <summary>Draws the groups and fights the rounds that are due in every live league; true when anything happened. Caller holds the lock.</summary>
        private bool RunTrench(DateTimeOffset now)
        {
            var changed = false;
            foreach (var live in _events.Values)
            {
                if (live.Def.Template != EventTemplates.TrenchConquest || live.Def.Status != EventStatus.Published || now < live.Def.StartsAt)
                    continue;
                changed |= AdvanceTrench(live, now < live.Def.EndsAt ? now : live.Def.EndsAt);
            }

            return changed;
        }

        /// <summary>Brings one league up to <paramref name="now"/>: the draw once registration closed, then every round due. Caller holds the lock.</summary>
        private bool AdvanceTrench(Live live, DateTimeOffset now)
        {
            var changed = false;
            if (live.Def.TrenchState.GroupedAt == null && now >= RegisterUntil(live.Def))
            {
                DrawTrench(live, now);
                changed = true;
            }

            if (live.Def.TrenchState.GroupedAt == null)
                return changed;
            var total = RoundCount(live.Def);
            while (DoneRounds(live.Def) < total && now >= RoundAt(live.Def, DoneRounds(live.Def)))
            {
                FightRound(live, DoneRounds(live.Def), now);
                changed = true;
            }

            return changed;
        }

        private static int DoneRounds(EventDefinition def) => def.TrenchState.RoundsFought;

        /// <summary>The draw: per kingdom, alliances with enough registered teams, grouped six by six by team power.</summary>
        private void DrawTrench(Live live, DateTimeOffset now)
        {
            var def = live.Def;
            var kingdoms = (DuelEntrants?.Invoke() ?? Array.Empty<DuelEntrant>()).ToDictionary(e => e.Id, e => e.Kingdom, StringComparer.Ordinal);
            var entrants = live.States
                .Where(s => s.Value.Teams.Count >= Math.Max(1, def.Trench.MinRegistered) && kingdoms.ContainsKey(s.Key))
                .Select(s => (Id: s.Key, Kingdom: kingdoms[s.Key], Power: s.Value.Teams.Sum(t => t.Power)))
                .Where(e => def.Kingdoms.Count == 0 || def.Kingdoms.Contains(e.Kingdom, StringComparer.Ordinal))
                .ToList();
            var copy = def.Clone();
            copy.TrenchState = new TrenchState { GroupedAt = now };
            foreach (var kingdom in entrants.GroupBy(e => e.Kingdom, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
                foreach (var group in TrenchRules.Groups(kingdom.Select(e => new KeyValuePair<string, long>(e.Id, e.Power))))
                    copy.TrenchState.Groups.Add(new TrenchGroup { Kingdom = kingdom.Key, Alliances = group });
            Replace(live, copy, now);
            foreach (var id in copy.TrenchState.Groups.SelectMany(g => g.Alliances))
                SetLeagueTotal(live, id);
            _logger.LogInformation("Trench {Id}: {Groups} groups over {Alliances} alliances.", def.Id, copy.TrenchState.Groups.Count, entrants.Count);
        }

        /// <summary>Fights round <paramref name="round"/> in every group: lane by lane, then points, kill points and the totals.</summary>
        private void FightRound(Live live, int round, DateTimeOffset now)
        {
            var def = live.Def;
            var copy = def.Clone();
            foreach (var group in def.TrenchState.Groups)
            {
                foreach (var (a, b) in TrenchRules.RoundPairs(group.Alliances, round))
                {
                    var result = new TrenchRoundResult { Round = round, A = a, B = b };
                    if (a == TrenchRules.Bye || b == TrenchRules.Bye)
                    {
                        if (a == TrenchRules.Bye)
                            result.BPoints = TrenchRules.WinPoints;
                        else
                            result.APoints = TrenchRules.WinPoints;
                        copy.TrenchState.Rounds.Add(result);
                        continue;
                    }

                    var teamsA = live.States.TryGetValue(a, out var sa) ? sa.Teams : new List<TrenchRules.Team>();
                    var teamsB = live.States.TryGetValue(b, out var sb) ? sb.Teams : new List<TrenchRules.Team>();
                    var lanes = new List<TrenchRules.LaneResult>();
                    for (var lane = 0; lane < TrenchRules.Lanes; lane++)
                    {
                        var l = lane;
                        var fought = TrenchRules.FightLane(Catalog.Troops, teamsA.Where(t => t.Lane == l).ToList(), teamsB.Where(t => t.Lane == l).ToList(),
                            def.Trench.FightsPerTeam, CombatRules.SeedOf($"{def.Id}:{round}:{a}:{b}:{lane}"));
                        lanes.Add(fought);
                        result.Lanes[lane] = fought.Flag;
                        foreach (var (player, wins) in fought.Wins)
                        {
                            if (teamsA.Any(t => t.PlayerId == player))
                                result.AWins += wins;
                            else
                                result.BWins += wins;
                            var score = ScoreOf(live, player, create: true)!;
                            score.Points += wins * Math.Max(0, def.Trench.PointsPerWin);
                            score.UpdatedAt = now;
                            _dirtyScores.Add(score);
                        }
                    }

                    (result.APoints, result.BPoints) = TrenchRules.RoundPoints(lanes);
                    copy.TrenchState.Rounds.Add(result);
                }
            }

            // Counted on its own: a league without groups still moves through its rounds (no endless catch-up).
            copy.TrenchState.RoundsFought = round + 1;
            Replace(live, copy, now);
            foreach (var id in copy.TrenchState.Groups.SelectMany(g => g.Alliances))
                SetLeagueTotal(live, id);
            _logger.LogInformation("Trench {Id}: round {Round} fought.", def.Id, round + 1);
        }

        /// <summary>League points, flags and fights won of an alliance so far.</summary>
        private static (int Points, int Flags, int Wins) Standing(EventDefinition def, string allianceId)
        {
            int points = 0, flags = 0, wins = 0;
            foreach (var r in def.TrenchState.Rounds)
            {
                if (r.A == allianceId)
                {
                    points += r.APoints;
                    flags += r.Lanes.Count(f => f == TrenchRules.FirstFlag);
                    wins += r.AWins;
                }
                else if (r.B == allianceId)
                {
                    points += r.BPoints;
                    flags += r.Lanes.Count(f => f == TrenchRules.SecondFlag);
                    wins += r.BWins;
                }
            }

            return (points, flags, wins);
        }

        /// <summary>The alliance's ranking total: league points first, then flags, then fights won.</summary>
        private void SetLeagueTotal(Live live, string allianceId)
        {
            var (points, flags, wins) = Standing(live.Def, allianceId);
            live.Alliances[allianceId] = points * 1_000_000L + flags * 1_000L + Math.Min(999, wins);
            _dirtyAlliances.Add((live.Def.Id, allianceId));
            live.Dirty = true;
        }

        /// <summary>The player's view of the league. Caller holds the lock.</summary>
        private TrenchDto TrenchView(Live live, string playerId, DateTimeOffset now)
        {
            var def = live.Def;
            var allianceId = AllianceIdOf?.Invoke(playerId) ?? string.Empty;
            var teams = allianceId.Length > 0 && live.States.TryGetValue(allianceId, out var state) ? state.Teams : new List<TrenchRules.Team>();
            var mine = teams.FirstOrDefault(t => t.PlayerId == playerId);
            var done = DoneRounds(def);
            var dto = new TrenchDto
            {
                RegisterUntilMs = RegisterUntil(def).ToUnixTimeMilliseconds(),
                NextRoundAtMs = done < RoundCount(def) ? RoundAt(def, done).ToUnixTimeMilliseconds() : 0,
                RoundsDone = done,
                MinReactor = def.Trench.MinReactor,
                MinRegistered = def.Trench.MinRegistered,
                Registered = mine != null,
                MyLane = mine?.Lane ?? 0,
                MyPower = mine?.Power ?? 0,
                Members = teams.OrderBy(t => t.Lane).ThenByDescending(t => t.Power)
                    .Select(t => new TrenchMemberDto { Name = t.Name, Lane = t.Lane, Power = t.Power, Mine = t.PlayerId == playerId }).ToArray()
            };

            var group = def.TrenchState.Groups.FirstOrDefault(g => g.Alliances.Contains(allianceId, StringComparer.Ordinal));
            if (group == null)
                return dto;
            dto.InLeague = true;
            dto.Group = group.Alliances.Select(id =>
            {
                var (points, flags, wins) = Standing(def, id);
                var info = AllianceInfo?.Invoke(id);
                return new TrenchStandingDto
                {
                    AllianceId = id, Tag = info?.Tag ?? string.Empty, Name = info?.Name ?? string.Empty, Points = points, Flags = flags, Wins = wins,
                    Teams = live.States.TryGetValue(id, out var s) ? s.Teams.Count : 0
                };
            }).OrderByDescending(s => s.Points).ThenByDescending(s => s.Flags).ThenByDescending(s => s.Wins).ToArray();
            dto.Rounds = def.TrenchState.Rounds.Where(r => r.A == allianceId || r.B == allianceId).OrderBy(r => r.Round).Select(r =>
            {
                var first = r.A == allianceId;
                var rival = first ? r.B : r.A;
                var info = rival == TrenchRules.Bye ? null : AllianceInfo?.Invoke(rival);
                return new TrenchRoundDto
                {
                    Round = r.Round + 1,
                    RivalTag = info?.Tag ?? string.Empty,
                    RivalName = info?.Name ?? string.Empty,
                    Bye = rival == TrenchRules.Bye,
                    Points = first ? r.APoints : r.BPoints,
                    Lanes = r.Lanes.Select(f => f == TrenchRules.NoFlag ? 0 : (f == TrenchRules.FirstFlag) == first ? 1 : 2).ToArray()
                };
            }).ToArray();
            return dto;
        }
    }
}
