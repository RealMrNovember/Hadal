using Hadal.Core.State;
using Hadal.Data.Config;
using Hadal.Managers.Base;
using VContainer;

namespace Hadal.Managers
{
    public class GameManager : ManagerBase
    {
        private GameStateMachine _stateMachine;

        protected override void OnInitialize(GameConfigSO config) { }

        [Inject]
        public void InjectStateMachine(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void RequestStateChange(GameStateType state) => _stateMachine?.ChangeState(state);

        protected override void OnShutdown() => _stateMachine = null;
    }
}
