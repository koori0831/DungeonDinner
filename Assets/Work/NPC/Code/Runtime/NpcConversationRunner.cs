using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Work.Chat.Code;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    [Serializable]
    public sealed class QuestionOptionsChangedEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public sealed class NpcOrderReadySummaryEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public sealed class NpcDishEvaluatedSummaryEvent : UnityEvent<string>
    {
    }

    public sealed class NpcConversationRunner : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string resourceFolder = "NPCData";
        [SerializeField] private string startEventId = "Yorn_Revisit_MagmaCubeMemory";

        [Header("Output")]
        [SerializeField] private ChatPanel chatPanel;
        [SerializeField] private NpcOrderSlipPanel orderSlipPanel;
        [SerializeField] private Sprite orderSlipPanelSprite;
        [SerializeField] private Sprite orderSlipLabelSprite;
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool showSpeakerName;
        [SerializeField] private bool useDirectChatPanelOutput = true;
        [SerializeField] private bool writeNpcBoldTextToOrderSlip = true;
        [SerializeField] private bool autoCreateOrderSlipPanel = true;
        [SerializeField] private string playerDisplayName = "플레이어";
        [SerializeField, Min(0f)] private float lineDelay = 0.15f;

        [Header("Affinity Question Rules")]
        [SerializeField] private bool useAffinityQuestionRules = true;
        [SerializeField, Min(0)] private int affinityForExtraQuestion = 2;
        [SerializeField, Min(0)] private int extraQuestionCount = 1;
        [SerializeField, Min(0)] private int affinityForAvoidQuestion = 2;
        [SerializeField] private string avoidQuestionCategoryId = NpcQuestionCategoryIds.Avoid;

        [Header("Events")]
        [SerializeField] private QuestionOptionsChangedEvent questionOptionsChanged = new QuestionOptionsChangedEvent();
        [SerializeField] private UnityEvent conversationStarted = new UnityEvent();
        [SerializeField] private UnityEvent readyForCooking = new UnityEvent();
        [SerializeField] private UnityEvent conversationCompleted = new UnityEvent();
        [SerializeField] private NpcOrderReadySummaryEvent orderReady = new NpcOrderReadySummaryEvent();
        [SerializeField] private NpcDishEvaluatedSummaryEvent dishEvaluated = new NpcDishEvaluatedSummaryEvent();

        private NpcConversationDatabase _database;
        private VisitEventData _currentEvent;
        private readonly HashSet<string> _usedQuestionCategories = new HashSet<string>();
        private Coroutine _playRoutine;
        private int _remainingQuestionCount;
        private int _currentNpcAffinity;
        private bool _resultDialoguePlayed;
        private bool _conversationCompleted;
        private bool _cookingStepNotified;

        public event Action<IReadOnlyList<QuestionCategoryData>> QuestionOptionsUpdated;
        public event Action<NpcOrderContext> OrderReady;
        public event Action<NpcDishResultContext> DishEvaluated;
        public event Action<NpcDialogueLineContext> DialogueLinePlayed;
        /// <summary>
        /// NPC 응대 대화가 시작될 때 발생하는 이벤트
        /// </summary>
        public event Action ConversationStarted;
        public event Action CookingStepReady;
        public event Action ConversationCompleted;
        public event Action<string, NpcConversationResult> ResultDialogueStarted;

        public bool IsPlaying => _playRoutine != null;
        public bool HasActiveConversation => _currentEvent != null && _conversationCompleted == false;
        public int RemainingQuestionCount => _remainingQuestionCount;
        public string CurrentEventId => _currentEvent?.EventId;
        public string CurrentNpcId => _currentEvent?.NpcId;
        public int CurrentNpcAffinity => _currentEvent != null ? _currentNpcAffinity : 0;
        public bool DirectChatPanelOutputEnabled => useDirectChatPanelOutput;
        public bool IsReadyForCooking => _currentEvent != null
                                         && NpcVisitEventRules.RequiresCookingStep(_currentEvent)
                                         && _cookingStepNotified
                                         && _resultDialoguePlayed == false
                                         && _conversationCompleted == false;

        private void Awake()
        {
            EnsureDatabase();
        }

        private void Start()
        {
            if (playOnStart)
                PlayStartEvent();
        }

        public void PlayStartEvent()
        {
            PlayEvent(startEventId);
        }

        public void PlayEvent(string eventId)
        {
            PlayEvent(eventId, 0);
        }

        public void PlayEvent(string eventId, int npcAffinity)
        {
            EnsureDatabase();
            StopPlayback();

            if (_database.TryGetVisitEvent(eventId, out VisitEventData visitEvent) == false)
            {
                Debug.LogError($"Visit event not found: {eventId}");
                return;
            }

            _currentEvent = visitEvent;
            _usedQuestionCategories.Clear();
            _currentNpcAffinity = Mathf.Max(0, npcAffinity);
            _remainingQuestionCount = GetQuestionLimit(visitEvent);
            _resultDialoguePlayed = false;
            _conversationCompleted = false;
            _cookingStepNotified = false;
            ResetOrderSlipPanel(visitEvent);
            ClearQuestionOptions();
            NotifyConversationStarted();
            _playRoutine = StartCoroutine(PlayStartGroupsRoutine());
        }

        public void SetDirectChatPanelOutput(bool enabled)
        {
            useDirectChatPanelOutput = enabled;
        }

        public void SelectQuestionCategory(string categoryId)
        {
            if (_currentEvent == null)
            {
                Debug.LogWarning("No active visit event.");
                return;
            }

            if (IsPlaying)
            {
                Debug.LogWarning("Dialogue is still playing.");
                return;
            }

            if (_remainingQuestionCount <= 0)
            {
                NotifyReadyForCooking();
                return;
            }

            if (_usedQuestionCategories.Contains(categoryId))
            {
                Debug.LogWarning($"Question category already used: {categoryId}");
                return;
            }

            if (_database.TryGetQuestionCategory(categoryId, out QuestionCategoryData category) == false)
            {
                Debug.LogWarning($"Question category not found: {categoryId}");
                return;
            }

            bool isAvailable = _currentEvent.AvailableQuestionCategories.Contains(categoryId);
            if (isAvailable == false)
            {
                Debug.LogWarning($"Question category is not available for this event: {categoryId}");
                return;
            }

            if (IsQuestionCategoryUnlocked(categoryId) == false)
            {
                Debug.LogWarning(
                    $"Question category is locked by NPC affinity. category={categoryId}, " +
                    $"npc={_currentEvent.NpcId}, affinity={_currentNpcAffinity}");
                return;
            }

            if (_database.HasDialogueGroup(_currentEvent.EventId, category.DialogueGroup) == false)
            {
                Debug.LogWarning($"Question dialogue group not found: {category.DialogueGroup}");
                return;
            }

            ClearQuestionOptions();
            _usedQuestionCategories.Add(categoryId);
            _remainingQuestionCount--;
            _playRoutine = StartCoroutine(PlayQuestionGroupRoutine(category.DialogueGroup));
        }

        public void SkipQuestions()
        {
            if (IsPlaying)
            {
                Debug.LogWarning("Dialogue is still playing.");
                return;
            }

            _remainingQuestionCount = 0;
            NotifyReadyForCooking();
        }

        public void PlayResultDialogue(NpcConversationResult result)
        {
            result = NormalizeResult(result);
            if (_currentEvent == null)
            {
                Debug.LogWarning("No active visit event.");
                return;
            }

            if (IsPlaying)
            {
                Debug.LogWarning("Dialogue is still playing.");
                return;
            }

            if (_resultDialoguePlayed)
            {
                Debug.LogWarning("Result dialogue already played for the current NPC event.");
                return;
            }

            string group = GetResultGroup(result);
            if (_database.HasDialogueGroup(_currentEvent.EventId, group) == false && result == NpcConversationResult.Perfect)
                group = GetResultGroup(NpcConversationResult.Correct);

            if (_database.HasDialogueGroup(_currentEvent.EventId, group) == false)
            {
                Debug.LogWarning($"Result dialogue group not found: {group}");
                NotifyConversationCompleted();
                return;
            }

            ClearQuestionOptions();
            _remainingQuestionCount = 0;
            _resultDialoguePlayed = true;
            ResultDialogueStarted?.Invoke(_currentEvent.EventId, result);
            _playRoutine = StartCoroutine(PlayResultGroupRoutine(group));
        }

        public void PlayResultDialogue(string resultName)
        {
            if (Enum.TryParse(resultName, true, out NpcConversationResult result) == false)
            {
                Debug.LogWarning($"Unknown NPC conversation result: {resultName}");
                return;
            }

            PlayResultDialogue(result);
        }

        public void SubmitDish(string recipeId, string foodType, string tagText)
        {
            SubmitDish(NpcDishSubmission.FromText(recipeId, foodType, tagText));
        }

        public void SubmitDish(NpcDishSubmission dish)
        {
            if (_currentEvent == null)
            {
                Debug.LogWarning("No active visit event.");
                return;
            }

            if (IsPlaying)
            {
                Debug.LogWarning("Dialogue is still playing.");
                return;
            }

            if (NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
            {
                Debug.LogWarning("Current NPC event has no cooking order.");
                return;
            }

            if (TryBuildDishResultContext(dish, out NpcDishResultContext resultContext) == false)
                return;

            DishEvaluated?.Invoke(resultContext);
            dishEvaluated.Invoke(resultContext.BuildDebugSummary());

            Debug.Log(
                $"NPC dish evaluated: event={_currentEvent.EventId}, result={resultContext.Result}, " +
                $"dish=({dish.BuildDebugSummary()}), reason={resultContext.Reason}");

            PlayResultDialogue(resultContext.Result);
        }

        public string PreviewDishResult(string recipeId, string foodType, string tagText)
        {
            if (TryBuildDishMatchReport(recipeId, foodType, tagText, out NpcDishMatchReport report) == false)
                return "No active NPC order.";

            return report.BuildDebugSummary();
        }

        public string GetCurrentOrderRequirementSummary()
        {
            return NpcDishResultEvaluator.BuildRequirementSummary(_currentEvent);
        }

        public bool TryGetCurrentOrderContext(out NpcOrderContext orderContext)
        {
            orderContext = null;
            if (_currentEvent == null || NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
                return false;

            orderContext = BuildCurrentOrderContext();
            return true;
        }

        public bool TryBuildDishMatchReport(
            string recipeId,
            string foodType,
            string tagText,
            out NpcDishMatchReport report)
        {
            return TryBuildDishMatchReport(NpcDishSubmission.FromText(recipeId, foodType, tagText), out report);
        }

        public bool TryBuildDishMatchReport(NpcDishSubmission dish, out NpcDishMatchReport report)
        {
            report = null;
            if (_currentEvent == null
                || dish == null
                || NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
            {
                return false;
            }

            report = NpcDishResultEvaluator.BuildMatchReport(BuildCurrentOrderContext(), dish);
            return true;
        }

        public bool TryBuildDishResultContext(NpcDishSubmission dish, out NpcDishResultContext resultContext)
        {
            resultContext = null;
            if (_currentEvent == null
                || dish == null
                || NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
            {
                return false;
            }

            NpcDishEvaluation evaluation = NpcDishResultEvaluator.Evaluate(_currentEvent, dish);
            resultContext = new NpcDishResultContext(
                BuildCurrentOrderContext(),
                dish,
                evaluation);
            return true;
        }

        public bool TryBuildMatchingTestDish(out NpcDishSubmission dish)
        {
            dish = null;
            if (_currentEvent == null || NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
                return false;

            dish = NpcDishResultEvaluator.BuildMatchingDish(_currentEvent);
            return true;
        }

        public bool TryBuildDisgustingTestDish(out NpcDishSubmission dish)
        {
            dish = null;
            if (_currentEvent == null || NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
                return false;

            dish = NpcDishResultEvaluator.BuildDisgustingDish(_currentEvent);
            return true;
        }

        public void SelectTasteQuestion()
        {
            SelectQuestionCategory(NpcQuestionCategoryIds.Taste);
        }

        public void SelectTextureTempQuestion()
        {
            SelectQuestionCategory(NpcQuestionCategoryIds.TextureTemp);
        }

        public void SelectConditionQuestion()
        {
            SelectQuestionCategory(NpcQuestionCategoryIds.Condition);
        }

        public void SelectAvoidQuestion()
        {
            SelectQuestionCategory(NpcQuestionCategoryIds.Avoid);
        }

        public void TestCorrectResult()
        {
            PlayResultDialogue(NpcConversationResult.Correct);
        }

        public void TestSimilarResult()
        {
            PlayResultDialogue(NpcConversationResult.Similar);
        }

        public void TestWrongResult()
        {
            PlayResultDialogue(NpcConversationResult.Wrong);
        }

        public void TestDisgustingResult()
        {
            PlayResultDialogue(NpcConversationResult.Wrong);
        }

        public IReadOnlyList<QuestionCategoryData> GetCurrentQuestionOptions()
        {
            if (_currentEvent == null || _remainingQuestionCount <= 0)
                return new List<QuestionCategoryData>();

            return _database
                .GetAvailableQuestionCategories(_currentEvent, _usedQuestionCategories)
                .Where(option => IsQuestionCategoryUnlocked(option.CategoryId))
                .ToList();
        }

        public bool IsQuestionCategoryUnlocked(string categoryId)
        {
            if (useAffinityQuestionRules == false)
                return true;

            if (string.Equals(categoryId, avoidQuestionCategoryId, StringComparison.OrdinalIgnoreCase) == false)
                return true;

            return _currentNpcAffinity >= affinityForAvoidQuestion;
        }

        private int GetQuestionLimit(VisitEventData visitEvent)
        {
            int questionLimit = Mathf.Max(0, visitEvent.QuestionLimit);
            if (useAffinityQuestionRules && _currentNpcAffinity >= affinityForExtraQuestion)
                questionLimit += Mathf.Max(0, extraQuestionCount);

            return questionLimit;
        }

        private IEnumerator PlayStartGroupsRoutine()
        {
            foreach (string group in _currentEvent.StartGroups)
            {
                yield return PlayGroupRoutine(group);
            }

            _playRoutine = null;
            NotifyQuestionOptionsOrReady();
        }

        private IEnumerator PlayQuestionGroupRoutine(string group)
        {
            yield return PlayGroupRoutine(group);

            _playRoutine = null;
            NotifyQuestionOptionsOrReady();
        }

        private IEnumerator PlayResultGroupRoutine(string group)
        {
            yield return PlayGroupRoutine(group);

            _playRoutine = null;
            NotifyConversationCompleted();
        }

        private IEnumerator PlayGroupRoutine(string group)
        {
            IReadOnlyList<DialogueLineData> lines = _database.GetDialogueLines(_currentEvent.EventId, group);
            if (lines.Count == 0)
                yield break;

            foreach (DialogueLineData line in lines)
            {
                NpcDialogueLineContext context = AddLine(line);
                yield return new WaitUntil(context.IsPresentationComplete);

                if (lineDelay > 0f)
                    yield return new WaitForSeconds(lineDelay);
            }
        }

        private NpcDialogueLineContext AddLine(DialogueLineData line)
        {
            string speakerName = GetSpeakerName(line.Speaker);
            NpcDialogueMarkupResult markup = NpcDialogueMarkupUtility.Parse(line.Text);
            string text = markup.RichText;
            if (showSpeakerName)
                text = $"{speakerName}: {text}";

            IReadOnlyList<string> orderHighlights = line.IsPlayer || writeNpcBoldTextToOrderSlip == false
                ? Array.Empty<string>()
                : markup.BoldSegments;
            bool hasViewSubscriber = DialogueLinePlayed != null;
            NpcDialogueLineContext context = new NpcDialogueLineContext(
                _currentEvent?.EventId,
                _currentEvent?.NpcId,
                line.Group,
                line.Speaker,
                speakerName,
                line.IsPlayer,
                line.Text,
                text,
                orderHighlights);

            DialogueLinePlayed?.Invoke(context);
            AppendOrderHighlights(context);

            if (useDirectChatPanelOutput && chatPanel != null)
            {
                ChatTextField chat = chatPanel.AddChat(text, line.IsPlayer);
                context.RegisterPresentationWaiter(() => chat == null || chat.IsTyping == false);
                return context;
            }

            if (hasViewSubscriber == false)
                Debug.Log(text);

            return context;
        }

        private string GetSpeakerName(string speaker)
        {
            if (string.Equals(speaker, "Player", StringComparison.OrdinalIgnoreCase))
                return playerDisplayName;

            if (_database.Npcs.TryGetValue(speaker, out NpcData npc))
                return npc.DisplayName;

            return speaker;
        }

        private void ResetOrderSlipPanel(VisitEventData visitEvent)
        {
            if (writeNpcBoldTextToOrderSlip == false)
                return;

            ResolveOrderSlipPanel();
            orderSlipPanel?.ResetForConversation(visitEvent?.EventId, visitEvent?.NpcId);
        }

        private void AppendOrderHighlights(NpcDialogueLineContext context)
        {
            if (writeNpcBoldTextToOrderSlip == false
                || context == null
                || context.IsPlayer
                || context.OrderHighlights.Count == 0)
            {
                return;
            }

            ResolveOrderSlipPanel();
            orderSlipPanel?.AppendOrderClues(context.OrderHighlights);
        }

        private void ResolveOrderSlipPanel()
        {
            if (orderSlipPanel == null && autoCreateOrderSlipPanel == true)
            {
                orderSlipPanel = FindFirstObjectByType<NpcOrderSlipPanel>();
                if (orderSlipPanel == null)
                    orderSlipPanel = NpcOrderSlipPanel.GetOrCreateGeneratedPanel();
            }

            if (orderSlipPanel == null)
            {
                return;
            }

            orderSlipPanel.SetVisualSprites(orderSlipPanelSprite, orderSlipLabelSprite);
        }

        private void NotifyQuestionOptionsOrReady()
        {
            IReadOnlyList<QuestionCategoryData> options = GetCurrentQuestionOptions();
            if (_remainingQuestionCount <= 0 || options.Count == 0)
            {
                NotifyReadyForCooking();
                return;
            }

            string payload = string.Join("|", options.Select(option => $"{option.CategoryId}:{option.DisplayName}"));
            QuestionOptionsUpdated?.Invoke(options);
            questionOptionsChanged.Invoke(payload);
            Debug.Log($"Available NPC questions: {payload}");
        }

        private void NotifyReadyForCooking()
        {
            if (_currentEvent == null || _cookingStepNotified)
                return;

            if (NpcVisitEventRules.RequiresCookingStep(_currentEvent) == false)
            {
                ClearQuestionOptions();
                NotifyConversationCompleted();
                return;
            }

            _cookingStepNotified = true;
            NpcOrderContext orderContext = BuildCurrentOrderContext();

            ClearQuestionOptions();
            OrderReady?.Invoke(orderContext);
            orderReady.Invoke(orderContext.BuildDebugSummary());
            CookingStepReady?.Invoke();
            readyForCooking.Invoke();
            Debug.Log($"NPC conversation is ready for cooking step. {orderContext.BuildDebugSummary()}");
        }

        private void NotifyConversationCompleted()
        {
            _conversationCompleted = true;
            ConversationCompleted?.Invoke();
            conversationCompleted.Invoke();
            Debug.Log("NPC conversation completed.");
        }

        private void NotifyConversationStarted()
        {
            ConversationStarted?.Invoke();
            conversationStarted.Invoke();
        }

        private void ClearQuestionOptions()
        {
            QuestionOptionsUpdated?.Invoke(new List<QuestionCategoryData>());
            questionOptionsChanged.Invoke(string.Empty);
        }

        private static string GetResultGroup(NpcConversationResult result)
        {
            return NormalizeResult(result) switch
            {
                NpcConversationResult.Wrong => "Result_Wrong",
                NpcConversationResult.Similar => "Result_Similar",
                NpcConversationResult.Correct => "Result_Correct",
                NpcConversationResult.Perfect => "Result_Perfect",
                _ => "Result_Wrong"
            };
        }

        public static NpcConversationResult NormalizeResult(NpcConversationResult result)
        {
            return result == NpcConversationResult.Disgusting
                ? NpcConversationResult.Wrong
                : result;
        }

        private void EnsureDatabase()
        {
            if (_database != null)
                return;

            _database = NpcConversationDatabase.LoadFromResources(resourceFolder);
        }

        private void StopPlayback()
        {
            if (_playRoutine == null)
                return;

            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        private NpcOrderContext BuildCurrentOrderContext()
        {
            return NpcOrderContext.FromVisitEvent(
                _currentEvent,
                _remainingQuestionCount,
                _currentNpcAffinity);
        }
    }

    public sealed class NpcOrderContext
    {
        public string EventId { get; }
        public string NpcId { get; }
        public string CorrectRecipeId { get; }
        public IReadOnlyList<string> AllowedFoodTypes { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> PreferredTags { get; }
        public IReadOnlyList<string> AvoidTags { get; }
        public IReadOnlyList<string> DisgustingTags { get; }
        public int RemainingQuestionCount { get; }
        public int NpcAffinity { get; }

        private NpcOrderContext(
            string eventId,
            string npcId,
            string correctRecipeId,
            IReadOnlyList<string> allowedFoodTypes,
            IReadOnlyList<string> requiredTags,
            IReadOnlyList<string> preferredTags,
            IReadOnlyList<string> avoidTags,
            IReadOnlyList<string> disgustingTags,
            int remainingQuestionCount,
            int npcAffinity)
        {
            EventId = eventId ?? string.Empty;
            NpcId = npcId ?? string.Empty;
            CorrectRecipeId = correctRecipeId ?? string.Empty;
            AllowedFoodTypes = CopyList(allowedFoodTypes);
            RequiredTags = CopyList(requiredTags);
            PreferredTags = CopyList(preferredTags);
            AvoidTags = CopyList(avoidTags);
            DisgustingTags = CopyList(disgustingTags);
            RemainingQuestionCount = Mathf.Max(0, remainingQuestionCount);
            NpcAffinity = Mathf.Max(0, npcAffinity);
        }

        public static NpcOrderContext FromVisitEvent(
            VisitEventData visitEvent,
            int remainingQuestionCount,
            int npcAffinity)
        {
            if (visitEvent == null)
            {
                return new NpcOrderContext(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    remainingQuestionCount,
                    npcAffinity);
            }

            return new NpcOrderContext(
                visitEvent.EventId,
                visitEvent.NpcId,
                visitEvent.CorrectRecipeId,
                visitEvent.AllowedFoodTypes,
                visitEvent.RequiredTags,
                visitEvent.PreferredTags,
                visitEvent.AvoidTags,
                visitEvent.DisgustingTags,
                remainingQuestionCount,
                npcAffinity);
        }

        public string BuildDebugSummary()
        {
            return
                $"event={ValueOrNone(EventId)}, npc={ValueOrNone(NpcId)}, " +
                $"recipe={ValueOrNone(CorrectRecipeId)}, food={ListOrNone(AllowedFoodTypes)}, " +
                $"required={ListOrNone(RequiredTags)}, preferred={ListOrNone(PreferredTags)}, " +
                $"avoid={ListOrNone(AvoidTags)}, disgusting={ListOrNone(DisgustingTags)}";
        }

        private static IReadOnlyList<string> CopyList(IReadOnlyList<string> values)
        {
            return values != null ? values.ToList() : new List<string>();
        }

        private static string ListOrNone(IReadOnlyList<string> values)
        {
            return values != null && values.Count > 0 ? string.Join("|", values) : "None";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    public sealed class NpcDishResultContext
    {
        public NpcOrderContext Order { get; }
        public NpcDishSubmission Dish { get; }
        public NpcDishEvaluation Evaluation { get; }
        public string EventId => Order?.EventId ?? string.Empty;
        public string NpcId => Order?.NpcId ?? string.Empty;
        public NpcConversationResult Result => Evaluation?.Result ?? NpcConversationResult.Wrong;
        public string Reason => Evaluation?.Reason ?? string.Empty;

        public NpcDishResultContext(
            NpcOrderContext order,
            NpcDishSubmission dish,
            NpcDishEvaluation evaluation)
        {
            Order = order;
            Dish = dish;
            Evaluation = evaluation;
        }

        public string BuildDebugSummary()
        {
            string dishSummary = Dish != null ? Dish.BuildDebugSummary() : "None";
            return
                $"event={ValueOrNone(EventId)}, npc={ValueOrNone(NpcId)}, " +
                $"result={Result}, dish=({dishSummary}), reason={ValueOrNone(Reason)}";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    public sealed class NpcDishSubmission
    {
        private static readonly char[] TagSeparators = { '|', ',', ';', ' ' };

        public string RecipeId { get; }
        public string FoodType { get; }
        public IReadOnlyList<string> Tags { get; }
        public bool IsDisgusting { get; }
        public bool HasRecipeId => string.IsNullOrWhiteSpace(RecipeId) == false;
        public bool HasFoodType => string.IsNullOrWhiteSpace(FoodType) == false;
        public bool HasTags => Tags.Count > 0;

        public NpcDishSubmission(
            string recipeId,
            string foodType,
            IReadOnlyList<string> tags,
            bool isDisgusting = false)
        {
            RecipeId = recipeId?.Trim() ?? string.Empty;
            FoodType = foodType?.Trim() ?? string.Empty;
            Tags = CopyTags(tags);
            IsDisgusting = isDisgusting;
        }

        public static NpcDishSubmission FromText(string recipeId, string foodType, string tagText)
        {
            List<string> tags = string.IsNullOrWhiteSpace(tagText)
                ? new List<string>()
                : tagText
                    .Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(tag => tag.Trim())
                    .Where(tag => string.IsNullOrWhiteSpace(tag) == false)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return new NpcDishSubmission(recipeId, foodType, tags);
        }

        public string BuildDebugSummary()
        {
            string tags = Tags.Count > 0 ? string.Join("|", Tags) : "None";
            return $"Recipe={ValueOrNone(RecipeId)}, FoodType={ValueOrNone(FoodType)}, Tags={tags}, Disgusting={IsDisgusting}";
        }

        private static IReadOnlyList<string> CopyTags(IReadOnlyList<string> tags)
        {
            if (tags == null)
                return new List<string>();

            return tags
                .Where(tag => string.IsNullOrWhiteSpace(tag) == false)
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    public sealed class NpcDishMatchReport
    {
        public NpcOrderContext Order { get; }
        public NpcDishSubmission Dish { get; }
        public NpcDishEvaluation Evaluation { get; }
        public bool RecipeMatches { get; }
        public bool FoodTypeMatches { get; }
        public IReadOnlyList<string> MatchedRequiredTags { get; }
        public IReadOnlyList<string> MissingRequiredTags { get; }
        public IReadOnlyList<string> MatchedPreferredTags { get; }
        public IReadOnlyList<string> MissingPreferredTags { get; }
        public IReadOnlyList<string> MatchedAvoidTags { get; }
        public IReadOnlyList<string> MatchedDisgustingTags { get; }
        public int MatchScore { get; }
        public int MaxMatchScore { get; }
        public float MatchRatio => MaxMatchScore > 0 ? (float)MatchScore / MaxMatchScore : 0f;
        public bool HasBlockingIssue => Dish != null && Dish.IsDisgusting
                                        || MatchedAvoidTags.Count > 0
                                        || MatchedDisgustingTags.Count > 0;

        public NpcDishMatchReport(
            NpcOrderContext order,
            NpcDishSubmission dish,
            NpcDishEvaluation evaluation,
            bool recipeMatches,
            bool foodTypeMatches,
            IReadOnlyList<string> matchedRequiredTags,
            IReadOnlyList<string> missingRequiredTags,
            IReadOnlyList<string> matchedPreferredTags,
            IReadOnlyList<string> missingPreferredTags,
            IReadOnlyList<string> matchedAvoidTags,
            IReadOnlyList<string> matchedDisgustingTags,
            int matchScore,
            int maxMatchScore)
        {
            Order = order;
            Dish = dish;
            Evaluation = evaluation;
            RecipeMatches = recipeMatches;
            FoodTypeMatches = foodTypeMatches;
            MatchedRequiredTags = CopyList(matchedRequiredTags);
            MissingRequiredTags = CopyList(missingRequiredTags);
            MatchedPreferredTags = CopyList(matchedPreferredTags);
            MissingPreferredTags = CopyList(missingPreferredTags);
            MatchedAvoidTags = CopyList(matchedAvoidTags);
            MatchedDisgustingTags = CopyList(matchedDisgustingTags);
            MatchScore = Mathf.Max(0, matchScore);
            MaxMatchScore = Mathf.Max(0, maxMatchScore);
        }

        public string BuildDebugSummary()
        {
            int percent = Mathf.RoundToInt(MatchRatio * 100f);
            return
                $"Result={Evaluation?.Result ?? NpcConversationResult.Wrong}, Match={MatchScore}/{MaxMatchScore} ({percent}%), " +
                $"Recipe={RecipeMatches}, FoodType={FoodTypeMatches}, " +
                $"Required={ListOrNone(MatchedRequiredTags)}, Missing={ListOrNone(MissingRequiredTags)}, " +
                $"Preferred={ListOrNone(MatchedPreferredTags)}, Avoid={ListOrNone(MatchedAvoidTags)}, " +
                $"Disgusting={Dish?.IsDisgusting ?? false}/{ListOrNone(MatchedDisgustingTags)}, " +
                $"Reason={Evaluation?.Reason ?? string.Empty}";
        }

        private static IReadOnlyList<string> CopyList(IReadOnlyList<string> values)
        {
            return values != null ? values.ToList() : new List<string>();
        }

        private static string ListOrNone(IReadOnlyList<string> values)
        {
            return values != null && values.Count > 0 ? string.Join("|", values) : "None";
        }
    }

    public sealed class NpcDishEvaluation
    {
        public NpcConversationResult Result { get; }
        public string Reason { get; }

        public NpcDishEvaluation(NpcConversationResult result, string reason)
        {
            Result = NpcConversationRunner.NormalizeResult(result);
            Reason = reason;
        }
    }

    public static class NpcDishResultEvaluator
    {
        private const int RecipeMatchScore = 2;
        private const int FoodTypeMatchScore = 1;
        private const int RequiredTagMatchScore = 2;
        private const int PreferredTagMatchScore = 1;
        private const int AvoidTagPenalty = 1;
        private const int DisgustingTagPenalty = 2;
        private const int DisgustingDishPenalty = 2;

        public static NpcDishEvaluation Evaluate(VisitEventData visitEvent, NpcDishSubmission dish)
        {
            if (visitEvent == null)
                return new NpcDishEvaluation(NpcConversationResult.Wrong, "Visit event is missing.");

            return Evaluate(NpcOrderContext.FromVisitEvent(visitEvent, 0, 0), dish);
        }

        public static NpcDishEvaluation Evaluate(NpcOrderContext order, NpcDishSubmission dish)
        {
            if (order == null)
                return new NpcDishEvaluation(NpcConversationResult.Wrong, "NPC order is missing.");

            if (dish == null)
                return new NpcDishEvaluation(NpcConversationResult.Wrong, "Dish submission is missing.");

            return EvaluateFacts(BuildMatchFacts(order, dish));
        }

        public static NpcDishMatchReport BuildMatchReport(NpcOrderContext order, NpcDishSubmission dish)
        {
            if (order == null || dish == null)
            {
                NpcDishEvaluation missingEvaluation = Evaluate(order, dish);
                return new NpcDishMatchReport(
                    order,
                    dish,
                    missingEvaluation,
                    false,
                    false,
                    new List<string>(),
                    order?.RequiredTags ?? new List<string>(),
                    new List<string>(),
                    order?.PreferredTags ?? new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    0,
                    0);
            }

            NpcDishMatchFacts facts = BuildMatchFacts(order, dish);
            NpcDishEvaluation evaluation = EvaluateFacts(facts);
            CalculateMatchScore(facts, out int score, out int maxScore);

            return new NpcDishMatchReport(
                order,
                dish,
                evaluation,
                facts.RecipeMatches,
                facts.FoodTypeMatches,
                facts.MatchedRequiredTags,
                facts.MissingRequiredTags,
                facts.MatchedPreferredTags,
                facts.MissingPreferredTags,
                facts.MatchedAvoidTags,
                facts.MatchedDisgustingTags,
                Mathf.Clamp(score, 0, maxScore),
                maxScore);
        }

        public static string BuildRequirementSummary(VisitEventData visitEvent)
        {
            if (visitEvent == null)
                return "No active NPC order.";

            if (NpcVisitEventRules.RequiresCookingStep(visitEvent) == false)
                return "This NPC event has no cooking order.";

            return
                $"Recipe: {ValueOrNone(visitEvent.CorrectRecipeId)}\n" +
                $"Food: {ListOrNone(visitEvent.AllowedFoodTypes)}\n" +
                $"Required: {ListOrNone(visitEvent.RequiredTags)}\n" +
                $"Preferred: {ListOrNone(visitEvent.PreferredTags)}\n" +
                $"Avoid: {ListOrNone(visitEvent.AvoidTags)}\n" +
                $"Disgusting: {ListOrNone(visitEvent.DisgustingTags)}";
        }

        public static NpcDishSubmission BuildMatchingDish(VisitEventData visitEvent)
        {
            if (visitEvent == null)
                return new NpcDishSubmission(string.Empty, string.Empty, new List<string>());

            List<string> tags = new List<string>();
            tags.AddRange(visitEvent.RequiredTags);
            tags.AddRange(visitEvent.PreferredTags.Take(Math.Max(0, 2 - tags.Count)));

            return new NpcDishSubmission(
                visitEvent.CorrectRecipeId,
                visitEvent.AllowedFoodTypes.FirstOrDefault() ?? string.Empty,
                tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        public static NpcDishSubmission BuildDisgustingDish(VisitEventData visitEvent)
        {
            if (visitEvent == null)
                return new NpcDishSubmission(string.Empty, string.Empty, new List<string>());

            List<string> tags = new List<string>();
            tags.AddRange(visitEvent.RequiredTags.Take(1));

            string badTag = visitEvent.DisgustingTags.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(badTag))
                badTag = visitEvent.AvoidTags.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(badTag) == false)
                tags.Add(badTag);

            return new NpcDishSubmission(
                "Debug_BadDish",
                visitEvent.AllowedFoodTypes.FirstOrDefault() ?? string.Empty,
                tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                true);
        }

        private static NpcDishEvaluation EvaluateFacts(NpcDishMatchFacts facts)
        {
            if (facts.Dish.IsDisgusting)
                return new NpcDishEvaluation(NpcConversationResult.Wrong, "Dish was marked as disgusting.");

            if (facts.MatchedDisgustingTags.Count > 0)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Wrong,
                    $"Disgusting tag matched. count={facts.MatchedDisgustingTags.Count}");
            }

            if (facts.RecipeMatches && facts.MatchedAvoidTags.Count == 0)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Perfect,
                    "Correct recipe matched without avoid tags.");
            }

            if (facts.RecipeMatches)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Similar,
                    $"Correct recipe matched, but avoid tags were present. avoid={facts.MatchedAvoidTags.Count}");
            }

            if (facts.FoodTypeMatches && facts.RequiredTagsMatched && facts.MatchedAvoidTags.Count == 0)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Correct,
                    $"Food type and required tags matched. preferred={facts.MatchedPreferredTags.Count}");
            }

            if (IsSimilarMatch(facts))
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Similar,
                    $"Partial match. type={facts.FoodTypeMatches}, required={facts.MatchedRequiredTags.Count}/{facts.Order.RequiredTags.Count}, preferred={facts.MatchedPreferredTags.Count}, avoid={facts.MatchedAvoidTags.Count}");
            }

            return new NpcDishEvaluation(
                NpcConversationResult.Wrong,
                $"Not enough clues matched. type={facts.FoodTypeMatches}, required={facts.MatchedRequiredTags.Count}/{facts.Order.RequiredTags.Count}, preferred={facts.MatchedPreferredTags.Count}, avoid={facts.MatchedAvoidTags.Count}");
        }

        private static NpcDishMatchFacts BuildMatchFacts(NpcOrderContext order, NpcDishSubmission dish)
        {
            HashSet<string> dishTags = new HashSet<string>(dish.Tags, StringComparer.OrdinalIgnoreCase);
            bool recipeMatches = string.IsNullOrWhiteSpace(order.CorrectRecipeId) == false
                                 && string.Equals(order.CorrectRecipeId, dish.RecipeId, StringComparison.OrdinalIgnoreCase);
            bool foodTypeMatches = IsFoodTypeMatched(order.AllowedFoodTypes, dish.FoodType);

            return new NpcDishMatchFacts(
                order,
                dish,
                recipeMatches,
                foodTypeMatches,
                FilterMatches(order.RequiredTags, dishTags),
                FilterMissing(order.RequiredTags, dishTags),
                FilterMatches(order.PreferredTags, dishTags),
                FilterMissing(order.PreferredTags, dishTags),
                FilterMatches(order.AvoidTags, dishTags),
                FilterMatches(order.DisgustingTags, dishTags));
        }

        private static void CalculateMatchScore(NpcDishMatchFacts facts, out int score, out int maxScore)
        {
            maxScore = 0;
            score = 0;

            if (string.IsNullOrWhiteSpace(facts.Order.CorrectRecipeId) == false)
            {
                maxScore += RecipeMatchScore;
                if (facts.RecipeMatches)
                    score += RecipeMatchScore;
            }

            if (facts.Order.AllowedFoodTypes.Count > 0)
            {
                maxScore += FoodTypeMatchScore;
                if (facts.FoodTypeMatches)
                    score += FoodTypeMatchScore;
            }

            maxScore += facts.Order.RequiredTags.Count * RequiredTagMatchScore;
            score += facts.MatchedRequiredTags.Count * RequiredTagMatchScore;
            maxScore += facts.Order.PreferredTags.Count * PreferredTagMatchScore;
            score += facts.MatchedPreferredTags.Count * PreferredTagMatchScore;

            score -= facts.MatchedAvoidTags.Count * AvoidTagPenalty;
            score -= facts.MatchedDisgustingTags.Count * DisgustingTagPenalty;
            if (facts.Dish.IsDisgusting)
                score -= DisgustingDishPenalty;
        }

        private static bool IsSimilarMatch(NpcDishMatchFacts facts)
        {
            if (facts.MatchedAvoidTags.Count > 0)
                return facts.FoodTypeMatches && facts.MatchedRequiredTags.Count > 0;

            if (facts.Order.RequiredTags.Count == 0)
                return facts.FoodTypeMatches && facts.MatchedPreferredTags.Count > 0;

            int halfRequired = Math.Max(1, (int)Math.Ceiling(facts.Order.RequiredTags.Count * 0.5f));
            if (facts.FoodTypeMatches && facts.MatchedRequiredTags.Count >= halfRequired)
                return true;

            if (facts.RequiredTagsMatched)
                return true;

            int preferredThreshold = Math.Min(2, facts.Order.PreferredTags.Count);
            return facts.FoodTypeMatches
                   && preferredThreshold > 0
                   && facts.MatchedPreferredTags.Count >= preferredThreshold;
        }

        private static bool IsFoodTypeMatched(IReadOnlyList<string> allowedFoodTypes, string foodType)
        {
            if (allowedFoodTypes.Count == 0)
                return true;

            if (string.IsNullOrWhiteSpace(foodType))
                return false;

            return allowedFoodTypes.Any(
                allowedType => string.Equals(allowedType, foodType, StringComparison.OrdinalIgnoreCase));
        }

        private static int CountMatches(IReadOnlyList<string> expectedValues, HashSet<string> actualValues)
        {
            int count = 0;
            foreach (string expectedValue in expectedValues)
            {
                if (actualValues.Contains(expectedValue))
                    count++;
            }

            return count;
        }

        private static List<string> FilterMatches(IReadOnlyList<string> expectedValues, HashSet<string> actualValues)
        {
            List<string> matches = new List<string>();
            foreach (string expectedValue in expectedValues)
            {
                if (actualValues.Contains(expectedValue))
                    matches.Add(expectedValue);
            }

            return matches;
        }

        private static List<string> FilterMissing(IReadOnlyList<string> expectedValues, HashSet<string> actualValues)
        {
            List<string> missing = new List<string>();
            foreach (string expectedValue in expectedValues)
            {
                if (actualValues.Contains(expectedValue) == false)
                    missing.Add(expectedValue);
            }

            return missing;
        }

        private static string ListOrNone(IReadOnlyList<string> values)
        {
            return values.Count > 0 ? string.Join("|", values) : "None";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }

        private sealed class NpcDishMatchFacts
        {
            public NpcOrderContext Order { get; }
            public NpcDishSubmission Dish { get; }
            public bool RecipeMatches { get; }
            public bool FoodTypeMatches { get; }
            public IReadOnlyList<string> MatchedRequiredTags { get; }
            public IReadOnlyList<string> MissingRequiredTags { get; }
            public IReadOnlyList<string> MatchedPreferredTags { get; }
            public IReadOnlyList<string> MissingPreferredTags { get; }
            public IReadOnlyList<string> MatchedAvoidTags { get; }
            public IReadOnlyList<string> MatchedDisgustingTags { get; }
            public bool RequiredTagsMatched => MatchedRequiredTags.Count >= Order.RequiredTags.Count;

            public NpcDishMatchFacts(
                NpcOrderContext order,
                NpcDishSubmission dish,
                bool recipeMatches,
                bool foodTypeMatches,
                IReadOnlyList<string> matchedRequiredTags,
                IReadOnlyList<string> missingRequiredTags,
                IReadOnlyList<string> matchedPreferredTags,
                IReadOnlyList<string> missingPreferredTags,
                IReadOnlyList<string> matchedAvoidTags,
                IReadOnlyList<string> matchedDisgustingTags)
            {
                Order = order;
                Dish = dish;
                RecipeMatches = recipeMatches;
                FoodTypeMatches = foodTypeMatches;
                MatchedRequiredTags = matchedRequiredTags;
                MissingRequiredTags = missingRequiredTags;
                MatchedPreferredTags = matchedPreferredTags;
                MissingPreferredTags = missingPreferredTags;
                MatchedAvoidTags = matchedAvoidTags;
                MatchedDisgustingTags = matchedDisgustingTags;
            }
        }
    }
}
