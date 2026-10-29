using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using Hadal.Core.Catalog;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Every team on the map (docs/28) as one submarine on a dashed route from its base to its target: green for the
    /// player's own, blue for alliance mates, red for everyone else. Other players' teams come with the world view and
    /// move on their own timetable, so all players see the same sea live.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private sealed class RouteView
        {
            public LineRenderer Line;
            public Transform Sub;
            public RectTransform Plate;
            public Text Name;
        }

        private readonly Dictionary<string, RouteView> _routes = new();
        private static Material _routeMaterial;

        private const float SubScale = 2.6f;
        private const float RouteHeight = 0.6f;

        public static Color RouteColor(int relation) => relation switch
        {
            0 => new Color(0.36f, 0.95f, 0.5f, 1f),
            1 => new Color(0.4f, 0.7f, 1f, 1f),
            _ => new Color(1f, 0.42f, 0.36f, 1f)
        };

        /// <summary>A short-dashed line texture, made once.</summary>
        private static Material RouteMaterial()
        {
            if (_routeMaterial != null)
                return _routeMaterial;
            var texture = new Texture2D(8, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (var x = 0; x < 8; x++)
            {
                var on = x < 5 ? 255 : 0;
                texture.SetPixel(x, 0, new Color32(255, 255, 255, (byte)on));
                texture.SetPixel(x, 1, new Color32(255, 255, 255, (byte)on));
            }

            texture.Apply();
            _routeMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture };
            return _routeMaterial;
        }

        /// <summary>The submarine every team travels in; a simple hull when the art is missing.</summary>
        private static Transform MakeSubmarine(Transform parent)
        {
            var prefab = CharacterLibrary.Instance?.Model("prop_sub_scout") ?? CharacterLibrary.Instance?.Model("prop_sub_hauler");
            GameObject sub;
            if (prefab != null)
            {
                sub = Instantiate(prefab, parent);
            }
            else
            {
                sub = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                sub.transform.SetParent(parent, false);
                sub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Destroy(sub.GetComponent<Collider>());
            }

            sub.name = "Submarine";
            sub.transform.localScale = Vector3.one * SubScale;
            return sub.transform;
        }

        /// <summary>Routes and submarines of every team in view; the player's own come from their march list.</summary>
        private void UpdateRoutes(MarchDto[] mine, long now)
        {
            var live = new HashSet<string>();
            var visible = _popup == null;

            foreach (var m in mine)
            {
                live.Add(m.Id);
                var route = RouteFor(m.Id, 0, null);
                DrawRoute(route, m.FromX, m.FromY, m.X, m.Y, visible);
            }

            foreach (var w in View.WorldView?.Marches ?? Array.Empty<WorldMarchDto>())
            {
                if (w.Relation == 0 || w.HomeAtMs <= now || live.Contains(w.Id))
                    continue;
                live.Add(w.Id);
                var route = RouteFor(w.Id, w.Relation, string.IsNullOrEmpty(w.AllianceTag) ? w.OwnerName : $"[{w.AllianceTag}] {w.OwnerName}");
                DrawRoute(route, w.FromX, w.FromY, w.ToX, w.ToY, visible);

                // Their submarine on its timetable, facing where it goes.
                var at = Position(w.FromX, w.FromY, w.ToX, w.ToY, w.StartedAtMs, w.ArrivesAtMs, w.GatheredAtMs, w.HomeAtMs, now);
                var heading = now < w.ArrivesAtMs ? TileToWorld(w.ToX, w.ToY) - TileToWorld(w.FromX, w.FromY)
                    : now >= w.GatheredAtMs ? TileToWorld(w.FromX, w.FromY) - TileToWorld(w.ToX, w.ToY) : Vector3.zero;
                route.Sub.gameObject.SetActive(visible);
                route.Sub.position = at + Vector3.up * 1.2f;
                if (heading.sqrMagnitude > 0.01f)
                    route.Sub.rotation = Quaternion.Slerp(route.Sub.rotation, Quaternion.LookRotation(heading.normalized, Vector3.up), 0.2f);
                var top = Project(at + Vector3.up * 6f, out var topOk);
                route.Plate.gameObject.SetActive(visible && topOk && _distance < 140f);
                route.Plate.anchoredPosition = top;
            }

            foreach (var id in _routes.Keys.Where(k => !live.Contains(k)).ToList())
            {
                var route = _routes[id];
                Destroy(route.Line.gameObject);
                if (route.Sub != null)
                    Destroy(route.Sub.gameObject);
                if (route.Plate != null)
                    Destroy(route.Plate.gameObject);
                _routes.Remove(id);
            }
        }

        private RouteView RouteFor(string id, int relation, string ownerLabel)
        {
            if (_routes.TryGetValue(id, out var route))
                return route;

            var color = RouteColor(relation);
            var go = new GameObject("Route " + id);
            go.transform.SetParent(_root, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.material = RouteMaterial();
            line.textureMode = LineTextureMode.Tile;
            line.widthMultiplier = 0.7f;
            line.startColor = line.endColor = new Color(color.r, color.g, color.b, 0.9f);
            line.numCapVertices = 2;
            route = new RouteView { Line = line };

            // Other players' teams get their own submarine and a name plate; the player's own ride in their team view.
            if (relation != 0)
            {
                route.Sub = MakeSubmarine(_root);
                var plate = UiKit.Box("RoutePlate", _labels, new Color(0.02f, 0.05f, 0.09f, 0.85f));
                plate.rectTransform.sizeDelta = new Vector2(300f, 52f);
                plate.raycastTarget = false;
                plate.gameObject.AddComponent<Outline>().effectColor = new Color(color.r, color.g, color.b, 0.7f);
                route.Name = UiKit.Label("Name", plate.transform, ownerLabel ?? string.Empty, 22, color, TextAnchor.MiddleCenter, FontStyle.Bold);
                route.Name.rectTransform.Place(0.03f, 0, 0.97f, 1);
                route.Name.supportRichText = false;
                route.Plate = plate.rectTransform;
                route.Plate.SetAsFirstSibling();
            }

            _routes[id] = route;
            return route;
        }

        private void DrawRoute(RouteView route, int fromX, int fromY, int toX, int toY, bool visible)
        {
            var a = TileToWorld(fromX, fromY) + Vector3.up * RouteHeight;
            var b = TileToWorld(toX, toY) + Vector3.up * RouteHeight;
            route.Line.enabled = visible;
            route.Line.SetPosition(0, a);
            route.Line.SetPosition(1, b);
            // One dash per world unit or so, whatever the zoom.
            route.Line.material.mainTextureScale = new Vector2(Vector3.Distance(a, b) / 2.5f, 1f);
        }

        /// <summary>Where a team is at <paramref name="now"/>: out, at the target, or on the way home.</summary>
        private Vector3 Position(int fromX, int fromY, int toX, int toY, long started, long arrives, long gathered, long home, long now)
        {
            var from = TileToWorld(fromX, fromY);
            var to = TileToWorld(toX, toY);
            if (now < arrives)
                return Vector3.Lerp(from, to, Mathf.InverseLerp(started, arrives, now));
            if (now < gathered)
                return to;
            var back = home - gathered;
            return Vector3.Lerp(to, from, back <= 0 ? 1f : Mathf.Clamp01((now - gathered) / (float)back));
        }
    }
}
