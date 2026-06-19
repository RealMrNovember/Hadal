using System;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Hadal.Core.Events;
using Hadal.Data.Config;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// WebSocket ingress, gateway handshake, connection health, and reconciliation enqueue.
    /// </summary>
    public sealed class NetworkStateReceiver : INetworkStateReceiver, IStartable, ITickable, IDisposable
    {
        private const float HealthCheckIntervalSeconds = 0.5f;

        private readonly IWebSocketClient _webSocket;
        private readonly NetworkSerializationLayer _serialization;
        private readonly ICommandReconciliationSystem _reconciliation;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IGatewaySessionState _session;
        private readonly ILocalEventBus _localBus;
        private readonly GameConfigSO _config;

        private bool _listening;
        private bool _reconnectingAnnounced;
        private float _healthTimer;

        public NetworkStateReceiver(
            IWebSocketClient webSocket,
            NetworkSerializationLayer serialization,
            ICommandReconciliationSystem reconciliation,
            ICommandDispatcher commandDispatcher,
            IGatewaySessionState session,
            ILocalEventBus localBus,
            GameConfigSO config)
        {
            _webSocket = webSocket;
            _serialization = serialization;
            _reconciliation = reconciliation;
            _commandDispatcher = commandDispatcher;
            _session = session;
            _localBus = localBus;
            _config = config;
        }

        public void Start()
        {
            StartListening();

            if (_config != null && !string.IsNullOrWhiteSpace(_config.GatewayUrl))
                _webSocket.Connect(_config.GatewayUrl);
        }

        public void StartListening()
        {
            if (_listening)
                return;

            _webSocket.OnMessageReceived += HandleMessageReceived;
            _webSocket.OnConnected += HandleConnected;
            _webSocket.OnDisconnected += HandleDisconnected;
            _listening = true;
        }

        public void StopListening()
        {
            if (!_listening)
                return;

            _webSocket.OnMessageReceived -= HandleMessageReceived;
            _webSocket.OnConnected -= HandleConnected;
            _webSocket.OnDisconnected -= HandleDisconnected;
            _listening = false;
        }

        public void Tick()
        {
            _webSocket.DispatchMessageQueue();

            _healthTimer += Time.deltaTime;
            if (_healthTimer < HealthCheckIntervalSeconds)
                return;

            _healthTimer = 0f;
            EvaluateConnectionHealth();
        }

        public void Dispose()
        {
            StopListening();
        }

        private void HandleConnected()
        {
            _session.Reset();
            SendHandshakeCommand();
        }

        private void HandleDisconnected()
        {
            _session.Reset();
        }

        private void SendHandshakeCommand()
        {
            var handshake = new ClientAuthCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = 0,
                ClientSchemaVersion = SchemaVersion.Current,
                ClientVersion = Application.version,
                DeviceId = SystemInfo.deviceUniqueIdentifier
            };

            _commandDispatcher.DispatchHandshake(handshake);
        }

        private void EvaluateConnectionHealth()
        {
            if (_webSocket.IsConnected)
            {
                if (_reconnectingAnnounced)
                {
                    _localBus.Publish(new NetworkConnectedEvent());
                    _reconnectingAnnounced = false;
                }

                return;
            }

            if (_reconnectingAnnounced)
                return;

            _session.Reset();
            _localBus.Publish(new NetworkReconnectingEvent { Message = "Reconnecting..." });
            _reconnectingAnnounced = true;
        }

        private void HandleMessageReceived(byte[] wire)
        {
            if (wire == null || wire.Length == 0)
                return;

            if (TryDecodeHandshakeAck(wire, out var ack))
            {
                HandleHandshakeAck(ack);
                return;
            }

            if (TryDecodeDelta(wire, out var deltaPacket))
            {
                _reconciliation.EnqueueInbound(deltaPacket);
                return;
            }

            if (TryDecodeSnapshot(wire, out var snapshotPacket))
            {
                _reconciliation.EnqueueInbound(snapshotPacket);
                return;
            }

            Debug.LogWarning($"[NetworkStateReceiver] Unrecognized wire frame ({wire.Length} bytes).");
        }

        private void HandleHandshakeAck(HandshakeAckEnvelope ack)
        {
            if (ack == null || !ack.Accepted || ack.Result == CommandResultCode.Rejected)
            {
                Debug.LogWarning("[NetworkStateReceiver] Gateway handshake rejected.");
                return;
            }

            _session.CompleteHandshake();
            _commandDispatcher.FlushBuffer();
            Debug.Log("[NetworkStateReceiver] Gateway handshake complete — command buffer flushed.");
        }

        private bool TryDecodeHandshakeAck(byte[] wire, out HandshakeAckEnvelope ack)
        {
            ack = null!;
            if (!_serialization.TryDeserialize(wire, out HandshakeAckEnvelope envelope))
                return false;

            if (envelope.SchemaVersion < SchemaVersion.MinSupported)
                return false;

            ack = envelope;
            return true;
        }

        private bool TryDecodeDelta(byte[] wire, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (!_serialization.TryDeserialize(wire, out StateDeltaEnvelope envelope))
                return false;

            if (envelope.SchemaVersion < SchemaVersion.MinSupported)
                return false;

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                return false;

            var delta = _serialization.Deserialize<StateDelta>(envelope.Payload);

            packet = new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateDelta,
                SchemaVersion = envelope.SchemaVersion,
                ServerTick = envelope.ServerTick,
                BaselineTick = envelope.BaselineTick,
                Delta = delta
            };

            return true;
        }

        private bool TryDecodeSnapshot(byte[] wire, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (!_serialization.TryDeserialize(wire, out StateSnapshotEnvelope envelope))
                return false;

            if (envelope.SchemaVersion < SchemaVersion.MinSupported)
                return false;

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                return false;

            var snapshot = _serialization.Deserialize<StateSnapshot>(envelope.Payload);

            packet = new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateSnapshot,
                SchemaVersion = envelope.SchemaVersion,
                ServerTick = envelope.ServerTick,
                Snapshot = snapshot
            };

            return true;
        }
    }
}
