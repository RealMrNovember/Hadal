using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Art;
using Hadal.Core.Catalog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Monsters on the map (docs/rehber 09 §5): a swarm model with a red level plate; tapping it shows its power and
    /// Attack, which opens the team setup in attack mode (the enemy's power against the team's, the Sonar cost). The
    /// magnifier finds the nearest swarm of a chosen level, like the genre's monster search. The new sea creatures
    /// (Art/Creatures) move procedurally (<see cref="CreatureMotion"/>); a team's arrival makes them lunge and flash,
    /// a beaten one sinks away. Event bosses (MonsterKind.EventOnly) show their shared body and the time they stay.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private readonly Dictionary<string, (GameObject Model, RectTransform Plate)> _monsters = new();
        private readonly Dictionary<string, List<CreatureMotion>> _monsterBodies = new();
        private readonly HashSet<string> _arrivalsShown = new();
        private MapWildlife _wildlife;
        private TMP_Text _sonar;
        private int _searchLevel = 1;

        private static readonly Color BossPlate = new(0.62f, 0.3f, 0.95f, 1f);

        public static string MonsterName(string type) => Loc.T("monster." + type);

        private void ApplyMonsters(WorldViewDto view)
        {
            var seen = new HashSet<string>();
            foreach (var m in view.Monsters ?? Array.Empty<WorldMonsterDto>())
            {
                seen.Add(m.Id);
                if (!_monsters.TryGetValue(m.Id, out var entry))
                {
                    entry = (BuildMonster(m), BuildNodePlate());
                    _monsters[m.Id] = entry;
                }

                entry.Model.transform.position = TileToWorld(m.X, m.Y);
                var plate = entry.Plate;
                var boss = m.HpMax > 0;
                plate.Find("Level").GetComponent<Image>().color = boss ? BossPlate : new Color(0.86f, 0.27f, 0.27f, 1f);
                plate.Find("Level/Text").GetComponent<TMP_Text>().text = m.Level.ToString();
                plate.Find("Level/Text").GetComponent<TMP_Text>().color = Color.white;
                plate.Find("Name").GetComponent<TMP_Text>().text = MonsterName(m.Type)
                    + (boss ? $" <color=#C9A6FF>{BossPercent(m)}%</color>" : m.Busy ? " <color=#FFB347>⚔</color>" : string.Empty);
                if (boss)
                    BossBar(plate, m);
                var button = plate.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                var info = m;
                button.onClick.AddListener(() => ShowMonster(info));
            }

            foreach (var id in _monsters.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                // Beaten (or out of view): the creatures sink away before the model goes.
                var dying = _monsterBodies.TryGetValue(id, out var bodies) && bodies.Count > 0;
                if (dying)
                {
                    foreach (var body in bodies)
                        body.Die();
                }

                Destroy(_monsters[id].Model, dying ? 1.6f : 0f);
                Destroy(_monsters[id].Plate.gameObject);
                _monsters.Remove(id);
                _monsterBodies.Remove(id);
            }
        }

        /// <summary>
        /// The boss's damage board for my alliance (server-ranked like the payout): the top five and my own line. Ranks 1–3
        /// get the biggest prizes when it dies or its event ends; everyone who hit it gets something.
        /// </summary>
        private static string BossBoardText(WorldMonsterDto m)
        {
            var board = m.Board ?? Array.Empty<BossBoardEntryDto>();
            var text = $"\n<b>{Loc.T("world.boss.board")}</b>";
            if (board.Length == 0)
                return text + $"\n<color=#8CA3AD>{Loc.T("world.boss.noboard")}</color>";
            foreach (var e in board.Where(e => e.Rank > 0 && e.Rank <= 5))
            {
                var medal = e.Rank switch { 1 => "#FFD54F", 2 => "#CFD8DC", 3 => "#D7A86E", _ => "#8CA3AD" };
                text += $"\n<color={medal}>{e.Rank}.</color> {Escape(e.Name)}  {e.Damage:N0}";
            }

            text += m.MyRank > 5 ? $"\n<color=#8CD9FF>{Loc.F("world.boss.myrank", m.MyRank, m.MyDamage.ToString("N0"))}</color>"
                : m.MyRank == 0 && m.MyDamage > 0 ? $"\n<color=#8CD9FF>{Loc.F("world.boss.mydamage", m.MyDamage.ToString("N0"))}</color>" : string.Empty;
            return text;
        }

        private static int BossPercent(WorldMonsterDto m) => m.HpMax <= 0 ? 0 : (int)Math.Ceiling(100.0 * m.HpLeft / m.HpMax);

        /// <summary>A thin bar under a boss's plate: what is left of its shared body.</summary>
        private static void BossBar(RectTransform plate, WorldMonsterDto m)
        {
            var bar = plate.Find("Hp") as RectTransform;
            if (bar == null)
            {
                var back = UiKit.Box("Hp", plate, new Color(0.08f, 0.04f, 0.12f, 0.9f));
                back.raycastTarget = false;
                bar = back.rectTransform;
                bar.anchorMin = new Vector2(0f, -0.22f);
                bar.anchorMax = new Vector2(1f, -0.04f);
                bar.offsetMin = bar.offsetMax = Vector2.zero;
                var fill = UiKit.Box("Fill", bar, BossPlate);
                fill.raycastTarget = false;
                fill.rectTransform.Place(0f, 0f, 1f, 1f);
            }

            var share = m.HpMax <= 0 ? 0f : Mathf.Clamp01((float)m.HpLeft / m.HpMax);
            ((RectTransform)bar.Find("Fill")).Place(0f, 0f, share, 1f);
        }

        private GameObject BuildMonster(WorldMonsterDto m)
        {
            var root = new GameObject("Monster_" + m.Id);
            root.transform.SetParent(_root, false);
            var bodies = new List<CreatureMotion>();
            _monsterBodies[m.Id] = bodies;

            // The new sea creatures: a school, a pair or one body; grows a little with the level (bosses do not).
            var creatures = CreatureLibrary.Instance;
            var creatureId = CreatureProfiles.ForMonster(m.Type);
            var group = CreatureProfiles.GroupSize(m.Type);
            var growth = m.HpMax > 0 ? 1f : 0.85f + m.Level * 0.012f;
            for (var i = 0; creatures != null && i < group; i++)
            {
                var body = creatures.Spawn(creatureId, root.transform, i == 0 ? growth : growth * 0.7f);
                if (body == null)
                    break;
                var angle = (m.X * 37 + m.Y * 11 + i * 120) % 360;
                body.transform.localPosition = i == 0 ? Vector3.zero : Quaternion.Euler(0, angle, 0) * new Vector3(3.2f, 0f, 0f);
                body.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                bodies.Add(body);
            }

            if (bodies.Count > 0)
                return root;

            var prefab = CharacterLibrary.Instance?.Model(creatureId == "creature_siren" ? "creature_siren" : "creature_crawler");
            if (prefab == null)
                return root;

            // A swarm: the leader and two smaller ones around it, growing with the level.
            var size = 3.2f + m.Level * 0.12f;
            for (var i = 0; i < 3; i++)
            {
                var body = Instantiate(prefab, root.transform);
                var angle = (m.X * 37 + m.Y * 11 + i * 120) % 360;
                body.transform.localPosition = i == 0 ? Vector3.zero : Quaternion.Euler(0, angle, 0) * new Vector3(2.4f, 0f, 0f);
                body.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                body.transform.localScale = Vector3.one * (i == 0 ? size : size * 0.65f);
                var motion = body.AddComponent<CreatureMotion>();
                motion.ConfigureSimple(body.transform, size);
                bodies.Add(motion);
            }

            return root;
        }

        /// <summary>When one of my teams reaches a monster, its creatures lunge at it and flash with the hits.</summary>
        private void ShowArrivals()
        {
            var marches = View.Marches?.Marches;
            if (marches == null)
                return;
            var now = View.ServerNowMs;
            foreach (var march in marches)
            {
                if (MonsterRules.Kind(march.NodeType) == null || march.ArrivesAtMs > now || now - march.ArrivesAtMs > 4000 || !_arrivalsShown.Add(march.Id))
                    continue;
                if (!_monsterBodies.TryGetValue(march.NodeId, out var bodies))
                    continue;
                foreach (var body in bodies)
                {
                    if (body == null)
                        continue;
                    body.Attack();
                    body.Hit();
                }
            }

            if (_arrivalsShown.Count > 64)
                _arrivalsShown.Clear();
        }

        /// <summary>Schools of glowing fish and drifting jellies around the camera (cosmetic, client-only).</summary>
        private void UpdateWildlife()
        {
            if (_wildlife == null)
            {
                var library = CreatureLibrary.Instance;
                if (library == null)
                    return;
                _wildlife = new GameObject("Wildlife").AddComponent<MapWildlife>();
                _wildlife.transform.SetParent(_root, false);
                if (!_wildlife.Build(library, _focus))
                {
                    _wildlife.gameObject.SetActive(false);
                    return;
                }
            }

            if (_wildlife.BodyCount > 0)
                _wildlife.Tick(_focus, _distance < 200f, Time.deltaTime);
        }

        private void UpdateMonsterPlates()
        {
            ShowArrivals();
            UpdateWildlife();
            var near = _distance < 120f;
            foreach (var (model, plate) in _monsters.Values)
            {
                var screen = _camera.WorldToScreenPoint(model.transform.position + Vector3.up * 6f);
                var visible = near && screen.z > 0f && _popup == null;
                plate.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_labels, screen, null, out var local);
                plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
                plate.anchoredPosition = local;
            }
        }

        private int SonarNow() => View.Marches?.SonarEnergy ?? MonsterRules.SonarCapacity;

        /// <summary>The magnifier and the Sonar gauge at the bottom left (Last Asylum's monster search).</summary>
        private void BuildSearch(Transform safe)
        {
            var search = UiKit.Rect("Search", safe).Place(0.02f, 0.155f, 0.15f, 0.215f);
            var disc = search.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.preserveAspect = true;
            disc.color = new Color(0.05f, 0.2f, 0.27f, 0.95f);
            search.gameObject.AddComponent<Button>().onClick.AddListener(OpenSearch);
            search.gameObject.AddComponent<PressFeedback>();
            Hud.HudIcons.Image(search, "left_search", 0.2f, 0.2f, 0.8f, 0.8f).raycastTarget = false;

            var sonar = UiKit.Box("Sonar", safe, new Color(0.02f, 0.05f, 0.08f, 0.85f));
            sonar.rectTransform.Place(0.02f, 0.105f, 0.19f, 0.145f);
            sonar.raycastTarget = false;
            _sonar = UiKit.Label("Text", sonar.transform, string.Empty, 22, new Color(0.55f, 0.85f, 1f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _sonar.rectTransform.Place(0, 0, 1, 1);
        }

        private void UpdateSonar()
        {
            if (_sonar != null)
                _sonar.text = $"{Loc.T("world.sonar")} {SonarNow()}/{View.Marches?.SonarCapacity ?? MonsterRules.SonarCapacity}";
        }

        /// <summary>Pick a level, then glide to the nearest swarm of it in view.</summary>
        private void OpenSearch()
        {
            ClosePopup();
            TargetMenu.Hide();
            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.5f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.04f, 0.96f, 0.42f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().Slide = true;

            UiKit.Label("Title", card.transform, Loc.T("world.search.title"), 34, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.82f, 0.8f, 0.97f);
            // The kinds the map lays out by itself (event bosses come with events), three to a row.
            var kinds = MonsterRules.Roaming;
            for (var i = 0; i < kinds.Length; i++)
            {
                var x0 = 0.05f + i % 3 * 0.305f;
                var y1 = 0.79f - i / 3 * 0.15f;
                var tile = UiKit.Box("Kind", card.transform, UiKit.Field);
                tile.rectTransform.Place(x0, y1 - 0.14f, x0 + 0.29f, y1);
                UiKit.Label("Name", tile.transform, $"{MonsterName(kinds[i].Id)}\n<size=16>{Loc.F("world.search.levels", kinds[i].MinLevel, kinds[i].MaxLevel)}</size>",
                    20, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
            }

            var minus = UiKit.Button("Minus", card.transform, "-", () => { _searchLevel = Math.Max(1, _searchLevel - 1); OpenSearch(); }, UiKit.Primary, 40);
            ((RectTransform)minus.transform).Place(0.05f, 0.28f, 0.2f, 0.42f);
            UiKit.Label("Level", card.transform, Loc.F("base.level", _searchLevel), 32, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.22f, 0.28f, 0.78f, 0.42f);
            var plus = UiKit.Button("Plus", card.transform, "+", () => { _searchLevel = Math.Min(35, _searchLevel + 1); OpenSearch(); }, UiKit.Primary, 40);
            ((RectTransform)plus.transform).Place(0.8f, 0.28f, 0.95f, 0.42f);

            var go = UiKit.Button("Find", card.transform, Loc.T("world.search.go"), FindMonster, UiKit.Primary, 36);
            ((RectTransform)go.transform).Place(0.25f, 0.05f, 0.75f, 0.22f);
        }

        private void FindMonster()
        {
            var view = View.WorldView;
            var target = (view?.Monsters ?? Array.Empty<WorldMonsterDto>())
                .Where(m => m.Level == _searchLevel && !m.Busy && m.HpMax == 0)
                .OrderBy(m => Math.Max(Math.Abs(m.X - view.MyX), Math.Abs(m.Y - view.MyY)))
                .FirstOrDefault();
            ClosePopup();
            if (target == null)
            {
                _toast?.Invoke(Loc.T("world.search.none"), UiKit.Amber);
                return;
            }

            ShowMonster(target);
        }

        /// <summary>A swarm: level, power, the trip and the Sonar it costs; Attack and Share.</summary>
        private void ShowMonster(WorldMonsterDto m)
        {
            var kind = MonsterRules.Kind(m.Type);
            var view = View.WorldView;
            var trip = view == null ? 0 : GatherRules.TravelSeconds(ClientCatalog.Catalog.World, view.MyX, view.MyY, m.X, m.Y);
            var cost = kind?.Sonar ?? 40;
            var info = $"{Loc.F("hero.power", m.Power.ToString("N0"))}   {Loc.T("world.sonar")} {cost}\n{Loc.F("world.monster.trip", Loc.Duration(trip))}";
            var boss = m.HpMax > 0;
            if (boss)
            {
                // An event boss: one shared body for everyone, here until its event ends.
                info += $"\n<color=#C9A6FF>{Loc.F("world.boss.body", BossPercent(m))}</color>   {Loc.F("world.boss.leaves", Loc.Duration(Math.Max(0, (m.EndsAtMs - View.ServerNowMs) / 1000f)))}";
                info += $"\n{Loc.T("world.boss.hint")}";
                info += BossBoardText(m);
            }
            else if (m.Busy)
                info += $"\n<color=#FFB347>{Loc.T("world.monster.busy")}</color>";
            var items = new List<TargetMenu.Item>
            {
                new("event_war", Loc.T("world.attack"), () => OpenAttackSetup(m), (boss ? m.HpLeft > 0 : !m.Busy) && SonarNow() >= cost),
                new("nav_world", Loc.T("share.title"), () => ShareLocation?.Invoke(m.X, m.Y, "enemy", $"{Loc.F("base.level", m.Level)} {MonsterName(m.Type)}"))
            };
            GlideTo(m.X, m.Y);
            var x = m.X;
            var y = m.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 3f,
                $"{Loc.F("base.level", m.Level)}  {MonsterName(m.Type)}" + (boss ? $"  <color=#C9A6FF>{Loc.T("world.boss.tag")}</color>" : string.Empty), info, items);
        }

        private void Attack(string monsterId, string[] heroIds) =>
            _commands.Dispatch(new MarchAttackCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, MonsterId = monsterId, HeroIds = heroIds });
    }
}
