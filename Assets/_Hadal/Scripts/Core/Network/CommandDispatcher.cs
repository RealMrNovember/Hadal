using System.Collections.Generic;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Serializes commands into Protobuf envelopes. Gameplay commands buffer until gateway handshake completes.
    /// </summary>
    public sealed class CommandDispatcher : ICommandDispatcher
    {
        private readonly struct BufferedCommand
        {
            public readonly byte[] Wire;
            public readonly string CommandId;
            public readonly ulong ClientSequence;
            public readonly CommandType CommandType;

            public BufferedCommand(byte[] wire, string commandId, ulong clientSequence, CommandType commandType)
            {
                Wire = wire;
                CommandId = commandId;
                ClientSequence = clientSequence;
                CommandType = commandType;
            }
        }

        private readonly NetworkSerializationLayer _serialization;
        private readonly IWebSocketClient _webSocket;
        private readonly ICommandReconciliationSystem _reconciliation;
        private readonly IGatewaySessionState _session;
        private readonly IPlacementAckSimulator _placementAckSimulator;
        private readonly Queue<BufferedCommand> _buffer = new();

        public CommandDispatcher(
            NetworkSerializationLayer serialization,
            IWebSocketClient webSocket,
            ICommandReconciliationSystem reconciliation,
            IGatewaySessionState session,
            IPlacementAckSimulator placementAckSimulator)
        {
            _serialization = serialization;
            _webSocket = webSocket;
            _reconciliation = reconciliation;
            _session = session;
            _placementAckSimulator = placementAckSimulator;
            _session.HandshakeCompleted += FlushBuffer;
        }

        public void Dispatch<T>(T command) where T : ICommand
        {
            var commandType = ResolveCommandType(command);
            var wire = SerializeEnvelope(command, commandType);

            _reconciliation.RegisterPendingCommand(command.CommandId, command.ClientSequence, commandType);

            if (!_session.IsHandshakeComplete)
            {
                _buffer.Enqueue(new BufferedCommand(wire, command.CommandId, command.ClientSequence, commandType));
                Debug.Log($"[CommandDispatcher] Buffered {commandType} — handshake pending (queue={_buffer.Count}).");
                TrySimulatePlacementAck(command);
                return;
            }

            SendWire(wire, commandType, command);
        }

        public void DispatchHandshake<T>(T command) where T : ICommand
        {
            var commandType = ResolveCommandType(command);
            var wire = SerializeEnvelope(command, commandType);
            _webSocket.Send(wire);
            Debug.Log($"[CommandDispatcher] Handshake sent ({commandType}, {wire.Length}B).");
        }

        public void FlushBuffer()
        {
            while (_buffer.Count > 0)
            {
                var item = _buffer.Dequeue();
                SendWireOnly(item.Wire, item.CommandType, item.CommandId);
            }
        }

        private byte[] SerializeEnvelope<T>(T command, CommandType commandType) where T : ICommand
        {
            var innerPayload = _serialization.Serialize(command);
            var envelope = EnvelopeBuilder.WrapCommand(command, commandType, innerPayload);
            return _serialization.Serialize(envelope);
        }

        private void SendWireOnly(byte[] wire, CommandType commandType, string commandId)
        {
            _webSocket.Send(wire);
            Debug.Log($"[CommandDispatcher] Sent {commandType} id={commandId} wire={wire.Length}B");
        }

        private void SendWire<T>(byte[] wire, CommandType commandType, T command) where T : ICommand
        {
            SendWireOnly(wire, commandType, command.CommandId);
            TrySimulatePlacementAck(command);
        }

        private void TrySimulatePlacementAck<T>(T command) where T : ICommand
        {
            if (command is PlaceBuildingCommand placeBuilding)
                _placementAckSimulator?.SchedulePlacementAck(placeBuilding);
        }

        private static CommandType ResolveCommandType<T>(T command) where T : ICommand
        {
            return command switch
            {
                ClientAuthCommand => CommandType.ClientAuth,
                PlaceBuildingCommand => CommandType.PlaceBuilding,
                ExecuteGachaCommand => CommandType.ExecuteGacha,
                _ => CommandType.Unknown
            };
        }
    }
}
