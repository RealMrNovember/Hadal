using System;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Binary WebSocket transport for Protobuf gameplay frames.
    /// Real socket library integration deferred to a later sprint.
    /// </summary>
    public interface IWebSocketClient
    {
        bool IsConnected { get; }

        event Action<byte[]> OnMessageReceived;

        void Connect(string url);
        void Send(byte[] data);
        void Disconnect();
    }
}
