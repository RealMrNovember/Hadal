using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game.PushNotifications
{
    /// <summary>What the server pushes to a phone while the game is closed (docs/36 phase 2).</summary>
    public enum PushKind
    {
        AttackIncoming,
        AttackResult,
        Mail,
        AllianceHelp,
        RallyStarted,
        ChatMention
    }

    public static class PushKinds
    {
        public static NotificationCategory Category(PushKind kind) => kind switch
        {
            PushKind.AttackIncoming or PushKind.AttackResult => NotificationCategory.Attack,
            PushKind.Mail => NotificationCategory.Mail,
            PushKind.AllianceHelp or PushKind.RallyStarted => NotificationCategory.Alliance,
            _ => NotificationCategory.Chat
        };

        /// <summary>The screen a tap opens (the client's deep link).</summary>
        public static string Screen(PushKind kind) => kind switch
        {
            PushKind.AttackIncoming => "base",
            PushKind.AttackResult or PushKind.Mail => "mail",
            PushKind.AllianceHelp or PushKind.RallyStarted => "alliance",
            _ => "chat"
        };
    }

    /// <summary>One phone notification on its way to one device.</summary>
    public sealed record PushMessage(string Token, string Title, string Body, string ChannelId, string Screen, string Tag);

    public enum PushSendResult
    {
        Sent,

        /// <summary>The token is dead (app uninstalled, token rotated): forget it.</summary>
        InvalidToken,

        /// <summary>Anything else (network, quota); the push is dropped, not retried.</summary>
        Failed
    }

    /// <summary>The transport (Firebase Cloud Messaging in production, nothing by default).</summary>
    public interface IPushSender
    {
        /// <summary>False for the no-op sender: nothing is queued or looked up.</summary>
        bool Enabled { get; }

        Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken);
    }

    /// <summary>The default: push is off until Firebase credentials are configured.</summary>
    public sealed class NoopPushSender : IPushSender
    {
        public static readonly NoopPushSender Instance = new();

        public bool Enabled => false;

        public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(PushSendResult.Sent);
    }

    /// <summary>A player's push choices and phones, copied out of their state.</summary>
    public sealed class PushProfile
    {
        public NotificationPrefsDto? Prefs { get; init; }
        public IReadOnlyList<string> Tokens { get; init; } = Array.Empty<string>();

        public static PushProfile From(PlayerState state) => new()
        {
            Prefs = state.NotifyPrefs?.Copy(),
            Tokens = state.PushDevices.Where(d => d.Token.Length > 0).Select(d => d.Token).Distinct(StringComparer.Ordinal).ToList()
        };
    }
}
