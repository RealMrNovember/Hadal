#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Copies the notification small icon (art/logo/make_notify_icon.py: a white HADAL mark on transparent, one PNG per
    /// density) into the generated unityLibrary's res/drawable-*, where Android finds it as "hadal_notify" (docs/36).
    /// The source folder ends in "~" so Unity never imports it as a texture.
    /// </summary>
    public sealed class HadalNotificationIcons : IPostGenerateGradleAndroidProject
    {
        private const string Source = "Assets/_Hadal/Art/Branding/Notify~";

        public int callbackOrder => 12;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var source = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", Source);
            if (!Directory.Exists(source))
            {
                Debug.LogWarning("[HadalBuild] No notification icon at " + Source + "; notifications use the app icon.");
                return;
            }

            foreach (var folder in Directory.GetDirectories(source, "drawable-*"))
            {
                var target = Path.Combine(path, "src", "main", "res", Path.GetFileName(folder));
                Directory.CreateDirectory(target);
                foreach (var file in Directory.GetFiles(folder, "*.png"))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }
        }
    }
}
#endif
