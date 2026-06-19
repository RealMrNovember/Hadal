using UnityEngine;
using Hadal.Core.DI;
using Hadal.Managers;

namespace Hadal.Gameplay.Expeditions
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        [SerializeField] private Light _mainLight;
        [SerializeField] private bool _sonarOnlyMode;
        [SerializeField] private float _creaturePassDistance = 50f;

        private AudioManager _audioManager;

        private void Start()
        {
            if (GameContext.Current != null)
                GameContext.Current.TryResolve(out _audioManager);

            ApplySonarMode(_sonarOnlyMode);
        }

        public void ApplySonarMode(bool enabled)
        {
            _sonarOnlyMode = enabled;
            if (_mainLight != null)
                _mainLight.enabled = !enabled;

            if (enabled)
                _audioManager?.PlaySonarAmbience();
        }

        public void TriggerCreaturePass(Transform creature)
        {
            if (creature == null)
                return;

            creature.position = transform.position + transform.forward * _creaturePassDistance;
        }
    }
}
