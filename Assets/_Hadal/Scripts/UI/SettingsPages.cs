using System;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services;
using Hadal.Core.Services.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The Settings home page and its sub-pages, after Last Asylum's settings (p06/p07): the home page shows who and
    /// which version, then a grid of icon tiles; the game settings are pale section bands (Sound, Graphics) with a
    /// label and an ON/OFF switch per line and check boxes for exclusive choices. Account pages live in
    /// AccountPanelView.cs.
    /// </summary>
    public sealed partial class AccountPanelView
    {
        public const string WebsiteUrl = "https://hadal.cicibyte.com/";

        private RectTransform _home;
        private RectTransform _language;
        private RectTransform _game;
        private RectTransform _notifications;
        private RectTransform _support;
        private RectTransform _about;

        private TMP_Text _homeProfile;
        private TMP_Text _supportId;
        private Button _supportCopy;
        private TMP_Text _aboutAccount;

        private void BuildSettings(Transform card)
        {
            BuildHome(_home = Page("Home", card, "settings.title"));
            BuildLanguage(_language = Page("Language", card, "settings.language"));
            BuildGame(_game = Page("Game", card, "settings.title"));
            BuildNotifications(_notifications = Page("Notifications", card, "settings.notifications"));
            BuildSupport(_support = Page("Support", card, "settings.support"));
            BuildAbout(_about = Page("About", card, "settings.about"));
        }

        /// <summary>Who is signed in and the version on top, then the tiles (four a row) to every page.</summary>
        private void BuildHome(Transform column)
        {
            _homeProfile = UiKit.Label("Profile", column, string.Empty, UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Row(44);
            UiKit.Label("Version", column, Loc.F("about.version", Application.version), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .Row(44);

            var tiles = new (string Name, string Icon, string Key, Func<RectTransform> Target)[]
            {
                ("Account", "g_profile", "settings.account", () => _main),
                ("Settings", "⚙", "settings.title", () => _game),
                ("Language", "🌐", "settings.language", () => _language),
                ("Notifications", "🔔", "settings.notifications", () => _notifications),
                ("Support", "left_help", "settings.support", () => _support),
                ("Gift", "event_gift", "settings.gift", () => _gift),
                ("About", "g_info", "settings.about", () => _about)
            };

            // "Row0" keeps the tiles where the account flow test looks for them (Panel/Card/Home/Row0/Account).
            var grid = UiFrames.Grid(column, 4, 1f, 16f);
            grid.name = "Row0";
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 8, 8);
            foreach (var tile in tiles)
            {
                var target = tile.Target;
                UiFrames.IconTile(grid, tile.Icon, Loc.T(tile.Key), () => Show(target())).name = tile.Name;
            }
        }

        private void BuildLanguage(Transform column)
        {
            UiFrames.Note(column, Loc.T("language.hint")).Row(96);
            var list = UiKit.ScrollList("List", column, 10f);
            list.parent.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            foreach (var (code, name) in Loc.Languages)
            {
                var selected = code == Loc.Current;
                var button = UiKit.Button(code, list, name, () => ChooseLanguage(code), selected ? UiKit.Primary : UiKit.TabOn, UiKit.TypeScale.Subtitle).Row(96);
                var text = button.GetComponentInChildren<TMP_Text>();
                UiText.SetFont(text, UiKit.FontFor(code));
                text.text = selected ? "✓  " + Loc.DisplayName(code) : Loc.DisplayName(code);
            }
        }

        private static void ChooseLanguage(string code)
        {
            if (code == Loc.Current)
                return;

            Loc.SetLanguage(code);
            // Every screen is built with its strings, so a restart is the clean way to switch.
            SessionRestart.Restart();
        }

        /// <summary>Last Asylum's game settings: a band per section, an ON/OFF line per switch, check boxes for choices.</summary>
        private void BuildGame(Transform column)
        {
            var list = UiKit.ScrollList("List", column, 6f);
            list.parent.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            UiFrames.Section(list, Loc.T("settings.sound")).Row(60);
            UiFrames.ToggleRow(list, "🎵", Loc.T("sound.music"), () => GameSettings.MusicOn, v => GameSettings.MusicOn = v).Row(96);
            UiKit.Slider("MusicVolume", list, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v).Row(64);
            UiFrames.ToggleRow(list, "🔊", Loc.T("sound.effects"), () => GameSettings.SoundOn, v => GameSettings.SoundOn = v).Row(96);
            UiKit.Slider("EffectsVolume", list, GameSettings.SoundVolume, v =>
            {
                GameSettings.SoundVolume = v;
                GameAudio.Play(Sfx.UiTap);
            }).Row(64);
            UiFrames.ToggleRow(list, "📱", Loc.T("sound.vibration"), () => GameSettings.Vibration, v =>
            {
                GameSettings.Vibration = v;
#if UNITY_ANDROID || UNITY_IOS
                if (v)
                    Handheld.Vibrate();
#endif
            }).Row(96);

            UiFrames.Section(list, Loc.T("settings.graphics")).Row(60);
            Caption(list, Loc.T("graphics.quality"));
            UiFrames.CheckRow(list, new[] { Loc.T("graphics.low"), Loc.T("graphics.medium"), Loc.T("graphics.high") },
                () => (int)GameSettings.Quality, i => GameSettings.Quality = (GraphicsQuality)i).Row(84);
            Caption(list, Loc.T("graphics.fps"));
            UiFrames.CheckRow(list, new[] { "30 FPS", "60 FPS" }, () => GameSettings.FrameRate >= 60 ? 1 : 0, i => GameSettings.FrameRate = i == 1 ? 60 : 30)
                .Row(84);
            UiFrames.ToggleRow(list, null, Loc.T("graphics.power"), () => GameSettings.PowerSaving, v => GameSettings.PowerSaving = v).Row(96);
            Hint(list, Loc.T("graphics.power.hint"));
            UiFrames.ToggleRow(list, null, Loc.T("graphics.reducealerts"), () => GameSettings.ReduceAlertEffects, v => GameSettings.ReduceAlertEffects = v).Row(96);
            Hint(list, Loc.T("graphics.reducealerts.hint"));
        }

        private static void Caption(Transform list, string text)
        {
            var label = UiKit.Label("Caption", list, text, UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft);
            label.margin = new Vector4(20f, 0f, 0f, 0f);
            label.Row(52);
        }

        private static void Hint(Transform list, string text) =>
            UiKit.Label("Hint", list, text, UiKit.TypeScale.Caption, UiFrames.InkSoft, TextAnchor.UpperLeft).Row(70);

        /// <summary>
        /// Phone notifications (docs/36): the master switch, the timer categories the phone schedules, the alerts the
        /// server will push (shown as "soon" until push is live) and quiet hours. Choices are kept on this device and
        /// on the server (PhoneNotifications syncs them).
        /// </summary>
        private void BuildNotifications(Transform column)
        {
            _notifyBlocked = UiKit.Button("Blocked", column, Loc.T("notify.permission.open"), PhoneNotifications.OpenPhoneSettings, UiKit.Danger, UiKit.TypeScale.Body);
            _notifyBlocked.Row(90);
            var list = UiKit.ScrollList("List", column, 6f);
            list.parent.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            UiKit.Label("Hint", list, Loc.T("notify.hint"), UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.MiddleCenter).Row(52);
            UiFrames.ToggleRow(list, "🔔", Loc.T("notify.master"), () => !NotificationPrefsStore.Current.Off, on =>
            {
                var prefs = NotificationPrefsStore.Current;
                prefs.Off = !on;
                NotificationPrefsStore.Save(prefs);
                if (on)
                    PhoneNotifications.OnPlayerEnabledSomething();
            }).Row(96);

            UiFrames.Section(list, Loc.T("settings.notifications")).Row(60);
            for (var i = 0; i < NotificationRules.CategoryCount; i++)
            {
                var category = (NotificationCategory)i;
                if (category == NotificationCategory.Attack)
                    UiFrames.Section(list, Loc.T("notify.section.push")).Row(60);
                var row = UiFrames.ToggleRow(list, null, Loc.T(NotificationTexts.CategoryKey(category)),
                    () => NotificationPrefsStore.IsEnabled(category), on =>
                    {
                        NotificationPrefsStore.SetEnabled(category, on);
                        if (on)
                            PhoneNotifications.OnPlayerEnabledSomething();
                    });
                row.Row(88);
                if (NotificationRules.IsPush(category))
                    Soon(row);
            }

            UiFrames.Section(list, Loc.T("notify.quiet")).Row(60);
            UiFrames.ToggleRow(list, null, Loc.T("notify.quiet"), () => NotificationPrefsStore.Current.QuietHours, on =>
            {
                var prefs = NotificationPrefsStore.Current;
                prefs.QuietHours = on;
                NotificationPrefsStore.Save(prefs);
            }).Row(96);
            Caption(list, Loc.T("notify.quiet.from"));
            QuietChoice("From", list, QuietStarts, p => p.QuietStartMinute, (p, m) => p.QuietStartMinute = m);
            Caption(list, Loc.T("notify.quiet.to"));
            QuietChoice("To", list, QuietEnds, p => p.QuietEndMinute, (p, m) => p.QuietEndMinute = m);
        }

        private static readonly int[] QuietStarts = { 21 * 60, 22 * 60, 23 * 60, 0 };
        private static readonly int[] QuietEnds = { 6 * 60, 7 * 60, 8 * 60, 9 * 60 };

        private Button _notifyBlocked;

        private static void QuietChoice(string name, Transform list, int[] minutes, Func<NotificationPrefsDto, int> get,
            Action<NotificationPrefsDto, int> set)
        {
            var labels = Array.ConvertAll(minutes, m => $"{m / 60:00}:{m % 60:00}");
            var row = UiFrames.CheckRow(list, labels, () => Math.Max(0, Array.IndexOf(minutes, get(NotificationPrefsStore.Current))), i =>
            {
                var prefs = NotificationPrefsStore.Current;
                set(prefs, minutes[i]);
                NotificationPrefsStore.Save(prefs);
            });
            row.name = name;
            row.Row(84);
        }

        /// <summary>A push category before phase 2: greyed out, "soon" instead of ON/OFF.</summary>
        private static void Soon(Button row)
        {
            row.interactable = false;
            row.transform.Find("Label").GetComponent<TMP_Text>().color = UiFrames.InkSoft;
            row.transform.Find("Switch").GetComponent<Image>().color = new Color(0.7f, 0.74f, 0.8f, 1f);
            row.transform.Find("Switch/Knob").gameObject.SetActive(false);
            var state = row.transform.Find("Switch/State").GetComponent<TMP_Text>();
            state.rectTransform.Place(0, 0, 1, 1);
            state.text = Loc.T("notify.soon");
        }

        private void BuildSupport(Transform column)
        {
            UiFrames.Note(column, Loc.T("support.hint")).Row(160);
            UiFrames.Section(column, Loc.T("account.id")).Row(56);
            _supportId = UiKit.Label("AccountId", column, string.Empty, UiKit.TypeScale.Display, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Row(96);
            _supportCopy = UiKit.Button("Copy", column, Loc.T("support.copy"), CopyAccountId, UiKit.Field, UiKit.TypeScale.Subtitle);
            _supportCopy.Row(104);
            UiKit.Button("Website", column, Loc.T("support.website"), () => Application.OpenURL(WebsiteUrl), UiKit.Primary, UiKit.TypeScale.Subtitle).Row(104);
        }

        private void BuildAbout(Transform column)
        {
            var logo = UiKit.Label("Logo", column, "HADAL", 120, UiFrames.Navy, TextAnchor.MiddleCenter, FontStyle.Bold).Row(180);
            logo.characterSpacing = 8f;
            UiKit.Label("Tagline", column, Loc.T("title.tagline"), UiKit.TypeScale.Body + 2, UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Italic).Row(90);
            UiFrames.Section(column, Loc.F("about.version", Application.version)).Row(60);
            _aboutAccount = UiKit.Label("Account", column, string.Empty, UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter).Row(56);
            UiKit.Label("Studio", column, Loc.T("about.studio"), UiKit.TypeScale.Body, UiFrames.InkSoft, TextAnchor.MiddleCenter).Row(56);
            UiKit.Label("Site", column, "hadal.cicibyte.com", UiKit.TypeScale.Body, UiKit.Primary, TextAnchor.MiddleCenter, FontStyle.Bold).Row(56);
        }

        private string AccountId => _account.Profile?.accountId ?? string.Empty;

        private string ShortAccountId => AccountId.Substring(0, Math.Min(8, AccountId.Length)).ToUpperInvariant();

        private void CopyAccountId()
        {
            GUIUtility.systemCopyBuffer = AccountId;
            _supportCopy.GetComponentInChildren<TMP_Text>().text = Loc.T("support.copied");
        }

        /// <summary>Refreshes the texts that depend on the profile when a settings page opens.</summary>
        private void OnPageShown(RectTransform page)
        {
            if (page == _home)
            {
                var profile = _account.Profile;
                _homeProfile.text = profile == null ? string.Empty : $"{profile.displayName}   ·   {Loc.T("account.id")}: {ShortAccountId}";
            }
            else if (page == _support)
            {
                _supportId.text = ShortAccountId;
                _supportCopy.GetComponentInChildren<TMP_Text>().text = Loc.T("support.copy");
            }
            else if (page == _notifications)
            {
                _notifyBlocked.gameObject.SetActive(PhoneNotifications.Blocked);
            }
            else if (page == _about)
            {
                _aboutAccount.text = AccountId.Length > 0 ? Loc.F("about.account", ShortAccountId) : string.Empty;
            }
        }
    }
}
