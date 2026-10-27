using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>The battle formula against the guide's worked battles (docs/rehber 07 §4.6).</summary>
    public sealed class CombatTests
    {
        private static readonly TroopCatalogData Troops = TestSupport.BalanceCatalog.Troops;

        private static BattleSide Mix(double count, int tier)
        {
            var side = new BattleSide();
            foreach (var type in new[] { "armored", "harpoon", "arc" })
                side.Troops[TroopRules.Key(type, tier)] = count / 3;
            return side;
        }

        private static BattleSide Pure(string type, double count, int tier)
        {
            var side = new BattleSide();
            side.Troops[TroopRules.Key(type, tier)] = count;
            return side;
        }

        private static (bool AttackerWon, int Rounds, int AttackerLoss, int DefenderLoss) Run(BattleSide a, BattleSide d)
        {
            var startA = a.Troops.Values.Sum();
            var startD = d.Troops.Values.Sum();
            var r = CombatRules.Resolve(Troops, a, d);
            return (r.AttackerWon, r.Rounds, (int)Math.Round(100.0 * r.AttackerLost.Values.Sum() / startA), (int)Math.Round(100.0 * r.DefenderLost.Values.Sum() / startD));
        }

        [Theory]
        // scenario, attacker won, rounds, attacker loss %, defender loss % (guide values, ±2 points)
        [InlineData("mirror", false, 10, 49, 49)]
        [InlineData("plus20", true, 10, 38, 64)]
        [InlineData("armoredVsArc", true, 10, 33, 50)]
        [InlineData("harpoonVsArmored", true, 10, 36, 61)]
        [InlineData("arcVsHarpoon", true, 9, 44, 72)]
        [InlineData("arcVsArmored", false, 10, 50, 33)]
        [InlineData("t6VsT5", true, 8, 21, 76)]
        public void Worked_battles_come_out_as_in_the_guide(string scenario, bool attackerWon, int rounds, int attackerLoss, int defenderLoss)
        {
            var (a, d) = scenario switch
            {
                "mirror" => (Mix(10000, 5), Mix(10000, 5)),
                "plus20" => (Mix(12000, 5), Mix(10000, 5)),
                "armoredVsArc" => (Pure("armored", 10000, 5), Pure("arc", 10000, 5)),
                "harpoonVsArmored" => (Pure("harpoon", 10000, 5), Pure("armored", 10000, 5)),
                "arcVsHarpoon" => (Pure("arc", 10000, 5), Pure("harpoon", 10000, 5)),
                "arcVsArmored" => (Pure("arc", 10000, 5), Pure("armored", 10000, 5)),
                _ => (Mix(10000, 6), Mix(10000, 5))
            };
            var result = Run(a, d);
            Assert.Equal(attackerWon, result.AttackerWon);
            Assert.Equal(rounds, result.Rounds);
            Assert.InRange(result.AttackerLoss, attackerLoss - 2, attackerLoss + 2);
            Assert.InRange(result.DefenderLoss, defenderLoss - 2, defenderLoss + 2);
        }

        [Fact]
        public void A_bigger_army_routs_its_enemy_early_and_bonuses_tip_a_mirror()
        {
            var plus50 = Run(Mix(15000, 5), Mix(10000, 5));
            Assert.True(plus50.AttackerWon);
            Assert.True(plus50.Rounds < 10);

            var boosted = Mix(10000, 5);
            boosted.AtkPercent = 10;
            Assert.True(Run(boosted, Mix(10000, 5)).AttackerWon);
        }

        [Fact]
        public void Stats_follow_the_guide_table()
        {
            Assert.Equal((26d, 38d, 120d), CombatRules.Stats(Troops, "armored", 1));
            Assert.Equal((143d, 123d, 412d), CombatRules.Stats(Troops, "arc", 5));
        }
    }
}
