using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.Simulation;
using Hadal.Data.Models;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Moving a building (Move in its round menu): a bar asks for the new slot, the next tap on the base picks it. A free
    /// slot takes the building; another building swaps places with it. Free, level and running work kept; the same
    /// rules as the server (PlacementRules.ValidateMove) answer at once, so a wrong slot never waits on a round trip.
    /// </summary>
    public sealed partial class BaseManagementView
    {
        private string _movingKey = string.Empty;
        private GameObject _moveBar;

        private void StartMove(BuildingSaveEntry building)
        {
            TargetMenu.Hide();
            Close();
            EndMove();
            _movingKey = building.instanceKey;

            var bar = UiKit.Box("MoveBar", _canvas.transform, new Color(0.02f, 0.12f, 0.17f, 0.94f));
            bar.rectTransform.Place(0.03f, 0.8f, 0.97f, 0.87f);
            var text = UiKit.Label("Text", bar.transform, Loc.F("base.move.pick", Loc.Building(building.definitionId)), 30, Color.white);
            text.rectTransform.Place(0.04f, 0f, 0.7f, 1f);
            var cancel = UiKit.Button("Cancel", bar.transform, Loc.T("base.cancel"), EndMove, UiKit.Danger, 30);
            ((RectTransform)cancel.transform).Place(0.73f, 0.15f, 0.97f, 0.85f);
            _moveBar = bar.gameObject;
        }

        private void EndMove()
        {
            _movingKey = string.Empty;
            if (_moveBar != null)
                Destroy(_moveBar);
            _moveBar = null;
        }

        /// <summary>The tap that ends a running move: true when it was taken (moved, refused, or the building itself to cancel).</summary>
        private bool TryMoveTo(int ring, int sector)
        {
            if (string.IsNullOrEmpty(_movingKey))
                return false;

            var moving = Buildings().FirstOrDefault(b => b.instanceKey == _movingKey);
            EndMove();
            if (string.IsNullOrEmpty(moving.instanceKey) || (moving.cellRing == ring && moving.cellSector == sector))
                return true;

            var occupant = Buildings().FirstOrDefault(b => b.cellRing == ring && b.cellSector == sector);
            var verdict = PlacementRules.ValidateMove(_catalog, moving.definitionId, moving.cellRing, moving.cellSector,
                ring, sector, _grid.UnlockedRingCount, string.IsNullOrEmpty(occupant.instanceKey) ? null : occupant.definitionId);
            if (verdict != PlacementRejectReason.None)
            {
                Toast(Loc.Reason(verdict.ToString()), UiKit.Danger);
                return true;
            }

            _commands.Dispatch(new MoveBuildingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                BuildingEntityId = moving.instanceKey,
                RingIndex = ring,
                SectorIndex = sector
            });
            return true;
        }
    }
}
