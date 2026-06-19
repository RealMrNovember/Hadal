using System;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Data.Config;
using UnityEngine;
using VContainer;

namespace Hadal.Gameplay.Grid
{
    /// <summary>
    /// Phase 1 Sprint 2 — polar snap ghost preview, PlaceBuildingCommand dispatch, server ack reconciliation.
    /// </summary>
    public sealed class GridBuildController : MonoBehaviour
    {
        [SerializeField] private Camera _buildCamera;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private PreviewGhost _previewGhost;
        [SerializeField] private float _previewRotationDegrees;
        [SerializeField] private float _rotationSnapDegrees = 60f;

        private IGridService _grid;
        private ICircularGridService _circularGrid;
        private ICommandDispatcher _commandDispatcher;
        private ILocalEventBus _localBus;
        private BuildingDatabaseSO _buildingDatabase;

        private string _previewBuildingId;
        private GridSlot _hoverSlot;
        private bool _hasHoverSlot;
        private bool _placementPending;
        private string _pendingCommandId;
        private ulong _clientSequence;

        [Inject]
        public void Construct(
            IGridService grid,
            ICircularGridService circularGrid,
            ICommandDispatcher commandDispatcher,
            ILocalEventBus localBus,
            GameConfigSO config)
        {
            _grid = grid;
            _circularGrid = circularGrid;
            _commandDispatcher = commandDispatcher;
            _localBus = localBus;
            _buildingDatabase = config.BuildingDatabase;

            if (_buildCamera == null)
                _buildCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (_localBus != null)
                _localBus.Subscribe<BuildingPlacementResolvedEvent>(OnPlacementResolved);
        }

        private void OnDisable()
        {
            if (_localBus != null)
                _localBus.Unsubscribe<BuildingPlacementResolvedEvent>(OnPlacementResolved);
        }

        private void Update()
        {
            if (_grid == null || string.IsNullOrWhiteSpace(_previewBuildingId))
                return;

            if (_placementPending)
                return;

            if (!TryGetGroundPoint(out var groundPoint))
            {
                _hasHoverSlot = false;
                return;
            }

            if (!_grid.TrySnapWorldPoint(groundPoint, out _hoverSlot))
            {
                _hasHoverSlot = false;
                return;
            }

            _hasHoverSlot = true;

            var snappedRotation = _circularGrid?.SnapRotation(_previewRotationDegrees) ?? _previewRotationDegrees;
            var position = _grid.SlotToWorld(_hoverSlot);
            var rotation = _grid.SlotToRotation(_hoverSlot, snappedRotation);

            _previewGhost?.MoveTo(_hoverSlot, position, rotation);

            if (WasPointerPressedThisFrame())
                DispatchPlacement(_hoverSlot, snappedRotation);
        }

        public void BeginPreview(string buildingDefinitionId)
        {
            _previewBuildingId = buildingDefinitionId;
            _previewRotationDegrees = 0f;
            _placementPending = false;
            _pendingCommandId = null;
            _previewGhost?.EnsureVisible();
        }

        public void EndPreview()
        {
            _previewBuildingId = null;
            _hasHoverSlot = false;
            _placementPending = false;
            _pendingCommandId = null;
            _previewGhost?.Hide();
        }

        public void RotatePreviewClockwise(float stepDegrees = 60f)
        {
            if (_placementPending)
                return;

            _previewRotationDegrees += stepDegrees;
        }

        private void DispatchPlacement(GridSlot slot, float rotationDegrees)
        {
            if (_commandDispatcher == null || string.IsNullOrWhiteSpace(_previewBuildingId))
                return;

            if (!_grid.IsSlotAvailable(slot))
            {
                _previewGhost?.PlayRejectionShake(0.35f);
                return;
            }

            var commandId = Guid.NewGuid().ToString("N");
            var rotationSteps = Mathf.RoundToInt(rotationDegrees / _rotationSnapDegrees);
            var command = new PlaceBuildingCommand
            {
                CommandId = commandId,
                ClientSequence = ++_clientSequence,
                BuildingDefinitionId = _previewBuildingId,
                RingIndex = slot.RingIndex,
                SectorIndex = slot.SectorIndex,
                RotationSteps = rotationSteps,
                Radius = slot.Radius,
                AngleDegrees = slot.AngleDegrees
            };

            _placementPending = true;
            _pendingCommandId = commandId;
            _previewGhost?.SetPending(true);

            _localBus?.Publish(new BuildingPlacementPendingEvent
            {
                CommandId = commandId,
                BuildingDefinitionId = _previewBuildingId
            });

            _commandDispatcher.Dispatch(command);
            Debug.Log($"[GridBuildController] Dispatched PlaceBuildingCommand {commandId} at {slot}.");
        }

        private void OnPlacementResolved(BuildingPlacementResolvedEvent evt)
        {
            if (!_placementPending || !string.Equals(evt.CommandId, _pendingCommandId, StringComparison.Ordinal))
                return;

            _placementPending = false;
            _pendingCommandId = null;

            if (evt.Result == CommandResultCode.Accepted)
            {
                _previewGhost?.SetPending(false);
                _previewGhost?.Hide();
            }
            else
            {
                _previewGhost?.SetPending(false);
                _previewGhost?.PlayRejectionShake(0.35f);
            }
        }

        private bool TryGetGroundPoint(out Vector3 groundPoint)
        {
            groundPoint = default;

            if (_buildCamera == null)
                return false;

            var ray = _buildCamera.ScreenPointToRay(GetPointerScreenPosition());
            if (Physics.Raycast(ray, out var hit, 500f, _groundMask))
            {
                groundPoint = hit.point;
                return true;
            }

            return false;
        }

        private static Vector3 GetPointerScreenPosition()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
#endif
            return Input.mousePosition;
        }

        private static bool WasPointerPressedThisFrame()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                return true;
#endif
            return Input.GetMouseButtonDown(0);
        }
    }
}
