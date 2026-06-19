using System;
using System.Collections.Generic;

namespace Hadal.Core.Events
{
    /// <summary>
    /// Sprint 1 stub — replaced with full LocalEventBus in a later Phase 0-R sprint.
    /// </summary>
    public sealed class LocalEventBusStub : ILocalEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

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
    }
}
