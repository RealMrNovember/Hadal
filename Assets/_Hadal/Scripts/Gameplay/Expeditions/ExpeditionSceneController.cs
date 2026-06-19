using UnityEngine;
using Hadal.Managers;
using VContainer;

namespace Hadal.Gameplay.Expeditions
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        private AudioManager _audioManager;

        [Inject]
        public void Construct(AudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public void PlayExpeditionAmbience()
        {
            _audioManager?.PlaySonarAmbience();
        }
    }
}
