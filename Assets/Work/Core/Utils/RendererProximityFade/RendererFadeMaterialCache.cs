using System.Collections.Generic;
using UnityEngine;

namespace Work.Core.Utils.RendererProximityFade
{
    internal sealed class RendererFadeMaterialCache
    {
        private const float AlphaApplyThreshold = 0.001f;

        private readonly List<RendererFadeTarget> _targets = new();

        private MaterialPropertyBlock _propertyBlock;

        public void Rebuild(RendererSettings[] rendererSettings, string[] colorPropertyNames)
        {
            EnsurePropertyBlock();

            _targets.Clear();

            if (rendererSettings == null)
            {
                return;
            }

            for (int settingsIndex = 0; settingsIndex < rendererSettings.Length; settingsIndex++)
            {
                RendererSettings settings = rendererSettings[settingsIndex];
                Renderer targetRenderer = settings.TargetRenderer;

                if (targetRenderer == null)
                {
                    continue;
                }

                RendererFadeTarget target = new(settingsIndex, targetRenderer);
                Material[] sharedMaterials = targetRenderer.sharedMaterials;

                for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    Material sharedMaterial = sharedMaterials[materialIndex];

                    if (sharedMaterial == null)
                    {
                        continue;
                    }

                    if (!TryGetColorProperty(sharedMaterial, colorPropertyNames, out int colorPropertyId))
                    {
                        continue;
                    }

                    Color baseColor = sharedMaterial.GetColor(colorPropertyId);

                    target.Slots.Add(new RendererMaterialSlot(
                        materialIndex,
                        colorPropertyId,
                        baseColor));
                }

                if (target.Slots.Count > 0)
                {
                    _targets.Add(target);
                }
            }
        }

        public void UpdateAlphas(
            RendererSettings[] rendererSettings,
            Vector3 cameraPosition,
            float deltaTime,
            float fadeStartDistance,
            float fadeEndDistance,
            float fadeEndAlpha,
            float alphaSmoothTime,
            float maxProcessDistance)
        {
            EnsurePropertyBlock();

            bool useImmediateAlpha = deltaTime < 0f || alphaSmoothTime <= 0f;

            for (int i = 0; i < _targets.Count; i++)
            {
                RendererFadeTarget target = _targets[i];

                if (!TryGetValidTargetData(
                        rendererSettings,
                        target,
                        out RendererSettings settings,
                        out Renderer targetRenderer))
                {
                    continue;
                }

                float targetAlpha = CalculateTargetAlpha(
                    cameraPosition,
                    settings,
                    targetRenderer,
                    fadeStartDistance,
                    fadeEndDistance,
                    fadeEndAlpha,
                    maxProcessDistance);

                if (useImmediateAlpha)
                {
                    target.CurrentAlpha = targetAlpha;
                    target.AlphaVelocity = 0f;
                }
                else
                {
                    target.CurrentAlpha = Mathf.SmoothDamp(
                        target.CurrentAlpha,
                        targetAlpha,
                        ref target.AlphaVelocity,
                        alphaSmoothTime,
                        float.PositiveInfinity,
                        deltaTime);
                }

                ApplyAlphaToTarget(target, targetRenderer, target.CurrentAlpha, false);
            }
        }

        public void ApplyAlphaToAll(RendererSettings[] rendererSettings, float alphaMultiplier, bool force)
        {
            EnsurePropertyBlock();

            for (int i = 0; i < _targets.Count; i++)
            {
                RendererFadeTarget target = _targets[i];

                if (!TryGetValidTargetData(rendererSettings, target, out _, out Renderer targetRenderer))
                {
                    continue;
                }

                target.CurrentAlpha = Mathf.Clamp01(alphaMultiplier);
                target.AlphaVelocity = 0f;

                ApplyAlphaToTarget(target, targetRenderer, alphaMultiplier, force);
            }
        }

        public void ResetAlphas(RendererSettings[] rendererSettings)
        {
            EnsurePropertyBlock();

            for (int i = 0; i < _targets.Count; i++)
            {
                RendererFadeTarget target = _targets[i];

                target.CurrentAlpha = 1f;
                target.AlphaVelocity = 0f;

                if (!TryGetValidTargetData(rendererSettings, target, out _, out Renderer targetRenderer))
                {
                    continue;
                }

                ApplyAlphaToTarget(target, targetRenderer, 1f, true);
            }
        }

        private void ApplyAlphaToTarget(
            RendererFadeTarget target,
            Renderer targetRenderer,
            float alphaMultiplier,
            bool force)
        {
            if (target == null)
            {
                return;
            }

            if (targetRenderer == null)
            {
                return;
            }

            float clampedAlpha = Mathf.Clamp01(alphaMultiplier);

            for (int i = 0; i < target.Slots.Count; i++)
            {
                RendererMaterialSlot slot = target.Slots[i];
                ApplyAlphaToSlot(slot, targetRenderer, clampedAlpha, force);
            }
        }

        private void ApplyAlphaToSlot(
            RendererMaterialSlot slot,
            Renderer targetRenderer,
            float alphaMultiplier,
            bool force)
        {
            if (slot == null)
            {
                return;
            }

            if (targetRenderer == null)
            {
                return;
            }

            if (!force && Mathf.Abs(slot.LastAppliedAlpha - alphaMultiplier) < AlphaApplyThreshold)
            {
                return;
            }

            Color color = slot.BaseColor;
            color.a *= alphaMultiplier;

            targetRenderer.GetPropertyBlock(_propertyBlock, slot.MaterialIndex);
            _propertyBlock.SetColor(slot.ColorPropertyId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock, slot.MaterialIndex);

            slot.LastAppliedAlpha = alphaMultiplier;
        }

        private bool TryGetValidTargetData(
            RendererSettings[] rendererSettings,
            RendererFadeTarget target,
            out RendererSettings settings,
            out Renderer targetRenderer)
        {
            settings = default;
            targetRenderer = null;

            if (target == null)
            {
                return false;
            }

            if (rendererSettings == null)
            {
                return false;
            }

            if (target.SettingsIndex < 0 || target.SettingsIndex >= rendererSettings.Length)
            {
                return false;
            }

            settings = rendererSettings[target.SettingsIndex];
            targetRenderer = settings.TargetRenderer;

            if (targetRenderer == null)
            {
                return false;
            }

            if (targetRenderer != target.CachedRenderer)
            {
                return false;
            }

            Material[] sharedMaterials = targetRenderer.sharedMaterials;

            for (int i = 0; i < target.Slots.Count; i++)
            {
                RendererMaterialSlot slot = target.Slots[i];

                if (slot.MaterialIndex < 0 || slot.MaterialIndex >= sharedMaterials.Length)
                {
                    return false;
                }
            }

            return true;
        }

        private float CalculateTargetAlpha(
            Vector3 cameraPosition,
            RendererSettings settings,
            Renderer targetRenderer,
            float fadeStartDistance,
            float fadeEndDistance,
            float fadeEndAlpha,
            float maxProcessDistance)
        {
            if (targetRenderer == null)
            {
                return 1f;
            }

            Vector3 centerPosition = targetRenderer.transform.position + settings.BodyOffset;
            float effectiveDistance = CalculateCapsuleDistance(
                cameraPosition,
                centerPosition,
                settings.BodyRadius,
                settings.BodyHeight);

            if (maxProcessDistance > 0f && effectiveDistance > maxProcessDistance)
            {
                return 1f;
            }

            float fadeEnd = Mathf.Min(fadeStartDistance, fadeEndDistance);
            float fadeStart = Mathf.Max(fadeStartDistance, fadeEndDistance);

            if (effectiveDistance <= fadeEnd)
            {
                return fadeEndAlpha;
            }

            if (effectiveDistance >= fadeStart)
            {
                return 1f;
            }

            float distanceRatio = Mathf.InverseLerp(fadeEnd, fadeStart, effectiveDistance);
            return Mathf.Lerp(fadeEndAlpha, 1f, distanceRatio);
        }

        private float CalculateCapsuleDistance(
            Vector3 point,
            Vector3 center,
            float radius,
            float height)
        {
            radius = Mathf.Max(0f, radius);
            height = Mathf.Max(height, radius * 2f);

            float lineHalfHeight = Mathf.Max(0f, height * 0.5f - radius);

            Vector3 a = center + Vector3.up * lineHalfHeight;
            Vector3 b = center - Vector3.up * lineHalfHeight;

            float distanceToSegment = DistancePointToSegment(point, a, b);
            return Mathf.Max(0f, distanceToSegment - radius);
        }

        private float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float abLengthSqr = ab.sqrMagnitude;

            if (abLengthSqr <= Mathf.Epsilon)
            {
                return Vector3.Distance(point, a);
            }

            float t = Vector3.Dot(point - a, ab) / abLengthSqr;
            t = Mathf.Clamp01(t);

            Vector3 closestPoint = a + ab * t;
            return Vector3.Distance(point, closestPoint);
        }

        private void EnsurePropertyBlock()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private bool TryGetColorProperty(
            Material material,
            string[] colorPropertyNames,
            out int propertyId)
        {
            if (material == null)
            {
                propertyId = 0;
                return false;
            }

            if (colorPropertyNames == null || colorPropertyNames.Length == 0)
            {
                propertyId = 0;
                return false;
            }

            for (int i = 0; i < colorPropertyNames.Length; i++)
            {
                string propertyName = colorPropertyNames[i];

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    continue;
                }

                int candidateId = Shader.PropertyToID(propertyName);

                if (material.HasProperty(candidateId))
                {
                    propertyId = candidateId;
                    return true;
                }
            }

            propertyId = 0;
            return false;
        }
    }
}
