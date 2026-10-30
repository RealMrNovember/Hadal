using Hadal.Game.Social;
using Hadal.Game.World;
using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>The alliance hub's header (docs/31 §1): the officers' announcement and the alliance language.</summary>
    public sealed class AllianceHubTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private readonly GameWorld _world;

        public AllianceHubTests()
        {
            _world = new GameWorld(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), _store, new InMemoryWorldStore(), _clock,
                TestSupport.Serializer, NullLogger<GameWorld>.Instance);
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

        private static AllianceDto Mine(RecordingConnection connection) =>
            TestSupport.Serializer.Deserialize<AllianceDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance && c.Kind != EntityChangeKind.Delete).Payload);

        private async Task<((PlayerSession S, RecordingConnection C) Leader, (PlayerSession S, RecordingConnection C) Member)> Alliance(string name)
        {
            var leader = await Connect(name + "-l");
            Send(leader.S, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant(), Language = "it" },
                CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leader.C).Result);
            var member = await Connect(name + "-m");
            Send(member.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = Mine(leader.C).Id }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(member.C).Result);
            return (leader, member);
        }

        [Fact]
        public async Task The_founders_language_comes_with_the_new_alliance()
        {
            var (leader, _) = await Alliance("hublang");
            Assert.Equal("it", Mine(leader.C).Language);
            Assert.Equal(string.Empty, Mine(leader.C).Announcement);
        }

        [Fact]
        public async Task Diplomacy_officers_write_the_announcement_and_every_member_sees_it()
        {
            var (leader, member) = await Alliance("hubnote");
            Send(member.S, new SetAllianceInfoCommand { CommandId = Id(), Announcement = "Saldırı 20:00'de" }, CommandType.SetAllianceInfo);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(member.C).Reason);

            // An officer of another duty may not (docs/29 §2.3); the diplomacy duty may.
            Send(leader.S, new SetMemberRankCommand { CommandId = Id(), MemberId = member.S.PublicId, Rank = AlliancePermissions.Officer, Role = "war" },
                CommandType.SetMemberRank);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leader.C).Result);
            Send(member.S, new SetAllianceInfoCommand { CommandId = Id(), Announcement = "Saldırı 20:00'de" }, CommandType.SetAllianceInfo);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(member.C).Reason);

            Send(leader.S, new SetMemberRankCommand { CommandId = Id(), MemberId = member.S.PublicId, Rank = AlliancePermissions.Officer, Role = "diplomacy" },
                CommandType.SetMemberRank);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leader.C).Result);
            Send(member.S, new SetAllianceInfoCommand { CommandId = Id(), Announcement = "  Saldırı\t20:00'de\r\nHerkes hazır olsun  ", Language = "de" },
                CommandType.SetAllianceInfo);
            Assert.Equal(CommandResultCode.Accepted, LastAck(member.C).Result);

            var seen = Mine(leader.C);
            Assert.Equal("Saldırı 20:00'de\nHerkes hazır olsun", seen.Announcement);
            Assert.Equal("it", seen.Language); // an officer cannot change the language
        }

        [Fact]
        public async Task The_leader_sets_the_language_and_long_announcements_are_cut()
        {
            var (leader, member) = await Alliance("hubcut");
            Send(leader.S, new SetAllianceInfoCommand { CommandId = Id(), Announcement = new string('x', 900), Language = "zh-Hans" }, CommandType.SetAllianceInfo);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leader.C).Result);
            var seen = Mine(member.C);
            Assert.Equal(SocialRules.MaxAnnouncement, seen.Announcement.Length);
            Assert.Equal("zh-Hans", seen.Language);

            Send(leader.S, new SetAllianceInfoCommand { CommandId = Id(), Announcement = "ok", Language = "<b>" }, CommandType.SetAllianceInfo);
            Assert.Equal("zh-Hans", Mine(member.C).Language); // not a language code: unchanged
        }

        [Fact]
        public async Task The_announcement_and_language_are_stored()
        {
            var (leader, _) = await Alliance("hubsave");
            Send(leader.S, new SetAllianceInfoCommand { CommandId = Id(), Announcement = "Kalıcı", Language = "tr" }, CommandType.SetAllianceInfo);
            await _world.Social.DrainAsyncForTests();

            var (alliances, _) = await _store.LoadAlliancesAsync(default);
            var stored = alliances.Single(a => a.Name == "House hubsave");
            Assert.Equal(("Kalıcı", "tr"), (stored.Announcement, stored.Language));
        }

        [Fact]
        public void Language_codes_and_announcements_are_checked()
        {
            Assert.True(SocialRules.IsLanguageCode("zh-Hant"));
            Assert.False(SocialRules.IsLanguageCode("<x>"));
            Assert.False(SocialRules.IsLanguageCode(""));
            Assert.Equal("a b\nc", SocialRules.CleanAnnouncement(" a\tb \r\n c "));
        }
    }
}
