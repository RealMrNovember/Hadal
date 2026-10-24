using ProtoBuf;

namespace HADAL.Shared.Enums
{
    /// <summary>
    /// What category of structure may occupy a polar grid slot. Shared because the server
    /// validates placement with the same rules the client previews with.
    /// Values are a published contract — never renumber.
    /// </summary>
    [ProtoContract]
    public enum GridSlotType
    {
        [ProtoEnum] Core = 0,
        [ProtoEnum] Production = 1,
        [ProtoEnum] Research = 2,
        [ProtoEnum] Military = 3,
        [ProtoEnum] MegaDome = 4,
        [ProtoEnum] Alliance = 5,
        [ProtoEnum] Decoration = 6,

        /// <summary>Accepts any building, and any building marked Universal fits any slot.</summary>
        [ProtoEnum] Universal = 99,
    }
}
