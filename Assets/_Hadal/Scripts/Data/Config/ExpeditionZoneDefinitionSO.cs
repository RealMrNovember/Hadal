using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Config
{
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
