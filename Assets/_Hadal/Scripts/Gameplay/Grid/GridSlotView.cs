using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Gameplay.Grid
{
    public class GridSlotView : MonoBehaviour
    {
        [SerializeField] private PolarGridSlotId _slotId;
        [SerializeField] private Renderer _floorRenderer;
        [SerializeField] private Renderer _highlightRenderer;
        [SerializeField] private Color _validColor = new(0.1f, 0.9f, 1f, 0.45f);
        [SerializeField] private Color _invalidColor = new(1f, 0.2f, 0.2f, 0.45f);
        [SerializeField] private Color _selectedColor = new(1f, 0.85f, 0.2f, 0.55f);
        [SerializeField] private Color _occupiedColor = new(0.2f, 0.2f, 0.25f, 0.35f);

        private GridHighlightState _state = GridHighlightState.None;
        private MaterialPropertyBlock _propertyBlock;

        public PolarGridSlotId SlotId => _slotId;

        public void Configure(PolarGridSlot slot, Vector3 worldPosition, Quaternion worldRotation)
        {
            _slotId = slot.Id;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            SetHighlight(GridHighlightState.None);
        }

        public void SetHighlight(GridHighlightState state)
        {
            _state = state;
            if (_highlightRenderer == null)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            _highlightRenderer.GetPropertyBlock(_propertyBlock);

            var color = state switch
            {
                GridHighlightState.Valid => _validColor,
                GridHighlightState.Invalid => _invalidColor,
                GridHighlightState.Selected => _selectedColor,
                GridHighlightState.Occupied => _occupiedColor,
                _ => Color.clear
            };

            _propertyBlock.SetColor("_BaseColor", color);
            _highlightRenderer.SetPropertyBlock(_propertyBlock);
            _highlightRenderer.enabled = state != GridHighlightState.None;
        }

        public void SetOccupiedVisual(bool occupied)
        {
            if (_floorRenderer != null)
                _floorRenderer.enabled = !occupied;

            if (occupied)
                SetHighlight(GridHighlightState.Occupied);
        }
    }
}
