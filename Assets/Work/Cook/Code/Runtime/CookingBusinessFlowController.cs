using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingBusinessFlowController : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private NpcEncounterDirector encounterDirector;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private bool startFirstCustomerOnStart = true;
        [SerializeField] private bool hideCookingTestPanelOnStart = true;
        [SerializeField] private bool advanceDayWhenShopCloses = true;
        [SerializeField] private bool startNextCustomerAfterAdvancingDay = true;
        [SerializeField] private RectTransform actionRoot;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Button nextCustomerButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private string waitingText = "\uC190\uB2D8\uC744 \uAE30\uB2E4\uB9AC\uB294 \uC911\uC785\uB2C8\uB2E4.";
        [SerializeField] private string completedText = "\uC624\uB298 \uC7A5\uC0AC\uB97C \uB9C8\uCCE4\uC2B5\uB2C8\uB2E4.";
        [SerializeField] private string nextDayText = "\uB2E4\uC74C\uB0A0 \uC601\uC5C5\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.";
        [SerializeField] private UnityEvent businessClosed = new UnityEvent();

        private bool _dishHandedToCurrentCustomer;
        private bool _businessClosed;

        public UnityEvent BusinessClosed => businessClosed;

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

            if (hideCookingTestPanelOnStart)
                HideCookingTestPanels();
        }

        private void Start()
        {
            if (startFirstCustomerOnStart && _businessClosed == false)
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

            if (encounterDirector == null)
            {
                SetStatus("\uC190\uB2D8 \uAD00\uB9AC\uC790\uB97C \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.");
                ShowCloseShopOnly();
                return false;
            }

            if (encounterDirector.CanStartEncounter() == false)
            {
                ShowCloseShopOnly();
                return false;
            }

            gamePanel?.ReturnToNpcConversation();

            bool started = encounterDirector.StartEncounter();
            if (started == false)
            {
                gamePanel?.CloseCookingViews();
                ShowCloseShopOnly();
                return false;
            }

            SetStatus(waitingText);
            return true;
        }

        public void CloseShop()
        {
            _businessClosed = true;
            _dishHandedToCurrentCustomer = false;
            HideActions();
            SetStatus(completedText);
            gamePanel?.CloseCookingViews();
            businessClosed.Invoke();

            if (advanceDayWhenShopCloses == false || encounterDirector == null)
                return;

            encounterDirector.AdvanceDay();
            _businessClosed = false;
            SetStatus(nextDayText);

            if (startNextCustomerAfterAdvancingDay == true)
                StartNextCustomer();
        }

        private void HandleDishHandedToNpc(DishResult result)
        {
            _dishHandedToCurrentCustomer = true;
            HideActions();
        }

        private void HandleConversationCompleted()
        {
            if (_businessClosed || _dishHandedToCurrentCustomer == false)
                return;

            _dishHandedToCurrentCustomer = false;
            ShowPostCustomerAction();
        }

        private void ShowPostCustomerAction()
        {
            EnsureReferences();
            EnsureControls();
            SetActive(actionRoot, true);

            bool canContinue = encounterDirector != null && encounterDirector.CanStartEncounter();
            if (canContinue)
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

            return $"{encounterDirector.EncountersStartedToday}/{encounterDirector.MaxEncountersPerDay} \uC190\uB2D8 \uC811\uB300";
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
        }

        private void Subscribe()
        {
            if (gamePanel != null)
                gamePanel.DishHandedToNpc += HandleDishHandedToNpc;
            if (npcRunner != null)
                npcRunner.ConversationCompleted += HandleConversationCompleted;
        }

        private void Unsubscribe()
        {
            if (gamePanel != null)
                gamePanel.DishHandedToNpc -= HandleDishHandedToNpc;
            if (npcRunner != null)
                npcRunner.ConversationCompleted -= HandleConversationCompleted;
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

        private static void HideCookingTestPanels()
        {
            CookingTestPanel[] panels = Resources.FindObjectsOfTypeAll<CookingTestPanel>();
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null && panels[i].gameObject.scene.IsValid())
                    panels[i].gameObject.SetActive(false);
            }
        }
    }
}
