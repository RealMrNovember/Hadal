using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Teams on the move (Last Asylum, docs/rehber 19 §4.4): each march is a group of its heroes with troops around them
    /// on a green ring, a plate with the leader, where it goes and a progress bar. Tapping the plate or the queue row
    /// follows the team with the camera and opens Speed up · Details · Recall around it; dragging lets go.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private sealed class TeamView
        {
            public Transform Group;
            public RectTransform Ring;
            public RectTransform Plate;
            public Text Name;
            public Text Info;
            public Image Bar;
            public string Look = string.Empty;
        }

        private readonly Dictionary<string, TeamView> _teams = new();
        private string _following = string.Empty;

        private const float HeroScale = 5f;
        private const float TroopScale = 3.5f;

        /// <summary>Draws every march as a moving team; called each frame with the march list.</summary>
        private void UpdateTeams(MarchDto[] marches, long now)
        {
            var live = new HashSet<string>();
            foreach (var m in marches)
            {
                live.Add(m.Id);
                if (!_teams.TryGetValue(m.Id, out var team))
                {
                    team = BuildTeam(m);
                    _teams[m.Id] = team;
                }

                // Rebuild the figures when the team changes (a recall does not, but the first data may arrive late).
                var look = string.Join(",", m.HeroIds ?? Array.Empty<string>()) + "|" + m.Crew;
                if (team.Look != look)
                {
                    team.Look = look;
                    FillFigures(team.Group, m);
                }

                var at = MarchPosition(m, now);
                var heading = now < m.ArrivesAtMs ? TileToWorld(m.X, m.Y) - TileToWorld(m.FromX, m.FromY)
                    : now >= m.GatheredAtMs ? TileToWorld(m.FromX, m.FromY) - TileToWorld(m.X, m.Y) : Vector3.zero;
                team.Group.position = at;
                if (heading.sqrMagnitude > 0.01f)
                    team.Group.rotation = Quaternion.Slerp(team.Group.rotation, Quaternion.LookRotation(heading.normalized, Vector3.up), 0.2f);

                var visible = _popup == null;
                var ground = Project(at, out var groundOk);
                team.Ring.gameObject.SetActive(visible && groundOk);
                team.Ring.anchoredPosition = ground;
                var top = Project(at + Vector3.up * 9f, out var topOk);
                team.Plate.gameObject.SetActive(visible && topOk && _following != m.Id);
                team.Plate.anchoredPosition = top;

                var phase = now < m.ArrivesAtMs ? "world.march.outbound" : now < m.GatheredAtMs ? "world.march.gathering" : "world.march.returning";
                var from = now < m.ArrivesAtMs ? m.StartedAtMs : now < m.GatheredAtMs ? m.ArrivesAtMs : m.GatheredAtMs;
                var until = now < m.ArrivesAtMs ? m.ArrivesAtMs : now < m.GatheredAtMs ? m.GatheredAtMs : m.HomeAtMs;
                team.Name.text = TeamName(m);
                team.Info.text = Loc.F(phase, Loc.Duration(Math.Max(0, (until - now) / 1000f)));
                team.Bar.rectTransform.Place(0, 0, Mathf.Clamp01((now - from) / (float)Math.Max(1, until - from)), 1);
            }

            foreach (var id in _teams.Keys.Where(k => !live.Contains(k)).ToList())
            {
                Destroy(_teams[id].Group.gameObject);
                Destroy(_teams[id].Ring.gameObject);
                Destroy(_teams[id].Plate.gameObject);
                _teams.Remove(id);
                if (_following == id)
                    StopFollowing();
            }

            // The camera rides with the followed team until the player drags.
            if (_following.Length > 0)
            {
                var m = marches.FirstOrDefault(x => x.Id == _following);
                if (m == null || _dragging)
                    StopFollowing();
                else
                    _focus = Vector3.Lerp(_focus, MarchPosition(m, now), 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            }
        }

        private TeamView BuildTeam(MarchDto m)
        {
            var group = new GameObject("Team " + m.Id).transform;
            group.SetParent(_root, false);

            // The ground ring and the plate are UI so they stay crisp at any zoom.
            var ring = UiKit.Rect("Ring", _labels);
            ring.sizeDelta = new Vector2(220f, 130f);
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = UiKit.CircleRing;
            ringImage.color = new Color(0.36f, 0.86f, 0.46f, 0.85f);
            ringImage.raycastTarget = false;
            ring.SetAsFirstSibling();

            var plate = UiKit.Box("TeamPlate", _labels, new Color(0.02f, 0.06f, 0.09f, 0.88f));
            plate.rectTransform.sizeDelta = new Vector2(330f, 92f);
            plate.gameObject.AddComponent<Outline>().effectColor = new Color(0.36f, 0.86f, 0.46f, 0.6f);
            var face = UiKit.Rect("Face", plate.transform).Place(0.02f, 0.08f, 0.24f, 0.92f);
            if (ClientCatalog.Catalog.TryGetHero(m.HeroId, out var leader))
                SocialView.Portrait(face, leader, true, 0, 0, 1, 1);
            var name = UiKit.Label("Name", plate.transform, string.Empty, 24, new Color(0.45f, 0.95f, 0.55f, 1f), TextAnchor.UpperLeft, FontStyle.Bold);
            name.rectTransform.Place(0.27f, 0.5f, 0.98f, 0.95f);
            var info = UiKit.Label("Info", plate.transform, string.Empty, 20, UiKit.Text, TextAnchor.MiddleLeft);
            info.rectTransform.Place(0.27f, 0.2f, 0.98f, 0.55f);
            var track = UiKit.Box("Track", plate.transform, new Color(0, 0, 0, 0.6f));
            track.rectTransform.Place(0.27f, 0.07f, 0.97f, 0.2f);
            var bar = UiKit.Box("Bar", track.transform, UiKit.Ok);
            var id = m.Id;
            plate.gameObject.AddComponent<Button>().onClick.AddListener(() => FollowTeam(id));
            plate.gameObject.AddComponent<PressFeedback>();

            return new TeamView { Group = group, Ring = ring, Plate = plate.rectTransform, Name = name, Info = info, Bar = bar };
        }

        /// <summary>Up to three heroes in front and a few troops behind, sized to read at map distance.</summary>
        private static void FillFigures(Transform group, MarchDto m)
        {
            for (var i = group.childCount - 1; i >= 0; i--)
                Destroy(group.GetChild(i).gameObject);
            var library = CharacterLibrary.Instance;
            if (library == null)
                return;
            var catalog = ClientCatalog.Catalog;

            var heroes = (m.HeroIds != null && m.HeroIds.Length > 0 ? m.HeroIds : new[] { m.HeroId }).Take(3).ToList();
            for (var i = 0; i < heroes.Count; i++)
            {
                var model = catalog.TryGetHero(heroes[i], out var hero) ? library.Model(hero.Model) : null;
                Place(model ?? library.Model("crew_engineer"), new Vector3((i - (heroes.Count - 1) / 2f) * 3.6f, 0f, 2.4f), HeroScale);
            }

            // Troops: one figure per stack type, a few rows deep; armored divers wear the heavy suit.
            var troops = (m.Troops ?? Array.Empty<TroopStackDto>()).Where(t => t.Count > 0).ToList();
            var figures = Mathf.Clamp(troops.Count == 0 ? 0 : (int)Math.Ceiling(Math.Log10(Math.Max(10, m.Crew)) * 2), 0, 8);
            for (var i = 0; i < figures; i++)
            {
                var type = troops[i % troops.Count].Type;
                var model = library.Model(type == "armored" ? "mech_guard" : type == "driller" ? "mech_worker" : "crew_scientist");
                var row = i / 4;
                var col = i % 4;
                Place(model, new Vector3((col - 1.5f) * 2.8f, 0f, -2f - row * 2.8f), TroopScale);
            }

            void Place(GameObject prefab, Vector3 offset, float scale)
            {
                if (prefab == null)
                    return;
                var figure = Instantiate(prefab, group);
                figure.transform.localPosition = offset;
                figure.transform.localRotation = Quaternion.identity;
                figure.transform.localScale = Vector3.one * scale;
            }
        }

        /// <summary>The camera follows the team; its round menu: Speed up · Details · Recall.</summary>
        private void FollowTeam(string marchId)
        {
            var m = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
            if (m == null)
                return;
            _following = marchId;
            _glide = null;

            var now = View.ServerNowMs;
            var returning = now >= m.GatheredAtMs;
            var items = new List<TargetMenu.Item>
            {
                new("queue_build", Loc.T("march.speedup"), () => _toast?.Invoke(Loc.T("march.speedup.soon"), UiKit.Amber)),
                new("left_search", Loc.T("base.details"), () => OpenTeamDetails(marchId)),
                new("left_home", Loc.T("world.recall"), () => { Recall(marchId); StopFollowing(); }, !returning)
            };
            var info = $"{Loc.F("march.crew.short", m.Crew)}   {Loc.F("march.to", m.X, m.Y)}";
            TargetMenu.Show(() => IsOpen ? _camera : null, () =>
                {
                    var live = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
                    return (live == null ? TileToWorld(m.X, m.Y) : MarchPosition(live, View.ServerNowMs)) + Vector3.up * 2f;
                },
                TeamName(m), info, items, closed: () => { if (_following == marchId) _following = string.Empty; });
        }

        private void StopFollowing()
        {
            if (_following.Length == 0)
                return;
            _following = string.Empty;
            TargetMenu.Hide();
        }

        /// <summary>The team in full: every hero with the troops they lead, the troops by type and tier, and the cargo.</summary>
        private void OpenTeamDetails(string marchId)
        {
            var m = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
            if (m == null)
                return;
            ClosePopup();
            var catalog = ClientCatalog.Catalog;

            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.25f, 0.96f, 0.75f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();

            UiKit.Label("Title", card.transform, TeamName(m), 38, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.87f, 0.8f, 0.97f);
            var close = UiKit.Button("Close", card.transform, "X", ClosePopup, UiKit.Field, 36);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.85f, 0.88f, 0.96f, 0.97f);

            var heroes = m.HeroIds != null && m.HeroIds.Length > 0 ? m.HeroIds : new[] { m.HeroId };
            for (var i = 0; i < Math.Min(5, heroes.Length); i++)
            {
                var x0 = 0.04f + i * 0.19f;
                var seat = UiKit.Box("Seat", card.transform, new Color(0, 0, 0, 0.35f));
                seat.rectTransform.Place(x0, 0.55f, x0 + 0.18f, 0.85f);
                if (catalog.TryGetHero(heroes[i], out var def))
                    SocialView.Portrait(seat.transform, def, true, 0.04f, 0.22f, 0.96f, 0.97f);
                // Marches sent before teams carried only a crew count under their one hero.
                var count = m.HeroTroops != null && i < m.HeroTroops.Length ? m.HeroTroops[i] : heroes.Length == 1 ? m.Crew : 0;
                var chip = UiKit.Box("Troops", seat.transform, new Color(0.17f, 0.6f, 0.3f, 1f));
                chip.rectTransform.Place(0.04f, 0.02f, 0.96f, 0.2f);
                UiKit.Label("Count", chip.transform, CostChips.Compact(count), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
            }

            var troops = string.Join("   ", (m.Troops ?? Array.Empty<TroopStackDto>()).Select(t => $"T{t.Tier} {Loc.T("troop." + t.Type)} ×{t.Count:N0}"));
            var cargo = string.Join("   ", (m.Cargo ?? Array.Empty<ResourceAmountEntry>()).Where(c => c.Value > 0)
                .Select(c => $"{CostChips.Compact(c.Value)} {Loc.Resource(c.Key.ToString())}"));
            UiKit.Label("Text", card.transform, $"{troops}\n\n{Loc.F("march.cargo", cargo.Length > 0 ? cargo : "—")}\n{Loc.F("march.to", m.X, m.Y)}", 26, UiKit.Text,
                TextAnchor.UpperLeft).rectTransform.Place(0.05f, 0.05f, 0.95f, 0.52f);
        }
    }
}
