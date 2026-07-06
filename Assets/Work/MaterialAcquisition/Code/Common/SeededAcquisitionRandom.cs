using System;
using System.Collections.Generic;

namespace Work.MaterialAcquisition.Code.Common
{
    public sealed class SeededAcquisitionRandom : IAcquisitionRandom
    {
        private readonly Random random;

        public SeededAcquisitionRandom(int seed)
        {
            random = new Random(seed);
        }

        public int RangeInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return random.Next(minInclusive, maxExclusive);
        }

        public float RangeFloat01()
        {
            return (float)random.NextDouble();
        }

        public int PickWeighted(IReadOnlyList<int> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                return -1;
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > 0)
                {
                    totalWeight += weights[i];
                }
            }

            if (totalWeight <= 0)
            {
                return -1;
            }

            int roll = RangeInt(0, totalWeight);
            int cursor = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                int weight = weights[i];
                if (weight <= 0)
                {
                    continue;
                }

                cursor += weight;
                if (roll < cursor)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
