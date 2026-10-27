using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Monsters on the map (docs/rehber 09 §5): a swarm model with a red level plate; tapping it shows its power and
    /// Attack, which opens the team setup in attack mode (the enemy's power against the team's, the Sonar cost). The
    /// magnifier finds the nearest swarm of a chosen level, like the genre's monster search.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private readonly Dictionary<string, (GameObject Model, RectTransform Plate)> _monsters = new();
        private Text _sonar;
        private int _searchLevel = 1;

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
                plate.Find("Level").GetComponent<Image>().color = new Color(0.86f, 0.27f, 0.27f, 1f);
                plate.Find("Level/Text").GetComponent<Text>().text = m.Level.ToString();
                plate.Find("Level/Text").GetComponent<Text>().color = Color.white;
                plate.Find("Name").GetComponent<Text>().text = MonsterName(m.Type) + (m.Busy ? " <color=#FFB347>⚔</color>" : string.Empty);
                var button = plate.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                var info = m;
                button.onClick.AddListener(() => ShowMonster(info));
            }

            foreach (var id in _monsters.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                Destroy(_monsters[id].Model);
                Destroy(_monsters[id].Plate.gameObject);
                _monsters.Remove(id);
            }
        }

        private GameObject BuildMonster(WorldMonsterDto m)
        {
            var root = new GameObject("Monster_" + m.Id);
            root.transform.SetParent(_root, false);
            var prefab = CharacterLibrary.Instance?.Model(m.Type == "siren_swarm" ? "creature_siren" : "creature_crawler");
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
            }

            return root;
        }

        private void UpdateMonsterPlates()
        {
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
            var search = UiKit.Rect("Search", safe).Place(0.02f, 0.24f, 0.15f, 0.3f);
            var disc = search.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.preserveAspect = true;
            disc.color = new Color(0.05f, 0.2f, 0.27f, 0.95f);
            search.gameObject.AddComponent<Button>().onClick.AddListener(OpenSearch);
            search.gameObject.AddComponent<PressFeedback>();
            Hud.HudIcons.Image(search, "left_search", 0.2f, 0.2f, 0.8f, 0.8f).raycastTarget = false;

            var sonar = UiKit.Box("Sonar", safe, new Color(0.02f, 0.05f, 0.08f, 0.85f));
            sonar.rectTransform.Place(0.02f, 0.19f, 0.19f, 0.23f);
            sonar.raycastTarget = false;
            _sonar = UiKit.Label("Text", sonar.transform, string.Empty, 22, new Color(0.55f, 0.85f, 1f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _sonar.rectTransform.Place(0, 0, 1, 1);
        }

        private void UpdateSonar()
        {
            if (_sonar != null)
                _sonar.text = $"⚡ {SonarNow()}/{View.Marches?.SonarCapacity ?? MonsterRules.SonarCapacity}";
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
            card.rectTransform.Place(0.04f, 0.04f, 0.96f, 0.36f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().Slide = true;

            UiKit.Label("Title", card.transform, Loc.T("world.search.title"), 34, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.8f, 0.8f, 0.96f);
            var kinds = MonsterRules.Kinds;
            for (var i = 0; i < kinds.Length; i++)
            {
                var x0 = 0.05f + i * 0.46f;
                var tile = UiKit.Box("Kind", card.transform, UiKit.Field);
                tile.rectTransform.Place(x0, 0.5f, x0 + 0.44f, 0.78f);
                UiKit.Label("Name", tile.transform, $"{MonsterName(kinds[i].Id)}\n<size=18>{Loc.F("world.search.levels", kinds[i].MinLevel, kinds[i].MaxLevel)}</size>",
                    24, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
            }

            var minus = UiKit.Button("Minus", card.transform, "-", () => { _searchLevel = Math.Max(1, _searchLevel - 1); OpenSearch(); }, UiKit.Primary, 40);
            ((RectTransform)minus.transform).Place(0.05f, 0.3f, 0.2f, 0.45f);
            UiKit.Label("Level", card.transform, Loc.F("base.level", _searchLevel), 32, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.22f, 0.3f, 0.78f, 0.45f);
            var plus = UiKit.Button("Plus", card.transform, "+", () => { _searchLevel = Math.Min(35, _searchLevel + 1); OpenSearch(); }, UiKit.Primary, 40);
            ((RectTransform)plus.transform).Place(0.8f, 0.3f, 0.95f, 0.45f);

            var go = UiKit.Button("Find", card.transform, Loc.T("world.search.go"), FindMonster, UiKit.Primary, 36);
            ((RectTransform)go.transform).Place(0.25f, 0.06f, 0.75f, 0.24f);
        }

        private void FindMonster()
        {
            var view = View.WorldView;
            var target = (view?.Monsters ?? Array.Empty<WorldMonsterDto>())
                .Where(m => m.Level == _searchLevel && !m.Busy)
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
            var info = $"{Loc.F("hero.power", m.Power.ToString("N0"))}   ⚡ {cost}\n{Loc.F("world.monster.trip", Loc.Duration(trip))}";
            if (m.Busy)
                info += $"\n<color=#FFB347>{Loc.T("world.monster.busy")}</color>";
            var items = new List<TargetMenu.Item>
            {
                new("event_war", Loc.T("world.attack"), () => OpenAttackSetup(m), !m.Busy && SonarNow() >= cost),
                new("nav_world", Loc.T("share.title"), () => ShareLocation?.Invoke(m.X, m.Y, "enemy", $"{Loc.F("base.level", m.Level)} {MonsterName(m.Type)}"))
            };
            GlideTo(m.X, m.Y);
            var x = m.X;
            var y = m.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 3f,
                $"{Loc.F("base.level", m.Level)}  {MonsterName(m.Type)}", info, items);
        }

        private void Attack(string monsterId, string[] heroIds) =>
            _commands.Dispatch(new MarchAttackCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, MonsterId = monsterId, HeroIds = heroIds });
    }
}
