namespace Work.MaterialAcquisition.Code.Integration
{
    public interface IAcquisitionDayProvider
    {
        int CurrentDay { get; }
        string CurrentDayText { get; }
        void AdvanceDay();
    }
}
