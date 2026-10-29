using System;
using System.Collections.Generic;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance research (docs/31 step 6, Last Asylum's alliance technology): a Development and a War tab of nodes, each
    /// up to <see cref="MaxLevel"/>. Officers (R4+) pick the focus; members donate resources to it for research points
    /// (and their own donation score), at most <see cref="DonationsPerDay"/> times a UTC day. Every level gives the whole
    /// alliance its bonus through PlayerModifiers.
    /// <code>
    ///   node             tab           bonus per level     donated resource
    ///   dev_production   development   +1 % production     Titanium
    ///   dev_build        development   +1 % build speed    Titanium
    ///   dev_research     development   +1 % research speed Biomass
    ///   dev_storage      development   +2 % storage        Biomass
    ///   dev_gather       development   +2 % gathering      Food
    ///   war_march        war           +2 % march speed    Food
    ///   war_load         war           +3 % troop load     Food
    ///   points to the next level: 1,000 × (level + 1)²; a donation: 5,000 × (level + 1) of the resource for 100 points
    /// </code>
    /// </summary>
    public static class AllianceResearchRules
    {
        public const string Development = "development";
        public const string War = "war";

        public const int MaxLevel = 10;
        public const int DonationsPerDay = 20;
        public const long PointsPerDonation = 100;

        public sealed class Node
        {
            public Node(string id, string tab, int perLevel, ResourceType resource)
            {
                Id = id;
                Tab = tab;
                PerLevel = perLevel;
                Resource = resource;
            }

            public string Id { get; }
            public string Tab { get; }

            /// <summary>The bonus percent each level adds.</summary>
            public int PerLevel { get; }

            /// <summary>What members donate to it.</summary>
            public ResourceType Resource { get; }
        }

        public static readonly Node[] Nodes =
        {
            new Node("dev_production", Development, 1, ResourceType.Titanium),
            new Node("dev_build", Development, 1, ResourceType.Titanium),
            new Node("dev_research", Development, 1, ResourceType.Biomass),
            new Node("dev_storage", Development, 2, ResourceType.Biomass),
            new Node("dev_gather", Development, 2, ResourceType.Food),
            new Node("war_march", War, 2, ResourceType.Food),
            new Node("war_load", War, 3, ResourceType.Food)
        };

        public static Node? Find(string? id) => Array.Find(Nodes, n => n.Id == id);

        /// <summary>Research points from <paramref name="level"/> to the next.</summary>
        public static long PointsToNext(int level) => level >= MaxLevel ? 0 : 1_000L * (level + 1) * (level + 1);

        /// <summary>What one donation to a node at <paramref name="level"/> costs.</summary>
        public static long DonationCost(int level) => 5_000L * (Math.Max(0, level) + 1);

        /// <summary>The bonus percent of a node at a level.</summary>
        public static int Bonus(Node node, int level) => node.PerLevel * Math.Max(0, Math.Min(MaxLevel, level));

        /// <summary>Adds the alliance's research to a player's modifiers.</summary>
        public static void Apply(PlayerModifiers modifiers, IReadOnlyDictionary<string, int>? levels)
        {
            if (levels == null)
                return;
            foreach (var node in Nodes)
            {
                if (!levels.TryGetValue(node.Id, out var level) || level <= 0)
                    continue;
                var bonus = Bonus(node, level);
                switch (node.Id)
                {
                    case "dev_production":
                        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                        {
                            if (type is ResourceType.Unknown or ResourceType.Research or ResourceType.Alloy or ResourceType.Hadalite)
                                continue;
                            modifiers.ProductionPercent[type] = (modifiers.ProductionPercent.TryGetValue(type, out var p) ? p : 0) + bonus;
                        }

                        break;
                    case "dev_build":
                        modifiers.BuildTimePercent -= bonus;
                        break;
                    case "dev_research":
                        modifiers.ResearchTimePercent -= bonus;
                        break;
                    case "dev_storage":
                        modifiers.CapacityPercent += bonus;
                        break;
                    case "dev_gather":
                        modifiers.GatherPercent += bonus;
                        break;
                    case "war_march":
                        modifiers.MarchSpeedPercent += bonus;
                        break;
                    case "war_load":
                        modifiers.MarchLoadPercent += bonus;
                        break;
                }
            }
        }
    }
}
