using System;

namespace Hadal.Core.Events
{
    /// <summary>
    /// Server-authoritative replication events decoded from Protobuf wire messages.
    /// Presentation layer MUST NOT subscribe — use <see cref="ILocalEventBus"/> via translator.
    /// </summary>
    public interface INetworkEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T payload);
    }
}
