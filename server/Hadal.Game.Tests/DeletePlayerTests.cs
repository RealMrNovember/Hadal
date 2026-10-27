using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;

namespace Hadal.Game.Tests
{
    /// <summary>Overseer removal of test players (Admin → Players → Delete).</summary>
    public sealed class DeletePlayerTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public DeletePlayerTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        [Fact]
        public async Task A_deleted_player_leaves_the_map_and_the_alliance_and_is_never_saved_again()
        {
            var leaderConnection = new RecordingConnection();
            var leader = await _world.ConnectAsync("del-a", leaderConnection, default, "Leader");
            leader.StateForTests.Wallet[ResourceType.Titanium] = 50000;
            var memberConnection = new RecordingConnection();
            var member = await _world.ConnectAsync("del-b", memberConnection, default, "Member");

            Send(leader, new CreateAllianceCommand { CommandId = "c1", Name = "House DEL", Tag = "DEL" }, CommandType.CreateAlliance);
            var allianceId = TestSupport.Serializer.Deserialize<AllianceDto>(leaderConnection.Messages.Where(m => m.Delta != null)
                .Select(m => m.Delta()).SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
            Send(member, new JoinAllianceCommand { CommandId = "j1", AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.NotNull(_world.Map.PositionOf(leader.PublicId));

            Assert.True(await _world.DeletePlayerAsync("del-a", default));

            Assert.Null(_world.Map.PositionOf(leader.PublicId));
            Assert.Null(await _repository.LoadByIdentityAsync("del-a", default));
            Assert.Equal("account removed", leaderConnection.ClosedReason);
            Assert.Equal("DEL", _world.Social.TagOf(member.PublicId)); // the member now leads
            Assert.Equal(string.Empty, _world.Social.TagOf(leader.PublicId));

            // The closing connection must not write the player back.
            await _world.DisconnectAsync(leader, leaderConnection, default);
            await _world.SaveAllAsync(default);
            Assert.Null(await _repository.LoadByIdentityAsync("del-a", default));
            Assert.Null(_world.Map.PositionOf(leader.PublicId));

            Assert.False(await _world.DeletePlayerAsync("nobody", default));
        }
    }
}
