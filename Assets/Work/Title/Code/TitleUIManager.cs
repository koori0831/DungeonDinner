using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Work.Core.EventBus;
using Work.UtillUI.Code.Fade;

[DisallowMultipleComponent]
public class TitleUIManager : MonoBehaviour
{
    [Header("Selection Effect")]
    [SerializeField] private RectTransform selectionEffect;
    [SerializeField] private Vector2 selectionOffset;
    [SerializeField, Min(0f)] private float selectionMoveTime = 0.12f;

    [Header("Start Action")]
    [SerializeField] private bool loadSceneOnStart = true;
    [SerializeField] private string startSceneName = "AdventureTestScene";

    [Header("Settings Action")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private bool closeSettingsPanelOnStart = true;
    [SerializeField] private bool toggleSettingsPanel = true;

    [Header("Exit Action")]
    [SerializeField] private bool quitGameOnExit = true;

    [Header("Button Events")]
    [SerializeField] private UnityEvent onStartClicked;
    [SerializeField] private UnityEvent onSettingsClicked;
    [SerializeField] private UnityEvent onExitClicked;

    private Vector2 _selectionTargetPosition;
    private Vector2 _selectionVelocity;
    private TitleUIButton _hoveredButton;
    private TitleUIButton _lockedButton;
    private bool _hasSelectionTarget;
    private bool _isSceneLoading;
    private bool _loggedMissingSelectionEffect;
    private bool _loggedMissingSettingsPanel;

    private void OnEnable()
    {
        Bus<TitleSettingsPanelClosedEvent>.Events += HandleSettingsPanelClosed;
    }

    private void OnDisable()
    {
        Bus<TitleSettingsPanelClosedEvent>.Events -= HandleSettingsPanelClosed;
    }

    private void Awake()
    {
        if (settingsPanel != null && closeSettingsPanelOnStart)
            settingsPanel.SetActive(false);

        if (selectionEffect != null)
            selectionEffect.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (selectionEffect == null || _hasSelectionTarget == false)
            return;

        if (selectionMoveTime <= 0f)
        {
            selectionEffect.anchoredPosition = _selectionTargetPosition;
            _selectionVelocity = Vector2.zero;
            return;
        }

        selectionEffect.anchoredPosition = Vector2.SmoothDamp(
            selectionEffect.anchoredPosition,
            _selectionTargetPosition,
            ref _selectionVelocity,
            selectionMoveTime);

        if (Vector2.SqrMagnitude(selectionEffect.anchoredPosition - _selectionTargetPosition) <= 0.01f)
        {
            selectionEffect.anchoredPosition = _selectionTargetPosition;
            _selectionVelocity = Vector2.zero;
        }
    }

    public void MoveSelectionTo(TitleUIButton button)
    {
        if (button == null)
            return;

        _hoveredButton = button;

        if (_lockedButton != null)
            return;

        MoveSelectionTo(button, false);
    }

    public void HideSelection(TitleUIButton button)
    {
        if (_hoveredButton == button)
            _hoveredButton = null;

        if (_lockedButton != null)
            return;

        if (_hoveredButton != null && button != null && _hoveredButton != button)
            return;

        _hoveredButton = null;
        _hasSelectionTarget = false;
        _selectionVelocity = Vector2.zero;

        if (selectionEffect != null)
            selectionEffect.gameObject.SetActive(false);
    }

    public void InvokeButton(TitleUIButton button)
    {
        if (button == null)
            return;

        LockSelectionTo(button);

        switch (button.Action)
        {
            case TitleButtonAction.Start:
                StartGame();
                break;
            case TitleButtonAction.Settings:
                ToggleSettings();
                break;
            case TitleButtonAction.Exit:
                ExitGame();
                break;
        }
    }

    public void StartGame()
    {
        if (_isSceneLoading)
            return;

        onStartClicked?.Invoke();

        if (loadSceneOnStart == false)
            return;

        if (string.IsNullOrWhiteSpace(startSceneName) == false)
        {
            StartCoroutine(LoadStartSceneAsync(startSceneName));
            return;
        }

        LoadFallbackBuildScene();
    }

    private IEnumerator LoadStartSceneAsync(string sceneName)
    {
        _isSceneLoading = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false;

        bool fadeCompleted = false;
        Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() => fadeCompleted = true));

        while (loadOperation.progress < 0.9f || fadeCompleted == false)
            yield return null;

        loadOperation.allowSceneActivation = true;

        while (loadOperation.isDone == false)
            yield return null;
    }

    public void ToggleSettings()
    {
        onSettingsClicked?.Invoke();

        if (settingsPanel == null)
        {
            LogMissingSettingsPanel();
            return;
        }

        if (settingsPanel.activeSelf && toggleSettingsPanel)
        {
            CloseSettings();
            return;
        }

        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        Bus<TitleSettingsPanelClosedEvent>.Raise(new TitleSettingsPanelClosedEvent());
    }

    public void ExitGame()
    {
        onExitClicked?.Invoke();

        if (quitGameOnExit == false)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void MoveSelectionTo(TitleUIButton button, bool snap)
    {
        if (selectionEffect == null)
        {
            LogMissingSelectionEffect();
            return;
        }

        if (button == null || button.SelectionTarget == null)
            return;

        _selectionTargetPosition = GetSelectionTargetPosition(button.SelectionTarget);
        _hasSelectionTarget = true;
        selectionEffect.gameObject.SetActive(true);

        if (snap)
        {
            selectionEffect.anchoredPosition = _selectionTargetPosition;
            _selectionVelocity = Vector2.zero;
        }
    }

    private void LockSelectionTo(TitleUIButton button)
    {
        if (button == null)
            return;

        _lockedButton = button;
        _hoveredButton = button;
        MoveSelectionTo(button, false);
    }

    private void UnlockSelection()
    {
        _lockedButton = null;

        if (_hoveredButton != null)
        {
            MoveSelectionTo(_hoveredButton, false);
            return;
        }

        _hasSelectionTarget = false;
        _selectionVelocity = Vector2.zero;

        if (selectionEffect != null)
            selectionEffect.gameObject.SetActive(false);
    }

    private void HandleSettingsPanelClosed(TitleSettingsPanelClosedEvent evt)
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (_lockedButton != null && _lockedButton.Action == TitleButtonAction.Settings)
            UnlockSelection();
    }

    private Vector2 GetSelectionTargetPosition(RectTransform target)
    {
        RectTransform selectionParent = selectionEffect.parent as RectTransform;
        if (selectionParent == null)
            return selectionEffect.anchoredPosition + selectionOffset;

        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);
        Camera canvasCamera = GetCanvasCamera(selectionParent);
        Vector2 targetScreenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, targetWorldCenter);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selectionParent,
                targetScreenPoint,
                canvasCamera,
                out Vector2 targetLocalPoint))
        {
            return targetLocalPoint + selectionOffset;
        }

        return selectionEffect.anchoredPosition + selectionOffset;
    }

    private void LoadFallbackBuildScene()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        if (sceneCount <= 0)
        {
            Debug.LogWarning("Start button could not load a scene because Build Settings has no scenes.", this);
            return;
        }

        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex = currentBuildIndex >= 0 && currentBuildIndex + 1 < sceneCount
            ? currentBuildIndex + 1
            : 0;

        SceneManager.LoadScene(nextBuildIndex);
    }

    private static Camera GetCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void LogMissingSelectionEffect()
    {
        if (_loggedMissingSelectionEffect)
            return;

        Debug.LogWarning("TitleUIManager needs a selectionEffect RectTransform.", this);
        _loggedMissingSelectionEffect = true;
    }

    private void LogMissingSettingsPanel()
    {
        if (_loggedMissingSettingsPanel)
            return;

        Debug.LogWarning("TitleUIManager needs a settingsPanel GameObject for the Settings button.", this);
        _loggedMissingSettingsPanel = true;
    }
}

public readonly record struct TitleSettingsPanelClosedEvent : IEvent;
