namespace Hadal.Core.Network
{
    /// <summary>
    /// Decodes inbound Protobuf wire frames from the WebSocket transport.
    /// </summary>
    public interface INetworkStateReceiver
    {
        void StartListening();
        void StopListening();
    }
}
