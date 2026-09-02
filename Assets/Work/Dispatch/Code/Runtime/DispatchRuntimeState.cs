using System.Collections.Generic;

namespace Work.Dispatch.Code.Runtime
{
    public sealed class DispatchRuntimeState
    {
        private DispatchJob _activeJob;
        private readonly List<DispatchJob> _returnedReports;

        public DispatchJob ActiveJob => _activeJob;
        public IReadOnlyList<DispatchJob> ReturnedReports => _returnedReports;
        public bool HasActiveJob => _activeJob != null && _activeJob.State == DispatchState.Active;

        public DispatchRuntimeState(DispatchSaveData saveData = null)
        {
            saveData ??= new DispatchSaveData();
            _activeJob = saveData.ActiveJob;
            _returnedReports = saveData.ReturnedReports ?? new List<DispatchJob>();

            if (_activeJob != null && _activeJob.State != DispatchState.Active)
            {
                if (_activeJob.State == DispatchState.Returned)
                {
                    _returnedReports.Add(_activeJob);
                }

                _activeJob = null;
            }
        }

        public bool TryStart(DispatchJob job)
        {
            if (HasActiveJob || job == null || job.State != DispatchState.Active)
            {
                return false;
            }

            _activeJob = job;
            return true;
        }

        public DispatchJob Reconcile(int totalElapsedTime)
        {
            if (_activeJob == null || _activeJob.IsCompleteAt(totalElapsedTime) == false)
            {
                return null;
            }

            DispatchJob returnedJob = _activeJob;
            returnedJob.State = DispatchState.Returned;
            _returnedReports.Add(returnedJob);
            _activeJob = null;
            return returnedJob;
        }

        public bool IsNpcDispatched(string npcId)
        {
            return HasActiveJob
                   && string.Equals(_activeJob.NpcId, npcId, System.StringComparison.OrdinalIgnoreCase);
        }

        public DispatchJob FindReturnedReport(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return null;
            }

            for (int i = 0; i < _returnedReports.Count; i++)
            {
                DispatchJob report = _returnedReports[i];
                if (report != null
                    && string.Equals(report.JobId, jobId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return report;
                }
            }

            return null;
        }

        public bool RemoveClaimedReport(string jobId)
        {
            for (int i = 0; i < _returnedReports.Count; i++)
            {
                DispatchJob report = _returnedReports[i];
                if (report != null
                    && report.State == DispatchState.Claimed
                    && string.Equals(report.JobId, jobId, System.StringComparison.OrdinalIgnoreCase))
                {
                    _returnedReports.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public DispatchSaveData CreateSaveData()
        {
            return new DispatchSaveData
            {
                ActiveJob = _activeJob,
                ReturnedReports = new List<DispatchJob>(_returnedReports)
            };
        }
    }
}
