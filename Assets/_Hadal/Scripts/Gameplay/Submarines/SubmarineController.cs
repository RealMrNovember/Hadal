using UnityEngine;
using Hadal.Core.Contracts;
using VContainer;

namespace Hadal.Gameplay.Submarines
{
    public class SubmarineController : MonoBehaviour
    {
        [SerializeField] private float _depthMeters;

        private IPressureService _pressureService;

        [Inject]
        public void Construct(IPressureService pressureService)
        {
            _pressureService = pressureService;
        }

        private void Update()
        {
            if (_pressureService == null)
                return;

            var snapshot = _pressureService.EvaluateDepth(_depthMeters);
            if (!snapshot.IsSurvivable)
                Debug.LogWarning("[Submarine] Hull stress critical.");
        }
    }
}
