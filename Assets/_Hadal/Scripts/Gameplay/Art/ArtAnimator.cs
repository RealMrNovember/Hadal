using UnityEngine;

namespace Hadal.Gameplay.Art
{
    /// <summary>
    /// Ambient motion for generated art (art/blender). HadalArtImporter adds one to every model part
    /// named ANIM_spin_&lt;axis&gt;_&lt;deg/s&gt;, ANIM_pulse_&lt;hz&gt; or ANIM_sway_&lt;hz&gt;.
    /// Purely cosmetic: no gameplay state, no allocations per frame.
    /// </summary>
    public sealed class ArtAnimator : MonoBehaviour
    {
        public enum Mode
        {
            Spin,
            Pulse,
            Sway
        }

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Mode _mode;
        [SerializeField] private Vector3 _axis = Vector3.up;
        [Tooltip("Spin: degrees per second. Pulse / Sway: cycles per second.")]
        [SerializeField] private float _speed = 1f;
        [SerializeField] private float _swayDegrees = 7f;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Color _baseEmission;
        private Quaternion _restRotation;
        private float _phase;

        public Mode AnimationMode => _mode;
        public Vector3 Axis => _axis;
        public float Speed => _speed;

        public void Configure(Mode mode, Vector3 axis, float speed)
        {
            _mode = mode;
            _axis = axis;
            _speed = speed;
        }

        private void Awake()
        {
            _restRotation = transform.localRotation;
            // Desynchronise identical parts so a row of buildings does not pulse in lockstep.
            _phase = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.61f) % 1f;

            if (_mode != Mode.Pulse)
                return;

            _renderer = GetComponent<Renderer>();
            if (_renderer == null || _renderer.sharedMaterial == null || !_renderer.sharedMaterial.HasProperty(EmissionColorId))
            {
                enabled = false;
                return;
            }

            _baseEmission = _renderer.sharedMaterial.GetColor(EmissionColorId);
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            var t = Time.time;
            switch (_mode)
            {
                case Mode.Spin:
                    transform.Rotate(_axis, _speed * Time.deltaTime, Space.Self);
                    break;

                case Mode.Pulse:
                    var k = 0.55f + 0.45f * Mathf.Sin((t * _speed + _phase) * Mathf.PI * 2f);
                    _renderer.GetPropertyBlock(_block);
                    _block.SetColor(EmissionColorId, _baseEmission * k);
                    _renderer.SetPropertyBlock(_block);
                    break;

                case Mode.Sway:
                    var a = (t * _speed + _phase) * Mathf.PI * 2f;
                    transform.localRotation = _restRotation * Quaternion.Euler(
                        Mathf.Sin(a) * _swayDegrees, 0f, Mathf.Cos(a * 0.7f) * _swayDegrees * 0.6f);
                    break;
            }
        }

        /// <summary>Parses an ANIM_* node name; false for any other name.</summary>
        public static bool TryParse(string nodeName, out Mode mode, out Vector3 axis, out float speed)
        {
            mode = Mode.Spin;
            axis = Vector3.up;
            speed = 0f;

            if (string.IsNullOrEmpty(nodeName) || !nodeName.StartsWith("ANIM_"))
                return false;

            var tag = nodeName.Substring(5);
            var cut = tag.IndexOf("__", System.StringComparison.Ordinal);
            if (cut >= 0)
                tag = tag.Substring(0, cut);

            var parts = tag.Split('_');
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            switch (parts[0])
            {
                case "spin" when parts.Length == 3:
                    mode = Mode.Spin;
                    // Blender Z-up -> Unity Y-up; Blender Y (depth) -> Unity Z.
                    axis = parts[1] switch { "x" => Vector3.right, "y" => Vector3.forward, _ => Vector3.up };
                    return float.TryParse(parts[2], System.Globalization.NumberStyles.Float, inv, out speed);
                case "pulse" when parts.Length == 2:
                    mode = Mode.Pulse;
                    return float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out speed);
                case "sway" when parts.Length == 2:
                    mode = Mode.Sway;
                    return float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out speed);
                default:
                    return false;
            }
        }
    }
}
