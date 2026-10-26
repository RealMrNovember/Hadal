using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>Heroes, chat and alliances on the client (docs/25).</summary>
    public sealed class SocialClientTests
    {
        private readonly NetworkSerializationLayer _serialization = new();
        private readonly ProtobufNetNetworkSerializer _serializer = new();
        private ClientStateView _view;

        [SetUp]
        public void SetUp()
        {
            _view = new ClientStateView();
            _view.ResetSession();
        }

        private void Apply(string entityId, object payload, EntityChangeKind kind = EntityChangeKind.Update) =>
            ClientStateViewMutator.ApplyDelta(_view, new StateDelta
            {
                ServerTick = 2,
                Changes = new List<EntityChangeDto>
                {
                    new() { EntityId = entityId, Kind = kind, Payload = payload == null ? System.Array.Empty<byte>() : _serializer.Serialize(payload) }
                }
            }, _serialization, new List<CommandAckDto>());

        [Test]
        public void Heroes_and_pod_results_replicate()
        {
            ClientStateViewMutator.ApplySnapshot(_view, new StateSnapshot
            {
                ServerTick = 1,
                PlayerId = "me",
                Heroes = new HeroRosterDto
                {
                    Heroes = new[] { new HeroStateDto { DefinitionId = "mara", Level = 3, Stars = 1, CrewSlot = 0 } },
                    SincePity = 4,
                    FreePullRemainingMs = 0
                }
            }, _serialization);

            Assert.AreEqual("me", _view.PlayerId);
            Assert.IsTrue(_view.TryGetHero("mara", out var mara));
            Assert.AreEqual((3, 0), (mara.Level, mara.Slot));
            Assert.AreEqual(4, _view.SincePity);

            Apply(EntityIds.GachaResult + "c1", new GachaResultDto { Pulls = new[] { new GachaPullEntry { HeroId = "lyra", IsNew = true } } });
            Assert.IsTrue(_view.TryTakeGachaResult(out var result));
            Assert.AreEqual("lyra", result.Pulls.Single().HeroId);
        }

        [Test]
        public void Chat_history_new_lines_and_unread()
        {
            _view.PlayerId = "me";
            Apply(EntityIds.ChatHistory, new ChatHistoryDto { Messages = new[] { new ChatMessageDto { Id = 1, SenderId = "x", Text = "hi" } } });
            Assert.AreEqual(1, _view.Chat.Count);

            Apply(EntityIds.ChatMessage + "2", new ChatMessageDto { Id = 2, SenderId = "x", Text = "there" });
            Apply(EntityIds.ChatMessage + "3", new ChatMessageDto { Id = 3, SenderId = "me", Text = "mine" });
            Apply(EntityIds.ChatMessage + "3", new ChatMessageDto { Id = 3, SenderId = "me", Text = "mine" }); // duplicate
            Assert.AreEqual(3, _view.Chat.Count);
            Assert.AreEqual(1, _view.UnreadChat); // own lines do not count

            // An empty history arrives as an empty payload.
            Apply(EntityIds.ChatHistory, null);
            Assert.AreEqual(0, _view.Chat.Count);
        }

        [Test]
        public void Leaving_the_alliance_drops_its_channel()
        {
            Apply(EntityIds.MyAlliance, new AllianceDto { Id = "a1", Tag = "AWD", Members = new[] { new AllianceMemberDto { PlayerId = "me" } } });
            Assert.AreEqual("AWD", _view.MyAlliance.Tag);
            Apply(EntityIds.ChatMessage + "1", new ChatMessageDto { Id = 1, Channel = (int)ChatChannel.Alliance, Text = "secret" });
            Apply(EntityIds.ChatMessage + "2", new ChatMessageDto { Id = 2, Channel = (int)ChatChannel.World, Text = "public" });
            Apply(EntityIds.AllianceHelp, new HelpListDto { Requests = new[] { new HelpRequestDto { RequestId = "h1" } } });
            Assert.AreEqual(1, _view.HelpRequests.Length);

            Apply(EntityIds.MyAlliance, null, EntityChangeKind.Delete);
            Apply(EntityIds.AllianceHelp, null);
            Assert.IsNull(_view.MyAlliance);
            Assert.AreEqual("public", _view.Chat.Single().Text);
            Assert.AreEqual(0, _view.HelpRequests.Length);
        }

        [Test]
        public void Quests_bag_and_reward_reports_replicate()
        {
            ClientStateViewMutator.ApplySnapshot(_view, new StateSnapshot
            {
                ServerTick = 1,
                Quests = new QuestStateDto { Chapter = new QuestProgressDto { Id = "q_oxygen2", Progress = 1, Target = 2 }, ChapterNumber = 1, ChapterTotal = 20, DailyResetMs = 60_000 },
                Inventory = new InventoryDto
                {
                    Items = new[] { new ItemCountDto { Id = "chest_bronze", Count = 2 } },
                    HeroCards = new[] { new ItemCountDto { Id = "juno", Count = 12 } }
                }
            }, _serialization);

            Assert.AreEqual("q_oxygen2", _view.Quests.Chapter.Id);
            Assert.AreEqual(2, _view.Items["chest_bronze"]);
            Assert.AreEqual(12, _view.HeroCards["juno"]);

            // An empty bag arrives as an empty payload.
            Apply(EntityIds.Inventory, null);
            Assert.AreEqual(0, _view.Items.Count);

            Apply(EntityIds.RewardReport + "c1", new RewardReportDto { Rewards = new[] { new RewardEntryDto { Type = "shards", Id = "lyra", Amount = 10 } } });
            Assert.IsTrue(_view.TryTakeRewardReport(out var report));
            Assert.AreEqual("lyra", report.Rewards.Single().Id);
        }

        [Test]
        public void The_catalog_quests_and_items_are_consistent()
        {
            var catalog = ClientCatalog.Catalog;
            Assert.GreaterOrEqual(catalog.Quests.chapter.Length, 10);
            Assert.IsTrue(catalog.TryGetItem("chest_gold", out var gold));
            var rewards = QuestRules.Open(catalog, gold, n => 0);
            Assert.That(rewards.Any(r => r.Type == "resource" && r.Id == "Hadalite"));
            Assert.IsTrue(catalog.TryGetHero("mara", out var mara));
            Assert.AreEqual("rourke", mara.Portrait);
        }

        [Test]
        public void The_shared_catalog_has_heroes_and_the_crew_speeds_things_up()
        {
            var catalog = ClientCatalog.Catalog;
            Assert.GreaterOrEqual(catalog.Heroes.Count, 8);
            Assert.IsTrue(catalog.TryGetHero(catalog.HeroRules.starterHeroId, out var starter));

            var modifiers = ResearchRules.Modifiers(catalog, new Dictionary<string, int>());
            HeroRules.AddCrew(catalog, modifiers, new[] { new OwnedHero(starter.Id, 1, 1, 0, 0) });
            Assert.AreEqual(HeroRules.EffectValue(starter, 1, 1), modifiers.BuildTimePercent);
            Assert.Less(modifiers.BuildTimePercent, 0);
        }
    }
}
