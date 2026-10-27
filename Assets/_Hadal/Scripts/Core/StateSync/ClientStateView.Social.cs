using System.Collections.Generic;
using HADAL.Shared.DTOs;

namespace Hadal.Core.StateSync
{
    /// <summary>An owned hero as the client shows it (docs/25).</summary>
    public readonly struct HeroView
    {
        public readonly string HeroId;
        public readonly int Level;
        public readonly int Stars;
        public readonly int Shards;

        /// <summary>Command crew slot, -1 when off the crew.</summary>
        public readonly int Slot;

        public HeroView(string heroId, int level, int stars, int shards, int slot)
        {
            HeroId = heroId;
            Level = level;
            Stars = stars;
            Shards = shards;
            Slot = slot;
        }
    }

    /// <summary>Heroes, chat and alliance state replicated by the server (docs/25).</summary>
    public sealed partial class ClientStateView
    {
        public const int ChatKeep = 200;

        private readonly List<HeroView> _heroes = new();
        private readonly Queue<GachaResultDto> _gachaResults = new();
        private readonly List<ChatMessageDto> _chat = new();

        /// <summary>The snapshot's player id: who "me" is in chat and alliances.</summary>
        public string PlayerId { get; set; } = string.Empty;

        public IReadOnlyList<HeroView> Heroes => _heroes;

        /// <summary>False until the server sent a roster (older servers never do).</summary>
        public bool HasHeroes { get; private set; }

        public int SincePity { get; private set; }

        /// <summary>Time.realtimeSinceStartup when the free pull is ready; -1 when there is none.</summary>
        public float FreePullReadyAt { get; private set; } = -1f;

        /// <summary>Chat lines of every readable channel, oldest first.</summary>
        public IReadOnlyList<ChatMessageDto> Chat => _chat;

        /// <summary>Bumps whenever <see cref="Chat"/> changes, so views redraw only then.</summary>
        public int ChatVersion { get; private set; }

        /// <summary>Lines that arrived while the chat was closed.</summary>
        public int UnreadChat { get; set; }

        /// <summary>Null when the player is in no alliance.</summary>
        public AllianceDto MyAlliance { get; private set; }

        public AllianceSummaryDto[] AllianceBrowse { get; private set; } = System.Array.Empty<AllianceSummaryDto>();

        public HelpRequestDto[] HelpRequests { get; private set; } = System.Array.Empty<HelpRequestDto>();

        /// <summary>Invitations and applications while the player has no alliance (docs/rehber 10 §2).</summary>
        public AllianceInviteListDto AllianceInvites { get; private set; } = new();

        public void ApplyAllianceInvites(AllianceInviteListDto invites)
        {
            AllianceInvites = invites ?? new AllianceInviteListDto();
            AllianceVersion++;
        }

        /// <summary>The viewer's rank in their alliance, 0 without one.</summary>
        public int MyRank()
        {
            if (MyAlliance == null)
                return 0;
            foreach (var m in MyAlliance.Members ?? System.Array.Empty<AllianceMemberDto>())
                if (m.PlayerId == PlayerId)
                    return m.Rank > 0 ? m.Rank : MyAlliance.LeaderId == PlayerId ? 5 : 2;
            return 0;
        }

        /// <summary>Bumps on any alliance change (membership, browse results, help requests).</summary>
        public int AllianceVersion { get; private set; }

        // --- Quests and bag (docs/27) ---------------------------------------------------------

        private readonly Queue<RewardReportDto> _rewardReports = new();

        /// <summary>Null until the server sent quest state (older servers never do).</summary>
        public QuestStateDto Quests { get; private set; }

        /// <summary>Time.realtimeSinceStartup of the next daily reset.</summary>
        public float DailyResetAt { get; private set; }

        public IReadOnlyDictionary<string, int> Items => _items;
        public IReadOnlyDictionary<string, int> HeroCards => _heroCards;
        private readonly Dictionary<string, int> _items = new(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> _heroCards = new(System.StringComparer.Ordinal);

        /// <summary>Bumps on any quest or bag change.</summary>
        public int QuestVersion { get; private set; }

        /// <summary>The last world map area the server sent (docs/28); null until the map was opened.</summary>
        public WorldViewDto WorldView { get; private set; }

        public int WorldVersion { get; private set; }

        /// <summary>The player's own profile (avatar choice).</summary>
        public PlayerProfileDto MyProfile { get; private set; } = new();

        /// <summary>The last profile card the player asked for (tapping an avatar or a base).</summary>
        public PlayerProfileDto ProfileCard { get; private set; }

        /// <summary>Bumps when either profile changes.</summary>
        public int ProfileVersion { get; private set; }

        public void ApplyMyProfile(PlayerProfileDto profile)
        {
            if (profile == null)
                return;
            MyProfile = profile;
            ProfileVersion++;
        }

        public void ApplyProfileCard(PlayerProfileDto profile)
        {
            ProfileCard = profile;
            ProfileVersion++;
        }

        public void ApplyWorldView(WorldViewDto view)
        {
            WorldView = view;
            WorldVersion++;
        }

        public bool TryTakeRewardReport(out RewardReportDto report) => _rewardReports.TryDequeue(out report);

        /// <summary>VIP standing (VipRules).</summary>
        public VipStateDto Vip { get; private set; } = new VipStateDto();
        public int VipVersion { get; private set; }
        public float VipReceivedAt { get; private set; }

        public void ApplyVip(VipStateDto vip)
        {
            Vip = vip ?? new VipStateDto();
            VipReceivedAt = UnityEngine.Time.realtimeSinceStartup;
            VipVersion++;
        }

        /// <summary>Events the player can see, with their progress (docs/29).</summary>
        public EventListDto Events { get; private set; } = new EventListDto();
        public int EventsVersion { get; private set; }
        private readonly Dictionary<string, EventRankingDto> _eventRankings = new Dictionary<string, EventRankingDto>();
        public int EventRankingVersion { get; private set; }

        public void ApplyEvents(EventListDto events)
        {
            Events = events ?? new EventListDto();
            if (Events.ServerNowMs > 0)
                ServerClockOffsetMs = Events.ServerNowMs - System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EventsVersion++;
        }

        public void ApplyEventRanking(EventRankingDto ranking)
        {
            if (ranking == null || string.IsNullOrEmpty(ranking.EventId))
                return;
            _eventRankings[ranking.EventId] = ranking;
            EventRankingVersion++;
        }

        public EventRankingDto EventRanking(string eventId) => _eventRankings.TryGetValue(eventId ?? string.Empty, out var r) ? r : null;

        private readonly Queue<BattleReportDto> _battleReports = new();

        public bool TryTakeBattleReport(out BattleReportDto report) => _battleReports.TryDequeue(out report);

        public void AddBattleReport(BattleReportDto report)
        {
            if (report != null)
                _battleReports.Enqueue(report);
        }

        public void AddRewardReport(RewardReportDto report)
        {
            if (report != null)
                _rewardReports.Enqueue(report);
        }

        public void ApplyQuests(QuestStateDto quests)
        {
            Quests = quests;
            DailyResetAt = UnityEngine.Time.realtimeSinceStartup + quests.DailyResetMs / 1000f;
            QuestVersion++;
        }

        public void ApplyInventory(InventoryDto inventory)
        {
            _items.Clear();
            foreach (var i in inventory.Items ?? System.Array.Empty<ItemCountDto>())
                _items[i.Id] = i.Count;
            _heroCards.Clear();
            foreach (var c in inventory.HeroCards ?? System.Array.Empty<ItemCountDto>())
                _heroCards[c.Id] = c.Count;
            QuestVersion++;
        }

        public bool TryTakeGachaResult(out GachaResultDto result) => _gachaResults.TryDequeue(out result);

        public bool TryGetHero(string heroId, out HeroView hero)
        {
            foreach (var h in _heroes)
            {
                if (h.HeroId == heroId)
                {
                    hero = h;
                    return true;
                }
            }

            hero = default;
            return false;
        }

        public void ApplyHeroes(HeroRosterDto roster)
        {
            _heroes.Clear();
            foreach (var h in roster.Heroes ?? System.Array.Empty<HeroStateDto>())
                _heroes.Add(new HeroView(h.DefinitionId, h.Level, h.Stars, h.Shards, h.CrewSlot));
            SincePity = roster.SincePity;
            FreePullReadyAt = roster.FreePullRemainingMs < 0 ? -1f : UnityEngine.Time.realtimeSinceStartup + roster.FreePullRemainingMs / 1000f;
            HasHeroes = true;
        }

        public void AddGachaResult(GachaResultDto result)
        {
            if (result != null)
                _gachaResults.Enqueue(result);
        }

        public void ApplyChatHistory(ChatHistoryDto history)
        {
            _chat.Clear();
            _chat.AddRange(history.Messages ?? System.Array.Empty<ChatMessageDto>());
            ChatVersion++;
        }

        public void AddChat(ChatMessageDto message)
        {
            if (message == null || _chat.Exists(m => m.Id == message.Id))
                return;
            _chat.Add(message);
            if (_chat.Count > ChatKeep)
                _chat.RemoveRange(0, _chat.Count - ChatKeep);
            if (message.SenderId != PlayerId)
                UnreadChat++;
            ChatVersion++;
        }

        public void ApplyMyAlliance(AllianceDto alliance)
        {
            // Leaving drops the alliance channel's lines too.
            if (alliance == null && MyAlliance != null)
            {
                _chat.RemoveAll(m => m.Channel == (int)HADAL.Shared.Simulation.ChatChannel.Alliance);
                ChatVersion++;
            }

            MyAlliance = alliance;
            AllianceVersion++;
        }

        public void ApplyAllianceBrowse(AllianceListDto list)
        {
            AllianceBrowse = list.Alliances ?? System.Array.Empty<AllianceSummaryDto>();
            AllianceVersion++;
        }

        public void ApplyHelp(HelpListDto help)
        {
            HelpRequests = help.Requests ?? System.Array.Empty<HelpRequestDto>();
            AllianceVersion++;
        }

        private void ResetSocial()
        {
            ResetIncome();
            Quests = null;
            _items.Clear();
            _heroCards.Clear();
            _rewardReports.Clear();
            QuestVersion++;
            PlayerId = string.Empty;
            _heroes.Clear();
            _gachaResults.Clear();
            _chat.Clear();
            HasHeroes = false;
            SincePity = 0;
            FreePullReadyAt = -1f;
            UnreadChat = 0;
            ChatVersion++;
            MyAlliance = null;
            AllianceBrowse = System.Array.Empty<AllianceSummaryDto>();
            HelpRequests = System.Array.Empty<HelpRequestDto>();
            AllianceInvites = new AllianceInviteListDto();
            AllianceVersion++;
        }
    }
}
