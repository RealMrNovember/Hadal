using UnityEngine;

namespace Hadal.Core.Art
{
    /// <summary>
    /// Whole-body motion of a map creature on top of the shader's tail, tentacle and claw waves: fish loop around their
    /// spot turning toward where they swim, walkers wobble on their steps, jellies rise and fall; angler lures bob and
    /// glow. <see cref="Attack"/> lunges, <see cref="Hit"/> flashes, <see cref="Die"/> sinks it away. Moves only the body
    /// inside its own root, so whoever places the root (the map) never fights the animation. No allocations per frame.
    /// </summary>
    public sealed class CreatureMotion : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private CreatureProfile _profile;
        private Transform _body;
        private Transform _lure;
        private Vector3 _lureRest;
        private float _lureSize;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private float _height;
        private float _phase;
        private float _angle;
        private float _yaw;
        private float _lunge = -1f;
        private float _flash;
        private float _dying = -1f;
        private bool _flashing;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _restScale;

        public CreatureProfile Profile => _profile;
        public Transform Body => _body;
        public bool Dying => _dying >= 0f;

        /// <summary>Part of a school (<see cref="MapWildlife"/>): the school steers, the body does not loop on its own.</summary>
        public bool Schooling { get; set; }

        /// <summary>For tests and for models without a profile (the rigged crawler and siren): lunge and flash only.</summary>
        public float LungeOffset { get; private set; }

        public void Configure(CreatureProfile profile, Transform body, float scale, Vector3 boundsMin, Vector3 boundsSize, Material lure)
        {
            _profile = profile;
            _body = body;
            _height = profile != null ? profile.MapHeight : Mathf.Max(1f, boundsSize.y * scale);
            _renderers = body.GetComponentsInChildren<Renderer>();
            _restPosition = body.localPosition;
            _restRotation = body.localRotation;
            _restScale = body.localScale;
            var p = transform.position;
            _phase = Mathf.Repeat(p.x * 0.137f + p.z * 0.113f + GetInstanceID() * 0.0007f, 1f) * Mathf.PI * 2f;
            _angle = _phase;
            _yaw = _phase * Mathf.Rad2Deg;

            if (profile is { HasLure: true } && lure != null)
            {
                var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var collider = bulb.GetComponent<Collider>();
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
                bulb.name = "Lure";
                bulb.transform.SetParent(body, false);
                _lureRest = boundsMin + Vector3.Scale(profile.Lure, boundsSize);
                _lureSize = profile.LureSize * boundsSize.y;
                bulb.transform.localPosition = _lureRest;
                bulb.transform.localScale = Vector3.one * _lureSize;
                var r = bulb.GetComponent<Renderer>();
                r.sharedMaterial = lure;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, profile.LureColor);
                r.SetPropertyBlock(block);
                _lure = bulb.transform;
            }

            Step(0f, 0f);
        }

        /// <summary>For a model without a profile: the same lunge / flash / death on its own transform.</summary>
        public void ConfigureSimple(Transform body, float height)
        {
            Configure(null, body, 1f, Vector3.zero, new Vector3(1f, height, 1f), null);
        }

        /// <summary>The creature lunges at the attacker (a short dash forward and back).</summary>
        public void Attack() => _lunge = 0f;

        /// <summary>A hit: the body flashes and recoils.</summary>
        public void Hit()
        {
            _flash = 1f;
            if (_lunge < 0f)
                _lunge = 0.45f;
        }

        /// <summary>Beaten: flashes, sinks and shrinks away over a second and a half.</summary>
        public void Die()
        {
            if (_dying < 0f)
                _dying = 0f;
            _flash = 1f;
        }

        private void Update() => Step(Time.time, Time.deltaTime);

        /// <summary>Advances the motion; public so tests can drive it without a frame loop.</summary>
        public void Step(float time, float deltaTime)
        {
            if (_body == null)
                return;
            var t = time + _phase;
            var h = _height;
            var position = Vector3.zero;
            var rotation = Quaternion.identity;

            switch (_profile?.Gait)
            {
                case CreatureGait.Swim when Schooling:
                    // In a school the group sets the heading; the body only weaves a little.
                    position = Vector3.up * (Mathf.Sin(t * 1.1f) * h * 0.08f);
                    rotation = Quaternion.Euler(0f, Mathf.Sin(t * 1.7f) * 7f, Mathf.Sin(t * 1.7f + 0.5f) * 5f);
                    break;
                case CreatureGait.Swim:
                {
                    // A slow loop around the spot; the body turns toward where it swims and rolls into the turn.
                    var radius = h * 0.22f;
                    _angle += deltaTime * 0.35f;
                    position = new Vector3(Mathf.Cos(_angle) * radius, h * 0.75f + Mathf.Sin(t * 0.8f) * h * 0.05f, Mathf.Sin(_angle) * radius);
                    var heading = new Vector3(-Mathf.Sin(_angle), 0f, Mathf.Cos(_angle));
                    var target = Quaternion.LookRotation(heading) * Quaternion.Euler(Mathf.Sin(t * 0.8f + 1f) * 4f, 0f, -8f);
                    rotation = deltaTime > 0f ? Quaternion.Slerp(_body.localRotation, target, 1f - Mathf.Exp(-3f * deltaTime)) : target;
                    break;
                }
                case CreatureGait.Crawl:
                {
                    // Short shuffles, swinging the heading a little; each step rocks the shell.
                    _yaw = _phase * Mathf.Rad2Deg + Mathf.Sin(t * 0.18f) * 30f;
                    var step = Mathf.Sin(t * 5.5f);
                    var forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
                    position = forward * (Mathf.Sin(t * 0.35f) * h * 0.18f) + Vector3.up * (Mathf.Abs(step) * h * 0.025f);
                    rotation = Quaternion.Euler(Mathf.Sin(t * 11f) * 1.5f, _yaw, step * 3.5f);
                    break;
                }
                case CreatureGait.Drift:
                    // Rises and sinks with its pulse, turning slowly.
                    _yaw += deltaTime * 6f;
                    position = Vector3.up * (h * 0.55f + Mathf.Sin(t * 0.55f) * h * 0.08f);
                    rotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 4f, _yaw, Mathf.Cos(t * 0.6f) * 4f);
                    break;
                default:
                    rotation = _body.localRotation;
                    break;
            }

            // Lunge: out and back along the body's facing.
            LungeOffset = 0f;
            if (_lunge >= 0f)
            {
                _lunge += deltaTime / 0.7f;
                LungeOffset = _lunge < 1f ? Mathf.Sin(_lunge * Mathf.PI) * h * 0.35f : 0f;
                if (_lunge >= 1f)
                    _lunge = -1f;
            }

            position += rotation * Vector3.forward * LungeOffset;

            var size = 1f;
            if (_dying >= 0f)
            {
                _dying += deltaTime / 1.5f;
                var k = Mathf.Clamp01(_dying);
                size = 1f - k * k;
                position += Vector3.down * (k * h * 0.4f);
                rotation *= Quaternion.Euler(0f, 0f, k * 70f);
            }

            if (_profile != null)
            {
                _body.localPosition = position;
                _body.localRotation = rotation;
            }
            else
            {
                // A model without a profile keeps its own pose; only the lunge and the death move it.
                _body.localPosition = _restPosition + _restRotation * Vector3.forward * LungeOffset + (position.y < 0f ? position : Vector3.zero);
            }

            if (_dying >= 0f)
                _body.localScale = _restScale * Mathf.Max(0.001f, size);

            if (_lure != null)
            {
                _lure.localPosition = _lureRest + Vector3.up * (Mathf.Sin(t * 2.3f) * _lureSize * 0.6f) + Vector3.right * (Mathf.Sin(t * 1.3f) * _lureSize * 0.3f);
                _lure.localScale = Vector3.one * (_lureSize * (0.85f + 0.3f * Mathf.Max(0f, Mathf.Sin(t * 3.1f))));
            }

            UpdateFlash(deltaTime);
        }

        private void UpdateFlash(float deltaTime)
        {
            if (_flash <= 0f && !_flashing)
                return;
            _block ??= new MaterialPropertyBlock();
            _flash = Mathf.Max(0f, _flash - deltaTime * 2.5f);
            foreach (var r in _renderers)
            {
                if (r == null)
                    continue;
                if (_flash <= 0f)
                {
                    r.SetPropertyBlock(null); // back on the shared material (SRP batcher)
                    continue;
                }

                r.GetPropertyBlock(_block);
                if (_profile != null)
                    _block.SetFloat(FlashId, _flash * 0.8f);
                else
                    _block.SetColor(BaseColorId, Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), _flash));
                r.SetPropertyBlock(_block);
            }

            _flashing = _flash > 0f;
        }
    }
}
