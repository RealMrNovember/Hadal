using UnityEditor;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Import settings for the synthesized audio (art/audio → Resources/Audio): music loops stream from
    /// Vorbis so they never sit decoded in memory; effects are short mono clips (levels set in
    /// art/audio/sfx.py) decoded on load so they start without delay.
    /// </summary>
    public sealed class HadalAudioImporter : AssetPostprocessor
    {
        private const string Music = "Assets/_Hadal/Resources/Audio/Music/";
        private const string Sfx = "Assets/_Hadal/Resources/Audio/Sfx/";

        private void OnPreprocessAudio()
        {
            var music = assetPath.StartsWith(Music);
            if (!music && !assetPath.StartsWith(Sfx))
                return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            if (music)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.45f;
                settings.preloadAudioData = false;
                importer.forceToMono = false;
                importer.loadInBackground = true;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.6f;
                settings.preloadAudioData = true;
                // Already mono; forcing it would also turn on Unity's normalize and undo the mixed levels.
                importer.forceToMono = false;
                importer.loadInBackground = false;
            }

            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
        }
    }
}
