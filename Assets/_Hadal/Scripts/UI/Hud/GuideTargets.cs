using System.Collections.Generic;
using UnityEngine;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// Named HUD buttons the first-session guide can point at (docs/34): "Task", "Heroes", "Territory", "Alliance",
    /// "Help", "Bag", "Quests", "Expand", "Research". Only a live, visible button is handed out.
    /// </summary>
    public static class GuideTargets
    {
        private static readonly Dictionary<string, RectTransform> Targets = new();

        public static void Register(string name, RectTransform rect)
        {
            if (!string.IsNullOrEmpty(name) && rect != null)
                Targets[name] = rect;
        }

        public static RectTransform Get(string name)
        {
            if (string.IsNullOrEmpty(name) || !Targets.TryGetValue(name, out var rect) || rect == null)
                return null;
            return rect.gameObject.activeInHierarchy ? rect : null;
        }

        /// <summary>Forgets everything (tests, a rebuilt HUD).</summary>
        public static void Clear() => Targets.Clear();
    }
}
