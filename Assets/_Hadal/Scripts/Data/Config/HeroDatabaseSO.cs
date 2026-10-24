using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
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
}
