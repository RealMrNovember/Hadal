using HADAL.Shared.DTOs;

namespace Hadal.Game
{
    /// <summary>
    /// One client transport. Implementations must accept <see cref="Send"/> from any thread
    /// (tick loop and command handling both send) and preserve order.
    /// </summary>
    public interface IClientConnection
    {
        string ConnectionId { get; }

        void Send(ServerMessage message);

        void Close(string reason);
    }
}
