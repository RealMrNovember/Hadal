using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Trench Conquest (Hendek Fethi, docs/29 §4.6), shared by the server (groups, rounds, lane battles) and the client
    /// (the league page).
    /// <para>
    /// Members of an alliance register one team each into one of <see cref="Lanes"/> trenches. A team is a snapshot (its
    /// heroes and the troops they would lead), so troops never die. Alliances of a kingdom are grouped by power, six to a
    /// group, and meet each other over <see cref="Rounds"/> rounds (round robin; an odd group gives one alliance a bye each
    /// round). In a round both alliances fight lane by lane:
    /// </para>
    /// <code>
    ///   each side's teams queue weakest first
    ///   the two front teams fight (CombatRules); the loser is out, the winner fights on
    ///   a team that has fought FightsPerTeam times retires even when it won, unless that win cleared the lane
    ///   the side with teams left takes the lane's flag (nobody there at all: no flag)
    /// </code>
    /// More flags wins the round (<see cref="WinPoints"/>), equal flags draw (<see cref="DrawPoints"/>), a bye counts as a
    /// win. The group ranks by points, then flags, then fights won.
    /// </summary>
    public static class TrenchRules
    {
        public const int Lanes = 3;
        public const int GroupSize = 6;
        public const int Rounds = 5;
        public const int DefaultMinReactor = 10;
        public const int DefaultMinRegistered = 15;
        public const int DefaultFightsPerTeam = 2;
        public const int WinPoints = 3;
        public const int DrawPoints = 1;

        /// <summary>The empty seat of an odd group: the alliance drawn against it has the round off and wins it.</summary>
        public const string Bye = "~bye";

        /// <summary>LaneResult.Flag values.</summary>
        public const int NoFlag = 0;
        public const int FirstFlag = 1;
        public const int SecondFlag = 2;

        /// <summary>A registered team: who, which lane, and the snapshot it fights with.</summary>
        public sealed class Team
        {
            public string PlayerId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Lane { get; set; }
            public long Power { get; set; }
            public Dictionary<string, long> Troops { get; set; } = new(StringComparer.Ordinal);
            public List<int> HeroPowers { get; set; } = new();
        }

        /// <summary>One fight of a lane: the two players and who won.</summary>
        public sealed class Fight
        {
            public string First { get; set; } = string.Empty;
            public string Second { get; set; } = string.Empty;
            public bool FirstWon { get; set; }
        }

        public sealed class LaneResult
        {
            public int Flag { get; set; }
            public List<Fight> Fights { get; } = new();

            /// <summary>Fights won per player (the members' kill rewards).</summary>
            public Dictionary<string, int> Wins { get; } = new(StringComparer.Ordinal);
        }

        /// <summary>
        /// The kingdom's groups: alliances strongest first, six to a group. A last group of one joins the group before
        /// it (a group of seven), so nobody sits a whole league alone.
        /// </summary>
        public static List<List<string>> Groups(IEnumerable<KeyValuePair<string, long>> alliancePowers)
        {
            var ordered = alliancePowers.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key).ToList();
            var groups = new List<List<string>>();
            for (var i = 0; i < ordered.Count; i += GroupSize)
                groups.Add(ordered.Skip(i).Take(GroupSize).ToList());
            if (groups.Count > 1 && groups[groups.Count - 1].Count == 1)
            {
                groups[groups.Count - 2].AddRange(groups[groups.Count - 1]);
                groups.RemoveAt(groups.Count - 1);
            }

            return groups;
        }

        /// <summary>
        /// The pairs of round <paramref name="round"/> (0-based) in a group, by the circle method: every alliance meets
        /// every other once per cycle; with an odd count one alliance meets <see cref="Bye"/>. A group of one has no pairs.
        /// </summary>
        public static List<(string First, string Second)> RoundPairs(IReadOnlyList<string> group, int round)
        {
            var seats = group.ToList();
            if (seats.Count < 2)
                return new List<(string, string)>();
            if (seats.Count % 2 == 1)
                seats.Add(Bye);
            var n = seats.Count;
            var turn = ((round % (n - 1)) + (n - 1)) % (n - 1);
            // Seat 0 stays; the others rotate one place per round.
            var rotating = seats.Skip(1).ToList();
            var shifted = rotating.Skip(rotating.Count - turn).Concat(rotating.Take(rotating.Count - turn)).ToList();
            var order = new List<string> { seats[0] };
            order.AddRange(shifted);
            var pairs = new List<(string, string)>();
            for (var i = 0; i < n / 2; i++)
                pairs.Add((order[i], order[n - 1 - i]));
            return pairs;
        }

        /// <summary>
        /// One lane: both queues weakest first, front against front, the loser out, the winner on until it has fought
        /// <paramref name="fightsPerTeam"/> times. Fights alternate who attacks (the first side on even fights); each fight
        /// has its own seed so a replay gives the same lane.
        /// </summary>
        public static LaneResult FightLane(TroopCatalogData troops, IReadOnlyList<Team> first, IReadOnlyList<Team> second, int fightsPerTeam, int seed)
        {
            var result = new LaneResult();
            var a = Queue(first);
            var b = Queue(second);
            var limit = Math.Max(1, fightsPerTeam);
            int aFought = 0, bFought = 0, index = 0;
            while (a.Count > 0 && b.Count > 0)
            {
                var x = a.Peek();
                var y = b.Peek();
                var firstAttacks = index % 2 == 0;
                var attacker = MonsterBattle.Team(firstAttacks ? x.Troops : y.Troops, firstAttacks ? x.HeroPowers : y.HeroPowers);
                var defender = MonsterBattle.Team(firstAttacks ? y.Troops : x.Troops, firstAttacks ? y.HeroPowers : x.HeroPowers);
                var battle = CombatRules.Resolve(troops, attacker, defender, seed: unchecked(seed + index * 7919));
                var firstWon = battle.AttackerWon == firstAttacks;
                result.Fights.Add(new Fight { First = x.PlayerId, Second = y.PlayerId, FirstWon = firstWon });
                var winner = firstWon ? x.PlayerId : y.PlayerId;
                result.Wins[winner] = (result.Wins.TryGetValue(winner, out var w) ? w : 0) + 1;
                aFought++;
                bFought++;
                // The winner retires after its last fight, unless that fight cleared the lane: then it holds the flag.
                if (firstWon)
                {
                    b.Dequeue();
                    bFought = 0;
                    if (aFought >= limit && b.Count > 0)
                    {
                        a.Dequeue();
                        aFought = 0;
                    }
                }
                else
                {
                    a.Dequeue();
                    aFought = 0;
                    if (bFought >= limit && a.Count > 0)
                    {
                        b.Dequeue();
                        bFought = 0;
                    }
                }

                index++;
            }

            result.Flag = a.Count > 0 ? FirstFlag : b.Count > 0 ? SecondFlag : NoFlag;
            return result;
        }

        /// <summary>Round points for both sides from their flags: more flags wins, equal flags draw.</summary>
        public static (int First, int Second) RoundPoints(IReadOnlyList<LaneResult> lanes)
        {
            var a = lanes.Count(l => l.Flag == FirstFlag);
            var b = lanes.Count(l => l.Flag == SecondFlag);
            return a > b ? (WinPoints, 0) : b > a ? (0, WinPoints) : (DrawPoints, DrawPoints);
        }

        /// <summary>Whether a team may enter the league: a lane in range and a reactor at or above the minimum.</summary>
        public static bool CanRegister(int lane, int reactor, int minReactor) => lane >= 0 && lane < Lanes && reactor >= minReactor;

        private static Queue<Team> Queue(IReadOnlyList<Team> teams) =>
            new(teams.OrderBy(t => t.Power).ThenBy(t => t.PlayerId, StringComparer.Ordinal));
    }
}
