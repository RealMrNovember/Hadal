using Hadal.Game.Mailbox;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>The mailbox (docs/29 §2.1): sending, the list on login, claiming once, expiry and deleting.</summary>
    public sealed class MailTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly InMemoryMailStore _store = new();
        private GameWorld _world;

        public MailTests()
        {
            _world = NewWorld();
        }

        /// <summary>A world over the same stores: a server restart.</summary>
        private GameWorld NewWorld() => new(new CatalogRuntime(TestSupport.Catalog), _repository, new InMemorySocialStore(), new InMemoryWorldStore(),
            _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, mailStore: _store);

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        /// <summary>Logs in once so the player exists, then leaves.</summary>
        private async Task Create(string identity)
        {
            var (session, connection) = await Connect(identity);
            await _world.DisconnectAsync(session, connection, default);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static MailListDto Box(RecordingConnection connection) =>
            TestSupport.Serializer.Deserialize<MailListDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.Mail).Payload);

        private static MailDraft Gift(string title = "Hediye") => new(MailKinds.Admin, string.Empty, title, "Teşekkürler",
            MailAttachments.From(new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 5000, [ResourceType.Hadalite] = 100 },
                new Dictionary<string, int> { [VipRules.Item(500)] = 2 }));

        private static MailDraft Letter(string title = "Duyuru") => new(MailKinds.System, "HADAL", title, "Bakım yarın.", new MailAttachments());

        private static long Hadalite(PlayerSession session) =>
            session.StateForTests.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;

        private static int Held(PlayerSession session, string item) =>
            session.StateForTests.Items.TryGetValue(item, out var n) ? n : 0;

        [Fact]
        public async Task A_mail_sent_while_offline_is_listed_on_login()
        {
            await Create("mail-offline");
            Assert.True(await _world.SendMailAsync("mail-offline", Gift(), default));
            Assert.False(await _world.SendMailAsync("nobody", Gift(), default));

            var (_, connection) = await Connect("mail-offline");
            var box = Box(connection);
            var mail = Assert.Single(box.Mails);
            Assert.Equal((1, 1), (box.Unread, box.Unclaimed));
            Assert.Equal(("admin", "Hediye", "Teşekkürler"), (mail.Kind, mail.Title, mail.Body));
            Assert.False(mail.Read || mail.Claimed);
            Assert.Equal(3, mail.Attachments.Length);
            Assert.Equal(_clock.UtcNow.Add(MailRules.Lifetime).ToUnixTimeMilliseconds(), mail.ExpiresAtMs);
        }

        [Fact]
        public async Task A_mail_reaches_an_online_player_at_once_newest_first()
        {
            var (session, connection) = await Connect("mail-online");
            await _world.SendMailAsync("mail-online", Letter("Eski"), default);
            _clock.Advance(TimeSpan.FromMinutes(1));
            await _world.SendMailAsync("mail-online", Letter("Yeni"), default);

            var box = Box(connection);
            Assert.Equal(new[] { "Yeni", "Eski" }, box.Mails.Select(m => m.Title));
            Assert.Equal((2, 0), (box.Unread, box.Unclaimed));

            Send(session, new ReadMailCommand { CommandId = "r", MailId = box.Mails[1].Id }, CommandType.ReadMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(1, Box(connection).Unread);
            Assert.True(Box(connection).Mails[1].Read);
        }

        [Fact]
        public async Task Attachments_are_paid_once_only()
        {
            var (session, connection) = await Connect("mail-claim");
            await _world.SendMailAsync("mail-claim", Gift(), default);
            var id = Box(connection).Mails[0].Id;
            var hadalite = Hadalite(session);

            Send(session, new ClaimMailCommand { CommandId = "c1", MailId = id }, CommandType.ClaimMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(hadalite + 100, Hadalite(session));
            Assert.Equal(2, Held(session, VipRules.Item(500)));
            Assert.Equal(1, Held(session, Capsules.Id(ResourceType.Titanium, 5000))); // resources a capsule holds come as one
            Assert.Contains(connection.Messages.Where(m => m.Delta != null).SelectMany(m => m.Delta().Changes),
                c => c.EntityId == EntityIds.RewardReport + "c1");
            var mail = Box(connection).Mails[0];
            Assert.True(mail.Claimed && mail.Read);
            Assert.Equal(0, Box(connection).Unclaimed);

            Send(session, new ClaimMailCommand { CommandId = "c2", MailId = id }, CommandType.ClaimMail);
            Assert.Equal("AlreadyClaimed", LastAck(connection).Reason);
            Send(session, new ClaimMailCommand { CommandId = "c3" }, CommandType.ClaimMail);
            Assert.Equal("NothingToClaim", LastAck(connection).Reason);
            Assert.Equal(hadalite + 100, Hadalite(session));
            Assert.Equal(2, Held(session, VipRules.Item(500)));
        }

        [Fact]
        public async Task A_claim_stays_claimed_after_logging_in_again_and_after_a_restart()
        {
            await Create("mail-again");
            await _world.SendMailAsync("mail-again", Gift(), default);
            var (session, connection) = await Connect("mail-again");
            Send(session, new ClaimMailCommand { CommandId = "c1", MailId = Box(connection).Mails[0].Id }, CommandType.ClaimMail);
            await _world.DisconnectAsync(session, connection, default);

            (session, connection) = await Connect("mail-again");
            Assert.True(Box(connection).Mails[0].Claimed);
            Send(session, new ClaimMailCommand { CommandId = "c2" }, CommandType.ClaimMail);
            Assert.Equal("NothingToClaim", LastAck(connection).Reason);
            await _world.DisconnectAsync(session, connection, default);

            await _world.Mail.DrainAsync();
            _world = NewWorld();
            (session, connection) = await Connect("mail-again");
            Assert.True(Box(connection).Mails[0].Claimed);
            Send(session, new ClaimMailCommand { CommandId = "c3" }, CommandType.ClaimMail);
            Assert.Equal("NothingToClaim", LastAck(connection).Reason);
            Assert.Equal(2, Held(session, VipRules.Item(500))); // paid once, saved with the player
        }

        [Fact]
        public async Task Claim_all_takes_every_waiting_mail()
        {
            var (session, connection) = await Connect("mail-all");
            await _world.SendMailAsync("mail-all", Gift("Bir"), default);
            await _world.SendMailAsync("mail-all", Gift("İki"), default);
            await _world.SendMailAsync("mail-all", Letter(), default);
            var hadalite = Hadalite(session);

            Send(session, new ClaimMailCommand { CommandId = "all" }, CommandType.ClaimMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(hadalite + 200, Hadalite(session));
            Assert.Equal(4, Held(session, VipRules.Item(500)));
            var box = Box(connection);
            Assert.Equal((0, 1), (box.Unclaimed, box.Unread)); // the letter without attachments stays unread
        }

        [Fact]
        public async Task Expired_mails_disappear_with_their_attachments()
        {
            var (session, connection) = await Connect("mail-expiry");
            await _world.SendMailAsync("mail-expiry", Gift(), default);
            var id = Box(connection).Mails[0].Id;

            _clock.Advance(MailRules.Lifetime + TimeSpan.FromMinutes(1));
            _world.Tick();
            Assert.Empty(Box(connection).Mails);
            Send(session, new ClaimMailCommand { CommandId = "late", MailId = id }, CommandType.ClaimMail);
            Assert.Equal("UnknownMail", LastAck(connection).Reason);
            Assert.Equal(0, Held(session, VipRules.Item(500)));

            Assert.Empty(await _store.LoadAsync(session.PublicId, _clock.UtcNow, default));
            Assert.Equal(1, await _store.PurgeAsync(_clock.UtcNow, default));
        }

        [Fact]
        public async Task Mails_are_deleted_once_nothing_is_left_to_take()
        {
            var (session, connection) = await Connect("mail-delete");
            await _world.SendMailAsync("mail-delete", Gift(), default);
            await _world.SendMailAsync("mail-delete", Letter("Okundu"), default);
            await _world.SendMailAsync("mail-delete", Letter("Okunmadı"), default);
            var box = Box(connection);
            var gift = box.Mails.First(m => m.Title == "Hediye").Id;
            var read = box.Mails.First(m => m.Title == "Okundu").Id;

            Send(session, new DeleteMailCommand { CommandId = "d1", MailId = gift }, CommandType.DeleteMail);
            Assert.Equal("MailHasAttachments", LastAck(connection).Reason);

            Send(session, new ReadMailCommand { CommandId = "r", MailId = read }, CommandType.ReadMail);
            Send(session, new ClaimMailCommand { CommandId = "c", MailId = gift }, CommandType.ClaimMail);
            Send(session, new DeleteMailCommand { CommandId = "d2" }, CommandType.DeleteMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(new[] { "Okunmadı" }, Box(connection).Mails.Select(m => m.Title));

            var last = Box(connection).Mails[0].Id;
            Send(session, new DeleteMailCommand { CommandId = "d3", MailId = last }, CommandType.DeleteMail);
            Assert.Empty(Box(connection).Mails);
            Send(session, new DeleteMailCommand { CommandId = "d4", MailId = last }, CommandType.DeleteMail);
            Assert.Equal("UnknownMail", LastAck(connection).Reason);

            await _world.Mail.DrainAsync();
            Assert.Empty(await _store.LoadAsync(session.PublicId, _clock.UtcNow, default));
        }

        [Fact]
        public async Task A_mail_to_everyone_reaches_every_player()
        {
            await Create("mail-everyone-1");
            await Create("mail-everyone-2");
            var (_, connection) = await Connect("mail-everyone-3");

            Assert.Equal(3, await _world.SendMailToAllAsync(Letter("Herkese"), default));
            Assert.Equal("Herkese", Assert.Single(Box(connection).Mails).Title);

            var (_, other) = await Connect("mail-everyone-1");
            Assert.Equal("Herkese", Assert.Single(Box(other).Mails).Title);
        }

        [Fact]
        public void Attachments_keep_only_positive_amounts_and_round_trip_as_json()
        {
            var attachments = MailAttachments.From(new Dictionary<ResourceType, long> { [ResourceType.Food] = 10, [ResourceType.Alloy] = -5 },
                new Dictionary<string, int> { ["vip_100"] = 1, ["x"] = 0 });
            var copy = MailAttachments.FromJson(attachments.ToJson());
            Assert.Equal(new[] { "Food" }, copy.Amounts.Keys);
            Assert.Equal(new[] { "vip_100" }, copy.Items.Keys);
            Assert.True(new MailAttachments().IsEmpty);
            Assert.True(MailAttachments.FromJson("{}").IsEmpty);
        }
    }
}
