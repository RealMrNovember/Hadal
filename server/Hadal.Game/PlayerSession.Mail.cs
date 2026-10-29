using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>What the <see cref="Mailbox.MailService"/> needs from a session: paying out claimed attachments.</summary>
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
    }
}
