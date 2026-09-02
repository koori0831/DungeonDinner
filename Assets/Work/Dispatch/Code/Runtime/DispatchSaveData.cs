using System;
using System.Collections.Generic;

namespace Work.Dispatch.Code.Runtime
{
    [Serializable]
    public sealed class DispatchSaveData
    {
        public const int CurrentSaveVersion = 1;

        public int SaveVersion = CurrentSaveVersion;
        public DispatchJob ActiveJob;
        public List<DispatchJob> ReturnedReports = new List<DispatchJob>();
    }
}
