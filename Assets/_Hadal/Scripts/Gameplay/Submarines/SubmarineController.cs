using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.DI;
using Hadal.Data.Config;

namespace Hadal.Gameplay.Submarines
{
    public class SubmarineController : MonoBehaviour
    {
        [SerializeField] private SubmarineDefinitionSO _definition;
        [SerializeField] private float _currentDepth;

        private IPressureService _pressureService;

        public float CurrentDepth => _currentDepth;

        private void Start()
        {
            if (GameContext.Current != null)
                GameContext.Current.TryResolve(out _pressureService);
        }

        public void SetDepth(float depthMeters)
        {
            _currentDepth = depthMeters;
            _pressureService?.EvaluateDepth(_currentDepth);
        }

        public void Dive(float deltaDepth) => SetDepth(_currentDepth + deltaDepth);
    }
}
