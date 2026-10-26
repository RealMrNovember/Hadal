using Hadal.Core.Catalog;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Renders one hero's animated character model into a texture for the hero screen: a private camera
    /// and lights far below the base, so nothing else is in shot. Drag turns the model, a tap waves.
    /// The camera only runs while the screen shows it.
    /// </summary>
    public sealed class HeroStage : MonoBehaviour
    {
        private static readonly Vector3 Origin = new(0f, -2000f, 0f);

        private RenderTexture _texture;
        private Camera _camera;
        private GameObject _model;
        private Animator _animator;
        private string _shown = string.Empty;
        private string _idle = "Idle";
        private float _spin;
        private float _backToIdleAt = -1f;

        public Texture Show(string modelId)
        {
            EnsureRig();
            if (_shown != modelId)
                Load(modelId);
            _camera.enabled = _model != null;
            return _model != null ? _texture : null;
        }

        public void Hide()
        {
            if (_camera != null)
                _camera.enabled = false;
        }

        public void Turn(float degrees) => _spin -= degrees;

        public void Wave()
        {
            if (_animator == null || !_animator.HasState(0, Animator.StringToHash("Wave")))
                return;
            _animator.CrossFadeInFixedTime("Wave", 0.15f);
            _backToIdleAt = Time.time + 2.2f;
        }

        private void EnsureRig()
        {
            if (_camera != null)
                return;

            _texture = new RenderTexture(512, 640, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2, name = "HeroStage" };
            var cameraGo = new GameObject("HeroStageCamera");
            cameraGo.transform.SetParent(transform, false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.03f, 0.09f, 0.12f, 1f);
            _camera.fieldOfView = 28f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 30f;
            _camera.enabled = false;

            Light(new Vector3(-1.6f, 2.6f, -2.2f), new Color(1f, 0.95f, 0.88f), 4f);
            Light(new Vector3(1.8f, 1.4f, -1.2f), new Color(0.45f, 0.8f, 1f), 2.5f);
            Light(new Vector3(0.2f, 2.2f, 1.8f), new Color(0.3f, 0.9f, 1f), 3f);
        }

        private void Light(Vector3 offset, Color color, float intensity)
        {
            var go = new GameObject("HeroStageLight");
            go.transform.SetParent(transform, false);
            go.transform.position = Origin + offset;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = 8f;
        }

        private void Load(string modelId)
        {
            if (_model != null)
                Destroy(_model);
            _model = null;
            _animator = null;
            _shown = modelId;
            _spin = 0f;

            var prefab = CharacterLibrary.Instance != null ? CharacterLibrary.Instance.Model(modelId) : null;
            if (prefab == null)
                return;

            _model = Instantiate(prefab, Origin, Quaternion.Euler(0f, 180f, 0f), transform);
            _model.name = "HeroModel_" + modelId;
            foreach (var behaviour in _model.GetComponentsInChildren<MonoBehaviour>())
                behaviour.enabled = false; // gameplay scripts (walking crew) must not drive the stage model

            _animator = _model.GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.enabled = true;
                _idle = _animator.HasState(0, Animator.StringToHash("Idle")) ? "Idle" : "Flying_Idle";
                _animator.Play(_idle, 0, Random.value);
            }

            // Frame the model: full height with a little headroom, slightly from above.
            var bounds = new Bounds(Origin + Vector3.up, Vector3.one);
            var first = true;
            foreach (var r in _model.GetComponentsInChildren<Renderer>())
            {
                if (first)
                {
                    bounds = r.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            var height = Mathf.Max(0.5f, bounds.size.y);
            var distance = height * 0.8f / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var target = bounds.center + Vector3.up * height * 0.02f;
            _camera.transform.position = target + new Vector3(0f, height * 0.18f, -distance);
            _camera.transform.LookAt(target);
        }

        private void Update()
        {
            if (_model == null || _camera == null || !_camera.enabled)
                return;

            _model.transform.rotation = Quaternion.Euler(0f, 180f + _spin + Mathf.Sin(Time.time * 0.6f) * 8f, 0f);
            if (_backToIdleAt > 0f && Time.time >= _backToIdleAt && _animator != null)
            {
                _backToIdleAt = -1f;
                _animator.CrossFadeInFixedTime(_idle, 0.25f);
            }
        }

        private void OnDestroy()
        {
            if (_texture != null)
                _texture.Release();
        }
    }

    /// <summary>Drag to turn the hero, tap to wave.</summary>
    public sealed class HeroStageInput : MonoBehaviour, IDragHandler, IPointerClickHandler
    {
        public HeroStage Stage;

        public void OnDrag(PointerEventData eventData) => Stage?.Turn(eventData.delta.x * 0.5f);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
                Stage?.Wave();
        }
    }
}

namespace Hadal.UI
{
    /// <summary>
    /// A painted portrait that feels alive: a slow drift and zoom over the painting (like a camera
    /// breathing in front of it) and a soft light sweep. Used where a matching 3D model does not exist.
    /// </summary>
    public sealed class LivingPortrait : MonoBehaviour
    {
        private UnityEngine.UI.RawImage _image;
        private UnityEngine.UI.Image _sheen;
        private float _seed;

        public static void Attach(UnityEngine.UI.RawImage image)
        {
            var living = image.gameObject.AddComponent<LivingPortrait>();
            living._image = image;
            living._seed = UnityEngine.Random.value * 10f;

            var sheen = UiKit.Box("Sheen", image.transform, new Color(0.4f, 0.9f, 1f, 0f));
            sheen.raycastTarget = false;
            ((RectTransform)sheen.transform).Place(0, 0, 0.35f, 1);
            living._sheen = sheen;
        }

        private void Update()
        {
            if (_image == null)
                return;

            var t = Time.time + _seed;
            var size = 0.9f + 0.05f * Mathf.Sin(t * 0.45f);
            var cx = 0.5f + 0.025f * Mathf.Sin(t * 0.23f);
            var cy = 0.52f + 0.02f * Mathf.Cos(t * 0.31f);
            _image.uvRect = new Rect(cx - size / 2f, cy - size / 2f, size, size);

            // A light band crosses the painting every few seconds.
            var phase = Mathf.Repeat(t * 0.12f, 1f);
            var x = Mathf.Lerp(-0.4f, 1.1f, phase);
            ((RectTransform)_sheen.transform).Place(x, 0f, x + 0.3f, 1f);
            _sheen.color = new Color(0.4f, 0.9f, 1f, 0.10f * Mathf.Sin(phase * Mathf.PI));
        }
    }
}
