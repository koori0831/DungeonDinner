using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Work.Core.EventBus;
using static Work.Input.Code.InputEvents;

namespace Work.UtillUI.Code.Settings
{
    /// <summary>
    /// UI Cancel 입력으로 열고 닫는 공통 게임 설정 패널
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSettingsPanel : MonoBehaviour
    {
        private const string MASTER_VOLUME_KEY = "GameSettings.MasterVolume";
        private const float DEFAULT_MASTER_VOLUME = 1f;

        [Header("View")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeValueField;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        [Header("Pause")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private bool disablePlayerInputWhenOpen = true;

        private global::Console _inputActions;
        private float _previousTimeScale = 1f;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        /// <summary>
        /// 설정 패널이 닫힌 뒤 발생하는 이벤트
        /// </summary>
        public event Action Closed;

        private void Awake()
        {
            BindControls();
            LoadSettings();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EnableCancelInput();
        }

        private void OnDisable()
        {
            DisableCancelInput();

            if (_isOpen == true)
                Close();
        }

        private void OnDestroy()
        {
            UnbindControls();
        }

        /// <summary>
        /// 설정 패널 표시 상태 전환
        /// </summary>
        public void Toggle()
        {
            if (_isOpen == true)
                Close();
            else
                Open();
        }

        /// <summary>
        /// 설정 패널 표시 및 게임 진행 일시정지
        /// </summary>
        public void Open()
        {
            if (_isOpen == true || panelRoot == null)
                return;

            _isOpen = true;
            _previousTimeScale = Time.timeScale;
            _previousCursorLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            panelRoot.SetActive(true);
            transform.SetAsLastSibling();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseGameWhenOpen == true)
                Time.timeScale = 0f;

            if (disablePlayerInputWhenOpen == true)
                Bus<PlayerInputEnableEvent>.Raise(new PlayerInputEnableEvent(false));
        }

        /// <summary>
        /// 설정 패널 숨김 및 기존 게임 진행 상태 복원
        /// </summary>
        public void Close()
        {
            if (_isOpen == false)
                return;

            _isOpen = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);

            RestoreGameplayState();
            Closed?.Invoke();
        }

        /// <summary>
        /// 에디터 플레이 또는 실행 중인 애플리케이션 종료
        /// </summary>
        public void QuitGame()
        {
            PlayerPrefs.Save();
            if (_isOpen == true)
            {
                _isOpen = false;
                RestoreGameplayState();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleCancelPerformed(InputAction.CallbackContext context)
        {
            if (context.performed == true)
                Toggle();
        }

        private void HandleMasterVolumeChanged(float value)
        {
            float volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
            SetVolumeText(volume);
        }

        private void LoadSettings()
        {
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME));
            AudioListener.volume = volume;

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(volume);

            SetVolumeText(volume);
        }

        private void SetVolumeText(float volume)
        {
            if (masterVolumeValueField != null)
                masterVolumeValueField.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }

        private void RestoreGameplayState()
        {
            if (pauseGameWhenOpen == true)
                Time.timeScale = _previousTimeScale;

            if (disablePlayerInputWhenOpen == true)
                Bus<PlayerInputEnableEvent>.Raise(new PlayerInputEnableEvent(true));

            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
        }

        private void BindControls()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Close);
                resumeButton.onClick.AddListener(Close);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void UnbindControls()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(Close);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitGame);
        }

        private void EnableCancelInput()
        {
            if (_inputActions != null)
                return;

            _inputActions = new global::Console();
            _inputActions.UI.Cancel.performed += HandleCancelPerformed;
            _inputActions.UI.Cancel.Enable();
        }

        private void DisableCancelInput()
        {
            if (_inputActions == null)
                return;

            _inputActions.UI.Cancel.performed -= HandleCancelPerformed;
            _inputActions.UI.Cancel.Disable();
            _inputActions.Dispose();
            _inputActions = null;
        }
    }
}
