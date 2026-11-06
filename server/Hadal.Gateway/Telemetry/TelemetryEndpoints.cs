using Hadal.Game;
using Hadal.Game.Telemetry;
using Hadal.Gateway.Auth;
using HADAL.Shared.Telemetry;

namespace Hadal.Gateway
{
    /// <summary>
    /// Client crash/error reports from signed-in players. Batches are sanitized here:
    /// capped size, truncated payloads, only the two known levels. Never throws to the caller —
    /// telemetry must not break gameplay.
    /// </summary>
    public static class TelemetryEndpoints
    {
        public const string Policy = "telemetry";
        private const int MaxEntries = 20;
        private const int MaxMessage = 300;
        private const int MaxStack = 4000;
        private const int MaxMeta = 64;

        public static void MapHadalTelemetry(this WebApplication app)
        {
            app.MapPost(TelemetryRoutes.ClientLog, async (HttpContext http, ClientLogBatch? batch, TokenService tokens,
                IClientLogStore store, IClock clock, CancellationToken ct) =>
            {
                var account = await AuthEndpoints.AuthenticateAsync(http, tokens);
                if (account == null)
                    return Results.Unauthorized();

                var entries = Sanitize(batch, account.Value.ToString("N"), clock.UtcNow);
                if (entries.Count > 0)
                {
                    try
                    {
                        await store.RecordAsync(entries, ct);
                    }
                    catch (Exception)
                    {
                        // Telemetry is best-effort: the client gets 202 either way.
                        return Results.Accepted(value: new ClientLogAck { accepted = 0 });
                    }
                }

                return Results.Accepted(value: new ClientLogAck { accepted = entries.Count });
            }).RequireRateLimiting(Policy);
        }

        internal static List<ClientLogRecord> Sanitize(ClientLogBatch? batch, string playerId, DateTimeOffset now)
        {
            var list = new List<ClientLogRecord>();
            if (batch?.entries == null)
                return list;

            var version = Clip(batch.clientVersion, MaxMeta);
            var platform = Clip(batch.platform, MaxMeta);
            var device = Clip(batch.device, MaxMeta);

            foreach (var entry in batch.entries.Take(MaxEntries))
            {
                var level = entry.level?.Trim().ToLowerInvariant();
                if (level != ClientLogLevels.Error && level != ClientLogLevels.Exception)
                    continue;

                var message = entry.message?.Trim() ?? string.Empty;
                if (message.Length == 0)
                    continue;

                var at = entry.at > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(entry.at)
                    : now;
                if (at > now.AddDays(1) || at < now.AddDays(-365))
                    at = now;

                list.Add(new ClientLogRecord(
                    playerId, level, Clip(message, MaxMessage), Clip(entry.stack, MaxStack), at, version, platform, device));
            }

            return list;
        }

        private static string Clip(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= max ? value : value[..max];
        }
    }
}
