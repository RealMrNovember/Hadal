using System.Net.WebSockets;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hadal.Game.Tests
{
    /// <summary>Real Kestrel pipeline + WebSocket + in-memory store: handshake → snapshot → place → ack.</summary>
    public sealed class GatewayEndToEndTests : IClassFixture<GatewayEndToEndTests.GatewayFactory>
    {
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
            }
        }

        [Fact]
        public async Task Handshake_Snapshot_Place_Ack()
        {
            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth("e2e-device-1", SchemaVersion.Current));

            var ack = await ReceiveAsync(socket);
            Assert.Equal(ServerMessageKind.HandshakeAck, ack.Kind);
            Assert.True(ack.HandshakeAck!.Accepted);

            var snapshot = await ReceiveAsync(socket);
            Assert.Equal(ServerMessageKind.StateSnapshot, snapshot.Kind);
            Assert.Equal(500, snapshot.Snapshot().Resources.Amount(ResourceType.Oxygen));

            await SendAsync(socket, TestSupport.Place("oxygen_plant", 1, 0, "e2e-cmd"));

            var delta = (await ReceiveAsync(socket)).Delta();
            Assert.Equal(CommandResultCode.Accepted, delta.Ack()!.Result);
            Assert.Single(delta.CreatedBuildings());
        }

        [Fact]
        public async Task OldSchema_IsRejected()
        {
            using var socket = await ConnectAsync();
            await SendAsync(socket, Auth("e2e-device-2", SchemaVersion.MinSupported - 1));

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

        private async Task<WebSocket> ConnectAsync()
        {
            var client = _factory.Server.CreateWebSocketClient();
            return await client.ConnectAsync(new Uri(_factory.Server.BaseAddress, "gateway"), CancellationToken.None);
        }

        private static CommandEnvelope Auth(string deviceId, uint schema)
        {
            var auth = new ClientAuthCommand { CommandId = "auth", ClientSchemaVersion = schema, ClientVersion = "test", DeviceId = deviceId };
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
