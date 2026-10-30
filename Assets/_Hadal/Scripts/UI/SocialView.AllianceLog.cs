using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance log (joins and leaves, rank changes, research; tabs) and the battle log (docs/31 §2): zebra lines,
    /// newest first, an icon per kind and the date on the right; fetched from the server each time the page opens.
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
            var entries = (log?.Entries ?? Array.Empty<AllianceLogEntryDto>()).AsEnumerable();
            if (kind == AllianceLogKinds.Log)
            {
                UiFrames.Tabs(_list, new[] { Loc.T("alliance.log.tab.members"), Loc.T("alliance.log.tab.ranks"), Loc.T("alliance.log.tab.research") },
                    Mathf.Clamp(_logTab, 0, 2), i => { _logTab = i; Render(force: true); }).Row(96);
                entries = entries.Where(e => Array.IndexOf(LogTabTypes[Mathf.Clamp(_logTab, 0, 2)], e.Type) >= 0);
            }

            var shown = entries.ToList();
            if (log == null)
            {
                InkNote(Loc.T("alliance.log.loading"));
                return;
            }

            if (shown.Count == 0)
            {
                InkNote(Loc.T("alliance.log.empty"));
                return;
            }

            for (var i = 0; i < shown.Count; i++)
            {
                var e = shown[i];
                var card = InkCard(110, i % 2 == 0 ? UiFrames.Card : UiFrames.CardAlt);
                HudIconImage(card.transform, LogIcon(kind, e.Type), 0.015f, 0.15f, 0.1f, 0.85f);
                var text = Ink(card.transform, LogText(kind, e), UiKit.TypeScale.Caption + 2, UiFrames.Ink, 0.12f, 0.06f, 0.78f, 0.94f);
                text.richText = false;
                var at = DateTimeOffset.FromUnixTimeMilliseconds(e.AtMs).ToLocalTime();
                Ink(card.transform, at.ToString("dd.MM") + "\n" + at.ToString("HH:mm"), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.78f, 0.06f, 0.97f, 0.94f,
                    TextAnchor.MiddleRight);
            }
        }

        /// <summary>The line's icon: battles by kind, the log by what changed.</summary>
        private static string LogIcon(string kind, string type)
        {
            type ??= string.Empty;
            if (kind == AllianceLogKinds.Battle)
                return type == "scout" ? "g_scout"
                    : type.StartsWith("rally", StringComparison.Ordinal) ? "g_rally"
                    : type.StartsWith("defence", StringComparison.Ordinal) ? "g_shield"
                    : "g_attack";
            return type switch
            {
                "joined" => "g_invite",
                "left" or "kicked" => "g_profile",
                "research" => "left_research",
                _ => "g_star"
            };
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
