namespace Work.NPC.Code.Runtime
{
    public enum NpcRequestState
    {
        Locked = 0,
        Unlocked = 1,
        Offered = 2,
        Accepted = 3,
        InProgress = 4,
        ReadyToComplete = 5,
        Completed = 6,
        EpilogueAvailable = 7,
        EpilogueCompleted = 8
    }
}
