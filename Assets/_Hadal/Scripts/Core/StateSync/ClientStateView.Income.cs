using System.Collections.Generic;
using HADAL.Shared.DTOs;

namespace Hadal.Core.StateSync
{
    /// <summary>Gathering marches and the Depth Map campaign (docs/rehber 08, 09).</summary>
    public sealed partial class ClientStateView
    {
        private readonly Queue<DiveReportDto> _diveReports = new();

        /// <summary>Null until the server sent marches (older servers never do).</summary>
        public MarchListDto Marches { get; private set; }

        /// <summary>Troops at home and the training queues; null until the server sends them.</summary>
        public ArmyDto Army { get; private set; }

        public int ArmyVersion { get; private set; }

        public void ApplyArmy(ArmyDto army)
        {
            Army = army;
            ArmyVersion++;
        }

        /// <summary>Server clock minus this device's clock, ms; march timestamps are server time.</summary>
        public long ServerClockOffsetMs { get; private set; }

        public ExpeditionStateDto Expedition { get; private set; }

        /// <summary>Bumps on any march or campaign change.</summary>
        public int IncomeVersion { get; private set; }

        /// <summary>Now on the server's clock, Unix ms.</summary>
        public long ServerNowMs => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ServerClockOffsetMs;

        public void ApplyMarches(MarchListDto marches)
        {
            Marches = marches;
            if (marches.ServerNowMs > 0)
                ServerClockOffsetMs = marches.ServerNowMs - System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            IncomeVersion++;
        }

        public void ApplyExpedition(ExpeditionStateDto expedition)
        {
            Expedition = expedition;
            IncomeVersion++;
        }

        public void AddDiveReport(DiveReportDto report)
        {
            if (report != null)
                _diveReports.Enqueue(report);
        }

        public bool TryTakeDiveReport(out DiveReportDto report) => _diveReports.TryDequeue(out report);

        private void ResetIncome()
        {
            Marches = null;
            Army = null;
            Expedition = null;
            _diveReports.Clear();
            IncomeVersion++;
        }
    }
}
