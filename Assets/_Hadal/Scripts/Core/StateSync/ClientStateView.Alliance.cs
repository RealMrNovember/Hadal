using System.Collections.Generic;
using HADAL.Shared.DTOs;

namespace Hadal.Core.StateSync
{
    /// <summary>The alliance screen's extra pages (docs/31): the alliance log and the battle log, as last fetched.</summary>
    public sealed partial class ClientStateView
    {
        private readonly Dictionary<string, AllianceLogDto> _allianceLogs = new Dictionary<string, AllianceLogDto>();
        public int AllianceLogVersion { get; private set; }

        public void ApplyAllianceLog(AllianceLogDto log)
        {
            if (log == null || string.IsNullOrEmpty(log.Kind))
                return;
            _allianceLogs[log.Kind] = log;
            AllianceLogVersion++;
        }

        /// <summary>Alliance coins, today's help coins and this week's shop (docs/31 step 4).</summary>
        public AllianceCoinsDto AllianceCoins { get; private set; } = new AllianceCoinsDto();
        public int AllianceCoinsVersion { get; private set; }
        public float AllianceCoinsAt { get; private set; }

        public void ApplyAllianceCoins(AllianceCoinsDto coins)
        {
            AllianceCoins = coins ?? new AllianceCoinsDto();
            AllianceCoinsAt = UnityEngine.Time.realtimeSinceStartup;
            AllianceCoinsVersion++;
        }

        /// <summary>The log of a kind ("log" or "battle"), or null before it was fetched.</summary>
        public AllianceLogDto AllianceLog(string kind) => _allianceLogs.TryGetValue(kind ?? string.Empty, out var log) ? log : null;
    }
}
