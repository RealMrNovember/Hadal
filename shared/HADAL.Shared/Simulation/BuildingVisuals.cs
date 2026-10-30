using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Building evolution (docs/rehber/04_bina_evrimi.md §2, §9): a building's look follows its level in six tiers,
    /// one every ten levels (Patch 1–9, Rivet 10–19, Pressure 20–29, Alloy 30–39, Biomechanical 40–49, Resonance 50).
    /// The server never needs it: the look is derived from the level the client already has.
    /// </summary>
    public static class BuildingVisuals
    {
        public const int Tiers = 6;

        /// <summary>The tier (1–6) of a building at <paramref name="level"/>; a building still being built (0) shows tier 1.</summary>
        public static int TierFor(int level) => level >= 50 ? Tiers : Math.Max(1, Math.Min(Tiers, 1 + Math.Max(0, level) / 10));
    }
}
