using System;

namespace Work.Dispatch.Code.Runtime
{
    [Serializable]
    public sealed class DispatchResolvedRequest
    {
        public string ItemId;
        public int RequestedAmount;
        public int MinimumExpectedAmount;
        public int MaximumExpectedAmount;

        public DispatchResolvedRequest()
        {
        }

        public DispatchResolvedRequest(
            string itemId,
            int requestedAmount,
            int minimumExpectedAmount,
            int maximumExpectedAmount)
        {
            ItemId = itemId;
            RequestedAmount = requestedAmount;
            MinimumExpectedAmount = minimumExpectedAmount;
            MaximumExpectedAmount = maximumExpectedAmount;
        }
    }
}
