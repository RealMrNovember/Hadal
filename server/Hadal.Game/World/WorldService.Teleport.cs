using HADAL.Shared.Commands;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.World
{
    /// <summary>
    /// Base teleports (TeleportRules): random (the escape when under attack), to a chosen free tile, or into the
    /// alliance's waters — next to the chosen mate, or, used from the bag, next to the leader (then officers, then
    /// the rest) on the first free tile found.
    /// </summary>
    public sealed partial class WorldService
    {
        private void HandleTeleport(PlayerSession session, TeleportCommand command, DateTimeOffset now)
        {
            var mode = command.Mode ?? string.Empty;
            if (!TeleportRules.IsMode(mode))
            {
                session.Acknowledge(command.CommandId, command.ClientSequence, "MalformedCommand", now);
                return;
            }

            // Teams out on the map would come home to an empty tile.
            if (session.MarchViews().Count > 0)
            {
                session.Acknowledge(command.CommandId, command.ClientSequence, "TeamsOut", now);
                return;
            }

            var myAlliance = mode == TeleportRules.Alliance ? _social.AllianceIdOf(session.PublicId) : string.Empty;
            var mates = mode == TeleportRules.Alliance ? _social.AllianceMatesOf(session.PublicId) : Array.Empty<string>();
            var chosen = command.MemberId ?? string.Empty;
            var candidates = chosen.Length == 0 ? mates : mates.Contains(chosen) ? new[] { chosen } : Array.Empty<string>();
            lock (_gate)
            {
                MaintainNodes(now);
                var me = Upsert(session);
                var world = _runtime.Current.Catalog.World;
                var size = Math.Max(10, world.size);
                (int X, int Y)? target = null;
                string? reason = null;

                switch (mode)
                {
                    case TeleportRules.Random:
                        target = PlaceBase();
                        break;
                    case TeleportRules.Target:
                        if (command.X < 0 || command.Y < 0 || command.X >= size || command.Y >= size)
                            reason = "OutOfMap";
                        else if (!CanStand(command.X, command.Y))
                            reason = "TileTaken";
                        else
                            target = (command.X, command.Y);
                        break;
                    case TeleportRules.Alliance:
                        if (myAlliance.Length == 0)
                        {
                            reason = "NotInAlliance";
                            break;
                        }

                        // From the bag (no mate chosen) the jump lands in the alliance's territory when it has one (docs/30 §6.3).
                        if (chosen.Length == 0 && TerritoryTile(myAlliance) is { } inside)
                        {
                            target = inside;
                            break;
                        }

                        var placed = candidates.Where(id => _bases.ContainsKey(id)).ToList();
                        if (placed.Count == 0)
                        {
                            reason = "UnknownMember";
                            break;
                        }

                        foreach (var id in placed)
                        {
                            var mate = _bases[id];
                            if ((target = NearestFree(mate.X, mate.Y, TeleportRules.AllianceRadius, size)) != null)
                                break;
                        }

                        if (target == null)
                            reason = "NoRoom";
                        break;
                }

                if (reason == null && target is { } t && t == (me.X, me.Y))
                    reason = "TileTaken";
                if (reason != null || target == null)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, reason ?? "NoRoom", now);
                    return;
                }

                // The jump is paid (item or Hadalite) only once the destination is sure.
                var unpaid = session.PayTeleport(mode, now);
                if (unpaid != null)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, unpaid, now);
                    return;
                }

                var (x, y) = target.Value;
                _taken.Remove((me.X, me.Y));
                var moved = me with { X = x, Y = y };
                _bases[me.PlayerId] = moved;
                _taken.Add((x, y));
                _writes.Writer.TryWrite(moved);
                session.Acknowledge(command.CommandId, command.ClientSequence, null, now);
                _logger.LogInformation("Player {PlayerId} teleported ({Mode}) from {FromX},{FromY} to {X},{Y}.", me.PlayerId, mode, me.X, me.Y, x, y);
            }
        }

        /// <summary>A base may stand here: inside the zones bases may use, not on a base or a node. Caller holds the lock.</summary>
        private bool CanStand(int x, int y)
        {
            var world = _runtime.Current.Catalog.World;
            if (_taken.Contains((x, y)) || _strongholdAt.ContainsKey((x, y)) || _nodeAt.ContainsKey((x, y)) || _monsterAt.ContainsKey((x, y)))
                return false;
            return GatherRules.BeltOf(world, x, y) is not { bases: false };
        }

        /// <summary>The free tile closest to (cx, cy), ring by ring out to <paramref name="radius"/>. Caller holds the lock.</summary>
        private (int X, int Y)? NearestFree(int cx, int cy, int radius, int size, Func<int, int, bool>? allowed = null)
        {
            for (var r = 1; r <= radius; r++)
            {
                (int X, int Y)? best = null;
                var bestDistance = double.MaxValue;
                for (var dx = -r; dx <= r; dx++)
                {
                    for (var dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                            continue;
                        var x = cx + dx;
                        var y = cy + dy;
                        if (x < 0 || y < 0 || x >= size || y >= size || !CanStand(x, y) || (allowed != null && !allowed(x, y)))
                            continue;
                        var d = GatherRules.Km(cx, cy, x, y);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            best = (x, y);
                        }
                    }
                }

                if (best != null)
                    return best;
            }

            return null;
        }
    }
}
