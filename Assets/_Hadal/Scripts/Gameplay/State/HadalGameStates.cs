using UnityEngine;
using Hadal.Core.State;

namespace Hadal.Gameplay.State
{
    public abstract class HadalGameState : GameState
    {
    }

    public sealed class MainMenuState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.MainMenu;
        public override void Enter() => Debug.Log("[State] MainMenu");
    }

    public sealed class BaseBuildingState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.BaseBuilding;
        public override void Enter() => Debug.Log("[State] BaseBuilding");
    }

    public sealed class ExpeditionState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.Expedition;
        public override void Enter() => Debug.Log("[State] Expedition");
    }

    public sealed class CombatState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.Combat;
        public override void Enter() => Debug.Log("[State] Combat");
    }

    public sealed class MapState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.Map;
        public override void Enter() => Debug.Log("[State] Map");
    }

    public sealed class AllianceState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.Alliance;
        public override void Enter() => Debug.Log("[State] Alliance");
    }

    public sealed class LoadingState : HadalGameState
    {
        public override GameStateType StateType => GameStateType.Loading;
        public override void Enter() => Debug.Log("[State] Loading");
    }
}
