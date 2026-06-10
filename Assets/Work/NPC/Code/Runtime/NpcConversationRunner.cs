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
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool showSpeakerName;
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
        public event Action CookingStepReady;
        public event Action ConversationCompleted;
        public event Action<string, NpcConversationResult> ResultDialogueStarted;

        public bool IsPlaying => _playRoutine != null;
        public bool HasActiveConversation => _currentEvent != null && _conversationCompleted == false;
        public int RemainingQuestionCount => _remainingQuestionCount;
        public string CurrentEventId => _currentEvent?.EventId;
        public string CurrentNpcId => _currentEvent?.NpcId;
        public int CurrentNpcAffinity => _currentEvent != null ? _currentNpcAffinity : 0;
        public bool IsReadyForCooking => _currentEvent != null
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
            _playRoutine = StartCoroutine(PlayStartGroupsRoutine());
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

            QuestionOptionsUpdated?.Invoke(new List<QuestionCategoryData>());
            questionOptionsChanged.Invoke(string.Empty);
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
            if (_currentEvent == null)
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
            if (_currentEvent == null || dish == null)
                return false;

            report = NpcDishResultEvaluator.BuildMatchReport(BuildCurrentOrderContext(), dish);
            return true;
        }

        public bool TryBuildDishResultContext(NpcDishSubmission dish, out NpcDishResultContext resultContext)
        {
            resultContext = null;
            if (_currentEvent == null || dish == null)
                return false;

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
            if (_currentEvent == null)
                return false;

            dish = NpcDishResultEvaluator.BuildMatchingDish(_currentEvent);
            return true;
        }

        public bool TryBuildDisgustingTestDish(out NpcDishSubmission dish)
        {
            dish = null;
            if (_currentEvent == null)
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
            PlayResultDialogue(NpcConversationResult.Disgusting);
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
                AddLine(line);

                if (lineDelay > 0f)
                    yield return new WaitForSeconds(lineDelay);
            }
        }

        private void AddLine(DialogueLineData line)
        {
            string text = line.Text;
            if (showSpeakerName)
                text = $"{GetSpeakerName(line.Speaker)}: {text}";

            if (chatPanel != null)
            {
                chatPanel.AddChat(text, line.IsPlayer);
                return;
            }

            Debug.Log(text);
        }

        private string GetSpeakerName(string speaker)
        {
            if (string.Equals(speaker, "Player", StringComparison.OrdinalIgnoreCase))
                return playerDisplayName;

            if (_database.Npcs.TryGetValue(speaker, out NpcData npc))
                return npc.DisplayName;

            return speaker;
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

            _cookingStepNotified = true;
            NpcOrderContext orderContext = BuildCurrentOrderContext();

            QuestionOptionsUpdated?.Invoke(new List<QuestionCategoryData>());
            questionOptionsChanged.Invoke(string.Empty);
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

        private static string GetResultGroup(NpcConversationResult result)
        {
            return result switch
            {
                NpcConversationResult.Disgusting => "Result_Disgusting",
                NpcConversationResult.Wrong => "Result_Wrong",
                NpcConversationResult.Similar => "Result_Similar",
                NpcConversationResult.Correct => "Result_Correct",
                NpcConversationResult.Perfect => "Result_Perfect",
                _ => "Result_Wrong"
            };
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

        public NpcDishSubmission(
            string recipeId,
            string foodType,
            IReadOnlyList<string> tags,
            bool isDisgusting = false)
        {
            RecipeId = recipeId?.Trim() ?? string.Empty;
            FoodType = foodType?.Trim() ?? string.Empty;
            Tags = tags ?? new List<string>();
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
            return $"Recipe={ValueOrNone(RecipeId)}, Type={ValueOrNone(FoodType)}, Tags={tags}, Disgusting={IsDisgusting}";
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
            Result = result;
            Reason = reason;
        }
    }

    public static class NpcDishResultEvaluator
    {
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

            if (dish.IsDisgusting)
                return new NpcDishEvaluation(NpcConversationResult.Disgusting, "Dish was marked as disgusting.");

            HashSet<string> dishTags = new HashSet<string>(dish.Tags, StringComparer.OrdinalIgnoreCase);
            int disgustingMatches = CountMatches(order.DisgustingTags, dishTags);
            if (disgustingMatches > 0)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Disgusting,
                    $"Disgusting tag matched. count={disgustingMatches}");
            }

            bool recipeMatches = string.IsNullOrWhiteSpace(order.CorrectRecipeId) == false
                                 && string.Equals(order.CorrectRecipeId, dish.RecipeId, StringComparison.OrdinalIgnoreCase);
            bool foodTypeMatches = IsFoodTypeMatched(order.AllowedFoodTypes, dish.FoodType);
            int requiredMatches = CountMatches(order.RequiredTags, dishTags);
            int preferredMatches = CountMatches(order.PreferredTags, dishTags);
            int avoidMatches = CountMatches(order.AvoidTags, dishTags);
            bool requiredTagsMatched = requiredMatches >= order.RequiredTags.Count;

            if (recipeMatches && avoidMatches == 0)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Perfect,
                    "Correct recipe matched without avoid tags.");
            }

            if (recipeMatches)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Similar,
                    $"Correct recipe matched, but avoid tags were present. avoid={avoidMatches}");
            }

            if (foodTypeMatches && requiredTagsMatched && avoidMatches == 0)
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Correct,
                    $"Food type and required tags matched. preferred={preferredMatches}");
            }

            if (IsSimilarMatch(order, foodTypeMatches, requiredMatches, preferredMatches, avoidMatches))
            {
                return new NpcDishEvaluation(
                    NpcConversationResult.Similar,
                    $"Partial match. type={foodTypeMatches}, required={requiredMatches}/{order.RequiredTags.Count}, preferred={preferredMatches}, avoid={avoidMatches}");
            }

            return new NpcDishEvaluation(
                NpcConversationResult.Wrong,
                $"Not enough clues matched. type={foodTypeMatches}, required={requiredMatches}/{order.RequiredTags.Count}, preferred={preferredMatches}, avoid={avoidMatches}");
        }

        public static NpcDishMatchReport BuildMatchReport(NpcOrderContext order, NpcDishSubmission dish)
        {
            NpcDishEvaluation evaluation = Evaluate(order, dish);

            if (order == null || dish == null)
            {
                return new NpcDishMatchReport(
                    order,
                    dish,
                    evaluation,
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

            HashSet<string> dishTags = new HashSet<string>(dish.Tags, StringComparer.OrdinalIgnoreCase);
            bool recipeMatches = string.IsNullOrWhiteSpace(order.CorrectRecipeId) == false
                                 && string.Equals(order.CorrectRecipeId, dish.RecipeId, StringComparison.OrdinalIgnoreCase);
            bool foodTypeMatches = IsFoodTypeMatched(order.AllowedFoodTypes, dish.FoodType);
            List<string> matchedRequiredTags = FilterMatches(order.RequiredTags, dishTags);
            List<string> missingRequiredTags = FilterMissing(order.RequiredTags, dishTags);
            List<string> matchedPreferredTags = FilterMatches(order.PreferredTags, dishTags);
            List<string> missingPreferredTags = FilterMissing(order.PreferredTags, dishTags);
            List<string> matchedAvoidTags = FilterMatches(order.AvoidTags, dishTags);
            List<string> matchedDisgustingTags = FilterMatches(order.DisgustingTags, dishTags);

            int maxScore = 0;
            int score = 0;

            if (string.IsNullOrWhiteSpace(order.CorrectRecipeId) == false)
            {
                maxScore += 2;
                if (recipeMatches)
                    score += 2;
            }

            if (order.AllowedFoodTypes.Count > 0)
            {
                maxScore += 1;
                if (foodTypeMatches)
                    score += 1;
            }

            maxScore += order.RequiredTags.Count * 2;
            score += matchedRequiredTags.Count * 2;
            maxScore += order.PreferredTags.Count;
            score += matchedPreferredTags.Count;

            score -= matchedAvoidTags.Count;
            score -= matchedDisgustingTags.Count * 2;
            if (dish.IsDisgusting)
                score -= 2;

            return new NpcDishMatchReport(
                order,
                dish,
                evaluation,
                recipeMatches,
                foodTypeMatches,
                matchedRequiredTags,
                missingRequiredTags,
                matchedPreferredTags,
                missingPreferredTags,
                matchedAvoidTags,
                matchedDisgustingTags,
                Mathf.Clamp(score, 0, maxScore),
                maxScore);
        }

        public static string BuildRequirementSummary(VisitEventData visitEvent)
        {
            if (visitEvent == null)
                return "No active NPC order.";

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

        private static bool IsSimilarMatch(
            NpcOrderContext order,
            bool foodTypeMatches,
            int requiredMatches,
            int preferredMatches,
            int avoidMatches)
        {
            if (avoidMatches > 0)
                return foodTypeMatches && requiredMatches > 0;

            if (order.RequiredTags.Count == 0)
                return foodTypeMatches && preferredMatches > 0;

            int halfRequired = Math.Max(1, (int)Math.Ceiling(order.RequiredTags.Count * 0.5f));
            if (foodTypeMatches && requiredMatches >= halfRequired)
                return true;

            if (requiredMatches >= order.RequiredTags.Count)
                return true;

            int preferredThreshold = Math.Min(2, order.PreferredTags.Count);
            return foodTypeMatches && preferredThreshold > 0 && preferredMatches >= preferredThreshold;
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
    }
}
