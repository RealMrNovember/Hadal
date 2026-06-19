using UnityEngine;
using Hadal.Data.Config;
using Hadal.Gameplay.Grid;

namespace Hadal.Gameplay.Grid
{
    [RequireComponent(typeof(LineRenderer))]
    public class UnderwaterDomeRingView : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Gradient _ringGradient;
        [SerializeField] private float _lineWidth = 0.08f;

        private void Awake()
        {
            if (_lineRenderer == null)
                _lineRenderer = GetComponent<LineRenderer>();

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.widthMultiplier = _lineWidth;
            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.numCapVertices = 4;

            if (_ringGradient != null)
                _lineRenderer.colorGradient = _ringGradient;
        }

        public void Configure(CircularGridConfigSO config, int ring)
        {
            if (config == null || _lineRenderer == null)
                return;

            if (ring == 0)
            {
                _lineRenderer.positionCount = 0;
                gameObject.SetActive(false);
                return;
            }

            var points = PolarGridLayoutUtility.BuildRingPolyline(config, ring, 72);
            _lineRenderer.positionCount = points.Length;

            for (var i = 0; i < points.Length; i++)
                _lineRenderer.SetPosition(i, points[i]);
        }
    }
}
