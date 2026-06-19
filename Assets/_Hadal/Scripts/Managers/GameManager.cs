using UnityEngine;
using Hadal.Core.DI;
using Hadal.Core.State;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class GameManager : ManagerBase
    {
        private GameStateMachine _stateMachine;

        protected override void OnInitialize(GameConfigSO config)
        {
            _stateMachine = GameContext.Current?.StateMachine;
        }

        public void RequestStateChange(GameStateType state) => _stateMachine?.ChangeState(state);

        protected override void OnShutdown() => _stateMachine = null;
    }
}
