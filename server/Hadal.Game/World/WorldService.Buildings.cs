using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game.World
{
    /// <summary>
    /// Alliance buildings (docs/32 §2.3): the leader and logistics officers (docs/29 §2.3) place a Relay, Rig or Cannon on the alliance's land once the alliance is
    /// big and strong enough; members' teams sent to it ("reinforce") build it, then guard it; donations raise its level;
    /// enemies wear its durability down, at 0 it is damaged (no bonus) and mends over time. Beacons can be attacked too
    /// and fall at 0. The hub cannot be attacked.
    /// </summary>
    public sealed partial class WorldService
    {
        private AllianceStructure? BuildingOf(string allianceId, string kind) =>
            _structures.Values.FirstOrDefault(s => s.AllianceId == allianceId && s.Kind == kind);

        /// <summary>An alliance's power: its members' bases as last seen. Caller holds the lock.</summary>
        private long AlliancePower(string allianceId, string anyMember)
        {
            var members = _social.AllianceMatesOf(anyMember).Append(anyMember);
            return members.Sum(id => _bases.TryGetValue(id, out var b) ? b.Power : 0);
        }

        private string? PlaceBuilding(PlayerSession session, string alliance, bool builder, string kind, int x, int y, DateTimeOffset now)
        {
            if (!builder)
                return "NoPermission";
            if (!AllianceLandRules.IsBuilding(kind))
                return "MalformedCommand";
            if (HubOf(alliance) == null)
                return "NoHub";
            var (members, power) = AllianceLandRules.Unlock(kind);
            if (_social.MemberCount(alliance) < members || AlliancePower(alliance, session.PublicId) < power)
                return "BuildingLocked";
            if (BuildingOf(alliance, kind) != null)
                return "AlreadyBuilt";
            if (LandOwner(x, y) != alliance)
                return "NotOurs";
            if (!FreeForStructure(x, y))
                return "TileTaken";
            Add(new AllianceStructure
            {
                Id = "s" + Guid.NewGuid().ToString("N")[..10], AllianceId = alliance, Kind = kind, X = x, Y = y, Level = 1, ReadyAt = now, CreatedAt = now,
                PlacedBy = session.PublicId, Built = false, WorkAt = now, Durability = -1, DurabilityAt = now
            });
            _social.LogBattle(alliance, "building_placed", now, NameOf(session.PublicId) ?? string.Empty, kind, x.ToString(), y.ToString());
            return null;
        }

        /// <summary>Teams standing at a structure (arrived), with their troops. Caller holds the lock.</summary>
        private List<(PvpRecord Record, PvpTeam Team)> Guards(string structureId, DateTimeOffset at)
        {
            var result = new List<(PvpRecord, PvpTeam)>();
            foreach (var r in _pvp.Values.Where(r => r.Mode == PvpRules.Reinforce && r.TargetKind == PvpRules.TargetStructure && r.TargetId == structureId))
            {
                if (SessionOf(r.AttackerId)?.ReadTeam(r.Members[0].MarchId, at) is { } team && team.ArrivesAt <= at && team.GatheredAt > at)
                    result.Add((r, team));
            }

            return result;
        }

        /// <summary>
        /// Brings buildings up to <paramref name="now"/>: the work of the teams standing at unfinished ones, the mending of
        /// damaged ones. Caller holds the lock.
        /// </summary>
        private void AdvanceBuildings(DateTimeOffset now)
        {
            foreach (var s in _structures.Values.Where(s => AllianceLandRules.IsBuilding(s.Kind) && !s.Built).ToList())
            {
                if (now <= s.WorkAt)
                    continue;
                var troops = Guards(s.Id, now).Sum(g => g.Team.Troops.Values.Sum());
                s.Work += troops * (now - s.WorkAt).TotalSeconds;
                s.WorkAt = now;
                if (s.Work >= AllianceLandRules.BuildWork)
                {
                    s.Built = true;
                    s.Durability = AllianceLandRules.MaxDurability(s.Kind, s.Level);
                    s.DurabilityAt = now;
                    _social.LogBattle(s.AllianceId, "building_built", now, string.Empty, s.Kind, s.X.ToString(), s.Y.ToString());
                }

                SaveStructure(s);
            }
        }

        /// <summary>Durability now (mending over time); full when never hit.</summary>
        private static double DurabilityOf(AllianceStructure s, DateTimeOffset now)
        {
            var max = AllianceLandRules.MaxDurability(s.Kind, s.Level);
            if (s.Durability < 0)
                return max;
            var hours = Math.Max(0, (now - s.DurabilityAt).TotalHours);
            return Math.Min(max, s.Durability + max * AllianceLandRules.RepairPerHour * hours);
        }

        /// <summary>A building gives its bonus while built and not worn to 0.</summary>
        private bool Working(AllianceStructure? s, DateTimeOffset now) => s != null && s.Built && DurabilityOf(s, now) > 0;

        /// <summary>The bonus percent an alliance building gives at (x, y) to a member of its alliance (0 off the land or when damaged).</summary>
        private int LandBonus(string allianceId, string kind, int x, int y, DateTimeOffset now)
        {
            if (allianceId.Length == 0)
                return 0;
            EnsureLand(now);
            if (LandOwner(x, y) != allianceId)
                return 0;
            var building = BuildingOf(allianceId, kind);
            return Working(building, now) ? AllianceLandRules.BonusPercent(kind, building!.Level) : 0;
        }

        /// <summary>A base's land bonus as a battle side's percent: Relay for defenders, Cannon for attackers.</summary>
        private int BaseBonus(string playerId, string kind, DateTimeOffset now) =>
            _bases.TryGetValue(playerId, out var b) ? LandBonus(_social.AllianceIdOf(playerId), kind, b.X, b.Y, now) : 0;

        private (PvpLaunch? Launch, string? Reason) StructureTarget(WorldBaseRecord me, string myAlliance, string mode, string targetId,
            IReadOnlyList<string> heroes, DateTimeOffset? departs)
        {
            if (!_structures.TryGetValue(targetId, out var s))
                return (null, "UnknownStructure");
            if (s.AllianceId == myAlliance)
                return (null, "Ally");
            if (s.Kind == AllianceLandRules.Hub)
                return (null, "HubRegion");
            return (new PvpLaunch(mode, PvpRules.TargetStructure, s.Id, string.Empty, string.Empty, me.X, me.Y, s.X, s.Y, heroes, string.Empty,
                mode == PvpRules.Rally, departs, me.X, me.Y), null);
        }

        /// <summary>
        /// An attack on a building or beacon: the teams standing guard and the structure's own defence (a garrison of its
        /// level) fight; a win wears its durability down; a beacon at 0 falls, a building at 0 is damaged.
        /// </summary>
        private bool ResolveStructure(PvpRecord r, List<PvpTeam> teams, Dictionary<string, PlayerSession> sessions, DateTimeOffset at, DateTimeOffset now)
        {
            if (!_structures.TryGetValue(r.TargetId, out var s))
            {
                Miss(r, teams, sessions, at, now, r.Mode, "gone", null);
                return true;
            }

            AdvanceBuildings(at);
            var posts = Guards(s.Id, at);
            var guardSessions = Sessions(posts.Select(p => p.Team.OwnerId));
            if (guardSessions == null)
                return false;

            var troops = Catalog.Troops;
            var attacker = Side(teams.Select(t => t.Troops), teams[0].HeroPowers);
            attacker.AtkPercent += BaseBonus(r.AttackerId, AllianceLandRules.Cannon, at);
            var own = TerritoryRules.Garrison(Math.Max(1, s.Level), s.Built ? 0.5 : 0.2).Troops
                .ToDictionary(p => p.Key, p => (long)Math.Round(p.Value), StringComparer.Ordinal);
            var defenders = new List<IReadOnlyDictionary<string, long>> { own };
            defenders.AddRange(posts.Select(p => (IReadOnlyDictionary<string, long>)p.Team.Troops));
            var defence = Side(defenders, posts.Count > 0 ? posts[0].Team.HeroPowers : new List<int>());
            var result = CombatRules.Resolve(troops, attacker, defence, seed: CombatRules.SeedOf(r.Id));
            var attackLosses = PvpRules.ShareLosses(teams.Select(t => (IReadOnlyDictionary<string, long>)t.Troops).ToList(), result.AttackerLost);
            var defenceLosses = PvpRules.ShareLosses(defenders, result.DefenderLost);

            var fell = false;
            if (result.AttackerWon)
            {
                var surviving = teams.Select((t, i) => TroopRules.Power(troops, Minus(t.Troops, attackLosses[i]))).Sum();
                s.Durability = Math.Max(0, DurabilityOf(s, at) - AllianceLandRules.Damage(surviving));
                s.DurabilityAt = at;
                if (s.Durability <= 0 && s.Kind == AllianceLandRules.Beacon)
                {
                    fell = true;
                    DropStructure(s);
                }
                else
                    SaveStructure(s);
            }

            var attStart = Sum(teams.Select(t => t.Troops));
            var defStart = Sum(defenders);
            PvpReportDto Report(string kind, bool won) => new()
            {
                Id = r.Id, AtMs = at.ToUnixTimeMilliseconds(), Kind = kind, Outcome = won ? "won" : "lost", X = r.ToX, Y = r.ToY, Rounds = result.Rounds,
                Attacker = Stacks(attStart, result.AttackerLost), Defender = Stacks(defStart, result.DefenderLost),
                AttackerPower = TroopRules.Power(troops, attStart), DefenderPower = TroopRules.Power(troops, defStart), StrongholdId = s.Kind,
                StrongholdLevel = s.Level, Captured = fell
            };

            for (var i = 0; i < posts.Count; i++)
            {
                var (record, team) = posts[i];
                var (dead, wounded) = guardSessions[team.OwnerId].SettleTeam(team.MarchId, at, defenceLosses[i + 1], NoResources, turnHome: false, now);
                if (Minus(team.Troops, defenceLosses[i + 1]).Values.Sum() <= 0 || fell)
                {
                    if (fell)
                        guardSessions[team.OwnerId].SendHome(team.MarchId, at, s.X, s.Y, now);
                    Drop(record);
                }

                var report = Report("reinforce", !result.AttackerWon);
                (report.Dead, report.Wounded) = (dead, wounded);
                Tell(guardSessions[team.OwnerId], report, r.AttackerId, now);
            }

            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                var (dead, wounded) = sessions[team.OwnerId].SettleTeam(team.MarchId, at, attackLosses[i], NoResources, turnHome: true, now);
                var report = Report(r.Mode == PvpRules.Rally ? "rally" : "attack", result.AttackerWon);
                (report.Dead, report.Wounded) = (dead, wounded);
                report.Contribution = Contribution(team.Troops, attStart);
                Tell(sessions[team.OwnerId], report, string.Empty, now);
            }

            var args = new[] { NameOf(r.AttackerId) ?? string.Empty, s.Kind, r.ToX.ToString(), r.ToY.ToString() };
            _social.LogBattle(_social.AllianceIdOf(r.AttackerId), result.AttackerWon ? AllianceLogKinds.AttackWon : AllianceLogKinds.AttackLost, now, args);
            _social.LogBattle(s.AllianceId, result.AttackerWon ? AllianceLogKinds.DefenceLost : AllianceLogKinds.DefenceWon, now, args);
            return true;
        }

        /// <summary>Donation points raise a building's level (Relay, Rig, Cannon).</summary>
        private string? DonateBuilding(PlayerSession session, AllianceStructure s, long amount, HADAL.Shared.Enums.ResourceType type, DateTimeOffset now)
        {
            if (!s.Built)
                return "NotBuilt";
            if (s.Level >= AllianceLandRules.MaxBuildingLevel)
                return "MaxLevel";
            long room = 0;
            for (var l = s.Level; l < AllianceLandRules.MaxBuildingLevel; l++)
                room += AllianceLandRules.BuildingPoints(l);
            amount = Math.Min(amount, room - s.Points);
            if (!session.TryCharge(new[] { new KeyValuePair<HADAL.Shared.Enums.ResourceType, long>(type, amount) }, now, "alliance_gift"))
                return "InsufficientResources";
            var durability = DurabilityOf(s, now);
            s.Points += amount;
            while (s.Level < AllianceLandRules.MaxBuildingLevel && s.Points >= AllianceLandRules.BuildingPoints(s.Level))
            {
                s.Points -= AllianceLandRules.BuildingPoints(s.Level);
                s.Level++;
                durability += AllianceLandRules.MaxDurability(s.Kind, 1);
            }

            s.Durability = Math.Min(durability, AllianceLandRules.MaxDurability(s.Kind, s.Level));
            s.DurabilityAt = now;
            SaveStructure(s);
            return null;
        }

        private StructureDto WithBuilding(StructureDto dto, AllianceStructure s, DateTimeOffset now)
        {
            dto.UnderConstruction = !s.Built;
            dto.Work = (long)s.Work;
            dto.WorkNeeded = AllianceLandRules.IsBuilding(s.Kind) && !s.Built ? (long)AllianceLandRules.BuildWork : 0;
            dto.Durability = (long)DurabilityOf(s, now);
            dto.MaxDurability = (long)AllianceLandRules.MaxDurability(s.Kind, s.Level);
            dto.Guards = Guards(s.Id, now).Count;
            dto.Bonus = AllianceLandRules.BonusPercent(s.Kind, s.Level);
            if (AllianceLandRules.IsBuilding(s.Kind))
            {
                dto.PointsNext = s.Built && s.Level < AllianceLandRules.MaxBuildingLevel ? AllianceLandRules.BuildingPoints(s.Level) : 0;
                dto.Radius = 0;
            }

            return dto;
        }
    }
}
