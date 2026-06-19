using System.Collections;
using HADAL.Shared.DTOs;
using UnityEngine;

namespace Hadal.Gameplay.Grid
{
    /// <summary>
    /// Visual placement ghost — pending tint while awaiting server ack; shake on rejection.
    /// </summary>
    public sealed class PreviewGhost : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _ghostTint = new(0.2f, 0.95f, 0.82f, 0.45f);
        [SerializeField] private Color _pendingTint = new(0.95f, 0.85f, 0.2f, 0.55f);
        [SerializeField] private Vector3 _defaultScale = new(1.2f, 0.6f, 1.2f);
        [SerializeField] private float _shakeStrength = 0.08f;

        private MaterialPropertyBlock _propertyBlock;
        private GameObject _fallbackMesh;
        private Coroutine _shakeRoutine;
        private Vector3 _baseLocalPosition;
        private bool _isPending;

        public GridSlot CurrentSlot { get; private set; }
        public bool IsPending => _isPending;

        public void EnsureVisible()
        {
            if (_visualRoot == null && _fallbackMesh == null)
            {
                _fallbackMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _fallbackMesh.name = "PreviewGhostMesh";
                _fallbackMesh.transform.SetParent(transform, false);
                _fallbackMesh.transform.localScale = _defaultScale;
                Destroy(_fallbackMesh.GetComponent<Collider>());
                _renderers = _fallbackMesh.GetComponentsInChildren<Renderer>();
                _baseLocalPosition = _fallbackMesh.transform.localPosition;
            }

            gameObject.SetActive(true);
            ApplyTint(_isPending ? _pendingTint : _ghostTint);
        }

        public void Hide()
        {
            _isPending = false;
            gameObject.SetActive(false);
        }

        public void SetPending(bool pending)
        {
            _isPending = pending;
            ApplyTint(pending ? _pendingTint : _ghostTint);
        }

        public void MoveTo(GridSlot slot, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (_isPending)
                return;

            CurrentSlot = slot;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            EnsureVisible();
        }

        public void PlayRejectionShake(float durationSeconds)
        {
            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);

            _shakeRoutine = StartCoroutine(ShakeRoutine(durationSeconds));
        }

        private IEnumerator ShakeRoutine(float durationSeconds)
        {
            var elapsed = 0f;
            var visual = _fallbackMesh != null ? _fallbackMesh.transform : transform;

            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var offset = Random.insideUnitSphere * _shakeStrength;
                offset.y *= 0.35f;
                visual.localPosition = _baseLocalPosition + offset;
                yield return null;
            }

            visual.localPosition = _baseLocalPosition;
            _shakeRoutine = null;
        }

        private void ApplyTint(Color tint)
        {
            if (_renderers == null || _renderers.Length == 0)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", tint);
                _propertyBlock.SetColor("_Color", tint);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
