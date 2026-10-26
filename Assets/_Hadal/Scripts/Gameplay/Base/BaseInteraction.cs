using System;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace Hadal.Gameplay.Base
{
    /// <summary>
    /// Turns a short, still tap on the seabed into <see cref="BaseSlotTappedEvent"/> for the slot
    /// under the finger. Drags stay with the camera rig; taps over UI are ignored.
    /// </summary>
    public sealed class BaseInteraction : MonoBehaviour
    {
        private const float MaxTapSeconds = 0.35f;
        private const float MaxTapPixels = 18f;
        private const float MaxSlotDistance = 3f; // building pad radius 2.3 m + margin

        [SerializeField] private Camera _camera;

        private ICircularGridService _grid;
        private IStateSyncService _stateSync;
        private ILocalEventBus _localBus;

        private Vector2 _downPosition;
        private float _downTime;
        private bool _tracking;

        [Inject]
        public void Construct(ICircularGridService grid, IStateSyncService stateSync, ILocalEventBus localBus)
        {
            _grid = grid;
            _stateSync = stateSync;
            _localBus = localBus;
        }

        public void SetCamera(Camera camera) => _camera = camera;

        private void Update()
        {
            if (_grid == null || _localBus == null || Hadal.Core.State.GameMode.InWorld)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                // Touch 0 arrives as mouse button 0 on mobile.
                _tracking = !PointerOverUi();
                _downPosition = Input.mousePosition;
                _downTime = Time.unscaledTime;
            }
            else if (Input.GetMouseButtonUp(0) && _tracking)
            {
                _tracking = false;
                var moved = ((Vector2)Input.mousePosition - _downPosition).magnitude;
                if (moved <= MaxTapPixels && Time.unscaledTime - _downTime <= MaxTapSeconds && Input.touchCount <= 1)
                    HandleTap(Input.mousePosition);
            }
        }

        private static bool PointerOverUi()
        {
            var events = EventSystem.current;
            if (events == null)
                return false;

            return Input.touchCount > 0
                ? events.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : events.IsPointerOverGameObject();
        }

        public void HandleTap(Vector2 screenPoint)
        {
            if (_grid.Config == null)
                return;

            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || !TrySeabedPoint(camera.ScreenPointToRay(screenPoint), out var point))
                return;

            if (!_grid.TryGetNearestSlot(point, out var slot))
                return;

            var centre = _grid.GetWorldPosition(slot);
            if (new Vector2(centre.x - point.x, centre.z - point.z).magnitude > MaxSlotDistance)
                return;

            _localBus.Publish(new BaseSlotTappedEvent
            {
                Ring = slot.Ring,
                Sector = slot.Sector,
                BuildingInstanceKey = BuildingAt(slot)
            });
        }

        private string BuildingAt(PolarGridSlotId slot)
        {
            foreach (var building in _stateSync.View.Data.buildings ?? Array.Empty<BuildingSaveEntry>())
            {
                if (building.cellRing == slot.Ring && building.cellSector == slot.Sector)
                    return building.instanceKey;
            }

            return string.Empty;
        }

        /// <summary>Intersection with the base plane (y = 0); the base has no colliders.</summary>
        public static bool TrySeabedPoint(Ray ray, out Vector3 point)
        {
            point = default;
            if (Mathf.Abs(ray.direction.y) < 1e-4f)
                return false;

            var t = -ray.origin.y / ray.direction.y;
            if (t <= 0f)
                return false;

            point = ray.origin + ray.direction * t;
            return true;
        }
    }
}
