using System;

namespace Work.Core.EventBus
{
    public static class Bus<T> where T : struct, IEvent
    {
        public static Action<T> Events;

        public static void Raise(T evt)
        {
            Events?.Invoke(evt);
        }
    }

    public static class Bus<T,T1> where T : struct, IEvent where T1 : IReturnValue
    {
        public static Func<T,T1> Events;

        public static T1 Raise(T evt)
        {
            return Events.Invoke(evt);
        }
    }
}
