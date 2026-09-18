using System;
using System.Collections.Generic;

namespace Work.Dispatch.Code.Runtime
{
    [Serializable]
    public sealed class DispatchDraft
    {
        public string NpcId;
        public string RegionId;
        public List<DispatchDraftRequest> Requests = new List<DispatchDraftRequest>();
    }

    [Serializable]
    public sealed class DispatchDraftRequest
    {
        public string ItemId;
        public int Amount;

        public DispatchDraftRequest()
        {
        }

        public DispatchDraftRequest(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }
}
