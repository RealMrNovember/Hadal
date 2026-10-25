using UnityEngine;

namespace Hadal.Gameplay.Art
{
    /// <summary>
    /// Procedural life for static props (TRELLIS meshes have no rig): circling swimmers, pulsing
    /// jellyfish, seabed crawlers, distant patrols and swaying plants. Cosmetic only, no physics,
    /// no allocations per frame. Each instance is desynchronised by its phase.
    /// </summary>
    public sealed class AmbientMotion : MonoBehaviour
    {
        public enum Mode
        {
            Swim,
            PulseSwim,
            Crawl,
            Patrol,
            Sway
        }

        [SerializeField] private Mode _mode;
        [SerializeField] private Vector3 _centre;
        [SerializeField] private float _radius = 20f;
        [SerializeField] private float _height = 4f;
        [Tooltip("Metres per second along the circle.")]
        [SerializeField] private float _speed = 1f;
        [SerializeField] private bool _clockwise;
        [Tooltip("Turns the model so its nose points along the path (depends on how the mesh was authored).")]
        [SerializeField] private float _yawOffset;
        [SerializeField] private float _phase;

        private Quaternion _rest;
        private Vector3 _restScale;
        private float _angle;

        public Mode MotionMode => _mode;
        public Vector3 Centre => _centre;
        public float Radius => _radius;
        public float Height => _height;

        public void Configure(Mode mode, Vector3 centre, float radius, float height, float speed, bool clockwise, float yawOffset, float phase)
        {
            _mode = mode;
            _centre = centre;
            _radius = Mathf.Max(0.1f, radius);
            _height = height;
            _speed = speed;
            _clockwise = clockwise;
            _yawOffset = yawOffset;
            _phase = phase;
            _angle = phase * Mathf.PI * 2f;
            _restScale = transform.localScale;
        }

        private void Awake()
        {
            _rest = transform.localRotation;
            _restScale = transform.localScale;
            _angle = _phase * Mathf.PI * 2f;
        }

        private void Update() => Step(Time.time, Time.deltaTime);

        /// <summary>Advances the motion; public so tests can drive it without a frame loop.</summary>
        public void Step(float time, float deltaTime)
        {
            var t = time + _phase * 10f;
            switch (_mode)
            {
                case Mode.Sway:
                    transform.localRotation = _rest * Quaternion.Euler(Mathf.Sin(t * 0.9f) * 4f, 0f, Mathf.Cos(t * 0.6f) * 3f);
                    return;

                case Mode.PulseSwim:
                    // Jellyfish: slow drift, strong bob, bell squashes and stretches with each pulse.
                    Circle(deltaTime, Mathf.Sin(t * 0.8f) * 0.9f, faceAlongPath: false);
                    var pulse = Mathf.Sin(t * 2.2f) * 0.08f;
                    transform.localScale = new Vector3(_restScale.x * (1f - pulse), _restScale.y * (1f + pulse), _restScale.z * (1f - pulse));
                    return;

                case Mode.Crawl:
                    Circle(deltaTime, 0f, faceAlongPath: true);
                    transform.rotation *= Quaternion.Euler(0f, Mathf.Sin(t * 5f) * 3f, Mathf.Sin(t * 7f) * 1.5f);
                    return;

                case Mode.Patrol:
                    Circle(deltaTime, Mathf.Sin(t * 0.25f) * 1.2f, faceAlongPath: true);
                    transform.rotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.4f) * 4f);
                    return;

                default:
                    Circle(deltaTime, Mathf.Sin(t * 0.7f) * 0.5f, faceAlongPath: true);
                    transform.rotation *= Quaternion.Euler(0f, Mathf.Sin(t * 3f) * 6f, 0f); // tail wag
                    return;
            }
        }

        private void Circle(float deltaTime, float bob, bool faceAlongPath)
        {
            var direction = _clockwise ? -1f : 1f;
            _angle += direction * _speed / _radius * deltaTime;
            var offset = new Vector3(Mathf.Cos(_angle), 0f, Mathf.Sin(_angle)) * _radius;
            transform.position = _centre + offset + Vector3.up * (_height + bob);

            if (!faceAlongPath)
                return;

            var tangent = new Vector3(-Mathf.Sin(_angle), 0f, Mathf.Cos(_angle)) * direction;
            transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, _yawOffset, 0f);
        }
    }
}
