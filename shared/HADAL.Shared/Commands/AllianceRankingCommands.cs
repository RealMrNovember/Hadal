using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks for one of the alliance's rankings (answered with an <c>AllianceRankingDto</c>).</summary>
    [ProtoContract]
    public sealed class ViewAllianceRankingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>AllianceRankingKinds: "power", "kills" or "donations".</summary>
        [ProtoMember(3)] public string Kind { get; set; } = string.Empty;
    }
}
