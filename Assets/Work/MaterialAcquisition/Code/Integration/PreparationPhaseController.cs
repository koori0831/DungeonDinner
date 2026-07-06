using System;
using UnityEngine;
using UnityEngine.Events;
using Work.Cook.Code.Runtime;
using Work.NPC.Code.Runtime;

namespace Work.MaterialAcquisition.Code.Integration
{
    [DisallowMultipleComponent]
    public sealed class PreparationPhaseController : MonoBehaviour
    {
        [Header("Existing Systems")]
        [SerializeField] private CookingBusinessFlowController businessFlowController;
        [SerializeField] private CookingGamePanel cookingGamePanel;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private MonoBehaviour dayProviderBehaviour;
        [SerializeField] private PreparationPhaseView view;

        [Header("Future Gateways")]
        [SerializeField] private MonoBehaviour dispatchGatewayBehaviour;
        [SerializeField] private MonoBehaviour adventureGatewayBehaviour;

        [Header("Policies")]
        [SerializeField] private bool allowDispatchAndAdventureSameDay = true;
        [SerializeField, Min(0)] private int maxDispatchStartsPerDay = 1;
        [SerializeField, Min(0)] private int maxAdventureStartsPerDay = 1;
        [SerializeField] private bool requireClaimReadyDispatchBeforeNextDay;
        [SerializeField] private bool startFirstCustomerOnNextDay = true;

        [Header("Runtime State")]
        [SerializeField] private PreparationPhaseState currentState = PreparationPhaseState.None;
        [SerializeField] private int activePreparationDay;
        [SerializeField] private int dispatchStartsToday;
        [SerializeField] private int adventureStartsToday;
        [SerializeField] private bool hasOpenedPreparationToday;

        [Header("Events")]
        [SerializeField] private UnityEvent preparationPhaseOpened = new UnityEvent();
        [SerializeField] private UnityEvent preparationPhaseClosed = new UnityEvent();
        [SerializeField] private UnityEvent dispatchRequested = new UnityEvent();
        [SerializeField] private UnityEvent adventureRequested = new UnityEvent();
        [SerializeField] private PreparationDayEvent dayAdvanced = new PreparationDayEvent();

        private IAcquisitionDayProvider _dayProvider;
        private IDispatchPreparationGateway _dispatchGateway;
        private IAdventurePreparationGateway _adventureGateway;
        private CookingBusinessFlowController _subscribedBusinessFlowController;

        public event Action PreparationPhaseOpened;
        public event Action PreparationPhaseClosed;
        public event Action DispatchRequested;
        public event Action AdventureRequested;
        public event Action<int> DayAdvanced;

        public PreparationPhaseState CurrentState => currentState;
        public int ActivePreparationDay => activePreparationDay;
        public int DispatchStartsToday => dispatchStartsToday;
        public int AdventureStartsToday => adventureStartsToday;
        public bool HasOpenedPreparationToday => hasOpenedPreparationToday;

        public UnityEvent PreparationPhaseOpenedEvent => preparationPhaseOpened;
        public UnityEvent PreparationPhaseClosedEvent => preparationPhaseClosed;
        public UnityEvent DispatchRequestedEvent => dispatchRequested;
        public UnityEvent AdventureRequestedEvent => adventureRequested;
        public PreparationDayEvent DayAdvancedEvent => dayAdvanced;

        private void Awake()
        {
            EnsureReferences();
            BindView();
            currentState = PreparationPhaseState.BusinessOpen;
        }

        private void OnEnable()
        {
            EnsureReferences();
            BindView();
            SubscribeBusinessFlow();
            RefreshView();
        }

        private void OnDisable()
        {
            UnsubscribeBusinessFlow();
        }

        public bool OpenPreparationPhase()
        {
            EnsureReferences();

            if (npcRunner != null && npcRunner.HasActiveConversation)
            {
                Debug.LogWarning("Preparation phase cannot open while an NPC conversation is active.", this);
                return false;
            }

            int currentDay = GetCurrentDay();
            if (currentDay <= 0)
            {
                Debug.LogWarning("Preparation phase cannot open because current day is invalid.", this);
                return false;
            }

            if (currentState == PreparationPhaseState.PreparationOpen
                || currentState == PreparationPhaseState.DispatchOpen
                || currentState == PreparationPhaseState.AdventureOpen)
            {
                RefreshView();
                return true;
            }

            currentState = PreparationPhaseState.BusinessClosing;
            ResetDailyCountsIfNeeded(currentDay);
            activePreparationDay = currentDay;
            hasOpenedPreparationToday = true;

            cookingGamePanel?.CloseCookingViews();
            RefreshDispatchGateway(currentDay);

            currentState = PreparationPhaseState.PreparationOpen;
            view?.Show();
            RefreshView();

            PreparationPhaseOpened?.Invoke();
            preparationPhaseOpened.Invoke();
            return true;
        }

        public void HidePreparationView()
        {
            view?.Hide();
        }

        public void ReturnToPreparationPhase()
        {
            if (currentState != PreparationPhaseState.DispatchOpen
                && currentState != PreparationPhaseState.AdventureOpen)
            {
                RefreshView();
                return;
            }

            currentState = PreparationPhaseState.PreparationOpen;
            view?.Show();
            RefreshView();
        }

        public bool RequestDispatch()
        {
            if (CanOpenDispatch(out string reason) == false)
            {
                RefreshView(reason);
                return false;
            }

            currentState = PreparationPhaseState.DispatchOpen;
            RefreshView();

            DispatchRequested?.Invoke();
            dispatchRequested.Invoke();
            return true;
        }

        public bool RequestAdventure()
        {
            if (CanOpenAdventure(out string reason) == false)
            {
                RefreshView(reason);
                return false;
            }

            currentState = PreparationPhaseState.AdventureOpen;
            RefreshView();

            AdventureRequested?.Invoke();
            adventureRequested.Invoke();
            return true;
        }

        public void MarkDispatchStarted()
        {
            ResetDailyCountsIfNeeded(GetCurrentDay());
            dispatchStartsToday = Mathf.Min(dispatchStartsToday + 1, Mathf.Max(0, maxDispatchStartsPerDay));
            RefreshView();
        }

        public void MarkAdventureStarted()
        {
            ResetDailyCountsIfNeeded(GetCurrentDay());
            adventureStartsToday = Mathf.Min(adventureStartsToday + 1, Mathf.Max(0, maxAdventureStartsPerDay));
            RefreshView();
        }

        public bool AdvanceToNextDay()
        {
            if (CanAdvanceDay(out string reason) == false)
            {
                RefreshView(reason);
                return false;
            }

            EnsureReferences();

            currentState = PreparationPhaseState.AdvancingDay;
            int previousDay = GetCurrentDay();
            RefreshDispatchGateway(previousDay);

            _dayProvider?.AdvanceDay();

            int newDay = GetCurrentDay();
            ResetDailyCountsForDay(newDay);
            RefreshDispatchGateway(newDay);

            ClosePreparationPhaseInternal();

            bool opened = businessFlowController == null
                || businessFlowController.OpenShopForNextDay(startFirstCustomerOnNextDay);

            DayAdvanced?.Invoke(newDay);
            dayAdvanced.Invoke(newDay);

            if (opened == false)
                Debug.LogWarning("Preparation phase advanced the day, but the next business day could not start.", this);

            return opened;
        }

        public bool CanOpenDispatch(out string reason)
        {
            reason = string.Empty;

            if (currentState != PreparationPhaseState.PreparationOpen)
            {
                reason = "준비 단계에서만 파견을 열 수 있습니다.";
                return false;
            }

            if (maxDispatchStartsPerDay <= 0)
            {
                reason = "오늘은 파견을 보낼 수 없습니다.";
                return false;
            }

            if (dispatchStartsToday >= maxDispatchStartsPerDay)
            {
                reason = "오늘 파견 가능 횟수를 모두 사용했습니다.";
                return false;
            }

            if (allowDispatchAndAdventureSameDay == false && adventureStartsToday > 0)
            {
                reason = "오늘은 이미 모험을 진행했습니다.";
                return false;
            }

            if (_adventureGateway != null && _adventureGateway.HasActiveSession)
            {
                reason = "진행 중인 모험을 먼저 종료해야 합니다.";
                return false;
            }

            return true;
        }

        public bool CanOpenAdventure(out string reason)
        {
            reason = string.Empty;

            if (currentState != PreparationPhaseState.PreparationOpen)
            {
                reason = "준비 단계에서만 모험을 열 수 있습니다.";
                return false;
            }

            if (maxAdventureStartsPerDay <= 0)
            {
                reason = "오늘은 모험을 시작할 수 없습니다.";
                return false;
            }

            if (adventureStartsToday >= maxAdventureStartsPerDay)
            {
                reason = "오늘 모험을 이미 진행했습니다.";
                return false;
            }

            if (allowDispatchAndAdventureSameDay == false && dispatchStartsToday > 0)
            {
                reason = "오늘은 이미 파견을 보냈습니다.";
                return false;
            }

            if (_adventureGateway != null && _adventureGateway.CanStartAdventure(GetCurrentDay()) == false)
            {
                reason = "현재 모험을 시작할 수 없습니다.";
                return false;
            }

            return true;
        }

        public bool CanAdvanceDay(out string reason)
        {
            reason = string.Empty;

            if (currentState != PreparationPhaseState.PreparationOpen)
            {
                reason = "파견 또는 모험 화면을 닫은 뒤 다음날로 넘어갈 수 있습니다.";
                return false;
            }

            if (_adventureGateway != null && _adventureGateway.HasActiveSession)
            {
                reason = "모험을 종료하거나 귀환해야 합니다.";
                return false;
            }

            if (requireClaimReadyDispatchBeforeNextDay
                && _dispatchGateway != null
                && _dispatchGateway.HasBlockingReadyToClaimTask)
            {
                reason = "복귀 가능한 파견 결과를 먼저 확인해야 합니다.";
                return false;
            }

            return true;
        }

        private void HandleBusinessClosed()
        {
            OpenPreparationPhase();
        }

        private void ClosePreparationPhaseInternal()
        {
            currentState = PreparationPhaseState.BusinessOpen;
            view?.Hide();

            PreparationPhaseClosed?.Invoke();
            preparationPhaseClosed.Invoke();
        }

        private void EnsureReferences()
        {
            if (businessFlowController == null)
                businessFlowController = FindFirstObjectByType<CookingBusinessFlowController>();

            if (cookingGamePanel == null)
                cookingGamePanel = FindFirstObjectByType<CookingGamePanel>();

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            if (view == null)
                view = GetComponentInChildren<PreparationPhaseView>(true);

            ResolveDayProvider();
            ResolveGateways();
        }

        private void ResolveDayProvider()
        {
            _dayProvider = dayProviderBehaviour as IAcquisitionDayProvider;
            if (_dayProvider != null)
                return;

            if (dayProviderBehaviour != null)
            {
                Debug.LogWarning(
                    $"{dayProviderBehaviour.name} must implement IAcquisitionDayProvider to be used as a day provider.",
                    this);
            }

            NpcEncounterDayProvider provider = GetComponent<NpcEncounterDayProvider>();
            if (provider == null)
                provider = FindFirstObjectByType<NpcEncounterDayProvider>();

            if (provider != null)
            {
                _dayProvider = provider;
                dayProviderBehaviour = provider;
            }
        }

        private void ResolveGateways()
        {
            _dispatchGateway = dispatchGatewayBehaviour as IDispatchPreparationGateway;
            if (dispatchGatewayBehaviour != null && _dispatchGateway == null)
            {
                Debug.LogWarning(
                    $"{dispatchGatewayBehaviour.name} must implement IDispatchPreparationGateway to be used as a dispatch gateway.",
                    this);
            }

            _adventureGateway = adventureGatewayBehaviour as IAdventurePreparationGateway;
            if (adventureGatewayBehaviour != null && _adventureGateway == null)
            {
                Debug.LogWarning(
                    $"{adventureGatewayBehaviour.name} must implement IAdventurePreparationGateway to be used as an adventure gateway.",
                    this);
            }
        }

        private void BindView()
        {
            if (view != null)
                view.Bind(this);
        }

        private void SubscribeBusinessFlow()
        {
            if (_subscribedBusinessFlowController == businessFlowController)
                return;

            UnsubscribeBusinessFlow();

            _subscribedBusinessFlowController = businessFlowController;
            if (_subscribedBusinessFlowController != null)
                _subscribedBusinessFlowController.BusinessClosed.AddListener(HandleBusinessClosed);
        }

        private void UnsubscribeBusinessFlow()
        {
            if (_subscribedBusinessFlowController == null)
                return;

            _subscribedBusinessFlowController.BusinessClosed.RemoveListener(HandleBusinessClosed);
            _subscribedBusinessFlowController = null;
        }

        private void RefreshDispatchGateway(int currentDay)
        {
            _dispatchGateway?.RefreshTasksForDay(currentDay);
        }

        private void RefreshView(string statusOverride = null)
        {
            if (view == null)
                return;

            string dispatchReason;
            string adventureReason;
            string advanceReason;
            bool canOpenDispatch = CanOpenDispatch(out dispatchReason);
            bool canOpenAdventure = CanOpenAdventure(out adventureReason);
            bool canAdvance = CanAdvanceDay(out advanceReason);
            int activeDispatchCount = _dispatchGateway?.ActiveTaskCount ?? 0;
            int readyDispatchCount = _dispatchGateway?.ReadyToClaimCount ?? 0;
            string summary = string.IsNullOrWhiteSpace(statusOverride)
                ? BuildSummaryText(activeDispatchCount, readyDispatchCount)
                : statusOverride;

            view.Refresh(new PreparationPhaseViewData(
                currentState,
                BuildDayText(),
                summary,
                canOpenDispatch,
                dispatchReason,
                canOpenAdventure,
                adventureReason,
                canAdvance,
                advanceReason,
                activeDispatchCount,
                readyDispatchCount));
        }

        private string BuildSummaryText(int activeDispatchCount, int readyDispatchCount)
        {
            string dispatchLimitText = maxDispatchStartsPerDay <= 0
                ? "파견 불가"
                : $"파견 {dispatchStartsToday}/{maxDispatchStartsPerDay}";
            string adventureLimitText = maxAdventureStartsPerDay <= 0
                ? "모험 불가"
                : $"모험 {adventureStartsToday}/{maxAdventureStartsPerDay}";

            return $"{dispatchLimitText} / {adventureLimitText} / 진행 중인 파견 {activeDispatchCount}건 / 복귀 가능 {readyDispatchCount}건";
        }

        private string BuildDayText()
        {
            if (_dayProvider == null)
                return "날짜 정보 없음";

            string text = _dayProvider.CurrentDayText;
            if (string.IsNullOrWhiteSpace(text) == false)
                return text;

            int day = _dayProvider.CurrentDay;
            return day > 0 ? $"{day}일차" : "날짜 정보 없음";
        }

        private int GetCurrentDay()
        {
            EnsureReferencesWithoutRecursion();
            return _dayProvider != null ? _dayProvider.CurrentDay : 0;
        }

        private void EnsureReferencesWithoutRecursion()
        {
            if (_dayProvider == null)
                ResolveDayProvider();

            if (_dispatchGateway == null || _adventureGateway == null)
                ResolveGateways();
        }

        private void ResetDailyCountsIfNeeded(int currentDay)
        {
            if (activePreparationDay == currentDay)
                return;

            ResetDailyCountsForDay(currentDay);
        }

        private void ResetDailyCountsForDay(int currentDay)
        {
            activePreparationDay = Mathf.Max(0, currentDay);
            dispatchStartsToday = 0;
            adventureStartsToday = 0;
            hasOpenedPreparationToday = false;
        }

        [Serializable]
        public sealed class PreparationDayEvent : UnityEvent<int>
        {
        }
    }
}
