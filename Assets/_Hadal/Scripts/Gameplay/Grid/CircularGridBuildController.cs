using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Models;
using VContainer;

namespace Hadal.Gameplay.Grid
{
    /// <summary>
    /// Phase 1 build-mode controller for circular grid placement, rotation, and snapping.
    /// </summary>
    public class CircularGridBuildController : MonoBehaviour
    {
        [SerializeField] private Camera _buildCamera;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private BuildingGhostPreview _ghostPreview;
        [SerializeField] private GridHighlightController _highlightController;
        [SerializeField] private float _rotationStepDegrees = 60f;

        private ICircularGridService _grid;
        private IBuildingService _buildingService;
        private BuildingDatabaseSO _buildingDatabase;

        private BuildModeState _mode = BuildModeState.Idle;
        private string _selectedBuildingId;
        private float _previewRotation;
        private PolarGridSlotId _hoverSlot;

        [Inject]
        public void Construct(ICircularGridService grid, IBuildingService buildingService, GameConfigSO config)
        {
            _grid = grid;
            _buildingService = buildingService;
            _buildingDatabase = config.BuildingDatabase;

            if (_buildCamera == null)
                _buildCamera = Camera.main;
        }

        private void Update()
        {
            if (_mode != BuildModeState.Placing || _grid == null || string.IsNullOrWhiteSpace(_selectedBuildingId))
                return;

            UpdateHoverSlot();
            HandleRotationInput();

            var definition = _buildingDatabase?.GetById(_selectedBuildingId);
            if (definition == null)
                return;

            var snappedRotation = _grid.SnapRotation(_previewRotation);
            var validation = _grid.ValidatePlacement(definition, _hoverSlot, snappedRotation);

            _highlightController?.PreviewPlacement(_selectedBuildingId, definition, _hoverSlot, snappedRotation);

            var position = _grid.GetWorldPosition(_hoverSlot);
            var rotation = _grid.GetWorldRotation(_hoverSlot, snappedRotation);
            _ghostPreview?.Show(definition, _hoverSlot, snappedRotation, validation.IsValid, position, rotation);

            if (Input.GetMouseButtonDown(0) && validation.IsValid)
                TryConfirmPlacement(definition, snappedRotation);
        }

        public void EnterBuildMode(string buildingId)
        {
            _selectedBuildingId = buildingId;
            _previewRotation = 0f;
            _mode = BuildModeState.Placing;
        }

        public void ExitBuildMode()
        {
            _mode = BuildModeState.Idle;
            _selectedBuildingId = null;
            _ghostPreview?.Hide();
            _highlightController?.ClearHighlight();
        }

        public void RotatePreviewClockwise()
        {
            _previewRotation += _rotationStepDegrees;
        }

        public void RotatePreviewCounterClockwise()
        {
            _previewRotation -= _rotationStepDegrees;
        }

        private void UpdateHoverSlot()
        {
            if (_buildCamera == null)
                return;

            var ray = _buildCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 500f, _groundMask))
                return;

            if (_grid.TryGetNearestSlot(hit.point, out var nearest))
                _hoverSlot = nearest;
        }

        private void HandleRotationInput()
        {
            if (Input.GetKeyDown(KeyCode.E))
                RotatePreviewClockwise();

            if (Input.GetKeyDown(KeyCode.Q))
                RotatePreviewCounterClockwise();
        }

        private void TryConfirmPlacement(BuildingDefinitionSO definition, float rotation)
        {
            if (!definition.AllowRotation)
                rotation = 0f;

            if (_buildingService.TryPlaceBuilding(definition.Id, _hoverSlot, rotation))
                ExitBuildMode();
        }
    }
}
