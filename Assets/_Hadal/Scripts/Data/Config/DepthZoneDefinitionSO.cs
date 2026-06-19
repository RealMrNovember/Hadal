using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "DepthZoneDefinition", menuName = "Hadal/Map/Depth Zone Definition")]
    public class DepthZoneDefinitionSO : ScriptableObject
    {
        [SerializeField] private DepthZone _zone;
        [SerializeField] private string _displayName;
        [SerializeField] private float _minDepthMeters;
        [SerializeField] private float _maxDepthMeters;
        [SerializeField] private float _resourceMultiplier = 1f;
        [SerializeField] private float _dangerMultiplier = 1f;
        [SerializeField] private bool _sonarOnly;
        [SerializeField] private List<ExpeditionBiome> _availableBiomes = new();

        public DepthZone Zone => _zone;
        public string DisplayName => _displayName;
        public float MinDepthMeters => _minDepthMeters;
        public float MaxDepthMeters => _maxDepthMeters;
        public float ResourceMultiplier => _resourceMultiplier;
        public float DangerMultiplier => _dangerMultiplier;
        public bool SonarOnly => _sonarOnly;
        public IReadOnlyList<ExpeditionBiome> AvailableBiomes => _availableBiomes;
    }

    [CreateAssetMenu(fileName = "DepthZoneDatabase", menuName = "Hadal/Map/Depth Zone Database")]
    public class DepthZoneDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DepthZoneDefinitionSO> _zones = new();

        public IReadOnlyList<DepthZoneDefinitionSO> Zones => _zones;

        public DepthZoneDefinitionSO Get(DepthZone zone)
        {
            foreach (var z in _zones)
            {
                if (z != null && z.Zone == zone)
                    return z;
            }
            return null;
        }
    }

    [CreateAssetMenu(fileName = "ExpeditionZoneDefinition", menuName = "Hadal/Expeditions/Expedition Zone Definition")]
    public class ExpeditionZoneDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ExpeditionBiome _biome;
        [SerializeField] private DepthZone _depthZone;
        [SerializeField] private float _durationSeconds = 300f;
        [SerializeField] private List<ResourceAmount> _rewards = new();
        [SerializeField] private List<EnemyDefinitionSO> _possibleEncounters = new();

        public string Id => _id;
        public string DisplayName => _displayName;
        public ExpeditionBiome Biome => _biome;
        public DepthZone DepthZone => _depthZone;
        public float DurationSeconds => _durationSeconds;
        public IReadOnlyList<ResourceAmount> Rewards => _rewards;
        public IReadOnlyList<EnemyDefinitionSO> PossibleEncounters => _possibleEncounters;
    }
}
