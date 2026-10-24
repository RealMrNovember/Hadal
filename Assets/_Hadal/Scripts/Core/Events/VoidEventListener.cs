using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Hadal.Core.Events
{
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
