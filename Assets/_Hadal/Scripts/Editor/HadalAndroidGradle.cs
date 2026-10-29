#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Adds the Gradle dependencies of HadalGoogleSignIn.java (Assets/Plugins/Android) to the generated
    /// unityLibrary module: Android Credential Manager and the Google ID token option. Resolved from
    /// Google Maven by the build; done here instead of a custom mainTemplate.gradle so Unity upgrades
    /// keep their own template.
    /// </summary>
    public sealed class HadalAndroidGradle : IPostGenerateGradleAndroidProject
    {
        private const string Marker = "// hadal: google sign-in";

        public const string Dependencies = Marker + @"
dependencies {
    implementation 'androidx.credentials:credentials:1.3.0'
    implementation 'androidx.credentials:credentials-play-services-auth:1.3.0'
    implementation 'com.google.android.libraries.identity.googleid:googleid:1.1.1'
}
";

        public int callbackOrder => 11;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var buildGradle = Path.Combine(path, "build.gradle");
            var text = File.ReadAllText(buildGradle);
            if (!text.Contains(Marker))
                File.WriteAllText(buildGradle, text.TrimEnd() + "\n\n" + Dependencies);

            // androidx libraries need the AndroidX flag (Unity's default template already sets it).
            var properties = Path.Combine(Path.GetDirectoryName(path) ?? path, "gradle.properties");
            if (File.Exists(properties))
            {
                var props = File.ReadAllText(properties);
                if (!props.Contains("android.useAndroidX"))
                    File.AppendAllText(properties, "\nandroid.useAndroidX=true\n");
            }
        }
    }
}
#endif
