using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Alliance help beyond construction (docs/25 §4): the Med Bay's healing and research, the per-help cut, the help cap,
    /// no help to yourself, helps given while the owner is offline, and the heal / general bubbles' filters.
    /// </summary>
    public sealed class AllianceHelpTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public AllianceHelpTests()
        {
            _world = TestSupport.World(_clock);
        }

        private async Task<(PlayerSession S, RecordingConnection C)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass })
                session.StateForTests.Wallet[type] = 1_000_000;
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private static IEnumerable<HelpReceivedDto> Received(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).SelectMany(m => m.Delta().Changes)
                .Where(c => c.EntityId.StartsWith(EntityIds.HelpReceived, StringComparison.Ordinal))
                .Select(c => TestSupport.Serializer.Deserialize<HelpReceivedDto>(c.Payload));

        /// <summary>An alliance of <paramref name="count"/> players; the first one founds it.</summary>
        private async Task<List<(PlayerSession S, RecordingConnection C)>> Alliance(string name, int count)
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect(name + "0") };
            Send(members[0].S, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(members[0].C).Result);
            var allianceId = Latest<AllianceDto>(members[0].C, EntityIds.MyAlliance).Id;
            for (var i = 1; i < count; i++)
            {
                var member = await Connect(name + i);
                Send(member.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
                Assert.Equal(CommandResultCode.Accepted, LastAck(member.C).Result);
                members.Add(member);
            }

            return members;
        }

        private HealState Heal(PlayerSession session, int seconds)
        {
            var healing = new HealState { Troops = { ["armored:1"] = 100 }, StartedAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddSeconds(seconds) };
            session.StateForTests.Healing = healing;
            return healing;
        }

        private ActiveResearch Research(PlayerSession session, int seconds)
        {
            var research = new ActiveResearch
            {
                NodeId = TestSupport.Catalog.ResearchNodes.First().Id, TargetLevel = 1, Seconds = seconds, EndsAt = _clock.UtcNow.AddSeconds(seconds)
            };
            session.StateForTests.ActiveResearch.Add(research);
            return research;
        }

        private void Ask(PlayerSession session, string kind, string target = "") =>
            Send(session, new RequestHelpCommand { CommandId = Id(), Kind = kind, Target = target }, CommandType.RequestHelp);

        private void HelpAll(PlayerSession session, string kind = "") =>
            Send(session, new HelpAllCommand { CommandId = Id(), Kind = kind }, CommandType.HelpAll);

        private static int Cut(int totalSeconds) => SocialRules.HelpSeconds(TestSupport.Catalog.Alliance, totalSeconds);

        [Fact]
        public async Task A_heal_help_shortens_the_healing_and_tells_the_owner()
        {
            var team = await Alliance("hheal", 2);
            var (owner, ownerConnection) = team[0];
            var healing = Heal(owner, 20_000);
            var ends = healing.EndsAt;

            Ask(owner, HelpKinds.Heal);
            Assert.Equal(CommandResultCode.Accepted, LastAck(ownerConnection).Result);
            var open = Assert.Single(Latest<HelpListDto>(team[1].C, EntityIds.AllianceHelp).Requests);
            Assert.Equal((HelpKinds.Heal, 100), (open.Kind, open.TargetLevel));
            Ask(owner, HelpKinds.Heal);
            Assert.Equal(nameof(SocialRejectReason.AlreadyRequested), LastAck(ownerConnection).Reason);

            HelpAll(team[1].S, HelpKinds.Heal);
            Assert.Equal(CommandResultCode.Accepted, LastAck(team[1].C).Result);
            Assert.Equal(200, Cut(20_000)); // 1 % of the total, at least 60 s
            Assert.Equal(ends.AddSeconds(-200), owner.StateForTests.Healing!.EndsAt);
            var note = Assert.Single(Received(ownerConnection));
            Assert.Equal((team[1].S.DisplayName, 1, 200), (note.HelperName, note.Count, note.Seconds));
            Assert.Equal(ends.AddSeconds(-200).ToUnixTimeMilliseconds(), Latest<ArmyDto>(ownerConnection, EntityIds.Army).Healing!.EndsAtMs);
        }

        [Fact]
        public async Task A_research_help_shortens_the_research_by_at_least_a_minute()
        {
            var team = await Alliance("hres", 2);
            var (owner, ownerConnection) = team[0];
            var research = Research(owner, 3000);
            var ends = research.EndsAt;

            Ask(owner, HelpKinds.Research, research.NodeId);
            Assert.Equal(CommandResultCode.Accepted, LastAck(ownerConnection).Result);
            Ask(owner, HelpKinds.Research, "no-such-node");
            Assert.Equal(nameof(SocialRejectReason.NothingToHelp), LastAck(ownerConnection).Reason);

            HelpAll(team[1].S);
            Assert.Equal(ends.AddSeconds(-60), research.EndsAt); // 1 % of 3000 s is under the 60 s floor
        }

        [Fact]
        public async Task A_request_takes_at_most_ten_helps()
        {
            var team = await Alliance("hcap", 12);
            var owner = team[0].S;
            var healing = Heal(owner, 100_000);
            var ends = healing.EndsAt;
            Ask(owner, HelpKinds.Heal);

            for (var i = 1; i <= 10; i++)
            {
                HelpAll(team[i].S);
                Assert.Equal(CommandResultCode.Accepted, LastAck(team[i].C).Result);
            }

            HelpAll(team[11].S);
            Assert.Equal(nameof(SocialRejectReason.NothingToHelp), LastAck(team[11].C).Reason);
            Assert.Equal(ends.AddSeconds(-10 * Cut(100_000)), owner.StateForTests.Healing!.EndsAt);
            Assert.Empty(Latest<HelpListDto>(team[11].C, EntityIds.AllianceHelp).Requests);
        }

        [Fact]
        public async Task Nobody_helps_themselves_or_helps_twice()
        {
            var team = await Alliance("hself", 2);
            var (owner, ownerConnection) = team[0];
            Heal(owner, 20_000);
            Ask(owner, HelpKinds.Heal);

            HelpAll(owner);
            Assert.Equal(nameof(SocialRejectReason.NothingToHelp), LastAck(ownerConnection).Reason);
            HelpAll(team[1].S);
            HelpAll(team[1].S);
            Assert.Equal(nameof(SocialRejectReason.NothingToHelp), LastAck(team[1].C).Reason);
        }

        [Fact]
        public async Task Helps_given_while_the_owner_is_away_apply_on_the_next_login()
        {
            var team = await Alliance("hoff", 2);
            var (owner, ownerConnection) = team[0];
            var ends = Heal(owner, 20_000).EndsAt;
            var research = Research(owner, 10_000);
            Ask(owner, HelpKinds.Heal);
            Ask(owner, HelpKinds.Research, research.NodeId);
            await _world.DisconnectAsync(owner, ownerConnection, default);

            HelpAll(team[1].S);
            Assert.Equal(CommandResultCode.Accepted, LastAck(team[1].C).Result);

            var (back, _) = await Connect("hoff0");
            Assert.Equal(ends.AddSeconds(-Cut(20_000)), back.StateForTests.Healing!.EndsAt);
            Assert.Equal(research.EndsAt.AddSeconds(-Cut(10_000)), back.StateForTests.ActiveResearch.Single().EndsAt);
        }

        [Fact]
        public async Task The_heal_bubble_helps_healing_only_and_the_general_bubble_the_rest()
        {
            var team = await Alliance("hkind", 2);
            var (owner, _) = team[0];
            var healing = Heal(owner, 20_000);
            var research = Research(owner, 20_000);
            var (healEnds, researchEnds) = (healing.EndsAt, research.EndsAt);
            Ask(owner, HelpKinds.Heal);
            Ask(owner, HelpKinds.Research, research.NodeId);

            HelpAll(team[1].S, HelpKinds.Heal);
            Assert.Equal((healEnds.AddSeconds(-200), researchEnds), (healing.EndsAt, research.EndsAt));
            HelpAll(team[1].S, HelpKinds.Heal);
            Assert.Equal(nameof(SocialRejectReason.NothingToHelp), LastAck(team[1].C).Reason);

            HelpAll(team[1].S, HelpKinds.General);
            Assert.Equal(CommandResultCode.Accepted, LastAck(team[1].C).Result);
            Assert.Equal(researchEnds.AddSeconds(-200), research.EndsAt);
            Assert.True(HelpKinds.Matches(HelpKinds.General, string.Empty)); // older clients' build requests are general
        }
    }
}
