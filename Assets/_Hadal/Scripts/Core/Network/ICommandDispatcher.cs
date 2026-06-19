using HADAL.Shared.Commands;

namespace Hadal.Core.Network
{
    public interface ICommandDispatcher
    {
        void Dispatch<T>(T command) where T : ICommand;

        /// <summary>
        /// Sends handshake immediately — never buffered.
        /// </summary>
        void DispatchHandshake<T>(T command) where T : ICommand;

        void FlushBuffer();
    }
}
