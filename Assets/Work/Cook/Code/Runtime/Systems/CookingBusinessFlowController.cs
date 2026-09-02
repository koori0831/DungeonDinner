using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Work.Core.EventBus;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.UI;
using Work.TimeSystem;

namespace Work.Cook.Code.Runtime.Systems
{
    /// <summary>
    /// 가게 접기 처리 완료 시 발생하는 이벤트
    /// </summary>
    /// <param name="Source">영업 흐름 컨트롤러</param>
    /// <param name="CurrentDay">영업 종료 시점의 일차</param>
    /// <param name="EncountersStartedToday">오늘 접대한 손님 수</param>
    /// <param name="MaxEncountersPerDay">하루 최대 접대 손님 수</param>
    public readonly record struct CookingBusinessClosedEvent(
        CookingBusinessFlowController Source,
        int CurrentDay,
        int EncountersStartedToday,
        int MaxEncountersPerDay
    ) : IEvent;

    /// <summary>
    /// 준비 화면에서 다음 음식점 운영을 시작하도록 요청하는 이벤트입니다.
    /// 날짜는 GameTimeService의 누적 시간으로 자동 계산됩니다.
    /// </summary>
    public readonly record struct CookingBusinessResumeRequestedEvent(
        CookingBusinessFlowController Target
    ) : IEvent;
    
    public sealed class CookingBusinessFlowController : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private NpcEncounterDirector encounterDirector;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private GameTimeService gameTimeService;
        [SerializeField] private bool startFirstCustomerOnStart = true;
        [SerializeField] private bool hideCookingTestPanelOnStart = true;
        [FormerlySerializedAs("startNextCustomerAfterAdvancingDay")]
        [SerializeField] private bool startNextCustomerAfterResuming = true;
        [SerializeField] private bool advanceDayWhenShopCloses = true;
        [SerializeField] private bool startNextCustomerAfterAdvancingDay = true;
        [SerializeField] private RectTransform actionRoot;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Button nextCustomerButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private string waitingText = "손님을 기다리는 중입니다.";
        [SerializeField] private string completedText = "오늘 영업을 마감했습니다.";
        [FormerlySerializedAs("nextDayText")]
        [SerializeField] private string resumedText = "다음 영업을 시작합니다.";
        private bool _dishHandedToCurrentCustomer;
        private bool _businessClosed;
        private CookingRewardGrant _lastRewardGrant;

        public void Initialize(CookingGamePanel owner, TMP_FontAsset defaultFontAsset)
        {
            if (owner != null)
                gamePanel = owner;
        }

        private void Awake()
        {
            EnsureReferences();
            EnsureControls();
            BindButtons();
            HideActions();
        }

        private void OnEnable()
        {
            EnsureReferences();
            Subscribe();
        }

        private void Start()
        {
            if (startFirstCustomerOnStart == true && _businessClosed == false)
                StartNextCustomer();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public bool StartNextCustomer()
        {
            EnsureReferences();
            HideActions();
            _dishHandedToCurrentCustomer = false;
            _lastRewardGrant = null;

            if (encounterDirector == null)
            {
                SetStatus("Missing Encounter Director");
                ShowCloseShopOnly();
                return false;
            }

            if (encounterDirector.CanStartEncounter() == false)
            {
                ShowCloseShopOnly();
                return false;
            }

            if (gamePanel != null)
                Bus<CookingNpcConversationReturnRequestedEvent>.Raise(new CookingNpcConversationReturnRequestedEvent(gamePanel));

            bool started = encounterDirector.StartEncounter();
            if (started == false)
            {
                if (gamePanel != null)
                    Bus<CookingViewsCloseRequestedEvent>.Raise(new CookingViewsCloseRequestedEvent(gamePanel));
                ShowCloseShopOnly();
                return false;
            }

            SetStatus(waitingText);
            return true;
        }

        public void CloseShop()
        {
            if (_businessClosed)
                return;

            _businessClosed = true;
            _dishHandedToCurrentCustomer = false;
            _lastRewardGrant = null;
            HideActions();
            SetStatus(completedText);
            if (gamePanel != null)
                Bus<CookingViewsCloseRequestedEvent>.Raise(new CookingViewsCloseRequestedEvent(gamePanel));
            if (gameTimeService != null)
                gameTimeService.AdvanceTime(3, GameTimeActivityType.Restaurant);
            else
                Debug.LogError("GameTimeService가 없어 음식점 운영 시간을 반영하지 못했습니다.", this);

            RaiseBusinessClosed();
        }

        private void HandleBusinessResumeRequested(CookingBusinessResumeRequestedEvent businessEvent)
        {
            if (businessEvent.Target != null && businessEvent.Target != this)
            {
                return;
            }

            if (encounterDirector == null)
            {
                return;
            }

            if (_businessClosed == false)
            {
                return;
            }

            _businessClosed = false;
            SetStatus(resumedText);

            if (startNextCustomerAfterResuming == true)
                StartNextCustomer();
        }

        private void RaiseBusinessClosed()
        {
            int currentDay = encounterDirector != null ? encounterDirector.CurrentDay : 0;
            int encountersStartedToday = encounterDirector != null ? encounterDirector.EncountersStartedToday : 0;
            int maxEncountersPerDay = encounterDirector != null ? encounterDirector.MaxEncountersPerDay : 0;

            Bus<CookingBusinessClosedEvent>.Raise(
                new CookingBusinessClosedEvent(
                    this,
                    currentDay,
                    encountersStartedToday,
                    maxEncountersPerDay));
        }

        private void HandleDishHandedToNpc(CookingDishHandedToNpcEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            _dishHandedToCurrentCustomer = true;
            HideActions();
        }

        private void HandleConversationCompleted()
        {
            if (_businessClosed == true || _dishHandedToCurrentCustomer == false)
                return;

            _dishHandedToCurrentCustomer = false;
            ShowPostCustomerAction();
        }

        private void HandleRewardGranted(CookingRewardGrantedEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            _lastRewardGrant = gameEvent.Grant;
            if (actionRoot != null && actionRoot.gameObject.activeSelf == true)
                SetStatus(BuildProgressText());
        }

        private void ShowPostCustomerAction()
        {
            EnsureReferences();
            EnsureControls();
            SetActive(actionRoot, true);

            bool canContinue = encounterDirector != null && encounterDirector.CanStartEncounter();
            if (canContinue == true)
            {
                SetStatus(BuildProgressText());
                SetActive(nextCustomerButton, true);
                SetActive(closeShopButton, false);
                return;
            }

            ShowCloseShopOnly();
        }

        private void ShowCloseShopOnly()
        {
            EnsureControls();
            SetActive(actionRoot, true);
            SetStatus(BuildProgressText());
            SetActive(nextCustomerButton, false);
            SetActive(closeShopButton, true);
        }

        private string BuildProgressText()
        {
            if (encounterDirector == null)
                return completedText;

            return BuildPostCustomerStatus(
                _lastRewardGrant,
                encounterDirector.EncountersStartedToday,
                encounterDirector.MaxEncountersPerDay);
        }

        private static string BuildPostCustomerStatus(
            CookingRewardGrant grant,
            int encountersStartedToday,
            int maxEncountersPerDay)
        {
            string progress = $"{encountersStartedToday}/{maxEncountersPerDay} 손님 접대";
            if (grant == null)
                return progress;

            string reward = grant.Amount > 0
                ? $"+{grant.Amount}"
                : "보상 없음";
            return $"{BuildResultLabel(grant.Result)} · {reward} · {progress}";
        }

        private static string BuildResultLabel(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return "완벽한 접대";
                case NpcConversationResult.Correct:
                    return "주문 만족";
                case NpcConversationResult.Similar:
                    return "흥미로운 반응";
                case NpcConversationResult.Disgusting:
                    return "요리 거부";
                default:
                    return "아쉬운 반응";
            }
        }

        private void HideActions()
        {
            SetActive(nextCustomerButton, false);
            SetActive(closeShopButton, false);
            SetActive(actionRoot, false);
        }

        private void SetStatus(string text)
        {
            if (statusField != null)
                statusField.text = text ?? string.Empty;
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
            if (encounterDirector == null)
                encounterDirector = FindFirstObjectByType<NpcEncounterDirector>();
            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();
            if (gameTimeService == null)
                gameTimeService = FindFirstObjectByType<GameTimeService>();
        }

        private void Subscribe()
        {
            if (gamePanel != null)
            {
                Bus<CookingDishHandedToNpcEvent>.Events += HandleDishHandedToNpc;
                Bus<CookingRewardGrantedEvent>.Events += HandleRewardGranted;
            }
            if (npcRunner != null)
                npcRunner.ConversationCompleted += HandleConversationCompleted;
            Bus<CookingBusinessResumeRequestedEvent>.Events += HandleBusinessResumeRequested;
        }

        private void Unsubscribe()
        {
            if (gamePanel != null)
            {
                Bus<CookingDishHandedToNpcEvent>.Events -= HandleDishHandedToNpc;
                Bus<CookingRewardGrantedEvent>.Events -= HandleRewardGranted;
            }
            if (npcRunner != null)
                npcRunner.ConversationCompleted -= HandleConversationCompleted;
            Bus<CookingBusinessResumeRequestedEvent>.Events -= HandleBusinessResumeRequested;
        }

        private void BindButtons()
        {
            if (nextCustomerButton != null)
            {
                nextCustomerButton.onClick.RemoveListener(HandleNextCustomerClicked);
                nextCustomerButton.onClick.AddListener(HandleNextCustomerClicked);
            }

            if (closeShopButton != null)
            {
                closeShopButton.onClick.RemoveListener(CloseShop);
                closeShopButton.onClick.AddListener(CloseShop);
            }
        }

        private void HandleNextCustomerClicked()
        {
            StartNextCustomer();
        }

        private void EnsureControls()
        {
            if (actionRoot != null && statusField != null && nextCustomerButton != null && closeShopButton != null)
            {
                return;
            }

            Debug.LogError("CookingBusinessFlowController is missing actionRoot/statusField/nextCustomerButton/closeShopButton references. Assign prefab or inspector references.", this);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

    }

    
}
