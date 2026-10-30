using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance log (joins and leaves, rank changes, research) and the battle log (docs/31 §2), newest first with the
    /// date; fetched from the server each time the page opens.
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabLog = 5;
        private const int AllianceTabBattleLog = 6;

        private int _logTab; // 0 members in and out, 1 ranks, 2 research

        private static readonly string[][] LogTabTypes =
        {
            new[] { "joined", "left", "kicked" },
            new[] { "rank", "leader" },
            new[] { "research" }
        };

        private void OpenAllianceLog(string kind)
        {
            _commands.Dispatch(new ViewAllianceLogCommand { CommandId = NewId(), ClientSequence = NextSequence(), Kind = kind });
            OpenAllianceTab(kind == AllianceLogKinds.Battle ? AllianceTabBattleLog : AllianceTabLog);
        }

        private void RenderAllianceLog(string kind)
        {
            var log = View.AllianceLog(kind);
            Header(Loc.T(kind == AllianceLogKinds.Battle ? "alliance.bar.battlelog" : "alliance.bar.log"));
            var entries = (log?.Entries ?? Array.Empty<AllianceLogEntryDto>()).AsEnumerable();
            if (kind == AllianceLogKinds.Log)
            {
                UiKit.Choice("LogTabs", _list, new[] { Loc.T("alliance.log.tab.members"), Loc.T("alliance.log.tab.ranks"), Loc.T("alliance.log.tab.research") },
                    () => _logTab, i => { _logTab = i; Render(force: true); }).Row(90);
                entries = entries.Where(e => Array.IndexOf(LogTabTypes[Mathf.Clamp(_logTab, 0, 2)], e.Type) >= 0);
            }

            var shown = entries.ToList();
            if (log == null)
            {
                Note(Loc.T("alliance.log.loading"));
                return;
            }

            if (shown.Count == 0)
            {
                Note(Loc.T("alliance.log.empty"));
                return;
            }

            foreach (var e in shown)
            {
                var card = Card(110);
                var text = CardText(card.transform, LogText(kind, e), 26, UiKit.Text, 0.03f, 0.08f, 0.78f, 0.92f, TextAnchor.MiddleLeft);
                text.richText = false;
                var at = DateTimeOffset.FromUnixTimeMilliseconds(e.AtMs).ToLocalTime();
                CardText(card.transform, at.ToString("dd.MM HH:mm"), 22, UiKit.Muted, 0.78f, 0.08f, 0.97f, 0.92f, TextAnchor.MiddleRight);
            }
        }

        /// <summary>Words one line: "alliance.log.joined" / "alliance.battle.attack_won" with its arguments.</summary>
        private static string LogText(string kind, AllianceLogEntryDto e)
        {
            var args = (e.Args ?? Array.Empty<string>()).Cast<object>().ToArray();
            if (e.Type == "rank" && args.Length > 1 && int.TryParse(args[1]?.ToString(), out var rank))
                args[1] = $"R{rank} {RankName(rank)}";
            if (e.Type == "research" && args.Length > 1)
                args[1] = Loc.T("alliance.research.node." + args[1]);
            var key = (kind == AllianceLogKinds.Battle ? "alliance.battle." : "alliance.log.") + e.Type;
            if (!Loc.Has(key))
                return string.Join(" · ", args);
            try
            {
                return string.Format(Loc.T(key), args.Concat(Enumerable.Repeat((object)string.Empty, 4)).ToArray());
            }
            catch (FormatException)
            {
                return string.Join(" · ", args);
            }
        }
    }
}
