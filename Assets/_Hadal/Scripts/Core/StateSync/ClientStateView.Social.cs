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

        /// <summary>Bumps on any alliance change (membership, browse results, help requests).</summary>
        public int AllianceVersion { get; private set; }

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
            AllianceVersion++;
        }
    }
}
