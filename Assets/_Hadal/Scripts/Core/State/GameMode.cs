namespace Hadal.Core.State
{
    /// <summary>
    /// Which place the player is looking at: their own base or the outer world map (docs/28). Base input
    /// (camera, slot taps) stays idle while the world map is open.
    /// </summary>
    public static class GameMode
    {
        public static bool InWorld { get; set; }
    }
}
