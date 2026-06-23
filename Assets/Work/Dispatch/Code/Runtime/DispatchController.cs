using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Dispatch.Code.Data;
using Work.Items.Code;
using Work.NPC.Code.Runtime;
using Work.Players.Code.Inventory;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 지도 선택, 진행 시간, 보상 지급 흐름을 관리
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DispatchController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private DispatchMapSO dispatchMap;
        [SerializeField] private PlayerInventoryModule inventoryModule;
        [SerializeField] private NpcConversationRunner npcRunner;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform generatedUiRoot;
        [SerializeField] private DispatchMapView mapView;
        [SerializeField] private DispatchProgressView progressView;
        [SerializeField] private DispatchResultView resultView;
        [SerializeField] private Button openButton;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite labelSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private bool autoCreateDefaultUI = true;
        [SerializeField] private bool autoCreateOpenButton = true;
        [SerializeField] private string openButtonText = "파견";

        [Header("Last Result")]
        [SerializeField] private int lastRewardAddedAmount;
        [SerializeField] private int lastRewardRemainingAmount;

        [Header("Events")]
        [SerializeField] private DispatchPointUnityEvent dispatchStarted = new DispatchPointUnityEvent();
        [SerializeField] private DispatchPointUnityEvent dispatchCompleted = new DispatchPointUnityEvent();
        [SerializeField] private UnityEvent mapOpened = new UnityEvent();
        [SerializeField] private UnityEvent mapClosed = new UnityEvent();

        private bool _isDispatching;
        private DispatchPointSO _currentPoint;
        private CancellationTokenSource _dispatchCancellationTokenSource;
        private NpcConversationRunner _subscribedNpcRunner;

        /// <summary>
        /// 파견이 시작될 때 발생하는 이벤트
        /// </summary>
        public event Action<DispatchPointSO> DispatchStarted;

        /// <summary>
        /// 파견이 완료될 때 발생하는 이벤트
        /// </summary>
        public event Action<DispatchPointSO> DispatchCompleted;

        /// <summary>
        /// 현재 파견 진행 중 여부
        /// </summary>
        public bool IsDispatching => _isDispatching;

        /// <summary>
        /// 현재 진행 중인 파견 포인트
        /// </summary>
        public DispatchPointSO CurrentPoint => _currentPoint;

        /// <summary>
        /// 마지막 파견 보상 중 실제 추가된 총수량
        /// </summary>
        public int LastRewardAddedAmount => lastRewardAddedAmount;

        /// <summary>
        /// 마지막 파견 보상 중 인벤토리에 들어가지 못한 총수량
        /// </summary>
        public int LastRewardRemainingAmount => lastRewardRemainingAmount;

        /// <summary>
        /// 인스펙터용 파견 시작 이벤트
        /// </summary>
        public DispatchPointUnityEvent DispatchStartedEvent => dispatchStarted;

        /// <summary>
        /// 인스펙터용 파견 완료 이벤트
        /// </summary>
        public DispatchPointUnityEvent DispatchCompletedEvent => dispatchCompleted;

        private void Awake()
        {
            EnsureReferences();
            EnsureDefaultUI();
            BindOpenButton();
            HideViews();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SyncNpcRunnerSubscription();
            EnsureDefaultUI();
            BindOpenButton();
            RefreshOpenButtonInteractable();
        }

        private void OnDisable()
        {
            UnsubscribeNpcRunnerEvents();
        }

        private void OnDestroy()
        {
            CancelCurrentDispatch();
            UnbindOpenButton();
            UnsubscribeNpcRunnerEvents();
        }

        /// <summary>
        /// 파견 지도 데이터 지정
        /// </summary>
        /// <param name="map">사용할 파견 지도</param>
        public void SetDispatchMap(DispatchMapSO map)
        {
            dispatchMap = map;
        }

        /// <summary>
        /// 보상을 지급할 인벤토리 지정
        /// </summary>
        /// <param name="inventory">대상 인벤토리</param>
        public void SetInventoryModule(PlayerInventoryModule inventory)
        {
            inventoryModule = inventory;
        }

        /// <summary>
        /// 파견 지도 UI 열기
        /// </summary>
        /// <returns>지도 열기 성공 여부</returns>
        public bool OpenMap()
        {
            EnsureReferences();
            SyncNpcRunnerSubscription();
            EnsureDefaultUI();

            if (_isDispatching == true)
            {
                return false;
            }

            if (IsGuestHandlingActive() == true)
            {
                RefreshOpenButtonInteractable();
                return false;
            }

            if (dispatchMap == null)
            {
                Debug.LogWarning("DispatchController needs a DispatchMapSO before opening the map.", this);
                return false;
            }

            if (mapView == null)
            {
                Debug.LogWarning("DispatchController needs a DispatchMapView before opening the map.", this);
                return false;
            }

            progressView?.Hide();
            resultView?.Hide();
            mapView.Show(this, dispatchMap, fontAsset);
            mapOpened.Invoke();
            RefreshOpenButtonInteractable();
            return true;
        }

        /// <summary>
        /// 파견 지도 UI 닫기
        /// </summary>
        /// <returns>지도 닫기 성공 여부</returns>
        public bool CloseMap()
        {
            if (_isDispatching == true)
            {
                return false;
            }

            mapView?.Hide();
            mapClosed.Invoke();
            RefreshOpenButtonInteractable();
            return true;
        }

        /// <summary>
        /// 지정 포인트로 파견 시작
        /// </summary>
        /// <param name="point">파견할 지도 포인트</param>
        /// <returns>파견 시작 성공 여부</returns>
        public bool StartDispatch(DispatchPointSO point)
        {
            EnsureReferences();
            SyncNpcRunnerSubscription();
            EnsureDefaultUI();

            if (_isDispatching == true)
            {
                return false;
            }

            if (IsGuestHandlingActive() == true)
            {
                RefreshOpenButtonInteractable();
                return false;
            }

            if (point == null)
            {
                Debug.LogWarning("DispatchController cannot start dispatch because the point is missing.", this);
                return false;
            }

            if (point.HasValidReward == false)
            {
                Debug.LogWarning($"Dispatch point has no valid rewards. point={point.DisplayName}", this);
                return false;
            }

            if (inventoryModule == null)
            {
                Debug.LogWarning("DispatchController cannot grant dispatch rewards because PlayerInventoryModule is missing.", this);
                return false;
            }

            if (progressView == null)
            {
                Debug.LogWarning("DispatchController needs a DispatchProgressView before starting dispatch.", this);
                return false;
            }

            _isDispatching = true;
            _currentPoint = point;
            lastRewardAddedAmount = 0;
            lastRewardRemainingAmount = 0;

            mapView?.Hide();
            progressView.Show(point);
            RefreshOpenButtonInteractable();
            DispatchStarted?.Invoke(point);
            dispatchStarted.Invoke(point);

            CancellationTokenSource dispatchTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _dispatchCancellationTokenSource = dispatchTokenSource;
            RunDispatchAsync(point, dispatchTokenSource).Forget();
            return true;
        }

        private async UniTask RunDispatchAsync(DispatchPointSO point, CancellationTokenSource dispatchTokenSource)
        {
            CancellationToken cancellationToken = dispatchTokenSource.Token;
            bool completed = false;

            try
            {
                float duration = point != null ? point.DurationSeconds : 0f;
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    float progress = duration > 0f ? Mathf.Clamp01(elapsedTime / duration) : 1f;
                    float remainingSeconds = Mathf.Max(0f, duration - elapsedTime);
                    progressView?.SetProgress(progress, remainingSeconds);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    elapsedTime += Time.deltaTime;
                }

                progressView?.SetProgress(1f, 0f);
                DispatchRewardResult result = GrantRewards(point);
                completed = true;
                DispatchCompleted?.Invoke(point);
                dispatchCompleted.Invoke(point);
                ShowResultOrComplete(result);
            }
            catch (OperationCanceledException)
            {
                // 오브젝트 파괴 또는 명시적 취소로 인한 종료는 정상 흐름으로 처리
            }
            finally
            {
                bool shouldResetState = completed == false && cancellationToken.IsCancellationRequested == false;
                if (shouldResetState == true)
                {
                    FinishDispatchInteraction();
                }

                if (_dispatchCancellationTokenSource == dispatchTokenSource)
                {
                    _dispatchCancellationTokenSource = null;
                }

                dispatchTokenSource.Dispose();
            }
        }

        private DispatchRewardResult GrantRewards(DispatchPointSO point)
        {
            List<DispatchRewardResultEntry> entries = new List<DispatchRewardResultEntry>();

            if (point == null || inventoryModule == null)
            {
                return new DispatchRewardResult(point, entries, 0, 0);
            }

            IReadOnlyList<DispatchRewardEntry> rewards = point.Rewards;
            if (rewards == null || rewards.Count == 0)
            {
                return new DispatchRewardResult(point, entries, 0, 0);
            }

            InventoryItemStack[] itemStacks = new InventoryItemStack[rewards.Count];
            int validRewardCount = 0;

            for (int i = 0; i < rewards.Count; i++)
            {
                DispatchRewardEntry reward = rewards[i];
                if (reward == null || reward.IsValid == false)
                {
                    continue;
                }

                InventoryItemStack itemStack = reward.CreateItemStack();
                if (itemStack.IsValid == false)
                {
                    continue;
                }

                itemStacks[validRewardCount] = itemStack;
                validRewardCount++;
            }

            if (validRewardCount <= 0)
            {
                return new DispatchRewardResult(point, entries, 0, 0);
            }

            InventoryAddResult[] addResults = new InventoryAddResult[validRewardCount];
            InventoryBatchAddResult result = inventoryModule.AddItems(itemStacks, 0, validRewardCount, addResults, 0);
            lastRewardAddedAmount = result.AddedAmount;
            lastRewardRemainingAmount = result.RemainingAmount;

            for (int i = 0; i < validRewardCount; i++)
            {
                InventoryAddResult addResult = addResults[i];
                int currentInventoryAmount = inventoryModule.GetItemAmount(addResult.Item);
                entries.Add(new DispatchRewardResultEntry(
                    addResult.Item,
                    addResult.RequestedAmount,
                    addResult.AddedAmount,
                    addResult.RemainingAmount,
                    currentInventoryAmount));
            }

            if (result.IsFullyAdded == false)
            {
                Debug.LogWarning(
                    $"Dispatch rewards were only partially added. added={result.AddedAmount}, remaining={result.RemainingAmount}",
                    this);
            }

            return new DispatchRewardResult(point, entries, result.AddedAmount, result.RemainingAmount);
        }

        private void ShowResultOrComplete(DispatchRewardResult result)
        {
            progressView?.Hide();

            if (resultView == null)
            {
                FinishDispatchInteraction();
                return;
            }

            resultView.Show(result, FinishDispatchInteraction);
        }

        private void FinishDispatchInteraction()
        {
            _isDispatching = false;
            _currentPoint = null;
            progressView?.Hide();
            RefreshOpenButtonInteractable();
        }

        private void EnsureReferences()
        {
            if (inventoryModule == null)
            {
                inventoryModule = GetComponentInParent<PlayerInventoryModule>();
            }

            if (inventoryModule == null)
            {
                inventoryModule = GetComponentInChildren<PlayerInventoryModule>(true);
            }

            if (inventoryModule == null)
            {
                inventoryModule = FindFirstObjectByType<PlayerInventoryModule>();
            }

            if (npcRunner == null)
            {
                npcRunner = GetComponentInParent<NpcConversationRunner>();
            }

            if (npcRunner == null)
            {
                npcRunner = GetComponentInChildren<NpcConversationRunner>(true);
            }

            if (npcRunner == null)
            {
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();
            }
        }

        private void EnsureDefaultUI()
        {
            if (autoCreateDefaultUI == false)
            {
                return;
            }

            RectTransform root = EnsureGeneratedUiRoot();
            if (root == null)
            {
                return;
            }

            if (mapView == null)
            {
                GameObject mapObject = new GameObject("TempDispatchMapView", typeof(RectTransform), typeof(DispatchMapView));
                mapObject.transform.SetParent(root, false);
                DispatchDefaultUiUtility.StretchToParent(mapObject.GetComponent<RectTransform>());
                mapView = mapObject.GetComponent<DispatchMapView>();
                mapView.SetFontAsset(fontAsset);
            }

            if (mapView != null)
            {
                mapView.SetUiSprites(panelSprite, labelSprite, buttonSprite);
            }

            if (progressView == null)
            {
                GameObject progressObject = new GameObject("TempDispatchProgressView", typeof(RectTransform), typeof(DispatchProgressView));
                progressObject.transform.SetParent(root, false);
                DispatchDefaultUiUtility.StretchToParent(progressObject.GetComponent<RectTransform>());
                progressView = progressObject.GetComponent<DispatchProgressView>();
                progressView.SetFontAsset(fontAsset);
            }

            if (resultView == null)
            {
                GameObject resultObject = new GameObject("TempDispatchResultView", typeof(RectTransform), typeof(DispatchResultView));
                resultObject.transform.SetParent(root, false);
                DispatchDefaultUiUtility.StretchToParent(resultObject.GetComponent<RectTransform>());
                resultView = resultObject.GetComponent<DispatchResultView>();
                resultView.SetFontAsset(fontAsset);
            }

            if (openButton == null && autoCreateOpenButton == true)
            {
                openButton = CreateOpenButton(root);
            }
        }

        private RectTransform EnsureGeneratedUiRoot()
        {
            if (generatedUiRoot != null)
            {
                return generatedUiRoot;
            }

            Canvas canvas = EnsureTargetCanvas();
            if (canvas == null)
            {
                return null;
            }

            GameObject rootObject = new GameObject("DispatchTempUiRoot", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
            generatedUiRoot = rootObject.GetComponent<RectTransform>();
            DispatchDefaultUiUtility.StretchToParent(generatedUiRoot);
            return generatedUiRoot;
        }

        private Canvas EnsureTargetCanvas()
        {
            if (targetCanvas != null)
            {
                return targetCanvas;
            }

            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas != null)
            {
                return targetCanvas;
            }

            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas != null)
            {
                return targetCanvas;
            }

            GameObject canvasObject = new GameObject("DispatchTempCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = canvasObject.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;
            return targetCanvas;
        }

        private Button CreateOpenButton(Transform parent)
        {
            Button button = DispatchDefaultUiUtility.CreateButton(
                parent,
                "DispatchOpenButton",
                openButtonText,
                new Color(0.43f, 0.29f, 0.16f, 1f),
                fontAsset,
                OpenMapClicked);
            ApplyOpenButtonSprite(button);

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-28f, -28f);
            rectTransform.sizeDelta = new Vector2(132f, 48f);
            return button;
        }

        private void ApplyOpenButtonSprite(Button button)
        {
            if (button == null || buttonSprite == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, Color.gray, 0.1f);
            colors.pressedColor = Color.Lerp(Color.white, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            button.colors = colors;
        }

        private void BindOpenButton()
        {
            if (openButton == null)
            {
                return;
            }

            openButton.onClick.RemoveListener(OpenMapClicked);
            openButton.onClick.AddListener(OpenMapClicked);
            RefreshOpenButtonInteractable();
        }

        private void UnbindOpenButton()
        {
            if (openButton == null)
            {
                return;
            }

            openButton.onClick.RemoveListener(OpenMapClicked);
        }

        private void SyncNpcRunnerSubscription()
        {
            if (_subscribedNpcRunner == npcRunner)
            {
                return;
            }

            UnsubscribeNpcRunnerEvents();

            _subscribedNpcRunner = npcRunner;
            if (_subscribedNpcRunner == null)
            {
                return;
            }

            _subscribedNpcRunner.ConversationStarted += HandleNpcConversationStarted;
            _subscribedNpcRunner.ConversationCompleted += HandleNpcConversationCompleted;
        }

        private void UnsubscribeNpcRunnerEvents()
        {
            if (_subscribedNpcRunner == null)
            {
                return;
            }

            _subscribedNpcRunner.ConversationStarted -= HandleNpcConversationStarted;
            _subscribedNpcRunner.ConversationCompleted -= HandleNpcConversationCompleted;
            _subscribedNpcRunner = null;
        }

        private void HandleNpcConversationStarted()
        {
            if (_isDispatching == false)
            {
                mapView?.Hide();
            }

            RefreshOpenButtonInteractable();
        }

        private void HandleNpcConversationCompleted()
        {
            RefreshOpenButtonInteractable();
        }

        private void OpenMapClicked()
        {
            OpenMap();
        }

        private bool IsGuestHandlingActive()
        {
            return npcRunner != null && npcRunner.HasActiveConversation == true;
        }

        private void RefreshOpenButtonInteractable()
        {
            if (openButton == null)
            {
                return;
            }

            openButton.interactable = _isDispatching == false && IsGuestHandlingActive() == false;
        }

        private void HideViews()
        {
            mapView?.Hide();
            progressView?.Hide();
            resultView?.Hide();
            RefreshOpenButtonInteractable();
        }

        private void CancelCurrentDispatch()
        {
            if (_dispatchCancellationTokenSource == null)
            {
                return;
            }

            _dispatchCancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// 파견 포인트를 전달하는 UnityEvent
    /// </summary>
    [Serializable]
    public sealed class DispatchPointUnityEvent : UnityEvent<DispatchPointSO>
    {
    }
}
