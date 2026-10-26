using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks for the bases around a map tile (docs/28_World_Map.md). The server answers with <c>EntityIds.WorldView</c>.</summary>
    [ProtoContract]
    public sealed class ViewWorldCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>Centre tile; both -1 means "around my base".</summary>
        [ProtoMember(3)] public int X { get; set; }
        [ProtoMember(4)] public int Y { get; set; }
    }
}
