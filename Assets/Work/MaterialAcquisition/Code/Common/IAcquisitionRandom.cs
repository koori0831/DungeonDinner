using System.Collections.Generic;

namespace Work.MaterialAcquisition.Code.Common
{
    public interface IAcquisitionRandom
    {
        int RangeInt(int minInclusive, int maxExclusive);

        float RangeFloat01();

        int PickWeighted(IReadOnlyList<int> weights);
    }
}
