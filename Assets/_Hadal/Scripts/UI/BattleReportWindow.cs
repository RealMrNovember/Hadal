using System;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The battle report (genre mail report): Victory or Defeat over the two sides — our heroes and troops by stack
    /// with what each lost (wounded, PvE never kills), the swarm with what it lost — the rounds and the spoils that
    /// travel home with the team. Pops up when a team reaches its target, over any screen.
    /// </summary>
    public sealed class BattleReportWindow : MonoBehaviour
    {
        private IStateSyncService _stateSync;
        private Canvas _canvas;
        private GameObject _window;

        public void Initialize(IStateSyncService stateSync)
        {
            _stateSync = stateSync;
            _canvas = UiKit.Canvas("BattleReportUi", 49);
            _canvas.transform.SetParent(transform, false);
        }

        private void Update()
        {
            if (_stateSync == null || _window != null)
                return;
            if (_stateSync.View.TryTakeBattleReport(out var report))
                Show(report);
        }

        private void Close()
        {
            if (_window != null)
                Destroy(_window);
            _window = null;
        }

        private void Show(BattleReportDto r)
        {
            AudioDirector.BattleResolved(r.Won);
            var dim = UiKit.Box("Report", _canvas.transform, new Color(0, 0, 0, 0.65f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _window = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.14f, 0.96f, 0.86f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();

            // Banner: gold for a win, red for a loss.
            var banner = UiKit.Box("Banner", card.transform, r.Won ? new Color(0.85f, 0.62f, 0.18f, 1f) : new Color(0.62f, 0.18f, 0.18f, 1f));
            banner.rectTransform.Place(0, 0.87f, 1, 1);
            var title = UiKit.Label("Title", banner.transform, Loc.T(r.Won ? "battle.won" : "battle.lost"), 52, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(0, 0.3f, 1, 1);
            UiText.Outline(title, new Color(0, 0, 0, 0.6f));
            UiKit.Label("Where", banner.transform, $"{Loc.F("base.level", r.MonsterLevel)} {WorldMapView.MonsterName(r.MonsterType)}  ·  X:{r.X} Y:{r.Y}  ·  {Loc.F("battle.rounds", r.Rounds)}",
                24, Color.white, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.02f, 1, 0.34f);
            var close = UiKit.Button("Close", banner.transform, "X", Close, UiKit.Field, 36);
            ((RectTransform)close.transform).Place(0.87f, 0.55f, 0.98f, 0.95f);

            // Our side and theirs, face to face.
            Side(card.transform, 0.02f, 0.49f, Loc.T("battle.us"), r.AttackerPower, r.Attacker, r.HeroIds, true);
            Side(card.transform, 0.51f, 0.98f, WorldMapView.MonsterName(r.MonsterType), r.DefenderPower, r.Defender, null, false, WorldMapView.MonsterName(r.MonsterType));

            // Spoils.
            UiKit.Label("SpoilsTitle", card.transform, Loc.T("battle.spoils"), 28, UiKit.Amber, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.21f, 0.95f, 0.26f);
            var spoils = r.Rewards ?? Array.Empty<ItemCountDto>();
            if (spoils.Length == 0)
                UiKit.Label("None", card.transform, Loc.T("battle.nospoils"), 24, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(0.05f, 0.13f, 0.95f, 0.2f);
            for (var i = 0; i < Math.Min(4, spoils.Length); i++)
            {
                var x0 = 0.05f + i * 0.23f;
                var tile = UiKit.Box("Spoil", card.transform, new Color(0.04f, 0.11f, 0.15f, 1f));
                tile.rectTransform.Place(x0, 0.09f, x0 + 0.21f, 0.2f);
                Hud.HudIcons.Image(tile.transform, QuestText.ItemIcon(spoils[i].Id, null), 0.05f, 0.15f, 0.35f, 0.85f).raycastTarget = false;
                UiKit.Label("Name", tile.transform, $"{QuestText.Item(spoils[i].Id)} ×{spoils[i].Count}", 18, UiKit.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.38f, 0, 0.98f, 1);
            }

            UiKit.Label("Home", card.transform, Loc.T("battle.home"), 20, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.01f, 0.96f, 0.08f);
        }

        /// <param name="unitName">The swarm's own name for its stacks (they fight as a troop type but are not troops).</param>
        private static void Side(Transform card, float x0, float x1, string title, long power, BattleStackDto[] stacks, string[] heroes, bool ours,
            string unitName = null)
        {
            var panel = UiKit.Box("Side", card, ours ? new Color(0.05f, 0.18f, 0.26f, 1f) : new Color(0.24f, 0.07f, 0.08f, 1f));
            panel.rectTransform.Place(x0, 0.28f, x1, 0.85f);
            UiKit.Label("Title", panel.transform, title, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.04f, 0.88f, 0.96f, 0.98f);
            UiKit.Label("Power", panel.transform, Loc.F("hero.power", power.ToString("N0")), 22, UiKit.Amber, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.8f, 0.96f, 0.88f);

            var top = 0.78f;
            if (heroes != null && heroes.Length > 0)
            {
                var catalog = ClientCatalog.Catalog;
                for (var i = 0; i < Math.Min(5, heroes.Length); i++)
                {
                    if (!catalog.TryGetHero(heroes[i], out var hero))
                        continue;
                    var face = UiKit.Rect("Hero", panel.transform).Place(0.04f + i * 0.19f, 0.6f, 0.21f + i * 0.19f, 0.78f);
                    SocialView.Portrait(face, hero, true, 0, 0, 1, 1);
                }

                top = 0.58f;
            }

            var lines = string.Join("\n", (stacks ?? Array.Empty<BattleStackDto>()).Select(s =>
                $"{unitName ?? $"T{s.Tier} {Loc.T("troop." + s.Type)}"}\n<size=20>{s.Start:N0}  <color={(ours ? "#FFB347" : "#FF8A80")}>-{s.Lost:N0}</color>{(ours ? " " + Loc.T("battle.wounded") : string.Empty)}</size>"));
            UiKit.Label("Stacks", panel.transform, lines, 24, UiKit.Text, TextAnchor.UpperCenter).rectTransform.Place(0.04f, 0.04f, 0.96f, top);
        }
    }
}
