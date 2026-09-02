using System.Collections.Generic;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class DisgustingEvaluation
    {
        public bool IsBizarre { get; }
        public IReadOnlyList<string> Reasons { get; }

        public DisgustingEvaluation(bool isBizarre, IReadOnlyList<string> reasons)
        {
            IsBizarre = isBizarre;
            Reasons = reasons ?? new List<string>();
        }
    }
}
