using System;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The battle result against a swarm, as Last Asylum's result dialog over the blurred game: our team (blue) and the
    /// swarm (red) face to face with Victory or Defeat over each, where and in how many rounds, then our heroes and
    /// troops stack by stack with what each lost (wounded, PvE never kills), the swarm's losses, and the spoils as
    /// item tiles that travel home with the team. Pops up when a team reaches its target, over any screen.
    /// </summary>
    public sealed class BattleReportWindow : MonoBehaviour
    {
        private IStateSyncService _stateSync;
        private Canvas _canvas;
        private UiFrames.DialogSheet _window;

        public void Initialize(IStateSyncService stateSync)
        {
            _stateSync = stateSync;
            _canvas = UiKit.Canvas("BattleReportUi", 49);
            _canvas.transform.SetParent(transform, false);
        }

        private void Update()
        {
            if (_stateSync == null || _window is { IsOpen: true })
                return;
            if (_stateSync.View.TryTakeBattleReport(out var report))
                Show(report);
        }

        private void Close()
        {
            _window?.Close();
            _window = null;
        }

        private void Show(BattleReportDto r)
        {
            AudioDirector.BattleResolved(r.Won);
            var monster = WorldMapView.MonsterName(r.MonsterType);
            var sheet = UiFrames.OpenSheet(_canvas.transform, $"{Loc.F("base.level", r.MonsterLevel)} {monster}", Close, footer: 128f);
            _window = sheet;
            var content = sheet.Content;

            // Who fought whom and how it ended.
            UiFrames.Versus(content,
                (Loc.T("battle.us"), Loc.T(r.Won ? "battle.won" : "battle.lost"), r.Won, Loc.F("hero.power", r.AttackerPower.ToString("N0"))),
                (monster, Loc.T(r.Won ? "battle.lost" : "battle.won"), !r.Won, Loc.F("hero.power", r.DefenderPower.ToString("N0")))).Row(230);
            UiFrames.SheetText(content, $"X:{r.X} Y:{r.Y}   ·   {Loc.F("battle.rounds", r.Rounds)}", UiKit.TypeScale.Caption + 2).Row(48);

            // Our side: the heroes in a row, then the troops with what they lost (wounded).
            UiFrames.Section(content, Loc.T("battle.us")).Row(60);
            var heroes = (r.HeroIds ?? Array.Empty<string>()).Where(id => ClientCatalog.Catalog.TryGetHero(id, out _)).Take(5).ToList();
            if (heroes.Count > 0)
            {
                var row = UiKit.Rect("Heroes", content).Row(150);
                for (var i = 0; i < heroes.Count; i++)
                {
                    ClientCatalog.Catalog.TryGetHero(heroes[i], out var hero);
                    var face = UiKit.Rect("Hero", row).Place(0.02f + i * 0.196f, 0f, 0.186f + i * 0.196f, 1f);
                    SocialView.Portrait(face, hero, true, 0, 0, 1, 1);
                }
            }

            Table(r.Attacker, true, null);

            // The swarm: its stacks fight as troop types but go by the swarm's name.
            UiFrames.Section(content, monster).Row(60);
            Table(r.Defender, false, monster);

            // Spoils, as reward tiles.
            UiFrames.Section(content, Loc.T("battle.spoils")).Row(60);
            var spoils = r.Rewards ?? Array.Empty<ItemCountDto>();
            if (spoils.Length == 0)
            {
                UiFrames.Note(content, Loc.T("battle.nospoils")).Row(90);
            }
            else
            {
                var grid = UiFrames.Grid(content, 5);
                foreach (var s in spoils)
                    UiFrames.ItemTile(grid, QuestText.ItemIcon(s.Id, null), "rare", "×" + CostChips.Compact(s.Count));
            }

            UiFrames.SheetText(content, Loc.T("battle.home"), UiKit.TypeScale.Caption + 2).Row(52);
            UiFrames.FooterButton(sheet.Footer, Loc.T("pvp.result.ok"), Close, UiKit.Primary, 0.25f, 0.75f);
            UiFrames.FitSheet(sheet);

            void Table(BattleStackDto[] stacks, bool ours, string unitName)
            {
                var all = (stacks ?? Array.Empty<BattleStackDto>()).OrderByDescending(s => s.Start).ToList();
                if (all.Count == 0)
                {
                    UiFrames.TableRow(content, new[] { "—" }).Row(64);
                    return;
                }

                UiFrames.TableRow(content, new[] { string.Empty, Loc.T("mail.report.start"), ours ? Loc.T("battle.wounded") : Loc.T("mail.report.lost") }, header: true)
                    .Row(52);
                for (var i = 0; i < all.Count; i++)
                {
                    var s = all[i];
                    UiFrames.TableRow(content, new[]
                    {
                        unitName ?? $"T{s.Tier} {Loc.T("troop." + s.Type)}", s.Start.ToString("N0"),
                        s.Lost > 0 ? $"<color={(ours ? "#B86A0C" : "#C8352B")}>−{s.Lost:N0}</color>" : "0"
                    }, index: i).Row(64);
                }
            }
        }
    }
}
