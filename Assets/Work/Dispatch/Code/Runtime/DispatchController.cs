using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Core.EventBus;
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
        [SerializeField] private NpcConversationRunner npcRunner;

        [Header("UI")]
        [SerializeField] private RectTransform generatedUiRoot;
        [SerializeField] private DispatchMapView mapView;
        [SerializeField] private DispatchProgressView progressView;
        [SerializeField] private DispatchResultView resultView;
        [SerializeField] private Button openButton;

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
        private bool _loggedMissingUiReferences;

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
            EnsureUiReferences();
            BindOpenButton();
            HideViews();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SyncNpcRunnerSubscription();
            EnsureUiReferences();
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
        /// 파견 지도 UI 열기
        /// </summary>
        /// <returns>지도 열기 성공 여부</returns>
        public bool OpenMap()
        {
            EnsureReferences();
            SyncNpcRunnerSubscription();
            EnsureUiReferences();

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
            mapView.Show(this, dispatchMap);
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
            EnsureUiReferences();

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

            if (point == null)
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

            Bus<InventoryItemsAddRequestedEvent>.Raise(new InventoryItemsAddRequestedEvent(itemStacks, 0, validRewardCount));

            int addedAmount = 0;

            for (int i = 0; i < validRewardCount; i++)
            {
                InventoryItemStack itemStack = itemStacks[i];
                addedAmount += itemStack.Amount;

                entries.Add(new DispatchRewardResultEntry(
                    itemStack.Item,
                    itemStack.Amount,
                    itemStack.Amount,
                    0,
                    0));
            }

            lastRewardAddedAmount = addedAmount;
            lastRewardRemainingAmount = 0;
            return new DispatchRewardResult(point, entries, addedAmount, 0);
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

        private bool EnsureUiReferences()
        {
            bool hasRequiredReferences = generatedUiRoot != null
                                         && mapView != null
                                         && progressView != null
                                         && resultView != null
                                         && openButton != null;
            if (hasRequiredReferences == true)
            {
                _loggedMissingUiReferences = false;
                return true;
            }

            if (_loggedMissingUiReferences == false)
            {
                _loggedMissingUiReferences = true;
                Debug.LogError("DispatchController is missing generatedUiRoot/mapView/progressView/resultView/openButton inspector references. Place the UI instances in the scene and assign them before runtime.", this);
            }

            return false;
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
