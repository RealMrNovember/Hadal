using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Mail between players (MailRules): one recipient, resources only to alliance mates within the daily allowance,
    /// Hadalite and Superalloy never, a daily spam cap, the wallet paying and the recipient claiming.
    /// </summary>
    public sealed class PlayerMailTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public PlayerMailTests()
        {
            _world = TestSupport.World(_clock);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            Fill(session);
            return (session, connection);
        }

        private static void Fill(PlayerSession session)
        {
            foreach (var type in Enum.GetValues<ResourceType>().Where(t => t != ResourceType.Unknown))
                session.StateForTests.Wallet[type] = 1_000_000;
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private static long Wallet(PlayerSession session, ResourceType type) =>
            session.StateForTests.Wallet.TryGetValue(type, out var n) ? n : 0;

        private void Mail(PlayerSession from, PlayerSession to, string title = "Selam", params (ResourceType Type, long Amount)[] resources) =>
            Send(from, new SendMailCommand
            {
                CommandId = Id(), PlayerId = to.PublicId, Title = title, Body = "Merhaba\ndünya",
                Resources = resources.Select(r => new ResourceAmountEntry { Key = r.Type, Value = r.Amount }).ToArray()
            }, CommandType.SendMail);

        /// <summary>Two players in one alliance.</summary>
        private async Task<((PlayerSession S, RecordingConnection C) A, (PlayerSession S, RecordingConnection C) B)> Mates(string name)
        {
            var a = await Connect(name + "-a");
            var b = await Connect(name + "-b");
            Send(a.Session, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(a.Connection).Result);
            var allianceId = Latest<AllianceDto>(a.Connection, EntityIds.MyAlliance).Id;
            Send(b.Session, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(b.Connection).Result);
            Fill(a.Session);
            Fill(b.Session);
            return (a, b);
        }

        [Fact]
        public void The_allowance_follows_the_table_and_grows_with_the_reactor()
        {
            Assert.Equal(100_000, MailRules.DailyAllowance(ResourceType.Titanium, 1));
            Assert.Equal(300_000, MailRules.DailyAllowance(ResourceType.Food, 15));
            Assert.Equal(500_000, MailRules.DailyAllowance(ResourceType.Biomass, 40));
            Assert.Equal(40_000, MailRules.DailyAllowance(ResourceType.Oxygen, 3));
            Assert.Equal(200_000, MailRules.DailyAllowance(ResourceType.Energy, 25));
            Assert.Equal(0, MailRules.DailyAllowance(ResourceType.Hadalite, 25));
            Assert.False(MailRules.CanSend(ResourceType.Alloy));
            Assert.Equal("a b\nc", MailRules.Clean("  a\tb\r\nc  ", 100, multiline: true));
            Assert.Equal("a b c", MailRules.Clean("a\tb\nc", 100, multiline: false));
        }

        [Fact]
        public async Task A_plain_mail_reaches_any_player_with_the_senders_name()
        {
            var (from, fromConnection) = await Connect("pm-plain-a");
            var (to, toConnection) = await Connect("pm-plain-b");

            Mail(from, to, "Merhaba");
            Assert.Equal(CommandResultCode.Accepted, LastAck(fromConnection).Result);
            var mail = Assert.Single(Latest<MailListDto>(toConnection, EntityIds.Mail).Mails);
            Assert.Equal(("player", "Merhaba", "Merhaba\ndünya", from.PublicId), (mail.Kind, mail.Title, mail.Body, mail.SenderId));
            Assert.False(string.IsNullOrEmpty(mail.Sender));
            Assert.Empty(mail.Attachments);
            Assert.Equal(MailRules.MailsPerDay - 1, Latest<MailQuotaDto>(fromConnection, EntityIds.MailQuota).MailsLeft);
        }

        [Fact]
        public async Task Bad_recipients_and_texts_are_refused()
        {
            var (from, connection) = await Connect("pm-bad-a");
            var (to, _) = await Connect("pm-bad-b");

            Mail(from, from);
            Assert.Equal("CannotMailSelf", LastAck(connection).Reason);
            Send(from, new SendMailCommand { CommandId = Id(), PlayerId = "nobody", Title = "x" }, CommandType.SendMail);
            Assert.Equal("UnknownPlayer", LastAck(connection).Reason);
            Mail(from, to, "   ");
            Assert.Equal("InvalidMailTitle", LastAck(connection).Reason);
            Send(from, new SendMailCommand { CommandId = Id(), PlayerId = to.PublicId, Title = "x", Body = new string('a', MailRules.MaxPlayerBody + 1) },
                CommandType.SendMail);
            Assert.Equal("InvalidMailBody", LastAck(connection).Reason);
            Assert.Equal(MailRules.MailsPerDay, Latest<MailQuotaDto>(connection, EntityIds.MailQuota).MailsLeft); // nothing counted
        }

        [Fact]
        public async Task Resources_go_only_to_a_mate_in_the_same_alliance()
        {
            var (a, b) = await Mates("pmally");
            var (stranger, strangerConnection) = await Connect("pm-stranger");

            Mail(a.S, stranger, "Al", (ResourceType.Titanium, 1000));
            Assert.Equal("NotSameAlliance", LastAck(a.C).Reason);
            Assert.Equal(1_000_000, Wallet(a.S, ResourceType.Titanium));

            Mail(stranger, a.S, "Al", (ResourceType.Food, 1000)); // no alliance at all
            Assert.Equal("NotSameAlliance", LastAck(strangerConnection).Reason);

            Mail(a.S, b.S, "Al", (ResourceType.Titanium, 1000));
            Assert.Equal(CommandResultCode.Accepted, LastAck(a.C).Result);
        }

        [Fact]
        public async Task Hadalite_and_superalloy_never_travel()
        {
            var (a, b) = await Mates("pmhad");
            Mail(a.S, b.S, "Al", (ResourceType.Hadalite, 10));
            Assert.Equal("ResourceNotSendable", LastAck(a.C).Reason);
            Mail(a.S, b.S, "Al", (ResourceType.Alloy, 10));
            Assert.Equal("ResourceNotSendable", LastAck(a.C).Reason);
            Assert.Equal((1_000_000L, 1_000_000L), (Wallet(a.S, ResourceType.Hadalite), Wallet(a.S, ResourceType.Alloy)));
        }

        [Fact]
        public async Task Resources_leave_the_wallet_and_arrive_as_a_claimable_attachment()
        {
            var (a, b) = await Mates("pmpay");
            Mail(a.S, b.S, "Destek", (ResourceType.Titanium, 60_000), (ResourceType.Oxygen, 10_000));
            Assert.Equal(CommandResultCode.Accepted, LastAck(a.C).Result);
            Assert.Equal((940_000L, 990_000L), (Wallet(a.S, ResourceType.Titanium), Wallet(a.S, ResourceType.Oxygen)));
            var quota = Latest<MailQuotaDto>(a.C, EntityIds.MailQuota);
            Assert.Equal(40_000, quota.Left.Single(l => l.Key == ResourceType.Titanium).Value); // 100k at reactor 1
            Assert.Equal(30_000, quota.Left.Single(l => l.Key == ResourceType.Oxygen).Value);

            var mail = Latest<MailListDto>(b.C, EntityIds.Mail).Mails.Single(m => m.Kind == "player");
            Assert.Equal(2, mail.Attachments.Length);
            Send(b.S, new ClaimMailCommand { CommandId = Id(), MailId = mail.Id }, CommandType.ClaimMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(b.C).Result);
            var items = b.S.StateForTests.Items;
            Assert.Equal(1, items[Capsules.Id(ResourceType.Titanium, 60_000)]);
            Assert.Equal(1, items[Capsules.Id(ResourceType.Oxygen, 10_000)]);
        }

        [Fact]
        public async Task Each_resource_has_its_own_daily_allowance_that_opens_again_the_next_day()
        {
            var (a, b) = await Mates("pmlim");
            Mail(a.S, b.S, "1", (ResourceType.Titanium, 100_000));
            Assert.Equal(CommandResultCode.Accepted, LastAck(a.C).Result);
            Mail(a.S, b.S, "2", (ResourceType.Titanium, 1));
            Assert.Equal("MailResourceLimit", LastAck(a.C).Reason);
            Mail(a.S, b.S, "3", (ResourceType.Food, 100_000)); // another resource, another allowance
            Assert.Equal(CommandResultCode.Accepted, LastAck(a.C).Result);
            Mail(a.S, b.S, "4", (ResourceType.Energy, 40_001));
            Assert.Equal("MailResourceLimit", LastAck(a.C).Reason);
            Assert.Equal((900_000L, 900_000L, 1_000_000L),
                (Wallet(a.S, ResourceType.Titanium), Wallet(a.S, ResourceType.Food), Wallet(a.S, ResourceType.Energy)));

            _clock.Advance(TimeSpan.FromDays(1));
            Mail(a.S, b.S, "5", (ResourceType.Titanium, 100_000));
            Assert.Equal(CommandResultCode.Accepted, LastAck(a.C).Result);
        }

        [Fact]
        public async Task A_missing_balance_refuses_the_mail_and_takes_nothing()
        {
            var (a, b) = await Mates("pmbal");
            a.S.StateForTests.Wallet[ResourceType.Biomass] = 500;
            Mail(a.S, b.S, "Al", (ResourceType.Biomass, 501));
            Assert.Equal("InsufficientResources", LastAck(a.C).Reason);
            Assert.Equal(500, Wallet(a.S, ResourceType.Biomass));
            Assert.Equal(MailRules.MailsPerDay, Latest<MailQuotaDto>(a.C, EntityIds.MailQuota).MailsLeft);
        }

        [Fact]
        public async Task Thirty_mails_a_day_then_the_spam_cap_until_tomorrow()
        {
            var (from, connection) = await Connect("pm-spam-a");
            var (to, _) = await Connect("pm-spam-b");
            for (var i = 0; i < MailRules.MailsPerDay; i++)
            {
                Mail(from, to, "m" + i);
                Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            }

            Mail(from, to, "one too many");
            Assert.Equal("MailLimitReached", LastAck(connection).Reason);
            Assert.Equal(0, Latest<MailQuotaDto>(connection, EntityIds.MailQuota).MailsLeft);

            _clock.Advance(TimeSpan.FromDays(1));
            Mail(from, to, "tomorrow");
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task The_days_usage_survives_logging_out()
        {
            var (a, b) = await Mates("pmsave");
            Mail(a.S, b.S, "1", (ResourceType.Titanium, 100_000));
            await _world.DisconnectAsync(a.S, a.C, default);

            var (again, connection) = await Connect("pmsave-a");
            Assert.Equal(0, Latest<MailQuotaDto>(connection, EntityIds.MailQuota).Left.Single(l => l.Key == ResourceType.Titanium).Value);
            Mail(again, b.S, "2", (ResourceType.Titanium, 1));
            Assert.Equal("MailResourceLimit", LastAck(connection).Reason);
        }
    }
}
