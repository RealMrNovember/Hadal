#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Adds what the in-app updater needs to the generated unityLibrary manifest: the
    /// REQUEST_INSTALL_PACKAGES permission and the ApkProvider (Assets/Plugins/Android).
    /// </summary>
    public sealed class HadalAndroidManifest : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var manifest = File.ReadAllText(manifestPath);
            if (manifest.Contains("com.cicibyte.hadal.ApkProvider"))
                return;

            manifest = manifest.Replace("<application",
                "<uses-permission android:name=\"android.permission.REQUEST_INSTALL_PACKAGES\" />\n  <application");
            manifest = manifest.Replace("</application>",
                "  <provider android:name=\"com.cicibyte.hadal.ApkProvider\" android:authorities=\"${applicationId}.apkprovider\"" +
                " android:exported=\"false\" android:grantUriPermissions=\"true\" />\n  </application>");
            File.WriteAllText(manifestPath, manifest);
        }
    }
}
#endif
