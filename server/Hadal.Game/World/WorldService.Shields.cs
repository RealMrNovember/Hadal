using HADAL.Shared.Simulation;

namespace Hadal.Game.World
{
    /// <summary>
    /// Every base's shield as the map shows it (docs/30 §3): read live from a loaded session, remembered (and stored with
    /// the fairness memory, "s|player") for players who are not loaded, so the dome and the refusal work for offline
    /// bases too.
    /// </summary>
    public sealed partial class WorldService
    {
        private readonly Dictionary<string, DateTimeOffset> _shields = new(StringComparer.Ordinal);

        /// <summary>Remembers a player's shield end. Caller holds the world lock.</summary>
        private void NoteShield(string playerId, DateTimeOffset? until)
        {
            if (until is { } end)
            {
                if (_shields.TryGetValue(playerId, out var known) && known == end)
                    return;
                _shields[playerId] = end;
                SaveFairness($"s|{playerId}", new List<DateTimeOffset> { end });
            }
            else if (_shields.Remove(playerId))
            {
                SaveFairness($"s|{playerId}", null);
            }
        }

        /// <summary>The shield's end if one is up at <paramref name="now"/>. Caller holds the world lock.</summary>
        private DateTimeOffset? ShieldOf(string playerId, DateTimeOffset now)
        {
            DateTimeOffset? until = SessionOf(playerId) is { } live ? live.ShieldUntil
                : _shields.TryGetValue(playerId, out var known) ? known : null;
            return until > now ? until : null;
        }

        /// <summary>The shield up now is the recovery shield (docs/33 §2). Caller holds the world lock.</summary>
        private bool IsRecoveryShield(string playerId, DateTimeOffset now) =>
            ShieldOf(playerId, now) is { } until && _recovered.TryGetValue(playerId, out var at) && until == at + PvpRules.RecoveryShield;
    }
}
