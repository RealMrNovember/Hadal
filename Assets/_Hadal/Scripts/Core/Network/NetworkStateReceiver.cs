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

            // Every server frame is a ServerMessage — the inner envelopes are wire-compatible
            // with each other, so the field number in ServerMessage is the only safe discriminator.
            if (!_serialization.TryDeserialize(wire, out ServerMessage message) || message == null)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Undecodable wire frame ({wire.Length} bytes).");
                return;
            }

            if (message.SchemaVersion < SchemaVersion.MinSupported)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Server schema {message.SchemaVersion} below minimum {SchemaVersion.MinSupported}.");
                return;
            }

            switch (message.Kind)
            {
                case ServerMessageKind.HandshakeAck:
                    HandleHandshakeAck(message.HandshakeAck);
                    return;

                case ServerMessageKind.StateDelta:
                    if (TryDecodeDelta(message.Delta, out var deltaPacket))
                        _reconciliation.EnqueueInbound(deltaPacket);
                    return;

                case ServerMessageKind.StateSnapshot:
                    if (TryDecodeSnapshot(message.Snapshot, out var snapshotPacket))
                        _reconciliation.EnqueueInbound(snapshotPacket);
                    return;

                default:
                    Debug.LogWarning($"[NetworkStateReceiver] Unrecognized server message ({wire.Length} bytes).");
                    return;
            }
        }

        private void HandleHandshakeAck(HandshakeAckEnvelope ack)
        {
            if (ack == null || !ack.Accepted || ack.Result != CommandResultCode.Accepted)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Gateway handshake rejected ({ack?.Result}).");
                return;
            }

            _session.CompleteHandshake();
            _commandDispatcher.FlushBuffer();
            Debug.Log("[NetworkStateReceiver] Gateway handshake complete — command buffer flushed.");
        }

        private bool TryDecodeDelta(StateDeltaEnvelope envelope, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                return false;

            if (!_serialization.TryDeserialize(envelope.Payload, out StateDelta delta))
            {
                Debug.LogWarning("[NetworkStateReceiver] StateDelta payload undecodable.");
                return false;
            }

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

        private bool TryDecodeSnapshot(StateSnapshotEnvelope envelope, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                return false;

            if (!_serialization.TryDeserialize(envelope.Payload, out StateSnapshot snapshot))
            {
                Debug.LogWarning("[NetworkStateReceiver] StateSnapshot payload undecodable.");
                return false;
            }

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
