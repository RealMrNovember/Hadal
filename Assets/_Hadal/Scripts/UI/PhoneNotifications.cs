using System;
using System.Collections;
using System.Collections.Generic;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.Services.Notifications;
using Hadal.Core.StateSync;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Phone notifications (docs/36). Collects the timers the server sent (construction, research, training, healing,
    /// marches, shield, daily rewards, events) and, when the game goes to the background, hands the phone a schedule
    /// built by <see cref="LocalNotificationPlanner"/> in the player's language; back in the game it clears them (the
    /// in-game alarms take over). Keeps the player's choices in sync with the server (push respects them), asks for the
    /// Android 13 permission once at a sensible moment, opens the screen a tapped notification names, and registers a
    /// push token when a token source exists (phase 2).
    /// </summary>
    public sealed class PhoneNotifications : MonoBehaviour
    {
        /// <summary>What a tapped notification can open; wired by <see cref="BaseManagementView"/>.</summary>
        public sealed class Actions
        {
            public Action Base;
            public Action Research;
            public Action Map;
            public Action Vip;
            public Action Events;
            public Action Mail;
            public Action Alliance;
            public Action Chat;
        }

        /// <summary>The guide step after which the permission is asked (the first steps are done by then).</summary>
        public const int PermissionAfterGuideStep = 4;

        /// <summary>Never at app start: the player has been in the game at least this long when it is asked.</summary>
        public const float PermissionAfterSeconds = 90f;

        public static PhoneNotifications Instance { get; private set; }

        /// <summary>The platform side; a silent stand-in outside Android.</summary>
        internal static IPhoneNotificationBackend Backend { get; set; } = CreateBackend();

        /// <summary>Phase 2 push token source; none until Firebase is added (docs/36 §5).</summary>
        public static IPushTokenSource TokenSource { get; set; } = CreateTokenSource();

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private Actions _actions;
        private ulong _sequence;
        private int _seenPrefsVersion = -1;
        private bool _syncPending;
        private float _syncAt;
        private string _registeredToken;
        private bool _channelsReady;
        private string _scheduledSignature;
        private float _nextPermissionCheck;
        private float _startedAt;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, Actions actions)
        {
            _stateSync = stateSync;
            _commands = commands;
            _actions = actions ?? new Actions();
            Instance = this;
            _startedAt = Time.unscaledTime;
            NotificationPrefsStore.Changed += OnLocalChanged;
            if (TokenSource != null)
                TokenSource.TokenChanged += OnToken;
            Backend.Initialize();
            EnsureChannels();
            ClearPhone();
            StartCoroutine(OpenTappedNextFrame());
        }

        private void OnDestroy()
        {
            NotificationPrefsStore.Changed -= OnLocalChanged;
            if (TokenSource != null)
                TokenSource.TokenChanged -= OnToken;
            if (Instance == this)
                Instance = null;
        }

        private ClientStateView View => _stateSync?.View;

        // --- Foreground / background -----------------------------------------------------------------

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ScheduleNow();
            else
            {
                ClearPhone();
                StartCoroutine(OpenTappedNextFrame());
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            // A system dialog (the permission question) or the pulled-down shade takes the focus without a pause.
            if (!focused)
                ScheduleNow();
            else if (_scheduledSignature != null)
                ClearPhone();
        }

        private void OnApplicationQuit() => ScheduleNow();

        /// <summary>Back in the game: the scheduled notifications go (the in-game alarms take over) and so do shown ones.</summary>
        private void ClearPhone()
        {
            Backend.CancelAll();
            _scheduledSignature = null;
        }

        /// <summary>Hands the phone the current plan (idempotent: an unchanged plan is not rescheduled).</summary>
        public void ScheduleNow()
        {
            var view = View;
            if (view == null || !view.HasPersistedData)
                return;
            var now = DateTimeOffset.UtcNow;
            var plan = LocalNotificationPlanner.Plan(NotificationCandidates.Collect(view, now.ToUnixTimeMilliseconds()),
                NotificationPrefsStore.Current, now.ToUnixTimeMilliseconds(), UtcOffsetMinutes(now));
            var signature = LocalNotificationPlanner.Signature(plan, Loc.Current);
            if (signature == _scheduledSignature)
                return;

            EnsureChannels();
            Backend.CancelAll();
            foreach (var p in plan)
            {
                var (title, body) = NotificationTexts.For(p);
                Backend.Schedule(new PhoneNotification
                {
                    ChannelId = NotificationRules.ChannelId(p.Category), Title = title, Body = body,
                    FireAtLocal = DateTimeOffset.FromUnixTimeMilliseconds(p.FireAtMs).LocalDateTime, Screen = p.Screen,
                    Group = NotificationRules.ChannelId(p.Category)
                });
            }

            _scheduledSignature = signature;
        }

        private static int UtcOffsetMinutes(DateTimeOffset now) => (int)TimeZoneInfo.Local.GetUtcOffset(now.UtcDateTime).TotalMinutes;

        private void EnsureChannels()
        {
            if (_channelsReady)
                return;
            var channels = new List<(string Id, string Name, bool Important)>();
            for (var i = 0; i < NotificationRules.CategoryCount; i++)
            {
                var category = (NotificationCategory)i;
                channels.Add((NotificationRules.ChannelId(category), Loc.Plain(NotificationTexts.CategoryKey(category)),
                    category is NotificationCategory.Attack or NotificationCategory.Shield));
            }

            _channelsReady = Backend.RegisterChannels(channels);
        }

        // --- Taps --------------------------------------------------------------------------------------

        private IEnumerator OpenTappedNextFrame()
        {
            // The HUD and the social views finish building first.
            yield return null;
            yield return null;
            var screen = Backend.TakeTappedScreen();
            if (string.IsNullOrEmpty(screen))
                yield break;
            Open(screen);
        }

        public void Open(string screen)
        {
            var action = screen switch
            {
                "research" => _actions.Research,
                "map" => _actions.Map,
                "vip" => _actions.Vip,
                "events" => _actions.Events,
                "mail" => _actions.Mail,
                "alliance" => _actions.Alliance,
                "chat" => _actions.Chat,
                _ => _actions.Base
            };
            action?.Invoke();
        }

        // --- Permission, sync, token -------------------------------------------------------------------

        private void Update()
        {
            var view = View;
            if (view == null)
                return;

            if (view.NotificationPrefsVersion != _seenPrefsVersion)
            {
                _seenPrefsVersion = view.NotificationPrefsVersion;
                OnServerPrefs(view.ServerNotificationPrefs);
            }

            if (_syncPending && Time.unscaledTime >= _syncAt)
            {
                _syncPending = false;
                SendPrefs();
            }

            if (Time.unscaledTime >= _nextPermissionCheck)
            {
                _nextPermissionCheck = Time.unscaledTime + 5f;
                if (!NotificationPrefsStore.PermissionAsked && !NotificationPrefsStore.Current.Off
                    && Time.unscaledTime - _startedAt >= PermissionAfterSeconds && GuideFarEnough(view))
                    AskPermission();
            }
        }

        /// <summary>The first steps of the guide are behind the player (or the guide is over or was never there).</summary>
        private static bool GuideFarEnough(ClientStateView view)
        {
            var quests = view.Quests;
            if (quests == null)
                return false;
            return quests.Guide == null || quests.GuideNumber > PermissionAfterGuideStep;
        }

        /// <summary>Asks for the Android 13+ permission, once; older phones and other platforms need none.</summary>
        public void AskPermission()
        {
            if (NotificationPrefsStore.PermissionAsked)
                return;
            NotificationPrefsStore.PermissionAsked = true;
            Backend.RequestPermission();
        }

        /// <summary>The player switched something on in Settings: the right moment to ask, if it was never asked.</summary>
        public static void OnPlayerEnabledSomething()
        {
            if (Instance != null)
                Instance.AskPermission();
        }

        /// <summary>The phone blocks HADAL's notifications (denied, or switched off in the phone's settings).</summary>
        public static bool Blocked => Backend.Blocked;

        public static void OpenPhoneSettings() => Backend.OpenSettings();

        private void OnServerPrefs(NotificationPrefsDto server)
        {
            if (server == null)
                return;
            // A new phone or a reinstall takes the choices saved on the server; otherwise this device's choices win.
            if (server.Saved && !NotificationPrefsStore.HasLocal)
            {
                NotificationPrefsStore.Adopt(server);
                return;
            }

            if (!server.Saved || !server.SameChoices(Outgoing()) || server.UtcOffsetMinutes != Outgoing().UtcOffsetMinutes)
                RequestSync(0.5f);
        }

        private void OnLocalChanged()
        {
            _scheduledSignature = null;
            RequestSync(1.5f);
        }

        private void RequestSync(float delay)
        {
            _syncPending = true;
            _syncAt = Time.unscaledTime + delay;
        }

        /// <summary>This device's choices as the server should keep them: with the game language and the UTC offset.</summary>
        private static NotificationPrefsDto Outgoing()
        {
            var prefs = NotificationPrefsStore.Current;
            prefs.Language = Loc.Current;
            prefs.UtcOffsetMinutes = UtcOffsetMinutes(DateTimeOffset.UtcNow);
            return prefs;
        }

        private void SendPrefs()
        {
            if (_commands == null || View?.ServerNotificationPrefs == null)
                return; // an older server: nothing to keep them in
            _commands.Dispatch(new SetNotificationPrefsCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Prefs = Outgoing()
            });
            TryRegisterToken();
        }

        private void OnToken(string token)
        {
            _registeredToken = null;
            TryRegisterToken(token);
        }

        private void TryRegisterToken(string token = null)
        {
            token ??= TokenSource?.Token;
            if (string.IsNullOrEmpty(token) || token == _registeredToken || _commands == null || View?.ServerNotificationPrefs == null)
                return;
            _commands.Dispatch(new RegisterPushTokenCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, DeviceId = SystemInfo.deviceUniqueIdentifier,
                Token = token, Platform = Application.platform == RuntimePlatform.Android ? "android" : "other"
            });
            _registeredToken = token;
        }

        private static IPhoneNotificationBackend CreateBackend()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidPhoneNotificationBackend();
#else
            return new SilentPhoneNotificationBackend();
#endif
        }

        private static IPushTokenSource CreateTokenSource()
        {
#if HADAL_FCM && UNITY_ANDROID && !UNITY_EDITOR
            return new FirebasePushTokenSource();
#else
            return null;
#endif
        }
    }

    /// <summary>One notification for the phone's schedule.</summary>
    public sealed class PhoneNotification
    {
        public string ChannelId;
        public string Title;
        public string Body;
        public DateTime FireAtLocal;
        public string Screen;
        public string Group;
    }

    /// <summary>The platform's notification API (Android: Unity Mobile Notifications).</summary>
    internal interface IPhoneNotificationBackend
    {
        void Initialize();

        /// <summary>Creates the channels (Android 8+); false when it could not (tried again later).</summary>
        bool RegisterChannels(IReadOnlyList<(string Id, string Name, bool Important)> channels);

        void Schedule(PhoneNotification notification);

        /// <summary>Removes scheduled and shown notifications.</summary>
        void CancelAll();

        void RequestPermission();

        bool Blocked { get; }

        void OpenSettings();

        /// <summary>The screen of the notification that opened the app, once; null when none.</summary>
        string TakeTappedScreen();
    }

    /// <summary>Editor and non-Android: nothing reaches a phone.</summary>
    internal sealed class SilentPhoneNotificationBackend : IPhoneNotificationBackend
    {
        public readonly List<PhoneNotification> Scheduled = new();

        public void Initialize()
        {
        }

        public bool RegisterChannels(IReadOnlyList<(string Id, string Name, bool Important)> channels) => true;

        public void Schedule(PhoneNotification notification) => Scheduled.Add(notification);

        public void CancelAll() => Scheduled.Clear();

        public void RequestPermission()
        {
        }

        public bool Blocked => false;

        public void OpenSettings()
        {
        }

        public string TakeTappedScreen() => null;
    }

    /// <summary>
    /// Phase 2 (docs/36 §5): a source of this phone's Firebase Cloud Messaging token. None ships until the Firebase
    /// Messaging SDK and google-services.json are added and the HADAL_FCM scripting define is set.
    /// </summary>
    public interface IPushTokenSource
    {
        string Token { get; }

        event Action<string> TokenChanged;
    }
}
