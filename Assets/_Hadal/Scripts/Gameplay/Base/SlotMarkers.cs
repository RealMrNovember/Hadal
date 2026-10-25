using System.Collections.Generic;
using Hadal.Core.Contracts;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using VContainer;

namespace Hadal.Gameplay.Base
{
    /// <summary>
    /// Glowing rings on every free, unlocked slot, so the player sees where to build. Rebuilt from
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
                    marker.localScale = new Vector3(s, 0.02f, s);
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

        private Transform CreateMarker(PolarGridSlotId id)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = $"SlotMarker_{id}";
            go.transform.SetParent(transform, false);
            go.transform.position = _grid.GetWorldPosition(id) + Vector3.up * 0.03f;
            go.transform.localScale = new Vector3(_radius, 0.02f, _radius);
            var renderer = go.GetComponent<Renderer>();
            if (_material != null)
                renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
