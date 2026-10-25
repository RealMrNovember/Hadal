using System.Text.Json;

namespace Hadal.Game.Persistence
{
    /// <summary>
    /// Research and building-function state (docs/24) stored as one JSON document per player
    /// (<c>hadal.players.extras</c>), so new systems do not need a table each.
    /// </summary>
    public static class PlayerExtras
    {
        private sealed record ActiveDoc(string NodeId, int TargetLevel, DateTimeOffset EndsAt, int Seconds);

        private sealed record BuildingDoc(
            DateTimeOffset? BoostEndsAt, string? MissionId, DateTimeOffset? MissionEndsAt, int MissionSeconds, int MissionSeed,
            DateTimeOffset? OverdriveEndsAt, DateTimeOffset? OverdriveReadyAt);

        private sealed record Doc(Dictionary<string, int>? Research, List<ActiveDoc>? Active, Dictionary<string, BuildingDoc>? Buildings);

        public static string Write(PlayerState state)
        {
            var buildings = new Dictionary<string, BuildingDoc>();
            foreach (var b in state.Buildings)
            {
                if (b.BoostEndsAt == null && b.MissionEndsAt == null && b.OverdriveEndsAt == null && b.OverdriveReadyAt == null)
                    continue;
                buildings[b.BuildingId] = new BuildingDoc(b.BoostEndsAt, b.MissionId, b.MissionEndsAt, b.MissionSeconds, b.MissionSeed,
                    b.OverdriveEndsAt, b.OverdriveReadyAt);
            }

            return JsonSerializer.Serialize(new Doc(
                new Dictionary<string, int>(state.ResearchLevels),
                state.ActiveResearch.Select(a => new ActiveDoc(a.NodeId, a.TargetLevel, a.EndsAt, a.Seconds)).ToList(),
                buildings));
        }

        /// <summary>Applies a stored document to a freshly loaded player (after its buildings).</summary>
        public static void Read(string? json, PlayerState state)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            Doc? doc;
            try
            {
                doc = JsonSerializer.Deserialize<Doc>(json);
            }
            catch (JsonException)
            {
                return; // a damaged document must not lock the player out
            }

            if (doc == null)
                return;

            foreach (var pair in doc.Research ?? new Dictionary<string, int>())
                state.ResearchLevels[pair.Key] = pair.Value;
            foreach (var a in doc.Active ?? new List<ActiveDoc>())
                state.ActiveResearch.Add(new ActiveResearch { NodeId = a.NodeId, TargetLevel = a.TargetLevel, EndsAt = a.EndsAt, Seconds = a.Seconds });

            foreach (var b in state.Buildings)
            {
                if (doc.Buildings == null || !doc.Buildings.TryGetValue(b.BuildingId, out var d))
                    continue;
                b.BoostEndsAt = d.BoostEndsAt;
                b.MissionId = d.MissionId ?? string.Empty;
                b.MissionEndsAt = d.MissionEndsAt;
                b.MissionSeconds = d.MissionSeconds;
                b.MissionSeed = d.MissionSeed;
                b.OverdriveEndsAt = d.OverdriveEndsAt;
                b.OverdriveReadyAt = d.OverdriveReadyAt;
            }
        }
    }
}
