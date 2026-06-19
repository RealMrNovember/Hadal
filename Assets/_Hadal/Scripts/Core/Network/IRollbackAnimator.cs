using Hadal.Data.Models;

namespace Hadal.Core.Network
{
    public interface IRollbackAnimator
    {
        void RequestBuildingRollback(BuildingSaveEntry authoritative, string commandId);
        void Tick(float deltaTime);
    }
}
