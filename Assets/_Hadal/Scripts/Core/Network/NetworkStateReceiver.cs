using HADAL.Shared.DTOs;
using HADAL.Shared.Serialization;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Listens to WebSocket binary frames, decodes Protobuf envelopes,
    /// and forwards normalized packets to <see cref="ICommandReconciliationSystem"/>.
    /// </summary>
    public sealed class NetworkStateReceiver : INetworkStateReceiver, IStartable, System.IDisposable
    {
        private readonly IWebSocketClient _webSocket;
        private readonly NetworkSerializationLayer _serialization;
        private readonly ICommandReconciliationSystem _reconciliation;
        private bool _listening;

        public NetworkStateReceiver(
            IWebSocketClient webSocket,
            NetworkSerializationLayer serialization,
            ICommandReconciliationSystem reconciliation)
        {
            _webSocket = webSocket;
            _serialization = serialization;
            _reconciliation = reconciliation;
        }

        public void Start()
        {
            StartListening();
        }

        public void StartListening()
        {
            if (_listening)
                return;

            _webSocket.OnMessageReceived += HandleMessageReceived;
            _listening = true;
            Debug.Log("[NetworkStateReceiver] Listening for inbound Protobuf frames.");
        }

        public void StopListening()
        {
            if (!_listening)
                return;

            _webSocket.OnMessageReceived -= HandleMessageReceived;
            _listening = false;
            Debug.Log("[NetworkStateReceiver] Stopped listening.");
        }

        public void Dispose()
        {
            StopListening();
        }

        private void HandleMessageReceived(byte[] wire)
        {
            if (wire == null || wire.Length == 0)
            {
                Debug.LogWarning("[NetworkStateReceiver] Ignored empty wire frame.");
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

        private bool TryDecodeDelta(byte[] wire, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (!_serialization.TryDeserialize(wire, out StateDeltaEnvelope envelope))
                return false;

            if (envelope.SchemaVersion < SchemaVersion.MinSupported)
            {
                Debug.LogWarning(
                    $"[NetworkStateReceiver] Rejected delta — schema {envelope.SchemaVersion} < min {SchemaVersion.MinSupported}.");
                return false;
            }

            if (envelope.Payload == null || envelope.Payload.Length == 0)
            {
                Debug.LogWarning("[NetworkStateReceiver] Delta envelope has empty payload.");
                return false;
            }

            var delta = _serialization.Deserialize<StateDelta>(envelope.Payload);

            packet = new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateDelta,
                SchemaVersion = envelope.SchemaVersion,
                ServerTick = envelope.ServerTick,
                BaselineTick = envelope.BaselineTick,
                Delta = delta
            };

            Debug.Log($"[NetworkStateReceiver] Decoded StateDelta tick={envelope.ServerTick}.");
            return true;
        }

        private bool TryDecodeSnapshot(byte[] wire, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (!_serialization.TryDeserialize(wire, out StateSnapshotEnvelope envelope))
                return false;

            if (envelope.SchemaVersion < SchemaVersion.MinSupported)
            {
                Debug.LogWarning(
                    $"[NetworkStateReceiver] Rejected snapshot — schema {envelope.SchemaVersion} < min {SchemaVersion.MinSupported}.");
                return false;
            }

            if (envelope.Payload == null || envelope.Payload.Length == 0)
            {
                Debug.LogWarning("[NetworkStateReceiver] Snapshot envelope has empty payload.");
                return false;
            }

            var snapshot = _serialization.Deserialize<StateSnapshot>(envelope.Payload);

            packet = new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateSnapshot,
                SchemaVersion = envelope.SchemaVersion,
                ServerTick = envelope.ServerTick,
                BaselineTick = 0,
                Snapshot = snapshot
            };

            Debug.Log($"[NetworkStateReceiver] Decoded StateSnapshot tick={envelope.ServerTick}.");
            return true;
        }
    }
}
