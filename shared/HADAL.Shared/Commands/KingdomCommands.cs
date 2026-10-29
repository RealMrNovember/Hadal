using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks for the kingdom list (<c>EntityIds.Kingdoms</c>, docs/39 §4).</summary>
    [ProtoContract]
    public sealed class ViewKingdomsCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }
}
