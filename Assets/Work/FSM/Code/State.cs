using Work.Entities.Code;

namespace Work.FSM.Code
{
    public class State
    {
        protected StateMachine _stateMachine;
        protected Entity _owner;
        protected EntityAnimationModule _animator;
        protected int _animationHash;

        public State(StateMachine stateMachine, Entity owner, int animationHash)
        {
            _stateMachine = stateMachine;
            _owner = owner;
            _owner.TryGetModule<EntityAnimationModule>(out _animator, true);
            _animationHash = animationHash;
        }

        public virtual void Enter()
        {
            if (_animationHash != 0)
            {
                _animator?.SetParam(_animationHash, true);
            }
        }

        public virtual void Exit()
        {
            if (_animationHash != 0)
            {
                _animator?.SetParam(_animationHash, false);
            }
        }

        public virtual void Update()
        {
        }

        public virtual void OnTriggerEnter(AnimationEventType eventType)
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
