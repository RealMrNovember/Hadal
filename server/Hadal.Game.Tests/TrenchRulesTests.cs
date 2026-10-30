using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;
using Xunit;

namespace Hadal.Game.Tests
{
    /// <summary>Trench Conquest's shared rules (docs/29 §4.6): groups, round robin, lane battles and round points.</summary>
    public sealed class TrenchRulesTests
    {
        private static readonly TroopCatalogData Troops = TestSupport.BalanceCatalog.Troops;

        private static TrenchRules.Team Team(string id, long count, int lane = 0, int tier = 1)
        {
            var team = new TrenchRules.Team { PlayerId = id, Name = id, Lane = lane, Power = count };
            foreach (var type in new[] { "armored", "harpoon", "arc" })
                team.Troops[TroopRules.Key(type, tier)] = count / 3;
            return team;
        }

        private static IEnumerable<KeyValuePair<string, long>> Alliances(int n) =>
            Enumerable.Range(0, n).Select(i => new KeyValuePair<string, long>($"a{i}", 1000 - i));

        [Fact]
        public void Groups_are_six_strongest_first_and_a_lone_last_alliance_joins_the_group_before()
        {
            var twelve = TrenchRules.Groups(Alliances(12));
            Assert.Equal(new[] { 6, 6 }, twelve.Select(g => g.Count));
            Assert.Equal("a0", twelve[0][0]);

            var seven = TrenchRules.Groups(Alliances(7));
            Assert.Single(seven);
            Assert.Equal(7, seven[0].Count);

            var eight = TrenchRules.Groups(Alliances(8));
            Assert.Equal(new[] { 6, 2 }, eight.Select(g => g.Count));
            Assert.Empty(TrenchRules.Groups(Alliances(0)));
        }

        [Fact]
        public void Five_rounds_of_six_meet_five_different_rivals_and_odd_groups_take_turns_with_the_bye()
        {
            var group = Enumerable.Range(0, 6).Select(i => $"a{i}").ToList();
            var met = group.ToDictionary(a => a, _ => new HashSet<string>());
            for (var round = 0; round < TrenchRules.Rounds; round++)
            {
                var pairs = TrenchRules.RoundPairs(group, round);
                Assert.Equal(3, pairs.Count);
                Assert.Equal(6, pairs.SelectMany(p => new[] { p.First, p.Second }).Distinct().Count());
                foreach (var (a, b) in pairs)
                {
                    Assert.True(met[a].Add(b));
                    Assert.True(met[b].Add(a));
                }
            }

            Assert.All(met.Values, rivals => Assert.Equal(5, rivals.Count));

            var odd = new List<string> { "x", "y", "z" };
            var byes = Enumerable.Range(0, 3).Select(r => TrenchRules.RoundPairs(odd, r).Single(p => p.First == TrenchRules.Bye || p.Second == TrenchRules.Bye))
                .Select(p => p.First == TrenchRules.Bye ? p.Second : p.First).ToList();
            Assert.Equal(3, byes.Distinct().Count());
            Assert.Empty(TrenchRules.RoundPairs(new List<string> { "alone" }, 0));
        }

        [Fact]
        public void A_strong_team_clears_a_lane_but_retires_after_its_fights()
        {
            var strong = new[] { Team("s", 30000) };
            var weak = new[] { Team("w1", 1000), Team("w2", 1200), Team("w3", 1400) };
            var lane = TrenchRules.FightLane(Troops, strong, weak, TrenchRules.DefaultFightsPerTeam, seed: 42);

            // Two wins, then the strong team retires; the weak side still has a team and takes the flag.
            Assert.Equal(2, lane.Fights.Count);
            Assert.All(lane.Fights, f => Assert.True(f.FirstWon));
            Assert.Equal("w1", lane.Fights[0].Second); // weakest first
            Assert.Equal(2, lane.Wins["s"]);
            Assert.Equal(TrenchRules.SecondFlag, lane.Flag);

            var three = TrenchRules.FightLane(Troops, strong, weak, fightsPerTeam: 3, seed: 42);
            Assert.Equal(TrenchRules.FirstFlag, three.Flag);
        }

        [Fact]
        public void Lanes_replay_the_same_with_the_same_seed_and_empty_lanes_go_to_whoever_is_there()
        {
            var a = new[] { Team("a1", 5000), Team("a2", 6000) };
            var b = new[] { Team("b1", 5200), Team("b2", 5900) };
            var first = TrenchRules.FightLane(Troops, a, b, 2, seed: 7);
            var again = TrenchRules.FightLane(Troops, a, b, 2, seed: 7);
            Assert.Equal(first.Flag, again.Flag);
            Assert.Equal(first.Fights.Select(f => (f.First, f.Second, f.FirstWon)), again.Fights.Select(f => (f.First, f.Second, f.FirstWon)));

            Assert.Equal(TrenchRules.FirstFlag, TrenchRules.FightLane(Troops, a, Array.Empty<TrenchRules.Team>(), 2, 1).Flag);
            Assert.Equal(TrenchRules.NoFlag, TrenchRules.FightLane(Troops, Array.Empty<TrenchRules.Team>(), Array.Empty<TrenchRules.Team>(), 2, 1).Flag);
        }

        [Fact]
        public void More_flags_win_three_points_and_equal_flags_draw()
        {
            TrenchRules.LaneResult Flag(int f) => new() { Flag = f };
            Assert.Equal((3, 0), TrenchRules.RoundPoints(new[] { Flag(1), Flag(1), Flag(2) }));
            Assert.Equal((0, 3), TrenchRules.RoundPoints(new[] { Flag(2), Flag(0), Flag(0) }));
            Assert.Equal((1, 1), TrenchRules.RoundPoints(new[] { Flag(1), Flag(2), Flag(0) }));
            Assert.True(TrenchRules.CanRegister(2, 10, TrenchRules.DefaultMinReactor));
            Assert.False(TrenchRules.CanRegister(3, 20, TrenchRules.DefaultMinReactor));
            Assert.False(TrenchRules.CanRegister(0, 9, TrenchRules.DefaultMinReactor));
        }
    }
}
