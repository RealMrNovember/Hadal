using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using Hadal.Core.Network;

namespace Hadal.Core.StateSync
{
    /// <summary>PvP state (docs/30): shield and incoming attacks, war log, alliance war list, strategic map.</summary>
    public sealed partial class ClientStateView
    {
        private readonly Queue<PvpReportDto> _newWarReports = new();

        public PvpStatusDto Pvp { get; private set; } = new PvpStatusDto();
        public int PvpVersion { get; private set; }

        public WarLogDto WarLog { get; private set; } = new WarLogDto();
        public int WarLogVersion { get; private set; }

        public WarListDto WarList { get; private set; } = new WarListDto();
        public int WarListVersion { get; private set; }

        public TerritoryDto Territory { get; private set; }

        public int TerritoryVersion { get; private set; }

        /// <summary>Every kingdom (docs/39), as last asked for.</summary>
        public KingdomListDto KingdomList { get; private set; } = new KingdomListDto();
        public int KingdomListVersion { get; private set; }

        /// <summary>Our kingdom's number: from the profile, else the map we look at; 1 before either arrived.</summary>
        public int MyKingdom => MyProfile?.Kingdom > 0 ? MyProfile.Kingdom
            : WorldView != null ? HADAL.Shared.Simulation.KingdomNaming.NumberOf(WorldView.WorldId) : 1;

        public void ApplyKingdoms(KingdomListDto list)
        {
            KingdomList = list ?? new KingdomListDto();
            KingdomListVersion++;
        }

        public void ApplyPvp(PvpStatusDto pvp)
        {
            Pvp = pvp ?? new PvpStatusDto();
            if (Pvp.ServerNowMs > 0)
                ServerClockOffsetMs = Pvp.ServerNowMs - System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PvpVersion++;
        }

        /// <summary>Server time the current login's snapshot arrived: battles from then on are news, older ones were missed.</summary>
        public long LiveSinceMs { get; private set; }

        public void MarkLiveSince() => LiveSinceMs = ServerNowMs;

        public void ApplyWarLog(WarLogDto log)
        {
            var known = new HashSet<string>();
            foreach (var r in WarLog.Reports ?? System.Array.Empty<PvpReportDto>())
                known.Add(r.Id + r.Kind + r.AtMs);
            WarLog = log ?? new WarLogDto();
            // A report fought after this login is news (the result popup), even when it is the player's very first one;
            // what the login brought is history (PvpCenter sums up what was missed).
            foreach (var r in WarLog.Reports ?? System.Array.Empty<PvpReportDto>())
                if (!known.Contains(r.Id + r.Kind + r.AtMs) && (LiveSinceMs > 0 ? r.AtMs >= LiveSinceMs : WarLogVersion > 0))
                    _newWarReports.Enqueue(r);
            WarLogVersion++;
        }

        public bool TryTakeWarReport(out PvpReportDto report) => _newWarReports.TryDequeue(out report);

        public void ApplyWarList(WarListDto list)
        {
            WarList = list ?? new WarListDto();
            WarListVersion++;
        }

        public void ApplyTerritory(TerritoryDto territory)
        {
            Territory = territory;
            TerritoryVersion++;
        }
    }

    /// <summary>Routes the PvP entities into the view.</summary>
    public static class PvpStateMutator
    {
        public static bool TryApply(ClientStateView view, EntityChangeDto change, NetworkSerializationLayer serialization)
        {
            var id = change.EntityId;
            if (id == EntityIds.Pvp)
            {
                view.ApplyPvp(Read<PvpStatusDto>(change, serialization) ?? new PvpStatusDto());
                return true;
            }

            if (id == EntityIds.WarLog)
            {
                view.ApplyWarLog(Read<WarLogDto>(change, serialization) ?? new WarLogDto());
                return true;
            }

            if (id == EntityIds.AllianceWar)
            {
                view.ApplyWarList(Read<WarListDto>(change, serialization) ?? new WarListDto());
                return true;
            }

            if (id == EntityIds.Kingdoms)
            {
                view.ApplyKingdoms(Read<KingdomListDto>(change, serialization) ?? new KingdomListDto());
                return true;
            }

            if (id == EntityIds.Territory)
            {
                if (Read<TerritoryDto>(change, serialization) is { } territory)
                    view.ApplyTerritory(territory);
                return true;
            }

            return false;
        }

        private static T Read<T>(EntityChangeDto change, NetworkSerializationLayer serialization) where T : class =>
            change.Payload != null && change.Payload.Length > 0 && serialization.TryDeserialize(change.Payload, out T value) ? value : null;
    }
}
