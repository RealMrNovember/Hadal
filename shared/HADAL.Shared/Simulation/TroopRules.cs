using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a training order was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum TroopRejectReason
    {
        None = 0,
        UnknownTroop = 1,
        UnknownBuilding = 2,
        WrongBuilding = 3,
        UnderConstruction = 4,
        QueueBusy = 5,
        TierLocked = 6,
        BadCount = 7,
        ArmyFull = 8,
        InsufficientResources = 9,
        NotTraining = 10,
        NothingToHeal = 11,
        HealBusy = 12,
        NotEnoughWounded = 13,
        NoMedBay = 14
    }

    /// <summary>
    /// Training rules shared by server and client (docs/rehber 07 §2.3–2.5, 19 §2): cost, time, batch, tier
    /// locks and army capacity. Costs are per troop and the same for every type.
    /// </summary>
    public static class TroopRules
    {
        /// <summary>Key of a troop stack: "armored:3".</summary>
        public static string Key(string type, int tier) => $"{type}:{tier}";

        public static bool TryParseKey(string key, out string type, out int tier)
        {
            type = string.Empty;
            tier = 0;
            var colon = (key ?? string.Empty).LastIndexOf(':');
            if (colon <= 0 || !int.TryParse(key!.Substring(colon + 1), out tier))
                return false;
            type = key.Substring(0, colon);
            return true;
        }

        public static TroopTypeData? Type(TroopCatalogData troops, string id) =>
            troops.types.FirstOrDefault(t => t.id == id);

        public static TroopTierData? Tier(TroopCatalogData troops, int tier) =>
            troops.tiers.FirstOrDefault(t => t.tier == tier);

        /// <summary>The troop type a training building trains, or null.</summary>
        public static TroopTypeData? TypeOfBuilding(TroopCatalogData troops, string buildingId) =>
            troops.types.FirstOrDefault(t => t.building == buildingId);

        public static bool IsTrainingBuilding(TroopCatalogData troops, string buildingId) => TypeOfBuilding(troops, buildingId) != null;

        /// <summary>Piecewise linear between anchors; flat outside them.</summary>
        public static long Interpolate(LevelValueData[] anchors, int level)
        {
            if (anchors.Length == 0 || level <= 0)
                return 0;
            var sorted = anchors.OrderBy(a => a.level).ToArray();
            if (level <= sorted[0].level)
                return sorted[0].value;
            for (var i = 1; i < sorted.Length; i++)
            {
                if (level > sorted[i].level)
                    continue;
                var a = sorted[i - 1];
                var b = sorted[i];
                return a.value + (long)Math.Round((b.value - a.value) * (level - a.level) / (double)Math.Max(1, b.level - a.level));
            }

            return sorted[sorted.Length - 1].value;
        }

        /// <summary>How many troops the base can keep: every Military Dome's capacity added up.</summary>
        public static long ArmyCapacity(TroopCatalogData troops, IEnumerable<(string DefinitionId, int Level)> buildings) =>
            buildings.Where(b => b.DefinitionId == troops.capacityBuilding && b.Level > 0).Sum(b => Interpolate(troops.capacity, b.Level));

        public static int BatchSize(TroopCatalogData troops, int buildingLevel) => (int)Interpolate(troops.batch, buildingLevel);

        /// <summary>Highest tier this training building level opens (0 when none).</summary>
        public static int MaxTier(TroopCatalogData troops, int buildingLevel) =>
            troops.tiers.Where(t => t.buildingLevel <= buildingLevel).Select(t => t.tier).DefaultIfEmpty(0).Max();

        public static double SpeedPercent(TroopCatalogData troops, int buildingLevel) => troops.speedPerLevel * Math.Max(0, buildingLevel);

        /// <summary>A batch's price: per-troop cost × count, rounded up per resource.</summary>
        public static List<KeyValuePair<ResourceType, long>> Cost(TroopTierData tier, long count)
        {
            var cost = new List<KeyValuePair<ResourceType, long>>();
            void Add(ResourceType type, double perTroop)
            {
                if (perTroop > 0)
                    cost.Add(new KeyValuePair<ResourceType, long>(type, (long)Math.Ceiling(perTroop * count - 1e-9)));
            }

            Add(ResourceType.Food, tier.food);
            Add(ResourceType.Biomass, tier.biomass);
            Add(ResourceType.Titanium, tier.titanium);
            Add(ResourceType.Alloy, tier.alloy);
            return cost;
        }

        /// <summary>Batch time: per-troop seconds × count, divided by the building's speed (K-13).</summary>
        public static int Seconds(TroopCatalogData troops, TroopTierData tier, long count, int buildingLevel) =>
            (int)Math.Max(1, Math.Ceiling(tier.seconds * count / (1 + SpeedPercent(troops, buildingLevel) / 100.0)));

        public static long Power(TroopCatalogData troops, IReadOnlyDictionary<string, long> army) =>
            army.Sum(pair => TryParseKey(pair.Key, out _, out var tier) && Tier(troops, tier) is { } t ? t.power * pair.Value : 0);

        /// <summary>Food and Oxygen the army eats an hour.</summary>
        public static (double Food, double Oxygen) Upkeep(TroopCatalogData troops, IReadOnlyDictionary<string, long> army)
        {
            double food = 0, oxygen = 0;
            foreach (var pair in army)
            {
                if (!TryParseKey(pair.Key, out _, out var tier) || Tier(troops, tier) is not { } t)
                    continue;
                food += t.upkeepFood * pair.Value;
                oxygen += t.upkeepOxygen * pair.Value;
            }

            return (food, oxygen);
        }

        /// <summary>
        /// Checks a training order: the building trains this type and is built, its queue is free, the tier is open
        /// at its level, the count fits the batch and the army, and the price is in the wallet.
        /// </summary>
        public static TroopRejectReason ValidateTrain(TroopCatalogData troops, string? buildingDefinition, int buildingLevel, bool queueBusy,
            string typeId, int tierNumber, long count, long armyNow, long capacity, IReadOnlyDictionary<ResourceType, long> wallet)
        {
            var type = Type(troops, typeId);
            var tier = Tier(troops, tierNumber);
            if (type == null || tier == null)
                return TroopRejectReason.UnknownTroop;
            if (buildingDefinition == null)
                return TroopRejectReason.UnknownBuilding;
            if (type.building != buildingDefinition)
                return TroopRejectReason.WrongBuilding;
            if (buildingLevel <= 0)
                return TroopRejectReason.UnderConstruction;
            if (queueBusy)
                return TroopRejectReason.QueueBusy;
            if (tier.buildingLevel > buildingLevel)
                return TroopRejectReason.TierLocked;
            if (count < 1 || count > BatchSize(troops, buildingLevel))
                return TroopRejectReason.BadCount;
            if (armyNow + count > capacity)
                return TroopRejectReason.ArmyFull;
            return BuildingProgression.CanAfford(Cost(tier, count), wallet) ? TroopRejectReason.None : TroopRejectReason.InsufficientResources;
        }
        // --- Med Bay (docs/rehber 07 §2.6–2.7) ---------------------------------------------

        /// <summary>Wounded the Med Bays hold together (every copy adds up).</summary>
        public static long InfirmaryCapacity(TroopCatalogData troops, IEnumerable<(string DefinitionId, int Level)> buildings) =>
            buildings.Where(b => b.DefinitionId == troops.infirmary.building && b.Level > 0).Sum(b => Interpolate(troops.infirmary.capacity, b.Level));

        /// <summary>Level of the best Med Bay (its speed counts).</summary>
        public static int BestInfirmary(TroopCatalogData troops, IEnumerable<(string DefinitionId, int Level)> buildings) =>
            buildings.Where(b => b.DefinitionId == troops.infirmary.building).Select(b => b.Level).DefaultIfEmpty(0).Max();

        /// <summary>Healing costs a share of the training price (no Superalloy: high tiers stay repairable).</summary>
        public static List<KeyValuePair<ResourceType, long>> HealCost(TroopCatalogData troops, IReadOnlyDictionary<string, long> stacks)
        {
            double food = 0, biomass = 0, titanium = 0;
            foreach (var pair in stacks)
            {
                if (!TryParseKey(pair.Key, out _, out var number) || Tier(troops, number) is not { } tier)
                    continue;
                food += tier.food * pair.Value;
                biomass += tier.biomass * pair.Value;
                titanium += tier.titanium * pair.Value;
            }

            var share = troops.infirmary.costPercent / 100.0;
            var cost = new List<KeyValuePair<ResourceType, long>>();
            void Add(ResourceType type, double amount)
            {
                if (amount > 0)
                    cost.Add(new KeyValuePair<ResourceType, long>(type, (long)Math.Ceiling(amount * share - 1e-9)));
            }

            Add(ResourceType.Food, food);
            Add(ResourceType.Biomass, biomass);
            Add(ResourceType.Titanium, titanium);
            return cost;
        }

        public static int HealSeconds(TroopCatalogData troops, IReadOnlyDictionary<string, long> stacks, int bestInfirmaryLevel)
        {
            double seconds = 0;
            foreach (var pair in stacks)
            {
                if (TryParseKey(pair.Key, out _, out var number) && Tier(troops, number) is { } tier)
                    seconds += tier.seconds * pair.Value;
            }

            var speed = troops.infirmary.speedPerLevel * Math.Max(0, bestInfirmaryLevel);
            return (int)Math.Max(1, Math.Ceiling(seconds * troops.infirmary.timePercent / 100.0 / (1 + speed / 100.0)));
        }

        public static TroopRejectReason ValidateHeal(TroopCatalogData troops, int bestInfirmaryLevel, bool healing,
            IReadOnlyDictionary<string, long> wounded, IReadOnlyDictionary<string, long> request, IReadOnlyDictionary<ResourceType, long> wallet)
        {
            if (bestInfirmaryLevel <= 0)
                return TroopRejectReason.NoMedBay;
            if (healing)
                return TroopRejectReason.HealBusy;
            if (request.Count == 0 || request.Values.All(v => v <= 0))
                return TroopRejectReason.NothingToHeal;
            foreach (var pair in request)
            {
                if (pair.Value < 0 || (wounded.TryGetValue(pair.Key, out var have) ? have : 0) < pair.Value)
                    return TroopRejectReason.NotEnoughWounded;
            }

            return BuildingProgression.CanAfford(HealCost(troops, request), wallet) ? TroopRejectReason.None : TroopRejectReason.InsufficientResources;
        }

        // --- Command capacity and teams (docs/rehber 19 §1.2) ------------------------------

        /// <summary>Troops one hero leads: a base, its level and stars, and the best Military Dome.</summary>
        public static int CommandCapacity(TroopCatalogData troops, int heroLevel, int heroStars, int bestDomeLevel)
        {
            var c = troops.command;
            return Math.Max(0, c.@base + c.perLevel * Math.Max(1, heroLevel) + c.perStar * Math.Max(0, heroStars - 1) + c.perDomeLevel * Math.Max(0, bestDomeLevel));
        }

        public static int BestDome(TroopCatalogData troops, IEnumerable<(string DefinitionId, int Level)> buildings) =>
            buildings.Where(b => b.DefinitionId == troops.capacityBuilding).Select(b => b.Level).DefaultIfEmpty(0).Max();

        /// <summary>The troop type a hero class leads best, or empty.</summary>
        public static string Affinity(TroopCatalogData troops, string heroClass) =>
            troops.affinity.FirstOrDefault(a => a.heroClass == heroClass)?.type ?? string.Empty;

        /// <summary>
        /// Fills each hero's command from the troops at home (Last Asylum's auto-fill): the hero's own troop type first,
        /// then any type, highest tier first. Returns what each hero takes; <paramref name="home"/> is not changed.
        /// </summary>
        public static List<Dictionary<string, long>> FillTeam(TroopCatalogData troops, IReadOnlyDictionary<string, long> home,
            IReadOnlyList<(string Affinity, int Capacity)> heroes)
        {
            var left = home.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            var result = new List<Dictionary<string, long>>();
            foreach (var hero in heroes)
            {
                var taken = new Dictionary<string, long>(StringComparer.Ordinal);
                long need = Math.Max(0, hero.Capacity);
                var order = left.Keys
                    .Select(k => (Key: k, Ok: TryParseKey(k, out var type, out var tier), Type: type, Tier: tier))
                    .Where(k => k.Ok)
                    .OrderByDescending(k => k.Type == hero.Affinity)
                    .ThenByDescending(k => k.Tier)
                    .ThenBy(k => k.Type, StringComparer.Ordinal)
                    .Select(k => k.Key)
                    .ToList();
                foreach (var key in order)
                {
                    if (need <= 0)
                        break;
                    var take = Math.Min(need, left[key]);
                    if (take <= 0)
                        continue;
                    taken[key] = take;
                    left[key] -= take;
                    need -= take;
                }

                result.Add(taken);
            }

            return result;
        }

        /// <summary>What troops carry on a march: count × tier load × type load × the catalog's march scale.</summary>
        public static long MarchLoad(TroopCatalogData troops, IReadOnlyDictionary<string, long> stacks)
        {
            double load = 0;
            foreach (var pair in stacks)
            {
                if (!TryParseKey(pair.Key, out var type, out var number) || Tier(troops, number) is not { } tier)
                    continue;
                load += pair.Value * tier.load * (Type(troops, type)?.load ?? 1);
            }

            return (long)Math.Floor(load * troops.marchLoad);
        }

        /// <summary>A march moves at its slowest troop type (100 = base speed).</summary>
        public static int MarchSpeed(TroopCatalogData troops, IEnumerable<string> keys)
        {
            var speeds = keys.Select(k => TryParseKey(k, out var type, out _) ? Type(troops, type)?.speed ?? 100 : 100).ToList();
            return speeds.Count == 0 ? 100 : Math.Max(1, speeds.Min());
        }
    }
}
