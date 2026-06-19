using System;
using Hadal.Core.Services;

namespace Hadal.Core.Events
{
    public interface IEventBus : IGameService
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T payload);
        void Clear();
    }

    public sealed class EventBusService : IEventBus
    {
        private readonly System.Collections.Generic.Dictionary<Type, Delegate> _handlers = new();

        public void Initialize() { }

        public void Shutdown() => Clear();

        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
                _handlers[type] = Delegate.Combine(existing, handler);
            else
                _handlers[type] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing))
                return;

            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
                _handlers.Remove(type);
            else
                _handlers[type] = updated;
        }

        public void Publish<T>(T payload)
        {
            if (_handlers.TryGetValue(typeof(T), out var handler))
                (handler as Action<T>)?.Invoke(payload);
        }

        public void Clear() => _handlers.Clear();
    }

    public readonly struct GameInitializedEvent { }
    public readonly struct GameShutdownEvent { }
}
