using Hadal.Core.Network;
using VContainer;
using VContainer.Unity;

namespace Hadal.Managers.Bootstrap
{
    /// <summary>
    /// Session-bound scope (Login → Logout). Owns network ingress and reconciliation stubs.
    /// </summary>
    public sealed class SessionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ICommandReconciliationSystem, CommandReconciliationSystem>(Lifetime.Singleton);
            builder.Register<INetworkStateReceiver, NetworkStateReceiver>(Lifetime.Singleton);
            builder.RegisterEntryPoint<NetworkStateReceiver>();
        }
    }
}
