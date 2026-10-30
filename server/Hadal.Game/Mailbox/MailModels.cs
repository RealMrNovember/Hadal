using System.Text.Json;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;

namespace Hadal.Game.Mailbox
{
    /// <summary>Who a mail comes from (docs/29 §2.1).</summary>
    public static class MailKinds
    {
        public const string System = "system";
        public const string Admin = "admin";
        public const string Event = "event";
        public const string Alliance = "alliance";

        /// <summary>From one player to another (MailRules).</summary>
        public const string Player = "player";

        /// <summary>PvP battle and scout reports (docs/30 §5.5).</summary>
        public const string Battle = "battle";

        public static bool IsValid(string kind) => kind is System or Admin or Event or Alliance or Player or Battle;
    }

    /// <summary>
    /// What a mail carries, in the admin grant's shape: resource amounts by type name and bag items by id. Stored as
    /// JSON; only positive amounts are kept.
    /// </summary>
    public sealed class MailAttachments
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public Dictionary<string, long> Amounts { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> Items { get; set; } = new(StringComparer.Ordinal);

        /// <summary>Hero cards by hero id (event rewards; a random-rarity card is drawn when the mail is made).</summary>
        public Dictionary<string, long> Shards { get; set; } = new(StringComparer.Ordinal);

        public bool IsEmpty => Amounts.All(p => p.Value <= 0) && Items.All(p => p.Value <= 0) && (Shards == null || Shards.All(p => p.Value <= 0));

        public static MailAttachments From(IReadOnlyDictionary<ResourceType, long>? amounts, IReadOnlyDictionary<string, int>? items) => new()
        {
            Amounts = (amounts ?? new Dictionary<ResourceType, long>()).Where(p => p.Value > 0 && p.Key != ResourceType.Unknown)
                .ToDictionary(p => p.Key.ToString(), p => p.Value, StringComparer.Ordinal),
            Items = (items ?? new Dictionary<string, int>()).Where(p => p.Value > 0 && p.Key.Length > 0)
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)
        };

        public string ToJson() => JsonSerializer.Serialize(this, Json);

        public static MailAttachments FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new MailAttachments();
            var parsed = JsonSerializer.Deserialize<MailAttachments>(json, Json) ?? new MailAttachments();
            parsed.Amounts ??= new Dictionary<string, long>(StringComparer.Ordinal);
            parsed.Items ??= new Dictionary<string, int>(StringComparer.Ordinal);
            parsed.Shards ??= new Dictionary<string, long>(StringComparer.Ordinal);
            return parsed;
        }
    }

    /// <summary>
    /// A mail that must go out exactly once: <paramref name="Key"/> names it for good ("ev:{event}:{player}:rank"), and a
    /// key already used sends nothing, also after a restart and after the first mail was claimed and deleted.
    /// </summary>
    public sealed record KeyedMail(string Key, string PlayerId, MailDraft Draft);

    /// <summary>
    /// The text and attachments of a mail before it is addressed; <paramref name="SenderId"/> for player mails,
    /// <paramref name="Report"/> for battle and scout mails (docs/37).
    /// </summary>
    public sealed record MailDraft(string Kind, string Sender, string Title, string Body, MailAttachments Attachments, string SenderId = "",
        PvpReportDto? Report = null);

    /// <summary>A battle mail's report as stored with the mail (JSON); empty text for none.</summary>
    public static class MailReports
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static string ToJson(PvpReportDto? report) => report == null ? string.Empty : JsonSerializer.Serialize(report, Json);

        public static PvpReportDto? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonSerializer.Deserialize<PvpReportDto>(json, Json);
            }
            catch (JsonException)
            {
                // An unreadable report leaves the mail as text (its body still says everything).
                return null;
            }
        }
    }

    /// <summary>One stored mail. Attachments stay on the server until claimed (once: <see cref="ClaimedAt"/>).</summary>
    public sealed class Mail
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>The recipient's public player id.</summary>
        public string PlayerId { get; set; } = string.Empty;
        public string Kind { get; set; } = MailKinds.System;
        public string Sender { get; set; } = string.Empty;

        /// <summary>The sending player's public id (player mails); empty for the game's own mail.</summary>
        public string SenderId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public MailAttachments Attachments { get; set; } = new();

        /// <summary>Battle and scout mails: the report the mailbox draws its card from (never changed once sent).</summary>
        public PvpReportDto? Report { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? ReadAt { get; set; }
        public DateTimeOffset? ClaimedAt { get; set; }
        public bool Deleted { get; set; }

        public bool HasAttachments => !Attachments.IsEmpty;

        public bool Expired(DateTimeOffset now) => ExpiresAt <= now;

        /// <summary>Attachments still waiting: not taken, not expired.</summary>
        public bool Claimable(DateTimeOffset now) => HasAttachments && ClaimedAt == null && !Deleted && !Expired(now);

        public Mail Copy() => new()
        {
            Id = Id, PlayerId = PlayerId, Kind = Kind, Sender = Sender, SenderId = SenderId, Title = Title, Body = Body,
            Attachments = MailAttachments.FromJson(Attachments.ToJson()), Report = Report,
            CreatedAt = CreatedAt, ExpiresAt = ExpiresAt, ReadAt = ReadAt, ClaimedAt = ClaimedAt, Deleted = Deleted
        };
    }
}
