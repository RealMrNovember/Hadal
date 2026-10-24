using System.Net.WebSockets;
using System.Threading.Channels;
using Hadal.Game;
using HADAL.Shared.DTOs;
using HADAL.Shared.Serialization;

namespace Hadal.Gateway
{
    /// <summary>
    /// <see cref="IClientConnection"/> over a WebSocket. WebSocket.SendAsync is not safe for
    /// concurrent callers, so sends are queued and written by a single pump.
    /// </summary>
    public sealed class WebSocketClientConnection : IClientConnection
    {
        // A client that stops reading must not grow server memory without bound.
        private const int MaxQueuedFrames = 256;

        private readonly WebSocket _socket;
        private readonly INetworkSerializer _serializer;
        private readonly ILogger _logger;
        private readonly TimeSpan _latency;
        private readonly Channel<(byte[] Frame, DateTime Due)> _outbox = Channel.CreateBounded<(byte[] Frame, DateTime Due)>(new BoundedChannelOptions(MaxQueuedFrames)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropWrite
        });

        private string _closeReason = "bye";

        public WebSocketClientConnection(WebSocket socket, INetworkSerializer serializer, ILogger logger, int simulatedLatencyMs = 0)
        {
            _socket = socket;
            _serializer = serializer;
            _logger = logger;
            _latency = TimeSpan.FromMilliseconds(Math.Max(0, simulatedLatencyMs));
        }

        public string ConnectionId { get; } = Guid.NewGuid().ToString("N");

        public void Send(ServerMessage message)
        {
            if (!_outbox.Writer.TryWrite((_serializer.Serialize(message), DateTime.UtcNow + _latency)))
            {
                _logger.LogWarning("Connection {ConnectionId}: outbox full or closed — closing.", ConnectionId);
                Close("client not reading");
            }
        }

        public void Close(string reason)
        {
            _closeReason = reason;
            _outbox.Writer.TryComplete();
        }

        /// <summary>Writes queued frames until the outbox completes, then closes the socket.</summary>
        public async Task PumpAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var (frame, due) in _outbox.Reader.ReadAllAsync(cancellationToken))
                {
                    // Each frame waits until its own due time, so simulated latency shifts the
                    // stream without stretching the gaps between frames.
                    var wait = due - DateTime.UtcNow;
                    if (wait > TimeSpan.Zero)
                        await Task.Delay(wait, cancellationToken);

                    if (_socket.State != WebSocketState.Open)
                        break;

                    await _socket.SendAsync(frame, WebSocketMessageType.Binary, true, cancellationToken);
                }

                if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, _closeReason, CancellationToken.None);
            }
            catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
            {
                // Client went away; nothing left to deliver.
            }
        }
    }
}
