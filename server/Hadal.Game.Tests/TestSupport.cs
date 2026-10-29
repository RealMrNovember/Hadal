using Hadal.Game;
using Hadal.Game.Persistence;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    internal sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => UtcNow += by;
    }

    internal sealed class RecordingConnection : IClientConnection
    {
        public string ConnectionId { get; } = Guid.NewGuid().ToString("N");
        public List<ServerMessage> Messages { get; } = new();
        public string? ClosedReason { get; private set; }

        public void Send(ServerMessage message) => Messages.Add(message);

        public void Close(string reason) => ClosedReason = reason;
    }

    internal static class TestSupport
    {
        public static readonly ProtobufNetNetworkSerializer Serializer = new();

        private static string CatalogPath => Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json");

        /// <summary>
        /// The shipped catalog without its balance tables (catalog v9 levels, copy unlocks, reactor
        /// prerequisites, reactor caps): the engine's mechanics tests run on the formula balance, so they do
        /// not change every time the design guide retunes a number. <see cref="BalanceCatalog"/> is the real one.
        /// </summary>
        public static GameCatalog Catalog { get; } = CatalogLoader.Parse(FormulaCatalogJson());

        /// <summary>The shipped catalog as it is, balance tables included (BalanceTableTests).</summary>
        public static GameCatalog BalanceCatalog { get; } = CatalogLoader.Load(CatalogPath);

        private static string FormulaCatalogJson()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(CatalogPath))!.AsObject();
            root.Remove("reactorRequirements");
            foreach (var building in root["buildings"]!.AsArray())
            {
                building!.AsObject().Remove("levels");
                building.AsObject().Remove("countUnlocks");
            }

            foreach (var resource in root["resources"]!.AsArray())
                resource!.AsObject().Remove("capacityByReactor");

            // Without belts the map keeps the old centre spiral and has no resource nodes.
            root["world"]!.AsObject().Remove("belts");

            // The mechanics tests keep the base layout they were written for: four open rings, ring 5 at reactor 3
            // (ring 7+ behind a Mega Dome), one builder, gathering at full yield. The live layout is checked against
            // BalanceCatalog (BuildingLimitTests).
            root["grid"]!["startingUnlockedRings"] = 4;
            root["progression"]!["builderSlots"] = 1;
            root["gather"]!.AsObject().Remove("yieldScale");
            var rings = new System.Text.Json.Nodes.JsonArray();
            foreach (var (ring, reactor, titanium, biomass) in new[] { (5, 3, 400, 100), (6, 4, 800, 200), (7, 6, 1500, 400), (8, 7, 2500, 700),
                         (9, 8, 4000, 1100), (10, 9, 6000, 1600), (11, 10, 8000, 2200), (12, 10, 9500, 2800) })
            {
                var unlock = new System.Text.Json.Nodes.JsonObject
                {
                    ["ring"] = ring, ["reactorLevel"] = reactor,
                    ["cost"] = new System.Text.Json.Nodes.JsonArray(
                        new System.Text.Json.Nodes.JsonObject { ["resource"] = "Titanium", ["amount"] = titanium },
                        new System.Text.Json.Nodes.JsonObject { ["resource"] = "Biomass", ["amount"] = biomass })
                };
                if (ring >= 7)
                    unlock["requiresBuilding"] = "mega_dome";
                rings.Add(unlock);
            }

            root["ringUnlocks"] = rings;
            return root.ToJsonString();
        }

        public static GameWorld World(FakeClock clock, IPlayerRepository? repository = null) =>
            new(Catalog, repository ?? new InMemoryPlayerRepository(), clock, Serializer, NullLogger<GameWorld>.Instance);

        public static StateSnapshot Snapshot(this ServerMessage message)
        {
            Assert.NotNull(message.Snapshot);
            return Serializer.Deserialize<StateSnapshot>(message.Snapshot!.Payload);
        }

        public static StateDelta Delta(this ServerMessage message)
        {
            Assert.NotNull(message.Delta);
            return Serializer.Deserialize<StateDelta>(message.Delta!.Payload);
        }

        public static CommandAckDto? Ack(this StateDelta delta) =>
            delta.Changes.Where(c => c.EntityId.StartsWith(EntityIds.Command, StringComparison.Ordinal))
                .Select(c => Serializer.Deserialize<CommandAckDto>(c.Payload))
                .SingleOrDefault();

        public static ResourceStateDto? Wallet(this StateDelta delta) =>
            delta.Changes.Where(c => c.EntityId == EntityIds.Wallet)
                .Select(c => Serializer.Deserialize<ResourceStateDto>(c.Payload))
                .SingleOrDefault();

        public static IEnumerable<BuildingStateDto> CreatedBuildings(this StateDelta delta) =>
            delta.Changes.Where(c => c.EntityId.StartsWith(EntityIds.Building, StringComparison.Ordinal) && c.Kind == EntityChangeKind.Create)
                .Select(c => Serializer.Deserialize<BuildingStateDto>(c.Payload));

        public static IEnumerable<BuildingStateDto> UpdatedBuildings(this StateDelta delta) =>
            delta.Changes.Where(c => c.EntityId.StartsWith(EntityIds.Building, StringComparison.Ordinal) && c.Kind == EntityChangeKind.Update)
                .Select(c => Serializer.Deserialize<BuildingStateDto>(c.Payload));

        public static CommandEnvelope Upgrade(string buildingEntityId, string commandId = "")
        {
            var command = new UpgradeBuildingCommand
            {
                CommandId = string.IsNullOrEmpty(commandId) ? Guid.NewGuid().ToString("N") : commandId,
                ClientSequence = 1,
                BuildingEntityId = buildingEntityId
            };
            return EnvelopeBuilder.WrapCommand(command, CommandType.UpgradeBuilding, Serializer.Serialize(command));
        }

        public static CommandEnvelope Finish(string buildingEntityId, string commandId = "")
        {
            var command = new FinishConstructionCommand
            {
                CommandId = string.IsNullOrEmpty(commandId) ? Guid.NewGuid().ToString("N") : commandId,
                ClientSequence = 1,
                BuildingEntityId = buildingEntityId
            };
            return EnvelopeBuilder.WrapCommand(command, CommandType.FinishConstruction, Serializer.Serialize(command));
        }

        /// <summary>Places a building and finishes its (free, short) first construction; returns its entity id.</summary>
        public static string PlaceBuilt(this GameWorld world, PlayerSession session, RecordingConnection connection,
            string buildingId, int ring, int sector)
        {
            world.HandleCommand(session, Place(buildingId, ring, sector));
            var created = Assert.Single(connection.Messages.Last().Delta().CreatedBuildings());
            world.HandleCommand(session, Finish(created.EntityId));
            Assert.Equal(CommandResultCode.Accepted, connection.Messages.Last().Delta().Ack()!.Result);
            return created.EntityId;
        }

        public static CommandEnvelope Place(string buildingId, int ring, int sector, string commandId = "")
        {
            var command = new PlaceBuildingCommand
            {
                CommandId = string.IsNullOrEmpty(commandId) ? Guid.NewGuid().ToString("N") : commandId,
                ClientSequence = 1,
                BuildingDefinitionId = buildingId,
                RingIndex = ring,
                SectorIndex = sector
            };

            return EnvelopeBuilder.WrapCommand(command, CommandType.PlaceBuilding, Serializer.Serialize(command));
        }

        public static long Amount(this ResourceStateDto wallet, ResourceType type) =>
            wallet.Amounts.TryGetValue(type, out var amount) ? amount : 0;
    }
}
