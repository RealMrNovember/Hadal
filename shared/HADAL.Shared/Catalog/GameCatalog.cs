using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace HADAL.Shared.Catalog
{
    public sealed class ResourceDefinition
    {
        public ResourceType Type { get; internal set; }
        public string DisplayName { get; internal set; } = string.Empty;
        public long StartingAmount { get; internal set; }
        public long MaxCapacity { get; internal set; }
        public bool Critical { get; internal set; }
    }

    public sealed class BuildingDefinition
    {
        public string Id { get; internal set; } = string.Empty;
        public string DisplayName { get; internal set; } = string.Empty;
        public GridSlotType SlotType { get; internal set; }
        public int MinRing { get; internal set; }
        public int MaxRing { get; internal set; }
        public IReadOnlyList<KeyValuePair<ResourceType, long>> Production { get; internal set; } = Array.Empty<KeyValuePair<ResourceType, long>>();
        public IReadOnlyList<KeyValuePair<ResourceType, long>> Consumption { get; internal set; } = Array.Empty<KeyValuePair<ResourceType, long>>();
        public IReadOnlyList<KeyValuePair<ResourceType, long>> BuildCost { get; internal set; } = Array.Empty<KeyValuePair<ResourceType, long>>();

        /// <summary>Level 1 construction time in seconds.</summary>
        public int BuildSeconds { get; internal set; }

        public int MaxLevel { get; internal set; }

        /// <summary>Core Reactor level needed to build it (0 = always).</summary>
        public int RequiredReactorLevel { get; internal set; }

        /// <summary>Most one base may own, counting constructions; 0 = unlimited.</summary>
        public int MaxCount { get; internal set; }

        public bool MatchesSlotType(GridSlotType slotType) =>
            SlotType == GridSlotType.Universal || slotType == GridSlotType.Universal || SlotType == slotType;
    }

    public sealed class ResearchNode
    {
        public string Id { get; internal set; } = string.Empty;
        public string Tree { get; internal set; } = string.Empty;
        public int MaxLevel { get; internal set; }
        public string Effect { get; internal set; } = string.Empty;
        public int ValuePerLevel { get; internal set; }
        public int RequiredCenterLevel { get; internal set; }
        public int BaseSeconds { get; internal set; }
        public IReadOnlyList<KeyValuePair<ResourceType, long>> BaseCost { get; internal set; } = Array.Empty<KeyValuePair<ResourceType, long>>();

        /// <summary>Node id -> level it must have reached.</summary>
        public IReadOnlyList<KeyValuePair<string, int>> Requires { get; internal set; } = Array.Empty<KeyValuePair<string, int>>();
    }

    public sealed class SalvageMission
    {
        public string Id { get; internal set; } = string.Empty;
        public int Seconds { get; internal set; }
        public IReadOnlyList<KeyValuePair<ResourceType, long>> Rewards { get; internal set; } = Array.Empty<KeyValuePair<ResourceType, long>>();
        public int LevelBonusPercent { get; internal set; }
        public int HadaliteChancePercent { get; internal set; }
        public int HadaliteMin { get; internal set; }
        public int HadaliteMax { get; internal set; }
    }

    public sealed class RingUnlock
    {
        public int Ring { get; internal set; }
        public int ReactorLevel { get; internal set; }
        public string RequiresBuilding { get; internal set; } = string.Empty;
        public IReadOnlyList<KeyValuePair<ResourceType, long>> Cost { get; internal set; } = Array.Empty<KeyValuePair<ResourceType, long>>();
    }

    /// <summary>
    /// Validated, indexed view of <see cref="GameCatalogData"/> — the single definition of
    /// buildings, resources and base layout. The server validates and simulates with it; the
    /// Unity setup generates its ScriptableObjects from the same file, so the two cannot drift.
    /// </summary>
    public sealed class GameCatalog
    {
        private readonly Dictionary<ResourceType, ResourceDefinition> _resources = new Dictionary<ResourceType, ResourceDefinition>();
        private readonly Dictionary<string, BuildingDefinition> _buildings = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<int, RingData> _rings = new Dictionary<int, RingData>();
        private readonly Dictionary<int, RingUnlock> _ringUnlocks = new Dictionary<int, RingUnlock>();
        private readonly List<ResearchNode> _research = new List<ResearchNode>();
        private readonly Dictionary<string, ResearchNode> _researchById = new Dictionary<string, ResearchNode>(StringComparer.Ordinal);
        private readonly List<SalvageMission> _salvage = new List<SalvageMission>();

        private GameCatalog(GameCatalogData data)
        {
            Data = data;
        }

        public GameCatalogData Data { get; }

        public GridLayoutData Grid => Data.grid;

        public IEnumerable<ResourceDefinition> Resources => _resources.Values;

        public IEnumerable<BuildingDefinition> Buildings => _buildings.Values;

        /// <summary>Level progression rules; defaults when the catalog has none.</summary>
        public ProgressionData Progression => Data.progression ?? new ProgressionData();

        public ResearchCatalogData Research => Data.research ?? new ResearchCatalogData();

        /// <summary>Research nodes in catalog order (the screen shows them in this order).</summary>
        public IReadOnlyList<ResearchNode> ResearchNodes => _research;

        public BoostData Boost => Data.boost ?? new BoostData();

        public IReadOnlyList<KeyValuePair<ResourceType, long>> BoostCost { get; private set; } = Array.Empty<KeyValuePair<ResourceType, long>>();

        public OverdriveData Overdrive => Data.overdrive ?? new OverdriveData();

        public IReadOnlyList<SalvageMission> SalvageMissions => _salvage;

        /// <summary>Parses and validates. Throws <see cref="FormatException"/> listing every problem.</summary>
        public static GameCatalog Create(GameCatalogData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var errors = new List<string>();
            var catalog = new GameCatalog(data);

            foreach (var resource in data.resources ?? Array.Empty<ResourceDefinitionData>())
            {
                if (!TryParseResource(resource?.type, out var type))
                {
                    errors.Add($"resource: unknown type '{resource?.type}'");
                    continue;
                }

                if (catalog._resources.ContainsKey(type))
                    errors.Add($"resource: duplicate '{type}'");

                catalog._resources[type] = new ResourceDefinition
                {
                    Type = type,
                    DisplayName = resource.displayName ?? string.Empty,
                    StartingAmount = resource.startingAmount,
                    MaxCapacity = resource.maxCapacity,
                    Critical = resource.critical
                };
            }

            foreach (var building in data.buildings ?? Array.Empty<BuildingDefinitionData>())
            {
                if (building == null || string.IsNullOrEmpty(building.id))
                {
                    errors.Add("building: missing id");
                    continue;
                }

                if (catalog._buildings.ContainsKey(building.id))
                    errors.Add($"building '{building.id}': duplicate id");

                if (!TryParseSlotType(building.slotType, out var slotType))
                    errors.Add($"building '{building.id}': unknown slotType '{building.slotType}'");

                if (building.minRing < 0 || building.maxRing < building.minRing)
                    errors.Add($"building '{building.id}': invalid ring range {building.minRing}..{building.maxRing}");

                catalog._buildings[building.id] = new BuildingDefinition
                {
                    Id = building.id,
                    DisplayName = building.displayName ?? string.Empty,
                    SlotType = slotType,
                    MinRing = building.minRing,
                    MaxRing = building.maxRing,
                    Production = ParseAmounts(building.id, "production", building.production, errors),
                    Consumption = ParseAmounts(building.id, "consumption", building.consumption, errors),
                    BuildCost = ParseAmounts(building.id, "buildCost", building.buildCost, errors),
                    BuildSeconds = Math.Max(0, building.buildSeconds),
                    MaxLevel = building.maxLevel > 0 ? building.maxLevel : Math.Max(1, (data.progression ?? new ProgressionData()).maxLevel),
                    RequiredReactorLevel = Math.Max(0, building.requiredReactorLevel),
                    MaxCount = Math.Max(0, building.maxCount)
                };
            }

            var grid = data.grid ?? new GridLayoutData();
            if (grid.startingUnlockedRings < 0 || grid.startingUnlockedRings > grid.maxRings)
                errors.Add($"grid: startingUnlockedRings {grid.startingUnlockedRings} outside 0..{grid.maxRings}");

            foreach (var ring in grid.rings ?? Array.Empty<RingData>())
            {
                if (ring == null)
                    continue;

                if (catalog._rings.ContainsKey(ring.ring))
                    errors.Add($"grid: duplicate ring {ring.ring}");

                if (!TryParseSlotType(ring.defaultSlotType, out _))
                    errors.Add($"grid ring {ring.ring}: unknown defaultSlotType '{ring.defaultSlotType}'");

                foreach (var sector in ring.sectors ?? Array.Empty<SectorData>())
                {
                    if (sector == null)
                        continue;

                    if (sector.sector < 0 || sector.sector >= ring.sectorCount)
                        errors.Add($"grid ring {ring.ring}: sector {sector.sector} outside 0..{ring.sectorCount - 1}");

                    if (!TryParseSlotType(sector.slotType, out _))
                        errors.Add($"grid ring {ring.ring} sector {sector.sector}: unknown slotType '{sector.slotType}'");

                    if (!string.IsNullOrEmpty(sector.preferredBuildingId) && !catalog._buildings.ContainsKey(sector.preferredBuildingId))
                        errors.Add($"grid ring {ring.ring} sector {sector.sector}: unknown building '{sector.preferredBuildingId}'");
                }

                catalog._rings[ring.ring] = ring;
            }

            foreach (var node in (data.research ?? new ResearchCatalogData()).nodes ?? Array.Empty<ResearchNodeData>())
            {
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    errors.Add("research: node without id");
                    continue;
                }

                if (catalog._researchById.ContainsKey(node.id))
                    errors.Add($"research '{node.id}': duplicate id");
                if (node.maxLevel < 1)
                    errors.Add($"research '{node.id}': maxLevel must be at least 1");
                if (!ResearchRules.IsKnownEffect(node.effect, id => catalog._buildings.ContainsKey(id)))
                    errors.Add($"research '{node.id}': unknown effect '{node.effect}'");

                var requires = new List<KeyValuePair<string, int>>();
                foreach (var requirement in node.requires ?? Array.Empty<ResearchRequirementData>())
                {
                    if (requirement != null)
                        requires.Add(new KeyValuePair<string, int>(requirement.id, Math.Max(1, requirement.level)));
                }

                var parsed = new ResearchNode
                {
                    Id = node.id,
                    Tree = node.tree ?? string.Empty,
                    MaxLevel = Math.Max(1, node.maxLevel),
                    Effect = node.effect ?? string.Empty,
                    ValuePerLevel = node.valuePerLevel,
                    RequiredCenterLevel = Math.Max(1, node.requiredCenterLevel),
                    BaseSeconds = Math.Max(0, node.baseSeconds),
                    BaseCost = ParseAmounts("research " + node.id, "baseCost", node.baseCost, errors),
                    Requires = requires
                };
                catalog._research.Add(parsed);
                catalog._researchById[node.id] = parsed;
            }

            foreach (var node in catalog._research)
            {
                foreach (var requirement in node.Requires)
                {
                    if (!catalog._researchById.ContainsKey(requirement.Key))
                        errors.Add($"research '{node.Id}': requires unknown node '{requirement.Key}'");
                }
            }

            catalog.BoostCost = ParseAmounts("boost", "cost", (data.boost ?? new BoostData()).cost, errors);

            foreach (var mission in data.salvageMissions ?? Array.Empty<SalvageMissionData>())
            {
                if (mission == null || string.IsNullOrEmpty(mission.id))
                    continue;
                if (mission.seconds <= 0)
                    errors.Add($"salvage '{mission.id}': seconds must be positive");
                catalog._salvage.Add(new SalvageMission
                {
                    Id = mission.id,
                    Seconds = Math.Max(1, mission.seconds),
                    Rewards = ParseAmounts("salvage " + mission.id, "rewards", mission.rewards, errors),
                    LevelBonusPercent = Math.Max(0, mission.levelBonusPercent),
                    HadaliteChancePercent = Math.Max(0, Math.Min(100, mission.hadaliteChancePercent)),
                    HadaliteMin = Math.Max(0, mission.hadaliteMin),
                    HadaliteMax = Math.Max(Math.Max(0, mission.hadaliteMin), mission.hadaliteMax)
                });
            }

            foreach (var unlock in data.ringUnlocks ?? Array.Empty<RingUnlockData>())
            {
                if (unlock == null)
                    continue;

                if (unlock.ring <= grid.startingUnlockedRings || unlock.ring > grid.maxRings)
                    errors.Add($"ringUnlocks: ring {unlock.ring} outside {grid.startingUnlockedRings + 1}..{grid.maxRings}");
                if (catalog._ringUnlocks.ContainsKey(unlock.ring))
                    errors.Add($"ringUnlocks: duplicate ring {unlock.ring}");
                if (!string.IsNullOrEmpty(unlock.requiresBuilding) && !catalog._buildings.ContainsKey(unlock.requiresBuilding))
                    errors.Add($"ringUnlocks ring {unlock.ring}: unknown building '{unlock.requiresBuilding}'");

                catalog._ringUnlocks[unlock.ring] = new RingUnlock
                {
                    Ring = unlock.ring,
                    ReactorLevel = Math.Max(0, unlock.reactorLevel),
                    RequiresBuilding = unlock.requiresBuilding ?? string.Empty,
                    Cost = ParseAmounts("ring " + unlock.ring, "cost", unlock.cost, errors)
                };
            }

            if (errors.Count > 0)
                throw new FormatException("Invalid game catalog:\n  " + string.Join("\n  ", errors));

            return catalog;
        }

        public bool TryGetBuilding(string id, out BuildingDefinition building)
        {
            building = null!;
            return !string.IsNullOrEmpty(id) && _buildings.TryGetValue(id, out building);
        }

        public bool TryGetResearch(string id, out ResearchNode node)
        {
            node = null!;
            return !string.IsNullOrEmpty(id) && _researchById.TryGetValue(id, out node);
        }

        public bool TryGetSalvageMission(string id, out SalvageMission mission)
        {
            mission = _salvage.Find(m => m.Id == id)!;
            return mission != null;
        }

        /// <summary>Requirements of opening <paramref name="ring"/>; false when the catalog defines none.</summary>
        public bool TryGetRingUnlock(int ring, out RingUnlock unlock) => _ringUnlocks.TryGetValue(ring, out unlock);

        public bool TryGetResource(ResourceType type, out ResourceDefinition resource) =>
            _resources.TryGetValue(type, out resource);

        public int GetSectorCount(int ring)
        {
            if (_rings.TryGetValue(ring, out var def))
                return def.sectorCount;

            if (ring <= 0)
                return 1;

            if (ring <= 3)
                return 3;

            var sectors = Grid.outerRingBaseSectors + (ring - 4) * Grid.outerRingSectorIncrement;
            return Math.Min(sectors, Grid.outerRingMaxSectors);
        }

        public bool SlotExists(int ring, int sector) =>
            ring >= 0 && ring <= Grid.maxRings && sector >= 0 && sector < GetSectorCount(ring);

        public GridSlotType GetSlotType(int ring, int sector)
        {
            var sectorDef = FindSector(ring, sector);
            if (sectorDef != null && TryParseSlotType(sectorDef.slotType, out var sectorType))
                return sectorType;

            if (_rings.TryGetValue(ring, out var ringDef) && TryParseSlotType(ringDef.defaultSlotType, out var ringType))
                return ringType;

            return GridSlotType.Universal;
        }

        public bool SectorStartsLocked(int ring, int sector) => FindSector(ring, sector)?.startsLocked ?? false;

        public bool SectorAllowsBuilding(int ring, int sector, string buildingId)
        {
            var allowed = FindSector(ring, sector)?.allowedBuildingIds;
            if (allowed == null || allowed.Length == 0)
                return true;

            return Array.IndexOf(allowed, buildingId) >= 0;
        }

        /// <summary>Production rules in the shape <see cref="ProductionSimulator"/> consumes.</summary>
        public ProductionCatalog ToProductionCatalog()
        {
            var rules = new List<ProductionRuleDto>();
            foreach (var building in _buildings.Values)
            {
                var perResource = new Dictionary<ResourceType, ProductionRuleDto>();
                Accumulate(perResource, building.Id, building.Production, isConsumption: false);
                Accumulate(perResource, building.Id, building.Consumption, isConsumption: true);
                rules.AddRange(perResource.Values);
            }

            return new ProductionCatalog(rules);
        }

        public Dictionary<ResourceType, long> StartingWallet()
        {
            var wallet = new Dictionary<ResourceType, long>();
            foreach (var resource in _resources.Values)
            {
                if (resource.StartingAmount > 0)
                    wallet[resource.Type] = resource.StartingAmount;
            }

            return wallet;
        }

        /// <summary>Buildings every new base starts with: the preferred building of each ring-0 sector.</summary>
        public IEnumerable<(string DefinitionId, int Ring, int Sector)> StarterBuildings()
        {
            if (!_rings.TryGetValue(0, out var core))
                yield break;

            foreach (var sector in core.sectors)
            {
                if (!string.IsNullOrEmpty(sector.preferredBuildingId) && _buildings.ContainsKey(sector.preferredBuildingId))
                    yield return (sector.preferredBuildingId, 0, sector.sector);
            }
        }

        public Dictionary<ResourceType, long> Capacities()
        {
            var caps = new Dictionary<ResourceType, long>();
            foreach (var resource in _resources.Values)
                caps[resource.Type] = resource.MaxCapacity;

            return caps;
        }

        public static bool TryParseResource(string? value, out ResourceType type) =>
            Enum.TryParse(value, ignoreCase: false, out type) && type != ResourceType.Unknown && Enum.IsDefined(typeof(ResourceType), type);

        public static bool TryParseSlotType(string? value, out GridSlotType type) =>
            Enum.TryParse(value, ignoreCase: false, out type) && Enum.IsDefined(typeof(GridSlotType), type);

        private SectorData? FindSector(int ring, int sector)
        {
            if (!_rings.TryGetValue(ring, out var ringDef) || ringDef.sectors == null)
                return null;

            foreach (var candidate in ringDef.sectors)
            {
                if (candidate != null && candidate.sector == sector)
                    return candidate;
            }

            return null;
        }

        private static IReadOnlyList<KeyValuePair<ResourceType, long>> ParseAmounts(
            string buildingId, string field, ResourceAmountData[]? amounts, List<string> errors)
        {
            var result = new List<KeyValuePair<ResourceType, long>>();
            foreach (var amount in amounts ?? Array.Empty<ResourceAmountData>())
            {
                if (amount == null)
                    continue;

                if (!TryParseResource(amount.resource, out var type))
                {
                    errors.Add($"building '{buildingId}' {field}: unknown resource '{amount.resource}'");
                    continue;
                }

                if (amount.amount < 0)
                    errors.Add($"building '{buildingId}' {field}: negative amount for {type}");

                result.Add(new KeyValuePair<ResourceType, long>(type, amount.amount));
            }

            return result;
        }

        private static void Accumulate(
            IDictionary<ResourceType, ProductionRuleDto> target,
            string buildingId,
            IReadOnlyList<KeyValuePair<ResourceType, long>> amounts,
            bool isConsumption)
        {
            foreach (var pair in amounts)
            {
                if (pair.Value == 0L)
                    continue;

                if (!target.TryGetValue(pair.Key, out var rule))
                {
                    rule = new ProductionRuleDto { BuildingDefinitionId = buildingId, Resource = pair.Key };
                    target[pair.Key] = rule;
                }

                if (isConsumption)
                    rule.ConsumptionPerTick += pair.Value;
                else
                    rule.ProductionPerTick += pair.Value;
            }
        }
    }
}
