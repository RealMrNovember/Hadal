using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using TMPro;
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
            public TMP_Text Name;
            public TMP_Text Info;
            public Image Bar;
            public string Look = string.Empty;
        }

        private readonly Dictionary<string, TeamView> _teams = new();
        private string _following = string.Empty;


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

                // While it gathers the team is only a badge on the node; the submarine shows again on the way home.
                var gathering = now >= m.ArrivesAtMs && now < m.GatheredAtMs;
                team.Group.gameObject.SetActive(!gathering);
                GatherBadge(m.Id, m.X, m.Y, 0, gathering && _popup == null, () => FollowTeam(m.Id));
                if (gathering)
                {
                    team.Ring.gameObject.SetActive(false);
                    team.Plate.gameObject.SetActive(false);
                    continue;
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

        /// <summary>The whole team rides one submarine (heroes and troops aboard), however many heroes it has.</summary>
        private static void FillFigures(Transform group, MarchDto m)
        {
            for (var i = group.childCount - 1; i >= 0; i--)
                Destroy(group.GetChild(i).gameObject);
            var sub = MakeSubmarine(group);
            sub.localPosition = Vector3.up * 1.2f;
            sub.localRotation = Quaternion.identity;
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
                new("queue_build", Loc.T("march.speedup"), () => SpeedUpMarch(marchId), now < m.ArrivesAtMs || returning),
                new("left_search", Loc.T("base.details"), () => OpenTeamDetails(marchId)),
                new("left_home", Loc.T("world.recall"), () => { Recall(marchId); StopFollowing(); }, !returning)
            };
            var info = $"{Loc.F("march.crew.short", m.Crew)}   {Loc.F("march.to", m.X, m.Y)}" + TargetShieldNote(m);
            TargetMenu.Show(() => IsOpen ? _camera : null, () =>
                {
                    var live = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
                    return (live == null ? TileToWorld(m.X, m.Y) : MarchPosition(live, View.ServerNowMs)) + Vector3.up * 2f;
                },
                TeamName(m), info, items, closed: () => { if (_following == marchId) _following = string.Empty; });
        }

        /// <summary>The speed-up window on a team: the leg it is on (out or home) is the timer.</summary>
        private void SpeedUpMarch(string marchId)
        {
            var m = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
            if (m == null)
                return;
            SpeedupWindow.Open("march:" + marchId, SpeedupKind.March, TeamName(m), () =>
            {
                var live = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
                if (live == null)
                    return null;
                var now = View.ServerNowMs;
                if (now < live.ArrivesAtMs)
                    return (live.StartedAtMs, live.ArrivesAtMs);
                return now >= live.GatheredAtMs ? (live.GatheredAtMs, live.HomeAtMs) : null;
            });
        }

        private void StopFollowing()
        {
            if (_following.Length == 0)
                return;
            _following = string.Empty;
            TargetMenu.Hide();
        }

        private Action _teamDetailsTick;

        /// <summary>
        /// The team in full, kept live: what it is doing and for how long (with a progress bar), where it is headed
        /// (node, level, coordinates, distance), what it has gathered so far against what it will carry and how fast,
        /// when gathering ends and when it is home, every hero with the troops they lead, and Go to · Speed up · Recall.
        /// </summary>
        private void OpenTeamDetails(string marchId)
        {
            var m = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
            if (m == null)
                return;
            TargetMenu.Hide();
            var catalog = ClientCatalog.Catalog;
            var sheet = MapDialog("team", TeamName(m), 128f);
            var content = sheet.Content;

            // Status: phase, countdown and the bar of the leg it is on.
            var status = UiFrames.SheetBlock(content, UiFrames.Card, "Status");
            status.Row(130);
            var phase = UiKit.Label("Phase", status.transform, string.Empty, UiKit.TypeScale.Body + 2, new Color(0.72f, 0.42f, 0.04f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            phase.rectTransform.Place(0.04f, 0.5f, 0.96f, 0.95f);
            var fill = ShareBar(UiKit.Rect("Leg", status.transform).Place(0.04f, 0.14f, 0.96f, 0.42f), 0f, null, UiKit.Ok);

            // Target.
            var gather = m.Kind != "attack";
            UiFrames.Section(content, Loc.T("march.details.target")).Row(56);
            var target = UiFrames.SheetBlock(content, UiFrames.Card, "Target");
            target.Row(130);
            var name = gather ? NodeName(m.NodeType) : MonsterName(m.NodeType);
            UiKit.Label("Node", target.transform, Loc.F("march.details.node", name, m.NodeLevel), UiKit.TypeScale.Body + 2, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.04f, 0.5f, 0.7f, 0.95f);
            var km = GatherRules.Km(m.FromX, m.FromY, m.X, m.Y);
            UiKit.Label("Where", target.transform, Loc.F("march.details.coords", m.X, m.Y, km.ToString("0.#")), UiKit.TypeScale.Caption + 2, UiFrames.InkGood, TextAnchor.MiddleLeft)
                .rectTransform.Place(0.04f, 0.06f, 0.7f, 0.5f);
            var go = UiKit.Button("Go", target.transform, Loc.T("march.details.go"), () =>
            {
                ClosePopup();
                GlideTo(m.X, m.Y);
            }, UiKit.Primary, UiKit.TypeScale.Body);
            ((RectTransform)go.transform).Place(0.72f, 0.16f, 0.97f, 0.84f);

            // Cargo: gathered so far / what it will carry, and the rate.
            UiFrames.Section(content, Loc.T(gather ? "march.details.gather" : "march.details.loot")).Row(56);
            var cargoBox = UiFrames.SheetBlock(content, UiFrames.Card, "Cargo");
            cargoBox.Row(60 + 46 * (2 + (m.Cargo?.Length ?? 0)));
            var cargo = UiKit.Label("Lines", cargoBox.transform, string.Empty, UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft);
            cargo.rectTransform.Place(0.04f, 0.04f, 0.96f, 0.96f);
            cargo.richText = true;

            // The team: heroes with the troops they lead, then troops by type.
            UiFrames.Section(content, Loc.T("march.team")).Row(56);
            var heroes = m.HeroIds != null && m.HeroIds.Length > 0 ? m.HeroIds : new[] { m.HeroId };
            var seats = UiKit.Rect("Seats", content).Row(220);
            for (var i = 0; i < 5; i++)
            {
                var cell = UiKit.Rect("Cell", seats).Place(i / 5f, 0f, (i + 1) / 5f, 1f, 5f);
                if (i >= heroes.Length || !catalog.TryGetHero(heroes[i], out var def))
                {
                    if (i >= heroes.Length)
                        Seat(cell, null, 0, null).Place(0, 0, 1, 1);
                    continue;
                }

                // Marches sent before teams carried only a crew count under their one hero.
                var count = m.HeroTroops != null && i < m.HeroTroops.Length ? m.HeroTroops[i] : heroes.Length == 1 ? m.Crew : 0;
                Seat(cell, def, count, null).Place(0, 0, 1, 1);
            }

            var troops = string.Join("   ", (m.Troops ?? Array.Empty<TroopStackDto>()).Select(t => $"T{t.Tier} {Loc.T("troop." + t.Type)} ×{t.Count:N0}"));
            UiFrames.SheetText(content, $"{Loc.F("march.crew", m.Crew.ToString("N0"))}   {troops}", UiKit.TypeScale.Caption + 2).Row(70);

            // Actions.
            var speed = UiFrames.FooterButton(sheet.Footer, Loc.T("march.speedup"), () => { ClosePopup(); SpeedUpMarch(marchId); }, UiKit.Gold, 0f, 0.5f);
            var recall = UiFrames.FooterButton(sheet.Footer, Loc.T("world.recall"), () => { ClosePopup(); Recall(marchId); }, UiKit.Danger, 0.5f, 1f);
            UiFrames.FitSheet(sheet);

            _teamDetailsTick = () =>
            {
                var live = View.Marches?.Marches?.FirstOrDefault(x => x.Id == marchId);
                if (live == null)
                {
                    ClosePopup();
                    return;
                }

                var now = View.ServerNowMs;
                var outbound = now < live.ArrivesAtMs;
                var working = !outbound && now < live.GatheredAtMs;
                var key = outbound ? "world.march.outbound" : working ? (gather ? "world.march.gathering" : "march.details.fighting") : "world.march.returning";
                var from = outbound ? live.StartedAtMs : working ? live.ArrivesAtMs : live.GatheredAtMs;
                var until = outbound ? live.ArrivesAtMs : working ? live.GatheredAtMs : live.HomeAtMs;
                phase.text = Loc.F(key, Loc.Duration(Math.Max(0, (until - now) / 1000f)));
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((now - from) / (float)Math.Max(1, until - from)), 1f);
                fill.enabled = fill.rectTransform.anchorMax.x > 0.005f;

                // What it has gathered by now: the cargo grows evenly while it works on the node.
                var span = Math.Max(1, live.GatheredAtMs - live.ArrivesAtMs);
                var share = outbound ? 0.0 : working ? (now - live.ArrivesAtMs) / (double)span : 1.0;
                var hours = span / 3_600_000.0;
                var lines = new List<string>();
                foreach (var c in (live.Cargo ?? Array.Empty<ResourceAmountEntry>()).Where(c => c.Value > 0))
                {
                    var got = (long)Math.Floor(c.Value * share);
                    var line = $"<b>{Loc.Resource(c.Key.ToString())}</b>  {got:N0} / {c.Value:N0}";
                    if (gather)
                        line += $"   <color=#1C6FB0>{Loc.F("march.details.rate", CostChips.Compact((long)(c.Value / Math.Max(0.01, hours))))}</color>";
                    lines.Add(line);
                }

                if (lines.Count == 0)
                    lines.Add("—");
                if (now < live.GatheredAtMs)
                    lines.Add(Loc.F(gather ? "march.details.finish" : "march.details.battle", Loc.Duration(Math.Max(0, (live.GatheredAtMs - now) / 1000f))));
                lines.Add(Loc.F("march.details.home", Loc.Duration(Math.Max(0, (live.HomeAtMs - now) / 1000f))));
                cargo.text = string.Join("\n", lines);

                speed.interactable = outbound || !working;
                recall.interactable = now < live.GatheredAtMs;
            };
            _teamDetailsTick();
        }

        /// <summary>Called every frame: keeps the open team sheet live.</summary>
        private void TickTeamDetails()
        {
            if (_popup == null || _sheetKind != "team")
                _teamDetailsTick = null;
            _teamDetailsTick?.Invoke();
        }
    }
}
