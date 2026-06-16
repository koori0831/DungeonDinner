using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcDebugPopupPanel : MonoBehaviour
    {
        [SerializeField] private NpcEncounterDirector director;
        [SerializeField] private NpcConversationRunner runner;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private bool createGeneratedUi = true;
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private InputAction toggleAction = new InputAction(
            "ToggleNpcDebugPopup",
            InputActionType.Button,
            "<Keyboard>/f9");
        [SerializeField] private List<string> quickRegionIds = new List<string> { "MossCave", "Volcano" };
        [SerializeField] private Vector2 panelSize = new Vector2(560f, 760f);

        private RectTransform _panelRoot;
        private Button _toggleButton;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _relationshipText;
        private TextMeshProUGUI _affinityChangeText;
        private TextMeshProUGUI _requestUnlockText;
        private TextMeshProUGUI _requestStateText;
        private TextMeshProUGUI _requestFlowText;
        private TextMeshProUGUI _eventPreviewText;
        private TextMeshProUGUI _orderRequirementText;
        private TextMeshProUGUI _dishPreviewText;
        private TextMeshProUGUI _validationText;
        private TextMeshProUGUI _historyText;
        private TMP_InputField _regionInput;
        private TMP_InputField _yearInput;
        private TMP_InputField _monthInput;
        private TMP_InputField _dateDayInput;
        private TMP_InputField _forceEventInput;
        private TMP_InputField _dishRecipeInput;
        private TMP_InputField _dishFoodTypeInput;
        private TMP_InputField _dishTagsInput;
        private readonly Dictionary<string, Button> _questionButtons = new Dictionary<string, Button>();
        private bool _visible;

        private void Awake()
        {
            ResolveReferences();
            ResolveFont();

            if (createGeneratedUi && _panelRoot == null)
                BuildGeneratedUi();

            SetVisible(visibleOnStart);
            RefreshPanel();
        }

        private void Start()
        {
            RefreshPanel();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnableToggleAction();
            SetRunnerEventSubscriptions(true);
            SetDirectorEventSubscriptions(true);
        }

        private void OnDisable()
        {
            DisableToggleAction();
            SetRunnerEventSubscriptions(false);
            SetDirectorEventSubscriptions(false);
        }

        private void SetRunnerEventSubscriptions(bool subscribe)
        {
            if (runner == null)
                return;

            if (subscribe)
            {
                runner.QuestionOptionsUpdated += HandleQuestionOptionsUpdated;
                runner.CookingStepReady += HandleConversationStateChanged;
                runner.ConversationCompleted += HandleConversationStateChanged;
                runner.ResultDialogueStarted += HandleResultDialogueStarted;
                return;
            }

            runner.QuestionOptionsUpdated -= HandleQuestionOptionsUpdated;
            runner.CookingStepReady -= HandleConversationStateChanged;
            runner.ConversationCompleted -= HandleConversationStateChanged;
            runner.ResultDialogueStarted -= HandleResultDialogueStarted;
        }

        private void SetDirectorEventSubscriptions(bool subscribe)
        {
            if (director == null)
                return;

            if (subscribe)
            {
                director.AffinityChanged += HandleAffinityChanged;
                director.RequestUnlocked += HandleRequestUnlocked;
                return;
            }

            director.AffinityChanged -= HandleAffinityChanged;
            director.RequestUnlocked -= HandleRequestUnlocked;
        }

        public void Toggle()
        {
            SetVisible(_visible == false);
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void SetPopupActive(bool active)
        {
            SetVisible(active);
        }

        private void StartEncounter()
        {
            ApplyRegionAndDayInputs();
            director?.StartEncounter();
            RefreshPanel();
        }

        private void StartEncounterAndAdvanceDay()
        {
            ApplyRegionAndDayInputs();
            director?.StartEncounterAndAdvanceDay();
            RefreshPanel();
        }

        private void AdvanceDay()
        {
            ApplyRegionAndDayInputs(false);
            director?.AdvanceDay();
            RefreshPanel();
        }

        private void ClearHistory()
        {
            director?.ClearEncounterHistory();
            RefreshPanel();
        }

        private void LogHistory()
        {
            director?.LogEncounterHistory();
            RefreshPanel();
        }

        private void ValidateData()
        {
            director?.ValidateNpcData();
            RefreshPanel();
        }

        private void ForceStartEvent()
        {
            ApplyRegionAndDayInputs();

            string eventId = _forceEventInput != null ? _forceEventInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(eventId))
            {
                Debug.LogWarning("Debug force event ID is empty.");
                RefreshPanel();
                return;
            }

            director?.ForceStartEvent(eventId);
            RefreshPanel();
        }

        private void UseCurrentEventId()
        {
            if (_forceEventInput == null || runner == null)
                return;

            string eventId = string.IsNullOrWhiteSpace(runner.CurrentEventId)
                ? string.Empty
                : runner.CurrentEventId;
            _forceEventInput.SetTextWithoutNotify(eventId);
            RefreshPanel();
        }

        private void AdvanceRequestState(NpcRequestState targetState)
        {
            if (director == null)
            {
                Debug.LogWarning("NpcEncounterDirector not found.");
                return;
            }

            if (director.AdvanceCurrentNpcRequestState(targetState) == false)
                Debug.LogWarning($"NPC request state was not advanced. target={targetState}");

            RefreshPanel();
        }

        private void SelectQuestion(string categoryId)
        {
            if (IsQuestionAvailableForDebug(categoryId) == false)
            {
                Debug.LogWarning($"Debug question button is not available now: {categoryId}");
                RefreshPanel();
                return;
            }

            runner?.SelectQuestionCategory(categoryId);
            RefreshPanel();
        }

        private void SkipQuestions()
        {
            runner?.SkipQuestions();
            RefreshPanel();
        }

        private void PlayResult(NpcConversationResult result)
        {
            runner?.PlayResultDialogue(result);
            RefreshPanel();
        }

        private void SubmitTestDish()
        {
            runner?.SubmitDish(GetDishRecipeId(), GetDishFoodType(), GetDishTagText());
            RefreshPanel();
        }

        private void PreviewTestDish()
        {
            RefreshPanel();
        }

        private void FillMatchingTestDish()
        {
            if (runner == null || runner.TryBuildMatchingTestDish(out NpcDishSubmission dish) == false)
                return;

            SetDishInputs(dish);
            RefreshPanel();
        }

        private void FillDisgustingTestDish()
        {
            if (runner == null || runner.TryBuildDisgustingTestDish(out NpcDishSubmission dish) == false)
                return;

            SetDishInputs(dish);
            RefreshPanel();
        }

        private void ApplyRegionAndDayInputs(bool applyDay = true)
        {
            if (director == null)
                return;

            string region = _regionInput != null ? _regionInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(region) == false)
                director.SetRegion(region);

            if (applyDay == false
                || _yearInput == null
                || _monthInput == null
                || _dateDayInput == null)
            {
                return;
            }

            if (int.TryParse(_yearInput.text, out int year)
                && int.TryParse(_monthInput.text, out int month)
                && int.TryParse(_dateDayInput.text, out int day))
            {
                director.TrySetCurrentImperialDate(year, month, day);
            }
        }

        private void SetQuickRegion(string regionId)
        {
            if (_regionInput != null)
                _regionInput.SetTextWithoutNotify(regionId);

            director?.SetRegion(regionId);
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            if (_statusText != null)
            {
                string region = director != null ? director.RegionId : "None";
                string dateText = director != null ? director.CurrentDateText : "None";
                string eventId = runner != null && string.IsNullOrWhiteSpace(runner.CurrentEventId) == false
                    ? runner.CurrentEventId
                    : "None";
                string npcId = runner != null && string.IsNullOrWhiteSpace(runner.CurrentNpcId) == false
                    ? runner.CurrentNpcId
                    : "None";
                string playing = runner != null && runner.IsPlaying ? "Playing" : "Idle";
                string cooking = runner != null && runner.IsReadyForCooking ? "Ready" : "Waiting";
                int remainingQuestions = runner != null ? runner.RemainingQuestionCount : 0;
                int affinity = runner != null ? runner.CurrentNpcAffinity : 0;
                string avoidState = runner != null && runner.IsQuestionCategoryUnlocked(NpcQuestionCategoryIds.Avoid) ? "Open" : "Locked";

                _statusText.text =
                    $"Date: {dateText}\n" +
                    $"Region: {region}\n" +
                    $"영업: {(director != null ? $"{director.EncountersStartedToday}/{director.MaxEncountersPerDay}" : "0/0")}명\n" +
                    $"NPC: {npcId}   Affinity: {affinity}   Avoid: {avoidState}\n" +
                    $"Event: {eventId}\n" +
                    $"State: {playing}   Cooking: {cooking}   Questions: {remainingQuestions}";
            }

            if (_regionInput != null && director != null)
                _regionInput.SetTextWithoutNotify(director.RegionId);

            if (_yearInput != null && _monthInput != null && _dateDayInput != null && director != null)
            {
                System.DateTime date = director.CurrentDate;
                _yearInput.SetTextWithoutNotify(date.Year.ToString());
                _monthInput.SetTextWithoutNotify(date.Month.ToString());
                _dateDayInput.SetTextWithoutNotify(date.Day.ToString());
            }

            if (_historyText != null)
                _historyText.text = director != null ? director.GetEncounterHistorySummary() : "NpcEncounterDirector not found.";

            if (_relationshipText != null)
                _relationshipText.text = director != null ? director.GetCurrentNpcProgressSummary() : "NpcEncounterDirector not found.";

            if (_affinityChangeText != null)
                _affinityChangeText.text = director != null ? director.GetLastAffinityChangeSummary() : "NpcEncounterDirector not found.";

            if (_requestUnlockText != null)
                _requestUnlockText.text = director != null ? director.GetLastRequestUnlockSummary() : "NpcEncounterDirector not found.";

            if (_requestStateText != null)
                _requestStateText.text = director != null ? director.GetCurrentNpcRequestStateSummary() : "NpcEncounterDirector not found.";

            if (_requestFlowText != null)
                _requestFlowText.text = director != null ? director.GetCurrentNpcRequestFlowSummary() : "NpcEncounterDirector not found.";

            if (_eventPreviewText != null)
                _eventPreviewText.text = director != null ? director.GetEventCandidateDebugSummary() : "NpcEncounterDirector not found.";

            if (_orderRequirementText != null)
                _orderRequirementText.text = runner != null ? runner.GetCurrentOrderRequirementSummary() : "NpcConversationRunner not found.";

            if (_dishPreviewText != null)
            {
                _dishPreviewText.text = runner != null
                    ? runner.PreviewDishResult(GetDishRecipeId(), GetDishFoodType(), GetDishTagText())
                    : "NpcConversationRunner not found.";
            }

            if (_validationText != null)
                _validationText.text = director != null ? director.GetNpcDataValidationSummary() : "NpcEncounterDirector not found.";

            RefreshQuestionButtonStates();
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;

            if (_panelRoot != null)
                _panelRoot.gameObject.SetActive(visible);

            if (_toggleButton != null)
                SetButtonLabel(_toggleButton, visible ? "닫기" : "NPC Debug");

            if (visible)
                RefreshPanel();
        }

        private void ResolveReferences()
        {
            if (director == null)
                director = FindFirstObjectByType<NpcEncounterDirector>();

            if (runner == null)
                runner = FindFirstObjectByType<NpcConversationRunner>();
        }

        private void ResolveFont()
        {
#if UNITY_EDITOR
            if (fontAsset == null)
                fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/MangoDdobak-B(otf) SDF.asset");
#endif
        }

        private void EnableToggleAction()
        {
            EnsureToggleAction();
            toggleAction.performed += HandleToggleActionPerformed;
            toggleAction.Enable();
        }

        private void DisableToggleAction()
        {
            if (toggleAction == null)
                return;

            toggleAction.performed -= HandleToggleActionPerformed;
            toggleAction.Disable();
        }

        private void EnsureToggleAction()
        {
            if (toggleAction == null)
            {
                toggleAction = new InputAction(
                    "ToggleNpcDebugPopup",
                    InputActionType.Button,
                    "<Keyboard>/f9");
                return;
            }

            if (toggleAction.bindings.Count == 0)
                toggleAction.AddBinding("<Keyboard>/f9");
        }

        private void HandleToggleActionPerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }

        private void HandleQuestionOptionsUpdated(IReadOnlyList<QuestionCategoryData> options)
        {
            RefreshPanel();
        }

        private void HandleConversationStateChanged()
        {
            RefreshPanel();
        }

        private void HandleResultDialogueStarted(string eventId, NpcConversationResult result)
        {
            RefreshPanel();
        }

        private void HandleAffinityChanged(NpcAffinityChangeContext context)
        {
            RefreshPanel();
        }

        private void HandleRequestUnlocked(NpcRequestUnlockContext context)
        {
            RefreshPanel();
        }

        private bool IsQuestionAvailableForDebug(string categoryId)
        {
            if (runner == null || runner.IsPlaying)
                return false;

            IReadOnlyList<QuestionCategoryData> options = runner.GetCurrentQuestionOptions();
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].CategoryId == categoryId)
                    return true;
            }

            return false;
        }

        private void RefreshQuestionButtonStates()
        {
            foreach (KeyValuePair<string, Button> pair in _questionButtons)
            {
                if (pair.Value == null)
                    continue;

                pair.Value.interactable = IsQuestionAvailableForDebug(pair.Key);
            }
        }

        private void BuildGeneratedUi()
        {
            _questionButtons.Clear();
            Canvas canvas = CreateCanvas();

            _toggleButton = CreateButton(canvas.transform, "NPC Debug", Toggle, new Vector2(132f, 38f));
            RectTransform toggleRect = _toggleButton.transform as RectTransform;
            toggleRect.anchorMin = new Vector2(1f, 1f);
            toggleRect.anchorMax = new Vector2(1f, 1f);
            toggleRect.pivot = new Vector2(1f, 1f);
            toggleRect.anchoredPosition = new Vector2(-18f, -18f);

            _panelRoot = CreatePanel(canvas.transform);

            RectTransform headerRow = CreateRow(_panelRoot, "Header", 40f);
            MakeDragHandle(headerRow, _panelRoot, canvas);

            TextMeshProUGUI title = CreateText(headerRow, "Title", "NPC Debug Panel", 22f, TextAlignmentOptions.Left);
            title.GetComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton(headerRow, "X", Hide, new Vector2(44f, 34f));

            RectTransform contentRoot = CreateScrollContent(_panelRoot);

            _statusText = CreateText(contentRoot, "Status", string.Empty, 16f, TextAlignmentOptions.Left);
            _statusText.textWrappingMode = TextWrappingModes.Normal;
            _statusText.overflowMode = TextOverflowModes.Truncate;
            LayoutElement statusLayout = _statusText.GetComponent<LayoutElement>();
            statusLayout.minHeight = 122f;
            statusLayout.preferredHeight = 122f;
            statusLayout.flexibleWidth = 1f;

            CreateSectionLabel(contentRoot, "지역 / 날짜");

            RectTransform regionRow = CreateRow(contentRoot, "RegionRow", 42f);
            _regionInput = CreateInputField(regionRow, "RegionInput", "RegionId");
            _regionInput.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _regionInput.onEndEdit.AddListener(regionId => SetQuickRegion(regionId));

            for (int i = 0; i < quickRegionIds.Count; i++)
            {
                string regionId = quickRegionIds[i];
                CreateButton(regionRow, regionId, () => SetQuickRegion(regionId), new Vector2(88f, 34f));
            }

            RectTransform dayRow = CreateRow(contentRoot, "DayRow", 42f);
            CreateText(dayRow, "DateLabel", NpcImperialCalendar.EraName, 18f, TextAlignmentOptions.Left, new Vector2(64f, 34f));
            _yearInput = CreateInputField(dayRow, "YearInput", "975");
            LayoutElement yearLayout = _yearInput.GetComponent<LayoutElement>();
            yearLayout.preferredWidth = 78f;
            yearLayout.flexibleWidth = 0f;
            _monthInput = CreateInputField(dayRow, "MonthInput", "7");
            LayoutElement monthLayout = _monthInput.GetComponent<LayoutElement>();
            monthLayout.preferredWidth = 54f;
            monthLayout.flexibleWidth = 0f;
            _dateDayInput = CreateInputField(dayRow, "DateDayInput", "14");
            LayoutElement dateDayLayout = _dateDayInput.GetComponent<LayoutElement>();
            dateDayLayout.preferredWidth = 54f;
            dateDayLayout.flexibleWidth = 0f;
            CreateButton(dayRow, "날짜 적용", () =>
            {
                ApplyRegionAndDayInputs();
                RefreshPanel();
            }, new Vector2(96f, 34f));
            CreateButton(dayRow, "다음 날", AdvanceDay, new Vector2(92f, 34f));

            CreateSectionLabel(contentRoot, "대화 시작");

            RectTransform encounterRow = CreateRow(contentRoot, "EncounterRow", 42f);
            CreateButton(encounterRow, "Start Encounter", StartEncounter);
            CreateButton(encounterRow, "Start + Day", StartEncounterAndAdvanceDay);

            RectTransform forceEventRow = CreateRow(contentRoot, "ForceEventRow", 42f);
            _forceEventInput = CreateInputField(forceEventRow, "ForceEventInput", "EventId");
            _forceEventInput.GetComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton(forceEventRow, "Force", ForceStartEvent, new Vector2(82f, 34f));
            CreateButton(forceEventRow, "Use Current", UseCurrentEventId, new Vector2(112f, 34f));

            RectTransform utilityRow = CreateRow(contentRoot, "UtilityRow", 42f);
            CreateButton(utilityRow, "질문 종료", SkipQuestions);
            CreateButton(utilityRow, "Validate Data", ValidateData);

            CreateSectionLabel(contentRoot, "Event Preview");
            _eventPreviewText = CreateInfoBox(contentRoot, "EventPreviewBox", 170f, 12f);

            CreateSectionLabel(contentRoot, "관계 진행");
            _relationshipText = CreateInfoBox(contentRoot, "RelationshipBox", 112f, 14f);

            CreateSectionLabel(contentRoot, "최근 관계 변화");
            _affinityChangeText = CreateInfoBox(contentRoot, "AffinityChangeBox", 58f, 13f);

            CreateSectionLabel(contentRoot, "Recent Request Unlock");
            _requestUnlockText = CreateInfoBox(contentRoot, "RequestUnlockBox", 58f, 13f);

            CreateSectionLabel(contentRoot, "Request State");
            _requestStateText = CreateInfoBox(contentRoot, "RequestStateBox", 92f, 12f);

            CreateSectionLabel(contentRoot, "Request Flow");
            _requestFlowText = CreateInfoBox(contentRoot, "RequestFlowBox", 126f, 12f);

            RectTransform requestRowA = CreateRow(contentRoot, "RequestStateRowA", 42f);
            CreateButton(requestRowA, "Accept", () => AdvanceRequestState(NpcRequestState.Accepted));
            CreateButton(requestRowA, "Progress", () => AdvanceRequestState(NpcRequestState.InProgress));
            CreateButton(requestRowA, "Ready", () => AdvanceRequestState(NpcRequestState.ReadyToComplete));

            RectTransform requestRowB = CreateRow(contentRoot, "RequestStateRowB", 42f);
            CreateButton(requestRowB, "Complete", () => AdvanceRequestState(NpcRequestState.Completed));
            CreateButton(requestRowB, "Epilogue", () => AdvanceRequestState(NpcRequestState.EpilogueAvailable));
            CreateButton(requestRowB, "Done", () => AdvanceRequestState(NpcRequestState.EpilogueCompleted));

            CreateSectionLabel(contentRoot, "주문 조건");
            _orderRequirementText = CreateInfoBox(contentRoot, "OrderRequirementBox", 120f, 14f);

            CreateSectionLabel(contentRoot, "질문");

            RectTransform questionRowA = CreateRow(contentRoot, "QuestionRowA", 42f);
            _questionButtons[NpcQuestionCategoryIds.Taste] = CreateButton(questionRowA, "맛", () => SelectQuestion(NpcQuestionCategoryIds.Taste));
            _questionButtons[NpcQuestionCategoryIds.TextureTemp] = CreateButton(questionRowA, "온도/식감", () => SelectQuestion(NpcQuestionCategoryIds.TextureTemp));

            RectTransform questionRowB = CreateRow(contentRoot, "QuestionRowB", 42f);
            _questionButtons[NpcQuestionCategoryIds.Condition] = CreateButton(questionRowB, "몸 상태", () => SelectQuestion(NpcQuestionCategoryIds.Condition));
            _questionButtons[NpcQuestionCategoryIds.Avoid] = CreateButton(questionRowB, "피하고 싶은 음식", () => SelectQuestion(NpcQuestionCategoryIds.Avoid));

            CreateSectionLabel(contentRoot, "테스트 음식");

            RectTransform dishRowA = CreateRow(contentRoot, "DishRowA", 42f);
            _dishRecipeInput = CreateInputField(dishRowA, "DishRecipeInput", "RecipeId");
            _dishRecipeInput.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _dishFoodTypeInput = CreateInputField(dishRowA, "DishFoodTypeInput", "FoodType");
            _dishFoodTypeInput.GetComponent<LayoutElement>().flexibleWidth = 1f;

            RectTransform dishRowB = CreateRow(contentRoot, "DishRowB", 42f);
            _dishTagsInput = CreateInputField(dishRowB, "DishTagsInput", "Tags: Hot|Spicy");
            _dishTagsInput.GetComponent<LayoutElement>().flexibleWidth = 1f;

            RectTransform dishButtonRow = CreateRow(contentRoot, "DishButtonRow", 42f);
            CreateButton(dishButtonRow, "맞는 샘플", FillMatchingTestDish);
            CreateButton(dishButtonRow, "괴식 샘플", FillDisgustingTestDish);
            CreateButton(dishButtonRow, "미리보기", PreviewTestDish);
            CreateButton(dishButtonRow, "음식 제출", SubmitTestDish);

            _dishPreviewText = CreateInfoBox(contentRoot, "DishPreviewBox", 48f, 14f);

            CreateSectionLabel(contentRoot, "결과");

            RectTransform resultRowA = CreateRow(contentRoot, "ResultRowA", 42f);
            CreateButton(resultRowA, "Correct", () => PlayResult(NpcConversationResult.Correct));
            CreateButton(resultRowA, "Similar", () => PlayResult(NpcConversationResult.Similar));

            RectTransform resultRowB = CreateRow(contentRoot, "ResultRowB", 42f);
            CreateButton(resultRowB, "Wrong", () => PlayResult(NpcConversationResult.Wrong));
            CreateButton(resultRowB, "Disgusting", () => PlayResult(NpcConversationResult.Disgusting));

            RectTransform resultRowC = CreateRow(contentRoot, "ResultRowC", 42f);
            CreateButton(resultRowC, "Perfect", () => PlayResult(NpcConversationResult.Perfect));

            CreateSectionLabel(contentRoot, "Data Validation");
            _validationText = CreateInfoBox(contentRoot, "ValidationBox", 156f, 12f);

            CreateSectionLabel(contentRoot, "히스토리");

            RectTransform historyButtonRow = CreateRow(contentRoot, "HistoryButtonRow", 42f);
            CreateButton(historyButtonRow, "새로고침", RefreshPanel);
            CreateButton(historyButtonRow, "콘솔 출력", LogHistory);
            CreateButton(historyButtonRow, "초기화", ClearHistory);

            _historyText = CreateHistoryBox(contentRoot);
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("NpcDebugPopupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private RectTransform CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("NpcDebugPopupPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -62f);
            rect.sizeDelta = panelSize;

            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private RectTransform CreateScrollContent(Transform parent)
        {
            GameObject scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);

            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = true;

            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleWidth = 1f;
            scrollLayout.flexibleHeight = 1f;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            RectTransform viewportRect = viewportObject.transform as RectTransform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-12f, 0f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            Scrollbar verticalScrollbar = CreateVerticalScrollbar(scrollObject.transform);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect = contentObject.transform as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 8, 0, 8);
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return contentRect;
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(parent, false);

            RectTransform scrollbarRect = scrollbarObject.transform as RectTransform;
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(8f, 0f);

            Image railImage = scrollbarObject.GetComponent<Image>();
            railImage.color = new Color(0.02f, 0.025f, 0.035f, 0.8f);

            GameObject slidingAreaObject = new GameObject("SlidingArea", typeof(RectTransform));
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);

            RectTransform slidingAreaRect = slidingAreaObject.transform as RectTransform;
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(1f, 2f);
            slidingAreaRect.offsetMax = new Vector2(-1f, -2f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(slidingAreaObject.transform, false);

            RectTransform handleRect = handleObject.transform as RectTransform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.44f, 0.56f, 0.72f, 0.95f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            ColorBlock colors = scrollbar.colors;
            colors.normalColor = handleImage.color;
            colors.highlightedColor = new Color(0.55f, 0.68f, 0.86f, 1f);
            colors.pressedColor = new Color(0.32f, 0.42f, 0.56f, 1f);
            colors.selectedColor = colors.highlightedColor;
            scrollbar.colors = colors;

            return scrollbar;
        }

        private RectTransform CreateRow(Transform parent, string name, float height)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            layoutElement.flexibleWidth = 1f;

            return rowObject.transform as RectTransform;
        }

        private void MakeDragHandle(RectTransform handle, RectTransform target, Canvas canvas)
        {
            Image image = handle.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            NpcDebugDragHandle dragHandle = handle.gameObject.AddComponent<NpcDebugDragHandle>();
            dragHandle.Initialize(target, canvas);
        }

        private void CreateSectionLabel(Transform parent, string text)
        {
            TextMeshProUGUI label = CreateText(parent, $"Section_{text}", text, 16f, TextAlignmentOptions.Left);
            label.color = new Color(0.78f, 0.86f, 1f, 1f);
            LayoutElement layoutElement = label.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 22f;
            layoutElement.minHeight = 22f;
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Vector2? size = null)
        {
            GameObject buttonObject = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.19f, 0.24f, 0.31f, 0.98f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.26f, 0.33f, 0.43f, 1f);
            colors.pressedColor = new Color(0.12f, 0.16f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = size?.y ?? 34f;
            layoutElement.preferredHeight = size?.y ?? 34f;
            layoutElement.preferredWidth = size?.x ?? 0f;
            layoutElement.flexibleWidth = size.HasValue ? 0f : 1f;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 16f, TextAlignmentOptions.Center);
            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            Destroy(text.GetComponent<LayoutElement>());

            return button;
        }

        private TMP_InputField CreateInputField(Transform parent, string name, string placeholder)
        {
            GameObject inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            inputObject.transform.SetParent(parent, false);

            Image image = inputObject.GetComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.08f, 0.96f);

            LayoutElement layoutElement = inputObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 34f;
            layoutElement.preferredHeight = 34f;
            layoutElement.flexibleWidth = 1f;

            TMP_InputField inputField = inputObject.GetComponent<TMP_InputField>();

            TextMeshProUGUI text = CreateText(inputObject.transform, "Text", string.Empty, 16f, TextAlignmentOptions.Left);
            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 5f);
            textRect.offsetMax = new Vector2(-10f, -5f);
            Destroy(text.GetComponent<LayoutElement>());

            TextMeshProUGUI placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, 16f, TextAlignmentOptions.Left);
            placeholderText.color = new Color(1f, 1f, 1f, 0.36f);
            RectTransform placeholderRect = placeholderText.transform as RectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10f, 5f);
            placeholderRect.offsetMax = new Vector2(-10f, -5f);
            Destroy(placeholderText.GetComponent<LayoutElement>());

            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.targetGraphic = image;

            return inputField;
        }

        private TextMeshProUGUI CreateHistoryBox(Transform parent)
        {
            return CreateTextBox(parent, "HistoryBox", "HistoryText", 128f, 14f);
        }

        private TextMeshProUGUI CreateInfoBox(Transform parent, string name, float height, float fontSize)
        {
            return CreateTextBox(parent, name, "Text", height, fontSize);
        }

        private TextMeshProUGUI CreateTextBox(
            Transform parent,
            string boxName,
            string textName,
            float height,
            float fontSize)
        {
            GameObject boxObject = new GameObject(boxName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            boxObject.transform.SetParent(parent, false);

            Image image = boxObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);

            LayoutElement layoutElement = boxObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1f;

            TextMeshProUGUI text = CreateText(boxObject.transform, textName, string.Empty, fontSize, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);
            Destroy(text.GetComponent<LayoutElement>());

            return text;
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2? size = null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            if (fontAsset != null)
                label.font = fontAsset;

            LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = size?.y ?? 24f;
            layoutElement.preferredHeight = size?.y ?? 24f;
            layoutElement.preferredWidth = size?.x ?? 0f;
            layoutElement.flexibleWidth = size.HasValue ? 0f : 1f;

            return label;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label;
        }

        private void SetDishInputs(NpcDishSubmission dish)
        {
            if (_dishRecipeInput != null)
                _dishRecipeInput.SetTextWithoutNotify(dish.RecipeId);

            if (_dishFoodTypeInput != null)
                _dishFoodTypeInput.SetTextWithoutNotify(dish.FoodType);

            if (_dishTagsInput != null)
                _dishTagsInput.SetTextWithoutNotify(string.Join("|", dish.Tags));
        }

        private string GetDishRecipeId()
        {
            return _dishRecipeInput != null ? _dishRecipeInput.text.Trim() : string.Empty;
        }

        private string GetDishFoodType()
        {
            return _dishFoodTypeInput != null ? _dishFoodTypeInput.text.Trim() : string.Empty;
        }

        private string GetDishTagText()
        {
            return _dishTagsInput != null ? _dishTagsInput.text.Trim() : string.Empty;
        }
    }

    public sealed class NpcDebugDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _target;
        private Canvas _canvas;
        private Vector2 _dragOffset;

        public void Initialize(RectTransform target, Canvas canvas)
        {
            _target = target;
            _canvas = canvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_target == null)
                return;

            if (TryGetParentPoint(eventData, out Vector2 parentPoint))
                _dragOffset = _target.anchoredPosition - parentPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_target == null)
                return;

            if (TryGetParentPoint(eventData, out Vector2 parentPoint))
                _target.anchoredPosition = parentPoint + _dragOffset;
        }

        private bool TryGetParentPoint(PointerEventData eventData, out Vector2 parentPoint)
        {
            parentPoint = Vector2.zero;

            RectTransform parent = _target.parent as RectTransform;
            if (parent == null)
                return false;

            Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventCamera,
                out parentPoint);
        }
    }
}
