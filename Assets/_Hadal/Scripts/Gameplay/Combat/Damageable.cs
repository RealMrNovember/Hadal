using UnityEngine;
using Hadal.Core.DI;
using Hadal.Data.Config;
using Hadal.Managers;

namespace Hadal.Gameplay.Combat
{
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO _enemyDefinition;
        [SerializeField] private float _health;

        private CombatManager _combatManager;
        private CombatEncounter _encounter;

        private void Start()
        {
            if (GameContext.Current != null)
                GameContext.Current.TryResolve(out _combatManager);

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
