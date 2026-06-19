namespace Hadal.Core.Events
{
    public interface IGameEventListener<T>
    {
        void OnEventRaised(T payload);
    }
}
