using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Marker for server-validated player intent messages.
    /// Wire encoding: Protobuf payload inside <see cref="CommandEnvelope"/>.
    /// </summary>
    public interface ICommand
    {
        string CommandId { get; }
        ulong ClientSequence { get; }
    }
}
