using HADAL.Shared.Enums;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "BuildingDefinition", menuName = "Hadal/Buildings/Building Definition")]
    public class BuildingDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private BuildingCategory _category;
        [SerializeField] private DomeType _domeType;
        [SerializeField] private GridSlotType _requiredSlotType = GridSlotType.Universal;
        [SerializeField] private int _minRing;
        [SerializeField] private int _maxRing = 12;
        [SerializeField] private int _maxLevel = 10;
        [SerializeField] private bool _allowRotation = true;
        [SerializeField] private float _rotationSnapOverride;
        [SerializeField] private List<ResourceAmount> _buildCosts = new();
        [SerializeField] private List<ResourceAmount> _productionPerTick = new();
        [SerializeField] private List<ResourceAmount> _consumptionPerTick = new();
        [SerializeField] private float _pressureResistanceBonus;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject _ghostPrefab;

        /// <summary>
        /// The building's evolution (docs/rehber/04_bina_evrimi.md): one model per look tier, tier 1 first. A tier
        /// without its own model shows the closest lower one; with none at all the single <see cref="Prefab"/> stays.
        /// </summary>
        [SerializeField] private List<GameObject> _tierPrefabs = new();

        public string Id => _id;
        public string DisplayName => _displayName;
        public BuildingCategory Category => _category;
        public DomeType DomeType => _domeType;
        public GridSlotType RequiredSlotType => _requiredSlotType;
        public int MinRing => _minRing;
        public int MaxRing => _maxRing;
        public int MaxLevel => _maxLevel;
        public bool AllowRotation => _allowRotation;
        public float RotationSnapOverride => _rotationSnapOverride;
        public IReadOnlyList<ResourceAmount> BuildCosts => _buildCosts;
        public IReadOnlyList<ResourceAmount> ProductionPerTick => _productionPerTick;
        public IReadOnlyList<ResourceAmount> ConsumptionPerTick => _consumptionPerTick;
        public float PressureResistanceBonus => _pressureResistanceBonus;
        public GameObject Prefab => _prefab;
        public GameObject GhostPrefab => _ghostPrefab != null ? _ghostPrefab : _prefab;

        /// <summary>The model for a building at <paramref name="level"/>: its tier's (or the closest lower tier's), else the single prefab.</summary>
        public GameObject PrefabFor(int level)
        {
            for (var tier = HADAL.Shared.Simulation.BuildingVisuals.TierFor(level); tier >= 1; tier--)
            {
                if (tier - 1 < _tierPrefabs.Count && _tierPrefabs[tier - 1] != null)
                    return _tierPrefabs[tier - 1];
            }

            return _prefab;
        }

        public bool MatchesSlotType(GridSlotType slotType)
        {
            if (_requiredSlotType == GridSlotType.Universal || slotType == GridSlotType.Universal)
                return true;

            return _requiredSlotType == slotType;
        }
    }
}
