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
        private readonly Queue<BufferedCommand> _buffer = new();

        public CommandDispatcher(
            NetworkSerializationLayer serialization,
            IWebSocketClient webSocket,
            ICommandReconciliationSystem reconciliation,
            IGatewaySessionState session)
        {
            _serialization = serialization;
            _webSocket = webSocket;
            _reconciliation = reconciliation;
            _session = session;
        }

        public void Dispatch<T>(T command) where T : ICommand
        {
            var commandType = ResolveCommandType(command);
            var wire = SerializeEnvelope(command, commandType);

            if (!_session.IsHandshakeComplete)
            {
                _buffer.Enqueue(new BufferedCommand(wire, command.CommandId, command.ClientSequence, commandType));
                Debug.Log($"[CommandDispatcher] Buffered {commandType} — handshake pending (queue={_buffer.Count}).");
                return;
            }

            SendWire(wire, command.CommandId, command.ClientSequence, commandType);
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
                SendWire(item.Wire, item.CommandId, item.ClientSequence, item.CommandType);
            }
        }

        private byte[] SerializeEnvelope<T>(T command, CommandType commandType) where T : ICommand
        {
            var innerPayload = _serialization.Serialize(command);
            var envelope = EnvelopeBuilder.WrapCommand(command, commandType, innerPayload);
            return _serialization.Serialize(envelope);
        }

        private void SendWire(byte[] wire, string commandId, ulong clientSequence, CommandType commandType)
        {
            _reconciliation.RegisterPendingCommand(commandId, clientSequence, commandType);
            _webSocket.Send(wire);
            Debug.Log($"[CommandDispatcher] Sent {commandType} id={commandId} wire={wire.Length}B");
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
