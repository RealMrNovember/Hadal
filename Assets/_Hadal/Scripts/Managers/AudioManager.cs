using UnityEngine;
using Hadal.Core.Services;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class AudioManager : ManagerBase
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClipCollectionSO _ambientCollection;
        [SerializeField] private AudioClipCollectionSO _uiCollection;

        protected override void OnInitialize(GameConfigSO config)
        {
            EnsureSources();
            ApplyVolumes();
            GameSettings.Changed += ApplyVolumes;
            PlaySonarAmbience();
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = GameSettings.EffectiveMusicVolume;
            if (_sfxSource != null)
                _sfxSource.volume = GameSettings.EffectiveSoundVolume;
        }

        private void EnsureSources()
        {
            if (_musicSource == null)
            {
                var musicGo = new GameObject("MusicSource");
                musicGo.transform.SetParent(transform);
                _musicSource = musicGo.AddComponent<AudioSource>();
                _musicSource.loop = true;
            }

            if (_sfxSource == null)
            {
                var sfxGo = new GameObject("SFXSource");
                sfxGo.transform.SetParent(transform);
                _sfxSource = sfxGo.AddComponent<AudioSource>();
            }
        }

        public void PlaySonarAmbience()
        {
            var clip = _ambientCollection?.GetClip("sonar_ping");
            if (clip != null && _musicSource != null)
            {
                _musicSource.clip = clip;
                _musicSource.Play();
            }
        }

        public void PlaySfx(string key)
        {
            var clip = _uiCollection?.GetClip(key);
            if (clip != null && _sfxSource != null)
                _sfxSource.PlayOneShot(clip);
        }

        protected override void OnShutdown()
        {
            GameSettings.Changed -= ApplyVolumes;
            if (_musicSource != null)
                _musicSource.Stop();
        }
    }
}
