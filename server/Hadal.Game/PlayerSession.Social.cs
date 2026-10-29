using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>A running construction as an alliance help request sees it.</summary>
    public readonly record struct ConstructionInfo(string BuildingId, string DefinitionId, int TargetLevel, int TotalSeconds, DateTimeOffset EndsAt);

    /// <summary>
    /// A running job alliance mates can help (HelpKinds): the building or research node with its target level, or the
    /// healing batch with its troop count, the job's total time and when it ends.
    /// </summary>
    public readonly record struct HelpJobInfo(string DefinitionId, int TargetLevel, int TotalSeconds, DateTimeOffset EndsAt);

    /// <summary>
    /// What the <see cref="Social.SocialHub"/> may ask of a session. Lock order is always hub, then
    /// session: nothing here calls back into the hub.
    /// </summary>
    public sealed partial class PlayerSession
    {
        private string _displayName = string.Empty;

        /// <summary>Public player id (the snapshot's PlayerId).</summary>
        public string PublicId => _state.PlayerId.ToString("N");

        public string DisplayName
        {
            get { lock (_gate) return _displayName; }
            internal set { lock (_gate) _displayName = value; }
        }

        /// <summary>The player's chosen avatar (hero portrait id; empty = initial).</summary>
        public string AvatarId
        {
            get { lock (_gate) return _state.AvatarId; }
        }

        public long Power
        {
            get { lock (_gate) return ComputePower(); }
        }

        /// <summary>Core Reactor level (world map).</summary>
        public int BaseLevel
        {
            get { lock (_gate) return ReactorLevel(); }
        }

        /// <summary>Sends entity changes that are not part of this player's economy (chat, alliance).</summary>
        internal void Push(List<EntityChangeDto> changes, DateTimeOffset now)
        {
            lock (_gate)
                SendDelta(now, changes);
        }

        internal void Acknowledge(string commandId, ulong sequence, string? rejectReason, DateTimeOffset now, List<EntityChangeDto>? changes = null)
        {
            lock (_gate)
            {
                var all = changes ?? new List<EntityChangeDto>();
                all.Add(rejectReason == null
                    ? Accepted(commandId, sequence)
                    : AckChange(commandId, sequence, HADAL.Shared.Enums.CommandResultCode.Rejected, rejectReason));
                SendDelta(now, all);
            }
        }

        /// <summary>Charges <paramref name="cost"/> if affordable and pushes the wallet.</summary>
        internal bool TryCharge(IReadOnlyList<KeyValuePair<HADAL.Shared.Enums.ResourceType, long>> cost, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                if (!BuildingProgression.CanAfford(cost, _state.Wallet))
                    return false;

                BuildingProgression.Charge(cost, _state.Wallet);
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { WalletChange() });
                return true;
            }
        }

        internal ConstructionInfo? Construction(string buildingId, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                var building = FindBuilding(buildingId);
                if (building?.ConstructionEndsAt == null)
                    return null;
                return new ConstructionInfo(building.BuildingId, building.DefinitionId, building.Level + 1,
                    building.ConstructionSeconds, building.ConstructionEndsAt.Value);
            }
        }

        /// <summary>The running job of a help request (HelpKinds), or null when there is none to help.</summary>
        internal HelpJobInfo? HelpJob(string kind, string target, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                switch (kind)
                {
                    case HelpKinds.Build:
                        var building = FindBuilding(target);
                        return building?.ConstructionEndsAt is { } ends && ends > now
                            ? new HelpJobInfo(building.DefinitionId, building.Level + 1, building.ConstructionSeconds, ends)
                            : null;
                    case HelpKinds.Heal:
                        return _state.Healing is { } healing && healing.EndsAt > now
                            ? new HelpJobInfo(string.Empty, (int)Math.Min(int.MaxValue, healing.Troops.Values.Sum()),
                                (int)Math.Max(1, (healing.EndsAt - healing.StartedAt).TotalSeconds), healing.EndsAt)
                            : null;
                    case HelpKinds.Research:
                        var research = _state.ActiveResearch.FirstOrDefault(r => r.NodeId == target);
                        return research != null && research.EndsAt > now
                            ? new HelpJobInfo(research.NodeId, research.TargetLevel, research.Seconds, research.EndsAt)
                            : null;
                    default:
                        return null;
                }
            }
        }

        /// <summary>An alliance mate's help on any job (HelpKinds); false if it is no longer running.</summary>
        internal bool ApplyHelp(string kind, string target, int seconds, DateTimeOffset now)
        {
            if (kind == HelpKinds.Build)
                return ApplyHelp(target, seconds, now);
            lock (_gate)
            {
                if (!ShortenJob(kind, target, seconds, now))
                    return false;

                // A help that finishes the job settles it at once; the queue is pushed either way.
                var changes = AdvanceTo(now);
                changes.Add(kind == HelpKinds.Heal ? ArmyChange(now) : ResearchChange(now));
                SendDelta(now, changes);
                return true;
            }
        }

        /// <summary>Heal and research help: the job ends sooner, never before the economy's current point. Caller holds the lock.</summary>
        private bool ShortenJob(string kind, string target, int seconds, DateTimeOffset now)
        {
            if (seconds <= 0)
                return false;
            var floor = _state.LastSimulatedAt < now ? _state.LastSimulatedAt : now;
            DateTimeOffset Cut(DateTimeOffset ends) => ends.AddSeconds(-seconds) < floor ? floor : ends.AddSeconds(-seconds);
            switch (kind)
            {
                case HelpKinds.Heal when _state.Healing is { } healing && healing.EndsAt > floor:
                    healing.EndsAt = Cut(healing.EndsAt);
                    break;
                case HelpKinds.Research when _state.ActiveResearch.FirstOrDefault(r => r.NodeId == target) is { } research && research.EndsAt > floor:
                    research.EndsAt = Cut(research.EndsAt);
                    break;
                default:
                    return false;
            }

            _state.IsDirty = true;
            return true;
        }

        /// <summary>An alliance mate's help: the construction ends <paramref name="seconds"/> sooner. False if it is no longer running.</summary>
        internal bool ApplyHelp(string buildingId, int seconds, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!ShortenConstruction(buildingId, seconds, now))
                    return false;

                var changes = AdvanceTo(now);
                var building = FindBuilding(buildingId);
                if (building != null && changes.Count == 0)
                    changes.Add(BuildingChange(building, now));
                SendDelta(now, changes);
                return true;
            }
        }

        /// <summary>Helps given while the player was offline; applied before the session advances on login.</summary>
        internal void ApplyOfflineHelps(IEnumerable<(string Kind, string Target, int Seconds)> helps, DateTimeOffset now)
        {
            lock (_gate)
            {
                foreach (var (kind, target, seconds) in helps)
                {
                    if (kind == HelpKinds.Build)
                        ShortenConstruction(target, seconds, now);
                    else
                        ShortenJob(kind, target, seconds, now);
                }
            }
        }

        private bool ShortenConstruction(string buildingId, int seconds, DateTimeOffset now)
        {
            var building = FindBuilding(buildingId);
            if (building?.ConstructionEndsAt == null || seconds <= 0)
                return false;

            // Never before the economy's current point: offline, the construction may already be due.
            var ends = building.ConstructionEndsAt.Value.AddSeconds(-seconds);
            var floor = _state.LastSimulatedAt < now ? _state.LastSimulatedAt : now;
            building.ConstructionEndsAt = ends < floor ? floor : ends;
            MarkChanged(building);
            return true;
        }
    }
}
