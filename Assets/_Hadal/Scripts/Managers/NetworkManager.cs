using UnityEngine;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    /// <summary>
    /// Client-side network facade. Server-authoritative logic lives on backend.
    /// </summary>
    public class NetworkManager : ManagerBase
    {
        private string _gatewayUrl;
        private float _heartbeatInterval;
        private float _heartbeatTimer;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        protected override void OnInitialize(GameConfigSO config)
        {
            _gatewayUrl = config.GatewayUrl;
            _heartbeatInterval = config.NetworkHeartbeatInterval;
        }

        public void Connect()
        {
            // WebSocket connection to Gateway Server — implement with NativeWebSocket or similar.
            _isConnected = true;
            Debug.Log($"[NetworkManager] Connecting to {_gatewayUrl}");
        }

        public void Disconnect()
        {
            _isConnected = false;
        }

        private void Update()
        {
            if (!IsInitialized || !_isConnected)
                return;

            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= _heartbeatInterval)
            {
                _heartbeatTimer = 0f;
                SendHeartbeat();
            }
        }

        private void SendHeartbeat()
        {
            // Send heartbeat to Gateway Server.
        }

        protected override void OnShutdown() => Disconnect();
    }
}
