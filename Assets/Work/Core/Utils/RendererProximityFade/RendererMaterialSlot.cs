using UnityEngine;

namespace Work.Core.Utils.RendererProximityFade
{
    internal sealed class RendererMaterialSlot
    {
        public readonly int MaterialIndex;
        public readonly int ColorPropertyId;
        public readonly Color BaseColor;

        public float LastAppliedAlpha;

        public RendererMaterialSlot(
            int materialIndex,
            int colorPropertyId,
            Color baseColor)
        {
            MaterialIndex = materialIndex;
            ColorPropertyId = colorPropertyId;
            BaseColor = baseColor;

            LastAppliedAlpha = -1f;
        }
    }
}
