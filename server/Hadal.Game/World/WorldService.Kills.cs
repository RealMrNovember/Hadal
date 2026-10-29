using HADAL.Shared.Simulation;

namespace Hadal.Game.World
{
    /// <summary>PvP battles count for the alliances' kill rankings (docs/31 step 7).</summary>
    public sealed partial class WorldService
    {
        /// <summary>Each side is credited with the troops it defeated. Caller holds the world lock.</summary>
        private void CreditKills(PvpRecord r, BattleResult result, DateTimeOffset now)
        {
            _social.AddKills(r.AttackerId, result.DefenderLost.Values.Sum(), now);
            if (r.TargetOwnerId.Length > 0)
                _social.AddKills(r.TargetOwnerId, result.AttackerLost.Values.Sum(), now);
        }
    }
}
