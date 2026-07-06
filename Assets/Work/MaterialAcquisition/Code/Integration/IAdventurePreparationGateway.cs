namespace Work.MaterialAcquisition.Code.Integration
{
    public interface IAdventurePreparationGateway
    {
        bool HasActiveSession { get; }
        bool CanStartAdventure(int currentDay);
    }
}
