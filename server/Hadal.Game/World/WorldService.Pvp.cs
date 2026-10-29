using System.Collections.Concurrent;
using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;
using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.World
{
    /// <summary>One team in a PvP march record: whose, which march, and its travel to the target (rallies).</summary>
    public sealed record PvpMember(string PlayerId, string MarchId, int TravelSeconds);

    /// <summary>
    /// A battle on its way (attack, scout, rally) or a reinforcement standing at a base (docs/30). The battle happens at
    /// <see cref="ArrivesAt"/>; persisted so a restart does not lose it.
    /// </summary>
    public sealed class PvpRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string AttackerId { get; set; } = string.Empty;
        public string TargetKind { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetOwnerId { get; set; } = string.Empty;
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset ArrivesAt { get; set; }

        /// <summary>Rally: the army leaves the leader's base then.</summary>
        public DateTimeOffset? DepartsAt { get; set; }
        public List<PvpMember> Members { get; set; } = new();
    }

    public partial interface IWorldStore
    {
        Task<List<PvpRecord>> LoadPvpAsync(CancellationToken cancellationToken);

        /// <summary>Saves (or with a null record, deletes) a PvP record.</summary>
        Task SavePvpAsync(string id, PvpRecord? record, CancellationToken cancellationToken);
    }

    public sealed partial class InMemoryWorldStore
    {
        private readonly ConcurrentDictionary<string, string> _pvp = new(StringComparer.Ordinal);

        public Task<List<PvpRecord>> LoadPvpAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_pvp.Values.Select(j => JsonSerializer.Deserialize<PvpRecord>(j)!).ToList());

        public Task SavePvpAsync(string id, PvpRecord? record, CancellationToken cancellationToken)
        {
            if (record == null)
                _pvp.TryRemove(id, out _);
            else
                _pvp[id] = JsonSerializer.Serialize(record);
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresWorldStore
    {
        private const string PvpSchema = @"
CREATE TABLE IF NOT EXISTS hadal.world_pvp (
    id   text PRIMARY KEY,
    doc  text NOT NULL
);";

        private Task InitializePvpAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, PvpSchema, cancellationToken);

        public async Task<List<PvpRecord>> LoadPvpAsync(CancellationToken cancellationToken)
        {
            var result = new List<PvpRecord>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT doc FROM hadal.world_pvp", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                if (JsonSerializer.Deserialize<PvpRecord>(reader.GetString(0)) is { } record)
                    result.Add(record);
            return result;
        }

        public async Task SavePvpAsync(string id, PvpRecord? record, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = record == null
                ? new NpgsqlCommand("DELETE FROM hadal.world_pvp WHERE id = @i", connection)
                : new NpgsqlCommand("INSERT INTO hadal.world_pvp (id, doc) VALUES (@i, @d) ON CONFLICT (id) DO UPDATE SET doc = EXCLUDED.doc", connection);
            command.Parameters.AddWithValue("i", id);
            if (record != null)
                command.Parameters.AddWithValue("d", JsonSerializer.Serialize(record));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// The PvP engine (docs/30 §2): every hostile march is settled at its server arrival instant, in arrival order,
    /// before any command or tick that comes later; players involved are kept resident so offline ones are simulated.
    /// </summary>
    public sealed partial class WorldService
    {
        private readonly Dictionary<string, PvpRecord> _pvp = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _pvpByMarch = new(StringComparer.Ordinal);
        private readonly ConcurrentQueue<(string Id, PvpRecord? Record)> _pvpWrites = new();
        private long _nextDueMs = long.MaxValue;

        /// <summary>A loaded session by public player id (online or resident); set by the game world.</summary>
        public Func<string, PlayerSession?> SessionOf { get; set; } = _ => null;

        /// <summary>Asks the game world to load a player's session without a connection.</summary>
        public Action<string> Summon { get; set; } = _ => { };

        private async Task InitializePvpAsync(CancellationToken cancellationToken)
        {
            var records = await _store.LoadPvpAsync(cancellationToken);
            lock (_gate)
            {
                foreach (var r in records)
                    Index(r);
                RefreshDue();
            }

            foreach (var id in records.SelectMany(Involved).Distinct(StringComparer.Ordinal))
                Summon(id);
            if (records.Count > 0)
                _logger.LogInformation("World map: {Count} PvP marches pending.", records.Count);
        }

        private static IEnumerable<string> Involved(PvpRecord r) =>
            r.Members.Select(m => m.PlayerId).Append(r.TargetOwnerId).Where(id => id.Length > 0);

        /// <summary>The player is in a pending battle or stands guard somewhere: keep the session loaded.</summary>
        public bool NeedsResident(string playerId)
        {
            lock (_gate)
                return _pvp.Values.Any(r => Involved(r).Contains(playerId));
        }

        private void Index(PvpRecord r)
        {
            _pvp[r.Id] = r;
            foreach (var m in r.Members)
                _pvpByMarch[m.MarchId] = r.Id;
        }

        private void Store(PvpRecord r)
        {
            Index(r);
            _pvpWrites.Enqueue((r.Id, JsonSerializer.Deserialize<PvpRecord>(JsonSerializer.Serialize(r))));
            RefreshDue();
        }

        private void Drop(PvpRecord r)
        {
            _pvp.Remove(r.Id);
            foreach (var m in r.Members)
                _pvpByMarch.Remove(m.MarchId);
            _pvpWrites.Enqueue((r.Id, null));
            RefreshDue();
        }

        private void RefreshDue() =>
            Interlocked.Exchange(ref _nextDueMs, _pvp.Values.Where(r => r.Mode != PvpRules.Reinforce)
                .Select(r => r.ArrivesAt.ToUnixTimeMilliseconds()).DefaultIfEmpty(long.MaxValue).Min());

        private async Task PvpWriteLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var batch = new Dictionary<string, PvpRecord?>(StringComparer.Ordinal);
                while (_pvpWrites.TryDequeue(out var w))
                    batch[w.Id] = w.Record;
                foreach (var pair in batch)
                {
                    try
                    {
                        await _store.SavePvpAsync(pair.Key, pair.Value, CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "World PvP write failed.");
                    }
                }
            }
        }

        // --- Settling ----------------------------------------------------------------------------

        /// <summary>
        /// Resolves every battle due at or before <paramref name="now"/>, in arrival order (ties by id). Called before every
        /// command and every tick, so nothing that happens later can reach back into a battle. Stops (and retries on the
        /// next call) when a player it needs is still being loaded.
        /// </summary>
        public void Settle(DateTimeOffset now)
        {
            if (now.ToUnixTimeMilliseconds() < Interlocked.Read(ref _nextDueMs))
                return;
            lock (_gate)
            {
                while (true)
                {
                    var next = _pvp.Values.Where(r => r.Mode != PvpRules.Reinforce && r.ArrivesAt <= now)
                        .OrderBy(r => r.ArrivesAt).ThenBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (next == null)
                        break;
                    bool done;
                    try
                    {
                        done = Resolve(next, now);
                    }
                    catch (Exception exception)
                    {
                        // A broken record must not block every later battle: call its teams home.
                        _logger.LogError(exception, "PvP battle {Id} failed; teams sent home.", next.Id);
                        foreach (var m in next.Members)
                            SessionOf(m.PlayerId)?.SendHome(m.MarchId, next.ArrivesAt, next.ToX, next.ToY, now);
                        done = true;
                    }

                    if (!done)
                        break;
                    Drop(next);
                    PushIncoming(next.TargetOwnerId, now);
                }
            }
        }

        /// <summary>Sessions for these players, summoning the missing ones; null until all are loaded.</summary>
        private Dictionary<string, PlayerSession>? Sessions(IEnumerable<string> ids)
        {
            var result = new Dictionary<string, PlayerSession>(StringComparer.Ordinal);
            var missing = false;
            foreach (var id in ids.Where(id => id.Length > 0).Distinct(StringComparer.Ordinal))
            {
                if (SessionOf(id) is { } s)
                    result[id] = s;
                else
                {
                    Summon(id);
                    missing = true;
                }
            }

            return missing ? null : result;
        }

        /// <summary>One battle at r.ArrivesAt. Caller holds the world lock. False = a session is still loading.</summary>
        private bool Resolve(PvpRecord r, DateTimeOffset now)
        {
            var at = r.ArrivesAt;
            var stations = r.TargetKind == PvpRules.TargetBase
                ? _pvp.Values.Where(s => s.Mode == PvpRules.Reinforce && s.TargetId == r.TargetId).ToList()
                : new List<PvpRecord>();
            var sessions = Sessions(r.Members.Select(m => m.PlayerId).Append(r.TargetOwnerId).Concat(stations.Select(s => s.AttackerId)));
            if (sessions == null)
                return false;

            var teams = r.Members.Select(m => sessions[m.PlayerId].ReadTeam(m.MarchId, at))
                .Where(t => t is { Pvp: { Settled: false } }).Select(t => t!).ToList();
            if (teams.Count == 0)
                return true;

            return r.TargetKind switch
            {
                PvpRules.TargetTeam => ResolveTeam(r, teams, sessions, at, now),
                PvpRules.TargetStronghold => ResolveStronghold(r, teams, sessions, at, now),
                _ => ResolveBase(r, teams, stations, sessions, at, now)
            };
        }

        private bool ResolveBase(PvpRecord r, List<PvpTeam> teams, List<PvpRecord> stations, Dictionary<string, PlayerSession> sessions,
            DateTimeOffset at, DateTimeOffset now)
        {
            var defender = sessions[r.TargetOwnerId];
            var kind = r.Mode == PvpRules.Scout ? "scout" : r.Mode == PvpRules.Rally ? "rally" : "attack";
            if (!_bases.TryGetValue(r.TargetOwnerId, out var b) || b.X != r.ToX || b.Y != r.ToY)
            {
                Miss(r, teams, sessions, at, now, kind, "moved", defender);
                return true;
            }

            var snap = defender.SnapshotBase(at);
            if (snap.Shielded || snap.Beginner)
            {
                Miss(r, teams, sessions, at, now, kind, "shielded", defender);
                return true;
            }

            var troops = Catalog.Troops;
            var posts = stations.Select(s => (Record: s, Team: sessions[s.AttackerId].ReadTeam(s.Members[0].MarchId, at)))
                .Where(p => p.Team != null && p.Team.ArrivesAt <= at && p.Team.GatheredAt > at).ToList();
            var guards = posts.Select(p => p.Team!).ToList();

            if (r.Mode == PvpRules.Scout)
            {
                var scout = teams[0];
                sessions[scout.OwnerId].SettleTeam(scout.MarchId, at, Empty, NoResources, turnHome: true, now);
                var seen = new PvpReportDto
                {
                    Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = "scout", Outcome = "scouted", X = r.ToX, Y = r.ToY,
                    Defender = Stacks(snap.Troops, Empty), DefenderPower = TroopRules.Power(troops, snap.Troops), Reinforcements = guards.Count,
                    Plunder = ResourceAmountEntry.FromMap(PvpRules.Plunder(snap.Wallet, snap.Reactor, long.MaxValue))
                };
                Tell(sessions[scout.OwnerId], seen, r.TargetOwnerId, now);
                Tell(defender, new PvpReportDto { Id = r.Id, AtMs = seen.AtMs, Kind = "scouted", Outcome = "scouted", X = r.ToX, Y = r.ToY }, scout.OwnerId, now);
                LogAlliances(r, AllianceLogKinds.Scout, AllianceLogKinds.Scout, now);
                return true;
            }

            // The battle: every attacking team against the troops at home and every team standing guard.
            var attacker = Side(teams.Select(t => t.Troops), teams[0].HeroPowers);
            var defenders = new List<IReadOnlyDictionary<string, long>> { snap.Troops };
            defenders.AddRange(guards.Select(g => g.Troops));
            var defence = Side(defenders, snap.HeroPowers);
            var result = CombatRules.Resolve(troops, attacker, defence);
            var attackLosses = PvpRules.ShareLosses(teams.Select(t => (IReadOnlyDictionary<string, long>)t.Troops).ToList(), result.AttackerLost);
            var defenceLosses = PvpRules.ShareLosses(defenders, result.DefenderLost);

            var plunder = new Dictionary<ResourceType, long>();
            var loads = teams.Select((t, i) => TroopRules.MarchLoad(troops, Minus(t.Troops, attackLosses[i]))).ToList();
            if (result.AttackerWon)
                plunder = PvpRules.Plunder(snap.Wallet, snap.Reactor, loads.Sum());
            var plunderShares = PvpRules.ShareResources(plunder, loads);

            var (homeDead, homeWounded) = defender.ApplyBaseLosses(defenceLosses[0], plunder, now);
            var attStart = Sum(teams.Select(t => t.Troops));
            var defStart = Sum(defenders);
            PvpReportDto Base(string reportKind, bool won) => new()
            {
                Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = reportKind, Outcome = won ? "won" : "lost", X = r.ToX, Y = r.ToY,
                Attacker = Stacks(attStart, result.AttackerLost), Defender = Stacks(defStart, result.DefenderLost),
                AttackerPower = TroopRules.Power(troops, attStart), DefenderPower = TroopRules.Power(troops, defStart), Rounds = result.Rounds
            };

            LogBattle(r, result.AttackerWon, now);
            var defenceReport = Base("defence", !result.AttackerWon);
            defenceReport.Dead = homeDead;
            defenceReport.Wounded = homeWounded;
            defenceReport.Plunder = ResourceAmountEntry.FromMap(plunder);
            Tell(defender, defenceReport, r.AttackerId, now);

            for (var i = 0; i < guards.Count; i++)
            {
                var guard = guards[i];
                var (dead, wounded) = sessions[guard.OwnerId].SettleTeam(guard.MarchId, at, defenceLosses[i + 1], NoResources, turnHome: false, now);
                if (Minus(guard.Troops, defenceLosses[i + 1]).Values.Sum() <= 0)
                    Drop(posts[i].Record); // wiped out: the team heads home empty
                var report = Base("reinforce", !result.AttackerWon);
                (report.Dead, report.Wounded) = (dead, wounded);
                Tell(sessions[guard.OwnerId], report, r.AttackerId, now);
            }

            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                var (dead, wounded) = sessions[team.OwnerId].SettleTeam(team.MarchId, at, attackLosses[i], plunderShares[i], turnHome: true, now);
                var report = Base(kind, result.AttackerWon);
                (report.Dead, report.Wounded) = (dead, wounded);
                report.Plunder = ResourceAmountEntry.FromMap(plunderShares[i]);
                Tell(sessions[team.OwnerId], report, r.TargetOwnerId, now);
            }

            return true;
        }

        private bool ResolveTeam(PvpRecord r, List<PvpTeam> teams, Dictionary<string, PlayerSession> sessions, DateTimeOffset at, DateTimeOffset now)
        {
            var owner = sessions[r.TargetOwnerId];
            var target = owner.ReadTeam(r.TargetId, at);
            if (target == null || target.Pvp != null || target.X != r.ToX || target.Y != r.ToY || target.ArrivesAt > at || target.GatheredAt <= at)
            {
                Miss(r, teams, sessions, at, now, "attack", "gone", null);
                return true;
            }

            var troops = Catalog.Troops;
            var attacker = Side(teams.Select(t => t.Troops), teams[0].HeroPowers);
            var defence = Side(new[] { target.Troops }, target.HeroPowers);
            var result = CombatRules.Resolve(troops, attacker, defence);
            var attackLosses = PvpRules.ShareLosses(teams.Select(t => (IReadOnlyDictionary<string, long>)t.Troops).ToList(), result.AttackerLost);
            var loads = teams.Select((t, i) => TroopRules.MarchLoad(troops, Minus(t.Troops, attackLosses[i]))).ToList();
            var taken = result.AttackerWon ? PvpRules.Fit(target.CargoAt, loads.Sum()) : new Dictionary<ResourceType, long>();
            var takenShares = PvpRules.ShareResources(taken, loads);

            long dead, wounded;
            var survivors = Minus(target.Troops, result.DefenderLost);
            if (result.AttackerWon || survivors.Values.Sum() <= 0)
            {
                var (nodeId, giveBack, d, w) = owner.BeatOffNode(target.MarchId, at, result.DefenderLost, taken, now);
                (dead, wounded) = (d, w);
                if (_nodes.TryGetValue(nodeId, out var node) && node.BusyBy == r.TargetOwnerId)
                {
                    node.Remaining += giveBack;
                    node.BusyUntil = null;
                    node.BusyBy = string.Empty;
                    node.DepletedAt = node.Remaining <= 0 ? at : null;
                    _nodeWrites.Enqueue(node.Copy());
                }
            }
            else
            {
                (dead, wounded) = owner.SettleTeam(target.MarchId, at, result.DefenderLost, NoResources, turnHome: false, now);
            }

            var attStart = Sum(teams.Select(t => t.Troops));
            PvpReportDto Report(string reportKind, bool won) => new()
            {
                Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = reportKind, Outcome = won ? "won" : "lost", X = r.ToX, Y = r.ToY,
                Attacker = Stacks(attStart, result.AttackerLost), Defender = Stacks(target.Troops, result.DefenderLost),
                AttackerPower = TroopRules.Power(troops, attStart), DefenderPower = TroopRules.Power(troops, target.Troops), Rounds = result.Rounds
            };

            LogBattle(r, result.AttackerWon, now);
            var hit = Report("team", !result.AttackerWon);
            (hit.Dead, hit.Wounded) = (dead, wounded);
            hit.Plunder = ResourceAmountEntry.FromMap(taken);
            Tell(owner, hit, r.AttackerId, now);
            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                var (d, w) = sessions[team.OwnerId].SettleTeam(team.MarchId, at, attackLosses[i], takenShares[i], turnHome: true, now);
                var report = Report(r.Mode == PvpRules.Rally ? "rally" : "attack", result.AttackerWon);
                (report.Dead, report.Wounded) = (d, w);
                report.Plunder = ResourceAmountEntry.FromMap(takenShares[i]);
                Tell(sessions[team.OwnerId], report, r.TargetOwnerId, now);
            }

            return true;
        }

        /// <summary>No battle (target moved, shielded, gone): the teams turn home from the target, the attackers are told.</summary>
        private void Miss(PvpRecord r, List<PvpTeam> teams, Dictionary<string, PlayerSession> sessions, DateTimeOffset at, DateTimeOffset now,
            string kind, string outcome, PlayerSession? defender)
        {
            foreach (var team in teams)
            {
                sessions[team.OwnerId].SendHome(team.MarchId, at, r.ToX, r.ToY, now);
                Tell(sessions[team.OwnerId], new PvpReportDto
                {
                    Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = kind, Outcome = outcome, X = r.ToX, Y = r.ToY,
                    StrongholdId = r.TargetKind == PvpRules.TargetStronghold ? r.TargetId : string.Empty
                }, r.TargetOwnerId, now);
            }

            if (defender != null && outcome == "shielded" && r.Mode != PvpRules.Scout)
                Tell(defender, new PvpReportDto { Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = "defence", Outcome = "shielded", X = r.ToX, Y = r.ToY },
                    r.AttackerId, now);
        }

        /// <summary>A battle in both alliances' battle logs (docs/31 §2): attacker, target, x, y.</summary>
        private void LogBattle(PvpRecord r, bool attackerWon, DateTimeOffset now)
        {
            var rally = r.Mode == PvpRules.Rally;
            LogAlliances(r,
                rally ? (attackerWon ? AllianceLogKinds.RallyWon : AllianceLogKinds.RallyLost) : attackerWon ? AllianceLogKinds.AttackWon : AllianceLogKinds.AttackLost,
                attackerWon ? AllianceLogKinds.DefenceLost : AllianceLogKinds.DefenceWon, now);
        }

        private void LogAlliances(PvpRecord r, string attackerType, string defenderType, DateTimeOffset now)
        {
            var target = r.TargetKind == PvpRules.TargetStronghold ? r.TargetId : NameOf(r.TargetOwnerId) ?? string.Empty;
            var args = new[] { NameOf(r.AttackerId) ?? string.Empty, target, r.ToX.ToString(), r.ToY.ToString() };
            var attackerAlliance = _social.AllianceIdOf(r.AttackerId);
            _social.LogBattle(attackerAlliance, attackerType, now, args);
            var defenderAlliance = r.TargetOwnerId.Length > 0 ? _social.AllianceIdOf(r.TargetOwnerId) : string.Empty;
            if (defenderAlliance != attackerAlliance)
                _social.LogBattle(defenderAlliance, defenderType, now, args);
        }

        private void Tell(PlayerSession session, PvpReportDto report, string opponentId, DateTimeOffset now)
        {
            if (opponentId.Length > 0)
            {
                report.OpponentId = opponentId;
                report.OpponentName = NameOf(opponentId) ?? string.Empty;
                report.OpponentTag = _social.TagOf(opponentId);
            }

            session.AddWarLog(report, now);
        }

        private static readonly IReadOnlyDictionary<string, long> Empty = new Dictionary<string, long>(StringComparer.Ordinal);
        private static readonly IReadOnlyDictionary<ResourceType, long> NoResources = new Dictionary<ResourceType, long>();

        private static BattleSide Side(IEnumerable<IReadOnlyDictionary<string, long>> armies, IReadOnlyList<int> heroPowers)
        {
            var side = new BattleSide();
            foreach (var army in armies)
                foreach (var pair in army)
                    side.Troops[pair.Key] = (side.Troops.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            side.AtkPercent = side.DefPercent = side.HpPercent = CombatRules.HeroBonus(heroPowers);
            return side;
        }

        private static Dictionary<string, long> Sum(IEnumerable<IReadOnlyDictionary<string, long>> armies)
        {
            var total = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var army in armies)
                foreach (var pair in army)
                    total[pair.Key] = (total.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            return total;
        }

        private static Dictionary<string, long> Minus(IReadOnlyDictionary<string, long> army, IReadOnlyDictionary<string, long> lost) =>
            army.ToDictionary(p => p.Key, p => Math.Max(0, p.Value - (lost.TryGetValue(p.Key, out var l) ? l : 0)), StringComparer.Ordinal);

        private static BattleStackDto[] Stacks(IReadOnlyDictionary<string, long> start, IReadOnlyDictionary<string, long> lost) =>
            start.Where(p => p.Value > 0).Select(p => TroopRules.TryParseKey(p.Key, out var type, out var tier)
                    ? new BattleStackDto { Type = type, Tier = tier, Start = p.Value, Lost = lost.TryGetValue(p.Key, out var l) ? Math.Min(l, p.Value) : 0 }
                    : null)
                .Where(s => s != null).Select(s => s!).ToArray();

        // --- Commands ----------------------------------------------------------------------------

        private void HandlePvp(PlayerSession session, MarchPvpCommand command, DateTimeOffset now)
        {
            var mode = command.Mode ?? string.Empty;
            var kind = command.TargetKind ?? string.Empty;
            if (!PvpRules.IsMode(mode) || (mode != PvpRules.Join && mode != PvpRules.Reinforce && !PvpRules.IsTargetKind(kind)))
            {
                session.Acknowledge(command.CommandId, command.ClientSequence, "MalformedCommand", now);
                return;
            }

            var heroes = command.HeroIds ?? Array.Empty<string>();
            lock (_gate)
            {
                var me = Upsert(session);
                var myAlliance = _social.AllianceIdOf(me.PlayerId);
                string? reason = null;
                PvpLaunch? launch = null;
                PvpRecord? rally = null;
                switch (mode)
                {
                    case PvpRules.Join:
                        if (!_pvp.TryGetValue(command.TargetId ?? string.Empty, out rally) || rally.Mode != PvpRules.Rally || rally.DepartsAt is not { } departs)
                            reason = "UnknownRally";
                        else if (myAlliance.Length == 0 || _social.AllianceIdOf(rally.AttackerId) != myAlliance)
                            reason = "NotInAlliance";
                        else if (now >= departs)
                            reason = "RallyGone";
                        else if (rally.Members.Count >= PvpRules.MaxRallyTeams)
                            reason = "RallyFull";
                        else if (rally.Members.Any(m => m.PlayerId == me.PlayerId))
                            reason = "AlreadyJoined";
                        else if (!_bases.TryGetValue(rally.AttackerId, out var lead))
                            reason = "UnknownRally";
                        else
                            launch = new PvpLaunch(PvpRules.Rally, rally.TargetKind, rally.TargetId, rally.TargetOwnerId, TargetName(rally.TargetKind, rally.TargetOwnerId),
                                me.X, me.Y, rally.ToX, rally.ToY, heroes, rally.Id, false, departs, lead.X, lead.Y);
                        break;
                    case PvpRules.Reinforce:
                        if (!_bases.TryGetValue(command.TargetId ?? string.Empty, out var mate) || mate.PlayerId == me.PlayerId)
                            reason = "UnknownPlayer";
                        else if (myAlliance.Length == 0 || _social.AllianceIdOf(mate.PlayerId) != myAlliance)
                            reason = "NotInAlliance";
                        else if (_pvp.Values.Count(s => s.Mode == PvpRules.Reinforce && s.TargetId == mate.PlayerId) >= PvpRules.MaxReinforcements)
                            reason = "ReinforcementsFull";
                        else
                            launch = new PvpLaunch(PvpRules.Reinforce, PvpRules.TargetBase, mate.PlayerId, mate.PlayerId, mate.Name, me.X, me.Y, mate.X, mate.Y, heroes);
                        break;
                    default:
                        if (mode == PvpRules.Rally && (myAlliance.Length == 0 || !PvpRules.IsMuster(command.MusterSeconds)))
                            reason = myAlliance.Length == 0 ? "NotInAlliance" : "MalformedCommand";
                        else if (mode == PvpRules.Scout && kind != PvpRules.TargetBase)
                            reason = "MalformedCommand";
                        else
                            (launch, reason) = HostileTarget(me, myAlliance, mode, kind, command.TargetId ?? string.Empty, heroes, command.MusterSeconds, now);
                        break;
                }

                if (reason != null || launch == null)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, reason ?? "MalformedCommand", now);
                    return;
                }

                var (march, _) = session.TryStartPvp(launch, command.CommandId, command.ClientSequence, now);
                if (march == null)
                    return;

                var travel = (int)Math.Round((march.ArrivesAt - (march.Pvp!.DepartsAt ?? march.StartedAt)).TotalSeconds);
                if (rally != null)
                {
                    rally.Members.Add(new PvpMember(me.PlayerId, march.Id, travel));
                    rally.ArrivesAt = rally.DepartsAt!.Value.AddSeconds(rally.Members.Max(m => m.TravelSeconds));
                    foreach (var m in rally.Members)
                        SessionOf(m.PlayerId)?.SetRallyTimes(m.MarchId, rally.ArrivesAt, now);
                    Store(rally);
                }
                else
                {
                    var record = new PvpRecord
                    {
                        Id = mode == PvpRules.Rally ? "r" + Guid.NewGuid().ToString("N")[..10] : march.Id,
                        Mode = launch.Mode, AttackerId = me.PlayerId, TargetKind = launch.TargetKind, TargetId = launch.TargetId,
                        TargetOwnerId = launch.TargetOwnerId, FromX = me.X, FromY = me.Y, ToX = launch.ToX, ToY = launch.ToY,
                        StartedAt = now, ArrivesAt = march.ArrivesAt, DepartsAt = launch.DepartsAt,
                        Members = new List<PvpMember> { new(me.PlayerId, march.Id, travel) }
                    };
                    Store(record);
                }

                _marchViews[me.PlayerId] = session.MarchViews();
                if (launch.TargetOwnerId.Length > 0 && launch.Mode != PvpRules.Reinforce)
                {
                    Summon(launch.TargetOwnerId);
                    PushIncoming(launch.TargetOwnerId, now);
                }
            }
        }

        /// <summary>Checks an attack, scout or rally target and plans the launch. Caller holds the world lock.</summary>
        private (PvpLaunch? Launch, string? Reason) HostileTarget(WorldBaseRecord me, string myAlliance, string mode, string kind, string targetId,
            IReadOnlyList<string> heroes, int muster, DateTimeOffset now)
        {
            DateTimeOffset? departs = mode == PvpRules.Rally ? now.AddSeconds(muster) : null;
            bool Ally(string playerId) => playerId == me.PlayerId || (myAlliance.Length > 0 && _social.AllianceIdOf(playerId) == myAlliance);
            switch (kind)
            {
                case PvpRules.TargetBase:
                {
                    if (!_bases.TryGetValue(targetId, out var b))
                        return (null, "UnknownPlayer");
                    if (Ally(b.PlayerId))
                        return (null, "Ally");
                    var live = SessionOf(b.PlayerId);
                    if (live?.IsBeginner ?? PvpRules.IsBeginner(b.Level))
                        return (null, "TargetBeginner");
                    if (live?.ShieldedAt(now) == true)
                        return (null, "TargetShielded");
                    return (new PvpLaunch(mode, kind, b.PlayerId, b.PlayerId, b.Name, me.X, me.Y, b.X, b.Y, heroes, string.Empty, mode == PvpRules.Rally, departs,
                        me.X, me.Y), null);
                }
                case PvpRules.TargetTeam:
                {
                    var nowMs = now.ToUnixTimeMilliseconds();
                    foreach (var pair in _online)
                        _marchViews[pair.Key] = pair.Value.MarchViews();
                    var found = _marchViews.SelectMany(p => p.Value.Select(v => (Owner: p.Key, View: v)))
                        .FirstOrDefault(x => x.View.Id == targetId && x.View.Kind == "gather");
                    if (found.View == null || found.View.ArrivesAtMs > nowMs || found.View.GatheredAtMs <= nowMs)
                        return (null, "UnknownTeam");
                    if (Ally(found.Owner))
                        return (null, "Ally");
                    return (new PvpLaunch(mode, kind, targetId, found.Owner, NameOf(found.Owner) ?? string.Empty, me.X, me.Y, found.View.ToX, found.View.ToY,
                        heroes, string.Empty, mode == PvpRules.Rally, departs, me.X, me.Y), null);
                }
                case PvpRules.TargetStronghold:
                    return StrongholdTarget(me, myAlliance, mode, targetId, heroes, departs);
                default:
                    return (null, "MalformedCommand");
            }
        }

        private string TargetName(string kind, string ownerId) => kind == PvpRules.TargetStronghold ? string.Empty : NameOf(ownerId) ?? string.Empty;

        /// <summary>
        /// A recall of a PvP team (docs/30 §5.3): an attack or scout is called off, a reinforcement leaves its post, a joiner
        /// leaves a rally, the leader cancels the rally (everyone goes home). A rally that has set out cannot be recalled.
        /// Returns true when handled here. Caller holds the world lock.
        /// </summary>
        private bool RecallPvp(PlayerSession session, RecallMarchCommand command, DateTimeOffset now)
        {
            if (!_pvpByMarch.TryGetValue(command.MarchId ?? string.Empty, out var id) || !_pvp.TryGetValue(id, out var r))
                return false;
            if (r.Mode == PvpRules.Rally)
            {
                if (r.DepartsAt is not { } departs || now >= departs)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, "RallyMarching", now);
                    return true;
                }

                var leaving = r.Members.Where(m => m.MarchId == command.MarchId).ToList();
                if (r.Members[0].MarchId == command.MarchId)
                    leaving = r.Members.ToList();
                foreach (var m in leaving)
                    if (SessionOf(m.PlayerId) is { } s)
                        RallyHome(s, m.MarchId, now);
                session.Acknowledge(command.CommandId, command.ClientSequence, null, now);
                if (leaving.Count == r.Members.Count)
                    Drop(r);
                else
                {
                    r.Members.RemoveAll(m => leaving.Contains(m));
                    _pvpByMarch.Remove(command.MarchId!);
                    r.ArrivesAt = departs.AddSeconds(r.Members.Max(m => m.TravelSeconds));
                    foreach (var m in r.Members)
                        SessionOf(m.PlayerId)?.SetRallyTimes(m.MarchId, r.ArrivesAt, now);
                    Store(r);
                }

                foreach (var m in leaving)
                    if (SessionOf(m.PlayerId) is { } s)
                        _marchViews[m.PlayerId] = s.MarchViews();
                PushIncoming(r.TargetOwnerId, now);
                return true;
            }

            var (_, _, reason) = session.Recall(command.MarchId!, command.CommandId, command.ClientSequence, now);
            if (reason == MarchRejectReason.None)
            {
                Drop(r);
                _marchViews[session.PublicId] = session.MarchViews();
                PushIncoming(r.TargetOwnerId, now);
            }

            return true;
        }

        /// <summary>A rally team goes home before the army sets out: from the leader's base if it got there, else turning round.</summary>
        private static void RallyHome(PlayerSession session, string marchId, DateTimeOffset now)
        {
            var team = session.ReadTeam(marchId, now);
            if (team?.Pvp == null)
                return;
            if (team.Pvp.Leader || team.Pvp.ViaAt is not { } via)
                session.SendHome(marchId, now, team.HomeX, team.HomeY, now);
            else if (now >= via)
                session.SendHome(marchId, now, team.Pvp.ViaX, team.Pvp.ViaY, now);
            else
            {
                // Turn round on the way: as far back as it came.
                var done = (now - team.StartedAt).TotalSeconds / Math.Max(1, (via - team.StartedAt).TotalSeconds);
                var x = (int)Math.Round(team.HomeX + (team.Pvp.ViaX - team.HomeX) * done);
                var y = (int)Math.Round(team.HomeY + (team.Pvp.ViaY - team.HomeY) * done);
                session.SendHome(marchId, now, x, y, now);
            }
        }

        // --- Alarms and the war list -------------------------------------------------------------

        /// <summary>Sends a player the hostile marches aimed at them. Caller holds the world lock.</summary>
        private void PushIncoming(string playerId, DateTimeOffset now)
        {
            if (playerId.Length == 0 || SessionOf(playerId) is not { } session)
                return;
            session.SetIncoming(IncomingFor(playerId), now);
        }

        private IncomingDto[] IncomingFor(string playerId) =>
            _pvp.Values.Where(r => r.Mode != PvpRules.Reinforce && r.TargetOwnerId == playerId)
                .OrderBy(r => r.ArrivesAt)
                .Select(r => new IncomingDto
                {
                    MarchId = r.Id, Kind = r.Mode, AttackerId = r.AttackerId, AttackerName = NameOf(r.AttackerId) ?? string.Empty,
                    AttackerTag = _social.TagOf(r.AttackerId), FromX = r.FromX, FromY = r.FromY, ToX = r.ToX, ToY = r.ToY,
                    ArrivesAtMs = r.ArrivesAt.ToUnixTimeMilliseconds(), TargetKind = r.TargetKind,
                    TargetMarchId = r.TargetKind == PvpRules.TargetTeam ? r.TargetId : string.Empty,
                    DepartsAtMs = r.DepartsAt?.ToUnixTimeMilliseconds() ?? 0
                }).ToArray();

        /// <summary>The PvP state a player gets on login: shield, incoming marches, war log.</summary>
        private void PushPvpOnConnect(PlayerSession session, DateTimeOffset now)
        {
            IncomingDto[] incoming;
            lock (_gate)
                incoming = IncomingFor(session.PublicId);
            session.PushPvpOnLogin(incoming, now);
        }

        private void HandleViewWar(PlayerSession session, ViewWarCommand command, DateTimeOffset now)
        {
            WarListDto list;
            lock (_gate)
            {
                var alliance = _social.AllianceIdOf(session.PublicId);
                var rows = new List<WarRowDto>();
                if (alliance.Length > 0)
                {
                    foreach (var r in _pvp.Values.Where(r => r.Mode != PvpRules.Reinforce).OrderBy(r => r.ArrivesAt))
                    {
                        var outgoing = _social.AllianceIdOf(r.AttackerId) == alliance;
                        var incoming = r.TargetOwnerId.Length > 0 && _social.AllianceIdOf(r.TargetOwnerId) == alliance;
                        if (!outgoing && !incoming)
                            continue;
                        var lead = _bases.TryGetValue(r.AttackerId, out var lb) ? lb : null;
                        rows.Add(new WarRowDto
                        {
                            Id = r.Id, Kind = r.Mode, Incoming = incoming && !outgoing, AttackerId = r.AttackerId,
                            AttackerName = NameOf(r.AttackerId) ?? string.Empty, AttackerTag = _social.TagOf(r.AttackerId),
                            TargetKind = r.TargetKind, TargetId = r.TargetKind == PvpRules.TargetTeam ? r.TargetOwnerId : r.TargetId,
                            TargetName = r.TargetKind == PvpRules.TargetStronghold ? string.Empty : NameOf(r.TargetOwnerId) ?? string.Empty,
                            TargetTag = r.TargetOwnerId.Length > 0 ? _social.TagOf(r.TargetOwnerId) : string.Empty,
                            ToX = r.ToX, ToY = r.ToY, ArrivesAtMs = r.ArrivesAt.ToUnixTimeMilliseconds(),
                            DepartsAtMs = r.DepartsAt is { } d && d > now ? d.ToUnixTimeMilliseconds() : 0,
                            Teams = r.Members.Count, MaxTeams = r.Mode == PvpRules.Rally ? PvpRules.MaxRallyTeams : 1,
                            Joined = r.Members.Any(m => m.PlayerId == session.PublicId),
                            FromX = lead?.X ?? r.FromX, FromY = lead?.Y ?? r.FromY
                        });
                    }
                }

                list = new WarListDto { Rows = rows.ToArray(), ServerNowMs = now.ToUnixTimeMilliseconds() };
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, null, now, new List<EntityChangeDto>
            {
                new() { EntityId = EntityIds.AllianceWar, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(list) }
            });
        }

        internal IReadOnlyList<PvpRecord> PvpForTests()
        {
            lock (_gate)
                return _pvp.Values.ToList();
        }
    }
}
