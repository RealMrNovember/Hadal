namespace Hadal.Game
{
    /// <summary>Server clock. Production accrues against this, never against anything the client sends.</summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
