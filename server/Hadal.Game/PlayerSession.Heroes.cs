using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>Heroes: Cryo-Pod, training, promotion and the command crew (docs/25_Heroes_Alliance_Chat.md).</summary>
    public sealed partial class PlayerSession
    {
        /// <summary>Takes every hero off the crew (tests that check plain timings).</summary>
        internal void ClearCrewForTests()
        {
            lock (_gate)
            {
                foreach (var hero in _state.Heroes)
                    hero.Slot = -1;
                RefreshModifiers();
            }
        }

        /// <summary>Gives a base without heroes the catalog's starter hero on the first crew slot.</summary>
        private void EnsureStarterHero()
        {
            var starter = _catalog.HeroRules.starterHeroId;
            if (_state.Heroes.Count > 0 || string.IsNullOrEmpty(starter) || !_catalog.TryGetHero(starter, out _))
                return;

            _state.Heroes.Add(new OwnedHeroState { HeroId = starter, Level = 1, Stars = 1, Slot = 0 });
            _state.IsDirty = true;
        }

        private void Pull(ExecuteGachaCommand command, DateTimeOffset now)
        {
            var count = command.PullCount >= 10 ? 10 : 1;
            if ((command.BannerId.Length > 0 && command.BannerId != HeroRules.PodBanner) || _catalog.Heroes.Count == 0)
            {
                Reject(command.CommandId, command.ClientSequence, HeroRejectReason.UnknownBanner.ToString(), now);
                return;
            }

            var free = count == 1 && _catalog.HeroRules.pod.freeSeconds > 0 && (_state.FreePullAt == null || _state.FreePullAt <= now);
            if (!free)
            {
                var cost = count == 10 ? _catalog.PodTenCost : _catalog.PodCost;
                if (!BuildingProgression.CanAfford(cost, _state.Wallet))
                {
                    Reject(command.CommandId, command.ClientSequence, HeroRejectReason.InsufficientResources.ToString(), now);
                    return;
                }

                BuildingProgression.Charge(cost, _state.Wallet);
            }
            else
            {
                _state.FreePullAt = now.AddSeconds(_catalog.HeroRules.pod.freeSeconds);
            }

            var pulls = new List<GachaPullEntry>();
            var sincePity = _state.SincePity;
            for (var i = 0; i < count; i++)
            {
                var hero = HeroRules.Pull(_catalog, ref sincePity, Random.Shared.Next);
                if (hero == null)
                    break;

                var owned = _state.Heroes.FirstOrDefault(h => h.HeroId == hero.Id);
                if (owned == null)
                {
                    _state.Heroes.Add(new OwnedHeroState { HeroId = hero.Id, Level = 1, Stars = 1, Slot = -1 });
                    pulls.Add(new GachaPullEntry { HeroId = hero.Id, IsNew = true });
                }
                else
                {
                    owned.Shards += hero.Rarity.duplicateShards;
                    pulls.Add(new GachaPullEntry { HeroId = hero.Id, Shards = hero.Rarity.duplicateShards });
                }
            }

            _state.SincePity = sincePity;
            _state.IsDirty = true;

            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.GachaResult + command.CommandId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serializer.Serialize(new GachaResultDto { Pulls = pulls.ToArray() })
                },
                HeroesChange(now),
                WalletChange(),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void LevelUpHero(LevelUpHeroCommand command, DateTimeOffset now)
        {
            var (hero, owned) = FindHero(command.HeroId);
            var verdict = HeroRules.ValidateLevelUp(_catalog, hero, Owned(owned), _state.Wallet);
            if (verdict != HeroRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            BuildingProgression.Charge(HeroRules.LevelCost(_catalog, hero!, owned!.Level), _state.Wallet);
            owned.Level += 1;
            HeroChanged(command.CommandId, command.ClientSequence, now);
        }

        private void PromoteHero(PromoteHeroCommand command, DateTimeOffset now)
        {
            var (hero, owned) = FindHero(command.HeroId);
            var verdict = HeroRules.ValidatePromote(_catalog, hero, Owned(owned));
            if (verdict != HeroRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            owned!.Shards -= HeroRules.ShardsForNextStar(_catalog.HeroRules, owned.Stars);
            owned.Stars += 1;
            HeroChanged(command.CommandId, command.ClientSequence, now);
        }

        private void AssignHero(AssignHeroCommand command, DateTimeOffset now)
        {
            var (hero, owned) = FindHero(command.HeroId);
            var verdict = HeroRules.ValidateAssign(_catalog, hero, Owned(owned), command.Slot, ReactorLevel());
            if (verdict != HeroRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            // Whoever held the slot steps down; the hero leaves its old slot.
            if (command.Slot >= 0)
            {
                foreach (var other in _state.Heroes.Where(h => h.Slot == command.Slot))
                    other.Slot = -1;
            }

            owned!.Slot = command.Slot;
            HeroChanged(command.CommandId, command.ClientSequence, now);
        }

        /// <summary>Settles the economy under the old crew, then applies the new bonuses.</summary>
        private void HeroChanged(string commandId, ulong sequence, DateTimeOffset now)
        {
            AdvanceTo(now);
            RefreshModifiers();
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto>
            {
                HeroesChange(now),
                WalletChange(),
                ProductionChange(now),
                Accepted(commandId, sequence)
            });
        }

        private (HeroDefinition? Hero, OwnedHeroState? Owned) FindHero(string heroId)
        {
            _catalog.TryGetHero(heroId, out var hero);
            return (hero, _state.Heroes.FirstOrDefault(h => h.HeroId == heroId));
        }

        private static OwnedHero? Owned(OwnedHeroState? owned) =>
            owned == null ? null : new OwnedHero(owned.HeroId, owned.Level, owned.Stars, owned.Shards, owned.Slot);

        private IEnumerable<OwnedHero> Crew() =>
            _state.Heroes.Select(h => new OwnedHero(h.HeroId, h.Level, h.Stars, h.Shards, h.Slot));

        /// <summary>Base power: building levels plus every owned hero's power.</summary>
        private long ComputePower()
        {
            long power = _state.Buildings.Sum(b => (long)b.Level) * 10;
            foreach (var owned in _state.Heroes)
            {
                if (_catalog.TryGetHero(owned.HeroId, out var hero))
                    power += HeroRules.Power(hero, owned.Level, owned.Stars);
            }

            return power;
        }

        private EntityChangeDto HeroesChange(DateTimeOffset now) => new()
        {
            EntityId = EntityIds.Heroes,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(RosterDto(now))
        };

        private HeroRosterDto RosterDto(DateTimeOffset now) => new()
        {
            Heroes = _state.Heroes.Select(h => new HeroStateDto
            {
                EntityId = h.HeroId,
                DefinitionId = h.HeroId,
                Level = h.Level,
                Stars = h.Stars,
                Shards = h.Shards,
                CrewSlot = h.Slot
            }).ToArray(),
            SincePity = _state.SincePity,
            FreePullRemainingMs = _catalog.HeroRules.pod.freeSeconds > 0 ? Remaining(_state.FreePullAt, now) : -1
        };
    }
}
