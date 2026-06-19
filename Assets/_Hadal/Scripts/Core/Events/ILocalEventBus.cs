using System;

namespace Hadal.Core.Events
{
    /// <summary>
    /// Client-local events: UI, animations, audio, camera effects.
    /// Network replication MUST NOT publish here directly.
    /// </summary>
    public interface ILocalEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T payload);
    }
}
