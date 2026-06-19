using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class CombatManager : ManagerBase
    {
        private EnemyDatabaseSO _database;
        private readonly List<CombatEncounter> _activeEncounters = new();

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.EnemyDatabase;
        }

        public CombatEncounter StartEncounter(EnemyDefinitionSO enemy, Vector3 position)
        {
            if (enemy == null)
                return default;

            var encounter = new CombatEncounter
            {
                EnemyId = enemy.Id,
                RemainingHealth = enemy.Health,
                Position = position,
                IsActive = true
            };

            _activeEncounters.Add(encounter);
            return encounter;
        }

        public void ApplyDamage(ref CombatEncounter encounter, float damage)
        {
            encounter.RemainingHealth -= damage;
            if (encounter.RemainingHealth <= 0f)
                encounter.IsActive = false;
        }

        public EnemyDefinitionSO GetEnemy(string id) => _database?.GetById(id);

        protected override void OnShutdown() => _activeEncounters.Clear();
    }

    public struct CombatEncounter
    {
        public string EnemyId;
        public float RemainingHealth;
        public Vector3 Position;
        public bool IsActive;
    }
}
