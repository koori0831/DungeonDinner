using Work.Core.EventBus;

namespace Work.TimeSystem
{
    public readonly record struct GameTimeAdvancedEvent(
        int PreviousTotalTime,
        int CurrentTotalTime,
        int PreviousDay,
        int CurrentDay,
        int CurrentTimeOfDay,
        int Amount,
        GameTimeActivityType ActivityType
    ) : IEvent;

    public readonly record struct GameDayChangedEvent(
        int PreviousDay,
        int CurrentDay,
        int TotalElapsedTime
    ) : IEvent;
}
