using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Hadal.Game.PushNotifications;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Phone notifications (docs/36): the player's choices and push devices persist in the player document, and server
    /// push respects them: never to an online player, never against the choices or in quiet hours, at most once per
    /// category in the coalescing window, and nothing at all while push is not configured.
    /// </summary>
    public sealed class NotificationTests
    {
        private sealed class FakeSender : IPushSender
        {
            public List<PushMessage> Sent { get; } = new();
            public HashSet<string> Dead { get; } = new();
            public bool Enabled { get; set; } = true;

            public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken)
            {
                if (Dead.Contains(message.Token))
                    return Task.FromResult(PushSendResult.InvalidToken);
                Sent.Add(message);
                return Task.FromResult(PushSendResult.Sent);
            }
        }

        private readonly FakeClock _clock = new() { UtcNow = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero) };
        private readonly GameWorld _world;
        private readonly FakeSender _sender = new();

        public NotificationTests()
        {
            _world = TestSupport.World(_clock);
            _world.Push.Sender = _sender;
            _world.Push.ManualPump = true;
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private void SetPrefs(PlayerSession session, NotificationPrefsDto prefs) =>
            Send(session, new SetNotificationPrefsCommand { CommandId = Id(), Prefs = prefs }, CommandType.SetNotificationPrefs);

        private void Register(PlayerSession session, string device, string token) =>
            Send(session, new RegisterPushTokenCommand { CommandId = Id(), DeviceId = device, Token = token, Platform = "android" },
                CommandType.RegisterPushToken);

        private void Whisper(PlayerSession from, PlayerSession to, string text = "hi")
        {
            _clock.Advance(TimeSpan.FromSeconds(3)); // past the chat interval
            Send(from, new SendChatCommand { CommandId = Id(), Channel = (int)ChatChannel.Private, TargetId = to.PublicId, Text = text },
                CommandType.SendChat);
        }

        /// <summary>A sender and an offline receiver with a phone (Turkish, default choices unless given).</summary>
        private async Task<(PlayerSession Sender, PlayerSession Receiver)> SenderAndOfflineReceiver(NotificationPrefsDto? prefs = null)
        {
            var a = await Connect("na-" + Id()[..8]);
            var b = await Connect("nb-" + Id()[..8]);
            SetPrefs(b.Session, prefs ?? new NotificationPrefsDto { Language = "tr" });
            Register(b.Session, "phone-1", "token-b");
            await _world.DisconnectAsync(b.Session, b.Connection, default);
            Assert.False(_world.IsOnline(b.Session.IdentityKey));
            return (a.Session, b.Session);
        }

        [Fact]
        public async Task Choices_and_phones_persist_and_come_back_in_the_snapshot()
        {
            var (session, connection) = await Connect("notify-persist");
            var first = connection.Messages.First(m => m.Snapshot != null).Snapshot();
            Assert.NotNull(first.Notifications);
            Assert.False(first.Notifications!.Saved); // never changed: all on
            Assert.False(first.Notifications.Off);

            var prefs = new NotificationPrefsDto
            {
                DisabledMask = 1 << (int)NotificationCategory.Shield | 1 << (int)NotificationCategory.Chat,
                QuietHours = true, QuietStartMinute = 23 * 60, QuietEndMinute = 8 * 60, UtcOffsetMinutes = 180, Language = "tr"
            };
            SetPrefs(session, prefs);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var change = connection.Messages.Last(m => m.Delta != null).Delta().Changes.Single(c => c.EntityId == EntityIds.NotificationPrefs);
            Assert.True(TestSupport.Serializer.Deserialize<NotificationPrefsDto>(change.Payload).SameChoices(prefs));

            Register(session, "phone-1", "token-1");
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            await _world.DisconnectAsync(session, connection, default);

            var (again, connection2) = await Connect("notify-persist");
            var snapshot = connection2.Messages.First(m => m.Snapshot != null).Snapshot();
            Assert.True(snapshot.Notifications!.Saved);
            Assert.True(snapshot.Notifications.SameChoices(prefs));
            Assert.Equal(180, snapshot.Notifications.UtcOffsetMinutes);
            Assert.Equal(new[] { "token-1" }, again.TakePushProfile().Tokens);
        }

        [Fact]
        public async Task A_phone_replaces_its_old_token_and_only_the_newest_five_phones_are_kept()
        {
            var (session, connection) = await Connect("notify-devices");
            Register(session, "phone-1", "old");
            Register(session, "phone-1", "new");
            Assert.Equal(new[] { "new" }, session.TakePushProfile().Tokens);

            for (var i = 2; i <= 7; i++)
                Register(session, "phone-" + i, "t" + i);
            var tokens = session.TakePushProfile().Tokens;
            Assert.Equal(NotificationRules.MaxDevices, tokens.Count);
            Assert.DoesNotContain("new", tokens);
            Assert.Contains("t7", tokens);

            Register(session, "phone-7", string.Empty); // the phone signs out of push
            Assert.DoesNotContain("t7", session.TakePushProfile().Tokens);

            Send(session, new RegisterPushTokenCommand { CommandId = Id(), DeviceId = string.Empty, Token = "x" }, CommandType.RegisterPushToken);
            Assert.Equal(CommandResultCode.Rejected, LastAck(connection).Result);
        }

        [Fact]
        public void Choices_are_kept_inside_the_rules()
        {
            var clean = NotificationRules.Sanitize(new NotificationPrefsDto
            {
                DisabledMask = -1, QuietStartMinute = 1500, QuietEndMinute = -60, UtcOffsetMinutes = 5000, Language = "much-too-long-code"
            });
            Assert.Equal((1 << NotificationRules.CategoryCount) - 1, clean.DisabledMask);
            Assert.Equal(60, clean.QuietStartMinute);
            Assert.Equal(23 * 60, clean.QuietEndMinute);
            Assert.Equal(14 * 60, clean.UtcOffsetMinutes);
            Assert.Equal(string.Empty, clean.Language);

            var quiet = new NotificationPrefsDto { QuietHours = true, QuietStartMinute = 23 * 60, QuietEndMinute = 8 * 60 };
            Assert.True(NotificationRules.InQuietHours(quiet, 23 * 60 + 30));
            Assert.True(NotificationRules.InQuietHours(quiet, 7 * 60));
            Assert.False(NotificationRules.InQuietHours(quiet, 8 * 60));
            Assert.False(NotificationRules.InQuietHours(quiet, 12 * 60));
            Assert.False(NotificationRules.InQuietHours(null, 0));
        }

        [Fact]
        public async Task An_offline_player_gets_a_push_in_their_language()
        {
            var (a, b) = await SenderAndOfflineReceiver();
            Whisper(a, b);
            await _world.Push.PumpAsync();

            var push = Assert.Single(_sender.Sent);
            Assert.Equal("token-b", push.Token);
            Assert.Equal("Yeni mesaj", push.Title);
            Assert.Contains(a.DisplayName, push.Body);
            Assert.Equal(NotificationRules.ChannelId(NotificationCategory.Chat), push.ChannelId);
            Assert.Equal("chat", push.Screen);
        }

        [Fact]
        public async Task An_online_player_is_never_pushed()
        {
            var a = await Connect("notify-online-a");
            var b = await Connect("notify-online-b");
            Register(b.Session, "phone-1", "token-b");
            Whisper(a.Session, b.Session);
            _world.Push.Notify(b.Session.PublicId, PushKind.AttackIncoming, "x");
            await _world.Push.PumpAsync();
            Assert.Empty(_sender.Sent);
        }

        [Fact]
        public async Task Switched_off_categories_and_the_master_switch_are_respected()
        {
            var (a, b) = await SenderAndOfflineReceiver(new NotificationPrefsDto { DisabledMask = 1 << (int)NotificationCategory.Chat });
            Whisper(a, b);
            _world.Push.Notify(b.PublicId, PushKind.Mail, "Overseer");
            await _world.Push.PumpAsync();
            Assert.Equal("hadal_mail", Assert.Single(_sender.Sent).ChannelId);

            var (c, d) = await SenderAndOfflineReceiver(new NotificationPrefsDto { Off = true });
            _sender.Sent.Clear();
            Whisper(c, d);
            _world.Push.Notify(d.PublicId, PushKind.AttackIncoming, "x");
            await _world.Push.PumpAsync();
            Assert.Empty(_sender.Sent);
        }

        [Fact]
        public async Task Quiet_hours_hold_pushes_back()
        {
            // 12:00 UTC is 15:00 at UTC+3: quiet 14:00–16:00 covers it.
            var (a, b) = await SenderAndOfflineReceiver(new NotificationPrefsDto
            {
                QuietHours = true, QuietStartMinute = 14 * 60, QuietEndMinute = 16 * 60, UtcOffsetMinutes = 180
            });
            Whisper(a, b);
            await _world.Push.PumpAsync();
            Assert.Empty(_sender.Sent);

            _clock.Advance(TimeSpan.FromHours(1.5)); // 16:30 local
            Whisper(a, b);
            await _world.Push.PumpAsync();
            Assert.Single(_sender.Sent);
        }

        [Fact]
        public async Task One_push_per_category_in_the_coalescing_window()
        {
            var (a, b) = await SenderAndOfflineReceiver();
            Whisper(a, b, "one");
            Whisper(a, b, "two");
            _world.Push.Notify(b.PublicId, PushKind.Mail, "Overseer"); // another category still goes
            await _world.Push.PumpAsync();
            Assert.Equal(2, _sender.Sent.Count);

            _clock.Advance(NotificationRules.CoalesceWindow);
            Whisper(a, b, "three");
            await _world.Push.PumpAsync();
            Assert.Equal(3, _sender.Sent.Count);
        }

        [Fact]
        public async Task Mail_to_an_offline_player_is_pushed_and_a_dead_token_is_dropped()
        {
            var (_, b) = await SenderAndOfflineReceiver();
            await _world.SendMailAsync(b.IdentityKey, new Hadal.Game.Mailbox.MailDraft(Hadal.Game.Mailbox.MailKinds.Admin, "Overseer", "Hi", "Body", new Hadal.Game.Mailbox.MailAttachments()), default);
            await _world.Push.PumpAsync();
            var push = Assert.Single(_sender.Sent);
            Assert.Equal("Yeni posta", push.Title);
            Assert.Contains("Overseer", push.Body);

            _sender.Dead.Add("token-b");
            _clock.Advance(NotificationRules.CoalesceWindow);
            _world.Push.Notify(b.PublicId, PushKind.AllianceHelp, "x");
            await _world.Push.PumpAsync();
            _clock.Advance(NotificationRules.CoalesceWindow);
            _sender.Dead.Clear();
            _world.Push.Notify(b.PublicId, PushKind.ChatMention, "x");
            await _world.Push.PumpAsync();
            Assert.Single(_sender.Sent); // the dead token is not tried again
        }

        [Fact]
        public async Task Nothing_is_queued_while_push_is_not_configured()
        {
            var (a, b) = await SenderAndOfflineReceiver();
            _sender.Enabled = false;
            Whisper(a, b);
            await _world.Push.PumpAsync();
            Assert.Empty(_sender.Sent);
            Assert.Equal(0, _world.Push.SentCount);
        }

        [Fact]
        public void Mentions_match_whole_names_after_an_at_sign()
        {
            var mates = new[] { ("1", "Mara"), ("2", "Mar"), ("3", "Deep Diver") };
            Assert.Equal(new[] { "1" }, PushService.Mentions("hey @mara come", mates));
            Assert.Equal(new[] { "2", "3" }, PushService.Mentions("@Mar and @Deep Diver!", mates));
            Assert.Empty(PushService.Mentions("mara without at", mates));
        }

        [Fact]
        public void Every_language_has_every_push_text()
        {
            Assert.Equal(18, PushTexts.Languages.Count);
            foreach (var language in PushTexts.Languages)
            foreach (var kind in Enum.GetValues<PushKind>())
            {
                var (title, body) = PushTexts.For(language, kind, "Nemo");
                Assert.False(string.IsNullOrWhiteSpace(title), $"{language} {kind}");
                Assert.False(string.IsNullOrWhiteSpace(body), $"{language} {kind}");
                if (PushTexts.For("en", kind, "{0}").Body.Contains("{0}"))
                    Assert.Contains("Nemo", body);
            }

            Assert.Equal(PushTexts.For("en", PushKind.Mail, "x"), PushTexts.For("xx", PushKind.Mail, "x"));
        }

        [Fact]
        public void The_fcm_sender_signs_with_the_service_account_and_builds_a_v1_message()
        {
            using var rsa = RSA.Create(2048);
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, new JsonObject
                {
                    ["type"] = "service_account", ["project_id"] = "hadal-513122", ["client_email"] = "push@hadal-513122.iam.gserviceaccount.com",
                    ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(), ["token_uri"] = "https://oauth2.googleapis.com/token"
                }.ToJsonString());
                using var sender = FcmPushSender.TryCreate(path, "hadal-513122", NullLogger.Instance);
                Assert.NotNull(sender);

                var jwt = sender!.Assertion(_clock.UtcNow).Split('.');
                Assert.Equal(3, jwt.Length);
                var claims = JsonNode.Parse(Encoding.UTF8.GetString(FromBase64Url(jwt[1])))!;
                Assert.Equal("push@hadal-513122.iam.gserviceaccount.com", claims["iss"]!.GetValue<string>());
                Assert.Equal("https://www.googleapis.com/auth/firebase.messaging", claims["scope"]!.GetValue<string>());
                Assert.True(rsa.VerifyData(Encoding.ASCII.GetBytes(jwt[0] + "." + jwt[1]), FromBase64Url(jwt[2]),
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

                var body = FcmPushSender.Body(new PushMessage("tok", "T", "B", "hadal_attack", "base", "hadal_attackincoming"));
                Assert.Equal("tok", body["message"]!["token"]!.GetValue<string>());
                Assert.Equal("hadal_attack", body["message"]!["android"]!["notification"]!["channel_id"]!.GetValue<string>());
                Assert.Equal(NotificationRules.SmallIcon, body["message"]!["android"]!["notification"]!["icon"]!.GetValue<string>());
                Assert.Equal("base", body["message"]!["data"]!["screen"]!.GetValue<string>());
            }
            finally
            {
                File.Delete(path);
            }

            Assert.Null(FcmPushSender.TryCreate(null, "hadal-513122", NullLogger.Instance));
            Assert.Null(FcmPushSender.TryCreate("/nonexistent/key.json", "hadal-513122", NullLogger.Instance));
        }

        private static byte[] FromBase64Url(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s + new string('=', (4 - s.Length % 4) % 4));
        }
    }
}
