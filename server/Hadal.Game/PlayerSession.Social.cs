using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>A running construction as an alliance help request sees it.</summary>
    public readonly record struct ConstructionInfo(string BuildingId, string DefinitionId, int TargetLevel, int TotalSeconds, DateTimeOffset EndsAt);

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

        public long Power
        {
            get { lock (_gate) return ComputePower(); }
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
        internal void ApplyOfflineHelps(IEnumerable<(string BuildingId, int Seconds)> helps, DateTimeOffset now)
        {
            lock (_gate)
            {
                foreach (var (buildingId, seconds) in helps)
                    ShortenConstruction(buildingId, seconds, now);
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
