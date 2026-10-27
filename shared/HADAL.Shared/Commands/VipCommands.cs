using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Claims today's sign-in VIP points (the streak grows when claimed on consecutive days).</summary>
    [ProtoContract]
    public sealed class ClaimVipLoginCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }

    /// <summary>Claims today's VIP chest (VIP 1 and up).</summary>
    [ProtoContract]
    public sealed class ClaimVipChestCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }
}
