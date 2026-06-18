using UnityEngine;
using Work.FSM.Code;

namespace Work.Entities.Code
{
    [RequireComponent(typeof(Animator))]
    public class EntityAnimationModule : MonoBehaviour, IEntityModule
    {
        private Entity _owner;
        private Animator _animator;

        private EntityMovementModule _movementModule;
        private EntityStateModule _stateModule;

        public void Initialize(Entity entity)
        {
            _owner = entity;
            _animator = GetComponent<Animator>();
            _owner.TryGetModule<EntityMovementModule>(out _movementModule);
            _owner.TryGetModule<EntityStateModule>(out _stateModule);
        }

        public void SetParam(int animHash, float value) => _animator.SetFloat(animHash, value);
        public void SetParam(int animHash, int value) => _animator.SetInteger(animHash, value);
        public void SetParam(int animHash, bool value) => _animator.SetBool(animHash, value);
        public void SetTrigger(int animHash) => _animator.SetTrigger(animHash);

        public void SetApplyRootMotion(bool apply)
        {
            _animator.applyRootMotion = apply;
        }

        private void OnAnimatorMove()
        {
            if (!_animator.applyRootMotion)
            {
                _animator.speed = 1f;
                return;
            }

            _animator.speed = _movementModule != null ? _movementModule.MoveSpeed : 1f;

            if (_movementModule != null)
            {
                _movementModule.ApplyRootMotion(_animator.deltaPosition);
            }
            else
            {
                _owner.transform.position += _animator.deltaPosition;
            }

            if (_owner.transform != transform)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        public void AnimationEvent(AnimationEventType eventType)
        {
            _stateModule?.TriggerEvent(eventType);
        }
    }
}
