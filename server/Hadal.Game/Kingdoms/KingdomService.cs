using Microsoft.Extensions.Logging;

namespace Hadal.Game.Kingdoms
{
    public static class KingdomRules
    {
        /// <summary>New players stop when a kingdom has this many members (docs/39 §1).</summary>
        public const int DefaultCap = 1000;

        /// <summary>Default age limit for new kingdoms in days; 0 = none (a small game should not split by the calendar).</summary>
        public const int DefaultOpenDays = 0;

        /// <summary>"Active" on the admin page: seen this recently.</summary>
        public static readonly TimeSpan ActiveWindow = TimeSpan.FromDays(7);
    }

    /// <summary>
    /// Kingdoms and who belongs where (docs/39): new players join the newest open kingdom; a kingdom that reaches its
    /// cap or age limit turns full and the next one opens. Its own lock is a leaf: nothing is called while holding it.
    /// </summary>
    public sealed class KingdomService
    {
        private readonly object _gate = new();
        private readonly IKingdomStore _store;
        private readonly ILogger _logger;
        private readonly Dictionary<string, Kingdom> _kingdoms = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _memberOf = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _seen = new(StringComparer.Ordinal);

        public KingdomService(IKingdomStore store, ILogger logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await _store.InitializeAsync(cancellationToken);
            var (kingdoms, members) = await _store.LoadAsync(cancellationToken);
            lock (_gate)
            {
                foreach (var k in kingdoms)
                    _kingdoms[k.Id] = k;
                foreach (var m in members)
                    Join(m.PlayerId, m.KingdomId);
            }

            _logger.LogInformation("Kingdoms: {Kingdoms}, {Members} players.", kingdoms.Count, members.Count);
        }

        /// <summary>The player's kingdom id, or null when they have none yet.</summary>
        public string? KingdomOf(string playerId)
        {
            lock (_gate)
                return _memberOf.TryGetValue(playerId, out var id) ? id : null;
        }

        public Kingdom? Get(string kingdomId)
        {
            lock (_gate)
                return _kingdoms.TryGetValue(kingdomId, out var k) ? k : null;
        }

        public int NumberOf(string kingdomId)
        {
            lock (_gate)
                return _kingdoms.TryGetValue(kingdomId, out var k) ? k.Number : 0;
        }

        public IReadOnlyList<(Kingdom Kingdom, int Members, int Active)> List(DateTimeOffset now)
        {
            lock (_gate)
            {
                var active = _seen.Where(p => now - p.Value < KingdomRules.ActiveWindow && _memberOf.ContainsKey(p.Key))
                    .GroupBy(p => _memberOf[p.Key]).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
                return _kingdoms.Values.OrderBy(k => k.Number)
                    .Select(k => (k, _counts.TryGetValue(k.Id, out var n) ? n : 0, active.TryGetValue(k.Id, out var a) ? a : 0)).ToList();
            }
        }

        /// <summary>Notes a login, for the "active" count.</summary>
        public void Seen(string playerId, DateTimeOffset now)
        {
            lock (_gate)
                _seen[playerId] = now;
        }

        /// <summary>
        /// The player's kingdom, joining them to the newest open one when they have none (a new account). Opens the next
        /// kingdom when the newest has filled up. Returns the kingdom and, when one opened, the new kingdom.
        /// </summary>
        public async Task<(string KingdomId, Kingdom? Opened)> AssignAsync(string playerId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Kingdom? opened = null;
            Kingdom? filled = null;
            KingdomMember member;
            lock (_gate)
            {
                if (_memberOf.TryGetValue(playerId, out var known))
                    return (known, null);

                var target = _kingdoms.Values.Where(k => k.Status == KingdomStatus.Open).OrderByDescending(k => k.Number).FirstOrDefault();
                if (target != null && Reached(target, now))
                {
                    filled = target with { Status = KingdomStatus.Full };
                    _kingdoms[target.Id] = filled;
                    target = null;
                }

                if (target == null)
                {
                    opened = target = NextKingdom(now, filled ?? _kingdoms.Values.OrderByDescending(k => k.Number).FirstOrDefault());
                    _kingdoms[target.Id] = target;
                }

                member = new KingdomMember(playerId, target.Id, now);
                Join(playerId, target.Id);
            }

            if (filled != null)
                await _store.SaveKingdomAsync(filled, cancellationToken);
            if (opened != null)
            {
                await _store.SaveKingdomAsync(opened, cancellationToken);
                _logger.LogInformation("Kingdom #{Number} opened.", opened.Number);
            }

            await _store.SaveMemberAsync(member, cancellationToken);
            return (member.KingdomId, opened);
        }

        /// <summary>Admin: opens the next kingdom now (the newest open one turns full). Returns it.</summary>
        public async Task<Kingdom> OpenNextAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            Kingdom opened;
            var filled = new List<Kingdom>();
            lock (_gate)
            {
                foreach (var k in _kingdoms.Values.Where(k => k.Status == KingdomStatus.Open).ToList())
                    filled.Add(_kingdoms[k.Id] = k with { Status = KingdomStatus.Full });
                opened = NextKingdom(now, _kingdoms.Values.OrderByDescending(k => k.Number).FirstOrDefault());
                _kingdoms[opened.Id] = opened;
            }

            foreach (var k in filled)
                await _store.SaveKingdomAsync(k, cancellationToken);
            await _store.SaveKingdomAsync(opened, cancellationToken);
            _logger.LogInformation("Kingdom #{Number} opened by an admin.", opened.Number);
            return opened;
        }

        /// <summary>Admin: status, cap, age limit or name of one kingdom. Null when it does not exist or a value is invalid.</summary>
        public async Task<Kingdom?> UpdateAsync(string kingdomId, string? status, int? cap, int? openDays, string? name, CancellationToken cancellationToken)
        {
            Kingdom changed;
            lock (_gate)
            {
                if (!_kingdoms.TryGetValue(kingdomId, out var k) || (status != null && !KingdomStatus.IsValid(status)) || cap is < 1 || openDays is < 0)
                    return null;
                changed = k with
                {
                    Status = status ?? k.Status, PlayerCap = cap ?? k.PlayerCap, OpenDays = openDays ?? k.OpenDays,
                    Name = name == null ? k.Name : name.Trim()[..Math.Min(40, name.Trim().Length)]
                };
                _kingdoms[kingdomId] = changed;
            }

            await _store.SaveKingdomAsync(changed, cancellationToken);
            return changed;
        }

        /// <summary>A deleted player leaves their kingdom.</summary>
        public async Task ForgetAsync(string playerId, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_memberOf.Remove(playerId, out var id))
                    _counts[id]--;
                _seen.Remove(playerId);
            }

            await _store.DeleteMemberAsync(playerId, cancellationToken);
        }

        private bool Reached(Kingdom k, DateTimeOffset now) =>
            (_counts.TryGetValue(k.Id, out var n) ? n : 0) >= k.PlayerCap || (k.OpenDays > 0 && now - k.OpenedAt >= TimeSpan.FromDays(k.OpenDays));

        private Kingdom NextKingdom(DateTimeOffset now, Kingdom? previous)
        {
            var number = (_kingdoms.Count == 0 ? 0 : _kingdoms.Values.Max(k => k.Number)) + 1;
            return new Kingdom(Kingdom.IdOf(number), number, string.Empty, now, KingdomStatus.Open,
                previous?.PlayerCap ?? KingdomRules.DefaultCap, previous?.OpenDays ?? KingdomRules.DefaultOpenDays);
        }

        private void Join(string playerId, string kingdomId)
        {
            if (_memberOf.TryGetValue(playerId, out var old))
                _counts[old]--;
            _memberOf[playerId] = kingdomId;
            _counts[kingdomId] = (_counts.TryGetValue(kingdomId, out var n) ? n : 0) + 1;
        }
    }
}
