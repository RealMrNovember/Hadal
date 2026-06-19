using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.Events
{
    [CreateAssetMenu(fileName = "GenericEventChannel", menuName = "Hadal/Events/Generic Event Channel")]
    public class GenericEventChannelSO<T> : ScriptableObject
    {
        private readonly List<IGameEventListener<T>> _listeners = new();

        public void Raise(T payload)
        {
            for (var i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised(payload);
        }

        public void RegisterListener(IGameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(IGameEventListener<T> listener)
            => _listeners.Remove(listener);
    }
}
