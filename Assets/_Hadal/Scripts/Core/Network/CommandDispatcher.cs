using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Serializes commands into <see cref="CommandEnvelope"/> Protobuf frames
    /// and dispatches them through <see cref="IWebSocketClient"/>.
    /// </summary>
    public sealed class CommandDispatcher : ICommandDispatcher
    {
        private readonly NetworkSerializationLayer _serialization;
        private readonly IWebSocketClient _webSocket;

        public CommandDispatcher(NetworkSerializationLayer serialization, IWebSocketClient webSocket)
        {
            _serialization = serialization;
            _webSocket = webSocket;
        }

        public void Dispatch<T>(T command) where T : ICommand
        {
            var commandType = ResolveCommandType(command);
            var innerPayload = _serialization.Serialize(command);
            var envelope = EnvelopeBuilder.WrapCommand(command, commandType, innerPayload);
            var wire = _serialization.Serialize(envelope);

            _webSocket.Send(wire);

            Debug.Log(
                $"[CommandDispatcher] {commandType} seq={command.ClientSequence} " +
                $"inner={innerPayload.Length}B wire={wire.Length}B");
        }

        private static CommandType ResolveCommandType<T>(T command) where T : ICommand
        {
            return command switch
            {
                PlaceBuildingCommand => CommandType.PlaceBuilding,
                ExecuteGachaCommand => CommandType.ExecuteGacha,
                _ => CommandType.Unknown
            };
        }
    }
}
