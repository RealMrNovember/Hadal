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

                if (!await DownloadAsync(url, path, progress))
                    return UpdateOutcome.Failed;

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

        private const int Attempts = 6;
        private const float StallSeconds = 20f;

        /// <summary>
        /// Downloads with resume: a connection that stops delivering bytes for
        /// <see cref="StallSeconds"/> is dropped and the next attempt continues from the bytes
        /// already on disk (HTTP Range), so a flaky mobile link still finishes.
        /// </summary>
        private static async Task<bool> DownloadAsync(string url, string path, IProgress<float> progress)
        {
            long total = await ContentLengthAsync(url);
            for (var attempt = 1; attempt <= Attempts; attempt++)
            {
                var offset = File.Exists(path) ? new FileInfo(path).Length : 0L;
                if (total > 0 && offset == total)
                    return true;
                if (total > 0 && offset > total)
                {
                    File.Delete(path);
                    offset = 0;
                }

                using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
                {
                    downloadHandler = new DownloadHandlerFile(path, offset > 0) { removeFileOnAbort = false }
                };
                if (offset > 0)
                    request.SetRequestHeader("Range", $"bytes={offset}-");

                var operation = request.SendWebRequest();
                var lastBytes = 0UL;
                var lastChange = Time.realtimeSinceStartup;
                while (!operation.isDone)
                {
                    if (request.downloadedBytes != lastBytes)
                    {
                        lastBytes = request.downloadedBytes;
                        lastChange = Time.realtimeSinceStartup;
                    }
                    else if (Time.realtimeSinceStartup - lastChange > StallSeconds)
                    {
                        Debug.LogWarning($"[AppUpdater] Download stalled at {offset + (long)lastBytes} bytes; resuming (attempt {attempt}).");
                        request.Abort();
                        break;
                    }

                    if (total > 0)
                        progress?.Report(Mathf.Clamp01((offset + (long)request.downloadedBytes) / (float)total));
                    await Task.Yield();
                }

                // A server that ignores Range answers 200 with the whole file: start over.
                if (offset > 0 && request.responseCode == 200)
                {
                    File.Delete(path);
                    continue;
                }

                if (request.result == UnityWebRequest.Result.Success && (total <= 0 || new FileInfo(path).Length == total))
                    return true;

                Debug.LogWarning($"[AppUpdater] Attempt {attempt} ended: {request.error ?? "incomplete"}");
            }

            return false;
        }

        private static async Task<long> ContentLengthAsync(string url)
        {
            using var head = UnityWebRequest.Head(url);
            head.timeout = 15;
            var operation = head.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();
            return long.TryParse(head.GetResponseHeader("Content-Length"), out var length) ? length : 0L;
        }
    }
}
