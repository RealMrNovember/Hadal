using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>Chapter and daily tasks, activity chests, the bag and hero cards (docs/27_Quests_And_Items.md).</summary>
    public sealed partial class PlayerSession
    {
        private bool _questsDirty;
        private bool _inAlliance;

        /// <summary>Counts a quest event (upgrade, research, salvage, boost, pod, heroTrain, help, chat). Caller holds the lock.</summary>
        private void Record(string counter, long amount = 1)
        {
            if (amount <= 0)
                return;
            _state.QuestCounters[counter] = (_state.QuestCounters.TryGetValue(counter, out var n) ? n : 0) + amount;
            _state.DailyCounters[counter] = (_state.DailyCounters.TryGetValue(counter, out var d) ? d : 0) + amount;
            _state.IsDirty = true;
            _questsDirty = true;
        }

        /// <summary>Events that happen outside the session (alliance help, chat) — the hub calls this.</summary>
        internal void RecordEvent(string counter, long amount, DateTimeOffset now)
        {
            lock (_gate)
            {
                Record(counter, amount);
                if (counter == "help")
                    Score(EventActions.Help, amount);
                SendDelta(now, new List<EntityChangeDto>());
            }
        }

        internal void SetInAlliance(bool inAlliance, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (_inAlliance == inAlliance)
                    return;
                _inAlliance = inAlliance;
                _questsDirty = true;
                SendDelta(now, new List<EntityChangeDto>());
            }
        }

        /// <summary>Starts a new day's tasks after 00:00 UTC.</summary>
        private void RollDailyIfNeeded(DateTimeOffset now)
        {
            var day = QuestRules.Day(now);
            if (_state.DailyDay == day)
                return;
            _state.DailyDay = day;
            _state.DailyCounters.Clear();
            _state.ClaimedDaily.Clear();
            _state.ClaimedChests.Clear();
            _state.DailyPoints = 0;
            _state.IsDirty = true;
            _questsDirty = true;
        }

        private QuestContext Context(bool daily) => new()
        {
            BuildingLevel = id => _state.Buildings.Where(b => b.DefinitionId == id).Select(b => b.Level).DefaultIfEmpty(0).Max(),
            BuildingCount = id => _state.Buildings.Count(b => b.DefinitionId == id && b.Level > 0),
            Counters = daily ? _state.DailyCounters : _state.QuestCounters,
            MaxHeroLevel = _state.Heroes.Select(h => h.Level).DefaultIfEmpty(0).Max(),
            HeroCount = _state.Heroes.Count,
            InAlliance = _inAlliance
        };

        private void ClaimQuest(ClaimQuestCommand command, DateTimeOffset now)
        {
            RollDailyIfNeeded(now);
            if (!_catalog.TryGetQuest(command.QuestId, out var quest, out var daily))
            {
                Reject(command.CommandId, command.ClientSequence, QuestRejectReason.UnknownQuest.ToString(), now);
                return;
            }

            QuestRejectReason verdict;
            if (!daily)
            {
                var chapter = _catalog.Quests.chapter;
                verdict = _state.ChapterIndex >= chapter.Length || chapter[_state.ChapterIndex].id != quest.id ? QuestRejectReason.NotCurrent
                    : !QuestRules.IsDone(quest, Context(false)) ? QuestRejectReason.NotDone
                    : QuestRejectReason.None;
            }
            else
            {
                verdict = _state.ClaimedDaily.Contains(quest.id) ? QuestRejectReason.AlreadyClaimed
                    : !QuestRules.IsDone(quest, Context(true)) ? QuestRejectReason.NotDone
                    : QuestRejectReason.None;
            }

            if (verdict != QuestRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            if (daily)
            {
                _state.ClaimedDaily.Add(quest.id);
                _state.DailyPoints += Math.Max(0, quest.points);
                Score(EventActions.DailyPoints, quest.points);
            }
            else
            {
                _state.ChapterIndex++;
            }

            GrantAndReport(command.CommandId, command.ClientSequence, QuestRules.Resolve(_catalog, quest.rewards, Random.Shared.Next), now);
        }

        private void ClaimDailyChest(ClaimDailyChestCommand command, DateTimeOffset now)
        {
            RollDailyIfNeeded(now);
            var chest = Array.Find(_catalog.Quests.dailyChests ?? Array.Empty<DailyChestData>(), c => c != null && c.points == command.Points);
            var verdict = chest == null ? QuestRejectReason.UnknownChest
                : _state.ClaimedChests.Contains(chest.points) ? QuestRejectReason.AlreadyClaimed
                : _state.DailyPoints < chest.points ? QuestRejectReason.NotEnoughPoints
                : QuestRejectReason.None;
            if (verdict != QuestRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _state.ClaimedChests.Add(chest!.points);
            GrantAndReport(command.CommandId, command.ClientSequence, QuestRules.Resolve(_catalog, chest.rewards, Random.Shared.Next), now);
        }

        /// <summary>How many rolling items (chests and the like) one "Use" opens at most, as Last Asylum's slider.</summary>
        public const int MaxOpenAtOnce = 10_000;

        private void OpenItem(OpenItemCommand command, DateTimeOffset now)
        {
            var count = Math.Clamp(command.Count, 1, MaxOpenAtOnce);

            // A resource capsule goes straight to the wallet and may pass the cap (docs/rehber 15 §2.2).
            // Capsules open in any number at once; anything that rolls (chests) up to MaxOpenAtOnce.
            if (Capsules.TryParse(command.ItemId, out var capsuleResource, out var capsuleAmount))
            {
                count = Math.Clamp(command.Count, 1, 1_000_000);
                if ((_state.Items.TryGetValue(command.ItemId, out var held) ? held : 0) < count)
                {
                    Reject(command.CommandId, command.ClientSequence, QuestRejectReason.NotEnoughItems.ToString(), now);
                    return;
                }

                TakeItem(command.ItemId, count);
                _state.Wallet[capsuleResource] = (_state.Wallet.TryGetValue(capsuleResource, out var w) ? w : 0) + capsuleAmount * count;
                _walletDirty = true;
                GrantAndReport(command.CommandId, command.ClientSequence,
                    new List<ResolvedReward> { new("resource", capsuleResource.ToString(), capsuleAmount * count) }, now, grant: false);
                return;
            }

            if (GiftItems.Is(command.ItemId))
            {
                OpenGift(command, count, now);
                return;
            }

            if (VipRules.TryParseItem(command.ItemId, out var vipPoints))
            {
                OpenVipItem(command, vipPoints, count, now);
                return;
            }

            var verdict = !_catalog.TryGetItem(command.ItemId, out var item) ? QuestRejectReason.UnknownItem
                : (_state.Items.TryGetValue(command.ItemId, out var have) ? have : 0) < count ? QuestRejectReason.NotEnoughItems
                : QuestRejectReason.None;
            if (verdict != QuestRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            TakeItem(item.id, count);
            var rewards = new List<ResolvedReward>();
            for (var i = 0; i < count; i++)
                rewards.AddRange(QuestRules.Open(_catalog, item, Random.Shared.Next));
            GrantAndReport(command.CommandId, command.ClientSequence, rewards, now);
        }

        /// <summary>
        /// An Overseer gift (GiftItems): a hero joins at its level and stars, or an owned one rises to them (never
        /// lower); troops join the army at home, past the barracks capacity like gathered resources pass the cap.
        /// </summary>
        private void OpenGift(OpenItemCommand command, int count, DateTimeOffset now)
        {
            var held = _state.Items.TryGetValue(command.ItemId, out var n) ? n : 0;
            if (!GiftItems.IsValid(_catalog, command.ItemId))
            {
                Reject(command.CommandId, command.ClientSequence, QuestRejectReason.UnknownItem.ToString(), now);
                return;
            }

            if (GiftItems.TryParseHero(command.ItemId, out var heroId, out var level, out var stars))
            {
                // One hero per use: a second copy of the same gift would change nothing.
                if (held < 1)
                {
                    Reject(command.CommandId, command.ClientSequence, QuestRejectReason.NotEnoughItems.ToString(), now);
                    return;
                }

                TakeItem(command.ItemId, 1);
                var owned = _state.Heroes.FirstOrDefault(h => h.HeroId == heroId);
                if (owned == null)
                {
                    _state.Heroes.Add(new OwnedHeroState { HeroId = heroId, Level = level, Stars = stars, Shards = 0, Slot = -1 });
                    Score(EventActions.HeroRecruit, 1);
                }
                else
                {
                    owned.Level = Math.Max(owned.Level, level);
                    owned.Stars = Math.Max(owned.Stars, stars);
                }

                RefreshModifiers();
                GrantAndReport(command.CommandId, command.ClientSequence, new List<ResolvedReward> { new("hero", heroId, 1) }, now, grant: false);
                return;
            }

            GiftItems.TryParseTroops(command.ItemId, out var type, out var tier, out var perPack);
            if (held < count)
            {
                Reject(command.CommandId, command.ClientSequence, QuestRejectReason.NotEnoughItems.ToString(), now);
                return;
            }

            TakeItem(command.ItemId, count);
            var key = TroopRules.Key(type, tier);
            _state.Troops[key] = (_state.Troops.TryGetValue(key, out var home) ? home : 0) + perPack * count;
            _armyDirty = true;
            GrantAndReport(command.CommandId, command.ClientSequence, new List<ResolvedReward> { new("troops", key, perPack * count) }, now, grant: false);
            SendDelta(now, new List<EntityChangeDto> { ArmyChange(now) });
        }

        private void RecruitHero(RecruitHeroCommand command, DateTimeOffset now)
        {
            var verdict = !_catalog.TryGetHero(command.HeroId, out var hero) ? QuestRejectReason.UnknownItem
                : _state.Heroes.Any(h => h.HeroId == command.HeroId) ? QuestRejectReason.AlreadyOwned
                : (_state.HeroCards.TryGetValue(command.HeroId, out var cards) ? cards : 0) < QuestRules.RecruitShards(hero) ? QuestRejectReason.NotEnoughCards
                : QuestRejectReason.None;
            if (verdict != QuestRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            // Cards beyond the recruit price stay with the hero as shards for its first stars.
            var spare = _state.HeroCards[hero.Id] - QuestRules.RecruitShards(hero);
            _state.HeroCards.Remove(hero.Id);
            _state.Heroes.Add(new OwnedHeroState { HeroId = hero.Id, Level = 1, Stars = 1, Shards = spare, Slot = -1 });
            Score(EventActions.HeroRecruit, 1);
            _state.IsDirty = true;
            _questsDirty = true;
            GrantAndReport(command.CommandId, command.ClientSequence, new List<ResolvedReward> { new("hero", hero.Id, 1) }, now, grant: false);
        }

        private void TakeItem(string id, int count)
        {
            var left = _state.Items[id] - count;
            if (left > 0)
                _state.Items[id] = left;
            else
                _state.Items.Remove(id);
            _state.IsDirty = true;
        }

        /// <summary>Pays rewards (resources capped at storage) and sends one report with everything that changed.</summary>
        private void GrantAndReport(string commandId, ulong sequence, List<ResolvedReward> rewards, DateTimeOffset now, bool grant = true)
        {
            if (grant)
            {
                // Quest and chest resources arrive as capsules in the bag: nothing is lost at the cap.
                rewards = rewards.Select(r => r.Type == "resource" && GameCatalog.TryParseResource(r.Id, out var res) && Capsules.Holds(res)
                    ? new ResolvedReward("item", Capsules.Id(res, r.Amount), 1)
                    : r).ToList();
                foreach (var r in rewards)
                    Grant(r);
            }

            _state.IsDirty = true;
            _questsDirty = true;
            RefreshModifiers();
            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.RewardReport + commandId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serializer.Serialize(new RewardReportDto
                    {
                        Rewards = rewards.Select(r => new RewardEntryDto { Type = r.Type, Id = r.Id, Amount = r.Amount }).ToArray()
                    })
                },
                WalletChange(),
                HeroesChange(now),
                InventoryChange(),
                ProductionChange(now),
                Accepted(commandId, sequence)
            });
        }

        private void Grant(ResolvedReward reward)
        {
            switch (reward.Type)
            {
                case "resource" when GameCatalog.TryParseResource(reward.Id, out var resource):
                    WalletOps.Add(_state.Wallet, new Dictionary<ResourceType, long> { [resource] = reward.Amount }, Capacities());
                    _walletDirty = true;
                    break;
                case "item":
                    _state.Items[reward.Id] = (_state.Items.TryGetValue(reward.Id, out var n) ? n : 0) + (int)reward.Amount;
                    break;
                case "shards":
                    var owned = _state.Heroes.FirstOrDefault(h => h.HeroId == reward.Id);
                    if (owned != null)
                        owned.Shards += (int)reward.Amount;
                    else
                        _state.HeroCards[reward.Id] = (_state.HeroCards.TryGetValue(reward.Id, out var c) ? c : 0) + (int)reward.Amount;
                    break;
            }
        }

        private EntityChangeDto QuestsChange(DateTimeOffset now) => new()
        {
            EntityId = EntityIds.Quests,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(QuestDto(now))
        };

        private QuestStateDto QuestDto(DateTimeOffset now)
        {
            RollDailyIfNeeded(now);
            var chapter = _catalog.Quests.chapter ?? Array.Empty<QuestData>();
            var dailyContext = Context(true);
            QuestProgressDto? current = null;
            if (_state.ChapterIndex < chapter.Length)
            {
                var q = chapter[_state.ChapterIndex];
                current = new QuestProgressDto { Id = q.id, Progress = QuestRules.Progress(q, Context(false)), Target = Math.Max(1, q.count) };
            }

            return new QuestStateDto
            {
                Chapter = current,
                ChapterNumber = Math.Min(_state.ChapterIndex + 1, chapter.Length),
                ChapterTotal = chapter.Length,
                Daily = (_catalog.Quests.daily ?? Array.Empty<QuestData>()).Select(q => new QuestProgressDto
                {
                    Id = q.id,
                    Progress = QuestRules.Progress(q, dailyContext),
                    Target = Math.Max(1, q.count),
                    Claimed = _state.ClaimedDaily.Contains(q.id)
                }).ToArray(),
                DailyPoints = _state.DailyPoints,
                ClaimedChests = _state.ClaimedChests.OrderBy(p => p).ToArray(),
                DailyResetMs = QuestRules.MillisecondsToReset(now)
            };
        }

        private EntityChangeDto InventoryChange() => new()
        {
            EntityId = EntityIds.Inventory,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(InventoryDto())
        };

        private InventoryDto InventoryDto() => new()
        {
            Items = _state.Items.Where(p => p.Value > 0).Select(p => new ItemCountDto { Id = p.Key, Count = p.Value }).ToArray(),
            HeroCards = _state.HeroCards.Where(p => p.Value > 0).Select(p => new ItemCountDto { Id = p.Key, Count = p.Value }).ToArray()
        };
    }
}
