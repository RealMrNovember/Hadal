using System;
using UnityEngine;

namespace Hadal.Core.State
{
    /// <summary>
    /// Lets the UI ask the base camera to glide to a point (a tapped building), like the genre's building menus,
    /// without the UI assembly knowing the camera rig. The rig subscribes; null distance keeps the zoom.
    /// </summary>
    public static class CameraFocus
    {
        public static event Action<Vector3, float?> Requested;

        public static void Request(Vector3 world, float? distance = null) => Requested?.Invoke(world, distance);
    }
}
