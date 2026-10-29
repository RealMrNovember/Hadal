using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Alliance coins and the alliance shop (docs/31 step 4, AllianceShopRules). Coins are personal and stay with the
    /// player across alliances; they come from helping (capped a day) and from alliance gifts. Stock is per player and
    /// week. Stored as the player's "p:&lt;id&gt;:coins" alliance document.
    /// </summary>
    public sealed partial class SocialHub
    {
        private sealed class CoinPurse
        {
            public long Coins { get; set; }
            public long HelpDay { get; set; }
            public int HelpCoins { get; set; }

            /// <summary>Alliance research donations made on <see cref="HelpDay"/>.</summary>
            public int DonationsToday { get; set; }
            public long Week { get; set; }
            public Dictionary<string, int> Bought { get; set; } = new(StringComparer.Ordinal);
        }

        private readonly Dictionary<string, CoinPurse> _purses = new(StringComparer.Ordinal);

        private static string PurseKey(string playerId) => $"p:{playerId}:coins";

        /// <summary>The player's purse, rolled to today and this week. Caller holds the lock.</summary>
        private CoinPurse PurseOf(string playerId, DateTimeOffset now)
        {
            if (!_purses.TryGetValue(playerId, out var purse))
                _purses[playerId] = purse = new CoinPurse();
            var ms = now.ToUnixTimeMilliseconds();
            var day = ms / 86_400_000L;
            if (purse.HelpDay != day)
            {
                purse.HelpDay = day;
                purse.HelpCoins = 0;
                purse.DonationsToday = 0;
            }

            var week = AllianceShopRules.Week(ms);
            if (purse.Week != week)
            {
                purse.Week = week;
                purse.Bought.Clear();
            }

            return purse;
        }

        private void SavePurse(string playerId, CoinPurse purse)
        {
            var json = JsonSerializer.Serialize(purse);
            Write(ct => _store.SaveDocAsync(PurseKey(playerId), json, ct));
        }

        /// <summary>Adds coins (alliance gifts, the Overseer). Caller holds the lock.</summary>
        private void AddCoins(string playerId, long coins, DateTimeOffset now)
        {
            if (coins <= 0)
                return;
            var purse = PurseOf(playerId, now);
            purse.Coins += coins;
            SavePurse(playerId, purse);
            if (_online.TryGetValue(playerId, out var session))
                session.Push(new List<EntityChangeDto> { CoinsChange(playerId, now) }, now);
        }

        /// <summary>Coins for helping mates, within the day's cap. Caller holds the lock.</summary>
        private void AwardHelpCoins(PlayerSession helper, int helps, DateTimeOffset now)
        {
            var purse = PurseOf(helper.PublicId, now);
            var earned = AllianceShopRules.HelpCoins(helps, purse.HelpCoins);
            if (earned <= 0)
                return;
            purse.HelpCoins += earned;
            purse.Coins += earned;
            SavePurse(helper.PublicId, purse);
            helper.Push(new List<EntityChangeDto> { CoinsChange(helper.PublicId, now) }, now);
        }

        private SocialRejectReason Buy(PlayerSession session, BuyAllianceItemCommand command, DateTimeOffset now)
        {
            if (AllianceOf(session.PublicId) == null)
                return SocialRejectReason.NotInAlliance;
            var offer = AllianceShopRules.Find(command.ItemId);
            var count = Math.Max(1, command.Count);
            if (offer == null || count > offer.Stock)
                return SocialRejectReason.NotAllowed;

            var purse = PurseOf(session.PublicId, now);
            var bought = purse.Bought.TryGetValue(offer.ItemId, out var b) ? b : 0;
            if (bought + count > offer.Stock)
                return SocialRejectReason.SoldOut;
            if (purse.Coins < offer.Price * count)
                return SocialRejectReason.NotEnoughCoins;

            purse.Coins -= offer.Price * count;
            purse.Bought[offer.ItemId] = bought + count;
            SavePurse(session.PublicId, purse);
            session.Push(new List<EntityChangeDto> { CoinsChange(session.PublicId, now) }, now);
            session.GrantShopItem(command.CommandId, command.ClientSequence, offer.ItemId, count, now);
            return SocialRejectReason.None;
        }

        private EntityChangeDto CoinsChange(string playerId, DateTimeOffset now)
        {
            var purse = PurseOf(playerId, now);
            var ms = now.ToUnixTimeMilliseconds();
            return new EntityChangeDto
            {
                EntityId = EntityIds.AllianceCoins,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new AllianceCoinsDto
                {
                    Coins = purse.Coins,
                    HelpCoinsToday = purse.HelpCoins,
                    DailyHelpCoins = AllianceShopRules.DailyHelpCoins,
                    ResetMs = AllianceShopRules.MsToReset(ms),
                    Shop = AllianceShopRules.Offers.Select(o => new AllianceShopStockDto
                    {
                        ItemId = o.ItemId, Price = o.Price, Stock = o.Stock,
                        Left = Math.Max(0, o.Stock - (purse.Bought.TryGetValue(o.ItemId, out var n) ? n : 0))
                    }).ToArray()
                })
            };
        }

        private void LoadPurses(IEnumerable<(string Key, string Json)> docs)
        {
            foreach (var (key, json) in docs)
            {
                if (!key.StartsWith("p:", StringComparison.Ordinal) || !key.EndsWith(":coins", StringComparison.Ordinal))
                    continue;
                try
                {
                    _purses[key.Substring(2, key.Length - 2 - ":coins".Length)] = JsonSerializer.Deserialize<CoinPurse>(json) ?? new CoinPurse();
                }
                catch (JsonException)
                {
                }
            }
        }

        /// <summary>The player's coins (tests and the Overseer).</summary>
        internal long CoinsOf(string playerId, DateTimeOffset now)
        {
            lock (_gate)
                return PurseOf(playerId, now).Coins;
        }

        /// <summary>Adds coins from outside (tests, the Overseer).</summary>
        internal void GrantCoinsForTests(string playerId, long coins, DateTimeOffset now)
        {
            lock (_gate)
                AddCoins(playerId, coins, now);
        }
    }
}
