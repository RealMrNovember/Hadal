using System.Collections.Generic;
using Hadal.Core.Contracts;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using VContainer;

namespace Hadal.Gameplay.Base
{
    /// <summary>
    /// Thin glowing rings on every free, unlocked slot, so the player sees where to build (a filled disc read as a
    /// glitch on the phone). Rebuilt from
    /// replicated state twice a second; no colliders (taps are resolved by <see cref="BaseInteraction"/>).
    /// </summary>
    public sealed class SlotMarkers : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private float _radius = 1.6f;

        private readonly Dictionary<PolarGridSlotId, Transform> _markers = new();
        private readonly HashSet<PolarGridSlotId> _occupied = new();
        private ICircularGridService _grid;
        private IStateSyncService _stateSync;
        private float _nextRefresh;

        [Inject]
        public void Construct(ICircularGridService grid, IStateSyncService stateSync)
        {
            _grid = grid;
            _stateSync = stateSync;
        }

        public void Configure(Material material) => _material = material;

        private void Update()
        {
            if (_grid == null)
                return;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.5f;
                Refresh();
            }

            // Breathing glow: scale the visible markers together.
            var s = _radius * (1f + 0.06f * Mathf.Sin(Time.time * 2.4f));
            foreach (var marker in _markers.Values)
            {
                if (marker.gameObject.activeSelf)
                    marker.localScale = new Vector3(s, 1f, s);
            }
        }

        private void Refresh()
        {
            if (_grid.Config == null)
                return; // grid model not built yet (VContainer Start runs after the first frames)

            _occupied.Clear();
            foreach (var building in _stateSync.View.Data.buildings ?? System.Array.Empty<BuildingSaveEntry>())
                _occupied.Add(new PolarGridSlotId(building.cellRing, building.cellSector));

            foreach (var slot in _grid.GetAllSlots())
            {
                // Ring 0 is the reactor; locked rings get no markers.
                if (slot.Id.Ring == 0 || !slot.IsUnlocked)
                    continue;

                if (!_markers.TryGetValue(slot.Id, out var marker))
                {
                    marker = CreateMarker(slot.Id);
                    _markers[slot.Id] = marker;
                }

                marker.gameObject.SetActive(!_occupied.Contains(slot.Id));
            }
        }

        private static Mesh _ringMesh;

        /// <summary>A flat annulus of radius 0.5 (like the unit cylinder it replaces), 10% wide, facing up.</summary>
        private static Mesh RingMesh
        {
            get
            {
                if (_ringMesh != null)
                    return _ringMesh;
                const int segments = 56;
                const float outer = 0.5f, inner = 0.45f;
                var vertices = new Vector3[segments * 2];
                var triangles = new int[segments * 6];
                for (var i = 0; i < segments; i++)
                {
                    var a = i * Mathf.PI * 2f / segments;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    vertices[i * 2] = dir * outer;
                    vertices[i * 2 + 1] = dir * inner;
                    var n = (i + 1) % segments;
                    var t = i * 6;
                    triangles[t] = i * 2;
                    triangles[t + 1] = i * 2 + 1;
                    triangles[t + 2] = n * 2;
                    triangles[t + 3] = n * 2;
                    triangles[t + 4] = i * 2 + 1;
                    triangles[t + 5] = n * 2 + 1;
                }

                _ringMesh = new Mesh { name = "SlotRing", vertices = vertices, triangles = triangles };
                _ringMesh.RecalculateNormals();
                _ringMesh.RecalculateBounds();
                return _ringMesh;
            }
        }

        private Transform CreateMarker(PolarGridSlotId id)
        {
            var go = new GameObject($"SlotMarker_{id}", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = RingMesh;
            go.transform.SetParent(transform, false);
            go.transform.position = _grid.GetWorldPosition(id) + Vector3.up * 0.03f;
            go.transform.localScale = new Vector3(_radius, 1f, _radius);
            var renderer = go.GetComponent<Renderer>();
            if (_material != null)
                renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
