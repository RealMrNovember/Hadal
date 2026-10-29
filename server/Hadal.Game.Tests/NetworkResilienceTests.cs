using System.Net.Http.Json;
using System.Net.WebSockets;
using HADAL.Shared.Auth;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Roadmap §1, research §14 "Ağ", through the real gateway (Kestrel, WebSocket, auth): a connection dropped mid-command,
    /// a command re-sent after reconnecting and a duplicate envelope after a timeout never charge or pay twice.
    /// </summary>
    public sealed class NetworkResilienceTests : IClassFixture<GatewayEndToEndTests.GatewayFactory>
    {
        private readonly GatewayEndToEndTests.GatewayFactory _factory;

        public NetworkResilienceTests(GatewayEndToEndTests.GatewayFactory factory)
        {
            _factory = factory;
        }

        private GameWorld World => _factory.Services.GetRequiredService<GameWorld>();

        private async Task<AuthResponse> GuestAsync()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Guest,
                new GuestLoginRequest { deviceId = "net-" + Guid.NewGuid().ToString("N"), clientVersion = "test" }, GatewayEndToEndTests.Json);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(GatewayEndToEndTests.Json))!;
        }

        /// <summary>Opens a socket, passes the handshake and reads the snapshot; the login pushes after it are left unread.</summary>
        private async Task<(WebSocket Socket, StateSnapshot Snapshot)> OpenAsync(string accessToken)
        {
            var socket = await _factory.Server.CreateWebSocketClient().ConnectAsync(new Uri(_factory.Server.BaseAddress, "gateway"), CancellationToken.None);
            var auth = new ClientAuthCommand { CommandId = "auth", ClientSchemaVersion = SchemaVersion.Current, ClientVersion = "test", AccessToken = accessToken };
            await SendAsync(socket, EnvelopeBuilder.WrapCommand(auth, CommandType.ClientAuth, TestSupport.Serializer.Serialize(auth)));
            Assert.True((await ReceiveAsync(socket)).HandshakeAck!.Accepted);
            return (socket, (await ReceiveAsync(socket)).Snapshot());
        }

        private static Task SendAsync(WebSocket socket, CommandEnvelope envelope) =>
            socket.SendAsync(TestSupport.Serializer.Serialize(envelope), WebSocketMessageType.Binary, true, CancellationToken.None);

        private static async Task<ServerMessage> ReceiveAsync(WebSocket socket)
        {
            var buffer = new byte[256 * 1024];
            var count = 0;
            using var timeout = new CancellationTokenSource(10_000);
            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), timeout.Token);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                count += result.Count;
                if (result.EndOfMessage)
                    return TestSupport.Serializer.Deserialize<ServerMessage>(buffer[..count]);
            }
        }

        /// <summary>Reads until the answer to <paramref name="commandId"/> arrives; skips everything else.</summary>
        private static async Task<CommandAckDto> AckAsync(WebSocket socket, string commandId)
        {
            while (true)
            {
                var message = await ReceiveAsync(socket);
                if (message.Delta == null)
                    continue;
                var ack = message.Delta().Changes.Where(c => c.EntityId == EntityIds.Command + commandId)
                    .Select(c => TestSupport.Serializer.Deserialize<CommandAckDto>(c.Payload)).FirstOrDefault();
                if (ack != null)
                    return ack;
            }
        }

        private static CommandEnvelope TenPull(string commandId)
        {
            var pull = new ExecuteGachaCommand { CommandId = commandId, ClientSequence = 1, BannerId = HeroRules.PodBanner, PullCount = 10 };
            return EnvelopeBuilder.WrapCommand(pull, CommandType.ExecuteGacha, TestSupport.Serializer.Serialize(pull));
        }

        /// <summary>The player exists once it connected; then it gets Hadalite for the pulls.</summary>
        private async Task GrantAsync(string accountId) =>
            Assert.True(await World.GrantAsync(accountId, new Dictionary<ResourceType, long> { [ResourceType.Hadalite] = 1000 }, default));

        private async Task<long> HadaliteAsync(string accountId) =>
            (await World.DescribeAsync(accountId, default))!.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;

        private long TenPullPrice => World.Catalog.Catalog.PodTenCost.Single(c => c.Key == ResourceType.Hadalite).Value;

        private async Task WaitUntilAsync(Func<Task<bool>> condition)
        {
            for (var i = 0; i < 100 && !await condition(); i++)
                await Task.Delay(50);
        }

        [Fact]
        public async Task A_command_resent_after_the_connection_dropped_is_charged_once()
        {
            var login = await GuestAsync();
            var (socket, _) = await OpenAsync(login.accessToken);
            await GrantAsync(login.accountId);
            var start = await HadaliteAsync(login.accountId);

            // The command leaves; the connection dies before the answer is read.
            var pull = TenPull("net-drop-" + Guid.NewGuid().ToString("N"));
            await SendAsync(socket, pull);
            socket.Abort();
            await WaitUntilAsync(async () => !World.IsOnline(login.accountId) && await HadaliteAsync(login.accountId) < start);

            // The client reconnects and, not knowing whether it arrived, sends the same command again.
            var (second, _) = await OpenAsync(login.accessToken);
            await SendAsync(second, pull);
            var ack = await AckAsync(second, pull.CommandId);
            Assert.Equal(CommandResultCode.Accepted, ack.Result);
            Assert.Equal(start - TenPullPrice, await HadaliteAsync(login.accountId));
            await second.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        [Fact]
        public async Task A_duplicate_envelope_after_a_timeout_is_answered_twice_and_runs_once()
        {
            var login = await GuestAsync();
            var (socket, _) = await OpenAsync(login.accessToken);
            await GrantAsync(login.accountId);
            var start = await HadaliteAsync(login.accountId);

            var pull = TenPull("net-dup-" + Guid.NewGuid().ToString("N"));
            await SendAsync(socket, pull);
            await SendAsync(socket, pull); // the client timed out waiting and sent it again
            var first = await AckAsync(socket, pull.CommandId);
            var second = await AckAsync(socket, pull.CommandId);
            Assert.Equal(CommandResultCode.Accepted, first.Result);
            Assert.Equal((first.Result, first.Reason), (second.Result, second.Reason));
            Assert.Equal(start - TenPullPrice, await HadaliteAsync(login.accountId));
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        [Fact]
        public async Task The_same_command_on_an_old_and_a_new_socket_runs_once()
        {
            var login = await GuestAsync();
            var (old, _) = await OpenAsync(login.accessToken);
            var (fresh, _) = await OpenAsync(login.accessToken); // the old socket is replaced but may still deliver
            await GrantAsync(login.accountId);
            var start = await HadaliteAsync(login.accountId);

            var pulls = Enumerable.Range(0, 3).Select(i => TenPull("net-both-" + i + "-" + Guid.NewGuid().ToString("N"))).ToList();
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    foreach (var p in pulls)
                        await SendSafeAsync(old, p);
                }),
                Task.Run(async () =>
                {
                    foreach (var p in pulls)
                        await SendAsync(fresh, p);
                }));

            // Every command is answered on the live socket (a copy may be answered twice: once run, once replayed).
            var answers = new Dictionary<string, CommandResultCode>();
            while (answers.Count < pulls.Count)
            {
                var message = await ReceiveAsync(fresh);
                if (message.Delta == null)
                    continue;
                foreach (var change in message.Delta().Changes.Where(c => c.EntityId.StartsWith(EntityIds.Command, StringComparison.Ordinal)))
                {
                    var ack = TestSupport.Serializer.Deserialize<CommandAckDto>(change.Payload);
                    answers[ack.CommandId] = ack.Result;
                }
            }

            Assert.All(pulls, p => Assert.Equal(CommandResultCode.Accepted, answers[p.CommandId]));

            await WaitUntilAsync(async () => await HadaliteAsync(login.accountId) <= start - 3 * TenPullPrice);
            await Task.Delay(200);
            Assert.Equal(start - 3 * TenPullPrice, await HadaliteAsync(login.accountId));
            await fresh.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        /// <summary>The replaced socket may already be closing; a failed send there is the dropped-connection case.</summary>
        private static async Task SendSafeAsync(WebSocket socket, CommandEnvelope envelope)
        {
            try
            {
                await SendAsync(socket, envelope);
            }
            catch (Exception exception) when (exception is WebSocketException or InvalidOperationException or ObjectDisposedException)
            {
            }
        }
    }
}
