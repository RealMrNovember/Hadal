namespace Hadal.Core.State
{
    public enum GameStateType
    {
        Bootstrap,
        MainMenu,
        BaseBuilding,
        Expedition,
        Combat,
        Map,
        Alliance,
        Loading
    }

    public abstract class GameState
    {
        public abstract GameStateType StateType { get; }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick(float deltaTime) { }
    }
}
