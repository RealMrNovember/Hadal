using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Hadal/Combat/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private EnemyType _type;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isBoss;
        [SerializeField] private float _health = 100f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _speed = 3f;
        [SerializeField] private DepthZone _minZone;
        [SerializeField] private GameObject _prefab;

        public string Id => _id;
        public EnemyType Type => _type;
        public string DisplayName => _displayName;
        public bool IsBoss => _isBoss;
        public float Health => _health;
        public float Damage => _damage;
        public float Speed => _speed;
        public DepthZone MinZone => _minZone;
        public GameObject Prefab => _prefab;
    }

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
