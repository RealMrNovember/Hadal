using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Enums;
using Hadal.Data.Events;
using Hadal.Data.Models;
using VContainer;

namespace Hadal.Gameplay.Grid
{
    /// <summary>
    /// Builds and maintains underwater dome slot visuals from polar grid data.
    /// </summary>
    public class CircularGridVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform _slotRoot;
        [SerializeField] private Transform _ringRoot;
        [SerializeField] private GridSlotView _slotPrefab;
        [SerializeField] private UnderwaterDomeRingView _ringPrefab;
        [SerializeField] private GridSlotHighlightEventSO _highlightEvent;

        private ICircularGridService _grid;
        private IPoolService _pool;
        private readonly Dictionary<PolarGridSlotId, GridSlotView> _slotViews = new();
        private readonly List<UnderwaterDomeRingView> _ringViews = new();

        [Inject]
        public void Construct(ICircularGridService grid, IPoolService pool)
        {
            _grid = grid;
            _pool = pool;
            RebuildVisuals();

            if (_highlightEvent != null)
                _highlightEvent.OnRaised += OnHighlightRequested;
        }

        private void OnDestroy()
        {
            if (_highlightEvent != null)
                _highlightEvent.OnRaised -= OnHighlightRequested;
        }

        public void RebuildVisuals()
        {
            ClearVisuals();
            if (_grid == null)
                return;

            BuildRingVisuals();
            BuildSlotVisuals();
        }

        private void BuildRingVisuals()
        {
            for (var ring = 0; ring <= _grid.UnlockedRingCount; ring++)
            {
                if (_ringPrefab == null)
                    continue;

                var ringView = Instantiate(_ringPrefab, _ringRoot != null ? _ringRoot : transform);
                ringView.Configure(_grid.Config, ring);
                _ringViews.Add(ringView);
            }
        }

        private void BuildSlotVisuals()
        {
            if (_slotPrefab == null)
                return;

            foreach (var slot in _grid.GetAllSlots())
            {
                if (!slot.IsUnlocked)
                    continue;

                var position = _grid.GetWorldPosition(slot.Id);
                var rotation = _grid.GetWorldRotation(slot.Id, 0f);
                var parent = _slotRoot != null ? _slotRoot : transform;

                GridSlotView view;
                if (_pool != null)
                {
                    var pool = _pool.GetOrCreatePool("grid_slot", _slotPrefab, parent);
                    view = pool.Get();
                    view.transform.SetParent(parent, false);
                }
                else
                {
                    view = Instantiate(_slotPrefab, parent);
                }

                view.Configure(slot, position, rotation);
                view.SetOccupiedVisual(slot.IsOccupied);
                _slotViews[slot.Id] = view;
            }
        }

        private void OnHighlightRequested(PolarGridSlotId slotId, GridHighlightState state)
        {
            if (_slotViews.TryGetValue(slotId, out var view))
                view.SetHighlight(state);
        }

        private void ClearVisuals()
        {
            foreach (var view in _slotViews.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            _slotViews.Clear();

            foreach (var ring in _ringViews)
            {
                if (ring != null)
                    Destroy(ring.gameObject);
            }

            _ringViews.Clear();
        }
    }
}
