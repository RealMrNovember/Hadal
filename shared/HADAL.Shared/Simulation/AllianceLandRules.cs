using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance land (docs/32 §2.1–2.2): the Alliance Hub claims a square around it that grows with its level; beacons
    /// placed next to the land claim more once built and while connected to the hub. Tiles go to the first claimer.
    /// </summary>
    public static class AllianceLandRules
    {
        /// <summary>The world every record belongs to until there are several (docs/32 §2.7).</summary>
        public const string DefaultWorld = "w1";

        public const string Hub = "hub";
        public const string Beacon = "beacon";

        public const string PlaceHub = "hub";
        public const string PlaceBeacon = "beacon";
        public const string MoveHub = "move";
        public const string Remove = "remove";
        public const string Donate = "donate";
        public const string AbandonCity = "abandon";

        public const int HubMinMembers = 3;
        public const int MaxHubLevel = 5;
        public const int BeaconRadius = 3;
        public const int BeaconBuildMinutes = 10;
        public const int MaxBeaconCap = 30;

        /// <summary>A beacon goes up at most this far outside the alliance's land.</summary>
        public const int PlaceReach = 2;

        /// <summary>A new hub keeps this distance from other alliances' land.</summary>
        public const int OtherAllianceGap = 3;

        /// <summary>A city this close to the alliance's land is a neighbour and can be attacked.</summary>
        public const int CityReach = 4;

        public static readonly TimeSpan HubMoveCooldown = TimeSpan.FromHours(24);

        public static int HubRadius(int level) => 5 + Math.Clamp(level, 1, MaxHubLevel);

        public static int Radius(string kind, int level) => kind == Hub ? HubRadius(level) : BeaconRadius;

        /// <summary>Beacons an alliance may have: two per hub level plus one per five members, at most 30.</summary>
        public static int MaxBeacons(int members, int hubLevel) => Math.Min(MaxBeaconCap, 2 * Math.Max(1, hubLevel) + Math.Max(0, members) / 5);

        /// <summary>Titanium the officer pays for the n-th beacon (n from 0).</summary>
        public static long BeaconCost(int existing) => 15_000L * (existing + 1);

        /// <summary>Donation points from level n to n + 1.</summary>
        public static long HubPoints(int level) => 40_000L * level * level;

        /// <summary>Cities an alliance may hold (Last Asylum's "6/6").</summary>
        public static int MaxCities(int members) => Math.Min(6, 1 + Math.Max(0, members) / 3);

        /// <summary>Two squares touch or overlap (Chebyshev), so the second is connected to the first.</summary>
        public static bool Touch(int x1, int y1, int r1, int x2, int y2, int r2) =>
            Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2)) <= r1 + r2 + 1;

        /// <summary>Resources a hub takes as donations.</summary>
        public static bool IsDonation(Enums.ResourceType type) =>
            type is Enums.ResourceType.Titanium or Enums.ResourceType.Biomass or Enums.ResourceType.Food;
    }
}
