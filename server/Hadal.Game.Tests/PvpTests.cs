using Hadal.Game.Persistence;
using Hadal.Game.World;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>PvP on the server timeline (docs/30): battles at the arrival instant, shields, teleports, offline players.</summary>
    public sealed class PvpTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;
        private static readonly string Light = TroopRules.Key("armored", 1);

        public PvpTests()
        {
            _world = new GameWorld(Catalog, new InMemoryPlayerRepository(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity, long troops = 5000, int reactor = 10)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            var core = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            core.Level = reactor;
            session.StateForTests.UnsavedBuildings.Add(core); // kept when the player is unloaded and loaded again
            // Five seasoned heroes: a team leads up to their command, the rest stays home.
            foreach (var id in Catalog.Heroes.Select(h => h.Id).Where(id => session.StateForTests.Heroes.All(h => h.HeroId != id)).Take(5))
                session.StateForTests.Heroes.Add(new OwnedHeroState { HeroId = id, Level = 60, Stars = 5, Slot = -1 });
            session.StateForTests.Troops[Light] = troops;
            session.StateForTests.Wallet[ResourceType.Titanium] = 1_000_000;
            session.StateForTests.Wallet[ResourceType.Hadalite] = 100_000;
            Send(session, new ViewWorldCommand { CommandId = "v", X = -1, Y = -1 }, CommandType.ViewWorld);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Where(d => d.Ack() != null).Last().Ack()!;

        private void Attack(PlayerSession attacker, string targetId, string kind = PvpRules.TargetBase, string mode = PvpRules.Attack) =>
            Send(attacker, new MarchPvpCommand { CommandId = "a" + Guid.NewGuid().ToString("N"), Mode = mode, TargetKind = kind, TargetId = targetId },
                CommandType.MarchPvp);

        private static MarchState Pvp(PlayerSession session) => session.MarchesForTests.Single(m => m.Pvp != null);

        private void At(DateTimeOffset when)
        {
            _clock.UtcNow = when;
            _world.Tick();
        }

        private static PvpReportDto LastReport(PlayerSession session) => session.StateForTests.WarLog.First();

        private void Ally(PlayerSession leader, RecordingConnection leaderConnection, params PlayerSession[] members)
        {
            Send(leader, new CreateAllianceCommand { CommandId = "c", Name = "Deep Wardens " + leader.PublicId[..4], Tag = "D" + leader.PublicId[..3] },
                CommandType.CreateAlliance);
            var id = TestSupport.Serializer.Deserialize<AllianceDto>(leaderConnection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
            foreach (var m in members)
                Send(m, new JoinAllianceCommand { CommandId = "j", AllianceId = id }, CommandType.JoinAlliance);
        }

        [Fact]
        public async Task An_attack_is_fought_at_the_arrival_instant_and_plunders()
        {
            var (a, aConnection) = await Connect("pvp-a");
            var (b, _) = await Connect("pvp-b", troops: 50);
            Attack(a, b.PublicId);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
            var march = Pvp(a);

            At(march.ArrivesAt.AddMilliseconds(-1));
            Assert.Empty(b.StateForTests.WarLog);
            Assert.Equal(50, b.StateForTests.Troops[Light]);

            At(march.ArrivesAt);
            var defence = LastReport(b);
            Assert.Equal("defence", defence.Kind);
            Assert.Equal("lost", defence.Outcome);
            Assert.Equal(a.PublicId, defence.OpponentId);
            Assert.True((b.StateForTests.Troops.TryGetValue(Light, out var left) ? left : 0) < 50);

            var attack = LastReport(a);
            Assert.Equal("won", attack.Outcome);
            var taken = attack.Plunder.Sum(p => p.Value);
            Assert.True(taken > 0);
            Assert.Equal(taken, Pvp(a).Cargo.Values.Sum());
            Assert.Equal(attack.AtMs, march.ArrivesAt.ToUnixTimeMilliseconds());

            // The team comes home with the plunder; it unloads only after the battle was settled.
            At(Pvp(a).HomeAt);
            Assert.DoesNotContain(a.MarchesForTests, m => m.Pvp != null);
            Assert.True(a.StateForTests.Wallet[ResourceType.Titanium] >= 1_000_000 + attack.Plunder.Where(p => p.Key == ResourceType.Titanium).Sum(p => p.Value) - 1);
        }

        [Fact]
        public async Task A_shield_raised_one_millisecond_before_arrival_blocks_but_at_the_instant_is_too_late()
        {
            var (a, _) = await Connect("sh-a");
            var (b, bConnection) = await Connect("sh-b", troops: 50);
            b.StateForTests.Items[PvpRules.Shield8h] = 2;

            Attack(a, b.PublicId);
            var first = Pvp(a);
            _clock.UtcNow = first.ArrivesAt.AddMilliseconds(-1);
            Send(b, new UseShieldCommand { CommandId = "s1", ItemId = PvpRules.Shield8h }, CommandType.UseShield);
            Assert.Equal(CommandResultCode.Accepted, LastAck(bConnection).Result);
            At(first.ArrivesAt);
            Assert.Equal("shielded", LastReport(a).Outcome);
            Assert.Equal(50, b.StateForTests.Troops[Light]);

            // A second attack once the shield is gone: the shield command arrives at the very instant of the battle.
            At(Pvp(a).HomeAt);
            _clock.UtcNow = b.StateForTests.ShieldUntil!.Value.AddSeconds(1);
            Attack(a, b.PublicId);
            var second = Pvp(a);
            _clock.UtcNow = second.ArrivesAt;
            Send(b, new UseShieldCommand { CommandId = "s2", ItemId = PvpRules.Shield8h }, CommandType.UseShield);
            Assert.Equal("won", LastReport(a).Outcome);
            Assert.Equal("lost", LastReport(b).Outcome);
            Assert.True(b.StateForTests.ShieldUntil > second.ArrivesAt); // raised, but after the battle
        }

        [Fact]
        public async Task Attacking_breaks_your_shield_and_shields_and_beginners_cannot_be_attacked()
        {
            var (a, aConnection) = await Connect("br-a");
            var (b, _) = await Connect("br-b");
            var (rookie, _) = await Connect("br-rookie", reactor: PvpRules.BeginnerReactor - 1);
            a.StateForTests.ShieldUntil = _clock.UtcNow.AddHours(8);
            b.StateForTests.ShieldUntil = _clock.UtcNow.AddHours(8);

            Attack(a, b.PublicId);
            Assert.Equal("TargetShielded", LastAck(aConnection).Reason);
            Attack(a, rookie.PublicId);
            Assert.Equal("TargetBeginner", LastAck(aConnection).Reason);
            Assert.True(a.StateForTests.ShieldUntil > _clock.UtcNow); // nothing launched, shield intact

            b.StateForTests.ShieldUntil = null;
            Attack(a, b.PublicId);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
            Assert.Null(a.StateForTests.ShieldUntil);
        }

        [Fact]
        public async Task A_teleport_before_arrival_makes_the_attack_miss()
        {
            var (a, _) = await Connect("tp-att");
            var (b, bConnection) = await Connect("tp-def", troops: 50);
            Attack(a, b.PublicId);
            var march = Pvp(a);
            _clock.UtcNow = march.ArrivesAt.AddSeconds(-1);
            Send(b, new TeleportCommand { CommandId = "t", Mode = TeleportRules.Random }, CommandType.Teleport);
            Assert.Equal(CommandResultCode.Accepted, LastAck(bConnection).Result);
            At(march.ArrivesAt);
            Assert.Equal("moved", LastReport(a).Outcome);
            Assert.Empty(b.StateForTests.WarLog);
            Assert.True(Pvp(a).HomeAt > march.ArrivesAt);
        }

        [Fact]
        public async Task An_offline_defender_is_loaded_and_fights_on_time()
        {
            var (a, _) = await Connect("off-a");
            var (b, bConnection) = await Connect("off-b", troops: 50);
            await _world.DisconnectAsync(b, bConnection, default);
            Assert.Null(_world.Map.SessionOf(b.PublicId));

            Attack(a, b.PublicId);
            var march = Pvp(a);
            await _world.SummonAsync(b.PublicId, default);
            At(march.ArrivesAt);
            Assert.Equal("won", LastReport(a).Outcome);

            var loaded = _world.Map.SessionOf(b.PublicId)!;
            Assert.Equal("lost", LastReport(loaded).Outcome);
            var back = await _world.ConnectAsync("off-b", new RecordingConnection(), default, "off-b");
            Assert.Same(loaded, back);
            Assert.True((back.StateForTests.Troops.TryGetValue(Light, out var left) ? left : 0) < 50);
        }

        [Fact]
        public async Task A_gathering_team_can_be_attacked_and_robbed()
        {
            var (a, aConnection) = await Connect("team-a", troops: 20_000);
            var (b, _) = await Connect("team-b", troops: 2000);
            var (bx, by) = _world.Map.PositionOf(b.PublicId)!.Value;
            var node = _world.Map.NodesForTests().Where(n => n.Remaining > 0 && n.BusyUntil == null && n.Type != "hadalit_damari")
                .OrderBy(n => GatherRules.Tiles(bx, by, n.X, n.Y)).First();
            Send(b, new MarchGatherCommand { CommandId = "g", NodeId = node.Id }, CommandType.MarchGather);
            var gather = b.MarchesForTests.Single();
            At(gather.ArrivesAt.AddSeconds(1));

            // The attacker jumps next to the node so the team is still at work when it arrives.
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (2, 0), (0, 2), (-2, 0), (0, -2) })
            {
                Send(a, new TeleportCommand { CommandId = "t" + dx + "_" + dy, Mode = TeleportRules.Target, X = node.X + dx, Y = node.Y + dy }, CommandType.Teleport);
                if (LastAck(aConnection).Result == CommandResultCode.Accepted)
                    break;
            }

            Attack(a, gather.Id, PvpRules.TargetTeam);
            var march = Pvp(a);
            At(march.ArrivesAt);
            Assert.Equal("won", LastReport(a).Outcome);
            Assert.Equal("team", LastReport(b).Kind);
            var beaten = b.MarchesForTests.Single();
            Assert.Equal(march.ArrivesAt, beaten.GatheredAt); // turned home at the battle
            Assert.Equal(LastReport(a).Plunder.Sum(p => p.Value), Pvp(a).Cargo.Values.Sum());
            Assert.Null(_world.Map.NodeForTests(node.Id)!.BusyUntil);
        }

        [Fact]
        public async Task Reinforcements_fight_at_the_base_they_guard()
        {
            var (a, _) = await Connect("rf-a", troops: 3000);
            var (b, bConnection) = await Connect("rf-b", troops: 100);
            var (c, cConnection) = await Connect("rf-c", troops: 5000);
            Ally(b, bConnection, c);
            Send(c, new MarchPvpCommand { CommandId = "r", Mode = PvpRules.Reinforce, TargetId = b.PublicId }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(cConnection).Result);
            var guard = c.MarchesForTests.Single();
            At(guard.ArrivesAt);

            Attack(a, b.PublicId);
            var march = Pvp(a);
            At(march.ArrivesAt);
            var guardReport = LastReport(c);
            Assert.Equal("reinforce", guardReport.Kind);
            Assert.Equal(LastReport(b).Outcome, guardReport.Outcome);
            Assert.True(guardReport.Dead + guardReport.Wounded > 0);
            Assert.True(c.MarchesForTests.Single().Troops.Values.Sum() < guard.Troops.Values.Sum() || guardReport.Dead + guardReport.Wounded > 0);
        }

        [Fact]
        public async Task A_rally_sets_out_together_and_shares_the_battle()
        {
            var (lead, leadConnection) = await Connect("ra-lead", troops: 2000);
            var (mate, mateConnection) = await Connect("ra-mate", troops: 2000);
            var (target, _) = await Connect("ra-target", troops: 200);
            Ally(lead, leadConnection, mate);
            Send(mate, new TeleportCommand { CommandId = "t", Mode = TeleportRules.Alliance, MemberId = lead.PublicId }, CommandType.Teleport);

            Send(lead, new MarchPvpCommand { CommandId = "s", Mode = PvpRules.Rally, TargetKind = PvpRules.TargetBase, TargetId = target.PublicId, MusterSeconds = 300 },
                CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leadConnection).Result);
            var rally = _world.Map.PvpForTests().Single();
            Send(mate, new MarchPvpCommand { CommandId = "rally-join", Mode = PvpRules.Join, TargetId = rally.Id }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateConnection).Result);
            Assert.Equal(2, _world.Map.PvpForTests().Single().Members.Count);

            // The target sees one rally coming, departing at the muster end.
            Assert.Single(target.PvpIncomingForTests);
            var arrives = _world.Map.PvpForTests().Single().ArrivesAt;
            Assert.Equal(arrives, Pvp(lead).ArrivesAt);
            Assert.Equal(arrives, Pvp(mate).ArrivesAt);

            At(arrives);
            Assert.Equal("rally", LastReport(lead).Kind);
            Assert.Equal("won", LastReport(mate).Outcome);
            Assert.Equal("lost", LastReport(target).Outcome);
            Assert.Empty(target.PvpIncomingForTests);
        }

        [Fact]
        public async Task Battles_go_into_both_alliances_battle_logs()
        {
            var (a, aConnection) = await Connect("log-a");
            var (b, bConnection) = await Connect("log-b", troops: 50);
            Ally(a, aConnection);
            Ally(b, bConnection);
            Attack(a, b.PublicId);
            At(Pvp(a).ArrivesAt);

            var (x, y) = _world.Map.PositionOf(b.PublicId)!.Value;
            var mine = _world.Social.LogOf(_world.Social.AllianceIdOf(a.PublicId), AllianceLogKinds.Battle).Single();
            Assert.Equal(AllianceLogKinds.AttackWon, mine.Type);
            Assert.Equal(new[] { "log-a", "log-b", x.ToString(), y.ToString() }, mine.Args);
            var theirs = _world.Social.LogOf(_world.Social.AllianceIdOf(b.PublicId), AllianceLogKinds.Battle).Single();
            Assert.Equal(AllianceLogKinds.DefenceLost, theirs.Type);
        }

        [Fact]
        public async Task Battles_due_together_are_fought_in_arrival_order()
        {
            var (a, _) = await Connect("ord-a", troops: 3000);
            var (c, _) = await Connect("ord-c", troops: 3000);
            var (b, _) = await Connect("ord-b", troops: 10);
            Attack(a, b.PublicId);
            Attack(c, b.PublicId);
            var first = new[] { Pvp(a), Pvp(c) }.OrderBy(m => m.ArrivesAt).ThenBy(m => m.Id, StringComparer.Ordinal).First();

            // Both are due by now; the earlier one beats the garrison, the later one finds nobody home.
            At(new[] { Pvp(a).ArrivesAt, Pvp(c).ArrivesAt }.Max());
            Assert.Equal(2, b.StateForTests.WarLog.Count);
            Assert.Equal(first.ArrivesAt.ToUnixTimeMilliseconds(), b.StateForTests.WarLog.Last().AtMs);
            Assert.True(b.StateForTests.WarLog.Last().Defender.Sum(s => s.Start) > 0);
        }

        private TerritoryDto Territory(PlayerSession session, RecordingConnection connection)
        {
            Send(session, new ViewTerritoryCommand { CommandId = "vt" + Guid.NewGuid().ToString("N") }, CommandType.ViewTerritory);
            return TestSupport.Serializer.Deserialize<TerritoryDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.Territory).Payload);
        }

        [Fact]
        public void Regions_cover_the_map_and_touch_symmetrically()
        {
            var size = Catalog.World.size;
            var map = TerritoryRules.Map(size);
            Assert.Equal(TerritoryRules.Cells * TerritoryRules.Cells, map.Distinct().Count());
            var adjacency = TerritoryRules.Adjacency(size);
            for (var i = 0; i < adjacency.Length; i++)
                foreach (var j in adjacency[i])
                    Assert.Contains(i, adjacency[j]);
            Assert.Equal(TerritoryRules.ThroneLevel, TerritoryRules.Level(Catalog.World, TerritoryRules.RegionOf(size, size / 2, size / 2)));
            var seeds = TerritoryRules.Seeds(size);
            for (var i = 0; i < seeds.Length; i++)
                Assert.Equal(i, TerritoryRules.RegionOf(size, seeds[i].X, seeds[i].Y));
        }

        /// <summary>Sends a land command and returns its rejection reason (null when accepted).</summary>
        private string? Land(PlayerSession session, RecordingConnection connection, string action, int x = 0, int y = 0, string id = "", string resource = "",
            long amount = 0)
        {
            Send(session, new TerritoryCommand
            {
                CommandId = "t" + Guid.NewGuid().ToString("N"), Action = action, X = x, Y = y, StrongholdId = id, Resource = resource, Amount = amount
            }, CommandType.Territory);
            var ack = LastAck(connection);
            return ack.Result == CommandResultCode.Accepted ? null : ack.Reason;
        }

        /// <summary>Plants the hub on the first free tile around (cx, cy); returns where.</summary>
        private (int X, int Y) PlantHub(PlayerSession lead, RecordingConnection connection, int cx, int cy)
        {
            for (var r = 0; r < 12; r++)
                for (var dx = -r; dx <= r; dx++)
                    for (var dy = -r; dy <= r; dy++)
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && Land(lead, connection, AllianceLandRules.PlaceHub, cx + dx, cy + dy) == null)
                            return (cx + dx, cy + dy);
            throw new InvalidOperationException("no free tile for the hub");
        }

        private async Task<(PlayerSession Lead, RecordingConnection Connection, PlayerSession Mate, RecordingConnection MateConnection)> Alliance(string prefix,
            long troops = 5000)
        {
            var (lead, leadConnection) = await Connect(prefix + "-lead", troops);
            var (mate, mateConnection) = await Connect(prefix + "-mate");
            var (third, _) = await Connect(prefix + "-third");
            Ally(lead, leadConnection, mate, third);
            return (lead, leadConnection, mate, mateConnection);
        }

        [Fact]
        public async Task Only_the_leader_and_logistics_officers_place_alliance_land()
        {
            var (lead, leadConnection, mate, mateConnection) = await Alliance("duty");
            var (lx, ly) = _world.Map.PositionOf(lead.PublicId)!.Value;

            // An officer on another duty may not plant the hub (docs/29 §2.3) ...
            Send(lead, new SetMemberRankCommand { CommandId = "r" + Guid.NewGuid().ToString("N"), MemberId = mate.PublicId, Rank = AlliancePermissions.Officer,
                Role = AlliancePermissions.War }, CommandType.SetMemberRank);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leadConnection).Result);
            Assert.Equal("NoPermission", Land(mate, mateConnection, AllianceLandRules.PlaceHub, lx + 3, ly));

            // ... the logistics officer may; the leader always may (here: refused only because the hub exists).
            Send(lead, new SetMemberRankCommand { CommandId = "r" + Guid.NewGuid().ToString("N"), MemberId = mate.PublicId, Rank = AlliancePermissions.Officer,
                Role = AlliancePermissions.Logistics }, CommandType.SetMemberRank);
            var hub = PlantHub(mate, mateConnection, lx + 3, ly);
            Assert.Equal(_world.Social.AllianceIdOf(lead.PublicId), _world.Map.LandOwnerForTests(hub.X, hub.Y));
            Assert.Equal("HasTerritory", Land(lead, leadConnection, AllianceLandRules.PlaceHub, hub.X + 1, hub.Y + 1));
        }

        [Fact]
        public async Task The_rally_point_is_the_leaders_base_until_the_hub_is_planted()
        {
            var (lead, leadConnection, diver, diverConnection) = await Alliance("rp");
            var before = Territory(diver, diverConnection);
            Assert.Equal(_world.Map.PositionOf(lead.PublicId), (before.RallyX, before.RallyY));
            Assert.False(before.RallyIsHub);

            var (lx, ly) = _world.Map.PositionOf(lead.PublicId)!.Value;
            var hub = PlantHub(lead, leadConnection, lx + 3, ly);
            var after = Territory(diver, diverConnection);
            Assert.True(after.RallyIsHub);
            Assert.Equal(hub, (after.RallyX, after.RallyY));
        }

        [Fact]
        public async Task The_hub_claims_land_that_grows_with_its_level_and_built_connected_beacons()
        {
            var (lead, connection, mate, mateConnection) = await Alliance("land");
            var alliance = _world.Social.AllianceIdOf(lead.PublicId);
            var (lx, ly) = _world.Map.PositionOf(lead.PublicId)!.Value;
            Assert.Equal("NoPermission", Land(mate, mateConnection, AllianceLandRules.PlaceHub, lx + 3, ly));
            var (hx, hy) = PlantHub(lead, connection, lx + 3, ly);
            var r1 = AllianceLandRules.HubRadius(1);
            Assert.Equal(alliance, _world.Map.LandOwnerForTests(hx + r1, hy));
            Assert.Equal(string.Empty, _world.Map.LandOwnerForTests(hx + r1 + 1, hy));
            Assert.Equal("HasTerritory", Land(lead, connection, AllianceLandRules.PlaceHub, hx + 1, hy + 1));

            // Donations raise the hub's level and its square.
            var hubId = _world.Map.StructuresForTests().Single(s => s.Kind == AllianceLandRules.Hub).Id;
            Assert.Null(Land(mate, mateConnection, AllianceLandRules.Donate, id: hubId, resource: "Titanium", amount: AllianceLandRules.HubPoints(1)));
            Assert.Equal(2, _world.Map.StructuresForTests().Single(s => s.Id == hubId).Level);
            Assert.Equal(alliance, _world.Map.LandOwnerForTests(hx + r1 + 1, hy));

            // A beacon next to the land claims its square once built.
            var r2 = AllianceLandRules.HubRadius(2);
            var bx = hx + r2 + 2;
            var by = hy;
            var placed = false;
            for (var dy = 0; dy < 8 && !placed; dy++)
            {
                by = hy + dy;
                placed = Land(lead, connection, AllianceLandRules.PlaceBeacon, bx, by) == null;
            }

            Assert.True(placed);
            Assert.Equal(string.Empty, _world.Map.LandOwnerForTests(bx + AllianceLandRules.BeaconRadius, by));
            _clock.UtcNow = _clock.UtcNow.AddMinutes(AllianceLandRules.BeaconBuildMinutes);
            Assert.Equal(alliance, _world.Map.LandOwnerForTests(bx + AllianceLandRules.BeaconRadius, by));
            Assert.Equal("LandTooFar", Land(lead, connection, AllianceLandRules.PlaceBeacon, hx + 40, hy + 40));
        }

        [Fact]
        public async Task Only_neighbouring_cities_can_be_attacked_and_a_won_battle_captures_one()
        {
            var (lead, connection, _, _) = await Alliance("city", troops: 200_000);
            var cities = Enumerable.Range(0, 100).Select(_world.Map.RegionForTests).Where(r => r.Level is >= 2 and <= 5).ToList();
            Attack(lead, cities[0].Id, PvpRules.TargetStronghold);
            Assert.Equal("NoHub", LastAck(connection).Reason);

            var city = cities[0];
            var (hx, hy) = PlantHub(lead, connection, city.X + AllianceLandRules.HubRadius(1) + 2, city.Y);
            var far = cities.First(c => Math.Max(Math.Abs(c.X - hx), Math.Abs(c.Y - hy)) > 40);
            Attack(lead, far.Id, PvpRules.TargetStronghold);
            Assert.Equal("CityTooFar", LastAck(connection).Reason);

            Attack(lead, city.Id, PvpRules.TargetStronghold);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            At(Pvp(lead).ArrivesAt);
            var report = LastReport(lead);
            Assert.Equal("won", report.Outcome);
            Assert.True(report.Captured);
            Assert.Equal(_world.Social.AllianceIdOf(lead.PublicId), _world.Map.RegionForTests(city.Index).AllianceId);
        }

        [Fact]
        public async Task A_reinforced_player_sees_who_guards_the_base()
        {
            var (lead, _, mate, mateConnection) = await Alliance("guard");
            Send(mate, new MarchPvpCommand { CommandId = "r", Mode = PvpRules.Reinforce, TargetId = lead.PublicId }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateConnection).Result);
            var guard = Assert.Single(lead.GarrisonForTests);
            Assert.Equal(mate.PublicId, guard.OwnerId);
            Assert.True(guard.Troops > 0);

            Send(mate, new RecallMarchCommand { CommandId = "back", MarchId = guard.MarchId }, CommandType.RecallMarch);
            Assert.Empty(lead.GarrisonForTests);
        }

        /// <summary>Places a structure on the first free tile of our land around (cx, cy); returns its id.</summary>
        private string PlaceOnLand(PlayerSession lead, RecordingConnection connection, string action, string kind, int cx, int cy)
        {
            var before = _world.Map.StructuresForTests().Select(s => s.Id).ToHashSet();
            for (var r = 1; r < 6; r++)
                for (var dx = -r; dx <= r; dx++)
                    for (var dy = -r; dy <= r; dy++)
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && Land(lead, connection, action, cx + dx, cy + dy, resource: kind) == null)
                            return _world.Map.StructuresForTests().Single(s => !before.Contains(s.Id)).Id;
            throw new InvalidOperationException("no free tile on the land");
        }

        private StructureDto StructureView(PlayerSession session, RecordingConnection connection, string id) =>
            Territory(session, connection).Structures.Single(s => s.Id == id);

        [Fact]
        public async Task Members_build_an_alliance_building_with_their_teams_and_donations_raise_it()
        {
            var (lead, connection, mate, mateConnection) = await Alliance("bld");
            var (lx, ly) = _world.Map.PositionOf(lead.PublicId)!.Value;
            var (hx, hy) = PlantHub(lead, connection, lx + 3, ly);
            Assert.Equal("BuildingLocked", Land(lead, connection, AllianceLandRules.PlaceBuilding, hx + 2, hy, resource: AllianceLandRules.Relay));

            var (m4, m4Connection) = await Connect("bld-4");
            var (m5, _) = await Connect("bld-5");
            var allianceId = _world.Social.AllianceIdOf(lead.PublicId);
            Send(m4, new JoinAllianceCommand { CommandId = "j4", AllianceId = allianceId }, CommandType.JoinAlliance);
            Send(m5, new JoinAllianceCommand { CommandId = "j5", AllianceId = allianceId }, CommandType.JoinAlliance);
            var relay = PlaceOnLand(lead, connection, AllianceLandRules.PlaceBuilding, AllianceLandRules.Relay, hx, hy);
            Assert.True(StructureView(lead, connection, relay).UnderConstruction);

            // A mate's team works on it until it stands.
            Send(mate, new MarchPvpCommand { CommandId = "w", Mode = PvpRules.Reinforce, TargetKind = PvpRules.TargetStructure, TargetId = relay }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateConnection).Result);
            var team = Pvp(mate);
            _clock.UtcNow = team.ArrivesAt.AddSeconds(AllianceLandRules.BuildWork / team.Troops.Values.Sum() + 5);
            var built = StructureView(lead, connection, relay);
            Assert.False(built.UnderConstruction);
            Assert.Equal(1, built.Guards);
            Assert.Equal(AllianceLandRules.BonusPercent(AllianceLandRules.Relay, 1), built.Bonus);

            Assert.Null(Land(m4, m4Connection, AllianceLandRules.Donate, id: relay, resource: "Titanium", amount: AllianceLandRules.BuildingPoints(1)));
            Assert.Equal(2, StructureView(lead, connection, relay).Level);
        }

        [Fact]
        public async Task An_enemy_wears_a_building_down_and_fells_a_beacon()
        {
            var (lead, connection, _, _) = await Alliance("siege");
            var (lx, ly) = _world.Map.PositionOf(lead.PublicId)!.Value;
            var (hx, hy) = PlantHub(lead, connection, lx + 3, ly);
            var r1 = AllianceLandRules.HubRadius(1);
            var beacon = string.Empty;
            for (var dy = 0; dy < 8 && beacon.Length == 0; dy++)
                if (Land(lead, connection, AllianceLandRules.PlaceBeacon, hx + r1 + 2, hy + dy) == null)
                    beacon = _world.Map.StructuresForTests().Single(s => s.Kind == AllianceLandRules.Beacon).Id;
            Assert.NotEmpty(beacon);

            var (enemy, enemyConnection) = await Connect("siege-enemy", troops: 200_000);
            var hub = _world.Map.StructuresForTests().Single(s => s.Kind == AllianceLandRules.Hub).Id;
            Attack(enemy, hub, PvpRules.TargetStructure);
            Assert.Equal("HubRegion", LastAck(enemyConnection).Reason);

            // A beacon has 200 durability: a strong enough attack fells it at once.
            Attack(enemy, beacon, PvpRules.TargetStructure);
            Assert.Equal(CommandResultCode.Accepted, LastAck(enemyConnection).Result);
            At(Pvp(enemy).ArrivesAt);
            Assert.Equal("won", LastReport(enemy).Outcome);
            Assert.DoesNotContain(_world.Map.StructuresForTests(), s => s.Id == beacon);
            Assert.Equal(AllianceLogKinds.DefenceLost, _world.Social.LogOf(_world.Social.AllianceIdOf(lead.PublicId), AllianceLogKinds.Battle).Last().Type);
        }

        [Fact]
        public async Task A_much_stronger_player_may_attack_a_small_base_but_takes_half_the_plunder()
        {
            var (a, aConnection) = await Connect("band-a", troops: 3_000_000);
            var (b, _) = await Connect("band-b", troops: 10);
            Assert.True(a.Power > PvpRules.PowerBand * b.Power);
            Attack(a, b.PublicId);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
            At(Pvp(a).ArrivesAt);
            Assert.Equal("won", LastReport(a).Outcome);
            Assert.Equal((int)(PvpRules.WeakTargetShare * 100), LastReport(a).PlunderShare);
        }

        [Fact]
        public void The_estimate_uses_the_combat_rules()
        {
            var troops = Catalog.Troops;
            var mine = new Dictionary<string, long> { [TroopRules.Key("armored", 3)] = 5_000 };
            Assert.Equal(CombatOutlook.Unknown, CombatEstimate.Of(troops, mine, new[] { 100 }, null));
            Assert.Equal(CombatOutlook.Win, CombatEstimate.Of(troops, mine, new[] { 100 },
                CombatEstimate.Army(new Dictionary<string, long> { [TroopRules.Key("armored", 1)] = 100 })));
            Assert.Equal(CombatOutlook.Lose, CombatEstimate.Of(troops, mine, new[] { 100 },
                CombatEstimate.Army(new Dictionary<string, long> { [TroopRules.Key("armored", 5)] = 50_000 })));
        }

        [Fact]
        public async Task Repeated_wins_on_one_base_take_less_and_three_defeats_raise_a_recovery_shield()
        {
            var (a, aConnection) = await Connect("fat-a");
            var (b, _) = await Connect("fat-b", troops: 40);
            // Next door, so three round trips fit in the fatigue window.
            var (bx, by) = _world.Map.PositionOf(b.PublicId)!.Value;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (2, 0), (0, 2), (-2, 0), (0, -2) })
            {
                Send(a, new TeleportCommand { CommandId = "near" + dx + dy, Mode = TeleportRules.Target, X = bx + dx, Y = by + dy }, CommandType.Teleport);
                if (LastAck(aConnection).Result == CommandResultCode.Accepted)
                    break;
            }

            var shares = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                b.StateForTests.Troops[Light] = 40;
                Attack(a, b.PublicId);
                Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
                var march = Pvp(a);
                At(march.ArrivesAt);
                Assert.Equal("won", LastReport(a).Outcome);
                shares.Add(LastReport(a).PlunderShare);
                At(Pvp(a).HomeAt);
            }

            Assert.Equal(new[] { 100, 50, 25 }, shares);
            // The third lost defence in six hours raised a two-hour recovery shield.
            Assert.True(b.StateForTests.ShieldUntil > _clock.UtcNow);

            // Without the shield a fourth attack within the fatigue window is refused.
            b.StateForTests.ShieldUntil = null;
            Attack(a, b.PublicId);
            Assert.Equal("Fatigue", LastAck(aConnection).Reason);
        }

        [Fact]
        public void Fallen_troops_are_wounded_while_the_med_bay_has_room_and_attackers_always_lose_a_quarter()
        {
            Assert.Equal((25L, 75L), PvpRules.Split(100, 1_000, attacking: true));
            Assert.Equal((90L, 10L), PvpRules.Split(100, 10, attacking: true));
            Assert.Equal((0L, 100L), PvpRules.Split(100, 1_000, attacking: false));
            Assert.Equal((100L, 0L), PvpRules.Split(100, 0, attacking: false));
        }

        [Fact]
        public void A_seeded_battle_reproduces_exactly()
        {
            var troops = Catalog.Troops;
            BattleSide Army(long n)
            {
                var side = new BattleSide();
                side.Troops[TroopRules.Key("armored", 3)] = n;
                side.Troops[TroopRules.Key("arc", 3)] = n;
                return side;
            }

            var seed = CombatRules.SeedOf("p0123456789");
            var first = CombatRules.Resolve(troops, Army(1000), Army(1000), seed: seed);
            var again = CombatRules.Resolve(troops, Army(1000), Army(1000), seed: seed);
            Assert.Equal(first.AttackerWon, again.AttackerWon);
            Assert.Equal(first.AttackerLost, again.AttackerLost);
            Assert.Equal(first.DefenderLost, again.DefenderLost);
            Assert.Equal(seed, CombatRules.SeedOf("p0123456789"));
        }

        [Fact]
        public async Task A_fought_battle_reports_its_seed_rule_version_and_contribution()
        {
            var (a, _) = await Connect("rep-a");
            var (b, _) = await Connect("rep-b", troops: 50);
            Attack(a, b.PublicId);
            var march = Pvp(a);
            At(march.ArrivesAt);
            var report = LastReport(a);
            Assert.Equal(CombatRules.SeedOf(march.Id), report.Seed);
            Assert.Equal(CombatRules.Version, report.RuleVersion);
            Assert.Equal(100, report.Contribution);
        }

        [Fact]
        public async Task A_rally_team_needs_a_minimum_of_troops()
        {
            var (lead, leadConnection) = await Connect("min-lead", troops: PvpRules.MinRallyTroops - 1);
            var (mate, _) = await Connect("min-mate");
            var (target, _) = await Connect("min-target", troops: 10);
            Ally(lead, leadConnection, mate);
            Send(lead, new MarchPvpCommand { CommandId = "min-rally", Mode = PvpRules.Rally, TargetKind = PvpRules.TargetBase, TargetId = target.PublicId, MusterSeconds = 60 },
                CommandType.MarchPvp);
            Assert.Equal("TooFewTroops", LastAck(leadConnection).Reason);
        }

        [Fact]
        public async Task An_attack_on_its_way_cannot_be_sped_up()
        {
            var (a, aConnection) = await Connect("spd-a");
            var (b, _) = await Connect("spd-b", troops: 50);
            a.StateForTests.Items["speed_march_p50"] = 1;
            Attack(a, b.PublicId);
            var march = Pvp(a);
            Send(a, new UseSpeedupCommand { CommandId = "spd", ItemId = "speed_march_p50", Target = march.Id }, CommandType.UseSpeedup);
            Assert.Equal(CommandResultCode.Rejected, LastAck(aConnection).Result);
            Assert.Equal(march.ArrivesAt, Pvp(a).ArrivesAt);
        }

        [Fact]
        public async Task No_alliance_land_goes_up_in_the_starter_belt()
        {
            var (lead, connection, _, _) = await Alliance("starter");
            var world = Catalog.World;
            var starter = world.belts.Where(b => b.bases).OrderByDescending(b => b.outer).First();
            var size = world.size;
            var tile = Enumerable.Range(0, size * size).Select(i => (X: i % size, Y: i / size)).First(t => GatherRules.BeltOf(world, t.X, t.Y) == starter);
            Assert.Equal("StarterZone", Land(lead, connection, AllianceLandRules.PlaceHub, tile.X, tile.Y));
        }

        [Fact]
        public void Plunder_never_touches_the_protected_amount()
        {
            Assert.Equal(PvpRules.ProtectedPerReactor * 10, PvpRules.Protected(0, 10, 0));
            Assert.Equal(140_000, PvpRules.Protected(1_000_000, 1, 1));
            var wallet = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 100_000, [ResourceType.Food] = 50_000 };
            var guard = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 90_000, [ResourceType.Food] = 60_000 };
            var taken = PvpRules.Plunder(wallet, guard, long.MaxValue);
            Assert.Equal(2_000, taken[ResourceType.Titanium]);
            Assert.False(taken.ContainsKey(ResourceType.Food));
        }

        [Fact]
        public async Task A_base_with_only_its_protected_share_loses_nothing_and_sees_the_share()
        {
            var (a, _) = await Connect("vault-a");
            var (b, bConnection) = await Connect("vault-b", troops: 20);
            Attack(a, b.PublicId);
            var status = TestSupport.Serializer.Deserialize<PvpStatusDto>(bConnection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.Pvp).Payload);
            var guard = status.Protected.ToDictionary(p => p.Key, p => p.Value);
            Assert.True(guard[ResourceType.Titanium] > 0);
            foreach (var r in PvpRules.Lootable)
                b.StateForTests.Wallet[r] = guard[r];

            At(Pvp(a).ArrivesAt);
            Assert.Equal("won", LastReport(a).Outcome);
            Assert.Equal(0, LastReport(a).Plunder.Sum(p => p.Value));
        }

        [Fact]
        public async Task Fatigue_counts_survive_a_restart()
        {
            var (a, aConnection) = await Connect("keep-a");
            var (b, _) = await Connect("keep-b", troops: 40);
            Attack(a, b.PublicId);
            At(Pvp(a).ArrivesAt);
            Assert.Equal(100, LastReport(a).PlunderShare);
            At(Pvp(a).HomeAt);

            // A restart: the world forgets, then reads the stored counts back.
            await _world.Map.FlushFairnessForTests();
            _world.Map.ForgetFairnessForTests();
            await _world.Map.ReloadFairnessForTests();

            b.StateForTests.Troops[Light] = 40;
            Attack(a, b.PublicId);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
            At(Pvp(a).ArrivesAt);
            Assert.Equal(50, LastReport(a).PlunderShare);
        }
        private List<Hadal.Game.Mailbox.Mail> BattleMails(PlayerSession session) =>
            _world.Mail.MailsOf(session.PublicId, _clock.UtcNow).Where(m => m.Kind == Hadal.Game.Mailbox.MailKinds.Battle).ToList();

        [Fact]
        public async Task Both_sides_get_one_battle_mail_and_the_defender_sees_the_attack_coming_at_once()
        {
            var (a, _) = await Connect("mail-a");
            var (b, bConnection) = await Connect("mail-b", troops: 50);
            var before = bConnection.Messages.Count;
            Attack(a, b.PublicId);

            // The defender's live session gets the incoming attack in the same instant, not on the next refresh.
            var pushed = bConnection.Messages.Skip(before).Where(m => m.Delta != null).SelectMany(m => m.Delta().Changes)
                .Where(c => c.EntityId == EntityIds.Pvp).Select(c => TestSupport.Serializer.Deserialize<PvpStatusDto>(c.Payload)).ToList();
            Assert.Contains(pushed, p => p.Incoming.Any(i => i.AttackerId == a.PublicId));
            Assert.Empty(BattleMails(a));

            At(Pvp(a).ArrivesAt);
            var won = Assert.Single(BattleMails(a));
            Assert.Equal("loc:mail.pvp.won|pvp.kind.attack|mail-b", won.Title);
            Assert.Contains("loc:mail.pvp.loot.won", won.Body);
            Assert.Contains($"loc:mail.pvp.id|{LastReport(a).Id}|attack|", won.Body);

            var lost = Assert.Single(BattleMails(b));
            Assert.StartsWith("loc:mail.pvp.lost|pvp.kind.defence|mail-a", lost.Title);
            // Who attacked, from where, and what was taken.
            Assert.Contains($"loc:mail.pvp.from|mail-a|{LastReport(b).FromX}|{LastReport(b).FromY}", lost.Body);
            Assert.Contains("loc:mail.pvp.minus|", lost.Body);
            Assert.Contains("loc:mail.pvp.losses|", lost.Body);

            // Later ticks and commands never send them again.
            At(_clock.UtcNow.AddSeconds(5));
            Send(a, new ViewWorldCommand { CommandId = "again", X = -1, Y = -1 }, CommandType.ViewWorld);
            Assert.Single(BattleMails(a));
            Assert.Single(BattleMails(b));
        }

        [Fact]
        public async Task A_battle_mails_both_sides_into_the_battle_tab_with_the_report_as_data()
        {
            var (a, aConnection) = await Connect("tab-a");
            var (b, bConnection) = await Connect("tab-b", troops: 50);
            Attack(a, b.PublicId);
            At(Pvp(a).ArrivesAt);

            MailListDto Box(RecordingConnection connection) => TestSupport.Serializer.Deserialize<MailListDto>(connection.Messages
                .Where(m => m.Delta != null).SelectMany(m => m.Delta().Changes).Last(c => c.EntityId == EntityIds.Mail).Payload);

            foreach (var (session, connection, kind, opponent) in new[] { (a, aConnection, "attack", "tab-b"), (b, bConnection, "defence", "tab-a") })
            {
                var box = Box(connection);
                var mail = Assert.Single(box.Mails, m => m.Kind == "battle");
                Assert.Equal(MailRules.TabBattle, MailRules.TabOf(mail.Kind));
                Assert.Equal(1, box.Tabs.Single(t => t.Tab == MailRules.TabBattle).Unread);

                // The card is drawn from data: the same report the war log holds, both armies and the loot.
                var report = mail.Report!;
                var logged = LastReport(session);
                Assert.Equal((logged.Id, kind, opponent), (report.Id, report.Kind, report.OpponentName));
                Assert.Equal((logged.Outcome, logged.Rounds, logged.Dead, logged.Wounded), (report.Outcome, report.Rounds, report.Dead, report.Wounded));
                Assert.NotEmpty(report.Attacker);
                Assert.NotEmpty(report.Defender);
                Assert.Equal(logged.Plunder.Sum(p => p.Value), report.Plunder.Sum(p => p.Value));
                Assert.True(report.Plunder.Sum(p => p.Value) > 0);
            }

            Assert.Equal("won", Box(aConnection).Mails.Single(m => m.Kind == "battle").Report!.Outcome);
            Assert.Equal("lost", Box(bConnection).Mails.Single(m => m.Kind == "battle").Report!.Outcome);
        }

        [Fact]
        public async Task A_scout_mails_the_report_and_tells_the_target_who_scouted_them()
        {
            var (a, _) = await Connect("smail-a");
            var (b, _) = await Connect("smail-b", troops: 50);
            Attack(a, b.PublicId, mode: PvpRules.Scout);
            At(Pvp(a).ArrivesAt);
            var report = Assert.Single(BattleMails(a));
            Assert.StartsWith("loc:mail.pvp.scouted|pvp.kind.scout|smail-b", report.Title);
            Assert.Contains("loc:mail.pvp.scout|", report.Body);
            var seen = Assert.Single(BattleMails(b));
            Assert.StartsWith("loc:mail.pvp.scouted|pvp.kind.scouted|smail-a", seen.Title);
            Assert.Contains("loc:mail.pvp.scoutedBy|smail-a|", seen.Body);
        }
        [Fact]
        public async Task A_shield_raised_after_the_attack_left_bounces_it_and_both_sides_are_told_and_see_the_dome()
        {
            var (a, aConnection) = await Connect("dome-a");
            var (b, _) = await Connect("dome-b", troops: 50);
            b.StateForTests.Items[PvpRules.Shield8h] = 1;
            Attack(a, b.PublicId);
            var march = Pvp(a);
            var sent = march.Troops.Values.Sum();

            _clock.UtcNow = march.StartedAt.AddSeconds(1);
            Send(b, new UseShieldCommand { CommandId = "dome-shield", ItemId = PvpRules.Shield8h }, CommandType.UseShield);

            // Everyone sees the dome and its end on the map.
            Send(a, new ViewWorldCommand { CommandId = "dome-view", X = march.X, Y = march.Y }, CommandType.ViewWorld);
            var view = TestSupport.Serializer.Deserialize<WorldViewDto>(aConnection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.WorldView).Payload);
            Assert.Equal(b.StateForTests.ShieldUntil!.Value.ToUnixTimeMilliseconds(), view.Bases.Single(x => x.PlayerId == b.PublicId).ShieldUntilMs);
            Assert.DoesNotContain(view.Bases, x => x.PlayerId != b.PublicId && x.ShieldUntilMs > 0);

            At(march.ArrivesAt);
            Assert.Equal("shielded", LastReport(a).Outcome);
            Assert.Equal(sent, Pvp(a).Troops.Values.Sum()); // no battle, no losses: the army sails home
            Assert.Contains("loc:mail.pvp.bounced", Assert.Single(BattleMails(a)).Body);
            Assert.Contains("loc:mail.pvp.blocked|dome-a", Assert.Single(BattleMails(b)).Body);

            // A new attack is refused before it sails.
            Attack(a, b.PublicId);
            Assert.Equal("TargetShielded", LastAck(aConnection).Reason);
        }
    }
}
