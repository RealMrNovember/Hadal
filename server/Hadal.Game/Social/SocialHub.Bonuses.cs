namespace Hadal.Game.Social
{
    /// <summary>Puts the alliance's research bonuses on a session before its login snapshot (docs/31 step 6).</summary>
    public sealed partial class SocialHub
    {
        public void PrepareSession(PlayerSession session, DateTimeOffset now)
        {
            lock (_gate)
            {
                var alliance = AllianceOf(session.PublicId);
                session.SetAllianceResearch(alliance == null
                    ? null
                    : new Dictionary<string, int>(TreeOf(alliance.Record.Id).Levels, StringComparer.Ordinal), now, push: false);
            }
        }
    }
}
