using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// What the <see cref="Events.EventService"/> needs from a session (docs/29). Actions are queued here under the
    /// session lock and taken by the world each tick, so the session never calls into the event service.
    /// </summary>
    public sealed partial class PlayerSession
    {
        private readonly List<(string Action, long Amount)> _eventScores = new();

        /// <summary>Queues an action for events. Caller holds the lock.</summary>
        private void Score(string action, long amount)
        {
            if (amount > 0)
                _eventScores.Add((action, amount));
        }

        /// <summary>The actions queued since the last call.</summary>
        internal List<(string Action, long Amount)> TakeEventScores()
        {
            lock (_gate)
            {
                if (_eventScores.Count == 0)
                    return new List<(string, long)>();
                var taken = new List<(string, long)>(_eventScores);
                _eventScores.Clear();
                return taken;
            }
        }

        /// <summary>Pays an event reward (bag capsules for resources) and answers the claim with a reward report.</summary>
        internal void GrantEventRewards(string commandId, ulong sequence, IEnumerable<RewardData> rewards, DateTimeOffset now)
        {
            lock (_gate)
                GrantAndReport(commandId, sequence, QuestRules.Resolve(_catalog, rewards, Random.Shared.Next), now);
        }

        internal void ScoreForTests(string action, long amount)
        {
            lock (_gate)
                Score(action, amount);
        }
    }
}
