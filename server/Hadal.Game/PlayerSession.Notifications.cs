using Hadal.Game.PushNotifications;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>Phone notifications (docs/36): the player's choices and push devices, which push delivery reads.</summary>
    public sealed partial class PlayerSession
    {
        private NotificationPrefsDto NotifyPrefsDto()
        {
            var prefs = _state.NotifyPrefs?.Copy() ?? new NotificationPrefsDto();
            prefs.Saved = _state.NotifyPrefs != null;
            return prefs;
        }

        private EntityChangeDto NotificationPrefsChange() => new()
        {
            EntityId = EntityIds.NotificationPrefs,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(NotifyPrefsDto())
        };

        private void SetNotificationPrefs(SetNotificationPrefsCommand command, DateTimeOffset now)
        {
            if (command.Prefs == null)
            {
                Reject(command.CommandId, command.ClientSequence, "MalformedCommand", now);
                return;
            }

            var prefs = NotificationRules.Sanitize(command.Prefs);
            prefs.Saved = false;
            _state.NotifyPrefs = prefs;
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { NotificationPrefsChange(), Accepted(command.CommandId, command.ClientSequence) });
        }

        private void RegisterPushToken(RegisterPushTokenCommand command, DateTimeOffset now)
        {
            var device = (command.DeviceId ?? string.Empty).Trim();
            var token = (command.Token ?? string.Empty).Trim();
            if (device.Length == 0 || device.Length > NotificationRules.MaxDeviceIdLength || token.Length > NotificationRules.MaxTokenLength)
            {
                Reject(command.CommandId, command.ClientSequence, "MalformedCommand", now);
                return;
            }

            _state.PushDevices.RemoveAll(d => d.DeviceId == device || (token.Length > 0 && d.Token == token));
            if (token.Length > 0)
            {
                _state.PushDevices.Add(new PushDevice
                {
                    DeviceId = device, Token = token, Platform = (command.Platform ?? string.Empty).Trim(), UpdatedAt = now
                });
                while (_state.PushDevices.Count > NotificationRules.MaxDevices)
                    _state.PushDevices.RemoveAt(0);
            }

            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { Accepted(command.CommandId, command.ClientSequence) });
        }

        /// <summary>What push delivery needs: the choices and the phones (a copy taken under the session lock).</summary>
        internal PushProfile TakePushProfile()
        {
            lock (_gate)
                return PushProfile.From(_state);
        }

        /// <summary>Drops a token Firebase no longer knows (the app was uninstalled or the token rotated).</summary>
        internal void ForgetPushToken(string token)
        {
            lock (_gate)
            {
                if (_state.PushDevices.RemoveAll(d => d.Token == token) > 0)
                    _state.IsDirty = true;
            }
        }
    }
}
