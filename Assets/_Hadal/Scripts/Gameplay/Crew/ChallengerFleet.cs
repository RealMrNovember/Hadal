using System.Collections.Generic;
using Hadal.Gameplay.Buildings;
using UnityEngine;

namespace Hadal.Gameplay.Crew
{
    /// <summary>
    /// The base's Challenger submersibles: a small pool that lives at the Challenger Hangars
    /// (catalog id drone_factory). Parked Challengers hover by their hangar and now and then make a
    /// salvage dive to the seabed; crew members heading for a distant building book one, it lands
    /// beside them, carries them across and sets them down by their destination. Cosmetic only:
    /// no server state, nothing allocated per frame once the pool exists.
    /// </summary>
    public sealed class ChallengerFleet : MonoBehaviour
    {
        public const string HangarId = "drone_factory";

        [SerializeField] private GameObject _prefab;
        [SerializeField] private BuildingStateView _buildings;
        [Tooltip("Challengers per hangar, plus one; capped here.")]
        [SerializeField] private int _maxVehicles = 3;
        [Tooltip("Crew walk anything shorter than this (metres); longer trips take a Challenger when one is free.")]
        [SerializeField] private float _rideDistance = 12f;
        [SerializeField] private float _refreshSeconds = 1.5f;
        [SerializeField] private Vector2 _diveEverySeconds = new(18f, 40f);

        private readonly List<ChallengerVehicle> _vehicles = new();
        private readonly List<Vector3> _hangars = new();
        private float _nextRefresh;
        private float _nextDive;

        public float RideDistance => _rideDistance;
        public IReadOnlyList<ChallengerVehicle> Vehicles => _vehicles;

        public void Configure(GameObject prefab, BuildingStateView buildings)
        {
            _prefab = prefab;
            _buildings = buildings;
        }

        private void Update()
        {
            if (Time.time >= _nextRefresh)
            {
                _nextRefresh = Time.time + _refreshSeconds;
                if (_buildings != null)
                {
                    _buildings.GetBuildingPositions(HangarId, _hangars);
                    Sync(_hangars);
                }
            }

            if (Time.time >= _nextDive)
            {
                _nextDive = Time.time + Random.Range(_diveEverySeconds.x, _diveEverySeconds.y);
                SendOnDive();
            }
        }

        /// <summary>Matches the pool to the hangars: one Challenger per hangar plus one, each parked at a hangar.</summary>
        public void Sync(IReadOnlyList<Vector3> hangars)
        {
            var wanted = hangars.Count == 0 || _prefab == null ? 0 : Mathf.Min(_maxVehicles, hangars.Count + 1);
            while (_vehicles.Count < wanted)
                Spawn(hangars);

            for (var i = _vehicles.Count - 1; i >= wanted; i--)
            {
                // Only parked ones leave; one carrying a passenger finishes the trip first.
                if (!_vehicles[i].IsFree)
                    continue;
                Destroy(_vehicles[i].gameObject);
                _vehicles.RemoveAt(i);
            }

            for (var i = 0; i < _vehicles.Count && hangars.Count > 0; i++)
                _vehicles[i].SetHome(HomeFor(i, hangars));
        }

        /// <summary>Books the free Challenger closest to <paramref name="pickup"/>; false when none is free.</summary>
        public bool TryBook(IChallengerRider rider, Vector3 pickup, Vector3 dropoff, out ChallengerVehicle vehicle)
        {
            vehicle = null;
            var best = float.MaxValue;
            foreach (var candidate in _vehicles)
            {
                if (candidate == null || !candidate.IsFree)
                    continue;
                var d = (candidate.transform.position - pickup).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    vehicle = candidate;
                }
            }

            return vehicle != null && vehicle.Book(rider, pickup, dropoff);
        }

        private void SendOnDive()
        {
            // Keep at least one at the hangar for the crew.
            var free = 0;
            foreach (var v in _vehicles)
                if (v != null && v.IsFree)
                    free++;
            if (free < 2)
                return;

            foreach (var v in _vehicles)
            {
                if (v == null || !v.IsFree)
                    continue;
                var a = Random.Range(0f, Mathf.PI * 2f);
                // Wreck fields beyond the built rings (ring 4 is 22 m out).
                v.Dive(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(24f, 32f));
                return;
            }
        }

        private void Spawn(IReadOnlyList<Vector3> hangars)
        {
            var index = _vehicles.Count;
            var go = Instantiate(_prefab, transform);
            go.name = $"Challenger_{index + 1}";
            var vehicle = go.AddComponent<ChallengerVehicle>();
            vehicle.Initialise(HomeFor(index, hangars), index * 137f);
            _vehicles.Add(vehicle);
        }

        /// <summary>Hover spot for vehicle <paramref name="index"/>: above and beside its hangar, staggered in height.</summary>
        public static Vector3 HomeFor(int index, IReadOnlyList<Vector3> hangars)
        {
            var hangar = hangars[index % hangars.Count];
            var a = index * 2.4f + 0.6f;
            return new Vector3(hangar.x + Mathf.Cos(a) * 2.6f, 3.6f + (index % 3) * 0.9f, hangar.z + Mathf.Sin(a) * 2.6f);
        }
    }
}
