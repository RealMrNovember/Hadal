using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Serialization
{
    /// <summary>
    /// Wraps typed payloads into wire envelopes (Protobuf).
    /// Client sends <see cref="CommandEnvelope"/>; server sends delta/snapshot envelopes.
    /// </summary>
    public static class EnvelopeBuilder
    {
        public static CommandEnvelope WrapCommand<T>(
            T command,
            CommandType type,
            byte[] payload,
            uint schemaVersion = SchemaVersion.Current) where T : ICommand
        {
            return new CommandEnvelope
            {
                SchemaVersion = schemaVersion,
                CommandId = command.CommandId,
                ClientSequence = command.ClientSequence,
                ServerTick = 0,
                Type = type,
                Payload = payload
            };
        }

        public static StateDeltaEnvelope WrapStateDelta(
            StateDelta delta,
            byte[] payload,
            ulong baselineTick = 0,
            uint schemaVersion = SchemaVersion.Current)
        {
            return new StateDeltaEnvelope
            {
                SchemaVersion = schemaVersion,
                ServerTick = delta.ServerTick,
                BaselineTick = baselineTick,
                Payload = payload
            };
        }

        public static StateSnapshotEnvelope WrapStateSnapshot(
            StateSnapshot snapshot,
            byte[] payload,
            uint schemaVersion = SchemaVersion.Current)
        {
            return new StateSnapshotEnvelope
            {
                SchemaVersion = schemaVersion,
                ServerTick = snapshot.ServerTick,
                Payload = payload
            };
        }
    }
}
