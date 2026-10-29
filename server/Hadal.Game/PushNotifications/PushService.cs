using System.Collections.Concurrent;
using System.Threading.Channels;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.PushNotifications
{
    /// <summary>
    /// Server push (docs/36 phase 2). Game systems call <see cref="Notify"/> from anywhere (under their own locks);
    /// requests are queued and delivered on a background loop. A push goes out only when the sender is configured,
    /// the player is offline (an online player sees the in-game alarm), the player's choices allow the category, it is
    /// not their quiet hours, and the category has not been pushed to them within the coalescing window.
    /// </summary>
    public sealed class PushService
    {
        private sealed record Request(string PlayerId, PushKind Kind, string Arg, DateTimeOffset At);

        private static readonly TimeSpan ProfileCacheLifetime = TimeSpan.FromMinutes(10);

        private readonly Channel<Request> _queue = Channel.CreateUnbounded<Request>(new UnboundedChannelOptions { SingleReader = true });
        private readonly ConcurrentDictionary<(string, NotificationCategory), DateTimeOffset> _lastSent = new();
        private readonly ConcurrentDictionary<string, (PushProfile Profile, DateTimeOffset LoadedAt)> _offline = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _deadTokens = new(StringComparer.Ordinal);
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private int _pumping;

        public PushService(IClock clock, ILogger logger)
        {
            _clock = clock;
            _logger = logger;
        }

        /// <summary>The transport; the no-op one until Firebase is configured (Hadal.Gateway Program).</summary>
        public IPushSender Sender { get; set; } = NoopPushSender.Instance;

        /// <summary>Tests pump the queue themselves (<see cref="PumpAsync"/>) instead of the background loop.</summary>
        public bool ManualPump { get; set; }

        /// <summary>True while the player has a live connection.</summary>
        public Func<string, bool> IsOnline { get; set; } = _ => false;

        /// <summary>A loaded session by public player id (online or resident), else null.</summary>
        public Func<string, PlayerSession?> SessionOf { get; set; } = _ => null;

        /// <summary>Loads an offline player's push profile from storage; null when unknown.</summary>
        public Func<string, CancellationToken, Task<PushProfile?>> LoadOffline { get; set; } = (_, _) => Task.FromResult<PushProfile?>(null);

        /// <summary>The player's display name for push texts (attacker, sender, who asked).</summary>
        public Func<string, string?> NameOf { get; set; } = _ => null;

        /// <summary>Pushes delivered, for the Overseer and the tests.</summary>
        public long SentCount => Interlocked.Read(ref _sent);
        private long _sent;

        /// <summary>
        /// Queues a push to one player. <paramref name="arg"/> fills the text (a name or a title). Cheap and non-blocking;
        /// nothing at all happens while push is not configured.
        /// </summary>
        public void Notify(string playerId, PushKind kind, string arg = "")
        {
            if (!Sender.Enabled || string.IsNullOrEmpty(playerId))
                return;
            _queue.Writer.TryWrite(new Request(playerId, kind, arg ?? string.Empty, _clock.UtcNow));
            if (!ManualPump && Interlocked.CompareExchange(ref _pumping, 1, 0) == 0)
                _ = Task.Run(RunAsync);
        }

        public void NotifyAll(IEnumerable<string> playerIds, PushKind kind, string arg = "")
        {
            foreach (var id in playerIds)
                Notify(id, kind, arg);
        }

        /// <summary>A player's state changed while loaded (new choices, new phone): forget the cached copy.</summary>
        public void Invalidate(string playerId) => _offline.TryRemove(playerId, out _);

        private async Task RunAsync()
        {
            try
            {
                await PumpAsync(CancellationToken.None);
            }
            finally
            {
                Interlocked.Exchange(ref _pumping, 0);
                // A request written after the last read but before the flag cleared starts a new loop.
                if (_queue.Reader.Count > 0 && !ManualPump && Interlocked.CompareExchange(ref _pumping, 1, 0) == 0)
                    _ = Task.Run(RunAsync);
            }
        }

        /// <summary>Delivers everything queued so far.</summary>
        public async Task PumpAsync(CancellationToken cancellationToken = default)
        {
            while (_queue.Reader.TryRead(out var request))
            {
                try
                {
                    await DeliverAsync(request, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogWarning(exception, "Push {Kind} to {PlayerId} failed.", request.Kind, request.PlayerId);
                }
            }
        }

        private async Task DeliverAsync(Request request, CancellationToken cancellationToken)
        {
            if (IsOnline(request.PlayerId))
                return;

            var category = PushKinds.Category(request.Kind);
            var profile = await ProfileAsync(request.PlayerId, cancellationToken);
            if (profile == null || profile.Tokens.Count == 0)
                return;
            var prefs = profile.Prefs;
            if (!NotificationRules.IsEnabled(prefs, category))
                return;

            var now = _clock.UtcNow;
            if (prefs != null)
            {
                var local = now.AddMinutes(prefs.UtcOffsetMinutes);
                if (NotificationRules.InQuietHours(prefs, local.Hour * 60 + local.Minute))
                    return;
            }

            var key = (request.PlayerId, category);
            if (_lastSent.TryGetValue(key, out var last) && now - last < NotificationRules.CoalesceWindow)
                return;
            _lastSent[key] = now;

            var (title, body) = PushTexts.For(prefs?.Language, request.Kind, request.Arg);
            foreach (var token in profile.Tokens)
            {
                if (_deadTokens.ContainsKey(token))
                    continue;
                var message = new PushMessage(token, title, body, NotificationRules.ChannelId(category), PushKinds.Screen(request.Kind),
                    "hadal_" + request.Kind.ToString().ToLowerInvariant());
                var result = await Sender.SendAsync(message, cancellationToken);
                if (result == PushSendResult.Sent)
                {
                    Interlocked.Increment(ref _sent);
                }
                else if (result == PushSendResult.InvalidToken)
                {
                    _deadTokens[token] = 0;
                    SessionOf(request.PlayerId)?.ForgetPushToken(token);
                    Invalidate(request.PlayerId);
                }
            }
        }

        private async Task<PushProfile?> ProfileAsync(string playerId, CancellationToken cancellationToken)
        {
            if (SessionOf(playerId) is { } session)
            {
                var live = session.TakePushProfile();
                _offline[playerId] = (live, _clock.UtcNow);
                return live;
            }

            if (_offline.TryGetValue(playerId, out var cached) && _clock.UtcNow - cached.LoadedAt < ProfileCacheLifetime)
                return cached.Profile;

            var loaded = await LoadOffline(playerId, cancellationToken);
            if (loaded != null)
                _offline[playerId] = (loaded, _clock.UtcNow);
            return loaded;
        }

        /// <summary>
        /// Names a chat message mentions: "@Name" for any of <paramref name="candidates"/> (id, name), whole word,
        /// case-insensitive. Returns the ids.
        /// </summary>
        public static List<string> Mentions(string text, IEnumerable<(string Id, string Name)> candidates)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(text) || text.IndexOf('@') < 0)
                return found;
            foreach (var (id, name) in candidates)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var at = 0;
                while ((at = text.IndexOf("@" + name, at, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    var end = at + name.Length + 1;
                    if (end >= text.Length || !char.IsLetterOrDigit(text[end]))
                    {
                        found.Add(id);
                        break;
                    }

                    at = end;
                }
            }

            return found;
        }
    }
}
