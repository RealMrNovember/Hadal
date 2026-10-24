using System.Net.WebSockets;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;

namespace Hadal.Gateway
{
    /// <summary>
    /// One WebSocket connection: schema handshake first, then the command loop.
    /// Wire frames are Protobuf <see cref="CommandEnvelope"/> (client → server).
    /// </summary>
    public sealed class GatewaySessionHandler
    {
        private const int MaxFrameBytes = 64 * 1024;

        private readonly INetworkSerializer _serializer;
        private readonly ILogger<GatewaySessionHandler> _logger;

        public GatewaySessionHandler(INetworkSerializer serializer, ILogger<GatewaySessionHandler> logger)
        {
            _serializer = serializer;
            _logger = logger;
        }

        public async Task RunAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var handshakeComplete = false;

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var frame = await ReceiveFrameAsync(socket, cancellationToken);
                if (frame == null)
                    break;

                if (!_serializer.TryDeserialize(frame, out CommandEnvelope? envelope) || envelope == null)
                {
                    _logger.LogWarning("Undecodable frame ({Bytes}B) — closing.", frame.Length);
                    await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData, "bad frame", cancellationToken);
                    break;
                }

                if (!handshakeComplete)
                {
                    handshakeComplete = await HandleHandshakeAsync(socket, envelope, cancellationToken);
                    if (!handshakeComplete)
                        break;

                    continue;
                }

                // Command validation lives in the game server (not built yet). Until then the
                // gateway accepts the connection but does not ack — the client keeps its ghost pending.
                _logger.LogInformation("Command {Type} ({CommandId}) received — no game server yet.",
                    envelope.Type, envelope.CommandId);
            }
        }

        private async Task<bool> HandleHandshakeAsync(WebSocket socket, CommandEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope.Type != CommandType.ClientAuth
                || !_serializer.TryDeserialize(envelope.Payload, out ClientAuthCommand? auth)
                || auth == null)
            {
                _logger.LogWarning("First frame was {Type}, expected ClientAuth — closing.", envelope.Type);
                await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "handshake required", cancellationToken);
                return false;
            }

            var compatible = auth.ClientSchemaVersion >= SchemaVersion.MinSupported
                             && auth.ClientSchemaVersion <= SchemaVersion.Current;

            var ack = new HandshakeAckEnvelope
            {
                SchemaVersion = SchemaVersion.Current,
                Accepted = compatible,
                Result = compatible ? CommandResultCode.Accepted : CommandResultCode.SchemaIncompatible,
                SessionToken = compatible ? Guid.NewGuid().ToString("N") : string.Empty
            };

            await SendAsync(socket, EnvelopeBuilder.WrapServerMessage(ack), cancellationToken);

            _logger.LogInformation("Handshake {Result}: device {DeviceId}, client {ClientVersion}, schema {Schema}.",
                ack.Result, auth.DeviceId, auth.ClientVersion, auth.ClientSchemaVersion);

            if (!compatible)
                await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "schema incompatible", cancellationToken);

            return compatible;
        }

        // Server → client frames are always ServerMessage; never send a bare envelope.
        private Task SendAsync(WebSocket socket, ServerMessage message, CancellationToken cancellationToken) =>
            socket.SendAsync(_serializer.Serialize(message), WebSocketMessageType.Binary, true, cancellationToken);

        private static async Task<byte[]?> ReceiveFrameAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            using var frame = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "bye", cancellationToken);
                    return null;
                }

                if (result.MessageType != WebSocketMessageType.Binary)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.InvalidMessageType, "binary only", cancellationToken);
                    return null;
                }

                frame.Write(buffer, 0, result.Count);
                if (frame.Length > MaxFrameBytes)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.MessageTooBig, "frame too large", cancellationToken);
                    return null;
                }

                if (result.EndOfMessage)
                    return frame.ToArray();
            }
        }

        private static Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string reason, CancellationToken cancellationToken)
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                return socket.CloseAsync(status, reason, cancellationToken);

            return Task.CompletedTask;
        }
    }
}
