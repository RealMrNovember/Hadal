using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Hadal.Core.StateSync;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// QA R5 / S2 / S9 and the stale-frame half of R3 / S6. The wallet is a singleton entity
    /// replicated as absolute totals: every frame is the whole wallet, frames are idempotent,
    /// and an older frame must never roll a newer balance back.
    /// </summary>
    public sealed class WalletReplicationTests
    {
        private readonly NetworkSerializationLayer _serialization = new();
        private readonly ProtobufNetNetworkSerializer _serializer = new();
        private ClientStateView _view;

        [SetUp]
        public void SetUp()
        {
            _view = new ClientStateView();
            _view.ResetSession();
        }

        [Test]
        public void SameWalletFrameTwice_IsIdempotent()
        {
            var frame = WalletDelta(10, (ResourceType.Oxygen, 500), (ResourceType.Food, 300));

            Apply(frame);
            Apply(frame);

            Assert.AreEqual(500, Balance(ResourceType.Oxygen));
            Assert.AreEqual(300, Balance(ResourceType.Food));
            Assert.AreEqual(2, _view.Data.resources.Length);
        }

        [Test]
        public void OxygenStaysOxygen_EndToEnd()
        {
            // S1: the original bug stored Shared.Oxygen (1) and read it back as Data.Energy.
            Apply(WalletDelta(1, (ResourceType.Oxygen, 500)));

            var entry = _view.Data.resources.Single();
            Assert.AreEqual(ResourceType.Oxygen, ResourceTypeMap.FromStoredValue(entry.resourceType));
            Assert.AreEqual(500, entry.amount);
        }

        [Test]
        public void WalletFrame_ReplacesWholeWallet()
        {
            // Contract, not accident: the wallet is one entity, so a frame is the full wallet.
            // Producers must always send every balance (see ProducerAlwaysSendsFullWallet).
            Apply(WalletDelta(1, (ResourceType.Oxygen, 500), (ResourceType.Food, 300), (ResourceType.Biomass, 100)));
            Apply(WalletDelta(2, (ResourceType.Oxygen, 510), (ResourceType.Food, 310), (ResourceType.Biomass, 100)));

            Assert.AreEqual(510, Balance(ResourceType.Oxygen));
            Assert.AreEqual(310, Balance(ResourceType.Food));
            Assert.AreEqual(100, Balance(ResourceType.Biomass));
        }

        [Test]
        public void ProducerAlwaysSendsFullWallet()
        {
            // S9 guard: ProductionSimulator.Advance carries untouched balances forward, so a
            // resource with no producer is never dropped from the replicated wallet.
            var totals = new Dictionary<ResourceType, long>
            {
                [ResourceType.Oxygen] = 500, [ResourceType.Food] = 300, [ResourceType.Titanium] = 150
            };
            var catalog = new ProductionCatalog(new[]
            {
                new ProductionRuleDto { BuildingDefinitionId = "food_lab", Resource = ResourceType.Food, ProductionPerTick = 2 }
            });

            var result = ProductionSimulator.Advance(totals,
                new[] { new BuildingProductionInput("food_lab", 1) }, catalog, null, 5);

            Assert.AreEqual(500, result.Totals[ResourceType.Oxygen]);
            Assert.AreEqual(310, result.Totals[ResourceType.Food]);
            Assert.AreEqual(150, result.Totals[ResourceType.Titanium]);
        }

        [Test]
        public void EmptyWallet_DoesNotChangeAnything()
        {
            // S2: zero buildings, zero production — nothing may move.
            var result = ProductionSimulator.Advance(
                new Dictionary<ResourceType, long> { [ResourceType.Oxygen] = 500 },
                new BuildingProductionInput[0], ProductionCatalog.Empty, null, 10);

            Assert.IsFalse(result.HasChanges);
            Assert.AreEqual(500, result.Totals[ResourceType.Oxygen]);
        }

        [Test]
        public void UnknownResourceKey_IsNotWritten()
        {
            Apply(WalletDelta(1, (ResourceType.Oxygen, 500), (ResourceType.Unknown, 999)));

            Assert.AreEqual(1, _view.Data.resources.Length);
            Assert.AreEqual(500, Balance(ResourceType.Oxygen));
        }

        [Test]
        public void CorruptWalletPayload_LeavesStateUntouched()
        {
            Apply(WalletDelta(1, (ResourceType.Oxygen, 500)));

            Apply(new StateDelta
            {
                ServerTick = 2,
                Changes = new List<EntityChangeDto>
                {
                    new() { EntityId = EntityIdPrefixes.WalletEntityId, Kind = EntityChangeKind.Update, Payload = new byte[] { 0xFF, 0xFF, 0xFF } }
                }
            });

            Assert.AreEqual(500, Balance(ResourceType.Oxygen));
            Assert.AreEqual(1UL, _view.LastResourceTick);
        }

        [Test]
        public void OlderWalletFrame_IsRejected()
        {
            Apply(WalletDelta(20, (ResourceType.Oxygen, 600)));
            Apply(WalletDelta(15, (ResourceType.Oxygen, 550)));

            Assert.AreEqual(600, Balance(ResourceType.Oxygen));
            Assert.AreEqual(20UL, _view.LastResourceTick);
        }

        [Test]
        public void SnapshotWins_ThenOlderDeltaIsRejected()
        {
            // S6: after a reconnect snapshot, a delta queued before the disconnect must not rewind.
            Apply(WalletDelta(10, (ResourceType.Oxygen, 500)));

            var snapshot = new StateSnapshot { ServerTick = 100, Resources = Wallet(100, (ResourceType.Oxygen, 900)) };
            ClientStateViewMutator.ApplySnapshot(_view, snapshot, _serialization);

            Apply(WalletDelta(40, (ResourceType.Oxygen, 520)));

            Assert.AreEqual(900, Balance(ResourceType.Oxygen));
        }

        [Test]
        public void SnapshotWithLowerTick_StillWins()
        {
            // A snapshot is the full truth (e.g. a fresh server session restarting its tick).
            Apply(WalletDelta(50, (ResourceType.Oxygen, 500)));
            ClientStateViewMutator.ApplySnapshot(_view,
                new StateSnapshot { ServerTick = 3, Resources = Wallet(3, (ResourceType.Oxygen, 120)) }, _serialization);

            Assert.AreEqual(120, Balance(ResourceType.Oxygen));
            Assert.AreEqual(3UL, _view.LastResourceTick);
        }

        [Test]
        public void UnversionedFrame_IsAccepted()
        {
            Apply(WalletDelta(30, (ResourceType.Oxygen, 500)));
            Apply(WalletDelta(0, (ResourceType.Oxygen, 450)));

            Assert.AreEqual(450, Balance(ResourceType.Oxygen));
        }

        private void Apply(StateDelta delta) =>
            ClientStateViewMutator.ApplyDelta(_view, delta, _serialization, new List<CommandAckDto>());

        private long Balance(ResourceType type) =>
            _view.Data.resources.Single(r => ResourceTypeMap.FromStoredValue(r.resourceType) == type).amount;

        private static ResourceStateDto Wallet(ulong tick, params (ResourceType type, long amount)[] amounts)
        {
            var dto = new ResourceStateDto { ServerTick = tick };
            foreach (var (type, amount) in amounts)
                dto.Amounts[type] = amount;
            return dto;
        }

        private StateDelta WalletDelta(ulong tick, params (ResourceType type, long amount)[] amounts) => new()
        {
            ServerTick = tick,
            Changes = new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIdPrefixes.WalletEntityId,
                    Kind = EntityChangeKind.Update,
                    Payload = _serializer.Serialize(Wallet(tick, amounts))
                }
            }
        };
    }
}
