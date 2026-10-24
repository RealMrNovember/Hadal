using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Hadal/Combat/Enemy Database")]
    public class EnemyDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinitionSO> _enemies = new();

        public IReadOnlyList<EnemyDefinitionSO> Enemies => _enemies;

        public EnemyDefinitionSO GetById(string id)
        {
            foreach (var e in _enemies)
            {
                if (e != null && e.Id == id)
                    return e;
            }
            return null;
        }
    }
}
