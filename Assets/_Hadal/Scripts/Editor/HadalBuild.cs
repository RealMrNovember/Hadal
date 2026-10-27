#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>Batchmode player builds (server/unity/unity.sh android).</summary>
    public static class HadalBuild
    {
        private const string ApplicationId = "com.cicibyte.hadal";
        private const string OutputDirectory = "artifacts/build";

        /// <summary>
        /// Development APK (ARM64, IL2CPP) signed with Unity's debug key: sideload-only, connects to the
        /// production gateway URL from GameConfig (wss://hadal-api.cicibyte.com/gateway).
        /// </summary>
        [MenuItem("Hadal/Build/Android (Development APK)")]
        public static void BuildAndroidDev()
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // IL2CPP + ARM64: current phones (e.g. Dimensity 8400 / Poco X7 Pro, Pixel 7+) have dropped
            // 32-bit support, Mono cannot target ARM64, and Google Play requires 64-bit anyway.
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            // HADAL_VERSION=1.2.0 stamps the build; the in-app updater compares it with the manifest,
            // and Android only installs over an older versionCode.
            var version = Environment.GetEnvironmentVariable("HADAL_VERSION");
            if (!string.IsNullOrWhiteSpace(version))
            {
                if (!Version.TryParse(version, out var parsed))
                    throw new InvalidOperationException($"HADAL_VERSION '{version}' is not a version like 1.2.0");
                PlayerSettings.bundleVersion = version;
                PlayerSettings.Android.bundleVersionCode = parsed.Major * 10000 + parsed.Minor * 100 + Math.Max(0, parsed.Build);
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No scenes in build settings — run Hadal > Setup > Create Bootstrap Scene.");

            Directory.CreateDirectory(OutputDirectory);
            var output = Path.Combine(OutputDirectory, "hadal-dev.apk");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                // Player builds carry no "Development Build" stamp and run faster; HADAL_DEV=1 brings the
                // profiler/debug build back for investigation.
                options = Environment.GetEnvironmentVariable("HADAL_DEV") == "1" ? BuildOptions.Development : BuildOptions.None
            });

            var summary = report.summary;
            Debug.Log($"[Hadal] Android build {summary.result}: {output} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime:mm\\:ss})");

            // Non-zero exit for unity.sh when the build failed.
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Android build {summary.result}");
        }
    }
}
#endif
