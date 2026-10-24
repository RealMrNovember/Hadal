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
}
