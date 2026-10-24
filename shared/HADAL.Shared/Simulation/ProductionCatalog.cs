using System.Collections.Generic;
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
