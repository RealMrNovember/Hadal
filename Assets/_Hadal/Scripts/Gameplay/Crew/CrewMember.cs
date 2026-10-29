using UnityEngine;

namespace Hadal.Gameplay.Crew
{
    /// <summary>
    /// One crew member's routine: pick a building, walk to its work spot while steering around
    /// the other buildings, work there for a while, repeat. Animation states are clip names
    /// (see HadalCharacterSetup), so this only cross-fades between "Walk" and a work clip.
    /// A long trip (beyond <see cref="ChallengerFleet.RideDistance"/>) takes a Challenger when one is
    /// free: the crew member walks to where it lands, rides in the pilot sphere and walks the rest.
    /// </summary>
    public sealed class CrewMember : MonoBehaviour, IChallengerRider
    {
        private enum Phase
        {
            Working,
            Walking,
            WalkingToRide,
            WaitingForRide,
            Riding
        }

        private const float RideWaitSeconds = 25f;

        private const float BuildingRadius = 2.6f;   // pad radius + margin (art/blender pad = 2.3 m)
        private const float WorkSpotDistance = 2.9f;
        private const float ArriveDistance = 0.25f;
        private const float TurnSpeed = 540f;

        private static readonly int Walk = Animator.StringToHash("Walk");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int[] WorkClips =
        {
            Animator.StringToHash("Idle"), Animator.StringToHash("Yes"), Animator.StringToHash("Wave"),
            Animator.StringToHash("Punch"), Animator.StringToHash("Pickup")
        };

        private CrewView _view;
        private Animator _animator;
        private float _speed;
        private Vector3 _target;
        private Vector3 _targetBuilding;
        private Vector3 _rideTarget;
        private float _workUntil;
        private float _nextWorkClip;
        private Phase _phase;
        private bool _canRide;
        private ChallengerVehicle _ride;
        private float _rideWaitUntil;
        private bool _destroyed;

        public bool IsWalking => _phase is Phase.Walking or Phase.WalkingToRide;
        public bool IsRiding => _phase == Phase.Riding;
        public Transform Body => _destroyed || this == null ? null : transform;

        public void Initialise(CrewView view, float speed, bool canRide = false)
        {
            _view = view;
            _canRide = canRide;
            _speed = speed * Random.Range(0.85f, 1.15f);
            _animator = GetComponentInChildren<Animator>();
            StartWorking(Random.Range(0.5f, 3f));
        }

        private void Update()
        {
            if (_view == null)
                return;

            if (_phase == Phase.Riding)
                return;   // the Challenger carries us
            if (_phase == Phase.WaitingForRide)
                WaitForRide();
            else if (IsWalking)
                WalkStep(Time.deltaTime);
            else if (Time.time >= _workUntil)
                PickNextJob();
            else if (Time.time >= _nextWorkClip)
                PlayWorkClip();
        }

        private void PickNextJob()
        {
            var buildings = _view.BuildingPositions;
            if (buildings.Count == 0)
            {
                StartWorking(2f);
                return;
            }

            _targetBuilding = buildings[Random.Range(0, buildings.Count)];
            // Work on the side that faces the base centre; the reactor itself is worked from any side.
            var outward = new Vector3(_targetBuilding.x, 0f, _targetBuilding.z);
            var side = outward.sqrMagnitude > 1f
                ? Quaternion.Euler(0f, Random.Range(-50f, 50f), 0f) * -outward.normalized
                : CrewView.RandomRing(1f);
            _target = _targetBuilding + side * WorkSpotDistance;
            _target.y = 0f;
            _phase = Phase.Walking;
            TryTakeChallenger();
            Play(Walk);
        }

        private void TryTakeChallenger()
        {
            var fleet = _canRide ? _view.Fleet : null;
            if (fleet == null)
                return;

            var from = transform.position;
            var trip = _target - from;
            trip.y = 0f;
            if (trip.magnitude < fleet.RideDistance)
                return;

            // It lands a couple of steps ahead and sets down short of the work spot.
            var direction = trip.normalized;
            var pickup = new Vector3(from.x, 0f, from.z) + direction * 1.6f;
            var dropoff = _target - direction * 1.4f;
            if (!fleet.TryBook(this, pickup, dropoff, out _ride))
                return;

            _phase = Phase.WalkingToRide;
            _rideTarget = _target;
            _target = pickup - direction * 0.75f;   // beside the viewport, not under the hull
            _rideWaitUntil = Time.time + RideWaitSeconds;
        }

        private void WaitForRide()
        {
            if (_ride != null && _ride.ReadyToBoard)
            {
                _ride.Board();
                return;
            }

            if (_ride != null)
                FaceTowards(_ride.transform.position - transform.position, Time.deltaTime);
            if (_ride == null || Time.time >= _rideWaitUntil)
            {
                // Nobody came: walk after all.
                if (_ride != null)
                    _ride.Cancel(this);
                _ride = null;
                _target = _rideTarget;
                _phase = Phase.Walking;
                Play(Walk);
            }
        }

        public void OnBoarded()
        {
            _phase = Phase.Riding;
            Play(Idle);
        }

        public void OnArrived(Vector3 at)
        {
            _ride = null;
            _target = _rideTarget;
            _phase = Phase.Walking;
            Play(Walk);
        }

        private void OnDestroy()
        {
            _destroyed = true;
            if (_ride != null)
                _ride.Cancel(this);
        }

        private void WalkStep(float dt)
        {
            var position = transform.position;
            var toTarget = _target - position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= ArriveDistance)
            {
                if (_phase == Phase.WalkingToRide)
                {
                    _phase = Phase.WaitingForRide;
                    Play(Idle);
                    return;
                }

                FaceTowards(_targetBuilding - position, 1f);
                StartWorking(Random.Range(4f, 9f));
                return;
            }

            var direction = toTarget.normalized;
            // Steer around buildings we pass by (not the one we are heading to).
            foreach (var building in _view.BuildingPositions)
            {
                if ((building - _targetBuilding).sqrMagnitude < 0.01f)
                    continue;

                var away = position - building;
                away.y = 0f;
                var distance = away.magnitude;
                if (distance > BuildingRadius + 1.2f || distance < 0.001f)
                    continue;

                var push = 1f - Mathf.Clamp01((distance - BuildingRadius) / 1.2f);
                var tangent = Vector3.Cross(Vector3.up, away / distance);
                if (Vector3.Dot(tangent, direction) < 0f)
                    tangent = -tangent;
                direction += (away / distance * 0.8f + tangent) * push * 1.5f;
            }

            direction.y = 0f;
            direction.Normalize();
            FaceTowards(direction, dt);
            transform.position = position + transform.forward * (_speed * dt);
        }

        private void StartWorking(float seconds)
        {
            _phase = Phase.Working;
            _workUntil = Time.time + seconds;
            _nextWorkClip = Time.time;
            Play(Idle);
        }

        private void PlayWorkClip()
        {
            _nextWorkClip = Time.time + Random.Range(1.5f, 3.5f);
            var clip = WorkClips[Random.Range(0, WorkClips.Length)];
            if (_animator != null && _animator.HasState(0, clip))
                Play(clip);
        }

        private void Play(int state)
        {
            if (_animator != null && _animator.isActiveAndEnabled && _animator.HasState(0, state))
                _animator.CrossFadeInFixedTime(state, 0.2f);
        }

        private void FaceTowards(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f)
                return;

            var wanted = Quaternion.LookRotation(direction);
            transform.rotation = dt >= 1f ? wanted : Quaternion.RotateTowards(transform.rotation, wanted, TurnSpeed * dt);
        }
    }
}
