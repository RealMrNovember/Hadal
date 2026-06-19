using Hadal.Core.DI;
using Hadal.Core.State;
using Hadal.Data.Config;

namespace Hadal.Core.DI
{
    public interface IGameContext
    {
        GameConfigSO Config { get; }
        GameStateMachine StateMachine { get; }
        GameServiceContainer Container { get; }
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T service) where T : class;
    }

    public sealed class GameContext : IGameContext
    {
        public static GameContext Current { get; private set; }

        public GameConfigSO Config { get; }
        public GameStateMachine StateMachine { get; }
        public GameServiceContainer Container { get; }

        public GameContext(GameConfigSO config, GameStateMachine stateMachine, GameServiceContainer container)
        {
            Config = config;
            StateMachine = stateMachine;
            Container = container;
        }

        public static void SetCurrent(GameContext context) => Current = context;

        public static void ClearCurrent()
        {
            Current = null;
        }

        public T Resolve<T>() where T : class => Container.Resolve<T>();

        public bool TryResolve<T>(out T service) where T : class => Container.TryResolve(out service);
    }
}
