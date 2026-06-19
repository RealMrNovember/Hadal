using HADAL.Shared.Commands;

namespace Hadal.Core.Network
{
    public interface ICommandDispatcher
    {
        void Dispatch<T>(T command) where T : ICommand;
    }
}
