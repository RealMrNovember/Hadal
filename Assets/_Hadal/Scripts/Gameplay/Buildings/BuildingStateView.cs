using System;
using System.Collections.Generic;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using HADAL.Shared.DTOs;
using UnityEngine;
using VContainer;

namespace Hadal.Gameplay.Buildings
{
    /// <summary>
    /// Renders authoritative buildings from <see cref="IStateSyncService"/> after server ack.
    /// </summary>
    public sealed class BuildingStateView : MonoBehaviour
    {
        [SerializeField] private Transform _buildingsRoot;
        [SerializeField] private Color _solidTint = new(0.72f, 0.78f, 0.86f, 1f);
        [SerializeField] private Vector3 _defaultScale = new(1.2f, 0.6f, 1.2f);

        private IStateSyncService _stateSync;
        private IGridService _grid;
        private ICircularGridService _circularGrid;
        private ILocalEventBus _localBus;
        private readonly Dictionary<string, GameObject> _instances = new();
        private MaterialPropertyBlock _propertyBlock;

        [Inject]
        public void Construct(
            IStateSyncService stateSync,
            IGridService grid,
            ICircularGridService circularGrid,
            ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _grid = grid;
            _circularGrid = circularGrid;
            _localBus = localBus;
        }

        private void OnEnable()
        {
            if (_localBus != null)
                _localBus.Subscribe<BuildingStateChangedEvent>(OnBuildingStateChanged);

            RefreshFromStateView();
        }

        private void OnDisable()
        {
            if (_localBus != null)
                _localBus.Unsubscribe<BuildingStateChangedEvent>(OnBuildingStateChanged);
        }

        private void OnBuildingStateChanged(BuildingStateChangedEvent _)
        {
            RefreshFromStateView();
        }

        public void RefreshFromStateView()
        {
            if (_stateSync == null || _grid == null)
                return;

            var buildings = _stateSync.View.Data.buildings ?? Array.Empty<BuildingSaveEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var building in buildings)
            {
                if (string.IsNullOrEmpty(building.instanceKey))
                    continue;

                seen.Add(building.instanceKey);

                if (!TryCreateSlot(building, out var slot))
                    continue;

                var position = _grid.SlotToWorld(slot);
                var rotation = _grid.SlotToRotation(slot, building.rotation);

                if (!_instances.TryGetValue(building.instanceKey, out var instance) || instance == null)
                {
                    instance = CreateSolidVisual(building.instanceKey);
                    _instances[building.instanceKey] = instance;
                }

                instance.transform.SetPositionAndRotation(position, rotation);
            }

            var staleKeys = new List<string>();
            foreach (var pair in _instances)
            {
                if (!seen.Contains(pair.Key))
                    staleKeys.Add(pair.Key);
            }

            foreach (var key in staleKeys)
            {
                if (_instances.TryGetValue(key, out var instance) && instance != null)
                    Destroy(instance);

                _instances.Remove(key);
            }
        }

        private bool TryCreateSlot(BuildingSaveEntry building, out GridSlot slot)
        {
            slot = default;

            var slotId = new PolarGridSlotId(building.cellRing, building.cellSector);
            if (_circularGrid == null || !_circularGrid.TryGetSlot(slotId, out var polarSlot))
                return false;

            var definition = _grid.Definition;
            var radius = building.cellRing >= 0 && building.cellRing < definition.RingRadii.Length
                ? definition.RingRadii[building.cellRing]
                : 0f;

            slot = GridSlot.Create(radius, polarSlot.SlotAngleDegrees, building.cellRing, building.cellSector);
            return true;
        }

        private GameObject CreateSolidVisual(string instanceKey)
        {
            var root = _buildingsRoot != null ? _buildingsRoot : transform;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Building_{instanceKey}";
            go.transform.SetParent(root, false);
            go.transform.localScale = _defaultScale;
            Destroy(go.GetComponent<Collider>());

            _propertyBlock ??= new MaterialPropertyBlock();
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", _solidTint);
                _propertyBlock.SetColor("_Color", _solidTint);
                renderer.SetPropertyBlock(_propertyBlock);
            }

            return go;
        }
    }
}
