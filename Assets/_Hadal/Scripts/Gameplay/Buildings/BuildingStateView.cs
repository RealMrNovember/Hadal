using System;
using System.Collections.Generic;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using Hadal.Data.Config;
using Hadal.Data.Models;
using HADAL.Shared.DTOs;
using UnityEngine;
using VContainer;

namespace Hadal.Gameplay.Buildings
{
    /// <summary>
    /// Renders authoritative buildings from <see cref="IStateSyncService"/> after server ack.
    /// Uses the definition's prefab (generated art) and falls back to a tinted cube.
    /// </summary>
    public sealed class BuildingStateView : MonoBehaviour
    {
        [SerializeField] private Transform _buildingsRoot;
        [SerializeField] private Color _solidTint = new(0.72f, 0.78f, 0.86f, 1f);
        [SerializeField] private Vector3 _defaultScale = new(1.2f, 0.6f, 1.2f);
        [Tooltip("Hologram look of a building during its first construction (HAD_Glass).")]
        [SerializeField] private Material _constructionMaterial;

        private IStateSyncService _stateSync;
        private IGridService _grid;
        private ICircularGridService _circularGrid;
        private ILocalEventBus _localBus;
        private BuildingDatabaseSO _buildingDatabase;
        private bool _subscribed;
        private readonly Dictionary<string, Instance> _instances = new();
        private MaterialPropertyBlock _propertyBlock;

        [Inject]
        public void Construct(
            IStateSyncService stateSync,
            IGridService grid,
            ICircularGridService circularGrid,
            ILocalEventBus localBus,
            GameConfigSO config)
        {
            _stateSync = stateSync;
            _grid = grid;
            _circularGrid = circularGrid;
            _localBus = localBus;
            _buildingDatabase = config != null ? config.BuildingDatabase : null;

            // Scene objects are enabled before VContainer injects them, so OnEnable alone would
            // never subscribe.
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            if (_localBus != null && _subscribed)
                _localBus.Unsubscribe<BuildingStateChangedEvent>(OnBuildingStateChanged);

            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_localBus == null || _subscribed)
                return;

            _localBus.Subscribe<BuildingStateChangedEvent>(OnBuildingStateChanged);
            _subscribed = true;
            RefreshFromStateView();
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

                // A first construction (level 0) shows as a rising hologram; it is swapped for the real
                // model when the server completes it.
                var hologram = building.level == 0 && building.IsUnderConstruction;
                if (!_instances.TryGetValue(building.instanceKey, out var instance) || instance.Go == null
                    || instance.DefinitionId != building.definitionId || instance.Hologram != hologram)
                {
                    if (instance.Go != null)
                        Destroy(instance.Go);

                    var go = CreateVisual(building);
                    if (hologram)
                        MakeHologram(go);
                    instance = new Instance(go, building.definitionId, hologram, building.constructionEndsAt, building.constructionSeconds);
                    _instances[building.instanceKey] = instance;
                }
                else if (hologram)
                {
                    instance = new Instance(instance.Go, instance.DefinitionId, true, building.constructionEndsAt, building.constructionSeconds);
                    _instances[building.instanceKey] = instance;
                }

                instance.Go.transform.SetPositionAndRotation(position, rotation);
            }

            var staleKeys = new List<string>();
            foreach (var pair in _instances)
            {
                if (!seen.Contains(pair.Key))
                    staleKeys.Add(pair.Key);
            }

            foreach (var key in staleKeys)
            {
                if (_instances.TryGetValue(key, out var instance) && instance.Go != null)
                    Destroy(instance.Go);

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

        public int InstanceCount => _instances.Count;

        private void Update()
        {
            foreach (var instance in _instances.Values)
            {
                if (!instance.Hologram || instance.Go == null)
                    continue;

                // Rises out of the seabed as the construction progresses.
                var progress = instance.Seconds > 0f
                    ? Mathf.Clamp01(1f - (instance.EndsAt - Time.realtimeSinceStartup) / instance.Seconds)
                    : 1f;
                var scale = instance.Go.transform.localScale;
                instance.Go.transform.localScale = new Vector3(scale.x, Mathf.Lerp(0.15f, 1f, progress), scale.z);
            }
        }

        private void MakeHologram(GameObject go)
        {
            if (_constructionMaterial == null)
                return;

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = _constructionMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>World positions of the rendered buildings (crew and effects use them).</summary>
        public void GetBuildingPositions(List<Vector3> into)
        {
            into.Clear();
            foreach (var instance in _instances.Values)
            {
                if (instance.Go != null)
                    into.Add(instance.Go.transform.position);
            }
        }

        private GameObject CreateVisual(BuildingSaveEntry building)
        {
            var root = _buildingsRoot != null ? _buildingsRoot : transform;
            var prefab = _buildingDatabase != null ? _buildingDatabase.GetById(building.definitionId)?.Prefab : null;
            if (prefab != null)
            {
                var go = Instantiate(prefab, root, false);
                go.name = $"Building_{building.instanceKey}";
                return go;
            }

            return CreateSolidVisual(building.instanceKey, root);
        }

        private GameObject CreateSolidVisual(string instanceKey, Transform root)
        {
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

        private readonly struct Instance
        {
            public readonly GameObject Go;
            public readonly string DefinitionId;
            public readonly bool Hologram;
            public readonly float EndsAt;
            public readonly float Seconds;

            public Instance(GameObject go, string definitionId, bool hologram, float endsAt, float seconds)
            {
                Go = go;
                DefinitionId = definitionId;
                Hologram = hologram;
                EndsAt = endsAt;
                Seconds = seconds;
            }
        }
    }
}
