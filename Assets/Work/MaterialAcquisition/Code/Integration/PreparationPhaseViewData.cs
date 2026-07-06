namespace Work.MaterialAcquisition.Code.Integration
{
    public readonly struct PreparationPhaseViewData
    {
        public readonly PreparationPhaseState State;
        public readonly string DayText;
        public readonly string SummaryText;
        public readonly bool CanOpenDispatch;
        public readonly string DispatchReason;
        public readonly bool CanOpenAdventure;
        public readonly string AdventureReason;
        public readonly bool CanAdvanceDay;
        public readonly string AdvanceDayReason;
        public readonly int ActiveDispatchTaskCount;
        public readonly int ReadyDispatchTaskCount;

        public PreparationPhaseViewData(
            PreparationPhaseState state,
            string dayText,
            string summaryText,
            bool canOpenDispatch,
            string dispatchReason,
            bool canOpenAdventure,
            string adventureReason,
            bool canAdvanceDay,
            string advanceDayReason,
            int activeDispatchTaskCount,
            int readyDispatchTaskCount)
        {
            State = state;
            DayText = dayText ?? string.Empty;
            SummaryText = summaryText ?? string.Empty;
            CanOpenDispatch = canOpenDispatch;
            DispatchReason = dispatchReason ?? string.Empty;
            CanOpenAdventure = canOpenAdventure;
            AdventureReason = adventureReason ?? string.Empty;
            CanAdvanceDay = canAdvanceDay;
            AdvanceDayReason = advanceDayReason ?? string.Empty;
            ActiveDispatchTaskCount = activeDispatchTaskCount;
            ReadyDispatchTaskCount = readyDispatchTaskCount;
        }
    }
}
