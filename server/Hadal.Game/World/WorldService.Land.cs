using System.Collections.Concurrent;
using System.Text.Json;
using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hadal.Game.World
{
    /// <summary>An alliance hub or beacon (docs/32 §2.1–2.2). Mutated only under the world lock.</summary>
    public sealed class AllianceStructure
    {
        public string Id { get; set; } = string.Empty;
        public string WorldId { get; set; } = AllianceLandRules.DefaultWorld;
        public string AllianceId { get; set; } = string.Empty;
        public string Kind { get; set; } = AllianceLandRules.Beacon;
        public int X { get; set; }
        public int Y { get; set; }
        public int Level { get; set; } = 1;

        /// <summary>Hub donations towards the next level.</summary>
        public long Points { get; set; }

        /// <summary>A beacon claims land from then on.</summary>
        public DateTimeOffset ReadyAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? MovedAt { get; set; }
        public string PlacedBy { get; set; } = string.Empty;

        /// <summary>An alliance building (docs/32 §2.3): work put in by members' teams until it stands.</summary>
        public double Work { get; set; }
        public bool Built { get; set; } = true;
        public DateTimeOffset WorkAt { get; set; }

        /// <summary>Durability as of <see cref="DurabilityAt"/>; a building at 0 is damaged and gives no bonus.</summary>
        public double Durability { get; set; } = -1;
        public DateTimeOffset DurabilityAt { get; set; }
    }

    public partial interface IWorldStore
    {
        Task<List<AllianceStructure>> LoadStructuresAsync(CancellationToken cancellationToken);

        /// <summary>Saves (or with a null structure, deletes) an alliance structure.</summary>
        Task SaveStructureAsync(string id, AllianceStructure? structure, CancellationToken cancellationToken);
    }

    public sealed partial class InMemoryWorldStore
    {
        private readonly ConcurrentDictionary<string, string> _structures = new(StringComparer.Ordinal);

        public Task<List<AllianceStructure>> LoadStructuresAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_structures.Values.Select(j => JsonSerializer.Deserialize<AllianceStructure>(j)!).ToList());

        public Task SaveStructureAsync(string id, AllianceStructure? structure, CancellationToken cancellationToken)
        {
            if (structure == null)
                _structures.TryRemove(id, out _);
            else
                _structures[id] = JsonSerializer.Serialize(structure);
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresWorldStore
    {
        // World ids on every world table (docs/32 §2.7): one world today, several later.
        private const string StructureSchema = @"
CREATE TABLE IF NOT EXISTS hadal.world_structures (
    id          text PRIMARY KEY,
    world_id    text NOT NULL DEFAULT 'w1',
    alliance_id text NOT NULL,
    doc         text NOT NULL
);
ALTER TABLE hadal.world_bases   ADD COLUMN IF NOT EXISTS world_id text NOT NULL DEFAULT 'w1';
ALTER TABLE hadal.world_nodes   ADD COLUMN IF NOT EXISTS world_id text NOT NULL DEFAULT 'w1';
ALTER TABLE hadal.world_pvp     ADD COLUMN IF NOT EXISTS world_id text NOT NULL DEFAULT 'w1';
ALTER TABLE hadal.world_regions ADD COLUMN IF NOT EXISTS world_id text NOT NULL DEFAULT 'w1';";

        private Task InitializeStructuresAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, StructureSchema, cancellationToken);

        public async Task<List<AllianceStructure>> LoadStructuresAsync(CancellationToken cancellationToken)
        {
            var result = new List<AllianceStructure>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT doc FROM hadal.world_structures WHERE world_id = @world", connection);
            command.Parameters.AddWithValue("world", _world);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                if (JsonSerializer.Deserialize<AllianceStructure>(reader.GetString(0)) is { } s)
                    result.Add(s);
            return result;
        }

        public async Task SaveStructureAsync(string id, AllianceStructure? structure, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = structure == null
                ? new NpgsqlCommand("DELETE FROM hadal.world_structures WHERE id = @i", connection)
                : new NpgsqlCommand(@"INSERT INTO hadal.world_structures (id, world_id, alliance_id, doc) VALUES (@i, @w, @a, @d)
ON CONFLICT (id) DO UPDATE SET world_id = EXCLUDED.world_id, alliance_id = EXCLUDED.alliance_id, doc = EXCLUDED.doc", connection);
            command.Parameters.AddWithValue("i", id);
            if (structure != null)
            {
                command.Parameters.AddWithValue("w", structure.WorldId);
                command.Parameters.AddWithValue("a", structure.AllianceId);
                command.Parameters.AddWithValue("d", JsonSerializer.Serialize(structure));
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Alliance land (docs/32): the hub claims a square that grows with its level, built beacons connected to it claim
    /// more; tiles go to the first claimer. The land drives the rally point, the alliance teleport, the gathering bonus
    /// and which cities are neighbours.
    /// </summary>
    public sealed partial class WorldService
    {
        /// <summary>This map's kingdom (docs/39); "w1" is Kingdom #1.</summary>
        public string WorldId { get; }

        private readonly Dictionary<string, AllianceStructure> _structures = new(StringComparer.Ordinal);
        private readonly Dictionary<(int, int), AllianceStructure> _structureAt = new();
        private readonly Dictionary<(int, int), string> _land = new();
        private readonly HashSet<string> _cut = new(StringComparer.Ordinal);
        private readonly ConcurrentQueue<(string Id, AllianceStructure? Structure)> _structureWrites = new();
        private DateTimeOffset _landValidUntil = DateTimeOffset.MinValue;

        private async Task LoadStructuresAsync(CancellationToken cancellationToken)
        {
            var stored = await _store.LoadStructuresAsync(cancellationToken);
            lock (_gate)
            {
                foreach (var s in stored.Where(s => s.WorldId == WorldId))
                {
                    _structures[s.Id] = s;
                    _structureAt[(s.X, s.Y)] = s;
                }

                _landValidUntil = DateTimeOffset.MinValue;
            }

            _ = Task.Run(StructureWriteLoopAsync);
        }

        private async Task StructureWriteLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                var batch = new Dictionary<string, AllianceStructure?>(StringComparer.Ordinal);
                while (_structureWrites.TryDequeue(out var w))
                    batch[w.Id] = w.Structure;
                foreach (var pair in batch)
                {
                    try
                    {
                        await _store.SaveStructureAsync(pair.Key, pair.Value, CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "World structure write failed.");
                    }
                }
            }
        }

        private void SaveStructure(AllianceStructure s)
        {
            _structureWrites.Enqueue((s.Id, JsonSerializer.Deserialize<AllianceStructure>(JsonSerializer.Serialize(s))));
            _landValidUntil = DateTimeOffset.MinValue;
        }

        private void DropStructure(AllianceStructure s)
        {
            _structures.Remove(s.Id);
            _structureAt.Remove((s.X, s.Y));
            _structureWrites.Enqueue((s.Id, null));
            _landValidUntil = DateTimeOffset.MinValue;
        }

        private AllianceStructure? HubOf(string allianceId) =>
            allianceId.Length == 0 ? null : _structures.Values.FirstOrDefault(s => s.AllianceId == allianceId && s.Kind == AllianceLandRules.Hub);

        private int BeaconsOf(string allianceId) => _structures.Values.Count(s => s.AllianceId == allianceId && s.Kind == AllianceLandRules.Beacon);

        /// <summary>
        /// Brings the land up to date: dissolved alliances lose their structures; per alliance the hub and the built beacons
        /// connected to it claim their squares, oldest first. Caller holds the world lock.
        /// </summary>
        private void EnsureLand(DateTimeOffset now)
        {
            foreach (var s in _structures.Values.Where(s => _social.MemberCount(s.AllianceId) == 0).ToList())
                DropStructure(s);
            if (now < _landValidUntil)
                return;

            _land.Clear();
            _cut.Clear();
            var size = MapSize;
            var claiming = new List<AllianceStructure>();
            foreach (var group in _structures.Values.GroupBy(s => s.AllianceId))
            {
                var hub = group.FirstOrDefault(s => s.Kind == AllianceLandRules.Hub);
                var beacons = group.Where(s => s.Kind == AllianceLandRules.Beacon && s.ReadyAt <= now).ToList();
                if (hub == null)
                {
                    foreach (var b in group)
                        _cut.Add(b.Id);
                    continue;
                }

                var connected = new List<AllianceStructure> { hub };
                bool grew;
                do
                {
                    grew = false;
                    foreach (var b in beacons.Where(b => !connected.Contains(b)).ToList())
                    {
                        if (!connected.Any(c => AllianceLandRules.Touch(c.X, c.Y, AllianceLandRules.Radius(c.Kind, c.Level), b.X, b.Y, AllianceLandRules.BeaconRadius)))
                            continue;
                        connected.Add(b);
                        grew = true;
                    }
                } while (grew);

                foreach (var b in beacons.Where(b => !connected.Contains(b)))
                    _cut.Add(b.Id);
                claiming.AddRange(connected);
            }

            foreach (var s in claiming.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id, StringComparer.Ordinal))
            {
                var r = AllianceLandRules.Radius(s.Kind, s.Level);
                for (var x = Math.Max(0, s.X - r); x <= Math.Min(size - 1, s.X + r); x++)
                    for (var y = Math.Max(0, s.Y - r); y <= Math.Min(size - 1, s.Y + r); y++)
                        _land.TryAdd((x, y), s.AllianceId);
            }

            // Valid until the next beacon finishes.
            _landValidUntil = _structures.Values.Where(s => s.ReadyAt > now).Select(s => s.ReadyAt).DefaultIfEmpty(DateTimeOffset.MaxValue).Min();
        }

        private string LandOwner(int x, int y) => _land.TryGetValue((x, y), out var a) ? a : string.Empty;

        /// <summary>Is any tile within <paramref name="reach"/> of (x, y) the alliance's land?</summary>
        private bool NearLand(string allianceId, int x, int y, int reach)
        {
            for (var dx = -reach; dx <= reach; dx++)
                for (var dy = -reach; dy <= reach; dy++)
                    if (LandOwner(x + dx, y + dy) == allianceId)
                        return true;
            return false;
        }

        /// <summary>The territory bonus a player's gathering gets on (x, y): +25 % on their alliance's land.</summary>
        private double GatherBonus(string playerId, int x, int y)
        {
            EnsureLand(_clock.UtcNow);
            var alliance = _social.AllianceIdOf(playerId);
            if (alliance.Length == 0)
                return 0;
            // Every city the alliance holds adds its City Bonus anywhere (Last Asylum's +5 % each, docs/32 §2.4).
            var cities = Regions.Count(r => r.AllianceId == alliance) * AllianceLandRules.CityGatherPercent / 100.0;
            return LandOwner(x, y) == alliance
                ? TerritoryRules.GatherBonus + LandBonus(alliance, AllianceLandRules.Rig, x, y, _clock.UtcNow) / 100.0 + cities
                : cities;
        }

        /// <summary>A free tile on the alliance's land nearest its hub (alliance teleport). Caller holds the lock.</summary>
        private (int X, int Y)? TerritoryTile(string allianceId)
        {
            EnsureLand(_clock.UtcNow);
            if (HubOf(allianceId) is not { } hub)
                return null;
            return NearestFree(hub.X, hub.Y, AllianceLandRules.HubRadius(AllianceLandRules.MaxHubLevel) + 20, MapSize, (x, y) => LandOwner(x, y) == allianceId);
        }

        /// <summary>Why the alliance cannot take this city now; null when it can (checked at launch and at the battle).</summary>
        private string? CaptureBlock(WorldRegion city, string alliance, DateTimeOffset now)
        {
            if (alliance.Length == 0)
                return "NotInAlliance";
            if (city.Level >= TerritoryRules.ThroneLevel)
                return "Throne";
            if (city.AllianceId == alliance)
                return "Ally";
            EnsureLand(now);
            if (HubOf(alliance) == null)
                return "NoHub";
            if (Regions.Count(r => r.AllianceId == alliance) >= AllianceLandRules.MaxCities(_social.MemberCount(alliance)))
                return "TooManyCities";
            return NearLand(alliance, city.X, city.Y, AllianceLandRules.CityReach) ? null : "CityTooFar";
        }

        // --- Commands ----------------------------------------------------------------------------

        private void HandleTerritory(PlayerSession session, TerritoryCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                MaintainRegions();
                EnsureLand(now);
                AdvanceBuildings(now);
                var alliance = _social.AllianceIdOf(session.PublicId);
                var rank = _social.RankOf(session.PublicId);
                // Hub, beacons and buildings: the leader and logistics officers (docs/29 §2.3).
                var builder = AlliancePermissions.CanBuild(rank, _social.RoleOf(session.PublicId));
                var reason = alliance.Length == 0 ? "NotInAlliance" : null;
                if (reason == null)
                {
                    reason = command.Action switch
                    {
                        AllianceLandRules.PlaceHub => PlaceHub(session, alliance, builder, command.X, command.Y, now),
                        AllianceLandRules.PlaceBeacon => PlaceBeacon(session, alliance, builder, command.X, command.Y, now),
                        AllianceLandRules.MoveHub => MoveHub(alliance, rank, command.X, command.Y, now),
                        AllianceLandRules.Remove => RemoveBeacon(alliance, builder, command.StrongholdId),
                        AllianceLandRules.Donate => Donate(session, alliance, command.StrongholdId, command.Resource, command.Amount, now),
                        AllianceLandRules.PlaceBuilding => PlaceBuilding(session, alliance, builder, command.Resource, command.X, command.Y, now),
                        AllianceLandRules.AbandonCity => AbandonCity(alliance, rank, command.StrongholdId),
                        _ => "MalformedCommand"
                    };
                }

                if (reason == null)
                    EnsureLand(now);
                session.Acknowledge(command.CommandId, command.ClientSequence, reason, now,
                    reason == null ? new List<EntityChangeDto> { TerritoryChange(session.PublicId, now) } : null);
                if (reason == null)
                    _logger.LogInformation("Player {PlayerId} land {Action} {Target} {X},{Y}.", session.PublicId, command.Action, command.StrongholdId, command.X, command.Y);
            }
        }

        /// <summary>
        /// The starter buffer (docs/33 §5): the outermost belt where bases may stand is where new bases land; no alliance
        /// hub, beacon or building goes up there.
        /// </summary>
        private bool InStarterBelt(int x, int y)
        {
            var world = Catalog.World;
            var starter = world.belts.Where(b => b.bases).OrderByDescending(b => b.outer).FirstOrDefault();
            return starter != null && GatherRules.BeltOf(world, x, y) == starter;
        }

        /// <summary>A free tile where bases may stand and nothing else is. Caller holds the lock.</summary>
        private bool FreeForStructure(int x, int y) =>
            x >= 0 && y >= 0 && x < MapSize && y < MapSize && CanStand(x, y) && !_structureAt.ContainsKey((x, y));

        private string? PlaceHub(PlayerSession session, string alliance, bool builder, int x, int y, DateTimeOffset now)
        {
            if (!builder)
                return "NoPermission";
            if (HubOf(alliance) != null)
                return "HasTerritory";
            if (_social.MemberCount(alliance) < AllianceLandRules.HubMinMembers)
                return "TooFewMembers";
            if (InStarterBelt(x, y))
                return "StarterZone";
            if (!FreeForStructure(x, y))
                return "TileTaken";
            for (var dx = -AllianceLandRules.OtherAllianceGap; dx <= AllianceLandRules.OtherAllianceGap; dx++)
                for (var dy = -AllianceLandRules.OtherAllianceGap; dy <= AllianceLandRules.OtherAllianceGap; dy++)
                    if (LandOwner(x + dx, y + dy) is { Length: > 0 } other && other != alliance)
                        return "OtherLand";
            Add(new AllianceStructure
            {
                Id = "h" + Guid.NewGuid().ToString("N")[..10], AllianceId = alliance, Kind = AllianceLandRules.Hub, X = x, Y = y, Level = 1,
                ReadyAt = now, CreatedAt = now, PlacedBy = session.PublicId
            });
            _social.LogBattle(alliance, "hub_planted", now, NameOf(session.PublicId) ?? string.Empty, string.Empty, x.ToString(), y.ToString());
            return null;
        }

        private string? PlaceBeacon(PlayerSession session, string alliance, bool builder, int x, int y, DateTimeOffset now)
        {
            if (!builder)
                return "NoPermission";
            if (HubOf(alliance) is not { } hub)
                return "NoHub";
            var placed = BeaconsOf(alliance);
            if (placed >= AllianceLandRules.MaxBeacons(_social.MemberCount(alliance), hub.Level))
                return "TooManyBeacons";
            var owner = LandOwner(x, y);
            if (owner.Length > 0 && owner != alliance)
                return "OtherLand";
            if (!NearLand(alliance, x, y, AllianceLandRules.PlaceReach))
                return "LandTooFar";
            if (InStarterBelt(x, y))
                return "StarterZone";
            if (!FreeForStructure(x, y))
                return "TileTaken";
            if (!session.TryCharge(new[] { new KeyValuePair<ResourceType, long>(ResourceType.Titanium, AllianceLandRules.BeaconCost(placed)) }, now))
                return "InsufficientResources";
            Add(new AllianceStructure
            {
                Id = "b" + Guid.NewGuid().ToString("N")[..10], AllianceId = alliance, Kind = AllianceLandRules.Beacon, X = x, Y = y, Level = 1,
                ReadyAt = now.AddMinutes(AllianceLandRules.BeaconBuildMinutes), CreatedAt = now, PlacedBy = session.PublicId
            });
            return null;
        }

        private void Add(AllianceStructure s)
        {
            _structures[s.Id] = s;
            _structureAt[(s.X, s.Y)] = s;
            SaveStructure(s);
        }

        private string? MoveHub(string alliance, int rank, int x, int y, DateTimeOffset now)
        {
            if (rank < AlliancePermissions.Leader)
                return "NoPermission";
            if (HubOf(alliance) is not { } hub)
                return "NoHub";
            if (hub.MovedAt is { } moved && now - moved < AllianceLandRules.HubMoveCooldown)
                return "Cooldown";
            if (!FreeForStructure(x, y))
                return "TileTaken";
            if (LandOwner(x, y) != alliance)
                return "NotOurs";
            _structureAt.Remove((hub.X, hub.Y));
            (hub.X, hub.Y, hub.MovedAt) = (x, y, now);
            _structureAt[(x, y)] = hub;
            SaveStructure(hub);
            return null;
        }

        private string? RemoveBeacon(string alliance, bool builder, string id)
        {
            if (!builder)
                return "NoPermission";
            if (!_structures.TryGetValue(id ?? string.Empty, out var s) || s.AllianceId != alliance)
                return "NotOurs";
            if (s.Kind == AllianceLandRules.Hub)
                return "HubRegion";
            // Teams building or guarding it go home.
            foreach (var r in _pvp.Values.Where(r => r.Mode == PvpRules.Reinforce && r.TargetId == s.Id).ToList())
            {
                SessionOf(r.AttackerId)?.SendHome(r.Members[0].MarchId, _clock.UtcNow, s.X, s.Y, _clock.UtcNow);
                Drop(r);
            }

            DropStructure(s);
            return null;
        }

        /// <summary>Members donate Titanium, Biomass or Food to the hub; each unit is a point; points raise its level.</summary>
        private string? Donate(PlayerSession session, string alliance, string id, string resource, long amount, DateTimeOffset now)
        {
            if (!_structures.TryGetValue(id ?? string.Empty, out var hub) || hub.AllianceId != alliance || hub.Kind == AllianceLandRules.Beacon)
                return "NotOurs";
            if (!Enum.TryParse(resource, out ResourceType type) || !AllianceLandRules.IsDonation(type) || amount <= 0)
                return "MalformedCommand";
            if (AllianceLandRules.IsBuilding(hub.Kind))
                return DonateBuilding(session, hub, amount, type, now);
            if (hub.Level >= AllianceLandRules.MaxHubLevel)
                return "MaxLevel";
            // Never more than the hub still needs up to its top level.
            long room = 0;
            for (var l = hub.Level; l < AllianceLandRules.MaxHubLevel; l++)
                room += AllianceLandRules.HubPoints(l);
            amount = Math.Min(amount, room - hub.Points);
            if (!session.TryCharge(new[] { new KeyValuePair<ResourceType, long>(type, amount) }, now))
                return "InsufficientResources";
            hub.Points += amount;
            while (hub.Level < AllianceLandRules.MaxHubLevel && hub.Points >= AllianceLandRules.HubPoints(hub.Level))
            {
                hub.Points -= AllianceLandRules.HubPoints(hub.Level);
                hub.Level++;
                _social.LogBattle(alliance, "hub_level", now, NameOf(session.PublicId) ?? string.Empty, hub.Level.ToString(), hub.X.ToString(), hub.Y.ToString());
            }

            SaveStructure(hub);
            return null;
        }

        private string? AbandonCity(string alliance, int rank, string id)
        {
            if (rank < AlliancePermissions.Leader)
                return "NoPermission";
            if (!TerritoryRules.TryParseId(id, out var index) || index >= Regions.Length || Regions[index].AllianceId != alliance)
                return "NotOurs";
            Regions[index].AllianceId = string.Empty;
            SaveRegion(Regions[index]);
            return null;
        }

        private void HandleViewTerritory(PlayerSession session, ViewTerritoryCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                MaintainRegions();
                EnsureLand(now);
                AdvanceBuildings(now);
                Upsert(session);
                session.Acknowledge(command.CommandId, command.ClientSequence, null, now, new List<EntityChangeDto> { TerritoryChange(session.PublicId, now) });
            }
        }

        /// <summary>The strategic map for a viewer: cities, hubs and beacons, the land, every base as a dot. Caller holds the lock.</summary>
        private EntityChangeDto TerritoryChange(string viewerId, DateTimeOffset now)
        {
            var alliance = _social.AllianceIdOf(viewerId);
            var attacked = _pvp.Values.Where(p => p.TargetKind == PvpRules.TargetStronghold).Select(p => p.TargetId).ToHashSet(StringComparer.Ordinal);
            var troops = Catalog.Troops;
            string TagOfAlliance(string id) => id.Length > 0 ? _social.AllianceInfo(id)?.Tag ?? string.Empty : string.Empty;
            var landAlliances = _land.Values.Distinct(StringComparer.Ordinal).ToList();
            var landIndex = landAlliances.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i, StringComparer.Ordinal);
            var members = alliance.Length > 0 ? _social.MemberCount(alliance) : 0;
            var hub = HubOf(alliance);
            var dto = new TerritoryDto
            {
                MapSize = MapSize,
                WorldId = WorldId,
                MyAllianceId = alliance,
                Strongholds = Regions.Select(r => new StrongholdDto
                {
                    Id = r.Id, Region = r.Index, X = r.X, Y = r.Y, Level = r.Level, AllianceId = r.AllianceId, AllianceTag = TagOfAlliance(r.AllianceId),
                    Color = TerritoryRules.ColorOf(r.AllianceId), UnderAttack = attacked.Contains(r.Id),
                    Power = TroopRules.Power(troops, TerritoryRules.Garrison(r.Level, TerritoryRules.GarrisonShare(r.Share, r.ShareAt, now)).Troops
                        .ToDictionary(p => p.Key, p => (long)Math.Round(p.Value), StringComparer.Ordinal))
                }).ToArray(),
                Structures = _structures.Values.Select(s => WithBuilding(new StructureDto
                {
                    Id = s.Id, Kind = s.Kind, AllianceId = s.AllianceId, AllianceTag = TagOfAlliance(s.AllianceId), Color = TerritoryRules.ColorOf(s.AllianceId),
                    X = s.X, Y = s.Y, Level = s.Level, Points = s.Points,
                    PointsNext = s.Kind == AllianceLandRules.Hub && s.Level < AllianceLandRules.MaxHubLevel ? AllianceLandRules.HubPoints(s.Level) : 0,
                    ReadyAtMs = s.ReadyAt > now ? s.ReadyAt.ToUnixTimeMilliseconds() : 0, Cut = _cut.Contains(s.Id),
                    Radius = AllianceLandRules.Radius(s.Kind, s.Level)
                }, s, now)).ToArray(),
                Land = _land.Select(p => p.Key.Item1 * 1_000_000 + p.Key.Item2 * 1_000 + landIndex[p.Value]).ToArray(),
                LandAlliances = landAlliances.ToArray(),
                LandColors = landAlliances.Select(TerritoryRules.ColorOf).ToArray(),
                LandTags = landAlliances.Select(TagOfAlliance).ToArray(),
                Bases = _bases.Values.Select(b =>
                {
                    var relation = b.PlayerId == viewerId ? 0 : alliance.Length > 0 && _social.AllianceIdOf(b.PlayerId) == alliance ? 1 : 2;
                    return b.X * 1_000_000 + b.Y * 1_000 + relation;
                }).ToArray(),
                Held = alliance.Length == 0 ? 0 : Regions.Count(r => r.AllianceId == alliance),
                Allowed = alliance.Length == 0 ? 0 : AllianceLandRules.MaxCities(members),
                Beacons = alliance.Length == 0 ? 0 : BeaconsOf(alliance),
                BeaconsAllowed = hub == null ? 0 : AllianceLandRules.MaxBeacons(members, hub.Level),
                Members = members,
                AlliancePower = alliance.Length == 0 ? 0 : AlliancePower(alliance, viewerId)
            };
            if (_bases.TryGetValue(viewerId, out var me))
                (dto.MyX, dto.MyY) = (me.X, me.Y);

            // The rally point: the hub, else the leader's base (the alliance teleport's fallback).
            if (hub != null)
                (dto.RallyX, dto.RallyY, dto.RallyIsHub) = (hub.X, hub.Y, true);
            else if (alliance.Length > 0)
            {
                var leader = _social.RankOf(viewerId) >= AlliancePermissions.Leader ? viewerId : _social.AllianceMatesOf(viewerId).FirstOrDefault();
                if (leader != null && _bases.TryGetValue(leader, out var lb))
                    (dto.RallyX, dto.RallyY) = (lb.X, lb.Y);
            }

            return new EntityChangeDto { EntityId = EntityIds.Territory, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(dto) };
        }

        internal IReadOnlyList<AllianceStructure> StructuresForTests()
        {
            lock (_gate)
                return _structures.Values.ToList();
        }

        internal string LandOwnerForTests(int x, int y)
        {
            lock (_gate)
            {
                EnsureLand(_clock.UtcNow);
                return LandOwner(x, y);
            }
        }
    }
}
