using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
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
