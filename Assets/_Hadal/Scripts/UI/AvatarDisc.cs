using System;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.UI.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Player faces and alliance banners, drawn the same everywhere (chat, profile, member list, map): a round
    /// avatar with the chosen portrait (else the initial on a colour from the player id) and a square banner.
    /// </summary>
    public static class AvatarDisc
    {
        /// <summary>A round avatar filling <paramref name="parent"/> (kept square, centred).</summary>
        public static RectTransform Draw(Transform parent, string avatarId, string name, string seed, Color? fill = null)
        {
            var root = UiKit.Rect("Avatar", parent);
            var fitter = root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            var disc = root.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.color = fill ?? Tint(seed);
            disc.raycastTarget = false;

            var painted = CharacterLibrary.Instance?.Portrait(avatarId);
            if (painted != null)
            {
                // The disc clips the portrait; the top square of a tall portrait is the face.
                root.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var picture = UiKit.Rect("Portrait", root).gameObject.AddComponent<RawImage>();
                picture.texture = painted;
                picture.raycastTarget = false;
                var ratio = painted.height > 0 ? (float)painted.width / painted.height : 1f;
                picture.uvRect = ratio < 1f
                    ? new Rect(0f, Mathf.Max(0f, 1f - ratio - 0.04f), 1f, ratio)
                    : new Rect((1f - 1f / ratio) / 2f, 0f, 1f / ratio, 1f);
                ((RectTransform)picture.transform).Place(0, 0, 1, 1);
            }
            else
            {
                var initial = UiKit.Label("Initial", root, string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant(), 40, Color.white,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                initial.enableAutoSizing = true;
                initial.fontSizeMin = 12;
                initial.fontSizeMax = 96;
                initial.raycastTarget = false;
                initial.rectTransform.Place(0.15f, 0.15f, 0.85f, 0.85f);
            }

            return root;
        }

        /// <summary>A steady colour per player for the initials disc.</summary>
        public static Color Tint(string seed)
        {
            var hash = 17;
            foreach (var c in seed ?? string.Empty)
                hash = unchecked(hash * 31 + c);
            return Color.HSVToRGB(Math.Abs(hash % 360) / 360f, 0.45f, 0.75f);
        }

        /// <summary>An alliance banner: the icon on its colour, filling <paramref name="parent"/> (kept square).</summary>
        public static RectTransform Emblem(Transform parent, string emblem)
        {
            var (icon, color) = EmblemParts(emblem);
            var root = UiKit.Rect("Emblem", parent);
            var fitter = root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            var plate = root.gameObject.AddComponent<Image>();
            plate.sprite = UiKit.Rounded;
            plate.type = Image.Type.Sliced;
            ColorUtility.TryParseHtmlString("#" + SocialRules.EmblemColors[color], out var tint);
            plate.color = tint;
            plate.raycastTarget = false;
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(2, -2);
            HudIcons.Image(root, SocialRules.EmblemIcons[icon], 0.16f, 0.16f, 0.84f, 0.84f).raycastTarget = false;
            return root;
        }

        public static (int Icon, int Color) EmblemParts(string emblem)
        {
            if (!SocialRules.IsValidEmblem(emblem))
                emblem = SocialRules.DefaultEmblem;
            var parts = emblem.Split(':');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }
}
