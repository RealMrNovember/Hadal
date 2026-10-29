using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a research was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum ResearchRejectReason
    {
        None = 0,
        UnknownNode = 1,
        NoResearchCenter = 2,
        MaxLevel = 3,
        AlreadyResearching = 4,
        ResearchBusy = 5,
        CenterLevelTooLow = 6,
        PrerequisiteMissing = 7,
        InsufficientResources = 8,
        NotResearching = 9,
        NotYetFree = 10,
    }

    /// <summary>
    /// Everything research changes, summed over the player's node levels. The server applies it;
    /// the client uses the same numbers to show costs, times and limits.
    /// </summary>
    public sealed class PlayerModifiers
    {
        public static readonly PlayerModifiers None = new PlayerModifiers();

        /// <summary>Percent added to construction time (negative = faster).</summary>
        public int BuildTimePercent { get; internal set; }

        public int ResearchTimePercent { get; internal set; }

        /// <summary>Percent added to each resource's production.</summary>
        public Dictionary<ResourceType, int> ProductionPercent { get; } = new Dictionary<ResourceType, int>();

        /// <summary>Percent added to every storage cap.</summary>
        public int CapacityPercent { get; internal set; }

        /// <summary>Extra copies allowed per building definition.</summary>
        public Dictionary<string, int> ExtraLimits { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Percent added to march speed (VIP); trips take raw / (1 + speed% / 100).</summary>
        public int MarchSpeedPercent { get; internal set; }

        public int ExtraBuilders { get; internal set; }
        public int ExtraResearchSlots { get; internal set; }

        /// <summary>
        /// Time bonuses divide (docs/rehber K-13): time = raw / (1 + speed% / 100), where a negative
        /// percent is a speed-up. Stacking bonuses never reach zero; a positive percent slows down.
        /// </summary>
        public static int ApplyPercent(int seconds, int percent)
        {
            if (seconds <= 0)
                return seconds;
            var scaled = percent < 0 ? seconds / (1.0 - percent / 100.0) : seconds * (1.0 + percent / 100.0);
            return Math.Max(1, (int)Math.Round(scaled));
        }

        public int BuildSeconds(int seconds) => ApplyPercent(seconds, BuildTimePercent);

        public int ResearchSeconds(int seconds) => ApplyPercent(seconds, ResearchTimePercent);

        public int MarchSeconds(int seconds) => ApplyPercent(seconds, -MarchSpeedPercent);

        /// <summary>0 = unlimited, as in the catalog.</summary>
        public int MaxCount(BuildingDefinition building) =>
            building.MaxCount <= 0 ? 0 : building.MaxCount + (ExtraLimits.TryGetValue(building.Id, out var extra) ? extra : 0);

        public Dictionary<ResourceType, long> Capacities(IReadOnlyDictionary<ResourceType, long> baseCapacities)
        {
            var caps = new Dictionary<ResourceType, long>();
            foreach (var pair in baseCapacities)
                caps[pair.Key] = pair.Value <= 0 ? pair.Value : pair.Value * (100L + CapacityPercent) / 100L;
            return caps;
        }
    }

    /// <summary>Research costs, times, requirements and effects (docs/24_Building_Functions.md).</summary>
    public static class ResearchRules
    {
        public const string CenterId = "research_center";

        public static List<KeyValuePair<ResourceType, long>> Cost(ResearchNode node, int targetLevel, ResearchCatalogData rules)
        {
            var factor = Math.Pow(rules.costGrowth, Math.Max(0, targetLevel - 1));
            var cost = new List<KeyValuePair<ResourceType, long>>(node.BaseCost.Count);
            foreach (var pair in node.BaseCost)
                cost.Add(new KeyValuePair<ResourceType, long>(pair.Key, (long)Math.Round(pair.Value * factor)));
            return cost;
        }

        /// <summary>Seconds before research-time bonuses.</summary>
        public static int BaseSeconds(ResearchNode node, int targetLevel, ResearchCatalogData rules) =>
            (int)Math.Round(node.BaseSeconds * Math.Pow(rules.timeGrowth, Math.Max(0, targetLevel - 1)));

        public static int CenterLevelFor(ResearchNode node, int targetLevel, ResearchCatalogData rules) =>
            node.RequiredCenterLevel + Math.Max(0, rules.centerStep) * Math.Max(0, targetLevel - 1);

        public static int Slots(ResearchCatalogData rules, PlayerModifiers modifiers) =>
            Math.Max(1, rules.baseSlots) + modifiers.ExtraResearchSlots;

        /// <summary>Checks in the order the player should hear about them.</summary>
        public static ResearchRejectReason Validate(
            GameCatalog catalog,
            string nodeId,
            IReadOnlyDictionary<string, int> levels,
            ICollection<string> inProgress,
            int centerLevel,
            PlayerModifiers modifiers,
            IReadOnlyDictionary<ResourceType, long> wallet)
        {
            if (!catalog.TryGetResearch(nodeId, out var node))
                return ResearchRejectReason.UnknownNode;
            if (centerLevel <= 0)
                return ResearchRejectReason.NoResearchCenter;

            var current = levels.TryGetValue(nodeId, out var l) ? l : 0;
            if (current >= node.MaxLevel)
                return ResearchRejectReason.MaxLevel;
            if (inProgress.Contains(nodeId))
                return ResearchRejectReason.AlreadyResearching;
            if (inProgress.Count >= Slots(catalog.Research, modifiers))
                return ResearchRejectReason.ResearchBusy;
            if (centerLevel < CenterLevelFor(node, current + 1, catalog.Research))
                return ResearchRejectReason.CenterLevelTooLow;

            foreach (var requirement in node.Requires)
            {
                if (!levels.TryGetValue(requirement.Key, out var have) || have < requirement.Value)
                    return ResearchRejectReason.PrerequisiteMissing;
            }

            return BuildingProgression.CanAfford(Cost(node, current + 1, catalog.Research), wallet)
                ? ResearchRejectReason.None
                : ResearchRejectReason.InsufficientResources;
        }

        /// <summary>Sums every researched level into one set of modifiers.</summary>
        public static PlayerModifiers Modifiers(GameCatalog catalog, IReadOnlyDictionary<string, int> levels)
        {
            var modifiers = new PlayerModifiers();
            if (levels == null)
                return modifiers;

            foreach (var pair in levels)
            {
                if (pair.Value <= 0 || !catalog.TryGetResearch(pair.Key, out var node))
                    continue;

                var value = node.ValuePerLevel * Math.Min(pair.Value, node.MaxLevel);
                var effect = node.Effect;
                var colon = effect.IndexOf(':');
                var kind = colon < 0 ? effect : effect.Substring(0, colon);
                var target = colon < 0 ? string.Empty : effect.Substring(colon + 1);

                switch (kind)
                {
                    case "buildTime": modifiers.BuildTimePercent += value; break;
                    case "researchTime": modifiers.ResearchTimePercent += value; break;
                    case "capacity": modifiers.CapacityPercent += value; break;
                    case "builderSlots": modifiers.ExtraBuilders += value; break;
                    case "researchSlots": modifiers.ExtraResearchSlots += value; break;
                    case "production":
                        if (GameCatalog.TryParseResource(target, out var resource))
                            modifiers.ProductionPercent[resource] = (modifiers.ProductionPercent.TryGetValue(resource, out var p) ? p : 0) + value;
                        break;
                    case "limit":
                        modifiers.ExtraLimits[target] = (modifiers.ExtraLimits.TryGetValue(target, out var e) ? e : 0) + value;
                        break;
                }
            }

            return modifiers;
        }

        /// <summary>Effect kinds the catalog may use (validation).</summary>
        public static bool IsKnownEffect(string effect, Func<string, bool> buildingExists)
        {
            if (string.IsNullOrEmpty(effect))
                return false;
            var colon = effect.IndexOf(':');
            var kind = colon < 0 ? effect : effect.Substring(0, colon);
            var target = colon < 0 ? string.Empty : effect.Substring(colon + 1);
            return kind switch
            {
                "buildTime" or "researchTime" or "builderSlots" or "researchSlots" => colon < 0,
                "capacity" => target == "All",
                "production" => GameCatalog.TryParseResource(target, out _),
                "limit" => buildingExists(target),
                _ => false
            };
        }
    }
}
