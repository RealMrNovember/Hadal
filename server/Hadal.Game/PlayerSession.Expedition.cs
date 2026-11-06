using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>The Depth Map campaign (docs/rehber 08 §5): dives resolve at once against the player's power.</summary>
    public sealed partial class PlayerSession
    {
        private const string DailyRewardedDives = "rewardedDive";

        private void DiveStage(DiveStageCommand command, DateTimeOffset now)
        {
            RollDailyIfNeeded(now);
            var expedition = _catalog.Expedition;
            var oxygen = _state.Wallet.TryGetValue(ResourceType.Oxygen, out var o2) ? o2 : 0;
            var verdict = ExpeditionRules.Validate(expedition, command.Chapter, command.Stage, _state.ExpeditionCleared, ReactorLevel(), oxygen);
            if (verdict != DiveRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            var chapter = ExpeditionRules.Chapter(expedition, command.Chapter)!;
            var power = ComputePower();
            var recommended = ExpeditionRules.RecommendedPower(expedition, chapter, command.Stage);
            var won = power >= recommended;
            var walletBeforeDive = WalletSnapshot();
            _state.Wallet[ResourceType.Oxygen] = oxygen - chapter.oxygen + (won ? 0 : ExpeditionRules.Refund(expedition, chapter));
            LogWalletDelta("dive", walletBeforeDive, now);
            _walletDirty = true;

            var report = new DiveReportDto
            {
                Chapter = command.Chapter,
                Stage = command.Stage,
                Won = won,
                Power = power,
                Recommended = recommended,
                OxygenSpent = won ? chapter.oxygen : chapter.oxygen - ExpeditionRules.Refund(expedition, chapter)
            };

            var rewards = new List<ResolvedReward>();
            if (won)
            {
                Record("dive");
                Score(EventActions.Dive, 1);
                var index = ExpeditionRules.Index(expedition, command.Chapter, command.Stage);
                report.FirstClear = index == _state.ExpeditionCleared;
                if (report.FirstClear)
                {
                    _state.ExpeditionCleared++;
                    if (ExpeditionRules.IsBoss(expedition, command.Stage))
                        rewards.AddRange(BossRewards(chapter));
                }

                var used = _state.DailyCounters.TryGetValue(DailyRewardedDives, out var u) ? u : 0;
                if (used < expedition.dailyRewarded)
                {
                    _state.DailyCounters[DailyRewardedDives] = used + 1;
                    report.Rewarded = true;
                    var titanium = (long)Math.Round(_catalog.IncomeHour(ReactorLevel()) * ExpeditionRules.RewardIncomeHours(expedition, ReactorLevel()));
                    rewards.Add(new ResolvedReward("resource", nameof(ResourceType.Titanium), titanium));
                    rewards.Add(new ResolvedReward("resource", nameof(ResourceType.Biomass), (long)Math.Round(titanium * _catalog.Data.incomeBiomassRatio)));
                    if (chapter.diveAlloy > 0)
                        rewards.Add(new ResolvedReward("resource", nameof(ResourceType.Alloy), chapter.diveAlloy));
                }
            }

            _state.IsDirty = true;

            // Campaign resources land in the bag as capsules (docs/rehber 08 §5.9), like quest rewards.
            rewards = rewards.Where(r => r.Amount > 0).Select(r => r.Type == "resource" && GameCatalog.TryParseResource(r.Id, out var res) && Capsules.Holds(res)
                ? new ResolvedReward("item", Capsules.Id(res, r.Amount), 1)
                : r).ToList();
            foreach (var r in rewards)
                Grant(r, now);
            report.Rewards = rewards.Select(r => new RewardEntryDto { Type = r.Type, Id = r.Id, Amount = r.Amount }).ToArray();

            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.DiveReport + command.CommandId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serializer.Serialize(report)
                },
                ExpeditionChange(),
                WalletChange(),
                HeroesChange(now),
                InventoryChange(),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        /// <summary>A chapter boss's one-time rewards: Hadalite, Superalloy and cards of a hero of the chapter's rarity.</summary>
        private IEnumerable<ResolvedReward> BossRewards(ExpeditionChapterData chapter)
        {
            if (chapter.bossHadalite > 0)
                yield return new ResolvedReward("resource", nameof(ResourceType.Hadalite), chapter.bossHadalite);
            if (chapter.bossAlloy > 0)
                yield return new ResolvedReward("resource", nameof(ResourceType.Alloy), chapter.bossAlloy);
            if (chapter.bossCards <= 0)
                yield break;

            // The guide lets the player choose; until a picker exists the cards go to the hero of that rarity
            // the player is closest to (owned first, then most cards), so they are never wasted.
            var hero = _catalog.Heroes
                .Where(h => h.Rarity.id == chapter.bossCardRarity)
                .OrderByDescending(h => _state.Heroes.Any(o => o.HeroId == h.Id))
                .ThenByDescending(h => _state.HeroCards.TryGetValue(h.Id, out var c) ? c : 0)
                .ThenBy(h => h.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (hero != null)
                yield return new ResolvedReward("shards", hero.Id, chapter.bossCards);
        }

        private EntityChangeDto ExpeditionChange() => new()
        {
            EntityId = EntityIds.Expedition,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(ExpeditionDto())
        };

        private ExpeditionStateDto ExpeditionDto() => new()
        {
            Cleared = _state.ExpeditionCleared,
            RewardedToday = (int)(_state.DailyCounters.TryGetValue(DailyRewardedDives, out var used) ? used : 0),
            DailyRewarded = _catalog.Expedition.dailyRewarded,
            Power = ComputePower()
        };
    }
}
