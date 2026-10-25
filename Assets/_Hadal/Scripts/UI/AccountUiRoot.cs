using Hadal.Core.Events;
using Hadal.Core.Network;
using UnityEngine;
using VContainer;

namespace Hadal.UI
{
    /// <summary>
    /// Scene entry for the title screen and the account UI. VContainer injects this one object
    /// (autoInjectGameObjects); it builds the runtime UI and relays connection events to it.
    /// </summary>
    public sealed class AccountUiRoot : MonoBehaviour
    {
        private IAccountService _account;
        private ILocalEventBus _localBus;
        private TitleScreenView _title;
        private AccountPanelView _panel;
        private bool _subscribed;
        private bool _profileRequested;

        public TitleScreenView Title => _title;
        public AccountPanelView Panel => _panel;

        [Inject]
        public void Construct(IAccountService account, ILocalEventBus localBus)
        {
            _account = account;
            _localBus = localBus;

            // Scene objects are enabled before VContainer injects them (see ResourceHudView).
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            if (!_subscribed || _localBus == null)
                return;

            _localBus.Unsubscribe<NetworkReconnectingEvent>(OnReconnecting);
            _localBus.Unsubscribe<ServerMaintenanceEvent>(OnMaintenance);
            _localBus.Unsubscribe<ClientUpdateRequiredEvent>(OnUpdateRequired);
            _localBus.Unsubscribe<AccountBannedEvent>(OnBanned);
            _localBus.Unsubscribe<ServerAnnouncementEvent>(OnAnnouncement);
            _localBus.Unsubscribe<ResourceStateChangedEvent>(OnWorldState);
            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_localBus == null || _subscribed)
                return;

            if (_title == null)
            {
                _title = TitleScreenView.Create(transform);
                _panel = AccountPanelView.Create(transform, _account);
            }

            _localBus.Subscribe<NetworkReconnectingEvent>(OnReconnecting);
            _localBus.Subscribe<ServerMaintenanceEvent>(OnMaintenance);
            _localBus.Subscribe<ClientUpdateRequiredEvent>(OnUpdateRequired);
            _localBus.Subscribe<AccountBannedEvent>(OnBanned);
            _localBus.Subscribe<ServerAnnouncementEvent>(OnAnnouncement);
            _localBus.Subscribe<ResourceStateChangedEvent>(OnWorldState);
            _subscribed = true;
        }

        private void OnReconnecting(NetworkReconnectingEvent _) => _title.SetStatus(TitleScreenView.Status.Reconnecting);

        private void OnMaintenance(ServerMaintenanceEvent maintenance) => _title.SetStatus(TitleScreenView.Status.Maintenance, maintenance.Message);

        private void OnUpdateRequired(ClientUpdateRequiredEvent _) => _title.SetStatus(TitleScreenView.Status.UpdateRequired);

        private void OnBanned(AccountBannedEvent _) => _title.SetStatus(TitleScreenView.Status.Banned);

        private void OnAnnouncement(ServerAnnouncementEvent announcement) => _title.SetAnnouncement(announcement.Text);

        /// <summary>The first snapshot means the base is on screen: the title can be dismissed.</summary>
        private void OnWorldState(ResourceStateChangedEvent state)
        {
            _title.SetStatus(TitleScreenView.Status.Ready);
            if (_profileRequested)
                return;

            _profileRequested = true;
            _ = _account.RefreshProfileAsync();
        }
    }
}
