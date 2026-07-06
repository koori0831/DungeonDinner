namespace Work.MaterialAcquisition.Code.Integration
{
    public interface IDispatchPreparationGateway
    {
        int ActiveTaskCount { get; }
        int ReadyToClaimCount { get; }
        bool HasBlockingReadyToClaimTask { get; }
        void RefreshTasksForDay(int currentDay);
    }
}
