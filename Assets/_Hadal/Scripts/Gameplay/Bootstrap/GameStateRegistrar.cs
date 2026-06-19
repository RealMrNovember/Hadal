using UnityEngine;
using Hadal.Core.State;

namespace Hadal.Gameplay.Bootstrap
{
    public class GameStateRegistrar : MonoBehaviour, IGameStateRegistration
    {
        public void Register(GameStateMachine machine)
        {
            machine.RegisterState(new State.MainMenuState());
            machine.RegisterState(new State.BaseBuildingState());
            machine.RegisterState(new State.ExpeditionState());
            machine.RegisterState(new State.CombatState());
            machine.RegisterState(new State.MapState());
            machine.RegisterState(new State.AllianceState());
            machine.RegisterState(new State.LoadingState());
        }
    }
}
