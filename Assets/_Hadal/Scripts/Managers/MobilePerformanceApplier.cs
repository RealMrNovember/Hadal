using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;

namespace Hadal.Managers
{
    public static class MobilePerformanceApplier
    {
        private static IPoolService _poolService;

        public static void Configure(IPoolService poolService) => _poolService = poolService;

        public static void Apply(GameConfigSO config)
        {
            if (config == null)
                return;

            var perf = config.MobilePerformanceConfig;
            if (perf == null)
                return;

            Application.targetFrameRate = perf.TargetFrameRate;
            QualitySettings.vSyncCount = perf.EnableVSync ? 1 : 0;

            if (perf.DisableShadowsOnLowEnd && SystemInfo.systemMemorySize < 4096)
                QualitySettings.shadows = ShadowQuality.Disable;

            Application.lowMemory += OnLowMemory;
        }

        public static void Shutdown()
        {
            Application.lowMemory -= OnLowMemory;
            _poolService = null;
        }

        private static void OnLowMemory()
        {
            _poolService?.ClearAll();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}
