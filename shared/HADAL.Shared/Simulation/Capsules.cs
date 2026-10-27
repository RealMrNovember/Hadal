using System;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Resource capsules (docs/rehber 15 §2.2): quest, chest and event resources land in the bag as
    /// capsules stamped with their amount ("cap_Titanium_1500"). Opening one adds to the wallet even past
    /// the storage cap; the cap only stops passive production.
    /// </summary>
    public static class Capsules
    {
        public const string Prefix = "cap_";

        /// <summary>Premium Hadalite and Superalloy are paid out directly; the capped resources become capsules.</summary>
        public static bool Holds(ResourceType resource) =>
            resource is ResourceType.Titanium or ResourceType.Biomass or ResourceType.Food or ResourceType.Oxygen or ResourceType.Energy;

        public static string Id(ResourceType resource, long amount) => Prefix + resource + "_" + amount;

        public static bool TryParse(string id, out ResourceType resource, out long amount)
        {
            resource = ResourceType.Unknown;
            amount = 0;
            if (string.IsNullOrEmpty(id) || !id.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            var parts = id.Substring(Prefix.Length).Split('_');
            return parts.Length == 2
                   && Enum.TryParse(parts[0], false, out resource) && Holds(resource)
                   && long.TryParse(parts[1], out amount) && amount > 0;
        }
    }
}
