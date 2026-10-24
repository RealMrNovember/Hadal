using HADAL.Shared.Enums;

namespace Hadal.Core.StateSync
{
    /// <summary>
    /// Encodes <see cref="ResourceType"/> to and from the untyped
    /// <c>ResourceSaveEntry.resourceType</c> int.
    /// <para>
    /// There is exactly one <see cref="ResourceType"/> — the one in HADAL.Shared — so the
    /// stored value is simply its ordinal. This class exists to keep raw
    /// <c>(ResourceType)someInt</c> casts out of the codebase: an unrecognised ordinal must
    /// degrade to <see cref="ResourceType.Unknown"/> rather than alias a real resource, which
    /// is exactly how a client-local copy of this enum once mislabelled every balance.
    /// </para>
    /// </summary>
    public static class ResourceTypeMap
    {
        public static int ToStoredValue(ResourceType resource) => (int)resource;

        public static ResourceType FromStoredValue(int storedValue)
        {
            switch (storedValue)
            {
                case (int)ResourceType.Oxygen:
                case (int)ResourceType.Food:
                case (int)ResourceType.Biomass:
                case (int)ResourceType.Research:
                case (int)ResourceType.Alloy:
                case (int)ResourceType.Energy:
                case (int)ResourceType.Titanium:
                case (int)ResourceType.Hadalite:
                    return (ResourceType)storedValue;
                default:
                    return ResourceType.Unknown;
            }
        }
    }
}
