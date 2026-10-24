using System.Collections.Generic;
using UnityEngine;
using HADAL.Shared.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "ResourceDatabase", menuName = "Hadal/Resources/Resource Database")]
    public class ResourceDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<ResourceDefinitionSO> _resources = new();

        public IReadOnlyList<ResourceDefinitionSO> Resources => _resources;

        public ResourceDefinitionSO Get(ResourceType type)
        {
            foreach (var r in _resources)
            {
                if (r != null && r.Type == type)
                    return r;
            }
            return null;
        }
    }
}
