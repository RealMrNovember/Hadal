using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// PvP on the map (docs/30): attack, rally, scout and reinforce from a base's ring, attack an enemy team gathering
    /// on a node, strongholds with their menu (attack, rally, plant or move the hub), and the strategic zoom that shows
    /// the whole map with alliance territories, strongholds and every base as a dot. The server decides every battle.
    /// </summary>
    public sealed partial class WorldMapView
    {
        /// <summary>A PvP team setup: what the march setup shows and what "Go" sends.</summary>
        private sealed class PvpPlan
        {
            public string Title;
            public int X;
            public int Y;
            public string Info;
            public string GoLabel;
            public Color GoColor;
            public Action<string[]> Send;
        }

        private PvpPlan _pvpPlan;

        private static readonly Color AttackRed = new(0.86f, 0.3f, 0.27f, 1f);
        private static readonly Color AllyBlue = new(0.25f, 0.55f, 0.95f, 1f);

        private void SendPvp(string mode, string kind, string targetId, string[] heroIds, int muster = 0) =>
            _commands.Dispatch(new MarchPvpCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Mode = mode, TargetKind = kind, TargetId = targetId,
                HeroIds = heroIds ?? Array.Empty<string>(), MusterSeconds = muster
            });

        private bool IBeginner => PvpRules.IsBeginner(ReactorLevel());

        /// <summary>The PvP buttons of a base's ring: attack, rally and scout on others; reinforce on alliance mates.</summary>
        private void AddPvpItems(List<TargetMenu.Item> items, WorldBaseDto b, bool ally)
        {
            var target = b;
            var label = (string.IsNullOrEmpty(b.AllianceTag) ? string.Empty : $"[{b.AllianceTag}] ") + b.Name;
            if (ally)
            {
                items.Insert(0, new TargetMenu.Item("nav_heroes", Loc.T("pvp.reinforce"), () => OpenPvpSetup(new PvpPlan
                {
                    Title = label, X = target.X, Y = target.Y, Info = Loc.T("pvp.reinforce.info"), GoLabel = Loc.T("pvp.reinforce"), GoColor = AllyBlue,
                    Send = heroes => SendPvp(PvpRules.Reinforce, PvpRules.TargetBase, target.PlayerId, heroes)
                })));
                return;
            }

            var open = !IBeginner && !PvpRules.IsBeginner(b.Level);
            items.Insert(0, new TargetMenu.Item("left_search", Loc.T("pvp.scout"), () =>
            {
                SendPvp(PvpRules.Scout, PvpRules.TargetBase, target.PlayerId, null);
                TargetMenu.Hide();
            }, open));
            items.Insert(0, new TargetMenu.Item("side_alliance", Loc.T("pvp.rally"),
                () => OpenMuster(label, target.X, target.Y, PvpRules.TargetBase, target.PlayerId), open && View.MyAlliance != null));
            items.Insert(0, new TargetMenu.Item("event_war", Loc.T("world.attack"), () => OpenPvpSetup(new PvpPlan
            {
                Title = label, X = target.X, Y = target.Y, Info = Loc.F("pvp.enemyPower", ScoutedPower(target.PlayerId)), GoLabel = Loc.T("world.attack"),
                GoColor = AttackRed, Send = heroes => SendPvp(PvpRules.Attack, PvpRules.TargetBase, target.PlayerId, heroes)
            }), open));
        }

        /// <summary>The defender's power from a scout report of the last 30 minutes, else "?" (Last Asylum's "Bilinmiyor").</summary>
        private string ScoutedPower(string playerId)
        {
            var fresh = View.ServerNowMs - 30 * 60 * 1000L;
            var scout = (View.WarLog.Reports ?? Array.Empty<PvpReportDto>())
                .FirstOrDefault(r => r.Kind == "scout" && r.OpponentId == playerId && r.AtMs >= fresh);
            return scout == null ? Loc.T("pvp.unknown") : scout.DefenderPower.ToString("N0");
        }

        private void OpenPvpSetup(PvpPlan plan)
        {
            _pvpPlan = plan;
            OpenMarchSetup(null, null, null);
        }

        /// <summary>An enemy team gathering on a node: attack it (shields do not cover teams outside).</summary>
        private void ShowEnemyTeam(WorldMarchDto w)
        {
            var label = string.IsNullOrEmpty(w.AllianceTag) ? w.OwnerName : $"[{w.AllianceTag}] {w.OwnerName}";
            var items = new List<TargetMenu.Item>
            {
                new("event_war", Loc.T("world.attack"), () => OpenPvpSetup(new PvpPlan
                {
                    Title = label, X = w.ToX, Y = w.ToY, Info = Loc.T("pvp.team.info"), GoLabel = Loc.T("world.attack"), GoColor = AttackRed,
                    Send = heroes => SendPvp(PvpRules.Attack, PvpRules.TargetTeam, w.Id, heroes)
                }), !IBeginner),
                new("avatar", Loc.T("profile.title"), () => ProfileCard.Instance?.Show(w.OwnerId, w.OwnerName, string.Empty))
            };
            GlideTo(w.ToX, w.ToY);
            var x = w.ToX;
            var y = w.ToY;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 3f, label,
                $"{Loc.T("pvp.team.gathering")}\n{Loc.F("world.coords", x, y)}", items);
        }

        /// <summary>Rally muster: 1, 3, 5 or 10 minutes, then the team setup.</summary>
        private void OpenMuster(string title, int x, int y, string kind, string targetId)
        {
            TargetMenu.Hide();
            ClosePopup();
            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.08f, 0.36f, 0.92f, 0.64f);
            card.gameObject.AddComponent<Button>();
            UiKit.Label("Title", card.transform, Loc.T("pvp.rally"), 40, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.05f, 0.78f, 0.95f, 0.96f);
            UiKit.Label("Target", card.transform, title, 28, UiKit.Accent, TextAnchor.MiddleCenter).rectTransform.Place(0.05f, 0.64f, 0.95f, 0.78f);
            UiKit.Label("Hint", card.transform, Loc.T("pvp.rally.muster"), 24, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.05f, 0.5f, 0.95f, 0.64f);
            var options = PvpRules.MusterOptions;
            for (var i = 0; i < options.Length; i++)
            {
                var seconds = options[i];
                var x0 = 0.05f + i * 0.23f;
                var button = UiKit.Button("Muster" + seconds, card.transform, Loc.F("pvp.minutes", seconds / 60), () => OpenPvpSetup(new PvpPlan
                {
                    Title = title, X = x, Y = y, Info = Loc.F("pvp.rally.info", seconds / 60), GoLabel = Loc.T("pvp.rally.start"), GoColor = AttackRed,
                    Send = heroes => SendPvp(PvpRules.Rally, kind, targetId, heroes, seconds)
                }), UiKit.Primary, 30);
                ((RectTransform)button.transform).Place(x0, 0.12f, x0 + 0.21f, 0.4f);
            }
        }

        /// <summary>Joins an alliance rally from the war list: the team setup towards the rally's target.</summary>
        public void JoinRally(WarRowDto row)
        {
            Open(() => OpenPvpSetup(new PvpPlan
            {
                Title = (string.IsNullOrEmpty(row.AttackerTag) ? string.Empty : $"[{row.AttackerTag}] ") + row.AttackerName,
                X = row.FromX, Y = row.FromY, Info = Loc.T("pvp.rally.join.info"), GoLabel = Loc.T("pvp.rally.join"), GoColor = AttackRed,
                Send = heroes => SendPvp(PvpRules.Join, string.Empty, row.Id, heroes)
            }));
        }

        /// <summary>Sends a team to guard an alliance mate's base (from the war list).</summary>
        public void Reinforce(string playerId, string name, int x, int y)
        {
            Open(() => OpenPvpSetup(new PvpPlan
            {
                Title = name, X = x, Y = y, Info = Loc.T("pvp.reinforce.info"), GoLabel = Loc.T("pvp.reinforce"), GoColor = AllyBlue,
                Send = heroes => SendPvp(PvpRules.Reinforce, PvpRules.TargetBase, playerId, heroes)
            }));
        }

        // --- Strongholds -------------------------------------------------------------------------

        private readonly Dictionary<string, RectTransform> _strongholdPlates = new();
        private float _nextTerritoryRequest;
        private int _strongholdVersion = -1;

        private void RequestTerritory()
        {
            _nextTerritoryRequest = Time.unscaledTime + (Strategic ? 15f : 45f);
            _commands.Dispatch(new ViewTerritoryCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence });
        }

        private void UpdateStrongholds()
        {
            if (Time.unscaledTime >= _nextTerritoryRequest)
                RequestTerritory();
            var territory = View.Territory;
            if (territory == null)
                return;
            if (_strongholdVersion != View.TerritoryVersion)
            {
                _strongholdVersion = View.TerritoryVersion;
                RebuildTerritoryTexture(territory);
            }

            var visible = _popup == null;
            foreach (var s in territory.Strongholds ?? Array.Empty<StrongholdDto>())
            {
                if (!_strongholdPlates.TryGetValue(s.Id, out var plate))
                {
                    plate = BuildStrongholdPlate(s.Id);
                    _strongholdPlates[s.Id] = plate;
                }

                var top = Project(TileToWorld(s.X, s.Y) + Vector3.up * 2f, out var onScreen);
                var near = Vector3.Distance(TileToWorld(s.X, s.Y), _focus) < 900f;
                plate.gameObject.SetActive(visible && onScreen && near && (Strategic ? s.Level >= 1 : true));
                plate.anchoredPosition = top;
                var owner = string.IsNullOrEmpty(s.AllianceTag) ? string.Empty : $"[{s.AllianceTag}] ";
                var name = s.Level >= TerritoryRules.ThroneLevel ? Loc.T("pvp.throne") : Loc.T("pvp.stronghold");
                var text = plate.Find("Name").GetComponent<Text>();
                text.text = $"{owner}{(s.Hub ? "⚑ " : string.Empty)}{name} {Loc.F("base.level", s.Level)}{(s.UnderAttack ? " ⚔" : string.Empty)}";
                text.fontSize = Strategic ? 22 : 26;
                plate.GetComponent<Image>().color = s.Color >= 0 ? AllianceColor(s.Color, 0.9f) : new Color(0.08f, 0.1f, 0.14f, 0.85f);
                var info = s;
                var button = plate.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowStronghold(info));
            }
        }

        private RectTransform BuildStrongholdPlate(string id)
        {
            var box = UiKit.Box("Stronghold " + id, _labels, new Color(0.08f, 0.1f, 0.14f, 0.85f));
            box.rectTransform.sizeDelta = new Vector2(330f, 46f);
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            box.gameObject.AddComponent<Button>();
            UiKit.Label("Name", box.transform, string.Empty, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.02f, 0, 0.98f, 1);
            box.rectTransform.SetAsFirstSibling();
            return box.rectTransform;
        }

        private static Color AllianceColor(int index, float alpha)
        {
            var rgb = TerritoryRules.Colors[Mathf.Clamp(index, 0, TerritoryRules.Colors.Length - 1)];
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        /// <summary>A stronghold: level, owner, garrison; attack or rally it, plant the hub, move the hub, abandon the region.</summary>
        private void ShowStronghold(StrongholdDto s)
        {
            var territory = View.Territory;
            var mine = territory != null && !string.IsNullOrEmpty(territory.MyAllianceId) && s.AllianceId == territory.MyAllianceId;
            var rank = View.MyRank();
            var hasTerritory = territory != null && territory.Held > 0;
            var throne = s.Level >= TerritoryRules.ThroneLevel;
            var owner = string.IsNullOrEmpty(s.AllianceTag) ? Loc.T("pvp.unowned") : $"[{s.AllianceTag}]{(s.Hub ? " ⚑" : string.Empty)}";
            var info = $"{owner}   {Loc.F("pvp.garrison", s.Power.ToString("N0"))}\n{Loc.F("world.coords", s.X, s.Y)}";
            if (territory != null && territory.Allowed > 0)
                info += $"\n{Loc.F("pvp.regions", territory.Held, territory.Allowed)}";
            var title = $"{(throne ? Loc.T("pvp.throne") : Loc.T("pvp.stronghold"))} {Loc.F("base.level", s.Level)}";
            var items = new List<TargetMenu.Item>();
            var target = s;
            if (!mine && !throne)
            {
                items.Add(new TargetMenu.Item("event_war", Loc.T("world.attack"), () => OpenPvpSetup(new PvpPlan
                {
                    Title = title, X = target.X, Y = target.Y, Info = Loc.F("pvp.garrison", target.Power.ToString("N0")), GoLabel = Loc.T("world.attack"),
                    GoColor = AttackRed, Send = heroes => SendPvp(PvpRules.Attack, PvpRules.TargetStronghold, target.Id, heroes)
                }), hasTerritory && !s.Hub));
                items.Add(new TargetMenu.Item("side_alliance", Loc.T("pvp.rally"), () => OpenMuster(title, target.X, target.Y, PvpRules.TargetStronghold, target.Id),
                    hasTerritory && !s.Hub));
            }

            if (!hasTerritory && string.IsNullOrEmpty(s.AllianceId) && s.Level <= TerritoryRules.MaxPlantLevel && View.MyAlliance != null)
                items.Add(new TargetMenu.Item("nav_world", Loc.T("pvp.plant"), () => Territory(TerritoryRules.Plant, target.Id),
                    rank >= AlliancePermissions.Officer));
            if (mine && !s.Hub)
            {
                items.Add(new TargetMenu.Item("nav_world", Loc.T("pvp.moveHub"), () => Territory(TerritoryRules.Move, target.Id), rank >= AlliancePermissions.Leader));
                items.Add(new TargetMenu.Item("left_home", Loc.T("pvp.abandon"), () => Territory(TerritoryRules.Abandon, target.Id), rank >= AlliancePermissions.Leader));
            }

            items.Add(new TargetMenu.Item("chat", Loc.T("share.title"), () => ShareLocation?.Invoke(target.X, target.Y, mine ? "base" : "enemy", title)));
            GlideTo(s.X, s.Y);
            var x = s.X;
            var y = s.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 2f, title, info, items);
        }

        private void Territory(string action, string strongholdId)
        {
            TargetMenu.Hide();
            _commands.Dispatch(new TerritoryCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Action = action, StrongholdId = strongholdId });
        }

        // --- Strategic zoom ----------------------------------------------------------------------

        /// <summary>Zoomed out this far, the map shows territories instead of details.</summary>
        private const float StrategicFrom = 230f;

        /// <summary>As far out as the camera goes: the whole 200 × 200 map on screen.</summary>
        private const float MaxDistance = 1500f;

        private const float MinDistance = 40f;

        private bool Strategic => _distance >= StrategicFrom;

        private Transform _territoryQuad;
        private Material _territoryMaterial;
        private Texture2D _territoryTexture;
        private bool _wasStrategic;

        /// <summary>Paints regions (owner colour, brighter borders), the hub regions, and every base as a dot.</summary>
        private void RebuildTerritoryTexture(TerritoryDto territory)
        {
            var size = Math.Max(10, territory.MapSize);
            const int scale = 2;
            if (_territoryTexture == null || _territoryTexture.width != size * scale)
            {
                _territoryTexture = new Texture2D(size * scale, size * scale, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
                };
            }

            var map = TerritoryRules.Map(size);
            var owner = new Color32[TerritoryRules.Cells * TerritoryRules.Cells];
            var border = new Color32[owner.Length];
            for (var i = 0; i < owner.Length; i++)
            {
                owner[i] = new Color32(6, 18, 28, 150);
                border[i] = new Color32(70, 100, 120, 200);
            }

            foreach (var s in territory.Strongholds ?? Array.Empty<StrongholdDto>())
            {
                if (s.Region < 0 || s.Region >= owner.Length || s.Color < 0)
                    continue;
                var c = AllianceColor(s.Color, 1f);
                owner[s.Region] = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, s.Hub ? 0.8f : 0.65f);
                border[s.Region] = new Color(Mathf.Min(1, c.r * 1.3f), Mathf.Min(1, c.g * 1.3f), Mathf.Min(1, c.b * 1.3f), 1f);
            }

            var pixels = new Color32[size * scale * size * scale];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var r = map[y * size + x];
                    var edge = (x > 0 && map[y * size + x - 1] != r) || (x < size - 1 && map[y * size + x + 1] != r)
                               || (y > 0 && map[(y - 1) * size + x] != r) || (y < size - 1 && map[(y + 1) * size + x] != r);
                    for (var dy = 0; dy < scale; dy++)
                        for (var dx = 0; dx < scale; dx++)
                            pixels[(y * scale + dy) * size * scale + x * scale + dx] = edge ? border[r] : owner[r];
                }
            }

            // Bases as 2 × 2 dots: cyan mine, blue alliance, pale red others.
            foreach (var packed in territory.Bases ?? Array.Empty<int>())
            {
                var bx = packed / 1_000_000;
                var by = packed / 1_000 % 1_000;
                var relation = packed % 1_000;
                if (bx < 0 || by < 0 || bx >= size || by >= size)
                    continue;
                var dot = relation == 0 ? new Color32(80, 240, 255, 255) : relation == 1 ? new Color32(90, 150, 255, 255) : new Color32(255, 120, 110, 255);
                for (var dy = 0; dy < scale; dy++)
                    for (var dx = 0; dx < scale; dx++)
                        pixels[(by * scale + dy) * size * scale + bx * scale + dx] = dot;
            }

            _territoryTexture.SetPixels32(pixels);
            _territoryTexture.Apply(false);

            if (_territoryQuad == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(quad.GetComponent<Collider>());
                quad.name = "Territory";
                quad.transform.SetParent(_root, false);
                _territoryMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = _territoryTexture, renderQueue = 3100 };
                quad.GetComponent<MeshRenderer>().sharedMaterial = _territoryMaterial;
                _territoryQuad = quad.transform;
            }

            _territoryMaterial.mainTexture = _territoryTexture;
            // Tile (x, y) sits at TileToWorld(x, y); the quad spans the whole map, flat on the seabed.
            var span = size * Tile;
            _territoryQuad.position = Origin + new Vector3(span / 2f - Tile / 2f, 0.4f, span / 2f - Tile / 2f);
            _territoryQuad.rotation = Quaternion.Euler(90f, 0f, 0f);
            _territoryQuad.localScale = new Vector3(span, span, 1f);
        }

        /// <summary>Each frame: territory fades in with the zoom, the camera sees far, and the fog thins out.</summary>
        private void UpdateTerritory()
        {
            UpdateStrongholds();
            if (Strategic != _wasStrategic)
            {
                _wasStrategic = Strategic;
                if (Strategic)
                    RequestTerritory();
            }

            if (_territoryMaterial != null)
            {
                var t = Mathf.InverseLerp(StrategicFrom * 0.6f, StrategicFrom * 1.4f, _distance);
                _territoryMaterial.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.18f, 1f, t));
            }

            _camera.farClipPlane = Mathf.Max(400f, _distance * 2.5f);
            RenderSettings.fogDensity = Strategic ? 0.0002f : 0.0035f;
        }

        private void ClearPvpPlan() => _pvpPlan = null;
    }
}
