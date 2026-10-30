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

        /// <summary>The alliance's gift level and this member's gifts (docs/31 step 5).</summary>
        public AllianceGiftsDto AllianceGifts { get; private set; } = new AllianceGiftsDto();
        public int AllianceGiftsVersion { get; private set; }

        public void ApplyAllianceGifts(AllianceGiftsDto gifts)
        {
            AllianceGifts = gifts ?? new AllianceGiftsDto();
            AllianceGiftsVersion++;
        }

        /// <summary>The alliance research tree and this member's donations (docs/31 step 6).</summary>
        public AllianceResearchDto AllianceResearch { get; private set; } = new AllianceResearchDto();
        public int AllianceResearchVersion { get; private set; }

        public void ApplyAllianceResearch(AllianceResearchDto research)
        {
            AllianceResearch = research ?? new AllianceResearchDto();
            AllianceResearchVersion++;
        }

        private readonly Dictionary<string, AllianceRankingDto> _allianceRankings = new Dictionary<string, AllianceRankingDto>();
        public int AllianceRankingVersion { get; private set; }

        public void ApplyAllianceRanking(AllianceRankingDto ranking)
        {
            if (ranking == null || string.IsNullOrEmpty(ranking.Kind))
                return;
            _allianceRankings[ranking.Kind] = ranking;
            AllianceRankingVersion++;
        }

        /// <summary>The alliance's open and recent polls and time-slot votes (docs/29 §5), pushed on every change.</summary>
        public AlliancePollsDto AlliancePolls { get; private set; } = new AlliancePollsDto();
        public int AlliancePollsVersion { get; private set; }

        public void ApplyAlliancePolls(AlliancePollsDto polls)
        {
            AlliancePolls = polls ?? new AlliancePollsDto();
            AlliancePollsVersion++;
        }

        /// <summary>The viewer's officer duty ("war", "logistics", "diplomacy", "scout"), empty without one.</summary>
        public string MyRole()
        {
            foreach (var m in MyAlliance?.Members ?? System.Array.Empty<AllianceMemberDto>())
                if (m.PlayerId == PlayerId)
                    return m.Role ?? string.Empty;
            return string.Empty;
        }

        /// <summary>An alliance ranking ("power", "kills", "donations") as last fetched, or null.</summary>
        public AllianceRankingDto AllianceRanking(string kind) => _allianceRankings.TryGetValue(kind ?? string.Empty, out var r) ? r : null;

        /// <summary>The log of a kind ("log" or "battle"), or null before it was fetched.</summary>
        public AllianceLogDto AllianceLog(string kind) => _allianceLogs.TryGetValue(kind ?? string.Empty, out var log) ? log : null;
    }
}
