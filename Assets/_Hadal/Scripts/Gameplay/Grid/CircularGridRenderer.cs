using System.Collections.Generic;
using Hadal.Core.Contracts;
using UnityEngine;
using VContainer;

namespace Hadal.Gameplay.Grid
{
    /// <summary>
    /// Draws circular grid rings and radial sector spokes (Gizmos + runtime LineRenderer mesh).
    /// </summary>
    [ExecuteAlways]
    public sealed class CircularGridRenderer : MonoBehaviour
    {
        private const int RingSegments = 72;

        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _ringColor = new(0.18f, 0.9f, 0.78f, 0.35f);
        [SerializeField] private Color _spokeColor = new(0.12f, 0.55f, 0.62f, 0.22f);
        [SerializeField] private float _lineWidth = 0.04f;

        private IGridService _grid;
        private ICircularGridService _circularGrid;
        private readonly List<LineRenderer> _ringLines = new();
        private readonly List<LineRenderer> _spokeLines = new();

        [Inject]
        public void Construct(IGridService grid, ICircularGridService circularGrid)
        {
            _grid = grid;
            _circularGrid = circularGrid;
            RebuildLineRenderers();
        }

        private void OnEnable()
        {
            if (_grid != null)
                RebuildLineRenderers();
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _grid?.Definition == null)
                return;

            var def = _grid.Definition;
            var unlocked = _circularGrid?.UnlockedRingCount ?? def.RingCount;
            Gizmos.color = _ringColor;

            for (var ring = 1; ring <= unlocked && ring < def.RingRadii.Length; ring++)
                DrawRingGizmo(def.RingRadii[ring], def.SlotYOffset);
        }

        public void RebuildLineRenderers()
        {
            ClearLineRenderers();

            if (_grid?.Definition == null)
                return;

            var def = _grid.Definition;
            var maxRing = Mathf.Min(_circularGrid?.UnlockedRingCount ?? def.RingCount, def.RingCount);

            for (var ring = 1; ring <= maxRing; ring++)
            {
                if (ring >= def.RingRadii.Length)
                    continue;

                CreateRingLine(def.RingRadii[ring], def.SlotYOffset, ring);
            }

            for (var ring = 1; ring <= maxRing; ring++)
            {
                if (ring >= def.SectorsPerRing.Length)
                    continue;

                var sectorCount = def.SectorsPerRing[ring];
                var radius = def.RingRadii[ring];
                var innerRadius = ring > 0 && ring - 1 < def.RingRadii.Length ? def.RingRadii[ring - 1] : 0f;
                CreateSpokeLines(ring, sectorCount, radius, innerRadius, def.SlotYOffset);
            }
        }

        private void CreateRingLine(float radius, float yOffset, int ringIndex)
        {
            var line = CreateLineRenderer($"Ring_{ringIndex}", _ringColor);
            var points = _circularGrid?.Config != null
                ? PolarGridLayoutUtility.BuildRingPolyline(_circularGrid.Config, ringIndex, RingSegments)
                : BuildFallbackRing(radius, yOffset);

            line.positionCount = points.Length;
            line.SetPositions(points);
            _ringLines.Add(line);
        }

        private void CreateSpokeLines(int ring, int sectorCount, float radius, float innerRadius, float yOffset)
        {
            if (sectorCount <= 0)
                return;

            var arc = 360f / sectorCount;
            for (var sector = 0; sector < sectorCount; sector++)
            {
                var angle = sector * arc * Mathf.Deg2Rad;
                var end = new Vector3(Mathf.Sin(angle) * radius, yOffset, Mathf.Cos(angle) * radius);
                var start = ring == 1
                    ? new Vector3(0f, yOffset, 0f)
                    : new Vector3(Mathf.Sin(angle) * innerRadius, yOffset, Mathf.Cos(angle) * innerRadius);

                var line = CreateLineRenderer($"Spoke_R{ring}_S{sector}", _spokeColor);
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                _spokeLines.Add(line);
            }
        }

        private LineRenderer CreateLineRenderer(string objectName, Color color)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = objectName.StartsWith("Ring");
            line.widthMultiplier = _lineWidth;
            line.numCapVertices = 4;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static Vector3[] BuildFallbackRing(float radius, float yOffset)
        {
            var points = new Vector3[RingSegments + 1];
            for (var i = 0; i <= RingSegments; i++)
            {
                var t = i / (float)RingSegments * Mathf.PI * 2f;
                points[i] = new Vector3(Mathf.Sin(t) * radius, yOffset, Mathf.Cos(t) * radius);
            }

            return points;
        }

        private static void DrawRingGizmo(float radius, float yOffset)
        {
            var prev = Vector3.zero;
            for (var i = 0; i <= RingSegments; i++)
            {
                var t = i / (float)RingSegments * Mathf.PI * 2f;
                var point = new Vector3(Mathf.Sin(t) * radius, yOffset, Mathf.Cos(t) * radius);
                if (i > 0)
                    Gizmos.DrawLine(prev, point);

                prev = point;
            }
        }

        private void ClearLineRenderers()
        {
            foreach (var line in _ringLines)
            {
                if (line != null)
                    DestroyImmediate(line.gameObject);
            }

            foreach (var line in _spokeLines)
            {
                if (line != null)
                    DestroyImmediate(line.gameObject);
            }

            _ringLines.Clear();
            _spokeLines.Clear();
        }
    }
}
