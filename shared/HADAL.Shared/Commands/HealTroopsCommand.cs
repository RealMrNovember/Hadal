using ProtoBuf;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Commands
{
    /// <summary>Heals wounded troops in the Med Bay (docs/rehber 07 §2.7); empty = every wounded troop.</summary>
    [ProtoContract]
    public sealed class HealTroopsCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public TroopStackDto[] Troops { get; set; } = System.Array.Empty<TroopStackDto>();
    }
}
