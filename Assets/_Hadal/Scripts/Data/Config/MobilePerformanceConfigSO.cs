using UnityEngine;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "MobilePerformanceConfig", menuName = "Hadal/Config/Mobile Performance Config")]
    public class MobilePerformanceConfigSO : ScriptableObject
    {
        [Header("Frame Rate")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private int _backgroundFrameRate = 30;
        [SerializeField] private bool _enableVSync;

        [Header("Memory")]
        [SerializeField] private int _defaultPoolCapacity = 16;
        [SerializeField] private int _maxPoolSize = 128;
        [SerializeField] private int _maxCachedAddressables = 64;

        [Header("Battery")]
        [SerializeField] private bool _reduceUpdatesOnLowPower = true;
        [SerializeField] private float _lowPowerTickMultiplier = 0.5f;

        [Header("Rendering")]
        [SerializeField] private bool _disableShadowsOnLowEnd;
        [SerializeField] private int _maxParticleBudget = 256;

        public int TargetFrameRate => _targetFrameRate;
        public int BackgroundFrameRate => _backgroundFrameRate;
        public bool EnableVSync => _enableVSync;
        public int DefaultPoolCapacity => _defaultPoolCapacity;
        public int MaxPoolSize => _maxPoolSize;
        public int MaxCachedAddressables => _maxCachedAddressables;
        public bool ReduceUpdatesOnLowPower => _reduceUpdatesOnLowPower;
        public float LowPowerTickMultiplier => _lowPowerTickMultiplier;
        public bool DisableShadowsOnLowEnd => _disableShadowsOnLowEnd;
        public int MaxParticleBudget => _maxParticleBudget;
    }
}
