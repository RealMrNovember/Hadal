using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Changing the player name (Last Asylum's rename card): the first three changes are free and need no wait; after
    /// that each change takes a Rename Card from the bag — or, without one, its Hadalite price — and paid changes are
    /// a day apart. Names follow AccountRules and must not be taken by another player.
    /// </summary>
    public static class RenameRules
    {
        public const string Item = "rename_card";
        public const int FreeRenames = 3;
        public const long HadalitePrice = 500;
        public static readonly TimeSpan Cooldown = TimeSpan.FromHours(24);

        public static bool IsItem(string? id) => id == Item;
    }
}
