using System.Collections.Generic;
using UnityEngine;

namespace Work.Core.Utils.RendererProximityFade
{
    internal sealed class RendererFadeTarget
    {
        public readonly int SettingsIndex;
        public readonly Renderer CachedRenderer;
        public readonly List<RendererMaterialSlot> Slots = new();

        public float CurrentAlpha;
        public float AlphaVelocity;

        public RendererFadeTarget(int settingsIndex, Renderer cachedRenderer)
        {
            SettingsIndex = settingsIndex;
            CachedRenderer = cachedRenderer;

            CurrentAlpha = 1f;
            AlphaVelocity = 0f;
        }
    }
}
