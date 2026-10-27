using System.Collections.Concurrent;

namespace Hadal.Game.Social
{
    /// <summary><see cref="JoinMode"/> is an <c>AllianceJoinMode</c> value.</summary>
    public sealed record AllianceRecord(string Id, string Name, string Tag, string Description, string LeaderId, DateTimeOffset CreatedAt, int JoinMode = 0);

    /// <summary><see cref="Rank"/> 1–5 (0 = stored before ranks existed), <see cref="Role"/> an officer duty tag.</summary>
    public sealed record MemberRecord(string PlayerId, string AllianceId, string Name, long Power, DateTimeOffset JoinedAt, int Rank = 0, string Role = "");

    /// <summary>One chat line. <see cref="Channel"/> is "world", "alliance:&lt;id&gt;" or "dm:&lt;id&gt;:&lt;id&gt;" (ids in order).</summary>
    public sealed record ChatRecord(long Id, string Channel, string SenderId, string SenderName, string AllianceTag, string Text, DateTimeOffset SentAt, bool IsSystem);

    /// <summary>Durable alliances, memberships and chat (docs/25). The hub keeps everything in memory and writes through.</summary>
    public interface ISocialStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task<(List<AllianceRecord> Alliances, List<MemberRecord> Members)> LoadAlliancesAsync(CancellationToken cancellationToken);

        /// <summary>The newest <paramref name="perChannel"/> lines of every channel, oldest first.</summary>
        Task<List<ChatRecord>> LoadRecentChatAsync(int perChannel, CancellationToken cancellationToken);

        Task SaveAllianceAsync(AllianceRecord alliance, CancellationToken cancellationToken);

        Task DeleteAllianceAsync(string allianceId, CancellationToken cancellationToken);

        Task SaveMemberAsync(MemberRecord member, CancellationToken cancellationToken);

        Task DeleteMemberAsync(string playerId, CancellationToken cancellationToken);

        Task AddChatAsync(ChatRecord message, CancellationToken cancellationToken);

        Task DeleteChatAsync(long messageId, CancellationToken cancellationToken);
    }

    public sealed class InMemorySocialStore : ISocialStore
    {
        private readonly ConcurrentDictionary<string, AllianceRecord> _alliances = new();
        private readonly ConcurrentDictionary<string, MemberRecord> _members = new();
        private readonly ConcurrentDictionary<long, ChatRecord> _chat = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<(List<AllianceRecord> Alliances, List<MemberRecord> Members)> LoadAlliancesAsync(CancellationToken cancellationToken) =>
            Task.FromResult((_alliances.Values.ToList(), _members.Values.ToList()));

        public Task<List<ChatRecord>> LoadRecentChatAsync(int perChannel, CancellationToken cancellationToken) =>
            Task.FromResult(_chat.Values.GroupBy(c => c.Channel).SelectMany(g => g.OrderBy(c => c.Id).TakeLast(perChannel)).OrderBy(c => c.Id).ToList());

        public Task SaveAllianceAsync(AllianceRecord alliance, CancellationToken cancellationToken)
        {
            _alliances[alliance.Id] = alliance;
            return Task.CompletedTask;
        }

        public Task DeleteAllianceAsync(string allianceId, CancellationToken cancellationToken)
        {
            _alliances.TryRemove(allianceId, out _);
            foreach (var member in _members.Values.Where(m => m.AllianceId == allianceId))
                _members.TryRemove(member.PlayerId, out _);
            return Task.CompletedTask;
        }

        public Task SaveMemberAsync(MemberRecord member, CancellationToken cancellationToken)
        {
            _members[member.PlayerId] = member;
            return Task.CompletedTask;
        }

        public Task DeleteMemberAsync(string playerId, CancellationToken cancellationToken)
        {
            _members.TryRemove(playerId, out _);
            return Task.CompletedTask;
        }

        public Task AddChatAsync(ChatRecord message, CancellationToken cancellationToken)
        {
            _chat[message.Id] = message;
            return Task.CompletedTask;
        }

        public Task DeleteChatAsync(long messageId, CancellationToken cancellationToken)
        {
            _chat.TryRemove(messageId, out _);
            return Task.CompletedTask;
        }
    }
}
