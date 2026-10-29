using HADAL.Shared.DTOs;
using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Saves the player's phone notification choices on the server, so push respects them (docs/36).</summary>
    [ProtoContract]
    public sealed class SetNotificationPrefsCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public NotificationPrefsDto? Prefs { get; set; }
    }

    /// <summary>
    /// Registers this device's push token (Firebase Cloud Messaging) for the player; an empty token removes the
    /// device. <see cref="DeviceId"/> lets a refreshed token replace the old one of the same phone.
    /// </summary>
    [ProtoContract]
    public sealed class RegisterPushTokenCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string DeviceId { get; set; } = string.Empty;
        [ProtoMember(4)] public string Token { get; set; } = string.Empty;

        /// <summary>"android" (the only push platform so far).</summary>
        [ProtoMember(5)] public string Platform { get; set; } = string.Empty;
    }
}
