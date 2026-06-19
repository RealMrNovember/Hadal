using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "HeroDefinition", menuName = "Hadal/Heroes/Hero Definition")]
    public class HeroDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private HeroClass _heroClass;
        [SerializeField] private FactionType _faction;
        [SerializeField] private HeroRarity _rarity;
        [SerializeField] private string _lore;
        [SerializeField] private string _skillName;
        [SerializeField] private string _ultimateName;
        [SerializeField] private float _baseHealth = 100f;
        [SerializeField] private float _baseAttack = 10f;
        [SerializeField] private float _baseDefense = 5f;
        [SerializeField] private float _pressureTolerance = 5000f;
        [SerializeField] private GameObject _prefab;

        public string Id => _id;
        public string DisplayName => _displayName;
        public HeroClass HeroClass => _heroClass;
        public FactionType Faction => _faction;
        public HeroRarity Rarity => _rarity;
        public string Lore => _lore;
        public string SkillName => _skillName;
        public string UltimateName => _ultimateName;
        public float BaseHealth => _baseHealth;
        public float BaseAttack => _baseAttack;
        public float BaseDefense => _baseDefense;
        public float PressureTolerance => _pressureTolerance;
        public GameObject Prefab => _prefab;
    }

    [CreateAssetMenu(fileName = "HeroDatabase", menuName = "Hadal/Heroes/Hero Database")]
    public class HeroDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<HeroDefinitionSO> _heroes = new();

        public IReadOnlyList<HeroDefinitionSO> Heroes => _heroes;

        public HeroDefinitionSO GetById(string id)
        {
            foreach (var h in _heroes)
            {
                if (h != null && h.Id == id)
                    return h;
            }
            return null;
        }
    }

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
