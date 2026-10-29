using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance hub (docs/31 §1, sampled from Last Asylum): a header card (banner, [TAG] Name, leader, power, members,
    /// language) with the officers' announcement under it, a two-column grid of tiles with red dots, and a bottom bar with
    /// the logs, applications and settings. Pages that do not exist yet say so; War opens the PvP war window and Rally
    /// Point the PvP rally card (<see cref="RallyPointPrompt"/>).
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabHub = 0;
        private const int AllianceTabHelp = 1;
        private const int AllianceTabMembers = 2;
        private const int AllianceTabSettings = 3;
        private const int AllianceTabApplications = 4;

        private string _editAnnouncement;
        private string _editLanguage;

        /// <summary>Opens the alliance war page (attacks, defences, rallies), once PvP provides one.</summary>
        public Action WarPage { get; set; }

        private void OpenAllianceTab(int tab)
        {
            _allianceTab = tab;
            Render(force: true);
        }

        /// <summary>A row back to the hub on top of every alliance sub-page.</summary>
        private void BackToHub()
        {
            var row = UiKit.Rect("Back", _list).Row(90);
            CardButton(row, Loc.T("alliance.back"), true, () => OpenAllianceTab(AllianceTabHub), UiKit.Field, 0f, 0.08f, 0.4f, 0.92f);
        }

        private void RenderHub(AllianceDto alliance, int myRank)
        {
            // Header: banner, [TAG] Name, leader, power, members and language.
            var leader = alliance.Members.FirstOrDefault(m => m.IsLeader)?.Name ?? "-";
            var head = Card(230);
            head.color = new Color(0.06f, 0.16f, 0.24f, 1f);
            AvatarDisc.Emblem(UiKit.Rect("Banner", head.transform).Place(0.02f, 0.1f, 0.2f, 0.9f), alliance.Emblem);
            var name = CardText(head.transform, $"[{alliance.Tag}] {alliance.Name}", 38, UiKit.Accent, 0.23f, 0.7f, 0.98f, 0.96f, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.supportRichText = false;
            var language = string.IsNullOrEmpty(alliance.Language) ? "-" : Loc.NameOf(alliance.Language);
            CardText(head.transform,
                $"{Loc.F("alliance.leader", leader)}\n{Loc.F("hero.power", alliance.Members.Sum(m => m.Power).ToString("N0"))}\n" +
                $"{Loc.F("alliance.members", alliance.Members.Length, alliance.MaxMembers)}   ·   {Loc.F("alliance.hub.language", language)}",
                24, UiKit.Text, 0.23f, 0.04f, 0.98f, 0.7f, TextAnchor.MiddleLeft);

            // The announcement, as written by the officers.
            var text = string.IsNullOrEmpty(alliance.Announcement) ? Loc.T("alliance.announcement.none") : alliance.Announcement;
            var lines = text.Split('\n').Sum(l => 1 + l.Length / 44);
            var note = Card(Mathf.Clamp(70 + lines * 34, 120, 520));
            CardText(note.transform, Loc.T("alliance.announcement"), 24, UiKit.Amber, 0.04f, 0.8f, 0.96f, 0.98f, TextAnchor.UpperLeft, FontStyle.Bold);
            var body = CardText(note.transform, text, 26, string.IsNullOrEmpty(alliance.Announcement) ? UiKit.Muted : UiKit.Text, 0.04f, 0.04f, 0.96f, 0.8f);
            body.supportRichText = false;

            // Nine tiles, two a row; a red dot where something waits.
            var waiting = View.HelpRequests.Count(r => !r.HelpedByMe);
            var tiles = new (string Label, string Icon, bool Dot, Action Open)[]
            {
                (Loc.T("alliance.tile.war"), "event_war", false, OpenWar),
                (waiting > 0 ? $"{Loc.T("alliance.tile.help")} ({waiting})" : Loc.T("alliance.tile.help"), "left_help", waiting > 0, () => OpenAllianceTab(AllianceTabHelp)),
                (Loc.T("alliance.tile.gifts"), "event_gift", false, () => AllianceSoon("alliance.tile.gifts")),
                (Loc.T("alliance.tile.members"), "nav_heroes", false, () => OpenAllianceTab(AllianceTabMembers)),
                (Loc.T("alliance.tile.shop"), "shop", false, () => AllianceSoon("alliance.tile.shop")),
                (Loc.T("alliance.tile.research"), "left_research", false, () => AllianceSoon("alliance.tile.research")),
                (Loc.T("alliance.tile.rankings"), "power", false, () => AllianceSoon("alliance.tile.rankings")),
                (Loc.T("alliance.tile.rally"), "nav_world", false, () => { Close(); RallyPointPrompt.Instance?.OpenRallyPoint(); }),
                (Loc.T("alliance.tile.log"), "quest", false, () => OpenAllianceLog(AllianceLogKinds.Log))
            };
            for (var start = 0; start < tiles.Length; start += 2)
            {
                var row = UiKit.Rect("Tiles", _list).Row(170);
                for (var c = 0; c < 2 && start + c < tiles.Length; c++)
                {
                    var tile = tiles[start + c];
                    var box = UiKit.Box("Tile", row, UiKit.Field);
                    box.rectTransform.Place(c * 0.5f + 0.005f, 0.04f, c * 0.5f + 0.495f, 0.96f);
                    var button = box.gameObject.AddComponent<Button>();
                    button.targetGraphic = box;
                    var open = tile.Open;
                    button.onClick.AddListener(() => open());
                    HudIconImage(box.transform, tile.Icon, 0.04f, 0.15f, 0.3f, 0.85f);
                    CardText(box.transform, tile.Label, 28, UiKit.Text, 0.33f, 0.1f, 0.97f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
                    if (tile.Dot)
                        RedDot(box.transform);
                }
            }

            // Bottom bar: the logs for everyone, applications and settings for officers.
            var applicants = alliance.Applicants?.Length ?? 0;
            var bar = new System.Collections.Generic.List<(string Label, bool Dot, Action Open)>
            {
                (Loc.T("alliance.bar.battlelog"), false, () => OpenAllianceLog(AllianceLogKinds.Battle)),
                (Loc.T("alliance.bar.log"), false, () => OpenAllianceLog(AllianceLogKinds.Log))
            };
            if (AlliancePermissions.CanApprove(myRank))
                bar.Add((applicants > 0 ? $"{Loc.T("alliance.bar.applications")} ({applicants})" : Loc.T("alliance.bar.applications"), applicants > 0,
                    () => OpenAllianceTab(AllianceTabApplications)));
            if (AlliancePermissions.CanEditAnnouncement(myRank))
                bar.Add((Loc.T("alliance.bar.settings"), false, () => OpenAllianceTab(AllianceTabSettings)));
            var strip = UiKit.Rect("Bar", _list).Row(110);
            var width = 1f / bar.Count;
            for (var i = 0; i < bar.Count; i++)
            {
                var entry = bar[i];
                var button = CardButton(strip, entry.Label, true, () => entry.Open(), UiKit.Field, i * width + 0.005f, 0.08f, (i + 1) * width - 0.005f, 0.92f);
                button.GetComponentInChildren<Text>().fontSize = 26;
                if (entry.Dot)
                    RedDot(button.transform);
            }
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

        private void AllianceSoon(string key) => Toast(Loc.F("alliance.soon", Loc.T(key)), UiKit.Amber);

        /// <summary>The announcement (officers) and the alliance language (the leader).</summary>
        private void RenderAnnouncementEditor(AllianceDto alliance, int myRank)
        {
            Header(Loc.T("alliance.announcement"));
            _editAnnouncement ??= alliance.Announcement ?? string.Empty;
            var input = UiKit.Input("Announcement", _list, Loc.T("alliance.announcement.hint"), characterLimit: SocialRules.MaxAnnouncement).Row(260);
            input.lineType = InputField.LineType.MultiLineNewline;
            input.textComponent.alignment = TextAnchor.UpperLeft;
            input.textComponent.fontSize = 30;
            input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            ((Text)input.placeholder).alignment = TextAnchor.UpperLeft;
            input.text = _editAnnouncement;
            input.onValueChanged.AddListener(v => _editAnnouncement = v);

            var leader = myRank == AlliancePermissions.Leader;
            _editLanguage ??= alliance.Language ?? string.Empty;
            if (leader)
            {
                // One button that steps through the game's languages.
                var row = Card(110);
                CardText(row.transform, Loc.T("alliance.language"), 28, UiKit.Text, 0.04f, 0.1f, 0.5f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
                var shown = string.IsNullOrEmpty(_editLanguage) ? "-" : Loc.NameOf(_editLanguage);
                CardButton(row.transform, shown, true, () =>
                {
                    var codes = Loc.Languages.Select(l => l.Code).ToArray();
                    _editLanguage = codes[(Array.IndexOf(codes, _editLanguage) + 1) % codes.Length];
                    Render(force: false);
                }, UiKit.Field, 0.52f, 0.12f, 0.97f, 0.88f);
            }

            var save = Card(110);
            CardButton(save.transform, Loc.T("alliance.save"), true, () =>
            {
                _commands.Dispatch(new SetAllianceInfoCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), Announcement = (_editAnnouncement ?? string.Empty).Trim(),
                    Language = leader ? _editLanguage ?? string.Empty : string.Empty
                });
                _editAnnouncement = _editLanguage = null;
            }, null, 0.25f, 0.1f, 0.75f, 0.9f);
        }
    }
}
