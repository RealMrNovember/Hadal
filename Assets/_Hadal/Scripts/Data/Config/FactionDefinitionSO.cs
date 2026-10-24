using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "FactionDefinition", menuName = "Hadal/Heroes/Faction Definition")]
    public class FactionDefinitionSO : ScriptableObject
    {
        [SerializeField] private FactionType _type;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Color _themeColor = Color.blue;

        public FactionType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Color ThemeColor => _themeColor;
    }
}
