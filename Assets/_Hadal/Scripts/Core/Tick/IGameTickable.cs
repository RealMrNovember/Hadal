namespace Hadal.Core.Tick
{
    public interface IGameTickable
    {
        int TickPriority { get; }
        void Tick(float deltaTime);
    }
}
