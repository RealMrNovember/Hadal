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
        /// <summary>The outermost unlocked ring's radius: the base camera may pan and zoom out that far.</summary>
        public static float Reach { get; set; }

        public static event Action<Vector3, float?> Requested;

        public static void Request(Vector3 world, float? distance = null) => Requested?.Invoke(world, distance);

        /// <summary>The base comes into view (title lifted, back from the world): the camera settles in from higher up.</summary>
        public static event Action SettleRequested;

        public static void Settle() => SettleRequested?.Invoke();
    }
}
