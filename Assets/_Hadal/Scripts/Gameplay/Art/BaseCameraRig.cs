using UnityEngine;
using UnityEngine.EventSystems;

namespace Hadal.Gameplay.Art
{
    /// <summary>
    /// 3/4 top-down camera over the base: one-finger drag (or mouse drag) pans, pinch (or wheel)
    /// zooms. The camera is a child looking at this rig's pivot from <see cref="_pitch"/> degrees.
    /// </summary>
    public sealed class BaseCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _pitch = 52f;
        [SerializeField] private float _yaw = 35f;
        [SerializeField] private float _distance = 58f;
        [SerializeField] private float _minDistance = 14f;
        [SerializeField] private float _maxDistance = 95f;
        [SerializeField] private float _panLimit = 40f;

        private Vector3 _lastPointer;
        private bool _dragging;
        private float _lastPinch;

        // A gesture that starts on a menu (a panel, the hero model, a list) belongs to the UI: the world stays put.
        private bool _gestureOnUi;

        public Camera Camera => _camera;

        // A glide requested by the UI (tapped building); any finger on the world cancels it.
        private Vector3? _glideTo;
        private float _glideDistance;

        private void OnEnable()
        {
            Hadal.Core.State.CameraFocus.Requested += OnFocus;
            Hadal.Core.State.CameraFocus.SettleRequested += OnSettle;
        }

        private void OnDisable()
        {
            Hadal.Core.State.CameraFocus.Requested -= OnFocus;
            Hadal.Core.State.CameraFocus.SettleRequested -= OnSettle;
        }

        /// <summary>Starts higher and glides down to the current view (the genre's soft arrival on the city).</summary>
        private void OnSettle()
        {
            var keep = _distance;
            _distance = Mathf.Min(_maxDistance, keep * 1.45f);
            _glideTo = transform.position;
            _glideDistance = keep;
        }

        private void OnFocus(Vector3 world, float? distance)
        {
            _glideTo = Vector3.ClampMagnitude(new Vector3(world.x, 0f, world.z), _panLimit);
            _glideDistance = distance ?? _distance;
        }

        public void Configure(Camera camera, float distance)
        {
            _camera = camera;
            _distance = distance;
            Apply();
        }

        private void Start() => Apply();

        private void LateUpdate()
        {
            if (_camera == null || Hadal.Core.State.GameMode.InWorld)
                return;

            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                _gestureOnUi = PointerOverUi();
            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
                _gestureOnUi = false;

            if (_gestureOnUi)
            {
                _dragging = false;
                Glide();
                Apply();
                return;
            }

            Glide();

            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                var pinch = Vector2.Distance(a.position, b.position);
                if (b.phase == TouchPhase.Began)
                    _lastPinch = pinch;

                _distance *= _lastPinch / Mathf.Max(1f, pinch);
                _lastPinch = pinch;
                _dragging = false;
                _glideTo = null; // the player takes the camera back
            }
            else if (Input.GetMouseButton(0))
            {
                // Touch 0 is reported as mouse button 0 on mobile.
                var pointer = Input.mousePosition;
                if (_dragging)
                {
                    var delta = pointer - _lastPointer;
                    // A real drag (not the lift of the tap that asked for the glide) takes the camera back.
                    if (delta.sqrMagnitude > 16f)
                        _glideTo = null;
                    if (_glideTo == null)
                        Pan(delta);
                }

                _lastPointer = pointer;
                _dragging = true;
            }
            else
            {
                _dragging = false;
            }

            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
                _distance *= 1f - wheel * 0.1f;

            Apply();
        }

        private void Glide()
        {
            if (_glideTo is not { } target)
                return;
            var k = 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, target, k);
            _distance = Mathf.Lerp(_distance, _glideDistance, k);
            if ((transform.position - target).sqrMagnitude < 0.0004f && Mathf.Abs(_distance - _glideDistance) < 0.02f)
                _glideTo = null;
        }

        private static bool PointerOverUi()
        {
            var events = EventSystem.current;
            if (events == null)
                return false;

            return Input.touchCount > 0
                ? events.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : events.IsPointerOverGameObject();
        }

        private void Pan(Vector3 screenDelta)
        {
            // World units per pixel grow with distance so panning feels the same at any zoom.
            var scale = _distance / Mathf.Max(1f, Screen.height) * 1.6f;
            var yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            var move = yawRotation * new Vector3(-screenDelta.x, 0f, -screenDelta.y) * scale;
            var p = transform.position + move;
            transform.position = Vector3.ClampMagnitude(new Vector3(p.x, 0f, p.z), _panLimit);
        }

        private void Apply()
        {
            if (_camera == null)
                return;

            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(transform.position - rotation * Vector3.forward * _distance, rotation);
        }
    }
}
