using System.Collections.Concurrent;
using System.Threading.Channels;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hadal.Game.World
{
    /// <summary>A base on the world map, as last seen (live sessions override name, level and power).</summary>
    public sealed record WorldBaseRecord(string PlayerId, int X, int Y, string Name, int Level, long Power, string Avatar = "");

    public partial interface IWorldStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task<List<WorldBaseRecord>> LoadAsync(CancellationToken cancellationToken);

        Task SaveAsync(WorldBaseRecord record, CancellationToken cancellationToken);

        Task DeleteAsync(string playerId, CancellationToken cancellationToken);

        /// <summary>Players who have a base but no map tile yet (existing players when the map arrived); X and Y are unset.</summary>
        Task<List<WorldBaseRecord>> LoadUnplacedAsync(CancellationToken cancellationToken);
    }

    public sealed partial class InMemoryWorldStore : IWorldStore
    {
        private readonly ConcurrentDictionary<string, WorldBaseRecord> _bases = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<List<WorldBaseRecord>> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_bases.Values.ToList());

        public Task SaveAsync(WorldBaseRecord record, CancellationToken cancellationToken)
        {
            _bases[record.PlayerId] = record;
            return Task.CompletedTask;
        }

        public Task<List<WorldBaseRecord>> LoadUnplacedAsync(CancellationToken cancellationToken) => Task.FromResult(new List<WorldBaseRecord>());

        public Task DeleteAsync(string playerId, CancellationToken cancellationToken)
        {
            _bases.TryRemove(playerId, out _);
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresWorldStore : IWorldStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;
CREATE TABLE IF NOT EXISTS hadal.world_bases (
    player_id  text    PRIMARY KEY,
    x          integer NOT NULL,
    y          integer NOT NULL,
    name       text    NOT NULL DEFAULT '',
    level      integer NOT NULL DEFAULT 1,
    power      bigint  NOT NULL DEFAULT 0,
    UNIQUE (x, y)
);
ALTER TABLE hadal.world_bases ADD COLUMN IF NOT EXISTS avatar text NOT NULL DEFAULT '';";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresWorldStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);
            await InitializeNodesAsync(cancellationToken);
        }

        public async Task<List<WorldBaseRecord>> LoadAsync(CancellationToken cancellationToken)
        {
            var result = new List<WorldBaseRecord>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT player_id, x, y, name, level, power, avatar FROM hadal.world_bases", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new WorldBaseRecord(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), reader.GetInt32(4), reader.GetInt64(5),
                    reader.GetString(6)));
            return result;
        }

        public async Task<List<WorldBaseRecord>> LoadUnplacedAsync(CancellationToken cancellationToken)
        {
            var result = new List<WorldBaseRecord>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
SELECT p.player_id, a.account_id, coalesce(a.display_name, ''),
       coalesce(max(b.level) FILTER (WHERE b.definition_id = 'core_reactor'), 1),
       coalesce(sum(b.level), 0) * 10
FROM hadal.players p
LEFT JOIN hadal.accounts a ON replace(a.account_id::text, '-', '') = p.identity_key
LEFT JOIN hadal.buildings b ON b.player_id = p.player_id
WHERE replace(p.player_id::text, '-', '') NOT IN (SELECT player_id FROM hadal.world_bases)
GROUP BY p.player_id, a.account_id, a.display_name", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var name = reader.GetString(2);
                if (string.IsNullOrWhiteSpace(name))
                    name = reader.IsDBNull(1) ? "Diver" : "Diver-" + (BitConverter.ToUInt32(reader.GetGuid(1).ToByteArray(), 0) % 10000).ToString("D4");
                result.Add(new WorldBaseRecord(reader.GetGuid(0).ToString("N"), -1, -1, name, reader.GetInt32(3), reader.GetInt64(4)));
            }

            return result;
        }

        public async Task DeleteAsync(string playerId, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM hadal.world_bases WHERE player_id = @p", connection);
            command.Parameters.AddWithValue("p", playerId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SaveAsync(WorldBaseRecord r, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.world_bases (player_id, x, y, name, level, power, avatar) VALUES (@p, @x, @y, @n, @l, @w, @a)
ON CONFLICT (player_id) DO UPDATE SET x = EXCLUDED.x, y = EXCLUDED.y, name = EXCLUDED.name, level = EXCLUDED.level, power = EXCLUDED.power,
    avatar = EXCLUDED.avatar", connection);
            command.Parameters.AddWithValue("p", r.PlayerId);
            command.Parameters.AddWithValue("x", r.X);
            command.Parameters.AddWithValue("y", r.Y);
            command.Parameters.AddWithValue("n", r.Name);
            command.Parameters.AddWithValue("l", r.Level);
            command.Parameters.AddWithValue("w", r.Power);
            command.Parameters.AddWithValue("a", r.Avatar ?? string.Empty);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// The outer world map (docs/28_World_Map.md): every base has a fixed tile; players look at an area and
    /// get the bases in it with name, alliance tag, level and power. Lock order: world, then hub, then session.
    /// </summary>
    public sealed partial class WorldService
    {
        private readonly object _gate = new();
        private readonly IWorldStore _store;
        private readonly CatalogRuntime _runtime;
        private readonly INetworkSerializer _serializer;
        private readonly SocialHub _social;
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private bool _nodesReady;
        private readonly Dictionary<string, WorldBaseRecord> _bases = new(StringComparer.Ordinal);
        private readonly HashSet<(int, int)> _taken = new();
        private readonly ConcurrentDictionary<string, string> _baseNames = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _baseAvatars = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerSession> _online = new(StringComparer.Ordinal);
        private readonly Channel<WorldBaseRecord> _writes = Channel.CreateUnbounded<WorldBaseRecord>(new UnboundedChannelOptions { SingleReader = true });

        public WorldService(IWorldStore store, CatalogRuntime runtime, INetworkSerializer serializer, SocialHub social, ILogger logger, IClock? clock = null)
        {
            _clock = clock ?? new SystemClock();
            _store = store;
            _runtime = runtime;
            _serializer = serializer;
            _social = social;
            _logger = logger;
            _ = Task.Run(WriteLoopAsync);
            _ = Task.Run(NodeWriteLoopAsync);
        }

        public static bool Handles(CommandType type) => type is CommandType.ViewWorld or CommandType.MarchGather or CommandType.RecallMarch
            or CommandType.ViewProfile;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await _store.InitializeAsync(cancellationToken);
            var bases = await _store.LoadAsync(cancellationToken);
            lock (_gate)
            {
                foreach (var b in bases)
                {
                    _bases[b.PlayerId] = b;
                    _baseNames[b.PlayerId] = b.Name;
                    _baseAvatars[b.PlayerId] = b.Avatar;
                    _taken.Add((b.X, b.Y));
                }
            }

            // Players from before the map existed get their tiles now, so the map is not empty.
            var unplaced = await _store.LoadUnplacedAsync(cancellationToken);
            lock (_gate)
            {
                foreach (var u in unplaced.Where(u => !_bases.ContainsKey(u.PlayerId)))
                {
                    var (x, y) = PlaceBase();
                    var record = u with { X = x, Y = y };
                    _bases[record.PlayerId] = record;
                    _baseNames[record.PlayerId] = record.Name;
                    _taken.Add((x, y));
                    _writes.Writer.TryWrite(record);
                }
            }

            if (unplaced.Count > 0)
                _logger.LogInformation("World map: placed {Count} existing players.", unplaced.Count);

            // Bases from the spiral era that stand where bases may not (the trench floor, the abyssal plain)
            // move once to the Hadal Threshold (docs/rehber K-08).
            lock (_gate)
            {
                var world = _runtime.Current.Catalog.World;
                var moved = 0;
                foreach (var b in _bases.Values.Where(b => GatherRules.BeltOf(world, b.X, b.Y) is { bases: false }).ToList())
                {
                    _taken.Remove((b.X, b.Y));
                    var (x, y) = PlaceBase();
                    var record = b with { X = x, Y = y };
                    _bases[b.PlayerId] = record;
                    _baseNames[b.PlayerId] = record.Name;
                    _taken.Add((x, y));
                    _writes.Writer.TryWrite(record);
                    moved++;
                }

                if (moved > 0)
                    _logger.LogInformation("World map: moved {Count} bases to the Hadal Threshold.", moved);
            }

            await InitializeNodesAsync(cancellationToken, _clock.UtcNow);
            lock (_gate)
                _nodesReady = true;

            _logger.LogInformation("World map: {Bases} bases.", bases.Count);
        }

        /// <summary>Gives a new player a tile and refreshes what others see of this one.</summary>
        public void OnConnected(PlayerSession session)
        {
            if (session.IsDeleted)
                return;
            lock (_gate)
            {
                _online[session.PublicId] = session;
                Upsert(session);
            }
        }

        public void OnDisconnected(PlayerSession session)
        {
            if (session.IsDeleted)
                return;
            lock (_gate)
            {
                if (_online.TryGetValue(session.PublicId, out var current) && ReferenceEquals(current, session))
                    _online.Remove(session.PublicId);
                Upsert(session);
            }
        }

        /// <summary>Takes a removed player's base off the map and frees the nodes their marches held.</summary>
        public async Task ForgetAsync(string playerId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_bases.Remove(playerId, out var b))
                    _taken.Remove((b.X, b.Y));
                _baseNames.TryRemove(playerId, out _);
                _online.Remove(playerId);
                ForgetMarches(playerId, now);
            }

            await _store.DeleteAsync(playerId, cancellationToken);
        }

        /// <summary>The name a base was last seen with; null when the player has no base.</summary>
        /// <summary>A base's avatar, lock-free like <see cref="NameOf"/>.</summary>
        public string? AvatarOf(string playerId) => _baseAvatars.TryGetValue(playerId, out var a) ? a : null;

        /// <summary>A profile card: what the map knows of the base plus the alliance (lock order world, then hub).</summary>
        private void HandleProfile(PlayerSession session, ViewProfileCommand command, DateTimeOffset now)
        {
            PlayerProfileDto? card = null;
            lock (_gate)
            {
                if (_bases.TryGetValue(command.PlayerId ?? string.Empty, out var b))
                {
                    var online = _online.TryGetValue(b.PlayerId, out var live);
                    var (tag, name) = _social.AllianceOfPlayer(b.PlayerId);
                    card = new PlayerProfileDto
                    {
                        PlayerId = b.PlayerId,
                        Name = online ? live!.DisplayName : b.Name,
                        AvatarId = online ? live!.AvatarId : b.Avatar,
                        Level = online ? live!.BaseLevel : b.Level,
                        Power = online ? live!.Power : b.Power,
                        AllianceTag = tag,
                        AllianceName = name,
                        X = b.X,
                        Y = b.Y,
                        Online = online
                    };
                }
            }

            if (card == null)
            {
                session.Acknowledge(command.CommandId, command.ClientSequence, "UnknownPlayer", now);
                return;
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, null, now, new List<EntityChangeDto>
            {
                new() { EntityId = EntityIds.Profile, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(card) }
            });
        }

        /// <remarks>Lock-free (the social hub asks while holding its own lock).</remarks>
        public string? NameOf(string playerId) => _baseNames.TryGetValue(playerId, out var name) ? name : null;

        public (int X, int Y)? PositionOf(string playerId)
        {
            lock (_gate)
                return _bases.TryGetValue(playerId, out var b) ? (b.X, b.Y) : null;
        }

        public void Handle(PlayerSession session, CommandEnvelope envelope, DateTimeOffset now)
        {
            lock (_gate)
            {
                // Tests and fresh in-memory worlds skip InitializeAsync; seed on first use.
                if (!_nodesReady)
                {
                    SeedNodes(now);
                    _nodesReady = true;
                }
            }

            switch (envelope.Type)
            {
                case CommandType.MarchGather when _serializer.TryDeserialize(envelope.Payload, out MarchGatherCommand? gather) && gather != null:
                    HandleGather(session, gather, now);
                    return;
                case CommandType.RecallMarch when _serializer.TryDeserialize(envelope.Payload, out RecallMarchCommand? recall) && recall != null:
                    HandleRecall(session, recall, now);
                    return;
                case CommandType.ViewProfile when _serializer.TryDeserialize(envelope.Payload, out ViewProfileCommand? profile) && profile != null:
                    HandleProfile(session, profile, now);
                    return;
                case CommandType.MarchGather or CommandType.RecallMarch or CommandType.ViewProfile:
                    session.Acknowledge(envelope.CommandId, envelope.ClientSequence, "MalformedCommand", now);
                    return;
            }

            if (!_serializer.TryDeserialize(envelope.Payload, out ViewWorldCommand? command) || command == null)
            {
                session.Acknowledge(envelope.CommandId, envelope.ClientSequence, "MalformedCommand", now);
                return;
            }

            WorldViewDto view;
            var mine = session.MarchNodes();
            lock (_gate)
            {
                MaintainNodes(now);
                var me = Upsert(session);
                var rules = _runtime.Current.Catalog.World;
                var radius = Math.Max(4, rules.viewRadius);
                var size = Math.Max(10, rules.size);
                var cx = command.X < 0 || command.Y < 0 ? me.X : Math.Clamp(command.X, 0, size - 1);
                var cy = command.X < 0 || command.Y < 0 ? me.Y : Math.Clamp(command.Y, 0, size - 1);

                view = new WorldViewDto
                {
                    CenterX = cx,
                    CenterY = cy,
                    Radius = radius,
                    MapSize = size,
                    MyX = me.X,
                    MyY = me.Y,
                    Bases = _bases.Values.Where(b => WorldRules.InView(b.X, b.Y, cx, cy, radius)).Select(b =>
                    {
                        var online = _online.TryGetValue(b.PlayerId, out var live);
                        return new WorldBaseDto
                        {
                            PlayerId = b.PlayerId,
                            Name = online ? live!.DisplayName : b.Name,
                            AllianceTag = _social.TagOf(b.PlayerId),
                            X = b.X,
                            Y = b.Y,
                            Level = online ? live!.BaseLevel : b.Level,
                            Power = online ? live!.Power : b.Power,
                            Online = online,
                            AvatarId = online ? live!.AvatarId : b.Avatar
                        };
                    }).ToArray(),
                    Nodes = NodesInView(cx, cy, radius, mine, now)
                };
            }

            session.Acknowledge(command.CommandId, command.ClientSequence, null, now, new List<EntityChangeDto>
            {
                new() { EntityId = EntityIds.WorldView, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(view) }
            });
        }

        /// <summary>Caller holds the lock.</summary>
        private WorldBaseRecord Upsert(PlayerSession session)
        {
            var id = session.PublicId;
            var (x, y) = _bases.TryGetValue(id, out var existing)
                ? (existing.X, existing.Y)
                : PlaceBase();
            var record = new WorldBaseRecord(id, x, y, session.DisplayName, session.BaseLevel, session.Power, session.AvatarId);
            if (existing == null)
                _taken.Add((x, y));
            if (existing != record)
            {
                _bases[id] = record;
                _baseNames[id] = record.Name;
                _baseAvatars[id] = record.Avatar;
                _writes.Writer.TryWrite(record);
            }

            return record;
        }

        /// <summary>A tile for a new base: the Hadal Threshold when the catalog has belts, else the old spiral. Caller holds the lock.</summary>
        private (int X, int Y) PlaceBase() =>
            WorldRules.PlaceInBelt(_runtime.Current.Catalog.World, (tx, ty) => _taken.Contains((tx, ty)), (tx, ty) => _nodeAt.ContainsKey((tx, ty)), _random);

        private async Task WriteLoopAsync()
        {
            await foreach (var record in _writes.Reader.ReadAllAsync())
            {
                try
                {
                    await _store.SaveAsync(record, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "World store write failed.");
                }
            }
        }
    }
}
