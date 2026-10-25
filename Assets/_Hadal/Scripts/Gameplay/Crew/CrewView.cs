using System.Collections.Generic;
using Hadal.Gameplay.Buildings;
using UnityEngine;

namespace Hadal.Gameplay.Crew
{
    /// <summary>
    /// The visible crew of the base: little people walking between buildings and working at them.
    /// Purely cosmetic for now — headcount follows the number of buildings. When the server
    /// replicates crew (Phase 4 survival loop) the count and roles come from state instead.
    /// </summary>
    public sealed class CrewView : MonoBehaviour
    {
        [SerializeField] private BuildingStateView _buildings;
        [SerializeField] private GameObject[] _crewPrefabs = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] _mechPrefabs = System.Array.Empty<GameObject>();
        [SerializeField] private int _crewPerBuilding = 2;
        [SerializeField] private int _baseCrew = 3;
        [SerializeField] private int _maxCrew = 16;
        [SerializeField] private float _refreshSeconds = 1f;

        private readonly List<CrewMember> _members = new();
        private readonly List<Vector3> _buildingPositions = new();
        private float _nextRefresh;
        private int _spawned;

        public IReadOnlyList<Vector3> BuildingPositions => _buildingPositions;
        public int CrewCount => _members.Count;

        public void Configure(BuildingStateView buildings, GameObject[] crew, GameObject[] mechs)
        {
            _buildings = buildings;
            _crewPrefabs = crew;
            _mechPrefabs = mechs;
        }

        private void Update()
        {
            if (Time.time < _nextRefresh)
                return;

            _nextRefresh = Time.time + _refreshSeconds;
            if (_buildings == null)
                return;

            _buildings.GetBuildingPositions(_buildingPositions);
            var wanted = _buildingPositions.Count == 0
                ? 0
                : Mathf.Min(_maxCrew, _baseCrew + _crewPerBuilding * (_buildingPositions.Count - 1));

            while (_members.Count < wanted)
                Spawn();

            while (_members.Count > wanted)
            {
                var last = _members[^1];
                _members.RemoveAt(_members.Count - 1);
                if (last != null)
                    Destroy(last.gameObject);
            }
        }

        private void Spawn()
        {
            // Every fifth worker is a mech; the rest rotate through the crew roles.
            var useMech = _mechPrefabs.Length > 0 && _spawned % 5 == 4;
            var pool = useMech ? _mechPrefabs : _crewPrefabs;
            if (pool.Length == 0)
                return;

            var prefab = pool[_spawned % pool.Length];
            _spawned++;

            var spawnAt = _buildingPositions.Count > 0
                ? _buildingPositions[Random.Range(0, _buildingPositions.Count)] + RandomRing(2.8f)
                : transform.position;
            var go = Instantiate(prefab, spawnAt, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            go.name = $"{prefab.name}_{_spawned}";

            var member = go.AddComponent<CrewMember>();
            member.Initialise(this, useMech ? 1.0f : 1.25f);
            _members.Add(member);
        }

        internal static Vector3 RandomRing(float radius)
        {
            var a = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
        }
    }
}
