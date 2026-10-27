using System;
using System.Collections.Generic;
using HADAL.Shared.Enums;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Lookup of <see cref="ProductionRuleDto"/> by building definition id.
    /// The server builds this from the database; the client stub builds it from
    /// ScriptableObjects. Both feed the same <see cref="ProductionSimulator"/>.
    /// </summary>
    public sealed class ProductionCatalog
    {
        private static readonly ProductionRuleDto[] NoRules = new ProductionRuleDto[0];

        private readonly Dictionary<string, List<ProductionRuleDto>> _byBuilding =
            new Dictionary<string, List<ProductionRuleDto>>();

        public ProductionCatalog(IEnumerable<ProductionRuleDto> rules)
        {
            if (rules == null)
                return;

            foreach (var rule in rules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.BuildingDefinitionId))
                    continue;

                if (!_byBuilding.TryGetValue(rule.BuildingDefinitionId, out var list))
                {
                    list = new List<ProductionRuleDto>();
                    _byBuilding[rule.BuildingDefinitionId] = list;
                }

                list.Add(rule);
            }
        }

        public static ProductionCatalog Empty { get; } = new ProductionCatalog(null);

        // Per-level hourly tables (docs/rehber K-17) in thousandths per tick: building -> level rows.
        private readonly Dictionary<string, IReadOnlyList<(IReadOnlyDictionary<ResourceType, long> Production, IReadOnlyDictionary<ResourceType, long> Consumption)>> _levels =
            new Dictionary<string, IReadOnlyList<(IReadOnlyDictionary<ResourceType, long>, IReadOnlyDictionary<ResourceType, long>)>>();

        public void SetLevelTable(string buildingId, IReadOnlyList<(IReadOnlyDictionary<ResourceType, long> Production, IReadOnlyDictionary<ResourceType, long> Consumption)> rows)
        {
            if (!string.IsNullOrEmpty(buildingId) && rows != null && rows.Count > 0)
                _levels[buildingId] = rows;
        }

        /// <summary>The level row of a building with a table; false when it uses the per-tick formula rules.</summary>
        public bool TryGetLevel(string buildingId, int level, out IReadOnlyDictionary<ResourceType, long> production, out IReadOnlyDictionary<ResourceType, long> consumption)
        {
            production = consumption = null!;
            if (!_levels.TryGetValue(buildingId, out var rows) || rows.Count == 0)
                return false;
            var row = rows[Math.Max(0, Math.Min(rows.Count - 1, level - 1))];
            production = row.Production;
            consumption = row.Consumption;
            return true;
        }

        public int BuildingCount => _byBuilding.Count;

        public IReadOnlyList<ProductionRuleDto> GetRules(string buildingDefinitionId)
        {
            if (!string.IsNullOrEmpty(buildingDefinitionId) &&
                _byBuilding.TryGetValue(buildingDefinitionId, out var list))
            {
                return list;
            }

            return NoRules;
        }
    }
}
