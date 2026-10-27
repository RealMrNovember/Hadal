using HADAL.Shared.Protocol;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using HADAL.Shared.Auth;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hadal.Game.Tests
{
    /// <summary>Real Kestrel pipeline + HTTP auth + WebSocket + in-memory stores.</summary>
    public sealed class GatewayEndToEndTests : IClassFixture<GatewayEndToEndTests.GatewayFactory>
    {
        internal static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

        private readonly GatewayFactory _factory;

        public GatewayEndToEndTests(GatewayFactory factory)
        {
            _factory = factory;
        }

        public sealed class GatewayFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting("Game:CatalogPath", Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json"));
                builder.UseSetting("ConnectionStrings:Hadal", string.Empty);
                builder.UseSetting("Auth:JwtSecret", "test-secret-test-secret-test-secret-0123");
                builder.UseSetting("Version:MinClientVersion", "0.1.0");
            }
        }

        [Fact]
        public async Task GuestLogin_Handshake_Snapshot_Place_Ack()
        {
            var login = await GuestAsync();
            Assert.False(string.IsNullOrEmpty(login.accessToken));
            Assert.False(string.IsNullOrEmpty(login.refreshToken));

            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth(login.accessToken, SchemaVersion.Current));

            var ack = await ReceiveAsync(socket);
            Assert.Equal(ServerMessageKind.HandshakeAck, ack.Kind);
            Assert.True(ack.HandshakeAck!.Accepted);

            var snapshot = await ReceiveAsync(socket);
            Assert.Equal(ServerMessageKind.StateSnapshot, snapshot.Kind);
            Assert.Equal(600, snapshot.Snapshot().Resources.Amount(ResourceType.Oxygen));
            Assert.Contains((await ReceiveAsync(socket)).Delta().Changes, c => c.EntityId == EntityIds.ChatHistory); // social state
            Assert.Contains((await ReceiveAsync(socket)).Delta().Changes, c => c.EntityId == EntityIds.Events); // events

            await SendAsync(socket, TestSupport.Place("oxygen_plant", 1, 0, "e2e-cmd"));

            var delta = (await ReceiveAsync(socket)).Delta();
            Assert.Equal(CommandResultCode.Accepted, delta.Ack()!.Result);
            Assert.Single(delta.CreatedBuildings());
        }

        [Fact]
        public async Task SameAccount_AfterRefresh_GetsTheSamePlayer()
        {
            var login = await GuestAsync();
            await PlaceOnceAsync(login.accessToken);

            var refreshed = await RefreshAsync(login.refreshToken);
            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
            var second = (await refreshed.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
            Assert.Equal(login.accountId, second.accountId);

            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth(second.accessToken, SchemaVersion.Current));
            await ReceiveAsync(socket); // ack
            var snapshot = (await ReceiveAsync(socket)).Snapshot();
            Assert.Equal(2, snapshot.Buildings.Count); // starter reactor + the placed building
        }

        [Fact]
        public async Task RefreshToken_IsSingleUse()
        {
            var login = await GuestAsync();
            Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(login.refreshToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(login.refreshToken)).StatusCode);
        }

        [Fact]
        public async Task TwoGuests_AreDifferentPlayers()
        {
            var a = await GuestAsync();
            var b = await GuestAsync();
            Assert.NotEqual(a.accountId, b.accountId);
        }

        [Theory]
        [InlineData("", HandshakeRejectReasons.TokenMissing)]
        [InlineData("not-a-jwt", HandshakeRejectReasons.TokenInvalid)]
        public async Task BadToken_IsRejectedWithReason(string token, string reason)
        {
            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth(token, SchemaVersion.Current));

            var ack = await ReceiveAsync(socket);
            Assert.False(ack.HandshakeAck!.Accepted);
            Assert.Equal(reason, ack.HandshakeAck.Reason);
        }

        [Fact]
        public async Task OldSchema_IsRejected()
        {
            var login = await GuestAsync();
            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth(login.accessToken, SchemaVersion.MinSupported - 1));

            var ack = await ReceiveAsync(socket);
            Assert.False(ack.HandshakeAck!.Accepted);
            Assert.Equal(CommandResultCode.SchemaIncompatible, ack.HandshakeAck.Result);
        }

        [Fact]
        public async Task CommandBeforeHandshake_ClosesConnection()
        {
            using var socket = await ConnectAsync();
            await SendAsync(socket, TestSupport.Place("oxygen_plant", 1, 0));

            var buffer = new byte[1024];
            var result = await socket.ReceiveAsync(buffer, new CancellationTokenSource(5000).Token);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        }

        [Fact]
        public async Task Manifest_ServesVersionPolicy()
        {
            var manifest = await _factory.CreateClient().GetFromJsonAsync<VersionManifest>(AuthRoutes.Manifest, Json);
            Assert.Equal("0.1.0", manifest!.minClientVersion);
            Assert.Equal(SchemaVersion.Current, manifest.schemaVersion);
        }

        private async Task<AuthResponse> GuestAsync()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Guest,
                new GuestLoginRequest { deviceId = "test-device", clientVersion = "test" }, Json);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        }

        private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
            _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Refresh, new RefreshRequest { refreshToken = refreshToken }, Json);

        private async Task PlaceOnceAsync(string accessToken)
        {
            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth(accessToken, SchemaVersion.Current));
            await ReceiveAsync(socket); // ack
            await ReceiveAsync(socket); // snapshot
            await ReceiveAsync(socket); // chat, alliance, help
            await ReceiveAsync(socket); // events
            await SendAsync(socket, TestSupport.Place("oxygen_plant", 1, 0));
            Assert.Equal(CommandResultCode.Accepted, (await ReceiveAsync(socket)).Delta().Ack()!.Result);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        private async Task<WebSocket> ConnectAsync()
        {
            var client = _factory.Server.CreateWebSocketClient();
            return await client.ConnectAsync(new Uri(_factory.Server.BaseAddress, "gateway"), CancellationToken.None);
        }

        private static CommandEnvelope Auth(string accessToken, uint schema)
        {
            var auth = new ClientAuthCommand { CommandId = "auth", ClientSchemaVersion = schema, ClientVersion = "test", AccessToken = accessToken };
            return EnvelopeBuilder.WrapCommand(auth, CommandType.ClientAuth, TestSupport.Serializer.Serialize(auth));
        }

        private static Task SendAsync(WebSocket socket, CommandEnvelope envelope) =>
            socket.SendAsync(TestSupport.Serializer.Serialize(envelope), WebSocketMessageType.Binary, true, CancellationToken.None);

        private static async Task<ServerMessage> ReceiveAsync(WebSocket socket)
        {
            var buffer = new byte[64 * 1024];
            var result = await socket.ReceiveAsync(buffer, new CancellationTokenSource(5000).Token);
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            return TestSupport.Serializer.Deserialize<ServerMessage>(buffer[..result.Count]);
        }
    }
}
