using System.Collections.Concurrent;
using Hadal.Game.Persistence;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hadal.Game.World
{
    /// <summary>A resource node on the map (docs/rehber 09 §4). Mutated only under the world lock.</summary>
    public sealed class WorldNode
    {
        public string Id { get; init; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Level { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public long Remaining { get; set; }
        public long Capacity { get; set; }

        /// <summary>A march works the node until then.</summary>
        public DateTimeOffset? BusyUntil { get; set; }
        public string BusyBy { get; set; } = string.Empty;

        /// <summary>Whale falls fade on their own.</summary>
        public DateTimeOffset? FadesAt { get; set; }

        /// <summary>When the node ran dry; it comes back elsewhere after the respawn delay.</summary>
        public DateTimeOffset? DepletedAt { get; set; }

        public bool IsBusy(DateTimeOffset now) => BusyUntil > now;

        public WorldNode Copy() => (WorldNode)MemberwiseClone();
    }

    public partial interface IWorldStore
    {
        Task<List<WorldNode>> LoadNodesAsync(CancellationToken cancellationToken);

        Task SaveNodesAsync(IReadOnlyCollection<WorldNode> nodes, CancellationToken cancellationToken);
    }

    public sealed partial class InMemoryWorldStore
    {
        private readonly ConcurrentDictionary<string, WorldNode> _nodes = new();

        public Task<List<WorldNode>> LoadNodesAsync(CancellationToken cancellationToken) => Task.FromResult(_nodes.Values.Select(n => n.Copy()).ToList());

        public Task SaveNodesAsync(IReadOnlyCollection<WorldNode> nodes, CancellationToken cancellationToken)
        {
            foreach (var n in nodes)
                _nodes[n.Id] = n.Copy();
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresWorldStore
    {
        private const string NodeSchema = @"
CREATE TABLE IF NOT EXISTS hadal.world_nodes (
    id          text        PRIMARY KEY,
    type        text        NOT NULL,
    level       integer     NOT NULL,
    x           integer     NOT NULL,
    y           integer     NOT NULL,
    remaining   bigint      NOT NULL,
    capacity    bigint      NOT NULL,
    busy_until  timestamptz NULL,
    busy_by     text        NOT NULL DEFAULT '',
    fades_at    timestamptz NULL,
    depleted_at timestamptz NULL
);";

        private Task InitializeNodesAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, NodeSchema, cancellationToken);

        public async Task<List<WorldNode>> LoadNodesAsync(CancellationToken cancellationToken)
        {
            var result = new List<WorldNode>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT id, type, level, x, y, remaining, capacity, busy_until, busy_by, fades_at, depleted_at FROM hadal.world_nodes", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new WorldNode
                {
                    Id = reader.GetString(0),
                    Type = reader.GetString(1),
                    Level = reader.GetInt32(2),
                    X = reader.GetInt32(3),
                    Y = reader.GetInt32(4),
                    Remaining = reader.GetInt64(5),
                    Capacity = reader.GetInt64(6),
                    BusyUntil = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                    BusyBy = reader.GetString(8),
                    FadesAt = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
                    DepletedAt = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10)
                });
            }

            return result;
        }

        public async Task SaveNodesAsync(IReadOnlyCollection<WorldNode> nodes, CancellationToken cancellationToken)
        {
            if (nodes.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (var n in nodes)
            {
                await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.world_nodes (id, type, level, x, y, remaining, capacity, busy_until, busy_by, fades_at, depleted_at)
VALUES (@id, @t, @l, @x, @y, @r, @c, @bu, @bb, @f, @d)
ON CONFLICT (id) DO UPDATE SET type = EXCLUDED.type, level = EXCLUDED.level, x = EXCLUDED.x, y = EXCLUDED.y,
    remaining = EXCLUDED.remaining, capacity = EXCLUDED.capacity, busy_until = EXCLUDED.busy_until, busy_by = EXCLUDED.busy_by,
    fades_at = EXCLUDED.fades_at, depleted_at = EXCLUDED.depleted_at", connection, transaction);
                command.Parameters.AddWithValue("id", n.Id);
                command.Parameters.AddWithValue("t", n.Type);
                command.Parameters.AddWithValue("l", n.Level);
                command.Parameters.AddWithValue("x", n.X);
                command.Parameters.AddWithValue("y", n.Y);
                command.Parameters.AddWithValue("r", n.Remaining);
                command.Parameters.AddWithValue("c", n.Capacity);
                command.Parameters.AddWithValue("bu", (object?)n.BusyUntil ?? DBNull.Value);
                command.Parameters.AddWithValue("bb", n.BusyBy);
                command.Parameters.AddWithValue("f", (object?)n.FadesAt ?? DBNull.Value);
                command.Parameters.AddWithValue("d", (object?)n.DepletedAt ?? DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }

    public sealed partial class WorldService
    {
        private readonly Dictionary<string, WorldNode> _nodes = new(StringComparer.Ordinal);
        private readonly Dictionary<(int, int), WorldNode> _nodeAt = new();
        private readonly ConcurrentQueue<WorldNode> _nodeWrites = new();
        private readonly Random _random = new(7);
        private int _nextNodeId;

        /// <summary>Loads the stored nodes, or seeds the map by the guide's density table the first time.</summary>
        private async Task InitializeNodesAsync(CancellationToken cancellationToken, DateTimeOffset now)
        {
            var stored = await _store.LoadNodesAsync(cancellationToken);
            lock (_gate)
            {
                foreach (var n in stored)
                {
                    _nodes[n.Id] = n;
                    _nodeAt[(n.X, n.Y)] = n;
                    if (int.TryParse(n.Id.AsSpan(1), out var number))
                        _nextNodeId = Math.Max(_nextNodeId, number + 1);
                }

                if (_nodes.Count == 0)
                    SeedNodes(now);
            }

            _logger.LogInformation("World map: {Nodes} resource nodes.", _nodes.Count);
        }

        /// <summary>Caller holds the lock.</summary>
        private void SeedNodes(DateTimeOffset now)
        {
            var world = Catalog.World;
            var size = Math.Max(10, world.size);
            foreach (var belt in world.belts)
            {
                var density = world.density.FirstOrDefault(d => d.belt == belt.id);
                if (density == null || density.per100 <= 0)
                    continue;
                var tiles = 0;
                for (var x = 0; x < size; x++)
                    for (var y = 0; y < size; y++)
                        if (GatherRules.BeltOf(world, x, y) == belt)
                            tiles++;
                for (var i = 0; i < tiles * density.per100 / 100; i++)
                    Spawn(new WorldNode { Id = "n" + _nextNodeId++ }, belt, now);
            }
        }

        /// <summary>Puts <paramref name="node"/> fresh on a free tile of <paramref name="belt"/>. Caller holds the lock.</summary>
        private void Spawn(WorldNode node, BeltData belt, DateTimeOffset now)
        {
            var world = Catalog.World;
            var veins = _nodes.Values.Count(n => n.Type == "hadalit_damari" && n != node);
            var type = GatherRules.PickType(world, belt, _random, t => t != "hadalit_damari" || veins < world.maxVeins);
            if (type == null)
                return;

            var size = Math.Max(10, world.size);
            var center = GatherRules.Center(world);
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var d = _random.Next(belt.inner, belt.outer + 1);
                var (x, y) = RingTile(center, d, _random);
                if (x < 0 || y < 0 || x >= size || y >= size || _taken.Contains((x, y)) || _nodeAt.ContainsKey((x, y)))
                    continue;

                if (_nodes.ContainsKey(node.Id))
                    _nodeAt.Remove((node.X, node.Y));
                node.Type = type.id;
                node.Level = GatherRules.PickLevel(world, belt, type, d, _random);
                node.X = x;
                node.Y = y;
                node.Capacity = GatherRules.Capacity(world, type.id, node.Level);
                node.Remaining = node.Capacity;
                node.BusyUntil = null;
                node.BusyBy = string.Empty;
                node.DepletedAt = null;
                node.FadesAt = type.lifeHours > 0 ? now.AddHours(type.lifeHours) : null;
                _nodes[node.Id] = node;
                _nodeAt[(x, y)] = node;
                _nodeWrites.Enqueue(node.Copy());
                return;
            }
        }

        /// <summary>A random tile at Chebyshev distance <paramref name="d"/> from the centre.</summary>
        private static (int X, int Y) RingTile(int center, int d, Random random)
        {
            if (d == 0)
                return (center, center);
            var side = random.Next(4);
            var along = random.Next(-d, d + 1);
            return side switch
            {
                0 => (center + along, center - d),
                1 => (center + d, center + along),
                2 => (center + along, center + d),
                _ => (center - d, center + along)
            };
        }

        /// <summary>Respawns dry and faded nodes and frees finished ones. Caller holds the lock.</summary>
        private void MaintainNodes(DateTimeOffset now)
        {
            var world = Catalog.World;
            foreach (var node in _nodes.Values.ToList())
            {
                if (node.BusyUntil <= now && node.BusyUntil != null)
                {
                    node.BusyUntil = null;
                    node.BusyBy = string.Empty;
                    _nodeWrites.Enqueue(node.Copy());
                }

                var faded = node.FadesAt <= now && !node.IsBusy(now);
                var dry = node.Remaining <= 0 && node.DepletedAt?.AddMinutes(world.respawnMinutes) <= now && !node.IsBusy(now);
                if (!faded && !dry)
                    continue;
                var belt = GatherRules.BeltOf(world, node.X, node.Y);
                if (belt != null)
                    Spawn(node, belt, now);
            }
        }

        private void HandleGather(PlayerSession session, MarchGatherCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                MaintainNodes(now);
                var me = Upsert(session);
                var reason = !_nodes.TryGetValue(command.NodeId, out var node) ? MarchRejectReason.UnknownDeposit
                    : node.IsBusy(now) ? MarchRejectReason.DepositBusy
                    : node.Remaining <= 0 || node.FadesAt <= now ? MarchRejectReason.DepositEmpty
                    : MarchRejectReason.None;
                if (reason != MarchRejectReason.None)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, reason.ToString(), now);
                    return;
                }

                var (march, _) = session.TryStartMarch(new NodeOffer(node!.Id, node.Type, node.Level, node.X, node.Y, node.Remaining, node.FadesAt),
                    me.X, me.Y, command.CommandId, command.ClientSequence, now);
                if (march == null)
                    return;

                node.Remaining -= march.Reserved;
                node.BusyUntil = march.GatheredAt;
                node.BusyBy = session.PublicId;
                node.DepletedAt = node.Remaining <= 0 ? march.GatheredAt : null;
                _nodeWrites.Enqueue(node.Copy());
            }
        }

        private void HandleRecall(PlayerSession session, RecallMarchCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                var (nodeId, giveBack, reason) = session.Recall(command.MarchId, command.CommandId, command.ClientSequence, now);
                if (reason != MarchRejectReason.None || !_nodes.TryGetValue(nodeId, out var node) || node.BusyBy != session.PublicId)
                    return;

                node.Remaining += giveBack;
                node.BusyUntil = null;
                node.BusyBy = string.Empty;
                node.DepletedAt = node.Remaining <= 0 ? now : null;
                _nodeWrites.Enqueue(node.Copy());
            }
        }

        /// <summary>Nodes in view, as the viewer sees them. Caller holds the lock.</summary>
        private WorldNodeDto[] NodesInView(int cx, int cy, int radius, HashSet<string> mine, DateTimeOffset now) =>
            _nodes.Values
                .Where(n => WorldRules.InView(n.X, n.Y, cx, cy, radius) && (n.Remaining > 0 || n.IsBusy(now)) && !(n.FadesAt <= now))
                .Select(n => new WorldNodeDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Level = n.Level,
                    X = n.X,
                    Y = n.Y,
                    Remaining = n.Remaining,
                    Capacity = n.Capacity,
                    Busy = n.IsBusy(now),
                    Mine = mine.Contains(n.Id),
                    FadesAtMs = n.FadesAt?.ToUnixTimeMilliseconds() ?? 0
                }).ToArray();

        private async Task NodeWriteLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                var batch = new Dictionary<string, WorldNode>(StringComparer.Ordinal);
                while (_nodeWrites.TryDequeue(out var n))
                    batch[n.Id] = n;
                if (batch.Count == 0)
                    continue;
                try
                {
                    await _store.SaveNodesAsync(batch.Values, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "World node write failed.");
                }
            }
        }

        internal WorldNode? NodeForTests(string id)
        {
            lock (_gate)
                return _nodes.TryGetValue(id, out var n) ? n.Copy() : null;
        }

        internal List<WorldNode> NodesForTests()
        {
            lock (_gate)
                return _nodes.Values.Select(n => n.Copy()).ToList();
        }

        private GameCatalog Catalog => _runtime.Current.Catalog;
    }
}
