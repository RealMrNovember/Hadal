using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
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
}
