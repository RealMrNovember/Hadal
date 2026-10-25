using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using Hadal.Core.StateSync;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>Open rings and removed buildings replicate from the server (docs/23, rules 12 and 14).</summary>
    public sealed class BaseLayoutReplicationTests
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
        public void Snapshot_And_LayoutDelta_SetTheOpenRings()
        {
            ClientStateViewMutator.ApplySnapshot(_view, new StateSnapshot { ServerTick = 1, UnlockedRings = 5 }, _serialization);
            Assert.AreEqual(5, _view.Data.unlockedGridRings);

            Apply(new EntityChangeDto
            {
                EntityId = EntityIds.BaseLayout,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new BaseLayoutDto { UnlockedRings = 6 })
            });
            Assert.AreEqual(6, _view.Data.unlockedGridRings);
        }

        [Test]
        public void OlderServerWithoutRings_KeepsTheDefault()
        {
            var before = _view.Data.unlockedGridRings;
            ClientStateViewMutator.ApplySnapshot(_view, new StateSnapshot { ServerTick = 1 }, _serialization);
            Assert.AreEqual(before, _view.Data.unlockedGridRings);
        }

        [Test]
        public void BuildingDelete_RemovesIt()
        {
            ClientStateViewMutator.ApplySnapshot(_view, new StateSnapshot
            {
                ServerTick = 1,
                Buildings = new List<BuildingStateDto> { new() { EntityId = "b1", DefinitionId = "oxygen_plant", RingIndex = 1, Level = 1 } }
            }, _serialization);
            Assert.AreEqual(1, _view.Data.buildings.Length);

            Apply(new EntityChangeDto { EntityId = EntityIds.Building + "b1", Kind = EntityChangeKind.Delete });
            Assert.AreEqual(0, _view.Data.buildings.Length);
        }

        private void Apply(EntityChangeDto change) =>
            ClientStateViewMutator.ApplyDelta(_view, new StateDelta { ServerTick = 2, Changes = new List<EntityChangeDto> { change } },
                _serialization, new List<CommandAckDto>());
    }
}
