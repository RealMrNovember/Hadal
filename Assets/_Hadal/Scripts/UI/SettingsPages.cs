using System;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services;
using Hadal.Core.Services.Notifications;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>The Settings home page and its sub-pages; account pages live in AccountPanelView.cs.</summary>
    public sealed partial class AccountPanelView
    {
        public const string WebsiteUrl = "https://hadal.cicibyte.com/";

        private RectTransform _home;
        private RectTransform _language;
        private RectTransform _sound;
        private RectTransform _graphics;
        private RectTransform _notifications;
        private RectTransform _support;
        private RectTransform _about;

        private Text _homeProfile;
        private Text _supportId;
        private Button _supportCopy;
        private Text _aboutAccount;

        private void BuildSettings(Transform card)
        {
            BuildHome(_home = Page("Home", card, "settings.title"));
            BuildLanguage(_language = Page("Language", card, "settings.language"));
            BuildSound(_sound = Page("Sound", card, "settings.sound"));
            BuildGraphics(_graphics = Page("Graphics", card, "settings.graphics"));
            BuildNotifications(_notifications = Page("Notifications", card, "settings.notifications"));
            BuildSupport(_support = Page("Support", card, "settings.support"));
            BuildAbout(_about = Page("About", card, "settings.about"));
        }

        private void BuildHome(Transform column)
        {
            _homeProfile = UiKit.Label("Profile", column, string.Empty, 34, UiKit.Muted).Row(60);

            var tiles = new (string Name, string Key, Func<RectTransform> Target)[]
            {
                ("Account", "settings.account", () => _main),
                ("Language", "settings.language", () => _language),
                ("Sound", "settings.sound", () => _sound),
                ("Graphics", "settings.graphics", () => _graphics),
                ("Notifications", "settings.notifications", () => _notifications),
                ("Gift", "settings.gift", () => _gift),
                ("Support", "settings.support", () => _support),
                ("About", "settings.about", () => _about)
            };

            for (var i = 0; i < tiles.Length; i += 2)
            {
                var row = UiKit.Rect("Row" + i / 2, column).Row(150);
                for (var j = 0; j < 2 && i + j < tiles.Length; j++)
                {
                    var tile = tiles[i + j];
                    var label = tile.Key == "settings.language" ? $"{Loc.T(tile.Key)}\n<size=28>{Loc.DisplayName(Loc.Current)}</size>" : Loc.T(tile.Key);
                    var button = UiKit.Button(tile.Name, row, label, () => Show(tile.Target()), UiKit.Field, 38);
                    button.GetComponentInChildren<Text>().color = tile.Key == "settings.gift" ? UiKit.Accent : UiKit.Text;
                    ((RectTransform)button.transform).Place(j * 0.5f, 0, j * 0.5f + 0.5f, 1, 8);
                }
            }

            UiKit.Rect("Spacer", column).Row(40);
            Back(column, Close, "account.close");
        }

        private void BuildLanguage(Transform column)
        {
            UiKit.Label("Hint", column, Loc.T("language.hint"), 34, UiKit.Muted).Row(70);
            var list = UiKit.ScrollList("List", column, 12f);
            list.parent.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            foreach (var (code, name) in Loc.Languages)
            {
                var selected = code == Loc.Current;
                var button = UiKit.Button(code, list, name, () => ChooseLanguage(code), selected ? UiKit.Accent : UiKit.Field, 38).Row(100);
                var text = button.GetComponentInChildren<Text>();
                text.color = selected ? UiKit.Abyss : UiKit.Text;
                text.font = UiKit.FontFor(code);
                text.text = Loc.DisplayName(code);
            }

            Back(column, () => Show(_home));
        }

        private static void ChooseLanguage(string code)
        {
            if (code == Loc.Current)
                return;

            Loc.SetLanguage(code);
            // Every screen is built with its strings, so a restart is the clean way to switch.
            SessionRestart.Restart();
        }

        private void BuildSound(Transform column)
        {
            UiKit.Switch("Music", column, Loc.T("sound.music"), () => GameSettings.MusicOn, v => GameSettings.MusicOn = v).Row(110);
            UiKit.Slider("MusicVolume", column, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v).Row(80);
            UiKit.Switch("Effects", column, Loc.T("sound.effects"), () => GameSettings.SoundOn, v => GameSettings.SoundOn = v).Row(110);
            UiKit.Slider("EffectsVolume", column, GameSettings.SoundVolume, v =>
            {
                GameSettings.SoundVolume = v;
                GameAudio.Play(Sfx.UiTap);
            }).Row(80);
            UiKit.Switch("Vibration", column, Loc.T("sound.vibration"), () => GameSettings.Vibration, v =>
            {
                GameSettings.Vibration = v;
#if UNITY_ANDROID || UNITY_IOS
                if (v)
                    Handheld.Vibrate();
#endif
            }).Row(110);
            UiKit.Rect("Spacer", column).Row(40);
            Back(column, () => Show(_home));
        }

        private void BuildGraphics(Transform column)
        {
            UiKit.Label("QualityLabel", column, Loc.T("graphics.quality"), 34, UiKit.Muted).Row(50);
            UiKit.Choice("Quality", column, new[] { Loc.T("graphics.low"), Loc.T("graphics.medium"), Loc.T("graphics.high") },
                () => (int)GameSettings.Quality, i => GameSettings.Quality = (GraphicsQuality)i).Row(110);
            UiKit.Label("FpsLabel", column, Loc.T("graphics.fps"), 34, UiKit.Muted).Row(50);
            UiKit.Choice("Fps", column, new[] { "30 FPS", "60 FPS" },
                () => GameSettings.FrameRate >= 60 ? 1 : 0, i => GameSettings.FrameRate = i == 1 ? 60 : 30).Row(110);
            UiKit.Switch("PowerSaving", column, Loc.T("graphics.power"), () => GameSettings.PowerSaving, v => GameSettings.PowerSaving = v).Row(110);
            UiKit.Label("PowerHint", column, Loc.T("graphics.power.hint"), 30, UiKit.Muted).Row(80);
            UiKit.Rect("Spacer", column).Row(20);
            Back(column, () => Show(_home));
        }

        /// <summary>
        /// Phone notifications (docs/36): the master switch, the timer categories the phone schedules, the alerts the
        /// server will push (shown as "soon" until push is live) and quiet hours. Choices are kept on this device and
        /// on the server (PhoneNotifications syncs them).
        /// </summary>
        private void BuildNotifications(Transform column)
        {
            UiKit.Label("Hint", column, Loc.T("notify.hint"), 30, UiKit.Muted).Row(60);
            _notifyBlocked = UiKit.Button("Blocked", column, Loc.T("notify.permission.open"), PhoneNotifications.OpenPhoneSettings, UiKit.Danger, 30);
            _notifyBlocked.Row(90);
            var list = UiKit.ScrollList("List", column, 10f);
            list.parent.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            UiKit.Switch("Master", list, Loc.T("notify.master"), () => !NotificationPrefsStore.Current.Off, on =>
            {
                var prefs = NotificationPrefsStore.Current;
                prefs.Off = !on;
                NotificationPrefsStore.Save(prefs);
                if (on)
                    PhoneNotifications.OnPlayerEnabledSomething();
            }).Row(110);

            for (var i = 0; i < NotificationRules.CategoryCount; i++)
            {
                var category = (NotificationCategory)i;
                if (category == NotificationCategory.Attack)
                    UiKit.Label("PushHeader", list, Loc.T("notify.section.push"), 30, UiKit.Muted).Row(60);
                var row = UiKit.Switch(category.ToString(), list, Loc.T(NotificationTexts.CategoryKey(category)),
                    () => NotificationPrefsStore.IsEnabled(category), on =>
                    {
                        NotificationPrefsStore.SetEnabled(category, on);
                        if (on)
                            PhoneNotifications.OnPlayerEnabledSomething();
                    });
                row.Row(100);
                if (NotificationRules.IsPush(category))
                    Soon(row);
            }

            UiKit.Switch("Quiet", list, Loc.T("notify.quiet"), () => NotificationPrefsStore.Current.QuietHours, on =>
            {
                var prefs = NotificationPrefsStore.Current;
                prefs.QuietHours = on;
                NotificationPrefsStore.Save(prefs);
            }).Row(110);
            UiKit.Label("QuietFrom", list, Loc.T("notify.quiet.from"), 28, UiKit.Muted).Row(44);
            QuietChoice("From", list, QuietStarts, p => p.QuietStartMinute, (p, m) => p.QuietStartMinute = m);
            UiKit.Label("QuietTo", list, Loc.T("notify.quiet.to"), 28, UiKit.Muted).Row(44);
            QuietChoice("To", list, QuietEnds, p => p.QuietEndMinute, (p, m) => p.QuietEndMinute = m);

            UiKit.Rect("Spacer", column).Row(10);
            Back(column, () => Show(_home));
        }

        private static readonly int[] QuietStarts = { 21 * 60, 22 * 60, 23 * 60, 0 };
        private static readonly int[] QuietEnds = { 6 * 60, 7 * 60, 8 * 60, 9 * 60 };

        private Button _notifyBlocked;

        private static void QuietChoice(string name, Transform list, int[] minutes, Func<NotificationPrefsDto, int> get,
            Action<NotificationPrefsDto, int> set)
        {
            var labels = Array.ConvertAll(minutes, m => $"{m / 60:00}:{m % 60:00}");
            UiKit.Choice(name, list, labels, () => Math.Max(0, Array.IndexOf(minutes, get(NotificationPrefsStore.Current))), i =>
            {
                var prefs = NotificationPrefsStore.Current;
                set(prefs, minutes[i]);
                NotificationPrefsStore.Save(prefs);
            }).Row(96);
        }

        /// <summary>A push category before phase 2: greyed out, "soon" instead of on/off.</summary>
        private static void Soon(Button row)
        {
            row.interactable = false;
            row.transform.Find("Label").GetComponent<Text>().color = UiKit.Muted;
            row.transform.Find("Pill").GetComponent<Image>().color = new Color(0.12f, 0.24f, 0.3f, 1f);
            var state = row.transform.Find("Pill/State").GetComponent<Text>();
            state.text = Loc.T("notify.soon");
            state.color = UiKit.Muted;
        }

        private void BuildSupport(Transform column)
        {
            UiKit.Label("Hint", column, Loc.T("support.hint"), 34, UiKit.Muted).Row(140);
            _supportId = UiKit.Label("AccountId", column, string.Empty, 40, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold).Row(90);
            _supportCopy = UiKit.Button("Copy", column, Loc.T("support.copy"), CopyAccountId, UiKit.Field);
            _supportCopy.Row(110).GetComponentInChildren<Text>().color = UiKit.Text;
            UiKit.Button("Website", column, Loc.T("support.website"), () => Application.OpenURL(WebsiteUrl)).Row(110);
            UiKit.Rect("Spacer", column).Row(40);
            Back(column, () => Show(_home));
        }

        private void BuildAbout(Transform column)
        {
            UiKit.Label("Logo", column, "HADAL", 120, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).Row(180);
            UiKit.Label("Tagline", column, Loc.T("title.tagline"), 32, UiKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic).Row(90);
            UiKit.Label("Version", column, Loc.F("about.version", Application.version), 34, UiKit.Text, TextAnchor.MiddleCenter).Row(60);
            _aboutAccount = UiKit.Label("Account", column, string.Empty, 30, UiKit.Muted, TextAnchor.MiddleCenter).Row(50);
            UiKit.Label("Studio", column, Loc.T("about.studio"), 30, UiKit.Muted, TextAnchor.MiddleCenter).Row(50);
            UiKit.Label("Site", column, "hadal.cicibyte.com", 30, UiKit.Accent, TextAnchor.MiddleCenter).Row(50);
            UiKit.Rect("Spacer", column).Row(40);
            Back(column, () => Show(_home));
        }

        private static void Back(Transform column, UnityEngine.Events.UnityAction action, string key = "account.back") =>
            UiKit.Button("Back", column, Loc.T(key), action, UiKit.Field).Row(100).GetComponentInChildren<Text>().color = UiKit.Text;

        private string AccountId => _account.Profile?.accountId ?? string.Empty;

        private string ShortAccountId => AccountId.Substring(0, Math.Min(8, AccountId.Length)).ToUpperInvariant();

        private void CopyAccountId()
        {
            GUIUtility.systemCopyBuffer = AccountId;
            _supportCopy.GetComponentInChildren<Text>().text = Loc.T("support.copied");
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
                _supportCopy.GetComponentInChildren<Text>().text = Loc.T("support.copy");
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
