using Hadal.Game.Events;
using HADAL.Shared.Simulation;

namespace Hadal.Game.World
{
    /// <summary>
    /// Abyss Siege waves (docs/29 §4.2, AbyssSiegeRules): the creatures fight every member's troops at home and the teams
    /// reinforcing that base, or on a hub wave the teams reinforcing the Alliance Hub. Shields do not matter, nothing is
    /// plundered and nobody dies. Waves leave no battle mail: twenty of them per base would bury the mailbox, so the
    /// event page tells how the siege stands.
    /// </summary>
    public sealed partial class WorldService
    {
        /// <summary>Fights one wave. Null while offline members' sessions are still loading (GameWorld asks again next tick).</summary>
        public SiegeWaveResult? FightSiegeWave(SiegeWaveOrder order, DateTimeOffset now)
        {
            lock (_gate)
            {
                var points = new Dictionary<string, long>(StringComparer.Ordinal);
                var breached = new List<string>();
                if (AbyssSiegeRules.IsHubWave(order.Wave))
                {
                    if (HubOf(order.AllianceId) is not { } hub)
                        return new SiegeWaveResult(order, points, breached, null);
                    var held = FightHub(order, hub, points, now);
                    return held == null ? null : new SiegeWaveResult(order, points, breached, held);
                }

                // Elite waves hit only the members who are here to answer them.
                var targets = order.Members.Where(id => _bases.ContainsKey(id)
                                                        && (!AbyssSiegeRules.IsEliteWave(order.Wave) || _online.ContainsKey(id))).ToList();
                var stations = _pvp.Values.Where(s => s.Mode == PvpRules.Reinforce && s.TargetKind == PvpRules.TargetBase && targets.Contains(s.TargetId)).ToList();
                var sessions = Sessions(targets.Concat(stations.Select(s => s.AttackerId)));
                if (sessions == null)
                    return null;

                foreach (var id in targets)
                    FightAtBase(order, id, stations, sessions, points, breached, now);
                return new SiegeWaveResult(order, points, breached, null);
            }
        }

        private void FightAtBase(SiegeWaveOrder order, string playerId, List<PvpRecord> stations, Dictionary<string, PlayerSession> sessions,
            Dictionary<string, long> points, List<string> breached, DateTimeOffset now)
        {
            var defender = sessions[playerId];
            var snap = defender.SnapshotBase(now);
            if (snap.Reactor < order.MinReactor)
                return;
            var posts = stations.Where(s => s.TargetId == playerId)
                .Select(s => (Record: s, Team: sessions[s.AttackerId].ReadTeam(s.Members[0].MarchId, now)))
                .Where(p => p.Team != null && p.Team.ArrivesAt <= now && p.Team.GatheredAt > now).ToList();
            var defenders = new List<IReadOnlyDictionary<string, long>> { snap.Troops };
            defenders.AddRange(posts.Select(p => (IReadOnlyDictionary<string, long>)p.Team!.Troops));
            if (defenders.All(d => d.Values.Sum() <= 0))
            {
                breached.Add(playerId); // nobody home: the wave walks in
                return;
            }

            var defence = Side(defenders, snap.HeroPowers);
            defence.DefPercent += BaseBonus(playerId, AllianceLandRules.Relay, now);
            var result = CombatRules.Resolve(Catalog.Troops, AbyssSiegeRules.Army(order.Difficulty, order.Wave), defence,
                seed: CombatRules.SeedOf($"{order.EventId}:{order.AllianceId}:{order.Wave}:{playerId}"));
            var losses = PvpRules.ShareLosses(defenders, result.DefenderLost);
            defender.ApplyBaseLosses(losses[0], new Dictionary<HADAL.Shared.Enums.ResourceType, long>(), now, spare: true);
            for (var i = 0; i < posts.Count; i++)
            {
                var team = posts[i].Team!;
                sessions[team.OwnerId].SettleTeam(team.MarchId, now, losses[i + 1], NoResources, turnHome: false, now, spare: true);
                if (Minus(team.Troops, losses[i + 1]).Values.Sum() <= 0)
                    Drop(posts[i].Record); // wiped out: the team heads home empty
            }

            CreditSiegeKills(result, defenders, posts.Select(p => p.Team!.OwnerId).Prepend(playerId).ToList(), ownerIndex: 0, points);
            if (result.AttackerWon)
                breached.Add(playerId);
        }

        /// <summary>A hub wave: the teams reinforcing the hub against it. No teams, or a lost fight, and the hub falls (null: sessions loading).</summary>
        private bool? FightHub(SiegeWaveOrder order, AllianceStructure hub, Dictionary<string, long> points, DateTimeOffset now)
        {
            var records = _pvp.Values.Where(r => r.Mode == PvpRules.Reinforce && r.TargetKind == PvpRules.TargetStructure && r.TargetId == hub.Id).ToList();
            if (Sessions(records.Select(r => r.AttackerId)) is not { } sessions)
                return null;
            var posts = Guards(hub.Id, now);
            if (posts.Count == 0)
                return false;

            var defenders = posts.Select(p => (IReadOnlyDictionary<string, long>)p.Team.Troops).ToList();
            var result = CombatRules.Resolve(Catalog.Troops, AbyssSiegeRules.Army(order.Difficulty, order.Wave), Side(defenders, posts[0].Team.HeroPowers),
                seed: CombatRules.SeedOf($"{order.EventId}:{order.AllianceId}:{order.Wave}:hub"));
            var losses = PvpRules.ShareLosses(defenders, result.DefenderLost);
            for (var i = 0; i < posts.Count; i++)
            {
                var team = posts[i].Team;
                sessions[team.OwnerId].SettleTeam(team.MarchId, now, losses[i], NoResources, turnHome: false, now, spare: true);
                if (Minus(team.Troops, losses[i]).Values.Sum() <= 0)
                    Drop(posts[i].Record);
            }

            CreditSiegeKills(result, defenders, posts.Select(p => p.Team.OwnerId).ToList(), ownerIndex: -1, points);
            return !result.AttackerWon;
        }

        /// <summary>
        /// The creatures killed, shared by troops fielded: the base's owner (<paramref name="ownerIndex"/>) scores in full,
        /// reinforcing teams at AbyssSiegeRules.ReinforceShare.
        /// </summary>
        private static void CreditSiegeKills(BattleResult result, List<IReadOnlyDictionary<string, long>> defenders, List<string> ids, int ownerIndex,
            Dictionary<string, long> points)
        {
            var kills = result.AttackerLost.Values.Sum();
            var fielded = defenders.Sum(d => d.Values.Sum());
            if (kills <= 0 || fielded <= 0)
                return;
            for (var i = 0; i < ids.Count; i++)
            {
                var share = (double)defenders[i].Values.Sum() / fielded * (i == ownerIndex ? 1 : AbyssSiegeRules.ReinforceShare);
                points[ids[i]] = (points.TryGetValue(ids[i], out var had) ? had : 0) + (long)Math.Round(kills * share);
            }
        }
    }
}
