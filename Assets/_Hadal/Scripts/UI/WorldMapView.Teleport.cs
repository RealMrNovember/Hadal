using System;
using System.Collections.Generic;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Moving the base (TeleportRules): tapping open sea offers "Teleport here", an alliance mate's base offers
    /// "Teleport next to them", the player's own base a random jump. Each uses its bag item or Hadalite.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private bool _recentreOnView;

        /// <summary>A tap on open sea: the tile, how far it is, and the jump there.</summary>
        private void ShowTile(int x, int y)
        {
            var view = View.WorldView;
            if (view == null)
                return;
            var km = GatherRules.Km(x, y, view.MyX, view.MyY);
            var info = $"{Loc.F("world.coords", x, y)}   {Loc.F("world.tiles", km.ToString("0.#"))}\n{TeleportPrice(TeleportRules.Target)}";
            var items = new List<TargetMenu.Item>
            {
                new("nav_world", Loc.T("world.teleport.here"), () => Teleport(TeleportRules.Target, x, y, string.Empty)),
                new("chat", Loc.T("share.title"), () => ShareLocation?.Invoke(x, y, "tile", Loc.F("world.coords", x, y)))
            };
            AddLandItems(items, x, y);
            GlideTo(x, y);
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 1f, Loc.T("world.teleport.sea"), info, items);
        }

        /// <summary>"1 in the bag" or "200 Hadalite": what the jump will take.</summary>
        private string TeleportPrice(string mode)
        {
            var item = TeleportRules.ItemFor(mode);
            var held = View.Items.TryGetValue(item, out var n) ? n : 0;
            return held > 0 ? Loc.F("world.teleport.item", held) : Loc.F("world.teleport.hadalite", TeleportRules.HadaliteFor(mode).ToString("N0"));
        }

        public void Teleport(string mode, int x, int y, string memberId)
        {
            ClosePopup();
            TargetMenu.Hide();
            _commands.Dispatch(new TeleportCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Mode = mode, X = x, Y = y, MemberId = memberId ?? string.Empty
            });
        }

        /// <summary>After a jump the map asks for the new home and flies there.</summary>
        private void AfterTeleport()
        {
            _recentreOnView = true;
            Request(-1, -1);
        }

        /// <summary>Called when a new view arrives: after a jump, the camera goes to the new home.</summary>
        private void RecentreIfTeleported(WorldViewDto view)
        {
            if (!_recentreOnView)
                return;
            _recentreOnView = false;
            _focus = TileToWorld(view.MyX, view.MyY);
            _requested = new Vector2Int(view.MyX, view.MyY);
        }

        /// <summary>A short tap (no drag) on the sea floor, not on any plate or button.</summary>
        private void HandleSeaTap()
        {
            if (!(Input.GetMouseButtonUp(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)))
                return;
            if (_gestureOnUi || _popup != null || ((Vector2)Input.mousePosition - (Vector2)_downAt).magnitude > 18f)
                return;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var enter))
                return;
            var tile = WorldToTile(ray.GetPoint(enter));
            var view = View.WorldView;
            if (view == null || tile.x < 0 || tile.y < 0 || tile.x >= view.MapSize || tile.y >= view.MapSize)
                return;
            // Bases, nodes and monsters answer through their own plates.
            foreach (var b in view.Bases ?? Array.Empty<WorldBaseDto>())
            {
                if (b.X == tile.x && b.Y == tile.y)
                    return;
            }

            foreach (var n in view.Nodes ?? Array.Empty<WorldNodeDto>())
            {
                if (n.X == tile.x && n.Y == tile.y)
                    return;
            }

            foreach (var m in view.Monsters ?? Array.Empty<WorldMonsterDto>())
            {
                if (m.X == tile.x && m.Y == tile.y)
                    return;
            }

            ShowTile(tile.x, tile.y);
        }
    }
}
