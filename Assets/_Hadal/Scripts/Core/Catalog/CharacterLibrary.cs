using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.Catalog
{
    /// <summary>
    /// Scene-held references to the character prefabs (Art/Characters) and painted portraits
    /// (Art/Portraits), so UI code can show heroes without the art living under Resources.
    /// Filled by Hadal > Setup (HadalWorldSetup); looked up by catalog id.
    /// </summary>
    public sealed class CharacterLibrary : MonoBehaviour
    {
        [Serializable]
        public struct ModelEntry
        {
            public string id;
            public GameObject prefab;
        }

        [Serializable]
        public struct PortraitEntry
        {
            public string id;
            public Texture2D texture;
        }

        [SerializeField] private ModelEntry[] _models = Array.Empty<ModelEntry>();
        [SerializeField] private PortraitEntry[] _portraits = Array.Empty<PortraitEntry>();

        private static CharacterLibrary _instance;
        private readonly Dictionary<string, GameObject> _modelById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> _portraitById = new(StringComparer.Ordinal);

        public static CharacterLibrary Instance => _instance != null ? _instance : _instance = FindFirstObjectByType<CharacterLibrary>();

        public void Configure(ModelEntry[] models, PortraitEntry[] portraits)
        {
            _models = models ?? Array.Empty<ModelEntry>();
            _portraits = portraits ?? Array.Empty<PortraitEntry>();
            _modelById.Clear();
            _portraitById.Clear();
        }

        public GameObject Model(string id)
        {
            Index();
            return !string.IsNullOrEmpty(id) && _modelById.TryGetValue(id, out var prefab) ? prefab : null;
        }

        public Texture2D Portrait(string id)
        {
            Index();
            return !string.IsNullOrEmpty(id) && _portraitById.TryGetValue(id, out var texture) ? texture : null;
        }

        private void Index()
        {
            if (_modelById.Count > 0 || _portraitById.Count > 0)
                return;
            foreach (var m in _models)
            {
                if (m.prefab != null && !string.IsNullOrEmpty(m.id))
                    _modelById[m.id] = m.prefab;
            }

            foreach (var p in _portraits)
            {
                if (p.texture != null && !string.IsNullOrEmpty(p.id))
                    _portraitById[p.id] = p.texture;
            }
        }
    }
}
