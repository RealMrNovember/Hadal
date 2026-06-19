using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.DI;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Events;
using Hadal.Data.Models;

namespace Hadal.Gameplay.Grid
{
    public class BuildingGhostPreview : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _validTint = new(0.2f, 1f, 0.8f, 0.55f);
        [SerializeField] private Color _invalidTint = new(1f, 0.25f, 0.25f, 0.55f);

        private MaterialPropertyBlock _propertyBlock;
        private GameObject _ghostInstance;
        private BuildingDefinitionSO _definition;
        private PolarGridSlotId _slotId;
        private float _rotation;
        private bool _isValid;

        public void Show(BuildingDefinitionSO definition, PolarGridSlotId slotId, float rotation, bool isValid, Vector3 position, Quaternion worldRotation)
        {
            _definition = definition;
            _slotId = slotId;
            _rotation = rotation;
            _isValid = isValid;

            EnsureGhostInstance(definition);
            if (_ghostInstance == null)
                return;

            _ghostInstance.transform.SetPositionAndRotation(position, worldRotation);
            ApplyTint(isValid);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_ghostInstance != null)
                _ghostInstance.SetActive(false);

            gameObject.SetActive(false);
        }

        private void EnsureGhostInstance(BuildingDefinitionSO definition)
        {
            if (definition?.GhostPrefab == null)
                return;

            if (_ghostInstance != null)
                return;

            _ghostInstance = Instantiate(definition.GhostPrefab, transform);
            _renderers = _ghostInstance.GetComponentsInChildren<Renderer>();
        }

        private void ApplyTint(bool valid)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            var tint = valid ? _validTint : _invalidTint;

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }

    public class GridHighlightController : MonoBehaviour
    {
        [SerializeField] private GridSlotHighlightEventSO _highlightEvent;
        [SerializeField] private BuildPreviewChangedEventSO _previewChangedEvent;

        private ICircularGridService _grid;
        private PolarGridSlotId _selectedSlot;
        private GridHighlightState _currentState = GridHighlightState.None;

        private void Start()
        {
            if (GameContext.Current != null)
                GameContext.Current.TryResolve(out _grid);
        }

        public void HighlightSlot(PolarGridSlotId slotId, GridHighlightState state)
        {
            ClearHighlight();
            _selectedSlot = slotId;
            _currentState = state;
            _highlightEvent?.Raise(slotId, state);
        }

        public void PreviewPlacement(string buildingId, BuildingDefinitionSO definition, PolarGridSlotId slotId, float rotation)
        {
            if (_grid == null || definition == null)
                return;

            var validation = _grid.ValidatePlacement(definition, slotId, rotation);
            var state = validation.IsValid ? GridHighlightState.Valid : GridHighlightState.Invalid;
            HighlightSlot(slotId, state);
            _previewChangedEvent?.Raise(buildingId, slotId, rotation, validation.IsValid);
        }

        public void ClearHighlight()
        {
            if (_currentState != GridHighlightState.None)
                _highlightEvent?.Raise(_selectedSlot, GridHighlightState.None);

            _currentState = GridHighlightState.None;
        }
    }
}
