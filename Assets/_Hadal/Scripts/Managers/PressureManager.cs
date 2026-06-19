using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Events;
using Hadal.Data.Models;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class PressureManager : ManagerBase, IPressureService
    {
        [SerializeField] private PressureChangedEventSO _pressureChangedEvent;

        private PressureDatabaseSO _database;
        private float _hullStrength = 1000f;
        private float _pressureShield = 500f;
        private float _nanoAlloyBonus;

        public float HullStrength => _hullStrength;
        public float PressureShield => _pressureShield;

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.PressureDatabase;
        }

        public PressureSnapshot EvaluateDepth(float depthMeters)
        {
            var tierDef = _database?.GetTierForDepth(depthMeters);
            var tier = tierDef != null ? tierDef.Tier : PressureTier.Tier1000m;
            var pressure = tierDef != null ? tierDef.PressureValue : depthMeters * 0.1f;
            var effectiveHull = _hullStrength + _nanoAlloyBonus;

            var snapshot = new PressureSnapshot
            {
                DepthMeters = depthMeters,
                Tier = tier,
                CurrentPressure = pressure,
                HullStrength = effectiveHull,
                PressureShield = _pressureShield,
                IsSurvivable = effectiveHull >= (tierDef?.RequiredHullStrength ?? 0f)
                               && _pressureShield >= (tierDef?.RequiredPressureShield ?? 0f)
            };

            _pressureChangedEvent?.Raise(snapshot);
            return snapshot;
        }

        public void UpgradeHull(float amount) => _hullStrength += amount;
        public void UpgradeShield(float amount) => _pressureShield += amount;
        public void ApplyNanoAlloyBonus(float bonus) => _nanoAlloyBonus += bonus;
    }
}
