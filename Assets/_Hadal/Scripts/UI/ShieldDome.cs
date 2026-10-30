using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The energy dome over a protected base (docs/30 §3): a translucent half-sphere that breathes and shimmers. Cyan for
    /// a shield, amber for the recovery shield, soft green for newbie protection. Unlit and see-through, so the base stays
    /// visible under it.
    /// </summary>
    public sealed class ShieldDome : MonoBehaviour
    {
        public enum Kind { None, Shield, Recovery, Beginner }

        private static Material _material;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Kind _kind = Kind.None;
        private float _phase;

        public static ShieldDome Create(Transform parent, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ShieldDome";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(radius * 2f, radius * 1.2f, radius * 2f);
            var dome = go.AddComponent<ShieldDome>();
            dome._renderer = go.GetComponent<MeshRenderer>();
            dome._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dome._renderer.receiveShadows = false;
            // The glowing fresnel dome (Resources/Shaders/HadalShieldDome); the plain see-through sphere if it is missing.
            _material ??= new Material(Shader.Find("Hadal/ShieldDome") ?? Shader.Find("Sprites/Default"));
            dome._renderer.sharedMaterial = _material;
            dome._block = new MaterialPropertyBlock();
            dome._phase = Random.value * 6f;
            go.SetActive(false);
            return dome;
        }

        public void Show(Kind kind)
        {
            _kind = kind;
            gameObject.SetActive(kind != Kind.None);
        }

        private Color Tint => _kind switch
        {
            Kind.Recovery => new Color(1f, 0.72f, 0.3f),
            Kind.Beginner => new Color(0.45f, 0.95f, 0.55f),
            _ => new Color(0.25f, 0.9f, 1f)
        };

        private void Update()
        {
            // A slow breath with a quick shimmer on top.
            var t = Time.unscaledTime + _phase;
            var alpha = 0.16f + 0.05f * Mathf.Sin(t * 1.6f) + 0.025f * Mathf.Sin(t * 7.3f);
            var tint = Tint;
            tint.a = alpha;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor("_Color", tint);
            _renderer.SetPropertyBlock(_block);
            transform.Rotate(0f, 6f * Time.unscaledDeltaTime, 0f, Space.Self);
        }
    }
}
