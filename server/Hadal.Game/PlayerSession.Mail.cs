using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// What the <see cref="Mailbox.MailService"/> needs from a session: paying out claimed attachments, and the daily
    /// mail allowance (MailRules) with the wallet for mails the player sends.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>
        /// Pays mail attachments and answers the claim with a reward report. Items go to the bag; resources a capsule can
        /// hold arrive as capsules, the rest straight to the wallet past the cap, so nothing sent is lost.
        /// </summary>
        internal void GrantMail(string commandId, ulong sequence, IReadOnlyDictionary<ResourceType, long> amounts,
            IReadOnlyDictionary<string, int> items, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                ApplyGrant(_state, amounts, asPackages: true, items);
                _walletDirty = true;
                var rewards = items.Select(p => new ResolvedReward("item", p.Key, p.Value))
                    .Concat(amounts.Where(p => p.Value > 0).Select(p => Capsules.Holds(p.Key)
                        ? new ResolvedReward("item", Capsules.Id(p.Key, p.Value), 1)
                        : new ResolvedReward("resource", p.Key.ToString(), p.Value)))
                    .ToList();
                GrantAndReport(commandId, sequence, rewards, now, grant: false);
            }
        }

        /// <summary>
        /// Takes one mail off today's allowance and its resources off the wallet. Null when paid, else the reason
        /// ("MailLimitReached", "MailResourceLimit", "InsufficientResources").
        /// </summary>
        internal string? PayMail(IReadOnlyDictionary<ResourceType, long> amounts, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                RollMailDay(now);
                if (_state.MailsSent >= MailRules.MailsPerDay)
                    return "MailLimitReached";
                var reactor = ReactorLevel();
                foreach (var pair in amounts)
                {
                    // A mail never pays the sender: only positive amounts, and no sum may wrap round.
                    if (pair.Value <= 0)
                        return "MalformedCommand";
                    if (WalletOps.Sum(MailSentToday(pair.Key), pair.Value) > MailRules.DailyAllowance(pair.Key, reactor))
                        return "MailResourceLimit";
                    if ((_state.Wallet.TryGetValue(pair.Key, out var have) ? have : 0) < pair.Value)
                        return "InsufficientResources";
                }

                foreach (var pair in amounts)
                {
                    _state.Wallet[pair.Key] -= pair.Value;
                    _state.MailSent[pair.Key] = MailSentToday(pair.Key) + pair.Value;
                }

                _state.MailsSent++;
                _state.IsDirty = true;
                var changes = new List<EntityChangeDto> { MailQuotaChange(now) };
                if (amounts.Count > 0)
                    changes.Add(WalletChange());
                SendDelta(now, changes);
                return null;
            }
        }

        /// <summary>Gives back a mail's payment when it could not be delivered.</summary>
        internal void RefundMail(IReadOnlyDictionary<ResourceType, long> amounts, DateTimeOffset now)
        {
            lock (_gate)
            {
                foreach (var pair in amounts)
                {
                    WalletOps.Credit(_state.Wallet, pair.Key, pair.Value);
                    if (_state.MailDay == MailRules.Day(now))
                        _state.MailSent[pair.Key] = Math.Max(0, MailSentToday(pair.Key) - pair.Value);
                }

                if (_state.MailDay == MailRules.Day(now))
                    _state.MailsSent = Math.Max(0, _state.MailsSent - 1);
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { MailQuotaChange(now), WalletChange() });
            }
        }

        /// <summary>Sends what may still be mailed today (on login), with <paramref name="with"/> in the same frame.</summary>
        internal void PushMailQuota(DateTimeOffset now, EntityChangeDto? with = null)
        {
            lock (_gate)
            {
                var changes = new List<EntityChangeDto> { MailQuotaChange(now) };
                if (with != null)
                    changes.Insert(0, with);
                SendDelta(now, changes);
            }
        }

        /// <summary>A new UTC day opens the allowance again. Caller holds the lock.</summary>
        private void RollMailDay(DateTimeOffset now)
        {
            var day = MailRules.Day(now);
            if (_state.MailDay == day)
                return;
            _state.MailDay = day;
            _state.MailsSent = 0;
            _state.MailSent.Clear();
            _state.IsDirty = true;
        }

        private long MailSentToday(ResourceType resource) => _state.MailSent.TryGetValue(resource, out var sent) ? sent : 0;

        private EntityChangeDto MailQuotaChange(DateTimeOffset now)
        {
            RollMailDay(now);
            var reactor = ReactorLevel();
            return new EntityChangeDto
            {
                EntityId = EntityIds.MailQuota,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new MailQuotaDto
                {
                    Day = _state.MailDay,
                    MailsLeft = Math.Max(0, MailRules.MailsPerDay - _state.MailsSent),
                    Left = MailRules.Sendable.Select(r => new ResourceAmountEntry
                    {
                        Key = r, Value = Math.Max(0, MailRules.DailyAllowance(r, reactor) - MailSentToday(r))
                    }).ToArray()
                })
            };
        }
    }
}
