using System;
using HADAL.Shared.Simulation;
using Hadal.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The map's dialogs (march setup, rally muster, team details, donations, the alliance building picker, monster
    /// search) on Last Asylum's frame: a centred dialog over the blurred map, its title on the navy bar, the light body
    /// sized to its rows. A dialog that redraws itself (picking heroes, stepping a level) refills in place.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private UiFrames.DialogSheet _sheet;
        private string _sheetKind = string.Empty;

        /// <summary>
        /// Opens the map's dialog of <paramref name="kind"/>, or empties the one already up for a refill (no new blur, no
        /// pop-in). Any other popup closes first.
        /// </summary>
        private UiFrames.DialogSheet MapDialog(string kind, string title, float footer = 0f)
        {
            if (_sheet is { IsOpen: true } && _popup == _sheet.Root.gameObject && _sheetKind == kind)
            {
                UiFrames.ClearSheet(_sheet);
                _sheet.Parts.Title.text = title;
                return _sheet;
            }

            ClosePopup();
            _sheet = UiFrames.OpenSheet(_popupCanvas.transform, title, ClosePopup, footer: footer);
            _sheetKind = kind;
            _popup = _sheet.Root.gameObject;
            return _sheet;
        }

        /// <summary>A hero seat of the march setup and team details: the portrait over a green chip with the troops led.</summary>
        private static RectTransform Seat(Transform parent, HADAL.Shared.Catalog.HeroDefinition hero, long troops, Action click, bool owned = true)
        {
            var seat = UiFrames.SheetBlock(parent, UiFrames.CardAlt, "Seat");
            if (hero == null)
            {
                UiKit.Label("Plus", seat.transform, "+", UiKit.TypeScale.Display + 8, UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
                return seat.rectTransform;
            }

            SocialView.Portrait(seat.transform, hero, owned, 0.05f, 0.24f, 0.95f, 0.97f);
            var chip = UiKit.Box("Troops", seat.transform, new Color(0.17f, 0.6f, 0.3f, 1f));
            chip.sprite = UiKit.Rounded;
            chip.rectTransform.Place(0.05f, 0.03f, 0.95f, 0.22f);
            var count = UiKit.Label("Count", chip.transform, CostChips.Compact(troops), UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            count.rectTransform.Place(0, 0, 1, 1);
            UiText.Outline(count, new Color(0.03f, 0.2f, 0.08f, 0.9f));
            if (click != null)
            {
                seat.raycastTarget = true;
                seat.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    click();
                });
                seat.gameObject.AddComponent<PressFeedback>();
            }

            return seat.rectTransform;
        }

        /// <summary>A bar with a fill share and a centred caption (troops aboard against command, a leg of a trip).</summary>
        private static Image ShareBar(Transform parent, float share, string caption, Color fill)
        {
            var track = UiKit.Box("Bar", parent, new Color(0.55f, 0.62f, 0.72f, 1f));
            track.sprite = UiKit.Rounded;
            track.raycastTarget = false;
            var bar = UiKit.Box("Fill", track.transform, fill);
            bar.sprite = UiKit.Rounded;
            bar.raycastTarget = false;
            bar.rectTransform.Place(0, 0, Mathf.Clamp01(share), 1);
            if (!string.IsNullOrEmpty(caption))
            {
                var text = UiKit.Label("Text", track.transform, caption, UiKit.TypeScale.Caption + 2, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                text.rectTransform.Place(0, 0, 1, 1);
                UiText.Outline(text, new Color(0.04f, 0.07f, 0.12f, 0.9f));
            }

            return bar;
        }

        /// <summary>Rich text lines on a pale block, the block as tall as the lines.</summary>
        private static TMP_Text InfoBlock(Transform parent, string text)
        {
            var lines = (text ?? string.Empty).Split('\n').Length;
            var block = UiFrames.SheetBlock(parent, UiFrames.Card, "Info");
            block.Row(30 + lines * 46);
            block.gameObject.AddComponent<InkSurface>(); // the setup texts carry light-on-dark colour tags
            var label = UiKit.Label("Text", block.transform, text, UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft);
            label.rectTransform.Place(0.04f, 0.04f, 0.96f, 0.96f);
            return label;
        }
    }
}
