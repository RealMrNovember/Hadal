using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "PressureTierDefinition", menuName = "Hadal/Pressure/Pressure Tier Definition")]
    public class PressureTierDefinitionSO : ScriptableObject
    {
        [SerializeField] private PressureTier _tier;
        [SerializeField] private float _depthMeters;
        [SerializeField] private float _pressureValue;
        [SerializeField] private float _requiredHullStrength;
        [SerializeField] private float _requiredPressureShield;
        [SerializeField] private string _warningMessage;

        public PressureTier Tier => _tier;
        public float DepthMeters => _depthMeters;
        public float PressureValue => _pressureValue;
        public float RequiredHullStrength => _requiredHullStrength;
        public float RequiredPressureShield => _requiredPressureShield;
        public string WarningMessage => _warningMessage;
    }

    [CreateAssetMenu(fileName = "PressureDatabase", menuName = "Hadal/Pressure/Pressure Database")]
    public class PressureDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<PressureTierDefinitionSO> _tiers = new();

        public IReadOnlyList<PressureTierDefinitionSO> Tiers => _tiers;

        public PressureTierDefinitionSO GetTierForDepth(float depthMeters)
        {
            PressureTierDefinitionSO best = null;
            foreach (var tier in _tiers)
            {
                if (tier != null && tier.DepthMeters <= depthMeters)
                {
                    if (best == null || tier.DepthMeters > best.DepthMeters)
                        best = tier;
                }
            }
            return best;
        }
    }

    [CreateAssetMenu(fileName = "SubmarineDefinition", menuName = "Hadal/Submarines/Submarine Definition")]
    public class SubmarineDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private SubmarineClass _submarineClass;
        [SerializeField] private float _baseHullStrength = 1000f;
        [SerializeField] private float _basePressureShield = 500f;
        [SerializeField] private int _heroCapacity = 3;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private GameObject _prefab;

        public string Id => _id;
        public string DisplayName => _displayName;
        public SubmarineClass SubmarineClass => _submarineClass;
        public float BaseHullStrength => _baseHullStrength;
        public float BasePressureShield => _basePressureShield;
        public int HeroCapacity => _heroCapacity;
        public float Speed => _speed;
        public GameObject Prefab => _prefab;
    }
}
