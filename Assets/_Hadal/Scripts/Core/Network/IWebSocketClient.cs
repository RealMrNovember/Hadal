using System;

namespace Hadal.Core.Network
{
    public enum WebSocketConnectionState
    {
        Closed = 0,
        Connecting = 1,
        Open = 2,
        Closing = 3
    }

    /// <summary>
    /// Binary WebSocket transport for Protobuf gameplay frames.
    /// </summary>
    public interface IWebSocketClient
    {
        bool IsConnected { get; }
        WebSocketConnectionState ConnectionState { get; }

        event Action OnConnected;
        event Action OnDisconnected;
        event Action<byte[]> OnMessageReceived;

        void Connect(string url);
        void Send(byte[] data);
        void Disconnect();
        void DispatchMessageQueue();
    }
}
