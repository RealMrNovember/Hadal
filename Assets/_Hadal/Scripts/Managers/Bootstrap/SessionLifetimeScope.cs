using Hadal.Core.Network;
using VContainer;
using VContainer.Unity;

namespace Hadal.Managers.Bootstrap
{
    /// <summary>
    /// Session-bound scope (Login → Logout). Owns ingress, dispatch, and reconciliation.
    /// </summary>
    public sealed class SessionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IGatewaySessionState, GatewaySessionState>(Lifetime.Singleton);
            builder.Register<IRollbackAnimator, RollbackAnimator>(Lifetime.Singleton);
            builder.Register<ICommandReconciliationSystem, CommandReconciliationSystem>(Lifetime.Singleton);
            builder.Register<ICommandDispatcher, CommandDispatcher>(Lifetime.Singleton);
            builder.Register<INetworkStateReceiver, NetworkStateReceiver>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CommandReconciliationSystem>();
            builder.RegisterEntryPoint<NetworkStateReceiver>();
        }
    }
}
