using VContainer;
using VContainer.Unity;

namespace Hadal.Managers.Bootstrap
{
    /// <summary>
    /// Session-bound scope placeholder — network services live in <see cref="ProjectLifetimeScope"/> for gameplay DI.
    /// </summary>
    public sealed class SessionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
        }
    }
}
