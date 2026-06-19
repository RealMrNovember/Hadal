using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Sends gameplay commands to the network layer. Sprint 3: stub transport (log only).
    /// </summary>
    public sealed class CommandDispatcher : ICommandDispatcher
    {
        private readonly NetworkSerializationLayer _serialization;

        public CommandDispatcher(NetworkSerializationLayer serialization)
        {
            _serialization = serialization;
        }

        public void Dispatch<T>(T command) where T : ICommand
        {
            var payload = _serialization.Serialize(command);
            var commandType = ResolveCommandType(command);
            Debug.Log($"[Network Stub] Dispatched Command: {commandType} - Payload Size: {payload.Length} bytes");
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
