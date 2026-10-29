using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Roadmap §1, research §14 "Ekonomi": every spend and grant is atomic and idempotent. A command sent twice runs
    /// once; no balance goes below zero; spends from two connections or threads add up exactly; balances above the cap
    /// or near long.MaxValue are never wiped.
    /// </summary>
    public sealed class EconomySafetyTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public EconomySafetyTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        // --- Helpers ------------------------------------------------------------------------------

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private static CommandEnvelope Envelope<T>(T command, CommandType type) where T : ICommand =>
            EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static void Fill(PlayerSession session, long amount = 1_000_000)
        {
            foreach (var type in Enum.GetValues<ResourceType>().Where(t => t != ResourceType.Unknown))
                session.StateForTests.Wallet[type] = amount;
        }

        private static List<CommandAckDto> Acks(RecordingConnection connection, string commandId) =>
            connection.Messages.Where(m => m.Delta != null).SelectMany(m => m.Delta().Changes)
                .Where(c => c.EntityId == EntityIds.Command + commandId)
                .Select(c => TestSupport.Serializer.Deserialize<CommandAckDto>(c.Payload)).ToList();

        /// <summary>A building put straight into the state, marked for the save like a placed one.</summary>
        private static void AddBuilding(PlayerSession session, PlacedBuilding building)
        {
            var state = session.StateForTests;
            state.Buildings.Add(building);
            state.UnsavedBuildings.Add(building);
            state.IsDirty = true;
        }

        private static long Wallet(PlayerSession session, ResourceType type) =>
            session.StateForTests.Wallet.TryGetValue(type, out var n) ? n : 0;

        /// <summary>Everything a spend or a grant can touch, in a stable order.</summary>
        internal static string Fingerprint(PlayerSession session)
        {
            var s = session.StateForTests;
            static string Map<T>(IEnumerable<KeyValuePair<string, T>> map) =>
                string.Join(",", map.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value));
            return string.Join(" | ",
                Map(s.Wallet.Select(p => new KeyValuePair<string, long>(p.Key.ToString(), p.Value))),
                Map(s.Items), Map(s.HeroCards), Map(s.Troops), Map(s.Wounded), Map(s.ResearchLevels),
                string.Join(",", s.Buildings.OrderBy(b => b.BuildingId, StringComparer.Ordinal)
                    .Select(b => $"{b.BuildingId}:{b.DefinitionId}:{b.Level}:{b.ConstructionEndsAt?.ToUnixTimeMilliseconds()}")),
                string.Join(",", s.Training.Select(t => $"{t.BuildingId}:{t.Count}:{t.EndsAt.ToUnixTimeMilliseconds()}")),
                s.Healing == null ? "-" : $"{s.Healing.Troops.Values.Sum()}:{s.Healing.EndsAt.ToUnixTimeMilliseconds()}",
                string.Join(",", s.Heroes.OrderBy(h => h.HeroId, StringComparer.Ordinal).Select(h => $"{h.HeroId}:{h.Level}:{h.Stars}:{h.Shards}")),
                s.SincePity, s.FreePullAt?.ToUnixTimeMilliseconds(), s.VipPoints, s.ShieldUntil?.ToUnixTimeMilliseconds(),
                s.MailsSent, s.Marches.Count, s.ChapterIndex, s.DailyPoints);
        }

        /// <summary>Nothing may hold less than zero: resources, items, cards, troops, wounded.</summary>
        internal static void AssertNothingNegative(PlayerSession session)
        {
            var s = session.StateForTests;
            Assert.All(s.Wallet, p => Assert.True(p.Value >= 0, $"{p.Key} = {p.Value}"));
            Assert.All(s.Items, p => Assert.True(p.Value >= 0, $"item {p.Key} = {p.Value}"));
            Assert.All(s.HeroCards, p => Assert.True(p.Value >= 0, $"cards {p.Key} = {p.Value}"));
            Assert.All(s.Troops, p => Assert.True(p.Value >= 0, $"troops {p.Key} = {p.Value}"));
            Assert.All(s.Wounded, p => Assert.True(p.Value >= 0, $"wounded {p.Key} = {p.Value}"));
        }

        /// <summary>
        /// Sends <paramref name="envelope"/>, then the very same envelope again on the same connection (a duplicate after
        /// a timeout), then once more after the player went offline, was saved and unloaded, and came back (a re-send
        /// after reconnect). Only the first copy may change anything; every copy gets the first answer.
        /// </summary>
        private async Task<CommandAckDto> AssertRunsOnce(string identity, PlayerSession session, RecordingConnection connection, CommandEnvelope envelope,
            Action<PlayerSession>? alsoUnchanged = null)
        {
            _world.HandleCommand(session, envelope);
            var first = Assert.Single(Acks(connection, envelope.CommandId));
            var after = Fingerprint(session);
            AssertNothingNegative(session);

            _world.HandleCommand(session, envelope);
            Assert.Equal(after, Fingerprint(session));
            var replay = Acks(connection, envelope.CommandId);
            Assert.Equal(2, replay.Count);
            Assert.Equal((first.Result, first.Reason), (replay[1].Result, replay[1].Reason));
            alsoUnchanged?.Invoke(session);

            await _world.DisconnectAsync(session, connection, default);
            var (again, againConnection) = await Connect(identity);
            var reloaded = Fingerprint(again);
            _world.HandleCommand(again, envelope);
            Assert.Equal(reloaded, Fingerprint(again));
            var resent = Assert.Single(Acks(againConnection, envelope.CommandId));
            Assert.Equal((first.Result, first.Reason), (resent.Result, resent.Reason));
            alsoUnchanged?.Invoke(again);
            return first;
        }

        // --- Idempotency: one CommandId runs once -----------------------------------------------

        [Fact]
        public async Task Placing_and_upgrading_a_building_twice_with_one_id_pays_once()
        {
            var (session, connection) = await Connect("eco-place");
            Fill(session);
            var placeAck = await AssertRunsOnce("eco-place", session, connection, TestSupport.Place("oxygen_plant", 1, 0, "place-" + Id()));
            Assert.Equal(CommandResultCode.Accepted, placeAck.Result);

            var (again, againConnection) = await Connect("eco-place");
            _clock.Advance(TimeSpan.FromHours(1));
            _world.Tick();
            var reactor = again.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            var upgrade = await AssertRunsOnce("eco-place", again, againConnection, TestSupport.Upgrade(reactor.BuildingId, "up-" + Id()));
            Assert.Equal(CommandResultCode.Accepted, upgrade.Result);
        }

        [Fact]
        public async Task A_ten_pull_sent_twice_charges_once()
        {
            var (session, connection) = await Connect("eco-pull");
            session.StateForTests.Wallet[ResourceType.Hadalite] = 1000;
            var pull = new ExecuteGachaCommand { CommandId = Id(), ClientSequence = 1, BannerId = HeroRules.PodBanner, PullCount = 10 };
            var price = TestSupport.Catalog.PodTenCost.Single(c => c.Key == ResourceType.Hadalite).Value;
            await AssertRunsOnce("eco-pull", session, connection, Envelope(pull, CommandType.ExecuteGacha),
                s => Assert.Equal(1000 - price, Wallet(s, ResourceType.Hadalite)));
        }

        [Fact]
        public async Task Opening_capsules_and_chests_twice_with_one_id_opens_once()
        {
            var (session, connection) = await Connect("eco-open");
            var capsule = Capsules.Id(ResourceType.Titanium, 5000);
            session.StateForTests.Items[capsule] = 5;
            session.StateForTests.Items["chest_gold"] = 4;
            var before = Wallet(session, ResourceType.Titanium);

            await AssertRunsOnce("eco-open", session, connection,
                Envelope(new OpenItemCommand { CommandId = Id(), ClientSequence = 1, ItemId = capsule, Count = 3 }, CommandType.OpenItem),
                s =>
                {
                    Assert.Equal(before + 15_000, Wallet(s, ResourceType.Titanium));
                    Assert.Equal(2, s.StateForTests.Items[capsule]);
                });

            var (again, againConnection) = await Connect("eco-open");
            await AssertRunsOnce("eco-open", again, againConnection,
                Envelope(new OpenItemCommand { CommandId = Id(), ClientSequence = 1, ItemId = "chest_gold", Count = 2 }, CommandType.OpenItem),
                s => Assert.Equal(2, s.StateForTests.Items["chest_gold"]));
        }

        [Fact]
        public async Task Training_cancelling_and_speeding_up_twice_with_one_id_count_once()
        {
            var (session, connection) = await Connect("eco-train");
            Fill(session);
            // The reactor allows a second copy of each, so the start-up copy limit (PlayerSession.Limits) keeps them on reload.
            var reactor = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            reactor.Level = 10;
            session.StateForTests.UnsavedBuildings.Add(reactor);
            var yard = session.StateForTests.Buildings.FirstOrDefault(b => b.DefinitionId == "armor_yard")?.BuildingId;
            if (yard == null)
                AddBuilding(session, new PlacedBuilding { BuildingId = yard = "yard", DefinitionId = "armor_yard", Ring = 4, Sector = 0, Level = 1 });
            if (!session.StateForTests.Buildings.Any(b => b.DefinitionId == "military_dome"))
                AddBuilding(session, new PlacedBuilding { BuildingId = "dome", DefinitionId = "military_dome", Ring = 4, Sector = 1, Level = 1 });
            session.StateForTests.Items[Speedups.Id(SpeedupKind.Train, 5)] = 3;

            CommandEnvelope Train() => Envelope(new TrainTroopsCommand { CommandId = Id(), BuildingId = yard, TroopType = "armored", Tier = 1, Count = 10 },
                CommandType.TrainTroops);
            Assert.Equal(CommandResultCode.Accepted, (await AssertRunsOnce("eco-train", session, connection, Train(),
                s => Assert.Equal(10, s.StateForTests.Training.Single().Count))).Result);

            // Cancelling refunds the batch once.
            var (s2, c2) = await Connect("eco-train");
            var paid = Wallet(s2, ResourceType.Food);
            var refund = s2.StateForTests.Training.Single().Cost[ResourceType.Food];
            var cancel = Envelope(new CancelTrainingCommand { CommandId = Id(), BuildingId = yard }, CommandType.CancelTraining);
            Assert.Equal(CommandResultCode.Accepted, (await AssertRunsOnce("eco-train", s2, c2, cancel,
                s => Assert.Equal(paid + refund, Wallet(s, ResourceType.Food)))).Result);

            // A speed-up is used once.
            var (s3, c3) = await Connect("eco-train");
            var again = Train();
            _world.HandleCommand(s3, again);
            Assert.Equal((CommandResultCode.Accepted, ""), (Acks(c3, again.CommandId).Single().Result, Acks(c3, again.CommandId).Single().Reason));
            var speed = Envelope(new UseSpeedupCommand { CommandId = Id(), ItemId = Speedups.Id(SpeedupKind.Train, 5), Target = "train:" + yard },
                CommandType.UseSpeedup);
            Assert.Equal(CommandResultCode.Accepted, (await AssertRunsOnce("eco-train", s3, c3, speed,
                s => Assert.Equal(2, s.StateForTests.Items[Speedups.Id(SpeedupKind.Train, 5)]))).Result);
        }

        [Fact]
        public async Task A_shield_and_a_teleport_sent_twice_use_one_item()
        {
            var (session, connection) = await Connect("eco-shield");
            session.StateForTests.Items[PvpRules.Shield8h] = 2;
            await AssertRunsOnce("eco-shield", session, connection,
                Envelope(new UseShieldCommand { CommandId = Id(), ItemId = PvpRules.Shield8h }, CommandType.UseShield),
                s => Assert.Equal(1, s.StateForTests.Items[PvpRules.Shield8h]));

            var (s2, c2) = await Connect("eco-shield");
            s2.StateForTests.Wallet[ResourceType.Hadalite] = 10_000;
            var price = TeleportRules.HadaliteFor(TeleportRules.Random);
            await AssertRunsOnce("eco-shield", s2, c2,
                Envelope(new TeleportCommand { CommandId = Id(), Mode = TeleportRules.Random }, CommandType.Teleport),
                s => Assert.Equal(10_000 - price, Wallet(s, ResourceType.Hadalite)));
        }

        [Fact]
        public async Task A_rejected_command_sent_again_is_rejected_again_without_side_effects()
        {
            var (session, connection) = await Connect("eco-reject");
            session.StateForTests.Wallet[ResourceType.Hadalite] = 5;
            var pull = Envelope(new ExecuteGachaCommand { CommandId = Id(), ClientSequence = 1, PullCount = 10 }, CommandType.ExecuteGacha);
            var ack = await AssertRunsOnce("eco-reject", session, connection, pull, s => Assert.Equal(5, Wallet(s, ResourceType.Hadalite)));
            Assert.Equal((CommandResultCode.Rejected, nameof(HeroRejectReason.InsufficientResources)), (ack.Result, ack.Reason));
        }

        [Fact]
        public async Task Read_only_requests_answer_every_time()
        {
            var (session, connection) = await Connect("eco-view");
            var view = Envelope(new ViewWorldCommand { CommandId = "view-once", X = -1, Y = -1 }, CommandType.ViewWorld);
            _world.HandleCommand(session, view);
            var first = connection.Messages.Count;
            _world.HandleCommand(session, view);
            Assert.True(connection.Messages.Count > first);
        }

        /// <summary>Two mates; the first sends resources by mail, the second claims them; donations to alliance research.</summary>
        [Fact]
        public async Task Mail_claims_and_alliance_donations_sent_twice_move_resources_once()
        {
            var (a, aConnection) = await Connect("eco-mail-a");
            var (b, bConnection) = await Connect("eco-mail-b");
            Fill(a);
            Fill(b);
            _world.HandleCommand(a, Envelope(new CreateAllianceCommand { CommandId = Id(), Name = "Ledger House", Tag = "LDGR" }, CommandType.CreateAlliance));
            var allianceId = TestSupport.Serializer.Deserialize<AllianceDto>(aConnection.Messages.Where(m => m.Delta != null)
                .SelectMany(m => m.Delta().Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
            _world.HandleCommand(b, Envelope(new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance));
            Fill(a);
            Fill(b);

            var send = Envelope(new SendMailCommand
            {
                CommandId = Id(), PlayerId = b.PublicId, Title = "Yardım", Body = "Al bunu",
                Resources = new[] { new ResourceAmountEntry { Key = ResourceType.Titanium, Value = 30_000 } }
            }, CommandType.SendMail);
            var sent = await AssertRunsOnce("eco-mail-a", a, aConnection, send, s => Assert.Equal(1_000_000 - 30_000, Wallet(s, ResourceType.Titanium)));
            Assert.Equal((CommandResultCode.Accepted, ""), (sent.Result, sent.Reason));
            await _world.Mail.DrainAsync();
            var mail = Assert.Single(_world.Mail.MailsOf(b.PublicId, _clock.UtcNow), m => m.HasAttachments);

            var claim = Envelope(new ClaimMailCommand { CommandId = Id(), MailId = mail.Id }, CommandType.ClaimMail);
            var capsule = Capsules.Id(ResourceType.Titanium, 30_000);
            await AssertRunsOnce("eco-mail-b", b, bConnection, claim, s => Assert.Equal(1, s.StateForTests.Items[capsule]));

            var (a2, a2Connection) = await Connect("eco-mail-a");
            var donate = Envelope(new DonateAllianceResearchCommand { CommandId = Id() }, CommandType.DonateAllianceResearch);
            var held = a2.StateForTests.Wallet.Values.Sum();
            var ack = await AssertRunsOnce("eco-mail-a", a2, a2Connection, donate,
                s => Assert.Equal(held - AllianceResearchRules.DonationCost(0), s.StateForTests.Wallet.Values.Sum()));
            Assert.Equal(CommandResultCode.Accepted, ack.Result);
        }

        [Fact]
        public async Task The_ledger_keeps_the_newest_ids_and_survives_a_save()
        {
            var (session, connection) = await Connect("eco-ledger");
            for (var i = 0; i < PlayerSession.CommandLedgerSize + 10; i++)
                _world.HandleCommand(session, Envelope(new ClaimVipLoginCommand { CommandId = "vip-" + i }, CommandType.ClaimVipLogin));
            var ids = session.StateForTests.RecentCommands.Select(r => r.Id).ToList();
            Assert.Equal(PlayerSession.CommandLedgerSize, ids.Count);
            Assert.Equal("vip-" + (PlayerSession.CommandLedgerSize + 9), ids[^1]);
            Assert.DoesNotContain("vip-0", ids);

            await _world.DisconnectAsync(session, connection, default);
            var (again, _) = await Connect("eco-ledger");
            Assert.Equal(ids, again.StateForTests.RecentCommands.Select(r => r.Id));
            Assert.All(again.StateForTests.RecentCommands, r => Assert.NotNull(r.Result));
        }

        // --- Never below zero ----------------------------------------------------------------------

        [Fact]
        public async Task Spending_the_last_unit_leaves_zero_and_the_next_spend_is_refused()
        {
            var (session, connection) = await Connect("eco-zero");
            var price = TestSupport.Catalog.PodTenCost.Single(c => c.Key == ResourceType.Hadalite).Value;
            session.StateForTests.Wallet[ResourceType.Hadalite] = price;
            _world.HandleCommand(session, Envelope(new ExecuteGachaCommand { CommandId = Id(), PullCount = 10 }, CommandType.ExecuteGacha));
            Assert.Equal(0, Wallet(session, ResourceType.Hadalite));
            var again = new ExecuteGachaCommand { CommandId = Id(), PullCount = 10 };
            _world.HandleCommand(session, Envelope(again, CommandType.ExecuteGacha));
            Assert.Equal(CommandResultCode.Rejected, Acks(connection, again.CommandId).Single().Result);
            Assert.Equal(0, Wallet(session, ResourceType.Hadalite));
        }

        /// <summary>
        /// The bug this suite found: two resource lines of one kind whose sum passed long.MaxValue wrapped round to a
        /// negative payment, which passed every check and paid the sender instead of charging them.
        /// </summary>
        [Fact]
        public async Task Mail_resources_that_overflow_are_refused_and_never_pay_the_sender()
        {
            var (a, aConnection) = await Connect("eco-ovf-a");
            var (b, _) = await Connect("eco-ovf-b");
            Fill(a);
            _world.HandleCommand(a, Envelope(new CreateAllianceCommand { CommandId = Id(), Name = "Overflow Club", Tag = "OVFL" }, CommandType.CreateAlliance));
            var allianceId = TestSupport.Serializer.Deserialize<AllianceDto>(aConnection.Messages.Where(m => m.Delta != null)
                .SelectMany(m => m.Delta().Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
            _world.HandleCommand(b, Envelope(new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance));
            a.StateForTests.Wallet[ResourceType.Titanium] = 0;
            var before = Fingerprint(a);

            var send = new SendMailCommand
            {
                CommandId = Id(), PlayerId = b.PublicId, Title = "x",
                Resources = new[]
                {
                    new ResourceAmountEntry { Key = ResourceType.Titanium, Value = long.MaxValue },
                    new ResourceAmountEntry { Key = ResourceType.Titanium, Value = 2 }
                }
            };
            _world.HandleCommand(a, Envelope(send, CommandType.SendMail));
            // Refused by the allowance check, not by anything earlier (the two are mates).
            Assert.Equal((CommandResultCode.Rejected, "MailResourceLimit"), (Acks(aConnection, send.CommandId).Single().Result, Acks(aConnection, send.CommandId).Single().Reason));
            Assert.Equal(before, Fingerprint(a));
            Assert.Equal(0, Wallet(a, ResourceType.Titanium));
            AssertNothingNegative(a);
        }

        [Fact]
        public async Task Negative_and_zero_counts_never_pay_out()
        {
            var (session, connection) = await Connect("eco-neg");
            Fill(session, 1000);
            AddBuilding(session, new PlacedBuilding { BuildingId = "yard", DefinitionId = "armor_yard", Ring = 4, Sector = 0, Level = 1 });
            AddBuilding(session, new PlacedBuilding { BuildingId = "dome", DefinitionId = "military_dome", Ring = 4, Sector = 1, Level = 1 });
            AddBuilding(session, new PlacedBuilding { BuildingId = "medbay", DefinitionId = TestSupport.Catalog.Troops.infirmary.building, Ring = 4, Sector = 2, Level = 1 });
            var capsule = Capsules.Id(ResourceType.Titanium, 100);
            session.StateForTests.Items[capsule] = 1;
            session.StateForTests.Wounded["armored:1"] = 5;

            var train = new TrainTroopsCommand { CommandId = Id(), BuildingId = "yard", TroopType = "armored", Tier = 1, Count = -50 };
            _world.HandleCommand(session, Envelope(train, CommandType.TrainTroops));
            Assert.Equal(nameof(TroopRejectReason.BadCount), Acks(connection, train.CommandId).Single().Reason);

            var heal = new HealTroopsCommand { CommandId = Id(), Troops = new[] { new TroopStackDto { Type = "armored", Tier = 1, Count = -5 } } };
            _world.HandleCommand(session, Envelope(heal, CommandType.HealTroops));
            Assert.Equal(CommandResultCode.Rejected, Acks(connection, heal.CommandId).Single().Result);

            // Counts below one open one item at most, never a negative number of them.
            var open = new OpenItemCommand { CommandId = Id(), ItemId = capsule, Count = -1000 };
            _world.HandleCommand(session, Envelope(open, CommandType.OpenItem));
            Assert.Equal(1100, Wallet(session, ResourceType.Titanium));
            Assert.False(session.StateForTests.Items.ContainsKey(capsule));

            var speed = new UseSpeedupCommand { CommandId = Id(), ItemId = Speedups.Id(SpeedupKind.Train, 5), Target = "train:yard", Count = -3 };
            _world.HandleCommand(session, Envelope(speed, CommandType.UseSpeedup));
            Assert.Equal(CommandResultCode.Rejected, Acks(connection, speed.CommandId).Single().Result);

            // A charge below zero (a bad catalog line or a forged amount) is refused rather than paid out.
            Assert.False(session.TryCharge(new[] { new KeyValuePair<ResourceType, long>(ResourceType.Titanium, -500) }, _clock.UtcNow));
            Assert.Equal(1100, Wallet(session, ResourceType.Titanium));
            AssertNothingNegative(session);
        }

        /// <summary>Random commands with hostile numbers: whatever is accepted or refused, nothing may end up below zero.</summary>
        [Fact]
        public async Task Hostile_random_commands_never_leave_a_negative_balance()
        {
            var (session, _) = await Connect("eco-fuzz");
            var (mate, _) = await Connect("eco-fuzz-mate");
            Fill(session, 2000);
            AddBuilding(session, new PlacedBuilding { BuildingId = "yard", DefinitionId = "armor_yard", Ring = 4, Sector = 0, Level = 1 });
            AddBuilding(session, new PlacedBuilding { BuildingId = "dome", DefinitionId = "military_dome", Ring = 4, Sector = 1, Level = 1 });
            session.StateForTests.Items[Capsules.Id(ResourceType.Food, 50)] = 3;
            session.StateForTests.Items["chest_gold"] = 2;
            session.StateForTests.Items[PvpRules.Shield8h] = 1;
            var random = new Random(1234);
            long[] numbers = { long.MinValue, -1, 0, 1, 7, 1000, int.MaxValue, long.MaxValue };
            int Count() => (int)Math.Clamp(numbers[random.Next(numbers.Length)], int.MinValue, int.MaxValue);
            string[] items = { Capsules.Id(ResourceType.Food, 50), "chest_gold", PvpRules.Shield8h, "nothing", Speedups.Id(SpeedupKind.Any, 5), "vip_100" };

            for (var i = 0; i < 400; i++)
            {
                var envelope = random.Next(9) switch
                {
                    0 => Envelope(new TrainTroopsCommand { CommandId = Id(), BuildingId = "yard", TroopType = "armored", Tier = random.Next(-1, 3), Count = Count() }, CommandType.TrainTroops),
                    1 => Envelope(new OpenItemCommand { CommandId = Id(), ItemId = items[random.Next(items.Length)], Count = Count() }, CommandType.OpenItem),
                    2 => Envelope(new UseSpeedupCommand { CommandId = Id(), ItemId = items[random.Next(items.Length)], Target = "train:yard", Count = Count() }, CommandType.UseSpeedup),
                    3 => Envelope(new ExecuteGachaCommand { CommandId = Id(), PullCount = Count() }, CommandType.ExecuteGacha),
                    4 => Envelope(new CancelTrainingCommand { CommandId = Id(), BuildingId = "yard" }, CommandType.CancelTraining),
                    5 => Envelope(new HealTroopsCommand { CommandId = Id(), Troops = new[] { new TroopStackDto { Type = "armored", Tier = 1, Count = numbers[random.Next(numbers.Length)] } } }, CommandType.HealTroops),
                    6 => Envelope(new SendMailCommand
                    {
                        CommandId = Id(), PlayerId = mate.PublicId, Title = "f",
                        Resources = new[] { new ResourceAmountEntry { Key = ResourceType.Food, Value = numbers[random.Next(numbers.Length)] },
                            new ResourceAmountEntry { Key = ResourceType.Food, Value = numbers[random.Next(numbers.Length)] } }
                    }, CommandType.SendMail),
                    7 => Envelope(new BuyAllianceItemCommand { CommandId = Id(), ItemId = "x", Count = Count() }, CommandType.BuyAllianceItem),
                    _ => TestSupport.Place("oxygen_plant", random.Next(0, 6), random.Next(-1, 12))
                };
                _world.HandleCommand(session, envelope);
                if (i % 50 == 0)
                {
                    _clock.Advance(TimeSpan.FromMinutes(random.Next(1, 120)));
                    _world.Tick();
                }

                AssertNothingNegative(session);
                AssertNothingNegative(mate);
            }
        }

        // --- Concurrency ---------------------------------------------------------------------------

        /// <summary>
        /// The same player on two connections (a reconnect while the old socket still delivers), many threads, every
        /// command id sent four times: exactly the affordable number of pulls go through and the wallet adds up to the unit.
        /// </summary>
        [Fact]
        public async Task Concurrent_spends_from_two_connections_add_up_exactly()
        {
            var (session, _) = await Connect("eco-race");
            var price = TestSupport.Catalog.PodTenCost.Single(c => c.Key == ResourceType.Hadalite).Value;
            session.StateForTests.Wallet[ResourceType.Hadalite] = price * 30;
            var ids = Enumerable.Range(0, 50).Select(i => "race-" + i).ToList();
            var envelopes = ids.SelectMany(id => Enumerable.Repeat(id, 4))
                .Select(id => Envelope(new ExecuteGachaCommand { CommandId = id, PullCount = 10 }, CommandType.ExecuteGacha))
                .OrderBy(_ => Guid.NewGuid()).ToList();

            // A second connection replaces the first while commands are in flight on both.
            var second = Task.Run(() => _world.ConnectAsync("eco-race", new RecordingConnection(), default, "eco-race"));
            using var stop = new CancellationTokenSource();
            var ticker = Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                    _world.Tick();
            });
            await Task.WhenAll(Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
            {
                for (var i = worker; i < envelopes.Count; i += 8)
                    _world.HandleCommand(session, envelopes[i]);
            })));
            await second;
            stop.Cancel();
            await ticker;

            var ledger = session.StateForTests.RecentCommands.Where(r => ids.Contains(r.Id)).ToList();
            Assert.Equal(50, ledger.Count);
            Assert.Equal(30, ledger.Count(r => r.Result == CommandResultCode.Accepted));
            Assert.Equal(20, ledger.Count(r => r.Result == CommandResultCode.Rejected));
            Assert.Equal(0, Wallet(session, ResourceType.Hadalite));
            AssertNothingNegative(session);
        }

        /// <summary>
        /// Commands for the session, the hub, the world and the mailbox from many threads at once, with ticks and saves
        /// running: the lock order (world → hub/events/mail → session) holds, nothing deadlocks, nothing goes negative.
        /// </summary>
        [Fact]
        public async Task Mixed_traffic_on_every_service_never_deadlocks_or_goes_negative()
        {
            var players = new List<PlayerSession>();
            for (var i = 0; i < 4; i++)
                players.Add((await Connect("eco-mix-" + i)).Session);
            var leaderConnection = new RecordingConnection();
            players[0] = await _world.ConnectAsync("eco-mix-0", leaderConnection, default, "eco-mix-0");
            Fill(players[0], 200_000);
            _world.HandleCommand(players[0], Envelope(new CreateAllianceCommand { CommandId = Id(), Name = "Mix House", Tag = "MIXH" }, CommandType.CreateAlliance));
            var allianceId = TestSupport.Serializer.Deserialize<AllianceDto>(leaderConnection.Messages.Where(m => m.Delta != null)
                .SelectMany(m => m.Delta().Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
            foreach (var p in players.Skip(1))
                _world.HandleCommand(p, Envelope(new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance));
            foreach (var p in players)
            {
                Fill(p, 200_000);
                p.StateForTests.Items[PvpRules.Shield8h] = 5;
            }

            using var stop = new CancellationTokenSource();
            var background = Task.Run(async () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    _world.Tick();
                    await _world.SaveAllAsync(default);
                }
            });

            var work = players.Select((p, n) => Task.Run(() =>
            {
                var random = new Random(n);
                for (var i = 0; i < 60; i++)
                {
                    var other = players[(n + 1 + random.Next(players.Count - 1)) % players.Count];
                    var envelope = random.Next(8) switch
                    {
                        0 => Envelope(new DonateAllianceResearchCommand { CommandId = Id() }, CommandType.DonateAllianceResearch),
                        1 => Envelope(new SendMailCommand
                        {
                            CommandId = Id(), PlayerId = other.PublicId, Title = "m",
                            Resources = new[] { new ResourceAmountEntry { Key = ResourceType.Food, Value = 100 } }
                        }, CommandType.SendMail),
                        2 => Envelope(new ExecuteGachaCommand { CommandId = Id(), PullCount = 1 }, CommandType.ExecuteGacha),
                        3 => Envelope(new TeleportCommand { CommandId = Id(), Mode = TeleportRules.Random }, CommandType.Teleport),
                        4 => Envelope(new ViewWorldCommand { CommandId = Id(), X = -1, Y = -1 }, CommandType.ViewWorld),
                        5 => Envelope(new UseShieldCommand { CommandId = Id(), ItemId = PvpRules.Shield8h }, CommandType.UseShield),
                        6 => Envelope(new SendChatCommand { CommandId = Id(), Channel = (int)ChatChannel.Alliance, Text = "hi" }, CommandType.SendChat),
                        _ => Envelope(new ViewProfileCommand { CommandId = Id(), PlayerId = other.PublicId }, CommandType.ViewProfile)
                    };
                    _world.HandleCommand(p, envelope);
                }
            })).ToArray();

            var all = Task.WhenAll(work);
            Assert.Same(all, await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(60))));
            stop.Cancel();
            await background;
            await _world.Mail.DrainAsync();
            foreach (var p in players)
                AssertNothingNegative(p);
        }

        // --- Overflow and the storage cap ----------------------------------------------------------

        [Fact]
        public void Wallet_arithmetic_saturates_instead_of_wrapping()
        {
            Assert.Equal(long.MaxValue, WalletOps.Sum(long.MaxValue, 1));
            Assert.Equal(long.MinValue, WalletOps.Sum(long.MinValue, -1));
            Assert.Equal(5, WalletOps.Sum(2, 3));
            Assert.Equal(long.MaxValue, WalletOps.Product(long.MaxValue / 2, 3));
            Assert.Equal(0, WalletOps.Product(-4, 3));

            var wallet = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = long.MaxValue - 5 };
            WalletOps.Credit(wallet, ResourceType.Titanium, 100);
            Assert.Equal(long.MaxValue, wallet[ResourceType.Titanium]);
            WalletOps.Credit(wallet, ResourceType.Food, -100);
            Assert.Equal(0, wallet[ResourceType.Food]);

            // Production at the cap: a balance above it is kept, never cut down to the cap.
            var caps = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 1000 };
            var over = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 5000 };
            WalletOps.Add(over, new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 50 }, caps);
            Assert.Equal(5000, over[ResourceType.Titanium]);
            WalletOps.Add(over, new Dictionary<ResourceType, long> { [ResourceType.Titanium] = long.MaxValue }, caps);
            Assert.Equal(5000, over[ResourceType.Titanium]);
            WalletOps.Add(over, new Dictionary<ResourceType, long> { [ResourceType.Titanium] = -200 }, caps);
            Assert.Equal(4800, over[ResourceType.Titanium]);

            Assert.Equal(5000, ProductionFormulas.ApplyToBalance(5000, 10, 1000));
            Assert.Equal(4990, ProductionFormulas.ApplyToBalance(5000, -10, 1000));
            Assert.Equal(1000, ProductionFormulas.ApplyToBalance(995, 10, 1000));
            Assert.Equal(long.MaxValue, ProductionFormulas.ApplyToBalance(long.MaxValue, 1, 0));
            Assert.Equal(0, ProductionFormulas.ApplyToBalance(3, -10, 1000));
        }

        /// <summary>The second bug: a grant that passed long.MaxValue wrapped to a negative number and was clamped to zero.</summary>
        [Fact]
        public async Task A_huge_grant_saturates_and_never_wipes_the_balance()
        {
            var (session, _) = await Connect("eco-grant");
            session.Grant(new Dictionary<ResourceType, long> { [ResourceType.Titanium] = long.MaxValue - 10 }, _clock.UtcNow);
            session.Grant(new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 1000 }, _clock.UtcNow);
            Assert.Equal(long.MaxValue, Wallet(session, ResourceType.Titanium));

            // Offline (stored) players take the same path.
            await _world.DisconnectAsync(session, session.Connection!, default);
            Assert.True(await _world.GrantAsync("eco-grant", new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 5 }, default));
            Assert.Equal(long.MaxValue, (await _repository.LoadByIdentityAsync("eco-grant", default))!.Wallet[ResourceType.Titanium]);

            // A huge capsule opened many times saturates as well.
            var (again, _) = await Connect("eco-grant");
            var capsule = Capsules.Id(ResourceType.Biomass, long.MaxValue / 4);
            again.StateForTests.Items[capsule] = 10;
            _world.HandleCommand(again, Envelope(new OpenItemCommand { CommandId = Id(), ItemId = capsule, Count = 10 }, CommandType.OpenItem));
            Assert.Equal(long.MaxValue, Wallet(again, ResourceType.Biomass));
            AssertNothingNegative(again);
        }

        [Fact]
        public async Task Production_never_takes_away_a_balance_above_the_cap()
        {
            var (session, _) = await Connect("eco-cap");
            session.Grant(new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 50_000_000 }, _clock.UtcNow);
            var before = Wallet(session, ResourceType.Titanium);
            for (var hour = 0; hour < 6; hour++)
            {
                _clock.Advance(TimeSpan.FromHours(1));
                _world.Tick();
                Assert.Equal(before, Wallet(session, ResourceType.Titanium));
            }

            // Spending from an over-cap balance works and keeps the rest.
            var place = TestSupport.Place("oxygen_plant", 1, 1);
            _world.HandleCommand(session, place);
            Assert.Equal(CommandResultCode.Accepted, Acks(session.Connection as RecordingConnection ?? new RecordingConnection(), place.CommandId).Single().Result);
            Assert.True(Wallet(session, ResourceType.Titanium) <= before);
            Assert.True(Wallet(session, ResourceType.Titanium) > before - 1_000_000);
        }
    }
}
