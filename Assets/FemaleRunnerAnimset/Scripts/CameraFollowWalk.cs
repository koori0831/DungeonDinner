using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowWalk : CameraWalk
{
    private Quaternion _initialRotation;
    private Transform _parent;
    private Vector3 initialLocalPosition;

    private Camera _camera;

    [Header("카메라가 캐릭터 따라 회전하기")]
    public bool isRotate = false;

    protected override void Start()
    {
        base.Start();

        _camera = GetComponent<Camera>();

        _initialRotation = transform.rotation;
        _parent = transform.parent;
        initialLocalPosition = transform.position - _parent.position;
    }

    protected virtual void LateUpdate()
    {
        if (!isRotate)
        {
            transform.rotation = _initialRotation;
            transform.position = _parent.position + initialLocalPosition;
        }
    }

    protected override void myCameraWalk()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null)
            return;

        Vector3 move = Vector3.zero;

        if (keyboard.aKey.isPressed) move.x -= 1f;
        if (keyboard.dKey.isPressed) move.x += 1f;
        if (keyboard.sKey.isPressed) move.z -= 1f;
        if (keyboard.wKey.isPressed) move.z += 1f;

        Vector3 prePos = transform.position;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.Self);
        initialLocalPosition += transform.position - prePos;

        if (keyboard.qKey.isPressed)
        {
            prePos = transform.position;
            transform.Translate(0, -moveSpeed * Time.deltaTime, 0, Space.Self);
            initialLocalPosition += transform.position - prePos;
        }

        if (keyboard.eKey.isPressed)
        {
            prePos = transform.position;
            transform.Translate(0, moveSpeed * Time.deltaTime, 0, Space.Self);
            initialLocalPosition += transform.position - prePos;
        }

        float scroll = mouse.scroll.ReadValue().y;
        float fov = _camera.fieldOfView;
        fov -= scroll * zoomSpeed * 0.01f;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        _camera.fieldOfView = fov;

        VisibleMouse();

        if (!isCursorVisible)
        {
            Vector2 delta = mouse.delta.ReadValue();

            float mouseX = delta.x * rotateSpeed * Time.deltaTime;
            float mouseY = delta.y * rotateSpeed * Time.deltaTime;

            transform.eulerAngles = new Vector3(
                transform.eulerAngles.x - mouseY,
                transform.eulerAngles.y + mouseX,
                0
            );

            _initialRotation = transform.rotation;
        }
    }
}