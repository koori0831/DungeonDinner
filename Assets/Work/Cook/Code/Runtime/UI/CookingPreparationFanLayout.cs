using UnityEngine;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 손질 카드 Fan의 위치를 계산하는 순수 레이아웃 도우미.
    /// 카드 개수가 늘어날수록 간격과 크기를 압축하되 1~7장은 항상 한 화면에 노출한다.
    /// </summary>
    public static class CookingPreparationFanLayout
    {
        public readonly struct CardPose
        {
            public Vector2 AnchoredPosition { get; }
            public float Rotation { get; }
            public float Scale { get; }

            public CardPose(Vector2 anchoredPosition, float rotation, float scale)
            {
                AnchoredPosition = anchoredPosition;
                Rotation = rotation;
                Scale = scale;
            }
        }

        public static CardPose Calculate(
            int index,
            int cardCount,
            float availableWidth,
            float cardWidth,
            float maxFanAngle,
            float minSpacing,
            float maxSpacing,
            float minScale,
            float arcHeight,
            int focusedIndex,
            int selectedIndex,
            float focusLift,
            float focusScale,
            float selectedLift,
            float neighborSpread,
            float peerDrop)
        {
            int safeCount = Mathf.Max(1, cardCount);
            int safeIndex = Mathf.Clamp(index, 0, safeCount - 1);
            float centerIndex = (safeCount - 1) * 0.5f;
            float distanceFromCenter = safeIndex - centerIndex;
            float normalized = centerIndex > 0f ? distanceFromCenter / centerIndex : 0f;

            float spacing = ResolveSpacing(safeCount, availableWidth, cardWidth, minSpacing, maxSpacing);
            float compressedScale = ResolveScale(safeCount, availableWidth, cardWidth, spacing, minScale);
            float x = distanceFromCenter * spacing;
            float y = arcHeight * (1f - normalized * normalized);
            float rotation = -normalized * Mathf.Max(0f, maxFanAngle);
            float scale = compressedScale;

            if (selectedIndex == safeIndex)
                y += Mathf.Max(0f, selectedLift);

            bool hasValidFocus = focusedIndex >= 0 && focusedIndex < safeCount;
            if (hasValidFocus)
            {
                if (safeIndex == focusedIndex)
                {
                    y += Mathf.Max(0f, focusLift);
                    rotation = 0f;
                    scale = Mathf.Max(scale, Mathf.Max(1f, focusScale));
                }
                else
                {
                    y -= Mathf.Max(0f, peerDrop);
                    int relative = safeIndex - focusedIndex;
                    if (relative != 0)
                        x += Mathf.Sign(relative) * Mathf.Max(0f, neighborSpread) / Mathf.Max(1f, Mathf.Abs(relative));
                }
            }

            return new CardPose(new Vector2(x, y), rotation, scale);
        }

        private static float ResolveSpacing(
            int cardCount,
            float availableWidth,
            float cardWidth,
            float minSpacing,
            float maxSpacing)
        {
            if (cardCount <= 1)
                return 0f;

            float safeMin = Mathf.Max(1f, minSpacing);
            float safeMax = Mathf.Max(safeMin, maxSpacing);
            float usableWidth = Mathf.Max(0f, availableWidth - Mathf.Max(0f, cardWidth));
            return Mathf.Clamp(usableWidth / (cardCount - 1), safeMin, safeMax);
        }

        private static float ResolveScale(
            int cardCount,
            float availableWidth,
            float cardWidth,
            float spacing,
            float minScale)
        {
            float safeMin = Mathf.Clamp(minScale, 0.5f, 1f);
            if (cardCount <= 1 || cardWidth <= 0f || availableWidth <= 0f)
                return 1f;

            float requiredWidth = cardWidth + spacing * (cardCount - 1);
            if (requiredWidth <= availableWidth)
                return 1f;

            return Mathf.Clamp(availableWidth / requiredWidth, safeMin, 1f);
        }
    }
}
