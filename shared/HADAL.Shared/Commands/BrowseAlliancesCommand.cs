using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks for alliances to join, optionally filtered by name or tag (docs/25).</summary>
    [ProtoContract]
    public sealed class BrowseAlliancesCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string Query { get; set; } = string.Empty;
    }
}
