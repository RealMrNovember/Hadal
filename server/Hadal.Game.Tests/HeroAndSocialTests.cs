using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Heroes, chat, alliances and alliance help (docs/25).</summary>
    public sealed class HeroAndSocialTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public HeroAndSocialTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity, string name = "")
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, name.Length > 0 ? name : identity);
            return (session, connection);
        }

        private static CommandAckDto LastAck(RecordingConnection connection) => connection.Messages.Last().Delta().Ack()!;

        private static CommandEnvelope Command<T>(T command, CommandType type) where T : ICommand =>
            EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete) is { } change
                ? TestSupport.Serializer.Deserialize<T>(change.Payload)
                : throw new InvalidOperationException("no " + entityId);

        // --- Heroes -----------------------------------------------------------------------------

        [Fact]
        public async Task Every_base_starts_with_the_starter_hero_on_the_crew()
        {
            var (_, connection) = await Connect("hero-a");
            var roster = connection.Messages.First().Snapshot().Heroes!;
            var mara = Assert.Single(roster.Heroes);
            Assert.Equal("mara", mara.DefinitionId);
            Assert.Equal(0, mara.CrewSlot);
            Assert.Equal(0, roster.FreePullRemainingMs);
        }

        private static void Rich(PlayerSession session)
        {
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Energy, ResourceType.Oxygen, ResourceType.Food })
                session.StateForTests.Wallet[type] = 20000;
        }

        [Fact]
        public async Task The_crew_hero_speeds_up_construction()
        {
            var (session, connection) = await Connect("hero-b");
            var reactor = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            Rich(session);
            Assert.True(TestSupport.Catalog.TryGetBuilding(reactor.DefinitionId, out var definition));
            var plain = BuildingProgression.Seconds(definition, 2, TestSupport.Catalog.Progression);

            // Mara (-3% at level 1) is on the crew from the start.
            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(PlayerModifiers.ApplyPercent(plain, -3), reactor.ConstructionSeconds);
            Assert.True(reactor.ConstructionSeconds < plain);

            // Off the crew: the plain time.
            reactor.ConstructionEndsAt = null;
            _world.HandleCommand(session, Command(new AssignHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "mara", Slot = -1 }, CommandType.AssignHero));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            reactor.Level = 1;
            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            Assert.Equal(plain, reactor.ConstructionSeconds);
        }

        [Fact]
        public async Task Free_pull_then_paid_pulls_add_heroes_or_shards()
        {
            var (session, connection) = await Connect("hero-c");
            var free = new ExecuteGachaCommand { CommandId = Id(), ClientSequence = 1, BannerId = HeroRules.PodBanner, PullCount = 1 };
            _world.HandleCommand(session, Command(free, CommandType.ExecuteGacha));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var result = Latest<GachaResultDto>(connection, EntityIds.GachaResult + free.CommandId);
            Assert.Single(result.Pulls);
            Assert.Equal(100, session.StateForTests.Wallet[ResourceType.Hadalite]); // free: nothing charged

            // The free pull is used: the next single costs Hadalite.
            _world.HandleCommand(session, Command(new ExecuteGachaCommand { CommandId = Id(), ClientSequence = 1, PullCount = 1 }, CommandType.ExecuteGacha));
            Assert.Equal(80, session.StateForTests.Wallet[ResourceType.Hadalite]);

            session.StateForTests.Wallet[ResourceType.Hadalite] = 1000;
            var ten = new ExecuteGachaCommand { CommandId = Id(), ClientSequence = 1, PullCount = 10 };
            _world.HandleCommand(session, Command(ten, CommandType.ExecuteGacha));
            Assert.Equal(820, session.StateForTests.Wallet[ResourceType.Hadalite]);
            var pulls = Latest<GachaResultDto>(connection, EntityIds.GachaResult + ten.CommandId).Pulls;
            Assert.Equal(10, pulls.Length);
            Assert.All(pulls, p => Assert.True(p.IsNew || p.Shards > 0));
            Assert.Equal(session.StateForTests.Heroes.Count, session.StateForTests.Heroes.Select(h => h.HeroId).Distinct().Count());
        }

        [Fact]
        public void Pity_guarantees_an_epic_or_better()
        {
            var since = TestSupport.Catalog.HeroRules.pod.pity - 1;
            var hero = HeroRules.Pull(TestSupport.Catalog, ref since, _ => 0); // roll 0 = the most common rarity without pity
            Assert.NotNull(hero);
            Assert.Contains(hero!.Rarity.id, new[] { "epic", "legendary" });
            Assert.Equal(0, since);
        }

        [Fact]
        public async Task Training_is_capped_by_stars_and_promotion_spends_shards()
        {
            var (session, connection) = await Connect("hero-d");
            var mara = session.StateForTests.Heroes.Single();
            session.StateForTests.Wallet[ResourceType.Titanium] = 25000;
            session.StateForTests.Wallet[ResourceType.Biomass] = 6000;

            mara.Level = 10;
            _world.HandleCommand(session, Command(new LevelUpHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "mara" }, CommandType.LevelUpHero));
            Assert.Equal(nameof(HeroRejectReason.LevelCapReached), LastAck(connection).Reason);

            _world.HandleCommand(session, Command(new PromoteHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "mara" }, CommandType.PromoteHero));
            Assert.Equal(nameof(HeroRejectReason.NotEnoughShards), LastAck(connection).Reason);

            mara.Shards = 15;
            _world.HandleCommand(session, Command(new PromoteHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "mara" }, CommandType.PromoteHero));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(2, mara.Stars);
            Assert.Equal(5, mara.Shards);

            var titanium = session.StateForTests.Wallet[ResourceType.Titanium];
            _world.HandleCommand(session, Command(new LevelUpHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "mara" }, CommandType.LevelUpHero));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(11, mara.Level);
            Assert.True(session.StateForTests.Wallet[ResourceType.Titanium] < titanium);
        }

        [Fact]
        public async Task Crew_slots_open_with_the_reactor()
        {
            var (session, connection) = await Connect("hero-e");
            session.StateForTests.Heroes.Add(new OwnedHeroState { HeroId = "lyra" });
            _world.HandleCommand(session, Command(new AssignHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "lyra", Slot = 1 }, CommandType.AssignHero));
            Assert.Equal(nameof(HeroRejectReason.SlotLocked), LastAck(connection).Reason);

            // Taking slot 0 moves Mara off the crew.
            _world.HandleCommand(session, Command(new AssignHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "lyra", Slot = 0 }, CommandType.AssignHero));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(-1, session.StateForTests.Heroes.Single(h => h.HeroId == "mara").Slot);
        }

        [Fact]
        public async Task Heroes_survive_a_reconnect()
        {
            var (session, connection) = await Connect("hero-f");
            session.StateForTests.Heroes.Add(new OwnedHeroState { HeroId = "juno", Level = 7, Stars = 2, Shards = 3 });
            session.StateForTests.IsDirty = true;
            await _world.DisconnectAsync(session, connection, default);

            var (_, again) = await Connect("hero-f");
            var juno = again.Messages.First().Snapshot().Heroes!.Heroes.Single(h => h.DefinitionId == "juno");
            Assert.Equal((7, 2, 3), (juno.Level, juno.Stars, juno.Shards));
        }

        // --- Chat and alliances ------------------------------------------------------------------

        private void Chat(PlayerSession session, string text, ChatChannel channel = ChatChannel.World) =>
            _world.HandleCommand(session, Command(new SendChatCommand { CommandId = Id(), ClientSequence = 1, Channel = (int)channel, Text = text }, CommandType.SendChat));

        private void CreateAlliance(PlayerSession session, string name, string tag) =>
            _world.HandleCommand(session, Command(new CreateAllianceCommand { CommandId = Id(), ClientSequence = 1, Name = name, Tag = tag }, CommandType.CreateAlliance));

        [Fact]
        public async Task World_chat_reaches_everyone_online_and_is_rate_limited()
        {
            var (alice, aliceConnection) = await Connect("chat-a", "Alice");
            var (_, bobConnection) = await Connect("chat-b", "Bob");

            Chat(alice, "  hello\n  deep  ");
            Assert.Equal(CommandResultCode.Accepted, LastAck(aliceConnection).Result);
            var line = bobConnection.Messages.Last().Delta().Changes.Single(c => c.EntityId.StartsWith(EntityIds.ChatMessage, StringComparison.Ordinal));
            var dto = TestSupport.Serializer.Deserialize<ChatMessageDto>(line.Payload);
            Assert.Equal(("Alice", "hello deep"), (dto.SenderName, dto.Text));

            Chat(alice, "again");
            Assert.Equal(nameof(SocialRejectReason.TooFast), LastAck(aliceConnection).Reason);

            Chat(alice, "   ");
            Assert.Equal(nameof(SocialRejectReason.EmptyMessage), LastAck(aliceConnection).Reason);

            // A newcomer gets the history.
            var (_, carolConnection) = await Connect("chat-c", "Carol");
            var history = Latest<ChatHistoryDto>(carolConnection, EntityIds.ChatHistory);
            Assert.Contains(history.Messages, m => m.Text == "hello deep");
        }

        [Fact]
        public async Task Alliance_lifecycle_create_join_chat_kick_leave()
        {
            var (leader, leaderConnection) = await Connect("ally-a", "Leader");
            var (member, memberConnection) = await Connect("ally-b", "Member");

            CreateAlliance(leader, "x", "AB");
            Assert.Equal(nameof(SocialRejectReason.InvalidName), LastAck(leaderConnection).Reason);
            CreateAlliance(leader, "Abyss Wardens", "AW");
            Assert.Equal(nameof(SocialRejectReason.InvalidTag), LastAck(leaderConnection).Reason);

            leader.StateForTests.Wallet[ResourceType.Titanium] = 500;
            CreateAlliance(leader, "Abyss Wardens", "awd");
            Assert.Equal(nameof(SocialRejectReason.InsufficientResources), LastAck(leaderConnection).Reason);

            leader.StateForTests.Wallet[ResourceType.Titanium] = 5000;
            CreateAlliance(leader, "Abyss Wardens", "awd");
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            Assert.Equal(2000, leader.StateForTests.Wallet[ResourceType.Titanium]);
            var mine = Latest<AllianceDto>(leaderConnection, EntityIds.MyAlliance);
            Assert.Equal(("AWD", leader.PublicId), (mine.Tag, mine.LeaderId));

            member.StateForTests.Wallet[ResourceType.Titanium] = 5000;
            CreateAlliance(member, "abyss wardens", "ZZZ");
            Assert.Equal(nameof(SocialRejectReason.NameTaken), LastAck(memberConnection).Reason);

            _world.HandleCommand(member, Command(new BrowseAlliancesCommand { CommandId = Id(), ClientSequence = 1, Query = "abyss" }, CommandType.BrowseAlliances));
            var found = Latest<AllianceListDto>(memberConnection, EntityIds.AllianceList).Alliances.Single();
            _world.HandleCommand(member, Command(new JoinAllianceCommand { CommandId = Id(), ClientSequence = 1, AllianceId = found.Id }, CommandType.JoinAlliance));
            Assert.Equal(CommandResultCode.Accepted, LastAck(memberConnection).Result);
            Assert.Equal(2, Latest<AllianceDto>(leaderConnection, EntityIds.MyAlliance).Members.Length);

            // Alliance chat stays inside the alliance.
            var (_, outsiderConnection) = await Connect("ally-c", "Outsider");
            var before = outsiderConnection.Messages.Count;
            Chat(member, "rally at the trench", ChatChannel.Alliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(memberConnection).Result);
            Assert.Equal(before, outsiderConnection.Messages.Count);
            var line = leaderConnection.Messages.Last().Delta().Changes.Single(c => c.EntityId.StartsWith(EntityIds.ChatMessage, StringComparison.Ordinal));
            Assert.Equal("AWD", TestSupport.Serializer.Deserialize<ChatMessageDto>(line.Payload).AllianceTag);

            // Only the leader kicks.
            _world.HandleCommand(member, Command(new KickMemberCommand { CommandId = Id(), ClientSequence = 1, MemberId = leader.PublicId }, CommandType.KickMember));
            Assert.Equal(nameof(SocialRejectReason.NotLeader), LastAck(memberConnection).Reason);
            _world.HandleCommand(leader, Command(new KickMemberCommand { CommandId = Id(), ClientSequence = 1, MemberId = member.PublicId }, CommandType.KickMember));
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            Assert.Contains(memberConnection.Messages.Last(m => m.Delta != null && m.Delta().Changes.Any(c => c.EntityId == EntityIds.MyAlliance)).Delta().Changes,
                c => c.EntityId == EntityIds.MyAlliance && c.Kind == EntityChangeKind.Delete);

            // The last one out disbands it: the name is free again.
            _world.HandleCommand(leader, Command(new LeaveAllianceCommand { CommandId = Id(), ClientSequence = 1 }, CommandType.LeaveAlliance));
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            CreateAlliance(member, "Abyss Wardens", "AWD");
            Assert.Equal(CommandResultCode.Accepted, LastAck(memberConnection).Result);
        }

        [Fact]
        public async Task Alliance_help_shortens_construction_online_and_offline()
        {
            var (owner, ownerConnection) = await Connect("help-a", "Owner");
            var (helper, helperConnection) = await Connect("help-b", "Helper");
            Rich(owner);
            CreateAlliance(owner, "Deep Help", "DH");
            Assert.Equal(nameof(SocialRejectReason.InvalidTag), LastAck(ownerConnection).Reason);
            CreateAlliance(owner, "Deep Help", "DHX");
            var id = Latest<AllianceDto>(ownerConnection, EntityIds.MyAlliance).Id;
            _world.HandleCommand(helper, Command(new JoinAllianceCommand { CommandId = Id(), ClientSequence = 1, AllianceId = id }, CommandType.JoinAlliance));

            // A long upgrade, so two helps do not finish it.
            var reactor = owner.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            reactor.Level = 25;
            foreach (var type in owner.StateForTests.Wallet.Keys.ToList())
                owner.StateForTests.Wallet[type] = 10_000_000;
            _world.HandleCommand(owner, TestSupport.Upgrade(reactor.BuildingId));
            Assert.Equal(CommandResultCode.Accepted, LastAck(ownerConnection).Result);
            var endsAt = reactor.ConstructionEndsAt!.Value;

            _world.HandleCommand(owner, Command(new RequestHelpCommand { CommandId = Id(), ClientSequence = 1, BuildingEntityId = reactor.BuildingId }, CommandType.RequestHelp));
            Assert.Equal(CommandResultCode.Accepted, LastAck(ownerConnection).Result);
            var request = Latest<HelpListDto>(helperConnection, EntityIds.AllianceHelp).Requests.Single();
            Assert.Equal(("Owner", BuildingProgression.ReactorId), (request.OwnerName, request.BuildingDefinitionId));

            _world.HandleCommand(helper, Command(new HelpAllCommand { CommandId = Id(), ClientSequence = 1 }, CommandType.HelpAll));
            Assert.Equal(CommandResultCode.Accepted, LastAck(helperConnection).Result);
            var help = SocialRules.HelpSeconds(TestSupport.Catalog.Alliance, reactor.ConstructionSeconds);
            Assert.Equal(endsAt.AddSeconds(-help), reactor.ConstructionEndsAt);

            // Once per helper per request.
            _world.HandleCommand(helper, Command(new HelpAllCommand { CommandId = Id(), ClientSequence = 1 }, CommandType.HelpAll));
            Assert.Equal(nameof(SocialRejectReason.NothingToHelp), LastAck(helperConnection).Reason);

            // Offline: a second helper's help lands when the owner logs back in.
            var (third, thirdConnection) = await Connect("help-c", "Third");
            _world.HandleCommand(third, Command(new JoinAllianceCommand { CommandId = Id(), ClientSequence = 1, AllianceId = id }, CommandType.JoinAlliance));
            await _world.DisconnectAsync(owner, ownerConnection, default);
            _world.HandleCommand(third, Command(new HelpAllCommand { CommandId = Id(), ClientSequence = 1 }, CommandType.HelpAll));
            Assert.Equal(CommandResultCode.Accepted, LastAck(thirdConnection).Result);

            var (again, _) = await Connect("help-a", "Owner");
            var reloaded = again.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            Assert.Equal(endsAt.AddSeconds(-2 * help), reloaded.ConstructionEndsAt);
        }
    }
}
