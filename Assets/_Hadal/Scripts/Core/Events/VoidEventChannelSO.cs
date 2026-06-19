using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Hadal.Core.Events
{
    /// <summary>
    /// Designer-friendly void event channel (Ryan Hipple pattern).
    /// </summary>
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "Hadal/Events/Void Event Channel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        private readonly List<VoidEventListener> _listeners = new();

        public void Raise()
        {
            for (var i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void RegisterListener(VoidEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(VoidEventListener listener)
            => _listeners.Remove(listener);
    }

    public class VoidEventListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO _channel;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_channel != null)
                _channel.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (_channel != null)
                _channel.UnregisterListener(this);
        }

        public void OnEventRaised() => _response?.Invoke();
    }
}
