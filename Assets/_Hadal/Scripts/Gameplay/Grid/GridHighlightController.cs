using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Events;
using Hadal.Data.Models;
using VContainer;

namespace Hadal.Gameplay.Grid
{
    public class GridHighlightController : MonoBehaviour
    {
        [SerializeField] private GridSlotHighlightEventSO _highlightEvent;
        [SerializeField] private BuildPreviewChangedEventSO _previewChangedEvent;

        private ICircularGridService _grid;
        private PolarGridSlotId _selectedSlot;
        private GridHighlightState _currentState = GridHighlightState.None;

        [Inject]
        public void Construct(ICircularGridService grid)
        {
            _grid = grid;
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
