using System.Collections.Generic;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class DisgustingEvaluation
    {
        public bool IsDisgusting { get; }
        public IReadOnlyList<string> Reasons { get; }

        public DisgustingEvaluation(bool isDisgusting, IReadOnlyList<string> reasons)
        {
            IsDisgusting = isDisgusting;
            Reasons = reasons ?? new List<string>();
        }
    }
}
