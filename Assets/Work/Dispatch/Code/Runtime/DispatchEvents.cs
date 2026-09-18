using Work.Core.EventBus;

namespace Work.Dispatch.Code.Runtime
{
    public readonly record struct DispatchStartedEvent(DispatchJob Job) : IEvent;
    public readonly record struct DispatchReturnedEvent(DispatchJob Job) : IEvent;
    public readonly record struct DispatchReportsChangedEvent : IEvent;
}
