using System.Collections.Concurrent;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;
using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.World
{
    /// <summary>A region of the map with its Stronghold (docs/30 §6). Mutated only under the world lock.</summary>
    public sealed class WorldRegion
    {
        public int Index { get; init; }
        public string Id => TerritoryRules.IdOf(Index);
        public int X { get; set; }
        public int Y { get; set; }
        public int Level { get; init; }

        /// <summary>The owning alliance; empty = unowned.</summary>
        public string AllianceId { get; set; } = string.Empty;

        /// <summary>The owner's Alliance Hub stands here.</summary>
        public bool Hub { get; set; }

        /// <summary>Garrison strength (0–1) as of <see cref="ShareAt"/>; it refills over TerritoryRules.RefillHours.</summary>
        public double Share { get; set; } = 1;
        public DateTimeOffset ShareAt { get; set; }

        /// <summary>When the hub last moved here (24 h cooldown).</summary>
        public DateTimeOffset? HubMovedAt { get; set; }

        public WorldRegion Copy() => (WorldRegion)MemberwiseClone();
    }

    public partial interface IWorldStore
    {
        Task<List<WorldRegion>> LoadRegionsAsync(CancellationToken cancellationToken);

        Task SaveRegionsAsync(IReadOnlyCollection<WorldRegion> regions, CancellationToken cancellationToken);
    }

    public sealed partial class InMemoryWorldStore
    {
        private readonly ConcurrentDictionary<int, WorldRegion> _regions = new();

        public Task<List<WorldRegion>> LoadRegionsAsync(CancellationToken cancellationToken) => Task.FromResult(_regions.Values.Select(r => r.Copy()).ToList());

        public Task SaveRegionsAsync(IReadOnlyCollection<WorldRegion> regions, CancellationToken cancellationToken)
        {
            foreach (var r in regions)
                _regions[r.Index] = r.Copy();
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresWorldStore
    {
        private const string RegionSchema = @"
CREATE TABLE IF NOT EXISTS hadal.world_regions (
    region       integer          PRIMARY KEY,
    alliance_id  text             NOT NULL DEFAULT '',
    hub          boolean          NOT NULL DEFAULT false,
    share        double precision NOT NULL DEFAULT 1,
    share_at     timestamptz      NOT NULL DEFAULT now(),
    hub_moved_at timestamptz      NULL
);";

        private Task InitializeRegionsAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, RegionSchema, cancellationToken);

        public async Task<List<WorldRegion>> LoadRegionsAsync(CancellationToken cancellationToken)
        {
            var result = new List<WorldRegion>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT region, alliance_id, hub, share, share_at, hub_moved_at FROM hadal.world_regions", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new WorldRegion
                {
                    Index = reader.GetInt32(0), AllianceId = reader.GetString(1), Hub = reader.GetBoolean(2), Share = reader.GetDouble(3),
                    ShareAt = reader.GetFieldValue<DateTimeOffset>(4), HubMovedAt = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5)
                });
            return result;
        }

        public async Task SaveRegionsAsync(IReadOnlyCollection<WorldRegion> regions, CancellationToken cancellationToken)
        {
            if (regions.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (var r in regions)
            {
                await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.world_regions (region, alliance_id, hub, share, share_at, hub_moved_at) VALUES (@r, @a, @h, @s, @t, @m)
ON CONFLICT (region) DO UPDATE SET alliance_id = EXCLUDED.alliance_id, hub = EXCLUDED.hub, share = EXCLUDED.share,
    share_at = EXCLUDED.share_at, hub_moved_at = EXCLUDED.hub_moved_at", connection, transaction);
                command.Parameters.AddWithValue("r", r.Index);
                command.Parameters.AddWithValue("a", r.AllianceId);
                command.Parameters.AddWithValue("h", r.Hub);
                command.Parameters.AddWithValue("s", r.Share);
                command.Parameters.AddWithValue("t", r.ShareAt);
                command.Parameters.AddWithValue("m", (object?)r.HubMovedAt ?? DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>Cities (docs/32 §2.4): the old strongholds as points with an owner, a garrison and the neighbour rule.</summary>
    public sealed partial class WorldService
    {
        private WorldRegion[]? _regionList;
        private readonly Dictionary<(int, int), WorldRegion> _strongholdAt = new();
        private readonly ConcurrentQueue<WorldRegion> _regionWrites = new();

        private int MapSize => Math.Max(10, Catalog.World.size);

        /// <summary>The regions, laid out from TerritoryRules on first use. Caller holds the lock.</summary>
        private WorldRegion[] Regions
        {
            get
            {
                if (_regionList != null)
                    return _regionList;
                var size = MapSize;
                var seeds = TerritoryRules.Seeds(size);
                _regionList = seeds.Select((s, i) => new WorldRegion { Index = i, X = s.X, Y = s.Y, Level = TerritoryRules.Level(Catalog.World, i) })
                    .ToArray();
                foreach (var r in _regionList)
                    _strongholdAt[(r.X, r.Y)] = r;
                return _regionList;
            }
        }

        private async Task LoadRegionsAsync(CancellationToken cancellationToken)
        {
            var stored = await _store.LoadRegionsAsync(cancellationToken);
            lock (_gate)
            {
                var regions = Regions;
                foreach (var s in stored.Where(s => s.Index >= 0 && s.Index < regions.Length))
                {
                    var r = regions[s.Index];
                    // Hubs are alliance structures now (docs/32); a city keeps only its owner and garrison.
                    (r.AllianceId, r.Hub, r.Share, r.ShareAt, r.HubMovedAt) = (s.AllianceId, false, s.Share, s.ShareAt, s.HubMovedAt);
                }

                // Nodes laid out before strongholds existed move off their tiles.
                var world = Catalog.World;
                foreach (var node in _nodes.Values.Where(n => _strongholdAt.ContainsKey((n.X, n.Y)) && !n.IsBusy(_clock.UtcNow)).ToList())
                    if (GatherRules.BeltOf(world, node.X, node.Y) is { } belt)
                        Spawn(node, belt, _clock.UtcNow);
            }

            _ = Task.Run(RegionWriteLoopAsync);
        }

        private async Task RegionWriteLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                var batch = new Dictionary<int, WorldRegion>();
                while (_regionWrites.TryDequeue(out var r))
                    batch[r.Index] = r;
                if (batch.Count == 0)
                    continue;
                try
                {
                    await _store.SaveRegionsAsync(batch.Values, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "World region write failed.");
                }
            }
        }

        private void SaveRegion(WorldRegion r) => _regionWrites.Enqueue(r.Copy());

        /// <summary>Regions of alliances that no longer exist go back to the trench. Caller holds the lock.</summary>
        private void MaintainRegions()
        {
            foreach (var r in Regions.Where(r => r.AllianceId.Length > 0 && _social.MemberCount(r.AllianceId) == 0))
            {
                r.AllianceId = string.Empty;
                r.Hub = false;
                SaveRegion(r);
            }
        }

        private WorldRegion? RegionAt(int x, int y)
        {
            var index = TerritoryRules.RegionOf(MapSize, x, y);
            return index < 0 ? null : Regions[index];
        }

        private (PvpLaunch? Launch, string? Reason) StrongholdTarget(WorldBaseRecord me, string myAlliance, string mode, string targetId,
            IReadOnlyList<string> heroes, DateTimeOffset? departs)
        {
            if (!TerritoryRules.TryParseId(targetId, out var index) || index >= Regions.Length)
                return (null, "UnknownStronghold");
            var region = Regions[index];
            var reason = CaptureBlock(region, myAlliance, _clock.UtcNow);
            return reason != null
                ? (null, reason)
                : (new PvpLaunch(mode, PvpRules.TargetStronghold, region.Id, string.Empty, string.Empty, me.X, me.Y, region.X, region.Y, heroes,
                    string.Empty, mode == PvpRules.Rally, departs, me.X, me.Y), null);
        }

        private bool ResolveStronghold(PvpRecord r, List<PvpTeam> teams, Dictionary<string, PlayerSession> sessions, DateTimeOffset at, DateTimeOffset now)
        {
            if (!TerritoryRules.TryParseId(r.TargetId, out var index) || index >= Regions.Length)
            {
                Miss(r, teams, sessions, at, now, r.Mode, "gone", null);
                return true;
            }

            var region = Regions[index];
            var alliance = _social.AllianceIdOf(r.AttackerId);
            var share = TerritoryRules.GarrisonShare(region.Share, region.ShareAt, at);
            var troops = Catalog.Troops;
            var garrison = TerritoryRules.Garrison(region.Level, share);
            var attacker = Side(teams.Select(t => t.Troops), teams[0].HeroPowers);
            var result = CombatRules.Resolve(troops, attacker, garrison);
            var losses = PvpRules.ShareLosses(teams.Select(t => (IReadOnlyDictionary<string, long>)t.Troops).ToList(), result.AttackerLost);

            var start = garrison.Troops.ToDictionary(p => p.Key, p => (long)Math.Round(p.Value), StringComparer.Ordinal);
            var lostShare = start.Values.Sum() <= 0 ? 1 : (double)result.DefenderLost.Values.Sum() / start.Values.Sum();
            var owner = region.AllianceId;
            var captured = result.AttackerWon && CaptureBlock(region, alliance, at) == null;
            if (captured)
            {
                region.AllianceId = alliance;
                region.Hub = false;
                region.Share = 1;
                _logger.LogInformation("Alliance {Alliance} captured stronghold {Region}.", alliance, region.Id);
            }
            else
                region.Share = Math.Max(0, share * (1 - lostShare));

            region.ShareAt = at;
            SaveRegion(region);

            // The stronghold's owner (an alliance, no player) logs the defence as well.
            var rally = r.Mode == PvpRules.Rally;
            var args = new[] { NameOf(r.AttackerId) ?? string.Empty, region.Id, r.ToX.ToString(), r.ToY.ToString() };
            _social.LogBattle(alliance, rally ? (result.AttackerWon ? AllianceLogKinds.RallyWon : AllianceLogKinds.RallyLost)
                : result.AttackerWon ? AllianceLogKinds.AttackWon : AllianceLogKinds.AttackLost, now, args);
            if (owner.Length > 0 && owner != alliance)
                _social.LogBattle(owner, result.AttackerWon ? AllianceLogKinds.DefenceLost : AllianceLogKinds.DefenceWon, now, args);

            var attStart = Sum(teams.Select(t => t.Troops));
            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                var (dead, wounded) = sessions[team.OwnerId].SettleTeam(team.MarchId, at, losses[i], NoResources, turnHome: true, now);
                Tell(sessions[team.OwnerId], new PvpReportDto
                {
                    Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = r.Mode == PvpRules.Rally ? "rally" : "attack",
                    Outcome = result.AttackerWon ? "won" : "lost", X = r.ToX, Y = r.ToY, Rounds = result.Rounds,
                    Attacker = Stacks(attStart, result.AttackerLost), Defender = Stacks(start, result.DefenderLost),
                    AttackerPower = TroopRules.Power(troops, attStart), DefenderPower = TroopRules.Power(troops, start),
                    Dead = dead, Wounded = wounded, StrongholdId = region.Id, StrongholdLevel = region.Level, Captured = captured
                }, string.Empty, now);
            }

            return true;
        }

        internal WorldRegion RegionForTests(int index)
        {
            lock (_gate)
                return Regions[index];
        }
    }
}
