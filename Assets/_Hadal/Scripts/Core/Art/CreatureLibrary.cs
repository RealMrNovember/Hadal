using System;
using UnityEngine;

namespace Hadal.Core.Art
{
    /// <summary>
    /// The world-map creatures (Art/Creatures): model, its <c>Hadal/Creature</c> material and its mesh bounds, by id.
    /// A Resources asset (Resources/Creatures/CreatureLibrary) written by Hadal > Setup > Creatures
    /// (HadalCreatureSetup), so the map needs no scene wiring to show them.
    /// </summary>
    public sealed class CreatureLibrary : ScriptableObject
    {
        public const string ResourcePath = "Creatures/CreatureLibrary";

        [Serializable]
        public struct Entry
        {
            public string id;
            public GameObject model;
            public Material material;

            /// <summary>Mesh bounds in model space (metres): the profile's shares and the map scale are taken from them.</summary>
            public Vector3 boundsMin;
            public Vector3 boundsSize;
            public int triangles;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private Material _lure;

        private static CreatureLibrary _instance;
        private static bool _looked;

        public static CreatureLibrary Instance
        {
            get
            {
                if (_instance == null && !_looked)
                {
                    _looked = true;
                    _instance = Resources.Load<CreatureLibrary>(ResourcePath);
                }

                return _instance;
            }
        }

        public Entry[] Entries => _entries;

        /// <summary>The glowing bulb material for angler lures.</summary>
        public Material Lure => _lure;

        public void Configure(Entry[] entries, Material lure)
        {
            _entries = entries ?? Array.Empty<Entry>();
            _lure = lure;
        }

        public bool TryGet(string id, out Entry entry)
        {
            foreach (var e in _entries)
            {
                if (e.id == id && e.model != null)
                {
                    entry = e;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// A ready creature: the model under a root that stands on the given spot, scaled to its map height, with its
        /// material and a <see cref="CreatureMotion"/>. Null when the art is missing (the caller keeps its fallback).
        /// </summary>
        public CreatureMotion Spawn(string id, Transform parent, float heightScale = 1f)
        {
            var profile = CreatureProfiles.Get(id);
            if (profile == null || !TryGet(id, out var entry))
                return null;

            var root = new GameObject(id).transform;
            root.SetParent(parent, false);
            var body = Instantiate(entry.model, root);
            body.name = "Body";
            var scale = profile.MapHeight * heightScale / Mathf.Max(0.01f, entry.boundsSize.y);
            body.transform.localScale = Vector3.one * scale;
            // Swimmers float at their centre; walkers stand on the floor (props.py pivots).
            body.transform.localPosition = Vector3.zero;
            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                if (entry.material != null)
                    r.sharedMaterial = entry.material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var motion = root.gameObject.AddComponent<CreatureMotion>();
            motion.Configure(profile, body.transform, scale, entry.boundsMin, entry.boundsSize, _lure);
            return motion;
        }
    }
}
