using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// The rendered HUD icons in Resources/Icons (art/blender/icons.py) as sprites, loaded once.
    /// A missing icon yields null, so the HUD still works with text only.
    /// </summary>
    public static class HudIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Get(string id)
        {
            if (Cache.TryGetValue(id, out var sprite))
                return sprite;

            var texture = Resources.Load<Texture2D>("Icons/" + id);
            sprite = texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[id] = sprite;
            return sprite;
        }

        /// <summary>An aspect-kept icon image filling <paramref name="parent"/>'s given area.</summary>
        public static Image Image(Transform parent, string id, float xMin, float yMin, float xMax, float yMax)
        {
            var image = UiKit.Box("Icon_" + id, parent, Color.white);
            image.sprite = Get(id);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = image.sprite != null;
            image.rectTransform.Place(xMin, yMin, xMax, yMax);
            return image;
        }

        /// <summary>The resource's icon id ("res_oxygen").</summary>
        public static string Resource(HADAL.Shared.Enums.ResourceType type) => "res_" + type.ToString().ToLowerInvariant();
    }
}
