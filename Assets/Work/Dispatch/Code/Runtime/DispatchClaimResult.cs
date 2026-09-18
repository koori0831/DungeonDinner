namespace Work.Dispatch.Code.Runtime
{
    public readonly struct DispatchClaimResult
    {
        public bool ReportFound { get; }
        public int AddedAmount { get; }
        public int RemainingAmount { get; }
        public bool IsFullyClaimed => ReportFound && RemainingAmount <= 0;

        public DispatchClaimResult(bool reportFound, int addedAmount, int remainingAmount)
        {
            ReportFound = reportFound;
            AddedAmount = addedAmount;
            RemainingAmount = remainingAmount;
        }
    }
}
