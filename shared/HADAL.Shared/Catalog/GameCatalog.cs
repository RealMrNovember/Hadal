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

        public bool MatchesSlotType(GridSlotType slotType) =>
            SlotType == GridSlotType.Universal || slotType == GridSlotType.Universal || SlotType == slotType;
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

        private GameCatalog(GameCatalogData data)
        {
            Data = data;
        }

        public GameCatalogData Data { get; }

        public GridLayoutData Grid => Data.grid;

        public IEnumerable<ResourceDefinition> Resources => _resources.Values;

        public IEnumerable<BuildingDefinition> Buildings => _buildings.Values;

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
                    BuildCost = ParseAmounts(building.id, "buildCost", building.buildCost, errors)
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

            if (errors.Count > 0)
                throw new FormatException("Invalid game catalog:\n  " + string.Join("\n  ", errors));

            return catalog;
        }

        public bool TryGetBuilding(string id, out BuildingDefinition building)
        {
            building = null!;
            return !string.IsNullOrEmpty(id) && _buildings.TryGetValue(id, out building);
        }

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
