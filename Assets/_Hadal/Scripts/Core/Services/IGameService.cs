namespace Hadal.Core.Services
{
    /// <summary>
    /// Lifecycle contract for runtime services registered via VContainer.
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
