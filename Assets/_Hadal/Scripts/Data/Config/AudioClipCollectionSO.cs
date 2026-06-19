using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "AudioClipCollection", menuName = "Hadal/Audio/Audio Clip Collection")]
    public class AudioClipCollectionSO : ScriptableObject
    {
        [SerializeField] private List<AudioEntry> _entries = new();

        public IReadOnlyList<AudioEntry> Entries => _entries;

        public AudioClip GetClip(string key)
        {
            foreach (var entry in _entries)
            {
                if (entry.Key == key)
                    return entry.Clip;
            }
            return null;
        }
    }

    [System.Serializable]
    public struct AudioEntry
    {
        public string Key;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume;
    }
}
