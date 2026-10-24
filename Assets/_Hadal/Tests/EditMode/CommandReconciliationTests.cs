using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// QA S5: two placements sent inside the 200 ms ack window. Both acks must resolve their
    /// own ghost; the second command must not make the first one's ack unrecognisable.
    /// </summary>
    public sealed class CommandReconciliationTests
    {
        private FakeStateSync _stateSync;
        private RecordingBus _bus;
        private CommandReconciliationSystem _reconciliation;

        [SetUp]
        public void SetUp()
        {
            _stateSync = new FakeStateSync();
            _bus = new RecordingBus();
            _reconciliation = new CommandReconciliationSystem(
                _stateSync, new NetworkSerializationLayer(), new NoRollback(), _bus);
        }

        [Test]
        public void TwoPlacementsInFlight_BothResolve()
        {
            _reconciliation.RegisterPendingCommand("cmd-1", 1, CommandType.PlaceBuilding);
            _reconciliation.RegisterPendingCommand("cmd-2", 2, CommandType.PlaceBuilding);

            DeliverAck("cmd-1", 1, CommandResultCode.Accepted);
            DeliverAck("cmd-2", 2, CommandResultCode.Accepted);

            CollectionAssert.AreEqual(new[] { "cmd-1", "cmd-2" }, _bus.Resolved);
        }

        [Test]
        public void AckIsConsumedOnce()
        {
            _reconciliation.RegisterPendingCommand("cmd-1", 1, CommandType.PlaceBuilding);

            DeliverAck("cmd-1", 1, CommandResultCode.Accepted);
            DeliverAck("cmd-1", 1, CommandResultCode.Accepted);

            CollectionAssert.AreEqual(new[] { "cmd-1" }, _bus.Resolved);
        }

        [Test]
        public void UnknownCommandAck_IsIgnored()
        {
            DeliverAck("never-sent", 9, CommandResultCode.Accepted);
            CollectionAssert.IsEmpty(_bus.Resolved);
        }

        [Test]
        public void SequenceMismatch_IsIgnored()
        {
            _reconciliation.RegisterPendingCommand("cmd-1", 1, CommandType.PlaceBuilding);
            DeliverAck("cmd-1", 7, CommandResultCode.Accepted);
            CollectionAssert.IsEmpty(_bus.Resolved);
        }

        [Test]
        public void Rejection_ResolvesAndShakesTheGhost()
        {
            _reconciliation.RegisterPendingCommand("cmd-1", 1, CommandType.PlaceBuilding);
            DeliverAck("cmd-1", 1, CommandResultCode.Rejected);

            CollectionAssert.AreEqual(new[] { "cmd-1" }, _bus.Resolved);
            Assert.AreEqual(1, _bus.Shakes);
        }

        private void DeliverAck(string commandId, ulong sequence, CommandResultCode result)
        {
            _stateSync.NextAcks.Add(new CommandAckDto { CommandId = commandId, ClientSequence = sequence, Result = result });
            _reconciliation.EnqueueInbound(new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateDelta,
                Delta = new StateDelta { Changes = new List<EntityChangeDto>() }
            });
            _reconciliation.ProcessQueue();
        }

        private sealed class FakeStateSync : IStateSyncService
        {
            public List<CommandAckDto> NextAcks { get; } = new();
            public ClientStateView View { get; } = new();
            public uint ActiveSchemaVersion => 2;
            public void Initialize() { }
            public void Shutdown() { }
            public void ApplySnapshot(StateSnapshot snapshot) { }

            public IReadOnlyList<CommandAckDto> ApplyDelta(StateDelta delta)
            {
                var acks = NextAcks.ToArray();
                NextAcks.Clear();
                return acks;
            }

            public void CaptureAll(IEnumerable<ISaveParticipant> participants) { }
            public void RestoreAll(IEnumerable<ISaveParticipant> participants) { }
        }

        private sealed class NoRollback : IRollbackAnimator
        {
            public void RequestBuildingRollback(BuildingSaveEntry authoritative, string commandId) { }
            public void Tick(float deltaTime) { }
        }

        private sealed class RecordingBus : ILocalEventBus
        {
            public List<string> Resolved { get; } = new();
            public int Shakes { get; private set; }

            public void Subscribe<T>(Action<T> handler) { }
            public void Unsubscribe<T>(Action<T> handler) { }

            public void Publish<T>(T payload)
            {
                switch (payload)
                {
                    case BuildingPlacementResolvedEvent resolved:
                        Resolved.Add(resolved.CommandId);
                        break;
                    case GhostRejectionShakeEvent:
                        Shakes++;
                        break;
                }
            }
        }
    }
}
