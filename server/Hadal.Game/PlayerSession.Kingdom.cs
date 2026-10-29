using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    public sealed partial class PlayerSession
    {
        /// <summary>The player's kingdom (docs/39), set when the session loads; its map, chat and alliances are theirs.</summary>
        public string KingdomId { get; internal set; } = AllianceLandRules.DefaultWorld;
    }
}
