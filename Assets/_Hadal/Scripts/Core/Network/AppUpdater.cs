using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Hadal.Core.Network
{
    public enum UpdateOutcome
    {
        /// <summary>The system installer is open; it replaces the app when the player confirms.</summary>
        InstallerOpened,

        /// <summary>Android asked the player to allow installs from this app; tap Update again afterwards.</summary>
        NeedsPermission,

        /// <summary>Not Android (editor, iOS): the download page was opened in the browser.</summary>
        OpenedInBrowser,

        Failed
    }

    /// <summary>
    /// Over-the-air app update for sideloaded Android builds: downloads the APK named by the
    /// manifest into the app cache and hands it to the system installer (Plugins/Android
    /// HadalUpdater + ApkProvider). Store builds will open their store page instead.
    /// </summary>
    public static class AppUpdater
    {
        public const string FileName = "hadal-update.apk";

        public static async Task<UpdateOutcome> UpdateAsync(string url, IProgress<float> progress)
        {
            if (string.IsNullOrEmpty(url))
                return UpdateOutcome.Failed;

#if UNITY_ANDROID && !UNITY_EDITOR
            var path = Path.Combine(Application.temporaryCachePath, FileName);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
                {
                    downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true }
                };
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    progress?.Report(request.downloadProgress);
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[AppUpdater] Download failed: {request.error}");
                    return UpdateOutcome.Failed;
                }

                progress?.Report(1f);
                using var updater = new AndroidJavaClass("com.cicibyte.hadal.HadalUpdater");
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                return updater.CallStatic<bool>("install", activity, path) ? UpdateOutcome.InstallerOpened : UpdateOutcome.NeedsPermission;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AppUpdater] {exception.Message} — falling back to the browser.");
                Application.OpenURL(url);
                return UpdateOutcome.OpenedInBrowser;
            }
#else
            await Task.CompletedTask;
            Application.OpenURL(url);
            return UpdateOutcome.OpenedInBrowser;
#endif
        }
    }
}
