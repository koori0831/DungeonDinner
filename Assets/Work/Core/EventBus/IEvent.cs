namespace Work.Core.EventBus
{
    /// <summary>
    /// Marker for event payloads. Implementations must be declared as readonly record structs.
    /// This convention is enforced by EventBus.Analyzers.
    /// </summary>
    public interface IEvent
    {
    }

    public interface IReturnValue
    {
    }
}
