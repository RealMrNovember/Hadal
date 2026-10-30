using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    public sealed partial class PlayerSession
    {
        /// <summary>
        /// The team this player would field in Trench Conquest (docs/29 §4.6): the heroes asked for (or the strongest),
        /// each filled with their command of troops from home like a march, as a snapshot. Nothing leaves the base: the
        /// league fights with copies, so troops never die. Returns the reactor level too (the league has a minimum), or
        /// why no team can be made.
        /// </summary>
        internal (TrenchRules.Team? Team, int Reactor, string? Reject) TrenchTeam(IReadOnlyList<string>? heroIds)
        {
            lock (_gate)
            {
                var reactor = ReactorLevel();
                var (team, _, aboard, reason) = PickTeam(string.Empty, heroIds);
                if (reason == MarchRejectReason.HeroBusy)
                    reason = MarchRejectReason.None; // a hero out on a march still stands in the league's copy
                if (reason != MarchRejectReason.None)
                    return (null, reactor, reason.ToString());
                if (team.Count == 0 || aboard.Values.Sum() <= 0)
                    return (null, reactor, nameof(MarchRejectReason.NoTroops));

                var heroPowers = HeroPowers(team.Select(h => h.HeroId));
                return (new TrenchRules.Team
                {
                    PlayerId = PublicId,
                    Name = _displayName,
                    Power = TroopRules.Power(_catalog.Troops, aboard) + heroPowers.Sum(),
                    Troops = new Dictionary<string, long>(aboard, StringComparer.Ordinal),
                    HeroPowers = heroPowers
                }, reactor, null);
            }
        }
    }
}
