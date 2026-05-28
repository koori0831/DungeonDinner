using UnityEngine;
using Work.Entities.Code;

[RequireComponent(typeof(CharacterController))]
public class EntityMovementModule : MonoBehaviour, IEntityModule
{
    private Entity _owner;
    private Transform _camTransform;
    private CharacterController _controller;
    private Vector3 _pendingHorizontalMovement = Vector3.zero;
    private float _verticalVelocity = 0f;
    private float _turnSmoothVelocity = 0f;

    [SerializeField]
    private float _moveSpeed = 5f;
    [SerializeField]
    private float _turnSmoothTime = 0.1f;

    public void Initialize(Entity entity)
    {
        _owner = entity;
        _controller = GetComponent<CharacterController>();
        _camTransform = Camera.main != null ? Camera.main.transform : transform;
    }

    public void Move(Vector2 direction, bool isSmooth = true)
    {
        if (_controller == null || _owner == null || _camTransform == null)
            return;

        Vector3 camForward = Vector3.Scale(_camTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
        Vector3 camRight = Vector3.Scale(_camTransform.right, new Vector3(1f, 0f, 1f)).normalized;
        Vector3 lookDirection = camForward * direction.y + camRight * direction.x;

        if (direction.sqrMagnitude > 0.01f && lookDirection != Vector3.zero)
        {
            Vector3 flatDir = new Vector3(lookDirection.x, 0f, lookDirection.z).normalized;
            _pendingHorizontalMovement = flatDir * _moveSpeed;

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
        else
        {
            _pendingHorizontalMovement = Vector3.zero;
        }
    }

    public void ApplyRootMotion(Vector3 deltaPosition)
    {
        if (_controller == null)
            return;

        _controller.Move(deltaPosition);
    }

    public void MoveStop()
    {
        if (_controller == null)
            return;

        _pendingHorizontalMovement = Vector3.zero;
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

        Vector3 totalMove = _pendingHorizontalMovement + new Vector3(0f, _verticalVelocity, 0f);
        _controller.Move(totalMove * Time.deltaTime);
    }
}
