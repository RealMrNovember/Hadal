using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using TMPro;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance hub (docs/31 §1, Last Asylum's alliance_hub): the banner card (emblem, [TAG] Name, then leader, power,
    /// members and language as label · value lines), the officers' announcement in a grey box with Edit, a two-column
    /// grid of feature tiles with red dots, and the navy icon bar fixed at the bottom (battle log, log, applications,
    /// settings). War opens the PvP war window and Rally Point the PvP rally card (<see cref="RallyPointPrompt"/>).
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabHub = 0;
        private const int AllianceTabHelp = 1;
        private const int AllianceTabMembers = 2;
        private const int AllianceTabSettings = 3;
        private const int AllianceTabApplications = 4;

        /// <summary>The hub's icon bar, in canvas units.</summary>
        private const float HubBarHeight = 176f;

        private string _editAnnouncement;
        private string _editLanguage;

        /// <summary>Opens the alliance war page (attacks, defences, rallies), once PvP provides one.</summary>
        public Action WarPage { get; set; }

        private void OpenAllianceTab(int tab)
        {
            _allianceTab = tab;
            Render(force: true);
        }

        private void RenderHub(AllianceDto alliance, int myRank)
        {
            PageTitle(Loc.T("social.alliance"));

            // The banner card: emblem on the left; [TAG] Name over the label · value lines.
            var leader = alliance.Members.FirstOrDefault(m => m.IsLeader)?.Name ?? "-";
            var head = InkCard(320);
            AvatarDisc.Emblem(UiKit.Rect("Banner", head.transform).Place(0.03f, 0.08f, 0.27f, 0.92f), alliance.Emblem);
            var name = Ink(head.transform, $"[{alliance.Tag}] {alliance.Name}", UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.31f, 0.76f, 0.97f, 0.95f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            name.richText = false;
            name.Fit(18, UiKit.TypeScale.Subtitle);
            var language = string.IsNullOrEmpty(alliance.Language) ? "-" : Loc.DisplayName(alliance.Language);
            UiFrames.InfoLine(head.transform, Loc.T("alliance.role.leader") + ":", leader, 0.31f, 0.57f, 0.97f, 0.75f);
            UiFrames.InfoLine(head.transform, Loc.T("hud.power") + ":", alliance.Members.Sum(m => m.Power).ToString("N0"), 0.31f, 0.39f, 0.97f, 0.57f);
            UiFrames.InfoLine(head.transform, Loc.T("alliance.tile.members") + ":", $"{alliance.Members.Length}/{alliance.MaxMembers}", 0.31f, 0.21f, 0.97f, 0.39f);
            UiFrames.InfoLine(head.transform, Loc.T("settings.language") + ":", language, 0.31f, 0.03f, 0.97f, 0.21f);

            // The announcement, as written by the officers, in the grey box; officers edit it from here.
            var none = string.IsNullOrEmpty(alliance.Announcement);
            var text = none ? Loc.T("alliance.announcement.none") : alliance.Announcement;
            var lines = text.Split('\n').Sum(l => 1 + l.Length / 46);
            var canEdit = AlliancePermissions.CanEditAnnouncement(myRank, View.MyRole());
            var height = Mathf.Clamp(60 + lines * 36 + (canEdit ? 70 : 0), 150, 560);
            var note = InkCard(height, UiFrames.CardAlt);
            var bottom = canEdit ? 80f / height : 0.04f;
            var body = Ink(note.transform, text, UiKit.TypeScale.Body, none ? UiFrames.InkSoft : UiFrames.Ink, 0.04f, bottom, 0.96f, 0.95f, TextAnchor.UpperLeft);
            body.richText = false;
            if (canEdit)
            {
                var edit = CardButton(note.transform, Loc.T("alliance.announcement.edit"), true, () => OpenAllianceTab(AllianceTabSettings), UiKit.Primary,
                    0.72f, 0.04f, 0.97f, bottom - 0.02f);
                edit.GetComponentInChildren<TMP_Text>().fontSizeMax = UiKit.TypeScale.Caption;
            }

            // The tiles, two a row, Last Asylum's order; a red dot where something waits.
            var waiting = View.HelpRequests.Count(r => !r.HelpedByMe);
            var gifts = GiftsWaiting();
            var tiles = new (string Label, string Icon, bool Dot, Action Open)[]
            {
                (Loc.T("alliance.tile.war"), "event_war", false, OpenWar),
                (Loc.T("alliance.tile.help"), "left_help", waiting > 0, () => OpenAllianceTab(AllianceTabHelp)),
                (Loc.T("alliance.tile.gifts"), "event_gift", gifts > 0, () => OpenAllianceTab(AllianceTabGifts)),
                (Loc.T("alliance.tile.members"), "nav_heroes", false, () => OpenAllianceTab(AllianceTabMembers)),
                (Loc.T("alliance.tile.shop"), "shop", false, () => OpenAllianceTab(AllianceTabShop)),
                (Loc.T("alliance.tile.research"), "left_research", false, () => OpenAllianceTab(AllianceTabResearch)),
                (Loc.T("alliance.tile.rankings"), "power", false, () => OpenAllianceRankings(0)),
                (Loc.T("alliance.tile.polls"), "g_info", PollsWaiting() > 0, () => OpenAllianceTab(AllianceTabPolls)),
                (Loc.T("alliance.tile.duel"), "g_attack", AllianceEventWaiting(EventTemplates.AllianceDuel), () => OpenAllianceEvent(EventTemplates.AllianceDuel)),
                (Loc.T("alliance.tile.mobilization"), "g_army", AllianceEventWaiting(EventTemplates.Mobilization), () => OpenAllianceEvent(EventTemplates.Mobilization)),
                (Loc.T("alliance.tile.rally"), "g_rally", false, () => { Close(); RallyPointPrompt.Instance?.OpenRallyPoint(); }),
                (Loc.T("city.list"), "left_home", false, () => { Close(); CityList.Instance?.Open(); })
            };
            var grid = UiFrames.Grid(_list, 2, 3.2f, 18f);
            grid.GetComponent<UnityEngine.UI.GridLayoutGroup>().padding = new RectOffset(0, 0, 8, 8);
            foreach (var tile in tiles)
            {
                var open = tile.Open;
                UiFrames.Feature(grid, tile.Icon, tile.Label, () => open(), tile.Dot);
            }

            // The icon bar along the bottom: the logs for everyone, applications and settings for officers.
            var applicants = alliance.Applicants?.Length ?? 0;
            var bar = new System.Collections.Generic.List<(string Icon, string Label, bool Dot, Action Click)>
            {
                ("event_war", Loc.T("alliance.bar.battlelog"), false, () => OpenAllianceLog(AllianceLogKinds.Battle)),
                ("quest", Loc.T("alliance.bar.log"), false, () => OpenAllianceLog(AllianceLogKinds.Log))
            };
            if (AlliancePermissions.CanApprove(myRank))
                bar.Add(("g_invite", Loc.T("alliance.bar.applications"), applicants > 0, () => OpenAllianceTab(AllianceTabApplications)));
            if (canEdit)
                bar.Add(("g_details", Loc.T("alliance.bar.settings"), false, () => OpenAllianceTab(AllianceTabSettings)));
            var holder = Fixed("HubBar", () => _page == Page.Alliance && _allianceTab == AllianceTabHub && View.MyAlliance != null).Place(0, 0, 1, 1);
            UiFrames.IconBar(holder, bar.ToArray(), HubBarHeight);
            ListAbove(HubBarHeight + 12f);
        }

        /// <summary>The PvP war window (attacks, defences, rallies), or a hook set in its place.</summary>
        private void OpenWar()
        {
            if (WarPage != null)
                WarPage();
            else if (PvpCenter.Instance != null)
            {
                Close();
                PvpCenter.Instance.Open(2); // the alliance war tab
            }
            else
                Toast(Loc.T("alliance.war.soon"), UiKit.Amber);
        }

        /// <summary>The announcement (officers) and the alliance language (the leader).</summary>
        private void RenderAnnouncementEditor(AllianceDto alliance, int myRank)
        {
            Band(Loc.T("alliance.announcement"));
            _editAnnouncement ??= alliance.Announcement ?? string.Empty;
            var input = UiKit.Input("Announcement", _list, Loc.T("alliance.announcement.hint"), characterLimit: SocialRules.MaxAnnouncement).Row(280);
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.textComponent.alignment = TextAlignmentOptions.TopLeft;
            input.textComponent.fontSize = UiKit.TypeScale.Body;
            input.textComponent.textWrappingMode = TextWrappingModes.Normal;
            ((TMP_Text)input.placeholder).alignment = TextAlignmentOptions.TopLeft;
            input.text = _editAnnouncement;
            input.onValueChanged.AddListener(v => _editAnnouncement = v);

            var leader = myRank == AlliancePermissions.Leader;
            _editLanguage ??= alliance.Language ?? string.Empty;
            if (leader)
            {
                // One button that steps through the game's languages.
                var row = InkCard(110);
                Ink(row.transform, Loc.T("alliance.language"), UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.1f, 0.5f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
                var shown = string.IsNullOrEmpty(_editLanguage) ? "-" : Loc.DisplayName(_editLanguage);
                CardButton(row.transform, shown, true, () =>
                {
                    var codes = Loc.Languages.Select(l => l.Code).ToArray();
                    _editLanguage = codes[(Array.IndexOf(codes, _editLanguage) + 1) % codes.Length];
                    Render(force: false);
                }, UiKit.TabOn, 0.52f, 0.12f, 0.97f, 0.88f);
            }

            WideButton(Loc.T("alliance.save"), true, () =>
            {
                _commands.Dispatch(new SetAllianceInfoCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), Announcement = (_editAnnouncement ?? string.Empty).Trim(),
                    Language = leader ? _editLanguage ?? string.Empty : string.Empty
                });
                _editAnnouncement = _editLanguage = null;
            }, UiKit.Primary);
        }
    }
}
