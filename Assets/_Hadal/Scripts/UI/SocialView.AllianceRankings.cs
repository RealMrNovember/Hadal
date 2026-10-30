using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance's rankings (docs/31 step 7): Power, Kills and Donations tabs over Last Asylum's ranking table — the
    /// top three on gold, silver and bronze rows, and the player's own row pinned green under the table. Fetched each
    /// time a tab opens.
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabRankings = 10;

        private static readonly string[] RankingKinds = { AllianceRankingKinds.Power, AllianceRankingKinds.Kills, AllianceRankingKinds.Donations };

        private int _rankingTab;

        private void OpenAllianceRankings(int tab)
        {
            _rankingTab = Mathf.Clamp(tab, 0, RankingKinds.Length - 1);
            _commands.Dispatch(new ViewAllianceRankingCommand { CommandId = NewId(), ClientSequence = NextSequence(), Kind = RankingKinds[_rankingTab] });
            OpenAllianceTab(AllianceTabRankings);
        }

        private void RenderAllianceRankings()
        {
            var labels = new[] { Loc.T("alliance.rank.tab.power"), Loc.T("alliance.rank.tab.kills"), Loc.T("alliance.rank.tab.donations") };
            UiFrames.Tabs(_list, labels, _rankingTab, OpenAllianceRankings).Row(96);
            var tab = Mathf.Clamp(_rankingTab, 0, RankingKinds.Length - 1);
            var kind = RankingKinds[tab];
            var ranking = View.AllianceRanking(kind);
            if (ranking == null)
            {
                InkNote(Loc.T("alliance.log.loading"));
                return;
            }

            var entries = ranking.Entries ?? Array.Empty<AllianceRankEntryDto>();
            RankTableHead(labels[tab]);
            for (var i = 0; i < entries.Length; i++)
                AllianceRankRow(_list, entries[i], kind, false, i).Row(110);

            // The player's own row stays in reach under the table, as in the genre.
            var mine = entries.FirstOrDefault(e => e.PlayerId == View.PlayerId);
            if (mine != null)
            {
                var shownTab = _rankingTab;
                var pinned = PinnedRow(() => _page == Page.Alliance && _allianceTab == AllianceTabRankings && _rankingTab == shownTab, 110);
                AllianceRankRow(pinned, mine, kind, true, 0).Place(0, 0, 1, 1);
            }
        }

        private RectTransform AllianceRankRow(Transform parent, AllianceRankEntryDto e, string kind, bool pinned, int index)
        {
            var value = kind == AllianceRankingKinds.Power ? e.Value.ToString("N0")
                : kind == AllianceRankingKinds.Kills ? Loc.F("alliance.rank.kills", e.Value.ToString("N0"))
                : Loc.F("alliance.rank.donated", e.Value.ToString("N0"));
            var row = UiFrames.RankRow(parent, e.Rank, e.Name, value, pinned, index, out var face);
            AvatarDisc.Draw(face, e.AvatarId, e.Name, e.PlayerId);
            return row;
        }
    }
}
