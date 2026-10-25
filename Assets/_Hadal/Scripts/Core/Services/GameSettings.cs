using System;
using UnityEngine;

namespace Hadal.Core.Services
{
    public enum GraphicsQuality
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Player preferences from the Settings screen (sound, graphics, notifications, vibration),
    /// kept in PlayerPrefs on this device. <see cref="Changed"/> lets live systems re-apply them;
    /// <see cref="ApplyDevice"/> pushes the graphics choices into Unity.
    /// </summary>
    public static class GameSettings
    {
        private const string Prefix = "hadal.settings.";

        public static event Action Changed;

        public static bool MusicOn
        {
            get => GetBool("music", true);
            set => SetBool("music", value);
        }

        /// <summary>0..1.</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(Prefix + "musicVolume", 0.7f);
            set => SetFloat("musicVolume", Mathf.Clamp01(value));
        }

        public static bool SoundOn
        {
            get => GetBool("sound", true);
            set => SetBool("sound", value);
        }

        /// <summary>0..1.</summary>
        public static float SoundVolume
        {
            get => PlayerPrefs.GetFloat(Prefix + "soundVolume", 0.8f);
            set => SetFloat("soundVolume", Mathf.Clamp01(value));
        }

        public static bool Vibration
        {
            get => GetBool("vibration", true);
            set => SetBool("vibration", value);
        }

        public static GraphicsQuality Quality
        {
            get => (GraphicsQuality)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "quality", DefaultQuality()), 0, 2);
            set => SetInt("quality", (int)value);
        }

        /// <summary>30 or 60.</summary>
        public static int FrameRate
        {
            get => PlayerPrefs.GetInt(Prefix + "fps", 60) >= 60 ? 60 : 30;
            set => SetInt("fps", value >= 60 ? 60 : 30);
        }

        /// <summary>Battery saver: 30 fps and low quality whatever the other choices say.</summary>
        public static bool PowerSaving
        {
            get => GetBool("powerSaving", false);
            set => SetBool("powerSaving", value);
        }

        public static bool NotifyConstruction
        {
            get => GetBool("notify.construction", true);
            set => SetBool("notify.construction", value);
        }

        public static bool NotifyStorageFull
        {
            get => GetBool("notify.storage", true);
            set => SetBool("notify.storage", value);
        }

        public static bool NotifyEvents
        {
            get => GetBool("notify.events", true);
            set => SetBool("notify.events", value);
        }

        public static float EffectiveMusicVolume => MusicOn ? MusicVolume : 0f;

        public static float EffectiveSoundVolume => SoundOn ? SoundVolume : 0f;

        /// <summary>Quality level and frame rate; the project's quality list is Very Low .. Ultra.</summary>
        public static void ApplyDevice()
        {
            var quality = PowerSaving ? GraphicsQuality.Low : Quality;
            var levels = QualitySettings.names.Length;
            var level = quality switch
            {
                GraphicsQuality.Low => 1,
                GraphicsQuality.Medium => 2,
                _ => 3
            };
            QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, Mathf.Max(0, levels - 1)), true);
            Application.targetFrameRate = PowerSaving ? 30 : FrameRate;
        }

        // Phones with little memory start on Low; everything else on Medium.
        private static int DefaultQuality() =>
            (int)(SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 4096 ? GraphicsQuality.Low : GraphicsQuality.Medium);

        private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;

        private static void SetBool(string key, bool value) => SetInt(key, value ? 1 : 0);

        private static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            Save();
        }

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            Save();
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
