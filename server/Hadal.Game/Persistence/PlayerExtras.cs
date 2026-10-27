using System.Text.Json;
using HADAL.Shared.Enums;

namespace Hadal.Game.Persistence
{
    /// <summary>
    /// Research, building-function (docs/24) and hero (docs/25) state stored as one JSON document per player
    /// (<c>hadal.players.extras</c>), so new systems do not need a table each.
    /// </summary>
    public static class PlayerExtras
    {
        private sealed record ActiveDoc(string NodeId, int TargetLevel, DateTimeOffset EndsAt, int Seconds);

        private sealed record BuildingDoc(
            DateTimeOffset? BoostEndsAt, string? MissionId, DateTimeOffset? MissionEndsAt, int MissionSeconds, int MissionSeed,
            DateTimeOffset? OverdriveEndsAt, DateTimeOffset? OverdriveReadyAt);

        private sealed record HeroDoc(string HeroId, int Level, int Stars, int Shards, int Slot);

        private sealed record Doc(
            Dictionary<string, int>? Research, List<ActiveDoc>? Active, Dictionary<string, BuildingDoc>? Buildings,
            List<HeroDoc>? Heroes = null, int SincePity = 0, DateTimeOffset? FreePullAt = null, QuestDoc? Quests = null,
            List<MarchDoc>? Marches = null, int ExpeditionCleared = 0, string? Avatar = null);

        private sealed record MarchDoc(
            string Id, string NodeId, string NodeType, int NodeLevel, int X, int Y, DateTimeOffset StartedAt, DateTimeOffset ArrivesAt,
            DateTimeOffset GatheredAt, DateTimeOffset HomeAt, long Reserved, Dictionary<string, double>? Rates, Dictionary<string, long>? Cargo,
            string? HeroId = null, int Crew = 0, int FromX = 0, int FromY = 0);

        private sealed record QuestDoc(
            int ChapterIndex, Dictionary<string, long>? Counters, Dictionary<string, long>? Daily, int DailyDay,
            List<string>? ClaimedDaily, int DailyPoints, List<int>? ClaimedChests, Dictionary<string, int>? Items, Dictionary<string, int>? HeroCards);

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
                buildings,
                state.Heroes.Select(h => new HeroDoc(h.HeroId, h.Level, h.Stars, h.Shards, h.Slot)).ToList(),
                state.SincePity,
                state.FreePullAt,
                new QuestDoc(state.ChapterIndex, new Dictionary<string, long>(state.QuestCounters), new Dictionary<string, long>(state.DailyCounters),
                    state.DailyDay, state.ClaimedDaily.ToList(), state.DailyPoints, state.ClaimedChests.ToList(),
                    new Dictionary<string, int>(state.Items), new Dictionary<string, int>(state.HeroCards)),
                state.Marches.Select(m => new MarchDoc(m.Id, m.NodeId, m.NodeType, m.NodeLevel, m.X, m.Y, m.StartedAt, m.ArrivesAt,
                    m.GatheredAt, m.HomeAt, m.Reserved,
                    m.Rates.ToDictionary(p => p.Key.ToString(), p => p.Value), m.Cargo.ToDictionary(p => p.Key.ToString(), p => p.Value),
                    m.HeroId, m.Crew, m.FromX, m.FromY)).ToList(),
                state.ExpeditionCleared,
                state.AvatarId));
        }

        private static Dictionary<ResourceType, T> Resources<T>(Dictionary<string, T>? map)
        {
            var result = new Dictionary<ResourceType, T>();
            foreach (var pair in map ?? new Dictionary<string, T>())
                if (Enum.TryParse(pair.Key, out ResourceType resource))
                    result[resource] = pair.Value;
            return result;
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

            foreach (var h in doc.Heroes ?? new List<HeroDoc>())
                state.Heroes.Add(new OwnedHeroState { HeroId = h.HeroId, Level = h.Level, Stars = h.Stars, Shards = h.Shards, Slot = h.Slot });
            state.SincePity = doc.SincePity;
            state.FreePullAt = doc.FreePullAt;

            if (doc.Quests is { } q)
            {
                state.ChapterIndex = q.ChapterIndex;
                foreach (var pair in q.Counters ?? new Dictionary<string, long>())
                    state.QuestCounters[pair.Key] = pair.Value;
                foreach (var pair in q.Daily ?? new Dictionary<string, long>())
                    state.DailyCounters[pair.Key] = pair.Value;
                state.DailyDay = q.DailyDay;
                state.ClaimedDaily.UnionWith(q.ClaimedDaily ?? new List<string>());
                state.DailyPoints = q.DailyPoints;
                state.ClaimedChests.UnionWith(q.ClaimedChests ?? new List<int>());
                foreach (var pair in q.Items ?? new Dictionary<string, int>())
                    state.Items[pair.Key] = pair.Value;
                foreach (var pair in q.HeroCards ?? new Dictionary<string, int>())
                    state.HeroCards[pair.Key] = pair.Value;
            }

            foreach (var m in doc.Marches ?? new List<MarchDoc>())
            {
                state.Marches.Add(new MarchState
                {
                    Id = m.Id, NodeId = m.NodeId, NodeType = m.NodeType, NodeLevel = m.NodeLevel, X = m.X, Y = m.Y,
                    StartedAt = m.StartedAt, ArrivesAt = m.ArrivesAt, GatheredAt = m.GatheredAt, HomeAt = m.HomeAt, Reserved = m.Reserved,
                    Rates = Resources(m.Rates), Cargo = Resources(m.Cargo),
                    HeroId = m.HeroId ?? string.Empty, Crew = m.Crew, FromX = m.FromX, FromY = m.FromY
                });
            }

            state.ExpeditionCleared = doc.ExpeditionCleared;
            state.AvatarId = doc.Avatar ?? string.Empty;

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
