using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Buys from the alliance shop with alliance coins (AllianceShopRules); items go to the bag.</summary>
    [ProtoContract]
    public sealed class BuyAllianceItemCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string ItemId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Count { get; set; } = 1;
    }
}
