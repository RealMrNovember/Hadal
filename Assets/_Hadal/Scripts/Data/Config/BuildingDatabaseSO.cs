using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Hadal/Buildings/Building Database")]
    public class BuildingDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<BuildingDefinitionSO> _buildings = new();

        public IReadOnlyList<BuildingDefinitionSO> Buildings => _buildings;

        public BuildingDefinitionSO GetById(string id)
        {
            foreach (var b in _buildings)
            {
                if (b != null && b.Id == id)
                    return b;
            }

            return null;
        }
    }
}
