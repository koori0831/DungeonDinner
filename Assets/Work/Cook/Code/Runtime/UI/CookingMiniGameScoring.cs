using UnityEngine;

namespace Work.Cook.Code.Runtime.UI
{
    public static class CookingMiniGameScoring
    {
        public static float ScoreTarget(float value, float targetMin, float targetMax)
        {
            float minimum = Mathf.Clamp01(Mathf.Min(targetMin, targetMax));
            float maximum = Mathf.Clamp01(Mathf.Max(targetMin, targetMax));
            float center = (minimum + maximum) * 0.5f;
            float halfWidth = Mathf.Max(0.01f, (maximum - minimum) * 0.5f);
            float distance = Mathf.Abs(Mathf.Clamp01(value) - center) / halfWidth;
            return distance <= 1f
                ? Mathf.Lerp(0.7f, 1f, 1f - distance)
                : Mathf.Clamp01(0.7f - (distance - 1f) * 0.35f);
        }

        public static float ScoreRoasting(
            float doneness,
            float targetMin,
            float targetMax,
            float flipProgress,
            float sideAExposure,
            float sideBExposure)
        {
            float finalState = ScoreTarget(doneness, targetMin, targetMax);
            float flipTiming = Mathf.Clamp01(1f - Mathf.Abs(flipProgress - 0.5f) * 2f);
            float totalExposure = Mathf.Max(0.0001f, sideAExposure + sideBExposure);
            float evenness = Mathf.Clamp01(1f - Mathf.Abs(sideAExposure - sideBExposure) / totalExposure);
            return Mathf.Clamp01(finalState * 0.6f + flipTiming * 0.2f + evenness * 0.2f);
        }

        public static float ScoreBoiling(float doneness, float targetMin, float targetMax)
        {
            return ScoreTarget(doneness, targetMin, targetMax);
        }

        public static float ScoreFreezing(float[] cells, float targetMin, float targetMax)
        {
            if (cells == null || cells.Length == 0)
                return 0f;

            float mean = 0f;
            float overcooled = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                float value = Mathf.Clamp01(cells[i]);
                mean += value;
                if (value > targetMax)
                    overcooled += 1f;
            }

            mean /= cells.Length;
            float variance = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                float delta = Mathf.Clamp01(cells[i]) - mean;
                variance += delta * delta;
            }

            float standardDeviation = Mathf.Sqrt(variance / cells.Length);
            float targetScore = ScoreTarget(mean, targetMin, targetMax);
            float uniformity = Mathf.Clamp01(1f - standardDeviation / 0.35f);
            float overcoolScore = 1f - overcooled / cells.Length;
            return Mathf.Clamp01(targetScore * 0.4f + uniformity * 0.4f + overcoolScore * 0.2f);
        }

        public static float ScoreDiluting(
            float concentration,
            float targetMin,
            float targetMax,
            float spillRatio)
        {
            return Mathf.Clamp01(
                ScoreTarget(concentration, targetMin, targetMax) * 0.7f
                + (1f - Mathf.Clamp01(spillRatio)) * 0.3f);
        }
    }
}
