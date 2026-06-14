using UnityEngine;

namespace Work.Entities.Code
{
    [RequireComponent(typeof(CharacterController))]
    public class EntityMovementModule : MonoBehaviour, IEntityModule
    {
        private Entity _owner;
        private Transform _camTransform;
        private CharacterController _controller;
        private float _verticalVelocity = 0f;
        private float _turnSmoothVelocity = 0f;

        [SerializeField]
        private float _turnSmoothTime = 0.1f;
        [SerializeField]
        private Transform _movementReference;

        public void Initialize(Entity entity)
        {
            _owner = entity;
            _controller = GetComponent<CharacterController>();
            _camTransform = _movementReference != null ? _movementReference : Camera.main != null ? Camera.main.transform : transform;
        }

        public void Move(Vector2 direction, bool isSmooth = true)
        {
            if (_controller == null || _owner == null || _camTransform == null)
                return;

            Vector3 camForward = Vector3.Scale(_camTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
            Vector3 camRight = Vector3.Scale(_camTransform.right, new Vector3(1f, 0f, 1f)).normalized;
            Vector3 lookDirection = camForward * direction.y + camRight * direction.x;

            float inputAmount = Mathf.Clamp01(direction.magnitude);
            if (inputAmount > 0.01f && lookDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 flatDir = new Vector3(lookDirection.x, 0f, lookDirection.z).normalized;

                float targetAngle = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
                if (isSmooth)
                {
                    float angle = Mathf.SmoothDampAngle(_owner.transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                    _owner.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }
                else
                {
                    _owner.transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
                }
            }
        }

        public void ApplyRootMotion(Vector3 deltaPosition)
        {
            if (_controller == null)
                return;

            Vector3 horizontalDelta = new Vector3(deltaPosition.x, 0f, deltaPosition.z);
            _controller.Move(horizontalDelta);
        }

        public void MoveStop()
        {
            if (_controller == null)
                return;

            _turnSmoothVelocity = 0f;
            if (_controller.isGrounded)
            {
                _verticalVelocity = 0f;
            }
        }

        private void Update()
        {
            if (_controller == null)
                return;

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = 0f;
            }
            else
            {
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }

            _controller.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
        }
    }
}
