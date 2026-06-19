using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.State
{
    public class GameStateMachine
    {
        private readonly Dictionary<GameStateType, GameState> _states = new();
        private GameState _current;

        public GameStateType CurrentStateType => _current?.StateType ?? GameStateType.Bootstrap;
        public event Action<GameStateType, GameStateType> StateChanged;

        public void RegisterState(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            _states[state.StateType] = state;
        }

        public void ChangeState(GameStateType next)
        {
            if (_current != null && _current.StateType == next)
                return;

            if (!_states.TryGetValue(next, out var newState))
            {
                Debug.LogError($"[GameStateMachine] Unregistered state: {next}");
                return;
            }

            var previous = _current?.StateType ?? GameStateType.Bootstrap;
            _current?.Exit();
            _current = newState;
            _current.Enter();
            StateChanged?.Invoke(previous, next);
        }

        public void Tick(float deltaTime) => _current?.Tick(deltaTime);
    }
}
