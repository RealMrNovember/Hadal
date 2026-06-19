using System;
using Hadal.Core.Services;

namespace Hadal.Core.Services
{
    /// <summary>
    /// Lifecycle contract for runtime services registered in <see cref="DI.GameServiceContainer"/>.
    /// </summary>
    public interface IGameService
    {
        void Initialize();
        void Shutdown();
    }

    public interface IGameServiceAsync : IGameService
    {
        System.Threading.Tasks.Task InitializeAsync();
    }
}
