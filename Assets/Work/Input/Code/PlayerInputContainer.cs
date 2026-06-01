using UnityEngine;
using Work.Core.EventBus;
using UnityEngine.InputSystem;
using static Work.Input.Code.InputEvents;

namespace Work.Input.Code
{
    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad
    }

    public class PlayerInputContainer : Console.IPlayerActions
    {
        private Console _console;
        private bool _isSubscribed = false;

        public Vector2 MoveVector { get; private set; }
        public InputDeviceType CurrentDeviceType { get; private set; } = InputDeviceType.KeyboardMouse;

        public void Initialize()
        {
            if (_console == null) 
            {
                _console = new Console();
                _console.Player.SetCallbacks(this);
                if (!_isSubscribed)
                {
                    Bus<PlayerInputEnableEvent>.Events += OnPlayerInputEnable;
                    _isSubscribed = true;
                }
            }
            _console.Player.Enable();
        }

        public void Uninitialize()
        {
            if (_isSubscribed)
            {
                Bus<PlayerInputEnableEvent>.Events -= OnPlayerInputEnable;
                _isSubscribed = false;
            }

            if (_console != null)
            {
                _console.Player.Disable();
            }
        }


        #region DefaultMovement
        public void OnMove(InputAction.CallbackContext context)
        {
            UpdateCurrentDeviceType(context);
            MoveVector = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            UpdateCurrentDeviceType(context);
            if (context.performed)
                Bus<InputJumpEvent>.Raise(new InputJumpEvent());
        }
        #endregion

        public void OnInteract(InputAction.CallbackContext context)
        {
            UpdateCurrentDeviceType(context);
            if (context.performed)
                Bus<InputInteractEvent>.Raise(new InputInteractEvent());
        }

        private void UpdateCurrentDeviceType(InputAction.CallbackContext context)
        {
            if (context.control.device is Keyboard || context.control.device is Mouse)
            {
                CurrentDeviceType = InputDeviceType.KeyboardMouse;
            }
            else if (context.control.device is Gamepad)
            {
                CurrentDeviceType = InputDeviceType.Gamepad;
            }
        }

        private void OnPlayerInputEnable(PlayerInputEnableEvent evt)
        {
            if (_console == null)
                return;

            if (evt.Enable)
                _console.Player.Enable();
            else
            {
                _console.Player.Disable();
                MoveVector = Vector2.zero;
            }
        }
    }
}
