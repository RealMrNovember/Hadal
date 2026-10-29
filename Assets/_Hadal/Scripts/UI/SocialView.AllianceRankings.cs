using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance's rankings (docs/31 step 7): Power, Kills and Donations tabs; the top three in gold, silver and
    /// bronze, and the player's own row pinned at the bottom. Fetched each time a tab opens.
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
            UiKit.Choice("RankTabs", _list, new[] { Loc.T("alliance.rank.tab.power"), Loc.T("alliance.rank.tab.kills"), Loc.T("alliance.rank.tab.donations") },
                () => _rankingTab, OpenAllianceRankings).Row(90);
            var kind = RankingKinds[Mathf.Clamp(_rankingTab, 0, RankingKinds.Length - 1)];
            var ranking = View.AllianceRanking(kind);
            if (ranking == null)
            {
                Note(Loc.T("alliance.log.loading"));
                return;
            }

            var entries = ranking.Entries ?? Array.Empty<AllianceRankEntryDto>();
            foreach (var e in entries)
                RankRow(e, kind, false);

            // The player's own row stays in reach at the bottom, as in the genre.
            var mine = entries.FirstOrDefault(e => e.PlayerId == View.PlayerId);
            if (mine != null)
            {
                Header(Loc.T("alliance.rank.mine"));
                RankRow(mine, kind, true);
            }
        }

        private void RankRow(AllianceRankEntryDto e, string kind, bool pinned)
        {
            var card = Card(130);
            var medal = e.Rank switch
            {
                1 => new Color(0.98f, 0.74f, 0.25f, 1f),
                2 => new Color(0.75f, 0.8f, 0.86f, 1f),
                3 => new Color(0.8f, 0.5f, 0.25f, 1f),
                _ => UiKit.Muted
            };
            if (e.Rank <= 3)
                card.color = new Color(medal.r * 0.25f, medal.g * 0.25f, medal.b * 0.25f, 1f);
            if (pinned)
                card.color = new Color(0.06f, 0.16f, 0.24f, 1f);
            CardText(card.transform, e.Rank.ToString(), e.Rank <= 3 ? 40 : 32, medal, 0.01f, 0.1f, 0.11f, 0.9f, TextAnchor.MiddleCenter, FontStyle.Bold);
            var face = UiKit.Rect("Face", card.transform).Place(0.12f, 0.1f, 0.24f, 0.9f);
            AvatarDisc.Draw(face, e.AvatarId, e.Name, e.PlayerId);
            var name = CardText(card.transform, e.Name, 30, e.PlayerId == View.PlayerId ? UiKit.Accent : UiKit.Text, 0.26f, 0.1f, 0.66f, 0.9f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            name.supportRichText = false;
            var value = kind == AllianceRankingKinds.Power ? Loc.F("hero.power", e.Value.ToString("N0"))
                : kind == AllianceRankingKinds.Kills ? Loc.F("alliance.rank.kills", e.Value.ToString("N0"))
                : Loc.F("alliance.rank.donated", e.Value.ToString("N0"));
            CardText(card.transform, value, 26, UiKit.Text, 0.66f, 0.1f, 0.98f, 0.9f, TextAnchor.MiddleRight);
        }
    }
}
