using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Models;
using Hadal.Managers.Services;

namespace Hadal.Gameplay.Grid
{
    public static class PolarGridLayoutUtility
    {
        public static Vector3 GetSlotWorldPosition(CircularGridConfigSO config, PolarGridSlotId id, float slotAngleDegrees)
        {
            if (config == null)
                return Vector3.zero;

            if (id.Ring == 0)
                return new Vector3(0f, config.SlotYOffset, 0f);

            var radius = config.GetRingRadius(id.Ring);
            var rad = slotAngleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * radius, config.SlotYOffset, Mathf.Cos(rad) * radius);
        }

        public static Quaternion GetSlotWorldRotation(float slotAngleDegrees, float buildingRotationOffset)
        {
            return Quaternion.Euler(0f, slotAngleDegrees + buildingRotationOffset, 0f);
        }

        public static bool TryGetNearestSlot(ICircularGridService grid, Vector3 worldPoint, out PolarGridSlotId nearest)
        {
            nearest = default;
            if (grid == null)
                return false;

            var bestDistance = float.MaxValue;
            var found = false;

            foreach (var slot in grid.GetAllSlots())
            {
                if (!slot.IsUnlocked)
                    continue;

                var position = grid.GetWorldPosition(slot.Id);
                var flat = worldPoint - position;
                flat.y = 0f;
                var distance = flat.sqrMagnitude;

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                nearest = slot.Id;
                found = true;
            }

            return found;
        }

        public static Vector3[] BuildRingPolyline(CircularGridConfigSO config, int ring, int segments = 64)
        {
            if (config == null || ring < 0)
                return System.Array.Empty<Vector3>();

            if (ring == 0)
                return new[] { Vector3.zero };

            var radius = config.GetRingRadius(ring);
            var points = new Vector3[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments * Mathf.PI * 2f;
                points[i] = new Vector3(Mathf.Sin(t) * radius, config.SlotYOffset, Mathf.Cos(t) * radius);
            }

            return points;
        }
    }
}
