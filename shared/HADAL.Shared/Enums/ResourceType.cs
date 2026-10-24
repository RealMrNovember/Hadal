using ProtoBuf;

namespace HADAL.Shared.Enums
{
    /// <summary>
    /// Canonical resource identity for wire and storage.
    /// Values are a published contract — never renumber or reuse one. New resources
    /// append; retired resources stay reserved.
    /// </summary>
    [ProtoContract]
    public enum ResourceType
    {
        [ProtoEnum] Unknown = 0,
        [ProtoEnum] Oxygen = 1,
        [ProtoEnum] Food = 2,
        [ProtoEnum] Biomass = 3,
        [ProtoEnum] Research = 4,

        /// <summary>Reserved. Superseded by <see cref="Titanium"/>; kept for wire compatibility.</summary>
        [ProtoEnum] Alloy = 5,

        // Canonical design resources (docs/06_Resources.md, docs/19_Economy.md).
        [ProtoEnum] Energy = 6,
        [ProtoEnum] Titanium = 7,
        [ProtoEnum] Hadalite = 8,
    }
}
