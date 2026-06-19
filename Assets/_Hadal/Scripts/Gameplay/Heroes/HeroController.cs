using UnityEngine;
using Hadal.Data.Config;

namespace Hadal.Gameplay.Heroes
{
    public class HeroController : MonoBehaviour
    {
        [SerializeField] private HeroDefinitionSO _definition;
        [SerializeField] private float _currentHealth;

        public HeroDefinitionSO Definition => _definition;
        public float CurrentHealth => _currentHealth;

        public void Bind(HeroDefinitionSO definition)
        {
            _definition = definition;
            _currentHealth = definition.BaseHealth;
        }

        public void TakeDamage(float amount)
        {
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        }

        public void Heal(float amount)
        {
            if (_definition == null)
                return;

            _currentHealth = Mathf.Min(_definition.BaseHealth, _currentHealth + amount);
        }
    }
}
