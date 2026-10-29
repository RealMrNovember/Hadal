using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hadal.Game.Events
{
    /// <summary>
    /// An event as the Overseer defines it (docs/29 §2.2). Stored as one JSON document, so new settings need no
    /// migration; the admin panel edits exactly this shape.
    /// </summary>
    public sealed class EventDefinition
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>EventTemplates: "points", "alliance" or "login".</summary>
        public string Template { get; set; } = "points";

        /// <summary>"draft" (hidden), "published" or "cancelled" (hidden, no claims).</summary>
        public string Status { get; set; } = EventStatus.Draft;

        /// <summary>Text per language code ("tr", "en", ...).</summary>
        public Dictionary<string, string> Title { get; set; } = new();
        public Dictionary<string, string> Description { get; set; } = new();
        public string Banner { get; set; } = "abyss";
        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset EndsAt { get; set; }

        /// <summary>Days after the end during which rank rewards can still be claimed.</summary>
        public int ClaimDays { get; set; } = 3;

        /// <summary>Core Reactor range of the players who see it (0 = no bound).</summary>
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }

        /// <summary>Account ids that alone see the event (a test run); empty = everyone in the level range.</summary>
        public List<string> TestAccounts { get; set; } = new();

        /// <summary>Points a player needs for a rank reward.</summary>
        public long MinRankPoints { get; set; }

        public List<EventActionConfig> Actions { get; set; } = new();
        public List<EventPhaseConfig> Phases { get; set; } = new();

        /// <summary>Phases repeat back to back until the event ends (a theme rotation).</summary>
        public bool CyclePhases { get; set; }

        /// <summary>Points thresholds with rewards; for login events, one entry per day.</summary>
        public List<EventTierConfig> Stages { get; set; } = new();
        public List<EventTierConfig> RankBands { get; set; } = new();

        /// <summary>Event bosses (MonsterKind.EventOnly) the map shows while the event is live (WorldService.Bosses).</summary>
        public List<EventBossConfig> Bosses { get; set; } = new();

        public string Note { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset ClaimUntil => EndsAt.AddDays(Math.Max(0, ClaimDays));

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public string ToJson() => JsonSerializer.Serialize(this, Json);

        public static EventDefinition FromJson(string json) => JsonSerializer.Deserialize<EventDefinition>(json, Json) ?? new EventDefinition();

        public EventDefinition Clone() => FromJson(ToJson());
    }

    public static class EventStatus
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Cancelled = "cancelled";
    }

    public sealed class EventActionConfig
    {
        public string Action { get; set; } = string.Empty;
        public long Points { get; set; } = 1;
        public long Per { get; set; } = 1;
    }

    public sealed class EventPhaseConfig
    {
        public Dictionary<string, string> Title { get; set; } = new();

        /// <summary>Hours from the event start, and the length in hours.</summary>
        public double StartHour { get; set; }
        public double Hours { get; set; } = 4;
        public List<EventActionConfig> Actions { get; set; } = new();
    }

    public sealed class EventTierConfig
    {
        public long Points { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public List<EventRewardConfig> Rewards { get; set; } = new();
    }

    /// <summary>"A boss of this kind and level appears at X:Y for the event's duration."</summary>
    public sealed class EventBossConfig
    {
        /// <summary>An event-only monster kind (MonsterRules: "chasm_maw", "depth_kraken").</summary>
        public string Kind { get; set; } = string.Empty;
        public int Level { get; set; } = 20;
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>A boss of a live event as the world map places it.</summary>
    public sealed record EventBossSpawn(string EventId, int Index, string Kind, int Level, int X, int Y, DateTimeOffset EndsAt);

    /// <summary>Same meaning as the catalog's RewardData: resource, item or shards (hero cards).</summary>
    public sealed class EventRewardConfig
    {
        public string Type { get; set; } = "resource";
        public string Id { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Rarity { get; set; } = string.Empty;
    }

    /// <summary>One player's progress in one event.</summary>
    public sealed class EventScore
    {
        public string EventId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>The alliance the player scored for last (alliance contests rank them by it).</summary>
        public string AllianceId { get; set; } = string.Empty;
        public long Points { get; set; }
        public HashSet<int> Claimed { get; set; } = new();
        public bool RankClaimed { get; set; }
        public int LoginDays { get; set; }

        /// <summary>UTC day number of the last counted sign-in.</summary>
        public long LastLoginDay { get; set; }

        /// <summary>Units of an action still short of a full "per" (EventRules.Score).</summary>
        public Dictionary<string, long> Carry { get; set; } = new();
        public DateTimeOffset UpdatedAt { get; set; }

        public string StateJson() => JsonSerializer.Serialize(new ScoreState(Claimed.ToList(), RankClaimed, LoginDays, LastLoginDay, Carry), EventDefinition.Json);

        public void ApplyState(string json)
        {
            var state = JsonSerializer.Deserialize<ScoreState>(json, EventDefinition.Json);
            if (state == null)
                return;
            Claimed = new HashSet<int>(state.Claimed ?? new List<int>());
            RankClaimed = state.RankClaimed;
            LoginDays = state.LoginDays;
            LastLoginDay = state.LastLoginDay;
            Carry = state.Carry ?? new Dictionary<string, long>();
        }

        private sealed record ScoreState(List<int>? Claimed, bool RankClaimed, int LoginDays, long LastLoginDay, Dictionary<string, long>? Carry);
    }
}
