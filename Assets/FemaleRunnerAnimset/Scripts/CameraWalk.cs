using UnityEngine;
using UnityEngine.InputSystem;

public class CameraWalk : MonoBehaviour
{
    [Header("카메라 이동속도")]
    public float moveSpeed = 10.0f;

    [Header("카메라 회전 감도(마우스)")]
    public float rotateSpeed = 500.0f;

    [Header("카메라 줌 속도")]
    public float zoomSpeed = 10.0f;

    [Header("카메라 줌 최소/최댓값")]
    public float minFov = 15.0f;
    public float maxFov = 90.0f;

    protected bool isCursorVisible = true;

    private Camera _camera;

    protected virtual void Start()
    {
        _camera = GetComponent<Camera>();
    }

    protected virtual void Update()
    {
        myCameraWalk();
    }

    protected virtual void myCameraWalk()
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

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.Self);

        if (keyboard.qKey.isPressed)
        {
            transform.Translate(0, -moveSpeed * Time.deltaTime, 0, Space.Self);
        }

        if (keyboard.eKey.isPressed)
        {
            transform.Translate(0, moveSpeed * Time.deltaTime, 0, Space.Self);
        }

        float scroll = mouse.scroll.ReadValue().y;

        float fov = _camera.fieldOfView;
        fov -= scroll * zoomSpeed * 0.01f;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        _camera.fieldOfView = fov;

        VisibleMouse();

        if (!isCursorVisible)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();

            float mouseX = mouseDelta.x * rotateSpeed * Time.deltaTime;
            float mouseY = mouseDelta.y * rotateSpeed * Time.deltaTime;

            transform.eulerAngles = new Vector3(
                transform.eulerAngles.x - mouseY,
                transform.eulerAngles.y + mouseX,
                0
            );
        }
    }

    protected void VisibleMouse()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            isCursorVisible = !isCursorVisible;
            Cursor.visible = isCursorVisible;
            Cursor.lockState = isCursorVisible
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }
}