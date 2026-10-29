using System;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The first-session guide's arrow on the world map (docs/34): it points at the nearest beginner monster or free
    /// resource node, bobs over it when it is on screen, and a tap opens it (Attack / Gather).
    /// </summary>
    public sealed partial class WorldMapView
    {
        /// <summary>"monster", "node" or null; set every frame by the guide.</summary>
        public string GuideTarget { get; set; }

        private RectTransform _guideArrow;
        private RectTransform _guidePointer;
        private Text _guideLabel;
        private Action _guideTap;

        private void BuildGuideArrow()
        {
            _guideArrow = UiKit.Rect("GuideArrow", _labels);
            _guideArrow.sizeDelta = new Vector2(150f, 150f);
            var disc = _guideArrow.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.color = new Color(0.95f, 0.6f, 0.15f, 0.95f);
            _guideArrow.gameObject.AddComponent<Button>().onClick.AddListener(() => _guideTap?.Invoke());
            _guideArrow.gameObject.AddComponent<PressFeedback>();
            _guideLabel = UiKit.Label("Label", _guideArrow, string.Empty, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _guideLabel.rectTransform.Place(0.08f, 0.1f, 0.92f, 0.9f);
            _guideLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            _guidePointer = UiKit.Rect("Pointer", _guideArrow);
            _guidePointer.sizeDelta = new Vector2(52f, 52f);
            var tip = UiKit.Label("Tip", _guidePointer, "▲", 46, new Color(0.95f, 0.6f, 0.15f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            tip.rectTransform.Place(0, 0, 1, 1);
            tip.raycastTarget = false;
            _guideArrow.gameObject.SetActive(false);
        }

        private void UpdateGuideArrow()
        {
            var view = View.WorldView;
            if (_guideArrow == null)
                return;
            if (view == null || string.IsNullOrEmpty(GuideTarget) || _popup != null || TargetMenu.IsOpen)
            {
                _guideArrow.gameObject.SetActive(false);
                return;
            }

            int x, y;
            if (GuideTarget == "monster")
            {
                var monster = (view.Monsters ?? Array.Empty<WorldMonsterDto>()).Where(m => !m.Busy && m.HpMax == 0)
                    .OrderBy(m => m.Level).ThenBy(m => GatherRules.Km(m.X, m.Y, view.MyX, view.MyY)).FirstOrDefault();
                if (monster == null)
                {
                    _guideArrow.gameObject.SetActive(false);
                    return;
                }

                (x, y) = (monster.X, monster.Y);
                _guideTap = () => ShowMonster(monster);
                _guideLabel.text = Loc.F("guide.map.monster", monster.Level);
            }
            else
            {
                var node = (view.Nodes ?? Array.Empty<WorldNodeDto>()).Where(n => !n.Busy && !n.Mine && n.Remaining > 0)
                    .OrderBy(n => GatherRules.Km(n.X, n.Y, view.MyX, view.MyY)).FirstOrDefault();
                if (node == null)
                {
                    _guideArrow.gameObject.SetActive(false);
                    return;
                }

                (x, y) = (node.X, node.Y);
                _guideTap = () => ShowNode(node);
                _guideLabel.text = Loc.T("guide.map.node");
            }

            _guideArrow.gameObject.SetActive(true);
            _guideArrow.anchorMin = _guideArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _guidePointer.anchorMin = _guidePointer.anchorMax = new Vector2(0.5f, 0.5f);
            var target = TileToWorld(x, y) + Vector3.up * 4f;
            var screen = _camera.WorldToScreenPoint(target);
            var onScreen = screen.z > 0f && screen.x > 0f && screen.y > 0f && screen.x < Screen.width && screen.y < Screen.height;
            if (onScreen)
            {
                // Over the target: the disc bobs above it with the tip pointing down.
                var local = Project(target, out _);
                _guideArrow.anchoredPosition = local + new Vector2(0f, 110f + 14f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
                _guidePointer.anchoredPosition = new Vector2(0f, -86f);
                _guidePointer.localRotation = Quaternion.Euler(0f, 0f, 180f);
                return;
            }

            // Off screen: on an inner frame towards the target, like the home arrow.
            var centre = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var dir = (Vector2)screen - centre;
            if (screen.z < 0f)
                dir = -dir;
            if (dir.sqrMagnitude < 1f)
                dir = Vector2.up;
            dir.Normalize();
            var half = _labels.rect.size / 2f;
            var frame = new Vector2(half.x - 120f, half.y - 360f);
            var scale = Mathf.Min(frame.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), frame.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            _guideArrow.anchoredPosition = dir * scale;
            _guidePointer.anchoredPosition = dir * 92f;
            _guidePointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }
    }
}
