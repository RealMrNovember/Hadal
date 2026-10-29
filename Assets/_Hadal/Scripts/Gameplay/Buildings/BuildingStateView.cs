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
            UpdateEmergencyFx();
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

        // --- Emergency effect (docs/35) ------------------------------------------------------------------------------

        private GameObject _emergencyFx;
        private string _emergencyFxKey;
        private Light _emergencyLight;
        private Transform _beacon;
        private readonly List<Transform> _bubbles = new();
        private Vector3 _fxTop;
        private float _fxSize = 1f;

        /// <summary>
        /// The building hit by an emergency flashes a red warning light with a beacon over it; a leak also streams
        /// bubbles. Driven by the survival state the server sends; it goes away when the emergency ends.
        /// </summary>
        private void UpdateEmergencyFx()
        {
            var survival = _stateSync?.View.Survival;
            var key = survival != null && !string.IsNullOrEmpty(survival.EmergencyId) ? survival.EmergencyBuildingId : null;
            var leak = survival != null && survival.EmergencyKind == HADAL.Shared.Simulation.EmergencyKinds.Leak;
            if (key != _emergencyFxKey || (key != null && _emergencyFx == null))
            {
                if (_emergencyFx != null)
                    Destroy(_emergencyFx);
                _emergencyFx = null;
                _bubbles.Clear();
                _emergencyFxKey = key;
                if (key != null && _instances.TryGetValue(key, out var instance) && instance.Go != null)
                    CreateEmergencyFx(instance.Go, leak);
            }

            if (_emergencyFx == null)
                return;

            var t = Time.time;
            var pulse = 0.5f + 0.5f * Mathf.Sin(t * 6f);
            if (_emergencyLight != null)
                _emergencyLight.intensity = Mathf.Lerp(0.6f, 4f, pulse);
            if (_beacon != null)
            {
                _beacon.position = _fxTop + Vector3.up * (_fxSize * (0.35f + 0.08f * Mathf.Sin(t * 2.5f)));
                _beacon.localScale = Vector3.one * (_fxSize * (0.14f + 0.04f * pulse));
            }

            for (var i = 0; i < _bubbles.Count; i++)
            {
                // Bubbles rise from the building, drift and start again at the bottom.
                var phase = Mathf.Repeat(t * 0.6f + i / (float)_bubbles.Count, 1f);
                var angle = i * 2.4f;
                _bubbles[i].position = _fxTop + new Vector3(Mathf.Cos(angle) * _fxSize * 0.25f, phase * _fxSize * 1.6f - _fxSize * 0.3f,
                    Mathf.Sin(angle) * _fxSize * 0.25f);
                _bubbles[i].localScale = Vector3.one * (_fxSize * (0.05f + 0.05f * phase));
            }
        }

        private void CreateEmergencyFx(GameObject building, bool leak)
        {
            var bounds = new Bounds(building.transform.position, Vector3.zero);
            var found = false;
            foreach (var r in building.GetComponentsInChildren<Renderer>())
            {
                if (!found)
                {
                    bounds = r.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            _fxSize = Mathf.Max(0.5f, Mathf.Max(bounds.size.x, bounds.size.z));
            _fxTop = new Vector3(bounds.center.x, found ? bounds.max.y : building.transform.position.y + 1f, bounds.center.z);

            _emergencyFx = new GameObject("EmergencyFx");
            _emergencyFx.transform.SetParent(building.transform.parent, true);
            var lightGo = new GameObject("WarningLight");
            lightGo.transform.SetParent(_emergencyFx.transform, false);
            lightGo.transform.position = _fxTop + Vector3.up * (_fxSize * 0.3f);
            _emergencyLight = lightGo.AddComponent<Light>();
            _emergencyLight.type = LightType.Point;
            _emergencyLight.color = new Color(1f, 0.15f, 0.1f);
            _emergencyLight.range = _fxSize * 3f;

            _propertyBlock ??= new MaterialPropertyBlock();
            _beacon = Ball("Beacon", new Color(1f, 0.2f, 0.12f, 1f));
            if (!leak)
                return;
            for (var i = 0; i < 6; i++)
                _bubbles.Add(Ball("Bubble", new Color(0.7f, 0.95f, 1f, 1f)));
        }

        private Transform Ball(string name, Color color)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = name;
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(_emergencyFx.transform, false);
            var renderer = ball.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _propertyBlock.SetColor("_Color", color);
            _propertyBlock.SetColor("_EmissionColor", color * 2f);
            renderer.SetPropertyBlock(_propertyBlock);
            return ball.transform;
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
