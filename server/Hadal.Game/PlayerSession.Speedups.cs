using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// Speed-up items (Last Asylum's speed-up window): minutes off a construction, a training batch, the Med Bay or a
    /// research, or a share of the time left off a march on the move. Whatever reaches zero completes on the spot.
    /// </summary>
    public sealed partial class PlayerSession
    {
        private void UseSpeedup(UseSpeedupCommand command, DateTimeOffset now)
        {
            var count = Math.Clamp(command.Count, 1, 999);
            if (!Speedups.TryParse(command.ItemId, out var kind, out var value, out var percent))
            {
                Reject(command.CommandId, command.ClientSequence, SpeedupRejectReason.UnknownItem.ToString(), now);
                return;
            }

            if ((_state.Items.TryGetValue(command.ItemId, out var held) ? held : 0) < count)
            {
                Reject(command.CommandId, command.ClientSequence, SpeedupRejectReason.NotEnoughItems.ToString(), now);
                return;
            }

            var target = command.Target ?? string.Empty;
            var colon = target.IndexOf(':');
            var what = colon < 0 ? target : target.Substring(0, colon);
            var id = colon < 0 ? string.Empty : target.Substring(colon + 1);
            var changes = new List<EntityChangeDto>();
            var verdict = what switch
            {
                "build" => SpeedBuild(id, kind, value, count, now, changes),
                "train" => SpeedTraining(id, kind, value, count, now),
                "heal" => SpeedHeal(kind, value, count, now),
                "research" => SpeedResearch(id, kind, value, count, now),
                "march" => SpeedMarch(id, kind, value, percent, count, now),
                _ => SpeedupRejectReason.WrongTarget
            };
            if (verdict != SpeedupRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            TakeItem(command.ItemId, count);
            if (!percent)
                Score(EventActions.SpeedupMinutes, (long)value * count);
            _state.IsDirty = true;

            // Timers that reached zero complete now, with everything that follows from them.
            changes.AddRange(AdvanceTo(now));
            changes.Add(InventoryChange());
            changes.Add(ArmyChange(now));
            changes.Add(MarchesChange(now));
            changes.Add(ResearchChange(now));
            changes.Add(Accepted(command.CommandId, command.ClientSequence));
            SendDelta(now, changes);
        }

        private static DateTimeOffset Cut(DateTimeOffset endsAt, int value, int count, DateTimeOffset now) =>
            endsAt.AddSeconds(-Speedups.Cut(value, false, count, (endsAt - now).TotalSeconds));

        private SpeedupRejectReason SpeedBuild(string id, SpeedupKind kind, int value, int count, DateTimeOffset now, List<EntityChangeDto> changes)
        {
            if (!Speedups.Fits(kind, SpeedupKind.Build))
                return SpeedupRejectReason.WrongTarget;
            var building = FindBuilding(id);
            if (building?.ConstructionEndsAt is not { } ends || ends <= now)
                return SpeedupRejectReason.NothingToSpeedUp;
            building.ConstructionEndsAt = Cut(ends, value, count, now);
            MarkChanged(building);
            changes.Add(BuildingChange(building, now));
            return SpeedupRejectReason.None;
        }

        private SpeedupRejectReason SpeedTraining(string id, SpeedupKind kind, int value, int count, DateTimeOffset now)
        {
            if (!Speedups.Fits(kind, SpeedupKind.Train))
                return SpeedupRejectReason.WrongTarget;
            var training = _state.Training.FirstOrDefault(t => t.BuildingId == id);
            if (training == null || training.EndsAt <= now)
                return SpeedupRejectReason.NothingToSpeedUp;
            training.EndsAt = Cut(training.EndsAt, value, count, now);
            return SpeedupRejectReason.None;
        }

        private SpeedupRejectReason SpeedHeal(SpeedupKind kind, int value, int count, DateTimeOffset now)
        {
            if (!Speedups.Fits(kind, SpeedupKind.Heal))
                return SpeedupRejectReason.WrongTarget;
            if (_state.Healing is not { } healing || healing.EndsAt <= now)
                return SpeedupRejectReason.NothingToSpeedUp;
            healing.EndsAt = Cut(healing.EndsAt, value, count, now);
            return SpeedupRejectReason.None;
        }

        private SpeedupRejectReason SpeedResearch(string id, SpeedupKind kind, int value, int count, DateTimeOffset now)
        {
            if (!Speedups.Fits(kind, SpeedupKind.Research))
                return SpeedupRejectReason.WrongTarget;
            var research = _state.ActiveResearch.FirstOrDefault(r => r.NodeId == id);
            if (research == null || research.EndsAt <= now)
                return SpeedupRejectReason.NothingToSpeedUp;
            research.EndsAt = Cut(research.EndsAt, value, count, now);
            return SpeedupRejectReason.None;
        }

        /// <summary>
        /// A march on the move: going out, the cut brings arrival (and so the gathering and the way home) forward;
        /// coming back, it brings home forward. Gathering at the node has nothing to speed up.
        /// </summary>
        private SpeedupRejectReason SpeedMarch(string id, SpeedupKind kind, int value, bool percent, int count, DateTimeOffset now)
        {
            if (kind != SpeedupKind.March)
                return SpeedupRejectReason.WrongTarget;
            var march = _state.Marches.FirstOrDefault(m => m.Id == id);
            if (march == null)
                return SpeedupRejectReason.NothingToSpeedUp;

            if (now < march.ArrivesAt)
            {
                var cut = TimeSpan.FromSeconds(Speedups.Cut(value, percent, count, (march.ArrivesAt - now).TotalSeconds));
                march.ArrivesAt -= cut;
                march.GatheredAt -= cut;
                march.HomeAt -= cut;
            }
            else if (now >= march.GatheredAt && now < march.HomeAt)
            {
                march.HomeAt -= TimeSpan.FromSeconds(Speedups.Cut(value, percent, count, (march.HomeAt - now).TotalSeconds));
            }
            else
            {
                return SpeedupRejectReason.NothingToSpeedUp;
            }

            _marchesDirty = true;
            return SpeedupRejectReason.None;
        }
    }
}
