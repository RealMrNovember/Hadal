using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Server-authoritative gacha pull — Cryo-Pod Salvage or Deep Sea Sonar Ping.
    /// </summary>
    [ProtoContract]
    public sealed class ExecuteGachaCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string BannerId { get; set; } = string.Empty;
        [ProtoMember(4)] public GachaType GachaType { get; set; }
        [ProtoMember(5)] public int PullCount { get; set; } = 1;
    }
}
