using System;
using NativeWebSocket;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Production WebSocket client backed by Endel NativeWebSocket.
    /// </summary>
    public sealed class NativeWebSocketClient : IWebSocketClient, ITickable, IDisposable
    {
        private WebSocket _socket;
        private string _url = string.Empty;

        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        public WebSocketConnectionState ConnectionState => MapState(_socket?.State ?? WebSocketState.Closed);

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnMessageReceived;

        public void Connect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogError("[WebSocket] Connect failed — empty URL.");
                return;
            }

            if (_socket != null)
                Disconnect();

            _url = url;
            _socket = new WebSocket(_url);

            _socket.OnOpen += HandleOpen;
            _socket.OnMessage += HandleMessage;
            _socket.OnError += HandleError;
            _socket.OnClose += HandleClose;

            _ = _socket.Connect();
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
                return;

            if (data == null || data.Length == 0)
                return;

            _ = _socket.Send(data);
        }

        public void Disconnect()
        {
            if (_socket == null)
                return;

            Unsubscribe(_socket);

            if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting)
                _ = _socket.Close();

            _socket = null;
        }

        public void DispatchMessageQueue()
        {
            _socket?.DispatchMessageQueue();
        }

        public void Tick()
        {
            DispatchMessageQueue();
        }

        public void Dispose()
        {
            Disconnect();
        }

        private void HandleOpen()
        {
            Debug.Log($"[WebSocket] Connected → {_url}");
            OnConnected?.Invoke();
        }

        private void HandleMessage(byte[] data)
        {
            OnMessageReceived?.Invoke(data);
        }

        private void HandleError(string error)
        {
            Debug.LogWarning($"[WebSocket] Error → {error}");
        }

        private void HandleClose(WebSocketCloseCode code)
        {
            Debug.Log($"[WebSocket] Closed → {code}");
            OnDisconnected?.Invoke();
        }

        private static void Unsubscribe(WebSocket socket)
        {
            socket.OnOpen -= HandleOpen;
            socket.OnMessage -= HandleMessage;
            socket.OnError -= HandleError;
            socket.OnClose -= HandleClose;
        }

        private static WebSocketConnectionState MapState(WebSocketState state)
        {
            return state switch
            {
                WebSocketState.Open => WebSocketConnectionState.Open,
                WebSocketState.Connecting => WebSocketConnectionState.Connecting,
                WebSocketState.Closing => WebSocketConnectionState.Closing,
                _ => WebSocketConnectionState.Closed
            };
        }
    }
}
