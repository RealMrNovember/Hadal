using System.Net.WebSockets;
using Hadal.Game;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Hadal.Gateway.Auth;
using HADAL.Shared.Auth;
using Microsoft.Extensions.Options;

namespace Hadal.Gateway
{
    /// <summary>
    /// One WebSocket connection: schema handshake first, then commands are forwarded to the
    /// <see cref="GameWorld"/>. Wire frames are Protobuf <see cref="CommandEnvelope"/> (client → server).
    /// </summary>
    public sealed class GatewaySessionHandler
    {
        private const int MaxFrameBytes = 64 * 1024;

        private readonly INetworkSerializer _serializer;
        private readonly GameWorld _world;
        private readonly TokenService _tokens;
        private readonly Admin.IAdminStore _admin;
        private readonly GatewayOptions _options;
        private readonly ILogger<GatewaySessionHandler> _logger;

        public GatewaySessionHandler(INetworkSerializer serializer, GameWorld world, TokenService tokens, Admin.IAdminStore admin,
            IOptions<GatewayOptions> options, ILogger<GatewaySessionHandler> logger)
        {
            _serializer = serializer;
            _world = world;
            _tokens = tokens;
            _admin = admin;
            _options = options.Value;
            _logger = logger;

            if (_options.SimulatedLatencyMs > 0)
                _logger.LogWarning("Simulated latency of {Latency} ms is ON — test configuration only.", _options.SimulatedLatencyMs);
        }

        public async Task RunAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var connection = new WebSocketClientConnection(socket, _serializer, _logger, _options.SimulatedLatencyMs);
            var pump = connection.PumpAsync(cancellationToken);
            PlayerSession? session = null;

            try
            {
                while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var frame = await ReceiveFrameAsync(socket, cancellationToken);
                    if (frame == null)
                        break;

                    if (!_serializer.TryDeserialize(frame, out CommandEnvelope? envelope) || envelope == null)
                    {
                        _logger.LogWarning("Undecodable frame ({Bytes}B) — closing.", frame.Length);
                        connection.Close("bad frame");
                        break;
                    }

                    if (session == null)
                    {
                        session = await HandshakeAsync(connection, envelope, cancellationToken);
                        if (session == null)
                            break;

                        continue;
                    }

                    _world.HandleCommand(session, envelope);
                }
            }
            catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
            {
                // Client dropped; fall through to cleanup.
            }
            finally
            {
                if (session != null)
                    await _world.DisconnectAsync(session, connection, CancellationToken.None);

                connection.Close("bye");
                await pump;
            }
        }

        private async Task<PlayerSession?> HandshakeAsync(WebSocketClientConnection connection, CommandEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope.Type != CommandType.ClientAuth
                || !_serializer.TryDeserialize(envelope.Payload, out ClientAuthCommand? auth)
                || auth == null)
            {
                _logger.LogWarning("First frame was {Type}, expected ClientAuth — closing.", envelope.Type);
                connection.Close("handshake required");
                return null;
            }

            var compatible = auth.ClientSchemaVersion >= SchemaVersion.MinSupported
                             && auth.ClientSchemaVersion <= SchemaVersion.Current;

            // Identity is the account id inside a token we signed — never anything the client claims.
            var token = compatible
                ? await _tokens.ValidateAccessTokenAsync(auth.AccessToken)
                : new TokenValidation(false, Guid.Empty, HandshakeRejectReasons.SchemaIncompatible);

            // Suspended accounts are refused with a reason the client shows instead of retrying.
            if (token.IsValid && await _admin.GetActiveBanAsync(token.AccountId, DateTimeOffset.UtcNow, cancellationToken) != null)
                token = new TokenValidation(false, token.AccountId, HandshakeRejectReasons.Banned);
            var accepted = token.IsValid;

            connection.Send(EnvelopeBuilder.WrapServerMessage(new HandshakeAckEnvelope
            {
                SchemaVersion = SchemaVersion.Current,
                Accepted = accepted,
                Result = accepted ? CommandResultCode.Accepted
                    : compatible ? CommandResultCode.Rejected : CommandResultCode.SchemaIncompatible,
                SessionToken = accepted ? Guid.NewGuid().ToString("N") : string.Empty,
                Reason = token.Reason
            }));

            _logger.LogInformation("Handshake {Result}{Reason}: account {AccountId}, client {ClientVersion}, schema {Schema}.",
                accepted ? "Accepted" : "Rejected", accepted ? string.Empty : $" ({token.Reason})",
                accepted ? token.AccountId.ToString("N") : "-", auth.ClientVersion, auth.ClientSchemaVersion);

            if (!accepted)
            {
                connection.Close(token.Reason);
                return null;
            }

            // Snapshot follows the ack on the same ordered outbox.
            return await _world.ConnectAsync(token.AccountId.ToString("N"), connection, cancellationToken);
        }

        private static async Task<byte[]?> ReceiveFrameAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            using var frame = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                if (result.MessageType != WebSocketMessageType.Binary)
                    return null;

                frame.Write(buffer, 0, result.Count);
                if (frame.Length > MaxFrameBytes)
                    return null;

                if (result.EndOfMessage)
                    return frame.ToArray();
            }
        }
    }
}
