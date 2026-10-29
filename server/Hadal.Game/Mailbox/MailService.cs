using System.Threading.Channels;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Mailbox
{
    /// <summary>
    /// The mailbox (docs/29 §2.1): system, Overseer, event and alliance mails with attachments the server holds until
    /// claimed, and mails from one player to another (MailRules). A player's mails are loaded on their first login and then kept in memory, which is the authority: a
    /// claim is marked here under the lock before anything is paid, so it pays once even while its write is still
    /// queued. Lock order: mail service, then session — sessions never call in here.
    /// </summary>
    public sealed class MailService
    {
        private static readonly TimeSpan HousekeepingEvery = TimeSpan.FromSeconds(30);

        private readonly object _gate = new();
        private readonly IMailStore _store;
        private readonly INetworkSerializer _serializer;
        private readonly ILogger _logger;
        private readonly Dictionary<string, List<Mail>> _boxes = new(StringComparer.Ordinal);
        // Mails sent while the recipient's box is being loaded; merged in when it arrives.
        private readonly Dictionary<string, List<Mail>> _arriving = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerSession> _online = new(StringComparer.Ordinal);
        private readonly Channel<Func<CancellationToken, Task>> _writes = Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions { SingleReader = true });
        private DateTimeOffset _nextHousekeeping;

        public MailService(IMailStore store, INetworkSerializer serializer, ILogger logger)
        {
            _store = store;
            _serializer = serializer;
            _logger = logger;
            _ = Task.Run(WriteLoopAsync);
        }

        /// <summary>Base names (a player exists when their base does) and alliances, set by the world.</summary>
        public Func<string, string?>? NameLookup { get; set; }
        public Func<string, string>? AllianceIdOf { get; set; }

        /// <summary>A player's kingdom (docs/39): players write only within their own kingdom.</summary>
        public Func<string, string>? KingdomOf { get; set; }

        public static bool Handles(CommandType type) =>
            type is CommandType.ReadMail or CommandType.ClaimMail or CommandType.DeleteMail or CommandType.SendMail;

        public async Task InitializeAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            await _store.InitializeAsync(cancellationToken);
            var purged = await _store.PurgeAsync(now, cancellationToken);
            _logger.LogInformation("Mail: {Purged} expired or deleted mails purged.", purged);
        }

        // --- Sessions -----------------------------------------------------------------------------

        /// <summary>Loads the player's mails unless they are already in memory. Call before <see cref="OnConnected"/>.</summary>
        public async Task LoadAsync(string playerId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_boxes.ContainsKey(playerId) || _arriving.ContainsKey(playerId))
                    return;
                _arriving[playerId] = new List<Mail>();
            }

            List<Mail> loaded;
            try
            {
                loaded = await _store.LoadAsync(playerId, now, cancellationToken);
            }
            catch
            {
                lock (_gate)
                    _arriving.Remove(playerId);
                throw;
            }

            lock (_gate)
            {
                var box = loaded;
                if (_arriving.Remove(playerId, out var arrived))
                    box.AddRange(arrived.Where(a => box.All(m => m.Id != a.Id)));
                _boxes[playerId] = box;
            }
        }

        public void OnConnected(PlayerSession session, DateTimeOffset now)
        {
            lock (_gate)
            {
                _online[session.PublicId] = session;
                // The box and today's sending allowance travel in one frame.
                session.PushMailQuota(now, _boxes.TryGetValue(session.PublicId, out var box) ? Change(box, now) : null);
            }
        }

        public void OnDisconnected(PlayerSession session)
        {
            lock (_gate)
            {
                if (_online.TryGetValue(session.PublicId, out var current) && ReferenceEquals(current, session))
                    _online.Remove(session.PublicId);
            }
        }

        /// <summary>A removed player's mails leave memory (their rows expire with the rest).</summary>
        public void Forget(string playerId)
        {
            lock (_gate)
            {
                _boxes.Remove(playerId);
                _online.Remove(playerId);
            }
        }

        // --- Sending ------------------------------------------------------------------------------

        /// <summary>
        /// Sends one mail to each player: stored first, then shown at once to those online. Returns how many were sent.
        /// </summary>
        public async Task<int> SendAsync(IReadOnlyCollection<string> playerIds, MailDraft draft, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var mails = playerIds.Where(p => p.Length > 0).Distinct(StringComparer.Ordinal).Select(playerId => new Mail
            {
                Id = Guid.NewGuid().ToString("N"),
                PlayerId = playerId,
                Kind = MailKinds.IsValid(draft.Kind) ? draft.Kind : MailKinds.System,
                Sender = Clip(draft.Sender, MailRules.MaxSender),
                SenderId = draft.SenderId,
                Title = Clip(draft.Title, MailRules.MaxTitle),
                Body = Clip(draft.Body, MailRules.MaxBody),
                Attachments = draft.Attachments,
                CreatedAt = now,
                ExpiresAt = now + MailRules.Lifetime
            }).ToList();
            if (mails.Count == 0)
                return 0;

            await _store.InsertAsync(mails, cancellationToken);

            lock (_gate)
            {
                foreach (var mail in mails)
                {
                    // Each recipient gets an own copy: the attachments object must not be shared between boxes.
                    var own = mail.Copy();
                    if (_boxes.TryGetValue(own.PlayerId, out var box))
                    {
                        box.Add(own);
                        if (_online.TryGetValue(own.PlayerId, out var session))
                            session.Push(new List<EntityChangeDto> { Change(box, now) }, now);
                    }
                    else if (_arriving.TryGetValue(own.PlayerId, out var arriving))
                    {
                        arriving.Add(own);
                    }
                }
            }

            _logger.LogInformation("Mail \"{Title}\" ({Kind}) sent to {Count} players.", mails[0].Title, mails[0].Kind, mails.Count);
            return mails.Count;
        }

        // --- Commands -----------------------------------------------------------------------------

        public void Handle(PlayerSession session, CommandEnvelope envelope, DateTimeOffset now)
        {
            switch (envelope.Type)
            {
                case CommandType.ReadMail when TryDeserialize(envelope.Payload, out ReadMailCommand? read):
                    Read(session, read!, now);
                    break;
                case CommandType.ClaimMail when TryDeserialize(envelope.Payload, out ClaimMailCommand? claim):
                    Claim(session, claim!, now);
                    break;
                case CommandType.DeleteMail when TryDeserialize(envelope.Payload, out DeleteMailCommand? delete):
                    Delete(session, delete!, now);
                    break;
                case CommandType.SendMail when TryDeserialize(envelope.Payload, out SendMailCommand? send):
                    SendFromPlayer(session, send!, now);
                    break;
                default:
                    session.Acknowledge(envelope.CommandId, envelope.ClientSequence, "MalformedCommand", now);
                    break;
            }
        }

        private void Read(PlayerSession session, ReadMailCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                var box = BoxOf(session);
                var mail = box?.Find(m => m.Id == command.MailId && !m.Deleted && !m.Expired(now));
                if (box == null || mail == null)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, "UnknownMail", now);
                    return;
                }

                if (mail.ReadAt == null)
                {
                    mail.ReadAt = now;
                    Save(new[] { mail });
                }

                session.Acknowledge(command.CommandId, command.ClientSequence, null, now, new List<EntityChangeDto> { Change(box, now) });
            }
        }

        private void Claim(PlayerSession session, ClaimMailCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                var box = BoxOf(session);
                if (box == null)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, "UnknownMail", now);
                    return;
                }

                List<Mail> taken;
                if (string.IsNullOrEmpty(command.MailId))
                {
                    taken = box.Where(m => m.Claimable(now)).ToList();
                    if (taken.Count == 0)
                    {
                        session.Acknowledge(command.CommandId, command.ClientSequence, "NothingToClaim", now);
                        return;
                    }
                }
                else
                {
                    var mail = box.Find(m => m.Id == command.MailId && !m.Deleted && !m.Expired(now));
                    var reason = mail == null ? "UnknownMail" : !mail.HasAttachments ? "NothingToClaim" : mail.ClaimedAt != null ? "AlreadyClaimed" : null;
                    if (reason != null)
                    {
                        session.Acknowledge(command.CommandId, command.ClientSequence, reason, now);
                        return;
                    }

                    taken = new List<Mail> { mail! };
                }

                // Marked before paying: a second claim, however quick, finds nothing left.
                var amounts = new Dictionary<ResourceType, long>();
                var items = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var mail in taken)
                {
                    mail.ClaimedAt = now;
                    mail.ReadAt ??= now;
                    foreach (var pair in mail.Attachments.Amounts)
                    {
                        if (pair.Value > 0 && Enum.TryParse<ResourceType>(pair.Key, out var type) && type != ResourceType.Unknown)
                            amounts[type] = (amounts.TryGetValue(type, out var a) ? a : 0) + pair.Value;
                    }

                    foreach (var pair in mail.Attachments.Items.Where(p => p.Value > 0))
                        items[pair.Key] = (items.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
                }

                Save(taken);
                session.Push(new List<EntityChangeDto> { Change(box, now) }, now);
                session.GrantMail(command.CommandId, command.ClientSequence, amounts, items, now);
                _logger.LogInformation("Player {PlayerId} claimed {Count} mails.", session.PublicId, taken.Count);
            }
        }

        private void Delete(PlayerSession session, DeleteMailCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                var box = BoxOf(session);
                if (box == null)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, "UnknownMail", now);
                    return;
                }

                List<Mail> gone;
                if (string.IsNullOrEmpty(command.MailId))
                {
                    // "Delete read": every opened mail with nothing left to take.
                    gone = box.Where(m => m.ReadAt != null && !m.Claimable(now)).ToList();
                }
                else
                {
                    var mail = box.Find(m => m.Id == command.MailId);
                    var reason = mail == null || mail.Deleted ? "UnknownMail" : mail.Claimable(now) ? "MailHasAttachments" : null;
                    if (reason != null)
                    {
                        session.Acknowledge(command.CommandId, command.ClientSequence, reason, now);
                        return;
                    }

                    gone = new List<Mail> { mail! };
                }

                foreach (var mail in gone)
                {
                    mail.Deleted = true;
                    box.Remove(mail);
                }

                Save(gone);
                session.Acknowledge(command.CommandId, command.ClientSequence, null, now, new List<EntityChangeDto> { Change(box, now) });
            }
        }

        /// <summary>
        /// A player's mail to one other player. Checked here (text, recipient, alliance), paid by the session (daily
        /// allowance, wallet), then delivered; resources travel as the mail's attachments. Should storing fail, the
        /// payment is given back.
        /// </summary>
        private void SendFromPlayer(PlayerSession session, SendMailCommand command, DateTimeOffset now)
        {
            var to = command.PlayerId ?? string.Empty;
            var title = MailRules.Clean(command.Title, int.MaxValue, multiline: false);
            var body = MailRules.Clean(command.Body, int.MaxValue, multiline: true);
            var amounts = new Dictionary<ResourceType, long>();
            string? reason = null;
            foreach (var entry in command.Resources ?? Array.Empty<ResourceAmountEntry>())
            {
                if (entry.Value <= 0)
                    continue;
                if (!MailRules.CanSend(entry.Key))
                {
                    reason = "ResourceNotSendable";
                    break;
                }

                // Saturating: two huge entries of one resource must not wrap round into a negative payment.
                amounts[entry.Key] = WalletOps.Sum(amounts.TryGetValue(entry.Key, out var a) ? a : 0, entry.Value);
            }

            reason ??= title.Length == 0 || title.Length > MailRules.MaxTitle ? "InvalidMailTitle"
                : body.Length > MailRules.MaxPlayerBody ? "InvalidMailBody"
                : to == session.PublicId ? "CannotMailSelf"
                : NameLookup?.Invoke(to) == null ? "UnknownPlayer"
                : KingdomOf != null && KingdomOf(to) != KingdomOf(session.PublicId) ? "OtherKingdom"
                : null;
            if (reason == null && amounts.Count > 0)
            {
                // Resources only between mates of one alliance (the genre's resource help).
                var mine = AllianceIdOf?.Invoke(session.PublicId) ?? string.Empty;
                if (mine.Length == 0 || mine != (AllianceIdOf?.Invoke(to) ?? string.Empty))
                    reason = "NotSameAlliance";
            }

            reason ??= session.PayMail(amounts, now);
            if (reason != null)
            {
                session.Acknowledge(command.CommandId, command.ClientSequence, reason, now);
                return;
            }

            var sender = NameLookup?.Invoke(session.PublicId) ?? session.DisplayName;
            var draft = new MailDraft(MailKinds.Player, sender, title, body, MailAttachments.From(amounts, null), session.PublicId);
            _ = DeliverAsync(session, command, to, draft, amounts, now);
        }

        private async Task DeliverAsync(PlayerSession session, SendMailCommand command, string to, MailDraft draft,
            Dictionary<ResourceType, long> amounts, DateTimeOffset now)
        {
            try
            {
                await SendAsync(new[] { to }, draft, now, CancellationToken.None);
                session.Acknowledge(command.CommandId, command.ClientSequence, null, now);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Player {PlayerId} could not mail {To}; payment returned.", session.PublicId, to);
                session.RefundMail(amounts, now);
                session.Acknowledge(command.CommandId, command.ClientSequence, "MailFailed", now);
            }
        }

        // --- Housekeeping -------------------------------------------------------------------------

        /// <summary>Every tick: a few times a minute, expired mails leave the boxes (their attachments with them).</summary>
        public void Tick(DateTimeOffset now)
        {
            if (now < _nextHousekeeping)
                return;
            _nextHousekeeping = now + HousekeepingEvery;
            lock (_gate)
            {
                foreach (var (playerId, box) in _boxes)
                {
                    if (box.RemoveAll(m => m.Expired(now)) > 0 && _online.TryGetValue(playerId, out var session))
                        session.Push(new List<EntityChangeDto> { Change(box, now) }, now);
                }
            }
        }

        /// <summary>Waits until every queued write is stored (shutdown, tests).</summary>
        public async Task DrainAsync()
        {
            var done = new TaskCompletionSource();
            Write(_ =>
            {
                done.SetResult();
                return Task.CompletedTask;
            });
            await done.Task;
        }

        /// <summary>The player's live mails, newest first (tests and the Overseer).</summary>
        public List<Mail> MailsOf(string playerId, DateTimeOffset now)
        {
            lock (_gate)
                return _boxes.TryGetValue(playerId, out var box)
                    ? box.Where(m => !m.Deleted && !m.Expired(now)).OrderByDescending(m => m.CreatedAt).Select(m => m.Copy()).ToList()
                    : new List<Mail>();
        }

        // --- Helpers ------------------------------------------------------------------------------

        private List<Mail>? BoxOf(PlayerSession session) => _boxes.TryGetValue(session.PublicId, out var box) ? box : null;

        private EntityChangeDto Change(List<Mail> box, DateTimeOffset now)
        {
            var live = box.Where(m => !m.Deleted && !m.Expired(now)).ToList();
            return new EntityChangeDto
            {
                EntityId = EntityIds.Mail,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new MailListDto
                {
                    Mails = live.OrderByDescending(m => m.CreatedAt).Take(MailRules.MaxShown).Select(m => Dto(m, now)).ToArray(),
                    Unread = live.Count(m => m.ReadAt == null),
                    Unclaimed = live.Count(m => m.Claimable(now))
                })
            };
        }

        private static MailDto Dto(Mail m, DateTimeOffset now) => new()
        {
            Id = m.Id,
            Kind = m.Kind,
            Sender = m.Sender,
            Title = m.Title,
            Body = m.Body,
            SentAtMs = m.CreatedAt.ToUnixTimeMilliseconds(),
            ExpiresAtMs = m.ExpiresAt.ToUnixTimeMilliseconds(),
            Read = m.ReadAt != null,
            Claimed = m.HasAttachments && m.ClaimedAt != null,
            SenderId = m.SenderId,
            Attachments = m.Attachments.Amounts.Where(p => p.Value > 0).Select(p => new RewardEntryDto { Type = "resource", Id = p.Key, Amount = p.Value })
                .Concat(m.Attachments.Items.Where(p => p.Value > 0).Select(p => new RewardEntryDto { Type = "item", Id = p.Key, Amount = p.Value }))
                .ToArray()
        };

        /// <summary>Queues the marks of these mails for the store (copies, so later changes do not race the write).</summary>
        private void Save(IEnumerable<Mail> mails)
        {
            var copies = mails.Select(m => m.Copy()).ToList();
            if (copies.Count > 0)
                Write(ct => _store.UpdateAsync(copies, ct));
        }

        private void Write(Func<CancellationToken, Task> write) => _writes.Writer.TryWrite(write);

        private async Task WriteLoopAsync()
        {
            await foreach (var write in _writes.Reader.ReadAllAsync())
            {
                try
                {
                    await write(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Mail store write failed.");
                }
            }
        }

        private bool TryDeserialize<T>(byte[] payload, out T? value) where T : class
        {
            value = null;
            return payload is { Length: > 0 } && _serializer.TryDeserialize(payload, out value) && value != null;
        }

        private static string Clip(string? text, int max)
        {
            var trimmed = (text ?? string.Empty).Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }
    }
}
