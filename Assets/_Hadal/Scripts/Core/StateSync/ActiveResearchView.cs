namespace Hadal.Core.StateSync
{
    /// <summary>A running research as the client shows it; <see cref="EndsAt"/> is Time.realtimeSinceStartup.</summary>
    public readonly struct ActiveResearchView
    {
        public readonly string NodeId;
        public readonly int TargetLevel;
        public readonly float EndsAt;
        public readonly float TotalSeconds;

        public ActiveResearchView(string nodeId, int targetLevel, float endsAt, float totalSeconds)
        {
            NodeId = nodeId;
            TargetLevel = targetLevel;
            EndsAt = endsAt;
            TotalSeconds = totalSeconds;
        }
    }
}
