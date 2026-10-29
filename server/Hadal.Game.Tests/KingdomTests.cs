using Hadal.Game.Kingdoms;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>Kingdoms (docs/39): assignment, the cap rollover, and that map, chat, PvP, alliances and mail stay inside one.</summary>
    public sealed class KingdomTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly InMemoryKingdomStore _kingdoms = new();
        private readonly InMemoryWorldStore _worldStore = new();
        private GameWorld _world;

        public KingdomTests()
        {
            _world = NewWorld();
        }

        private GameWorld NewWorld() => new(new CatalogRuntime(Catalog), _repository, new InMemorySocialStore(), _worldStore, _clock, TestSupport.Serializer,
            NullLogger<GameWorld>.Instance, kingdomStore: _kingdoms);

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            var core = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            core.Level = 10;
            session.StateForTests.UnsavedBuildings.Add(core);
            session.StateForTests.Troops[TroopRules.Key("armored", 1)] = 5000;
            session.StateForTests.Wallet[ResourceType.Titanium] = 10_000_000;
            session.StateForTests.Wallet[ResourceType.Hadalite] = 100_000;
            Send(session, new ViewWorldCommand { CommandId = "v" + Guid.NewGuid().ToString("N"), X = -1, Y = -1 }, CommandType.ViewWorld);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static IEnumerable<EntityChangeDto> Changes(RecordingConnection c) =>
            c.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).SelectMany(d => d.Changes);

        private static CommandAckDto LastAck(RecordingConnection c) =>
            c.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Where(d => d.Ack() != null).Last().Ack()!;

        private static T Last<T>(RecordingConnection c, string entityId) =>
            TestSupport.Serializer.Deserialize<T>(Changes(c).Last(ch => ch.EntityId == entityId).Payload);

        /// <summary>Two kingdoms: the cap of #1 is 2, so the third player opens #2.</summary>
        private async Task<((PlayerSession S, RecordingConnection C) A1, (PlayerSession S, RecordingConnection C) A2, (PlayerSession S, RecordingConnection C) B)> TwoKingdoms()
        {
            await _world.InitializeWorldAsync(default);
            await _world.Kingdoms.UpdateAsync("w1", null, 2, null, null, default);
            var a1 = await Connect("k-a1");
            var a2 = await Connect("k-a2");
            var b = await Connect("k-b");
            return (a1, a2, b);
        }

        [Fact]
        public async Task New_players_fill_the_newest_kingdom_and_the_cap_opens_the_next()
        {
            var (a1, a2, b) = await TwoKingdoms();
            Assert.Equal("w1", a1.S.KingdomId);
            Assert.Equal("w1", a2.S.KingdomId);
            Assert.Equal("w2", b.S.KingdomId);
            var list = _world.Kingdoms.List(_clock.UtcNow);
            Assert.Equal(new[] { (1, KingdomStatus.Full, 2), (2, KingdomStatus.Open, 1) }, list.Select(k => (k.Kingdom.Number, k.Kingdom.Status, k.Members)));
            Assert.Equal(2, list[1].Kingdom.PlayerCap); // the next kingdom keeps the cap

            // The player list comes with "mine" and the own profile carries the number.
            Send(b.S, new ViewKingdomsCommand { CommandId = "kl" }, CommandType.ViewKingdoms);
            var shown = Last<KingdomListDto>(b.C, EntityIds.Kingdoms);
            Assert.Equal(new[] { false, true }, shown.Kingdoms.Select(k => k.Mine));
            Assert.Equal("w2", Last<WorldViewDto>(b.C, EntityIds.WorldView).WorldId);

            // A restart keeps everyone where they are.
            _world = NewWorld();
            await _world.InitializeWorldAsync(default);
            var again = await Connect("k-b");
            Assert.Equal("w2", again.Session.KingdomId);
            Assert.Equal("w2", (await Connect("k-c")).Session.KingdomId); // #2 still has room: cap 2
        }

        [Fact]
        public async Task An_admin_opens_the_next_kingdom_and_new_players_land_there()
        {
            await _world.InitializeWorldAsync(default);
            var a = await Connect("adm-a");
            var opened = await _world.OpenKingdomAsync(default);
            Assert.Equal((2, KingdomStatus.Open), (opened.Number, opened.Status));
            Assert.Equal(KingdomStatus.Full, _world.Kingdoms.Get("w1")!.Status);
            Assert.Equal("w2", (await Connect("adm-b")).Session.KingdomId);
            Assert.Equal("w1", a.Session.KingdomId);

            // A closed kingdom takes nobody; a reopened one does again.
            await _world.Kingdoms.UpdateAsync("w2", KingdomStatus.Closed, null, null, null, default);
            Assert.Equal("w3", (await Connect("adm-c")).Session.KingdomId);
        }

        [Fact]
        public async Task Each_kingdom_has_its_own_map_and_pvp_stays_inside()
        {
            var (a1, _, b) = await TwoKingdoms();
            var bView = Last<WorldViewDto>(b.C, EntityIds.WorldView);
            Assert.Equal("w2", bView.WorldId);
            Assert.DoesNotContain(bView.Bases, x => x.PlayerId == a1.S.PublicId);
            Assert.Contains(bView.Bases, x => x.PlayerId == b.S.PublicId);
            Assert.DoesNotContain(Last<WorldViewDto>(a1.C, EntityIds.WorldView).Bases, x => x.PlayerId == b.S.PublicId);
            Assert.Same(_world.MapOf("w2"), _world.MapOf(b.S));

            // An attack on a base of another kingdom finds no such base.
            Send(b.S, new MarchPvpCommand { CommandId = "kx", Mode = PvpRules.Attack, TargetKind = PvpRules.TargetBase, TargetId = a1.S.PublicId }, CommandType.MarchPvp);
            Assert.Equal("UnknownPlayer", LastAck(b.C).Reason);

            // A profile from another kingdom still opens, with its number.
            Send(b.S, new ViewProfileCommand { CommandId = "kp", PlayerId = a1.S.PublicId }, CommandType.ViewProfile);
            var card = Last<PlayerProfileDto>(b.C, EntityIds.Profile);
            Assert.Equal((a1.S.PublicId, 1), (card.PlayerId, card.Kingdom));
        }

        [Fact]
        public async Task World_chat_alliances_and_mail_stay_inside_the_kingdom()
        {
            var (a1, a2, b) = await TwoKingdoms();

            // World chat: #1 hears #1 only.
            Send(a1.S, new SendChatCommand { CommandId = "kc", Channel = (int)ChatChannel.World, Text = "hello kingdom one" }, CommandType.SendChat);
            bool Heard(RecordingConnection c) => Changes(c).Where(ch => ch.EntityId.StartsWith(EntityIds.ChatMessage, StringComparison.Ordinal))
                .Any(ch => TestSupport.Serializer.Deserialize<ChatMessageDto>(ch.Payload).Text == "hello kingdom one");
            Assert.True(Heard(a2.C));
            Assert.False(Heard(b.C));

            // An alliance of #1 cannot be found, joined or invite a player of #2.
            Send(a1.S, new CreateAllianceCommand { CommandId = "ka", Name = "Kingdom One", Tag = "KONE" }, CommandType.CreateAlliance);
            var allianceId = _world.Social.AllianceIdOf(a1.S.PublicId);
            Assert.NotEmpty(allianceId);
            Send(b.S, new BrowseAlliancesCommand { CommandId = "kb" }, CommandType.BrowseAlliances);
            Assert.DoesNotContain(Last<AllianceListDto>(b.C, EntityIds.AllianceList).Alliances, x => x.Id == allianceId);
            Send(b.S, new JoinAllianceCommand { CommandId = "kj", AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(string.Empty, _world.Social.AllianceIdOf(b.S.PublicId));
            Send(a2.S, new JoinAllianceCommand { CommandId = "kj2", AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(allianceId, _world.Social.AllianceIdOf(a2.S.PublicId));
            Send(a1.S, new InviteToAllianceCommand { CommandId = "ki", PlayerId = b.S.PublicId }, CommandType.InviteToAlliance);
            Assert.Equal(CommandResultCode.Rejected, LastAck(a1.C).Result);

            // Players write only within their kingdom.
            Send(b.S, new SendMailCommand { CommandId = "km", PlayerId = a1.S.PublicId, Title = "hi", Body = "across" }, CommandType.SendMail);
            Assert.Equal("OtherKingdom", LastAck(b.C).Reason);
            Send(a2.S, new SendMailCommand { CommandId = "km2", PlayerId = a1.S.PublicId, Title = "hi", Body = "inside" }, CommandType.SendMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(a2.C).Result);
        }
    }
}
