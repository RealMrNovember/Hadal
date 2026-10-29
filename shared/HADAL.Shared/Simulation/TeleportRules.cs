using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Moving a base on the world map (docs/28): a random jump within the zone bases may stand in, a chosen free
    /// tile, or next to an alliance mate's base. Each uses its bag item, or Hadalite when there is none; all teams
    /// must be home.
    /// </summary>
    public static class TeleportRules
    {
        public const string Random = "random";
        public const string Target = "target";
        public const string Alliance = "alliance";

        public const string RandomItem = "teleport_random";
        public const string TargetItem = "teleport_target";
        public const string AllianceItem = "teleport_alliance";

        /// <summary>How far from an alliance mate's base the jump may land.</summary>
        public const int AllianceRadius = 3;

        public static bool IsMode(string? mode) => mode is Random or Target or Alliance;

        public static string ItemFor(string mode) => mode switch
        {
            Target => TargetItem,
            Alliance => AllianceItem,
            _ => RandomItem
        };

        /// <summary>Hadalite paid when the player has no item for the jump.</summary>
        public static long HadaliteFor(string mode) => mode switch
        {
            Target => 1_000,
            Alliance => 500,
            _ => 200
        };

        public static bool IsItem(string? id) => id is RandomItem or TargetItem or AllianceItem;

        public static string ModeOfItem(string id) => id switch
        {
            TargetItem => Target,
            AllianceItem => Alliance,
            _ => Random
        };
    }
}
