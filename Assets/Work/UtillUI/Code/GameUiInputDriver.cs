using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Work.UtillUI.Code
{
    [DefaultExecutionOrder(-2000)]
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public sealed class GameUiInputDriver : MonoBehaviour
    {
        private InputSystemUIInputModule _module;
        private InputActionReference _originalSubmit;
        private InputActionReference _submitReference;
        private InputActionAsset _actions;

        private void Awake()
        {
            _module = GetComponent<InputSystemUIInputModule>();
            _originalSubmit = _module.submit;
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = _actions.AddActionMap("ScreenUI");
            var submit = map.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
            submit.AddBinding("<Keyboard>/numpadEnter");
            submit.AddBinding("<Gamepad>/buttonSouth");
            _submitReference = InputActionReference.Create(submit);
            _module.submit = _submitReference;
            _actions.Enable();
        }

        private void Update()
        {
            GameUiInput.PollRelease();
            if (_module != null) _module.enabled = !GameUiInput.IsBlocked;
        }

        private void OnDestroy()
        {
            if (_module != null) _module.submit = _originalSubmit;
            if (_actions != null) { _actions.Disable(); Destroy(_actions); }
            if (_submitReference != null) Destroy(_submitReference);
        }
    }
}
