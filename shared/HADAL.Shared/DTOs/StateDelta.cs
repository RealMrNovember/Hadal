using System.Collections.Generic;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class StateDelta
    {
        [ProtoMember(1)] public ulong ServerTick { get; set; }
        [ProtoMember(2)] public ulong BaselineTick { get; set; }
        public List<EntityChangeDto> Changes { get; set; } = new();

        /// <summary>Wire form of <see cref="Changes"/> (IL2CPP: arrays only, see <see cref="ResourceAmountEntry"/>).</summary>
        [ProtoMember(3)] public EntityChangeDto[] ChangesWire { get; set; } = System.Array.Empty<EntityChangeDto>();

        [ProtoBeforeSerialization]
        private void BeforeSerialize() => ChangesWire = Changes?.ToArray() ?? System.Array.Empty<EntityChangeDto>();

        [ProtoAfterDeserialization]
        private void AfterDeserialize() => Changes = new List<EntityChangeDto>(ChangesWire ?? System.Array.Empty<EntityChangeDto>());
    }
}
