using Unity.Cinemachine;
using UnityEngine;

namespace Work.Core.Utils.RendererProximityFade
{
    [DisallowMultipleComponent]
    public sealed class CinemachineRendererProximityFade : CinemachineExtension
    {
        [Header("References")]
        [SerializeField] private RendererSettings[] _rendererSettings;

        [Header("Fade Settings")]
        [SerializeField, Min(0f)] private float _fadeStartDistance = 1.5f;
        [SerializeField, Min(0f)] private float _fadeEndDistance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _fadeEndAlpha = 0.2f;
        [SerializeField, Min(0f)] private float _alphaSmoothTime = 0.05f;

        [Header("Optimization")]
        [SerializeField, Min(0f)] private float _maxProcessDistance = 8f;

        [Header("Shader")]
        [SerializeField] private string[] _colorPropertyNames = { "_BaseColor", "_Color" };

        [Header("Gizmos")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private bool _drawGizmosOnlyWhenSelected = true;
        [SerializeField] private bool _drawFadeRanges = true;
        [SerializeField] private Color _bodyGizmoColor = new(1f, 0.75f, 0.1f, 0.9f);
        [SerializeField] private Color _fadeEndGizmoColor = new(1f, 0.25f, 0.15f, 0.7f);
        [SerializeField] private Color _fadeStartGizmoColor = new(0.2f, 0.8f, 1f, 0.7f);

        private readonly RendererFadeMaterialCache _materialCache = new();

        private bool _isInitialized;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Initialize();
            _materialCache.ApplyAlphaToAll(_rendererSettings, 1f, true);
        }

        private void OnDisable()
        {
            if (_isInitialized)
            {
                _materialCache.ResetAlphas(_rendererSettings);
            }
        }

        protected override void OnDestroy()
        {
            if (_isInitialized)
            {
                _materialCache.ResetAlphas(_rendererSettings);
            }

            base.OnDestroy();
        }

        public void RefreshCache()
        {
            EnsureColorPropertyNames();
            ClampRendererSettings();

            _materialCache.Rebuild(_rendererSettings, _colorPropertyNames);
            _isInitialized = true;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
            {
                return;
            }

            if (vcam != ComponentOwner)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            UpdateAlphas(state.GetFinalPosition(), deltaTime);
        }

        private void UpdateAlphas(Vector3 cameraPosition, float deltaTime)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _materialCache.UpdateAlphas(
                _rendererSettings,
                cameraPosition,
                deltaTime,
                _fadeStartDistance,
                _fadeEndDistance,
                _fadeEndAlpha,
                _alphaSmoothTime,
                _maxProcessDistance);
        }

        private void OnValidate()
        {
            _fadeStartDistance = Mathf.Max(0f, _fadeStartDistance);
            _fadeEndDistance = Mathf.Max(0f, _fadeEndDistance);
            _fadeEndAlpha = Mathf.Clamp01(_fadeEndAlpha);
            _alphaSmoothTime = Mathf.Max(0f, _alphaSmoothTime);
            _maxProcessDistance = Mathf.Max(0f, _maxProcessDistance);

            EnsureColorPropertyNames();
            ClampRendererSettings();

            if (Application.isPlaying && _isInitialized)
            {
                RefreshCache();
            }
        }

        private void Initialize()
        {
            EnsureColorPropertyNames();

            ClampRendererSettings();

            _materialCache.Rebuild(_rendererSettings, _colorPropertyNames);
            _isInitialized = true;
        }

        private void EnsureColorPropertyNames()
        {
            if (_colorPropertyNames == null || _colorPropertyNames.Length == 0)
            {
                _colorPropertyNames = new[] { "_BaseColor", "_Color" };
                return;
            }

            for (int i = 0; i < _colorPropertyNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_colorPropertyNames[i]))
                {
                    _colorPropertyNames[i] = "_BaseColor";
                }
            }
        }

        private void ClampRendererSettings()
        {
            if (_rendererSettings == null)
            {
                return;
            }

            for (int i = 0; i < _rendererSettings.Length; i++)
            {
                RendererSettings settings = _rendererSettings[i];

                if (settings.BodyRadius <= 0f)
                {
                    settings.BodyRadius = 0.45f;
                }

                if (settings.BodyHeight <= 0f)
                {
                    settings.BodyHeight = 1.8f;
                }

                settings.BodyHeight = Mathf.Max(settings.BodyHeight, settings.BodyRadius * 2f);

                _rendererSettings[i] = settings;
            }
        }

        private void OnDrawGizmos()
        {
            if (_drawGizmosOnlyWhenSelected)
            {
                return;
            }

            DrawFadeGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmosOnlyWhenSelected)
            {
                return;
            }

            DrawFadeGizmos();
        }

        private void DrawFadeGizmos()
        {
            if (!_drawGizmos)
            {
                return;
            }

            if (_rendererSettings == null)
            {
                return;
            }

            float fadeEnd = Mathf.Min(_fadeStartDistance, _fadeEndDistance);
            float fadeStart = Mathf.Max(_fadeStartDistance, _fadeEndDistance);

            for (int i = 0; i < _rendererSettings.Length; i++)
            {
                RendererSettings settings = _rendererSettings[i];

                if (settings.TargetRenderer == null)
                {
                    continue;
                }

                Vector3 center = settings.TargetRenderer.transform.position + settings.BodyOffset;

                DrawCapsuleGizmo(settings, center, 0f, _bodyGizmoColor);

                if (!_drawFadeRanges)
                {
                    continue;
                }

                if (fadeEnd > 0f)
                {
                    DrawCapsuleGizmo(settings, center, fadeEnd, _fadeEndGizmoColor);
                }

                if (fadeStart > 0f && !Mathf.Approximately(fadeStart, fadeEnd))
                {
                    DrawCapsuleGizmo(settings, center, fadeStart, _fadeStartGizmoColor);
                }
            }
        }

        private void DrawCapsuleGizmo(
            RendererSettings settings,
            Vector3 center,
            float expand,
            Color color)
        {
            Gizmos.color = color;

            float radius = Mathf.Max(0f, settings.BodyRadius + expand);
            float height = Mathf.Max(settings.BodyHeight + expand * 2f, radius * 2f);
            float lineHalfHeight = Mathf.Max(0f, height * 0.5f - radius);

            Vector3 top = center + Vector3.up * lineHalfHeight;
            Vector3 bottom = center - Vector3.up * lineHalfHeight;

            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);

            Vector3 right = Vector3.right * radius;
            Vector3 forward = Vector3.forward * radius;

            Gizmos.DrawLine(top + right, bottom + right);
            Gizmos.DrawLine(top - right, bottom - right);
            Gizmos.DrawLine(top + forward, bottom + forward);
            Gizmos.DrawLine(top - forward, bottom - forward);
        }
    }
}
