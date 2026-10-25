using UnityEngine;

namespace Hadal.Gameplay.Crew
{
    /// <summary>
    /// One crew member's routine: pick a building, walk to its work spot while steering around
    /// the other buildings, work there for a while, repeat. Animation states are clip names
    /// (see HadalCharacterSetup), so this only cross-fades between "Walk" and a work clip.
    /// </summary>
    public sealed class CrewMember : MonoBehaviour
    {
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
        private float _workUntil;
        private float _nextWorkClip;
        private bool _walking;

        public bool IsWalking => _walking;

        public void Initialise(CrewView view, float speed)
        {
            _view = view;
            _speed = speed * Random.Range(0.85f, 1.15f);
            _animator = GetComponentInChildren<Animator>();
            StartWorking(Random.Range(0.5f, 3f));
        }

        private void Update()
        {
            if (_view == null)
                return;

            if (_walking)
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
            _walking = true;
            Play(Walk);
        }

        private void WalkStep(float dt)
        {
            var position = transform.position;
            var toTarget = _target - position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= ArriveDistance)
            {
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
            _walking = false;
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
