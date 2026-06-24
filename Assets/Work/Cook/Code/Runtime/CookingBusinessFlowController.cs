using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.NPC.Code.Runtime;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingBusinessFlowController : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private NpcEncounterDirector encounterDirector;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private bool startFirstCustomerOnStart = true;
        [SerializeField] private bool hideCookingTestPanelOnStart = true;
        [SerializeField] private bool advanceDayWhenShopCloses = true;
        [SerializeField] private bool startNextCustomerAfterAdvancingDay = true;
        [SerializeField] private bool autoCreateDefaultControls = true;
        [SerializeField] private RectTransform actionRoot;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Button nextCustomerButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private string nextCustomerText = "\uB2E4\uC74C";
        [SerializeField] private string closeShopText = "\uAC00\uAC8C \uC811\uAE30";
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

            if (fontAsset == null)
                fontAsset = defaultFontAsset;

            EnsureFontAsset();
            ApplyFontAssetToTexts();
        }

        private void Awake()
        {
            EnsureReferences();
            EnsureFontAsset();
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

            bool started = encounterDirector.StartEncounter();
            if (started == false)
            {
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
            if (fontAsset == null && gamePanel != null)
                fontAsset = gamePanel.TemporaryUiFontAsset;
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
            if (autoCreateDefaultControls == false)
                return;

            EnsureFontAsset();

            if (actionRoot != null && statusField != null && nextCustomerButton != null && closeShopButton != null)
            {
                ApplyFontAssetToTexts();
                return;
            }

            RectTransform root = EnsureRectTransform(gameObject);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            if (actionRoot == null)
            {
                GameObject panelObject = new GameObject("BusinessActionPanel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                actionRoot = panelObject.GetComponent<RectTransform>();
                actionRoot.SetParent(transform, false);
                actionRoot.anchorMin = new Vector2(0.5f, 0f);
                actionRoot.anchorMax = new Vector2(0.5f, 0f);
                actionRoot.pivot = new Vector2(0.5f, 0f);
                actionRoot.anchoredPosition = new Vector2(0f, 28f);
                actionRoot.sizeDelta = new Vector2(520f, 66f);

                Image image = panelObject.GetComponent<Image>();
                image.color = new Color(0.08f, 0.065f, 0.05f, 0.90f);

                HorizontalLayoutGroup layout = panelObject.GetComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 12, 12);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
            }

            if (statusField == null)
            {
                statusField = CreateText(actionRoot, "Status", waitingText, 17f, TextAlignmentOptions.MidlineLeft);
                AddLayoutElement(statusField.gameObject, 260f, -1f, 1f);
            }

            if (nextCustomerButton == null)
                nextCustomerButton = CreateButton(actionRoot, "NextCustomerButton", nextCustomerText, new Color(0.36f, 0.50f, 0.25f, 1f));

            if (closeShopButton == null)
                closeShopButton = CreateButton(actionRoot, "CloseShopButton", closeShopText, new Color(0.48f, 0.31f, 0.20f, 1f));

            BindButtons();
            ApplyFontAssetToTexts();
        }

        private void EnsureFontAsset()
        {
            if (fontAsset != null)
                return;

            if (gamePanel != null)
                fontAsset = gamePanel.TemporaryUiFontAsset;

#if UNITY_EDITOR
            if (fontAsset == null)
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/MangoDdobak-R(otf) SDF.asset");
#endif
        }

        private void ApplyFontAssetToTexts()
        {
            if (fontAsset == null)
                return;

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
                labels[i].font = fontAsset;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI field = textObject.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null)
                field.font = fontAsset;
            field.fontSize = size;
            field.alignment = alignment;
            field.color = Color.white;
            field.text = text;
            return field;
        }

        private Button CreateButton(Transform parent, string objectName, string label, Color color)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI labelField = CreateText(buttonObject.transform, "Label", label, 18f, TextAlignmentOptions.Center);
            RectTransform labelRect = labelField.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            AddLayoutElement(buttonObject, 116f, -1f, 0f);
            return button;
        }

        private static void AddLayoutElement(GameObject target, float preferredWidth, float preferredHeight, float flexibleWidth)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
                element = target.AddComponent<LayoutElement>();

            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
        }

        private static RectTransform EnsureRectTransform(GameObject target)
        {
            RectTransform rect = target.transform as RectTransform;
            return rect != null ? rect : target.AddComponent<RectTransform>();
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
