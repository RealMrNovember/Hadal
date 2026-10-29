using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.Art
{
    /// <summary>
    /// Cosmetic sea life around the world-map camera: a few schools of glowing angelfish and some drifting jellies.
    /// Client-only and cheap: a fixed set of bodies (28 by default) sharing one material each, moved on the CPU with no
    /// allocations; a school that falls behind the camera is moved ahead of it again. Hidden in the strategic view.
    /// </summary>
    public sealed class MapWildlife : MonoBehaviour
    {
        public const int Schools = 3;
        public const int FishPerSchool = 7;
        public const int Jellies = 7;

        /// <summary>Life stays within this distance of the camera focus; beyond it a group moves back in.</summary>
        public const float Range = 110f;

        private sealed class Group
        {
            public Vector3 Centre;
            public float Angle;
            public float Radius;
            public float Speed;
            public float Depth;
            public readonly List<(Transform Body, Vector3 Offset)> Members = new();
        }

        private readonly List<Group> _schools = new();
        private readonly List<(Transform Body, Vector3 Anchor, float Phase)> _jellies = new();
        private System.Random _random = new(7);
        private Vector3 _focus;

        public int BodyCount { get; private set; }

        /// <summary>Builds the schools from the creature library; false when the art is missing (nothing is shown).</summary>
        public bool Build(CreatureLibrary library, Vector3 focus)
        {
            if (library == null || BodyCount > 0)
                return BodyCount > 0;
            _focus = focus;
            for (var s = 0; s < Schools; s++)
            {
                var school = new Group();
                for (var i = 0; i < FishPerSchool; i++)
                {
                    var fish = library.Spawn("creature_glow_angelfish", transform, 0.8f + (float)_random.NextDouble() * 0.4f);
                    if (fish == null)
                        break;
                    // The school moves the roots; each body only weaves (CreatureMotion.Schooling).
                    fish.Schooling = true;
                    var offset = new Vector3(Rand(-6f, 6f), Rand(-2f, 2f), Rand(-6f, 6f));
                    school.Members.Add((fish.transform, offset));
                    BodyCount++;
                }

                if (school.Members.Count == 0)
                    return false;
                Respawn(school, focus);
                _schools.Add(school);
            }

            for (var j = 0; j < Jellies; j++)
            {
                var jelly = library.Spawn("creature_drift_jelly", transform, 0.7f + (float)_random.NextDouble() * 0.6f);
                if (jelly == null)
                    break;
                var anchor = focus + new Vector3(Rand(-Range, Range), Rand(4f, 14f), Rand(-Range, Range));
                jelly.transform.position = anchor;
                _jellies.Add((jelly.transform, anchor, Rand(0f, 10f)));
                BodyCount++;
            }

            return BodyCount > 0;
        }

        private float Rand(float a, float b) => Mathf.Lerp(a, b, (float)_random.NextDouble());

        private void Respawn(Group school, Vector3 focus)
        {
            school.Centre = focus + new Vector3(Rand(-Range, Range) * 0.7f, 0f, Rand(-Range, Range) * 0.7f);
            school.Radius = Rand(14f, 30f);
            school.Speed = Rand(2.5f, 4.5f) * (_random.Next(2) == 0 ? 1f : -1f);
            school.Angle = Rand(0f, Mathf.PI * 2f);
            school.Depth = Rand(6f, 12f);
        }

        /// <summary>Moves the schools around the camera focus; call each frame while the map is open.</summary>
        public void Tick(Vector3 focus, bool visible, float deltaTime)
        {
            _focus = focus;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            if (!visible)
                return;

            foreach (var school in _schools)
            {
                if (Flat(school.Centre - focus).magnitude > Range * 1.4f)
                    Respawn(school, focus);
                school.Angle += school.Speed / school.Radius * deltaTime;
                var direction = Mathf.Sign(school.Speed);
                var lead = school.Centre + new Vector3(Mathf.Cos(school.Angle), 0f, Mathf.Sin(school.Angle)) * school.Radius + Vector3.up * school.Depth;
                var heading = new Vector3(-Mathf.Sin(school.Angle), 0f, Mathf.Cos(school.Angle)) * direction;
                var facing = Quaternion.LookRotation(heading);
                for (var i = 0; i < school.Members.Count; i++)
                {
                    var (body, offset) = school.Members[i];
                    var wobble = new Vector3(0f, Mathf.Sin(Time.time * 1.3f + i) * 0.6f, 0f);
                    body.SetPositionAndRotation(lead + facing * offset + wobble, facing);
                }
            }

            for (var i = 0; i < _jellies.Count; i++)
            {
                var (body, anchor, phase) = _jellies[i];
                if (Flat(anchor - focus).magnitude > Range * 1.4f)
                {
                    anchor = focus + new Vector3(Rand(-Range, Range), Rand(4f, 14f), Rand(-Range, Range));
                    _jellies[i] = (body, anchor, phase);
                }

                // A slow drift with the current.
                body.position = anchor + new Vector3(Mathf.Sin((Time.time + phase) * 0.05f) * 6f, 0f, Mathf.Cos((Time.time + phase) * 0.04f) * 6f);
            }
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
