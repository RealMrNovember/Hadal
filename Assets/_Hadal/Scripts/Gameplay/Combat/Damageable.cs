using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Managers;
using VContainer;

namespace Hadal.Gameplay.Combat
{
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO _enemyDefinition;
        [SerializeField] private float _health;

        private CombatManager _combatManager;
        private CombatEncounter _encounter;

        [Inject]
        public void Construct(CombatManager combatManager)
        {
            _combatManager = combatManager;

            if (_enemyDefinition != null)
            {
                _health = _enemyDefinition.Health;
                _encounter = _combatManager?.StartEncounter(_enemyDefinition, transform.position) ?? default;
            }
        }

        public void ApplyDamage(float damage)
        {
            _health -= damage;
            if (_combatManager != null)
                _combatManager.ApplyDamage(ref _encounter, damage);

            if (_health <= 0f)
                Destroy(gameObject);
        }
    }
}
