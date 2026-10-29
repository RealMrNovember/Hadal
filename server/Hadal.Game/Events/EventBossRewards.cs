using Hadal.Game.Mailbox;
using HADAL.Shared.Enums;

namespace Hadal.Game.Events
{
    /// <summary>A boss that died or whose event ended, with who hit it how hard and their alliances at that moment.</summary>
    public sealed record BossOutcome(string EventId, int Index, string Kind, int Level, bool Killed, string KillerId,
        IReadOnlyDictionary<string, long> Damage, IReadOnlyDictionary<string, string> AllianceOf);

    /// <summary>One contributor's prize: their rank inside their alliance (0 = no alliance) and what they get.</summary>
    public sealed record BossPrize(string PlayerId, int Rank, long Damage, List<EventRewardConfig> Rewards);

    /// <summary>
    /// Boss payouts (docs/29, research §6.2 "alliance boss"): every member who hit the boss is ranked by damage inside
    /// their own alliance; ranks 1, 2 and 3 get clearly bigger gifts, the rest a participation gift. Players without an
    /// alliance get the participation gift. The event definition sets the bands (BossRanks, BossParticipation); empty
    /// means the built-in tiers below.
    /// </summary>
    public static class EventBossRewards
    {
        private static EventRewardConfig Item(string id, long amount) => new() { Type = "item", Id = id, Amount = amount };

        public static List<EventTierConfig> DefaultRanks() => new()
        {
            new EventTierConfig { From = 1, To = 1, Rewards = { Item("chest_gold", 1), Item("chest_silver", 1) } },
            new EventTierConfig { From = 2, To = 2, Rewards = { Item("chest_gold", 1) } },
            new EventTierConfig { From = 3, To = 3, Rewards = { Item("chest_silver", 2) } },
            new EventTierConfig { From = 4, To = 10, Rewards = { Item("chest_silver", 1) } }
        };

        public static List<EventRewardConfig> DefaultParticipation() => new() { Item("chest_bronze", 1) };

        /// <summary>The members of one alliance who hit the boss, most damage first (ties by id), with their ranks.</summary>
        public static List<(string PlayerId, long Damage, int Rank)> AllianceBoard(IReadOnlyDictionary<string, long> damage,
            Func<string, string> allianceOf, string allianceId) =>
            damage.Where(p => p.Value > 0 && allianceId.Length > 0 && allianceOf(p.Key) == allianceId)
                .OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal)
                .Select((p, i) => (p.Key, p.Value, i + 1)).ToList();

        /// <summary>Every contributor's prize for one boss.</summary>
        public static List<BossPrize> Plan(BossOutcome outcome, IReadOnlyList<EventTierConfig> ranks, IReadOnlyList<EventRewardConfig> participation)
        {
            var bands = ranks.Count > 0 || participation.Count > 0 ? ranks : DefaultRanks();
            var rest = ranks.Count > 0 || participation.Count > 0 ? participation : DefaultParticipation();
            string AllianceOf(string id) => outcome.AllianceOf.TryGetValue(id, out var a) ? a ?? string.Empty : string.Empty;

            var prizes = new List<BossPrize>();
            foreach (var group in outcome.Damage.Where(p => p.Value > 0).GroupBy(p => AllianceOf(p.Key)))
            {
                if (group.Key.Length == 0)
                {
                    prizes.AddRange(group.Select(p => new BossPrize(p.Key, 0, p.Value, rest.ToList())));
                    continue;
                }

                foreach (var (playerId, damage, rank) in AllianceBoard(outcome.Damage, AllianceOf, group.Key))
                {
                    var band = bands.FirstOrDefault(b => b.From <= rank && rank <= b.To);
                    prizes.Add(new BossPrize(playerId, rank, damage, (band?.Rewards ?? rest).ToList()));
                }
            }

            return prizes.Where(p => p.Rewards.Count > 0).OrderBy(p => p.PlayerId, StringComparer.Ordinal).ToList();
        }

        /// <summary>Only resources and bag items fit a mail (no hero shards).</summary>
        public static bool Mailable(EventRewardConfig r) => r.Type is "resource" or "item";

        /// <summary>The prize as a mail. Title and body are localisation keys with arguments ("loc:key|arg|…"), shown in
        /// the reader's language by the client.</summary>
        public static MailDraft Mail(BossPrize prize, BossOutcome outcome)
        {
            var amounts = new Dictionary<string, long>(StringComparer.Ordinal);
            var items = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var r in prize.Rewards.Where(Mailable))
            {
                if (r.Type == "resource" && Enum.TryParse<ResourceType>(r.Id, out var resource) && resource != ResourceType.Unknown)
                    amounts[resource.ToString()] = (amounts.TryGetValue(resource.ToString(), out var a) ? a : 0) + r.Amount;
                else if (r.Type == "item")
                    items[r.Id] = (items.TryGetValue(r.Id, out var n) ? n : 0) + (int)Math.Min(int.MaxValue, r.Amount);
            }

            var body = prize.Rank > 0
                ? $"loc:mail.boss.ranked|monster.{outcome.Kind}|{prize.Rank}|{prize.Damage}"
                : $"loc:mail.boss.joined|monster.{outcome.Kind}|{prize.Damage}";
            var title = outcome.Killed ? $"loc:mail.boss.killed|monster.{outcome.Kind}" : $"loc:mail.boss.gone|monster.{outcome.Kind}";
            return new MailDraft(MailKinds.Event, "HADAL", title, body, new MailAttachments { Amounts = amounts, Items = items });
        }
    }
}
