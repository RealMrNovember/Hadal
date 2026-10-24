using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Config
{
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
}
