using System.Linq;
using HADAL.Shared.Simulation;
using Hadal.Core.Art;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>World-map sea creatures (Art/Creatures): art budget, library, motion, wildlife and their texts.</summary>
    public sealed class CreatureTests
    {
        private static readonly string[] Legacy = { "creature_crawler", "creature_siren" };

        [Test]
        public void Every_monster_kind_has_a_creature_and_a_name()
        {
            var english = Loc.Parse(Resources.Load<TextAsset>("Localization/en").text);
            var turkish = Loc.Parse(Resources.Load<TextAsset>("Localization/tr").text);
            foreach (var kind in MonsterRules.Kinds)
            {
                var creature = CreatureProfiles.ForMonster(kind.Id);
                Assert.IsTrue(CreatureProfiles.Get(creature) != null || Legacy.Contains(creature), $"{kind.Id}: no creature");
                Assert.IsTrue(english.ContainsKey("monster." + kind.Id), $"en: monster.{kind.Id}");
                Assert.IsTrue(turkish.ContainsKey("monster." + kind.Id), $"tr: monster.{kind.Id}");
            }

            foreach (var action in EventActions.All)
                Assert.IsTrue(english.ContainsKey("event.action." + action), $"en: event.action.{action}");
            foreach (var key in new[] { "world.boss.tag", "world.boss.body", "world.boss.leaves", "world.boss.hint", "world.boss.board", "world.boss.myrank",
                         "world.estimate.damage", "world.estimate.nodamage", "mail.boss.killed", "mail.boss.gone", "mail.boss.ranked", "mail.boss.joined" })
                Assert.IsTrue(turkish.ContainsKey(key), key);
        }

        [Test]
        public void The_library_holds_every_creature_within_the_mobile_budget()
        {
            var library = Resources.Load<CreatureLibrary>(CreatureLibrary.ResourcePath);
            Assert.IsNotNull(library, "Resources/" + CreatureLibrary.ResourcePath + " missing (Hadal > Setup > Creatures)");
            Assert.IsNotNull(library.Lure, "no lure material");
            foreach (var profile in CreatureProfiles.All)
            {
                Assert.IsTrue(library.TryGet(profile.Id, out var entry), $"{profile.Id} missing");
                Assert.LessOrEqual(entry.triangles, 15000, $"{profile.Id}: {entry.triangles} triangles");
                Assert.Greater(entry.triangles, 200, $"{profile.Id}: empty mesh");
                Assert.IsNotNull(entry.material, $"{profile.Id}: no material");
                Assert.AreEqual("Hadal/Creature", entry.material.shader.name, profile.Id);
                var albedo = entry.material.GetTexture("_BaseMap");
                Assert.IsNotNull(albedo, $"{profile.Id}: no albedo");
                Assert.LessOrEqual(Mathf.Max(albedo.width, albedo.height), 1024, $"{profile.Id}: texture too big");
                Assert.Greater(entry.boundsSize.y, 0.05f, $"{profile.Id}: no bounds");
            }
        }

        [Test]
        public void A_spawned_creature_stands_at_its_map_height_and_anglers_carry_a_lure()
        {
            var library = Resources.Load<CreatureLibrary>(CreatureLibrary.ResourcePath);
            Assume.That(library, Is.Not.Null);
            var parent = new GameObject("map").transform;
            try
            {
                foreach (var profile in CreatureProfiles.All)
                {
                    var motion = library.Spawn(profile.Id, parent);
                    Assert.IsNotNull(motion, profile.Id);
                    motion.Body.localRotation = Quaternion.identity; // measure the rest pose, not a roll into a turn
                    var bounds = motion.Body.GetComponentsInChildren<MeshRenderer>().Where(r => r.name != "Lure")
                        .Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                    Assert.AreEqual(profile.MapHeight, bounds.size.y, profile.MapHeight * 0.15f, $"{profile.Id}: map height");
                    Assert.AreEqual(profile.HasLure, motion.Body.Find("Lure") != null, $"{profile.Id}: lure");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void A_creature_lunges_out_and_back_and_a_beaten_one_sinks_away()
        {
            var body = new GameObject("body").transform;
            var root = new GameObject("creature");
            body.SetParent(root.transform, false);
            var motion = root.AddComponent<CreatureMotion>();
            try
            {
                motion.Configure(CreatureProfiles.Get("creature_armored_crab"), body, 1f, Vector3.zero, Vector3.one, null);
                motion.Attack();
                var peak = 0f;
                for (var i = 1; i <= 10; i++)
                {
                    motion.Step(i * 0.1f, 0.1f);
                    peak = Mathf.Max(peak, motion.LungeOffset);
                }

                Assert.Greater(peak, 1f, "no lunge");
                Assert.AreEqual(0f, motion.LungeOffset, 1e-4f, "did not come back");

                var scale = body.localScale.y;
                motion.Die();
                for (var i = 1; i <= 20; i++)
                    motion.Step(1f + i * 0.1f, 0.1f);
                Assert.IsTrue(motion.Dying);
                Assert.Less(body.localScale.y, scale * 0.05f, "did not shrink away");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void A_swimmer_turns_toward_where_it_swims()
        {
            var body = new GameObject("body").transform;
            var root = new GameObject("fish");
            body.SetParent(root.transform, false);
            var motion = root.AddComponent<CreatureMotion>();
            try
            {
                motion.Configure(CreatureProfiles.Get("creature_abyss_angler"), body, 1f, Vector3.zero, Vector3.one, null);
                for (var i = 1; i <= 200; i++)
                {
                    var before = body.localPosition;
                    motion.Step(i * 0.05f, 0.05f);
                    if (i < 120)
                        continue;
                    var moved = body.localPosition - before;
                    moved.y = 0f;
                    if (moved.sqrMagnitude < 1e-8f)
                        continue;
                    var facing = body.forward;
                    facing.y = 0f;
                    Assert.Greater(Vector3.Dot(facing.normalized, moved.normalized), 0.8f, "swims sideways");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Wildlife_is_a_small_fixed_set_that_follows_the_camera()
        {
            var library = Resources.Load<CreatureLibrary>(CreatureLibrary.ResourcePath);
            Assume.That(library, Is.Not.Null);
            var wildlife = new GameObject("wildlife").AddComponent<MapWildlife>();
            try
            {
                Assert.IsTrue(wildlife.Build(library, Vector3.zero));
                Assert.LessOrEqual(wildlife.BodyCount, 30);
                Assert.AreEqual(MapWildlife.Schools * MapWildlife.FishPerSchool + MapWildlife.Jellies, wildlife.BodyCount);

                var far = new Vector3(2000f, 0f, 2000f);
                wildlife.Tick(far, true, 0.1f);
                foreach (Transform child in wildlife.transform)
                {
                    var flat = new Vector2(child.position.x - far.x, child.position.z - far.z);
                    Assert.Less(flat.magnitude, MapWildlife.Range * 1.4f + 40f, child.name + " left behind");
                }

                wildlife.Tick(far, false, 0.1f);
                Assert.IsFalse(wildlife.gameObject.activeSelf, "shown in the strategic view");
            }
            finally
            {
                Object.DestroyImmediate(wildlife.gameObject);
            }
        }

        [Test]
        public void Boss_prize_mails_read_in_the_players_language()
        {
            var before = Loc.Current;
            try
            {
                Loc.Use("en");
                Assert.AreEqual("Chasm Maw defeated!", MailText.Show("loc:mail.boss.killed|monster.chasm_maw", "event"));
                StringAssert.StartsWith("In the fight against Depth Kraken you ranked 2 in your alliance (640 damage)",
                    MailText.Show("loc:mail.boss.ranked|monster.depth_kraken|2|640", "event"));
                Loc.Use("tr");
                Assert.AreEqual("Uçurum Ağzı yenildi!", MailText.Show("loc:mail.boss.killed|monster.chasm_maw", "event"));
                // A player's mail is shown as written, whatever it starts with.
                Assert.AreEqual("loc:mail.boss.killed|x", MailText.Show("loc:mail.boss.killed|x", "player"));
                Assert.AreEqual("Merhaba", MailText.Show("Merhaba", "system"));
            }
            finally
            {
                Loc.Use(before);
            }
        }

        [Test]
        public void The_creature_shader_compiles()
        {
            var shader = Shader.Find("Hadal/Creature");
            Assert.IsNotNull(shader);
            Assert.IsTrue(shader.isSupported);
        }
    }
}
