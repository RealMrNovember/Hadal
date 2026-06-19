using System;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>
    /// WebSocket client stub — Sprint 4 transport shell.
    /// NativeWebSocket (or equivalent) wiring comes in the next phase.
    /// </summary>
    public sealed class NativeWebSocketClient : IWebSocketClient
    {
        private string _url = string.Empty;

        public bool IsConnected { get; private set; }

        public event Action<byte[]> OnMessageReceived;

        public void Connect(string url)
        {
            _url = url;
            IsConnected = true;
            Debug.Log($"[WebSocket Stub] Connect requested → {_url}");
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebSocket Stub] Send ignored — not connected.");
                return;
            }

            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("[WebSocket Stub] Send ignored — empty payload.");
                return;
            }

            Debug.Log($"[WebSocket Stub] Send {data.Length} bytes → {_url}");
        }

        public void Disconnect()
        {
            IsConnected = false;
            Debug.Log("[WebSocket Stub] Disconnected.");
        }

        /// <summary>
        /// Test hook — simulates an inbound server frame on the main thread.
        /// </summary>
        internal void SimulateInboundMessage(byte[] payload)
        {
            OnMessageReceived?.Invoke(payload);
        }
    }
}
