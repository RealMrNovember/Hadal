using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "ResourceDefinition", menuName = "Hadal/Resources/Resource Definition")]
    public class ResourceDefinitionSO : ScriptableObject
    {
        [SerializeField] private ResourceType _type;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _uiColor = Color.cyan;
        [SerializeField] private long _startingAmount;
        [SerializeField] private long _maxCapacity = long.MaxValue;
        [SerializeField] private bool _isCritical;

        public ResourceType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color UiColor => _uiColor;
        public long StartingAmount => _startingAmount;
        public long MaxCapacity => _maxCapacity;
        public bool IsCritical => _isCritical;
    }

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
