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

        public void ApplyPvp(PvpStatusDto pvp)
        {
            Pvp = pvp ?? new PvpStatusDto();
            if (Pvp.ServerNowMs > 0)
                ServerClockOffsetMs = Pvp.ServerNowMs - System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PvpVersion++;
        }

        public void ApplyWarLog(WarLogDto log)
        {
            var known = new HashSet<string>();
            foreach (var r in WarLog.Reports ?? System.Array.Empty<PvpReportDto>())
                known.Add(r.Id + r.Kind + r.AtMs);
            WarLog = log ?? new WarLogDto();
            // Reports that arrive after the first one are news (a toast); the first load is history.
            if (WarLogVersion > 0)
                foreach (var r in WarLog.Reports ?? System.Array.Empty<PvpReportDto>())
                    if (!known.Contains(r.Id + r.Kind + r.AtMs))
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
