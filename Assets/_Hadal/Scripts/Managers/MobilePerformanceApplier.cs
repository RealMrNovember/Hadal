using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;

namespace Hadal.Managers
{
    public static class MobilePerformanceApplier
    {
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
        }

        private static void OnLowMemory()
        {
            if (DI.GameContext.Current != null && DI.GameContext.Current.TryResolve<IPoolService>(out var pool))
                pool.ClearAll();

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}
