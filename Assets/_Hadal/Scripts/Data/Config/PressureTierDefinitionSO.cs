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
}
