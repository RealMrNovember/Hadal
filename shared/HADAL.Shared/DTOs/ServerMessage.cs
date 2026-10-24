using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    public enum ServerMessageKind
    {
        Unknown = 0,
        HandshakeAck = 1,
        StateSnapshot = 2,
        StateDelta = 3,
    }

    /// <summary>
    /// The only top-level server → client wire frame. Exactly one body field is set.
    /// The inner envelopes are wire-compatible with each other (same field numbers and
    /// wire types), so they must never be sent bare — the field number here is the discriminator.
    /// </summary>
    [ProtoContract]
    public sealed class ServerMessage
    {
        [ProtoMember(1)] public uint SchemaVersion { get; set; }
        [ProtoMember(2)] public HandshakeAckEnvelope? HandshakeAck { get; set; }
        [ProtoMember(3)] public StateSnapshotEnvelope? Snapshot { get; set; }
        [ProtoMember(4)] public StateDeltaEnvelope? Delta { get; set; }

        /// <summary><see cref="ServerMessageKind.Unknown"/> unless exactly one body is set.</summary>
        public ServerMessageKind Kind
        {
            get
            {
                var count = (HandshakeAck != null ? 1 : 0) + (Snapshot != null ? 1 : 0) + (Delta != null ? 1 : 0);
                if (count != 1)
                    return ServerMessageKind.Unknown;

                if (HandshakeAck != null)
                    return ServerMessageKind.HandshakeAck;

                return Snapshot != null ? ServerMessageKind.StateSnapshot : ServerMessageKind.StateDelta;
            }
        }
    }
}
