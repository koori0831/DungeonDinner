using UnityEngine;
using Work.Core.EventBus;

namespace Work.Input.Code
{
    public class InputEvents
    {
        #region PlayerInputEvents
        public readonly record struct PlayerInputEnableEvent(bool Enable) : IEvent;

        public readonly record struct InputInteractEvent : IEvent;
        public readonly record struct InputJumpEvent : IEvent;
        public readonly record struct InputMoveEvent(Vector2 MoveVector) : IEvent;
        #endregion
    }
}
