using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime
{
    [Serializable]
    public sealed class CookingGameScreenChangedEvent : UnityEvent<CookingGameScreenState>
    {
    }

    [Serializable]
    public sealed class CookingGameDishResultEvent : UnityEvent<DishResult>
    {
    }

    [Serializable]
    public sealed class CookingGameRewardAmountEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public sealed class CookingGameSnapshotEvent : UnityEvent<CookingGameSnapshot>
    {
    }

    public sealed class CookingGamePanel : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;
        [SerializeField] private CookingRecipeIngredientChoiceSource recipeIngredientChoiceSource;
        [SerializeField] private CookingGameScreenState initialScreen = CookingGameScreenState.None;
        [SerializeField] private bool applyInitialScreenOnAwake = true;
        [SerializeField] private bool resetFlowWhenOpeningRecipeSelection = true;
        [SerializeField] private bool resetFlowAfterHandingDish = true;
        [SerializeField] private bool keepNpcConversationVisibleBeforePreparation = true;
        [SerializeField] private bool keepRecipeSelectionVisibleBeforePreparation = true;
        [SerializeField] private bool keepRecipeSelectionVisibleDuringInventory;
        [SerializeField] private bool autoCreateTemporaryInventoryView = true;
        [SerializeField] private bool autoCreateTemporaryPreparationView = true;
        [SerializeField] private bool autoCreateTemporaryResultView = true;
        [SerializeField] private TMP_FontAsset temporaryUiFontAsset;

        [Header("Rewards")]
        [SerializeField] private CookingRewardWallet rewardWallet;
        [SerializeField] private CookingRewardCalculator rewardCalculator;
        [SerializeField] private bool autoCreateRewardSystems = true;
        [SerializeField] private bool autoCreateTemporaryRewardView = true;

        [Header("Views")]
        [SerializeField] private GameObject npcConversationView;
        [SerializeField] private GameObject recipeSelectionView;
        [SerializeField] private GameObject inventoryView;
        [SerializeField] private GameObject preparationView;
        [SerializeField] private GameObject resultView;
        [SerializeField] private GameObject rewardView;

        [Header("Events")]
        [SerializeField] private CookingGameScreenChangedEvent screenChanged = new CookingGameScreenChangedEvent();
        [SerializeField] private CookingGameDishResultEvent resultReady = new CookingGameDishResultEvent();
        [SerializeField] private CookingGameDishResultEvent dishHandedToNpc = new CookingGameDishResultEvent();
        [SerializeField] private CookingGameRewardAmountEvent rewardGranted = new CookingGameRewardAmountEvent();
        [SerializeField] private CookingGameSnapshotEvent snapshotChanged = new CookingGameSnapshotEvent();

        private DishResult _currentResult;
        private CookingFlowRunner _subscribedFlowRunner;
        private CookingKnowledgeStore _subscribedKnowledgeStore;
        private CookingRewardWallet _subscribedRewardWallet;

        public event Action<CookingGameScreenState> ScreenChanged;
        public event Action<DishResult> ResultReady;
        public event Action<DishResult> DishHandedToNpc;
        public event Action<CookingRewardGrant> RewardGranted;
        public event Action<CookingGameSnapshot> SnapshotChanged;

        public CookingFlowRunner FlowRunner => flowRunner;
        public NpcConversationRunner NpcRunner => npcRunner;
        public GameObject NpcConversationView => npcConversationView;
        public GameObject RecipeSelectionView => recipeSelectionView;
        public GameObject InventoryView => inventoryView;
        public GameObject PreparationView => preparationView;
        public GameObject ResultView => resultView;
        public GameObject RewardView => rewardView;
        public CookingRewardWallet RewardWallet
        {
            get
            {
                EnsureRewardSystems();
                return rewardWallet;
            }
        }
        public CookingKnowledgeStore KnowledgeStore
        {
            get
            {
                EnsureKnowledgeStore();
                return knowledgeStore;
            }
        }
        public CookingGameScreenState CurrentScreen { get; private set; } = CookingGameScreenState.None;
        public DishResult CurrentResult => _currentResult;
        public CookingGameSnapshot CurrentSnapshot => BuildSnapshot();

        private void Awake()
        {
            EnsureReferences();

            if (applyInitialScreenOnAwake)
                SetScreen(initialScreen);
            else
            {
                ApplyViewActiveStates();
                PublishSnapshotChanged();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeStateSources();
        }

        public void SetFlowRunner(CookingFlowRunner value)
        {
            flowRunner = value;
            InitializeKnowledgeStore();
            ReinitializeCookingViews();
        }

        public void SetNpcRunner(NpcConversationRunner value)
        {
            npcRunner = value;
            PublishSnapshotChanged();
        }

        public void SetKnowledgeStore(CookingKnowledgeStore value)
        {
            knowledgeStore = value;
            InitializeKnowledgeStore();
            ReinitializeCookingViews();
        }

        public void SetTemporaryUiFontAsset(TMP_FontAsset value)
        {
            temporaryUiFontAsset = value;
            ApplyTemporaryFontToViews();
        }

        public void SetNpcConversationView(GameObject value)
        {
            npcConversationView = value;
            ReinitializeCookingViews();
        }

        public void SetRecipeSelectionView(GameObject value)
        {
            recipeSelectionView = value;
            ReinitializeCookingViews();
        }

        public void SetInventoryView(GameObject value)
        {
            inventoryView = value;
            ReinitializeCookingViews();
        }

        public void SetPreparationView(GameObject value)
        {
            preparationView = value;
            ReinitializeCookingViews();
        }

        public void SetResultView(GameObject value)
        {
            resultView = value;
            ReinitializeCookingViews();
        }

        public void SetRewardView(GameObject value)
        {
            rewardView = value;
            ReinitializeCookingViews();
        }

        public void BindCookingViews(
            GameObject npcConversation,
            GameObject recipeSelection,
            GameObject inventory,
            GameObject preparation,
            GameObject result,
            GameObject reward)
        {
            npcConversationView = npcConversation;
            recipeSelectionView = recipeSelection;
            inventoryView = inventory;
            preparationView = preparation;
            resultView = result;
            rewardView = reward;
            ReinitializeCookingViews();
        }

        public void ReinitializeCookingViews()
        {
            EnsureReferences();
            InitializeRecipeSelectionView(recipeSelectionView);
            InitializeIngredientSelectionView(inventoryView);
            InitializePreparationView(preparationView);
            InitializeResultView(resultView);
            InitializeRewardView(rewardView);
            ApplyViewActiveStates();
            RefreshCookingViews();
            PublishSnapshotChanged();
        }

        public void RefreshCookingViews()
        {
            RefreshRecipeSelectionView(recipeSelectionView);

            if (CurrentScreen == CookingGameScreenState.Inventory)
                RefreshIngredientSelectionView(inventoryView);

            if (CurrentScreen == CookingGameScreenState.Preparation)
                RefreshPreparationView(preparationView);

            if (CurrentScreen == CookingGameScreenState.Result)
                RefreshResultView(resultView);
        }

        public CookingGameSnapshot BuildSnapshot()
        {
            CookingFlowState flowState = flowRunner != null ? flowRunner.State : CookingFlowState.Idle;
            CookingSession session = flowRunner != null ? flowRunner.Controller.CurrentSession : null;
            RecipeSO selectedRecipe = session != null ? session.SelectedRecipe : null;
            IngredientSO currentIngredient = flowRunner != null ? flowRunner.GetNextUnpreparedIngredient() : null;
            int knownRecipeCount = knowledgeStore != null ? knowledgeStore.DiscoveredRecipeCount : 0;
            int knownPreparationEffectCount = knowledgeStore != null ? knowledgeStore.KnownPreparationEffectCount : 0;
            int rewardBalance = rewardWallet != null ? rewardWallet.Balance : 0;
            DishResult currentResult = _currentResult ?? flowRunner?.LastResult;
            NpcDishMatchReport matchReport = null;
            int previewRewardAmount = 0;

            if (currentResult != null
                && npcRunner != null
                && CookingNpcDishAdapter.TryBuildMatchReport(npcRunner, currentResult, out matchReport)
                && rewardCalculator != null)
            {
                previewRewardAmount = rewardCalculator.CalculateAmount(matchReport, currentResult);
            }

            return new CookingGameSnapshot(
                CurrentScreen,
                flowState,
                session?.Mode,
                selectedRecipe,
                currentIngredient,
                currentResult,
                flowRunner != null ? flowRunner.SelectedIngredients : null,
                flowRunner != null ? flowRunner.PreparedIngredients : null,
                knownRecipeCount,
                knownPreparationEffectCount,
                rewardBalance,
                previewRewardAmount,
                matchReport,
                currentResult != null && npcRunner != null);
        }

        public void OpenRecipeSelection()
        {
            EnsureReferences();

            if (resetFlowWhenOpeningRecipeSelection && flowRunner != null)
                flowRunner.ResetFlow();

            _currentResult = null;
            SetScreen(CookingGameScreenState.RecipeSelection);
        }

        public bool ConfirmRecipe(RecipeSO recipe)
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can confirm a recipe.", this);
                return false;
            }

            if (TryBeginRecipeWithIngredientChoices(recipe))
                return true;

            if (flowRunner.BeginRecipeCooking(recipe) == false)
            {
                Debug.LogWarning("CookingGamePanel could not begin recipe cooking because the recipe is missing.", this);
                return false;
            }

            _currentResult = null;
            SetScreen(CookingGameScreenState.Preparation);
            return true;
        }

        private bool TryBeginRecipeWithIngredientChoices(RecipeSO recipe)
        {
            if (recipe == null || flowRunner == null)
                return false;

            List<IngredientSO> fixedIngredients = new List<IngredientSO>();
            List<IngredientSO> choiceCandidates = new List<IngredientSO>();
            int minChoiceCount = 0;
            int maxChoiceCount = 0;

            IReadOnlyList<IngredientSO> availableIngredients = flowRunner.Ingredients;
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null)
                    continue;

                List<IngredientSO> candidates = BuildRecipeRequirementCandidates(requirement, availableIngredients);
                if (RequiresPlayerChoice(requirement, candidates))
                {
                    AddUnique(choiceCandidates, candidates);
                    minChoiceCount += requirement.MinCount;
                    if (requirement.HasMaxCount)
                        maxChoiceCount += requirement.MaxCount;
                    else
                        maxChoiceCount = 0;
                    continue;
                }

                int autoCount = Mathf.Max(1, requirement.MinCount);
                for (int candidateIndex = 0; candidateIndex < candidates.Count && candidateIndex < autoCount; candidateIndex++)
                    AddUnique(fixedIngredients, candidates[candidateIndex]);
            }

            if (choiceCandidates.Count == 0)
                return false;

            if (flowRunner.BeginRecipeIngredientSelection(recipe) == false)
                return false;

            for (int i = 0; i < fixedIngredients.Count; i++)
                flowRunner.AddRecipeIngredient(fixedIngredients[i]);

            EnsureRecipeIngredientChoiceSource();
            recipeIngredientChoiceSource.SetCandidates(choiceCandidates);
            SetIngredientSelectionSource(recipeIngredientChoiceSource);
            SetIngredientSelectionLimits(
                fixedIngredients.Count + minChoiceCount,
                maxChoiceCount > 0 ? fixedIngredients.Count + maxChoiceCount : 0);

            _currentResult = null;
            SetScreen(CookingGameScreenState.Inventory);
            return true;
        }

        private static List<IngredientSO> BuildRecipeRequirementCandidates(
            RecipeIngredientRequirement requirement,
            IReadOnlyList<IngredientSO> availableIngredients)
        {
            List<IngredientSO> candidates = new List<IngredientSO>();
            if (requirement == null)
                return candidates;

            if (availableIngredients != null)
            {
                for (int i = 0; i < availableIngredients.Count; i++)
                {
                    IngredientSO ingredient = availableIngredients[i];
                    if (ingredient != null && requirement.IsMatchedBy(ingredient))
                        AddUnique(candidates, ingredient);
                }
            }

            if (candidates.Count == 0 && requirement.Ingredient != null)
                candidates.Add(requirement.Ingredient);

            return candidates;
        }

        private static bool RequiresPlayerChoice(
            RecipeIngredientRequirement requirement,
            IReadOnlyList<IngredientSO> candidates)
        {
            if (requirement == null || candidates == null)
                return false;

            if (requirement.RequiresChoice == false)
                return false;

            if (candidates.Count <= 1)
                return false;

            if (requirement.HasMaxCount && requirement.MinCount == requirement.MaxCount && candidates.Count <= requirement.MinCount)
                return false;

            return true;
        }

        private static void AddUnique(ICollection<IngredientSO> target, IngredientSO ingredient)
        {
            if (target == null || ingredient == null || target.Contains(ingredient))
                return;

            target.Add(ingredient);
        }

        private static void AddUnique(ICollection<IngredientSO> target, IReadOnlyList<IngredientSO> ingredients)
        {
            if (target == null || ingredients == null)
                return;

            for (int i = 0; i < ingredients.Count; i++)
                AddUnique(target, ingredients[i]);
        }

        public bool OpenDirectIngredientSelection()
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can open direct ingredient selection.", this);
                return false;
            }

            if (flowRunner.State != CookingFlowState.SelectingIngredients)
                flowRunner.BeginDirectSelection();

            SetIngredientSelectionSource(null);
            SetIngredientSelectionLimits(1, 0);
            _currentResult = null;
            SetScreen(CookingGameScreenState.Inventory);
            return true;
        }

        public bool OpenInventory()
        {
            return OpenDirectIngredientSelection();
        }

        public void SetIngredientSelectionSource(ICookingIngredientSource source)
        {
            EnsureReferences();
            GetIngredientSelectionView()?.SetIngredientSource(source);
        }

        public void SetIngredientSelectionLimits(int minCount, int maxCount = 0)
        {
            EnsureReferences();
            GetIngredientSelectionView()?.SetSelectionLimits(minCount, maxCount);
        }

        public void SetIngredientSearchQuery(string query)
        {
            EnsureReferences();
            GetIngredientSelectionView()?.SetSearchQuery(query);
        }

        public void ToggleIngredientSelection(IngredientSO ingredient)
        {
            EnsureReferences();
            GetIngredientSelectionView()?.ToggleIngredient(ingredient);
        }

        public void RemoveIngredientSelection(IngredientSO ingredient)
        {
            EnsureReferences();
            GetIngredientSelectionView()?.RemoveIngredient(ingredient);
        }

        public void ClearIngredientSelection()
        {
            EnsureReferences();
            GetIngredientSelectionView()?.ClearSelection();
        }

        public void ConfirmIngredientSelection()
        {
            EnsureReferences();

            ICookingIngredientSelectionView selectionView = GetIngredientSelectionView();
            if (selectionView != null)
                selectionView.ConfirmSelection();
            else
                ConfirmDirectIngredients();
        }

        public bool ConfirmDirectIngredients()
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can confirm ingredients.", this);
                return false;
            }

            if (flowRunner.ConfirmDirectIngredients() == false)
            {
                Debug.LogWarning("CookingGamePanel could not confirm direct ingredients. Select at least one ingredient first.", this);
                return false;
            }

            SetIngredientSelectionSource(null);
            SetScreen(CookingGameScreenState.Preparation);
            return true;
        }

        public void OpenPreparation()
        {
            SetScreen(CookingGameScreenState.Preparation);
        }

        public IngredientSO GetCurrentPreparationIngredient()
        {
            EnsureReferences();
            return flowRunner != null ? flowRunner.GetNextUnpreparedIngredient() : null;
        }

        public IReadOnlyList<IngredientPreparationOption> GetCurrentPreparationOptions()
        {
            IngredientSO ingredient = GetCurrentPreparationIngredient();
            return GetPreparationOptions(ingredient);
        }

        public IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient)
        {
            EnsureReferences();

            if (flowRunner == null || ingredient == null)
                return Array.Empty<IngredientPreparationOption>();

            return flowRunner.GetPreparationOptions(ingredient);
        }

        public bool IsPreparationEffectKnown(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();
            return knowledgeStore != null && knowledgeStore.IsPreparationEffectKnown(ingredient, option);
        }

        public bool SelectCurrentPreparationByIndex(int optionIndex)
        {
            IngredientSO ingredient = GetCurrentPreparationIngredient();
            IReadOnlyList<IngredientPreparationOption> options = GetPreparationOptions(ingredient);

            if (optionIndex < 0 || options == null || optionIndex >= options.Count)
            {
                Debug.LogWarning($"CookingGamePanel could not select preparation index {optionIndex}.", this);
                return false;
            }

            return SelectPreparation(ingredient, options[optionIndex]);
        }

        public bool SelectCurrentPreparation(IngredientPreparationOption option)
        {
            return SelectPreparation(GetCurrentPreparationIngredient(), option);
        }

        public bool SelectPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can select a preparation.", this);
                return false;
            }

            if (ingredient == null)
            {
                Debug.LogWarning("CookingGamePanel could not select a preparation because the ingredient is missing.", this);
                return false;
            }

            if (option != null)
                knowledgeStore?.LearnPreparationEffect(ingredient, option);

            if (flowRunner.SelectPreparation(ingredient, option) == false)
            {
                Debug.LogWarning("CookingGamePanel could not apply the selected preparation.", this);
                return false;
            }

            if (flowRunner.GetNextUnpreparedIngredient() == null)
                return CompleteCooking();

            RefreshPreparationView(preparationView);
            PublishSnapshotChanged();
            return true;
        }

        public bool CompleteCooking()
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can complete cooking.", this);
                return false;
            }

            if (flowRunner.TryCompleteCooking(out DishResult result) == false)
            {
                Debug.LogWarning("CookingGamePanel could not complete cooking. Make sure every selected ingredient is prepared.", this);
                return false;
            }

            return OpenResult(result);
        }

        public bool OpenResult(DishResult result)
        {
            if (result == null)
            {
                Debug.LogWarning("CookingGamePanel cannot open the result screen without a dish result.", this);
                return false;
            }

            _currentResult = result;
            knowledgeStore?.LearnFromResult(result);
            SetScreen(CookingGameScreenState.Result);
            ResultReady?.Invoke(result);
            resultReady.Invoke(result);
            return true;
        }

        public DishResult GetCurrentDishResult()
        {
            EnsureCoreReferences();
            return _currentResult ?? flowRunner?.LastResult;
        }

        public bool CanHandCurrentResultToNpc()
        {
            return GetCurrentDishResult() != null && NpcRunner != null;
        }

        public bool TryBuildCurrentNpcMatchReport(out NpcDishMatchReport matchReport)
        {
            return TryBuildNpcMatchReport(GetCurrentDishResult(), out matchReport);
        }

        public bool TryBuildNpcMatchReport(DishResult result, out NpcDishMatchReport matchReport)
        {
            EnsureCoreReferences();
            return CookingNpcDishAdapter.TryBuildMatchReport(npcRunner, result, out matchReport);
        }

        public int PreviewCurrentRewardAmount()
        {
            return PreviewRewardAmount(GetCurrentDishResult());
        }

        public int PreviewRewardAmount(DishResult result)
        {
            EnsureCoreReferences();

            if (rewardCalculator == null
                || TryBuildNpcMatchReport(result, out NpcDishMatchReport matchReport) == false)
            {
                return 0;
            }

            return rewardCalculator.CalculateAmount(matchReport, result);
        }

        public bool HandResultToNpc()
        {
            EnsureCoreReferences();

            DishResult result = GetCurrentDishResult();
            if (result == null)
            {
                Debug.LogWarning("CookingGamePanel cannot hand a dish to the NPC because no result is ready.", this);
                return false;
            }

            TryBuildNpcMatchReport(result, out NpcDishMatchReport matchReport);

            ReturnToNpcConversation();
            Canvas.ForceUpdateCanvases();

            if (CookingNpcDishAdapter.SubmitToNpc(npcRunner, result, out string submitBlockReason) == false)
            {
                Debug.LogWarning(
                    $"CookingGamePanel could not submit the dish. reason={submitBlockReason}",
                    this);
                SetScreen(CookingGameScreenState.Result);
                return false;
            }

            DishHandedToNpc?.Invoke(result);
            dishHandedToNpc.Invoke(result);
            GrantReward(result, matchReport);

            if (resetFlowAfterHandingDish && flowRunner != null)
                flowRunner.ResetFlow();

            return true;
        }

        public void ReturnToNpcConversation()
        {
            SetScreen(CookingGameScreenState.NpcConversation);
        }

        public void CloseCookingViews()
        {
            SetScreen(CookingGameScreenState.None);
        }

        private void EnsureCoreReferences()
        {
            if (flowRunner == null)
                flowRunner = GetComponentInChildren<CookingFlowRunner>(true);

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            EnsureKnowledgeStore();
            EnsureRewardSystems();
            EnsureRecipeIngredientChoiceSource();
            SubscribeStateSources();
        }

        private void EnsureRecipeIngredientChoiceSource()
        {
            if (recipeIngredientChoiceSource == null)
                recipeIngredientChoiceSource = GetComponentInChildren<CookingRecipeIngredientChoiceSource>(true);

            if (recipeIngredientChoiceSource == null)
                recipeIngredientChoiceSource = GetComponent<CookingRecipeIngredientChoiceSource>();

            if (recipeIngredientChoiceSource == null)
                recipeIngredientChoiceSource = gameObject.AddComponent<CookingRecipeIngredientChoiceSource>();
        }

        private void EnsureReferences()
        {
            EnsureCoreReferences();
            InitializeRecipeSelectionView(recipeSelectionView);
            EnsureInventoryView();
            EnsurePreparationView();
            EnsureResultView();
            EnsureRewardView();
        }

        private void EnsureKnowledgeStore()
        {
            if (knowledgeStore == null)
                knowledgeStore = GetComponentInChildren<CookingKnowledgeStore>(true);

            if (knowledgeStore == null)
                knowledgeStore = GetComponent<CookingKnowledgeStore>();

            if (knowledgeStore == null)
                knowledgeStore = gameObject.AddComponent<CookingKnowledgeStore>();

            InitializeKnowledgeStore();
        }

        private void InitializeKnowledgeStore()
        {
            if (knowledgeStore == null)
                return;

            knowledgeStore.Initialize(flowRunner != null ? flowRunner.Catalog : null);
        }

        private void EnsureRewardSystems()
        {
            if (autoCreateRewardSystems == false)
                return;

            if (rewardWallet == null)
                rewardWallet = GetComponentInChildren<CookingRewardWallet>(true);

            if (rewardWallet == null)
                rewardWallet = GetComponent<CookingRewardWallet>();

            if (rewardWallet == null)
                rewardWallet = gameObject.AddComponent<CookingRewardWallet>();

            rewardWallet.Initialize();

            if (rewardCalculator == null)
                rewardCalculator = GetComponentInChildren<CookingRewardCalculator>(true);

            if (rewardCalculator == null)
                rewardCalculator = GetComponent<CookingRewardCalculator>();

            if (rewardCalculator == null)
                rewardCalculator = gameObject.AddComponent<CookingRewardCalculator>();
        }

        private CookingRewardGrant GrantReward(DishResult result, NpcDishMatchReport matchReport)
        {
            if (result == null || matchReport == null)
                return null;

            EnsureRewardSystems();

            if (rewardWallet == null || rewardCalculator == null)
                return null;

            int amount = rewardCalculator.CalculateAmount(matchReport, result);
            int balanceAfter = rewardWallet.Grant(amount);
            CookingRewardGrant grant = new CookingRewardGrant(result, matchReport, amount, balanceAfter);

            RewardGranted?.Invoke(grant);
            rewardGranted.Invoke(grant.Amount);

            Debug.Log($"Cooking reward resolved: {grant.BuildDebugSummary()}", this);
            PublishSnapshotChanged();
            return grant;
        }

        private void SetScreen(CookingGameScreenState screen)
        {
            CurrentScreen = screen;
            ApplyViewActiveStates();
            ScreenChanged?.Invoke(CurrentScreen);
            screenChanged.Invoke(CurrentScreen);
            PublishSnapshotChanged();
        }

        private void SubscribeStateSources()
        {
            if (_subscribedFlowRunner != flowRunner)
            {
                if (_subscribedFlowRunner != null)
                    _subscribedFlowRunner.StateChanged -= HandleFlowRunnerStateChanged;

                _subscribedFlowRunner = flowRunner;

                if (_subscribedFlowRunner != null)
                    _subscribedFlowRunner.StateChanged += HandleFlowRunnerStateChanged;
            }

            if (_subscribedKnowledgeStore != knowledgeStore)
            {
                if (_subscribedKnowledgeStore != null)
                    _subscribedKnowledgeStore.KnowledgeChanged -= HandleKnowledgeChanged;

                _subscribedKnowledgeStore = knowledgeStore;

                if (_subscribedKnowledgeStore != null)
                    _subscribedKnowledgeStore.KnowledgeChanged += HandleKnowledgeChanged;
            }

            if (_subscribedRewardWallet != rewardWallet)
            {
                if (_subscribedRewardWallet != null)
                    _subscribedRewardWallet.BalanceChanged -= HandleRewardBalanceChanged;

                _subscribedRewardWallet = rewardWallet;

                if (_subscribedRewardWallet != null)
                    _subscribedRewardWallet.BalanceChanged += HandleRewardBalanceChanged;
            }
        }

        private void UnsubscribeStateSources()
        {
            if (_subscribedFlowRunner != null)
                _subscribedFlowRunner.StateChanged -= HandleFlowRunnerStateChanged;

            if (_subscribedKnowledgeStore != null)
                _subscribedKnowledgeStore.KnowledgeChanged -= HandleKnowledgeChanged;

            if (_subscribedRewardWallet != null)
                _subscribedRewardWallet.BalanceChanged -= HandleRewardBalanceChanged;

            _subscribedFlowRunner = null;
            _subscribedKnowledgeStore = null;
            _subscribedRewardWallet = null;
        }

        private void HandleFlowRunnerStateChanged(CookingFlowState state)
        {
            RefreshCookingViews();
            PublishSnapshotChanged();
        }

        private void HandleKnowledgeChanged()
        {
            RefreshRecipeSelectionView(recipeSelectionView);
            PublishSnapshotChanged();
        }

        private void HandleRewardBalanceChanged(int balance)
        {
            PublishSnapshotChanged();
        }

        private void PublishSnapshotChanged()
        {
            CookingGameSnapshot snapshot = BuildSnapshot();
            SnapshotChanged?.Invoke(snapshot);
            snapshotChanged.Invoke(snapshot);
        }

        private void ApplyViewActiveStates()
        {
            bool beforePreparation = IsBeforePreparation(CurrentScreen);
            bool showNpcConversation = CurrentScreen == CookingGameScreenState.NpcConversation
                                       || keepNpcConversationVisibleBeforePreparation && beforePreparation;
            bool showRecipeSelection = CurrentScreen == CookingGameScreenState.RecipeSelection
                                       || keepRecipeSelectionVisibleBeforePreparation
                                       && beforePreparation
                                       && (CurrentScreen != CookingGameScreenState.Inventory
                                           || keepRecipeSelectionVisibleDuringInventory);

            SetActive(npcConversationView, showNpcConversation);
            SetActive(recipeSelectionView, showRecipeSelection);
            SetActive(inventoryView, CurrentScreen == CookingGameScreenState.Inventory);

            if (CurrentScreen == CookingGameScreenState.Inventory && inventoryView != null)
                inventoryView.transform.SetAsLastSibling();

            SetActive(preparationView, CurrentScreen == CookingGameScreenState.Preparation);

            if (CurrentScreen == CookingGameScreenState.Preparation && preparationView != null)
                preparationView.transform.SetAsLastSibling();

            SetActive(resultView, CurrentScreen == CookingGameScreenState.Result);

            if (CurrentScreen == CookingGameScreenState.Result && resultView != null)
                resultView.transform.SetAsLastSibling();

            if (rewardView != null)
                rewardView.transform.SetAsLastSibling();
        }

        private void EnsureInventoryView()
        {
            if (inventoryView != null)
            {
                InitializeIngredientSelectionView(inventoryView);
                return;
            }

            CookingIngredientSelectionView existingView = GetComponentInChildren<CookingIngredientSelectionView>(true);
            if (existingView != null)
            {
                inventoryView = existingView.gameObject;
                InitializeIngredientSelectionView(inventoryView);
                return;
            }

            if (autoCreateTemporaryInventoryView == false)
                return;

            Transform parent = FindInventoryViewParent();

            GameObject generatedView = new GameObject(
                "TemporaryIngredientSelectionView",
                typeof(RectTransform),
                typeof(CookingIngredientSelectionView));
            generatedView.transform.SetParent(parent, false);
            generatedView.transform.localRotation = Quaternion.identity;
            generatedView.transform.localScale = Vector3.one;
            inventoryView = generatedView;
            InitializeIngredientSelectionView(inventoryView);
            inventoryView.SetActive(false);
        }

        private Transform FindInventoryViewParent()
        {
            RectTransform recipeRect = recipeSelectionView != null
                ? recipeSelectionView.transform as RectTransform
                : null;

            if (recipeRect != null)
            {
                RectTransform recipeParent = recipeRect.parent as RectTransform;
                if (recipeParent != null)
                {
                    if (recipeParent.GetComponent<LayoutGroup>() != null && recipeParent.parent != null)
                        return recipeParent.parent;

                    return recipeParent;
                }
            }

            RectTransform npcRect = npcConversationView != null
                ? npcConversationView.transform as RectTransform
                : null;

            if (npcRect != null && npcRect.parent != null)
                return npcRect.parent;

            return transform;
        }

        private void InitializeRecipeSelectionView(GameObject view)
        {
            if (view == null)
                return;

            ICookingRecipeSelectionView selectionView = GetViewContract<ICookingRecipeSelectionView>(view);
            if (selectionView != null)
            {
                selectionView.Initialize(this, flowRunner, knowledgeStore);
            }

            CookingRecipeDisplayPanel[] displayPanels = view.GetComponentsInChildren<CookingRecipeDisplayPanel>(true);
            for (int i = 0; i < displayPanels.Length; i++)
            {
                if (displayPanels[i] != null)
                    displayPanels[i].SetGamePanel(this);
            }
        }

        private void InitializeIngredientSelectionView(GameObject view)
        {
            if (view == null)
                return;

            ICookingIngredientSelectionView selectionView = GetViewContract<ICookingIngredientSelectionView>(view);
            if (selectionView == null)
                return;

            selectionView.Initialize(this, flowRunner, temporaryUiFontAsset);
        }

        private void EnsurePreparationView()
        {
            if (preparationView != null)
            {
                InitializePreparationView(preparationView);
                return;
            }

            CookingPreparationView existingView = GetComponentInChildren<CookingPreparationView>(true);
            if (existingView != null)
            {
                preparationView = existingView.gameObject;
                InitializePreparationView(preparationView);
                return;
            }

            if (autoCreateTemporaryPreparationView == false)
                return;

            GameObject generatedView = new GameObject(
                "TemporaryPreparationView",
                typeof(RectTransform),
                typeof(CookingPreparationView));
            generatedView.transform.SetParent(FindInventoryViewParent(), false);
            generatedView.transform.localRotation = Quaternion.identity;
            generatedView.transform.localScale = Vector3.one;
            preparationView = generatedView;
            InitializePreparationView(preparationView);
            preparationView.SetActive(false);
        }

        private void InitializePreparationView(GameObject view)
        {
            if (view == null)
                return;

            ICookingPreparationView preparation = GetViewContract<ICookingPreparationView>(view);
            if (preparation == null)
                return;

            preparation.Initialize(this, flowRunner, temporaryUiFontAsset);
        }

        private void EnsureResultView()
        {
            if (resultView != null)
            {
                InitializeResultView(resultView);
                return;
            }

            CookingResultView existingView = GetComponentInChildren<CookingResultView>(true);
            if (existingView != null)
            {
                resultView = existingView.gameObject;
                InitializeResultView(resultView);
                return;
            }

            if (autoCreateTemporaryResultView == false)
                return;

            GameObject generatedView = new GameObject(
                "TemporaryResultView",
                typeof(RectTransform),
                typeof(CookingResultView));
            generatedView.transform.SetParent(FindInventoryViewParent(), false);
            generatedView.transform.localRotation = Quaternion.identity;
            generatedView.transform.localScale = Vector3.one;
            resultView = generatedView;
            InitializeResultView(resultView);
            resultView.SetActive(false);
        }

        private void InitializeResultView(GameObject view)
        {
            if (view == null)
                return;

            ICookingResultView result = GetViewContract<ICookingResultView>(view);
            if (result == null)
                return;

            result.Initialize(this, flowRunner, temporaryUiFontAsset);
        }

        private void EnsureRewardView()
        {
            if (rewardView != null)
            {
                AttachRewardViewToOverlayRoot(rewardView);
                InitializeRewardView(rewardView);
                return;
            }

            CookingRewardToastView existingView = GetComponentInChildren<CookingRewardToastView>(true);
            if (existingView != null)
            {
                rewardView = existingView.gameObject;
                AttachRewardViewToOverlayRoot(rewardView);
                InitializeRewardView(rewardView);
                return;
            }

            if (autoCreateTemporaryRewardView == false)
                return;

            GameObject generatedView = new GameObject(
                "TemporaryRewardToastView",
                typeof(RectTransform),
                typeof(CookingRewardToastView));
            generatedView.transform.SetParent(FindOverlayViewParent(), false);
            generatedView.transform.localRotation = Quaternion.identity;
            generatedView.transform.localScale = Vector3.one;
            rewardView = generatedView;
            InitializeRewardView(rewardView);
            rewardView.SetActive(true);
        }

        private void AttachRewardViewToOverlayRoot(GameObject view)
        {
            if (view == null)
                return;

            Transform overlayParent = FindOverlayViewParent();
            if (overlayParent == null || view.transform.parent == overlayParent)
                return;

            view.transform.SetParent(overlayParent, false);
            view.transform.localRotation = Quaternion.identity;
            view.transform.localScale = Vector3.one;
        }

        private void InitializeRewardView(GameObject view)
        {
            if (view == null)
                return;

            ICookingRewardView rewardToast = GetViewContract<ICookingRewardView>(view);
            if (rewardToast == null)
                return;

            rewardToast.Initialize(this, rewardWallet, temporaryUiFontAsset);
        }

        private void ApplyTemporaryFontToViews()
        {
            ApplyFontToView(inventoryView);
            ApplyFontToView(preparationView);
            ApplyFontToView(resultView);
            ApplyFontToView(rewardView);
        }

        private void ApplyFontToView(GameObject view)
        {
            if (view == null || temporaryUiFontAsset == null)
                return;

            ICookingIngredientSelectionView ingredientSelection = GetViewContract<ICookingIngredientSelectionView>(view);
            if (ingredientSelection != null)
                ingredientSelection.SetFontAsset(temporaryUiFontAsset);

            ICookingPreparationView preparation = GetViewContract<ICookingPreparationView>(view);
            if (preparation != null)
                preparation.SetFontAsset(temporaryUiFontAsset);

            ICookingResultView result = GetViewContract<ICookingResultView>(view);
            if (result != null)
                result.SetFontAsset(temporaryUiFontAsset);

            ICookingRewardView rewardToast = GetViewContract<ICookingRewardView>(view);
            if (rewardToast != null)
                rewardToast.SetFontAsset(temporaryUiFontAsset);
        }

        private static void RefreshRecipeSelectionView(GameObject view)
        {
            if (view == null)
                return;

            ICookingRecipeSelectionView selectionView = GetViewContract<ICookingRecipeSelectionView>(view);
            if (selectionView != null)
                selectionView.Refresh();
        }

        private static void RefreshIngredientSelectionView(GameObject view)
        {
            if (view == null)
                return;

            ICookingIngredientSelectionView selectionView = GetViewContract<ICookingIngredientSelectionView>(view);
            if (selectionView != null)
                selectionView.Refresh();
        }

        private ICookingIngredientSelectionView GetIngredientSelectionView()
        {
            return GetViewContract<ICookingIngredientSelectionView>(inventoryView);
        }

        private static void RefreshPreparationView(GameObject view)
        {
            if (view == null)
                return;

            ICookingPreparationView preparation = GetViewContract<ICookingPreparationView>(view);
            if (preparation != null)
                preparation.Refresh();
        }

        private static void RefreshResultView(GameObject view)
        {
            if (view == null)
                return;

            ICookingResultView result = GetViewContract<ICookingResultView>(view);
            if (result != null)
                result.Refresh();
        }

        private static T GetViewContract<T>(GameObject view)
            where T : class
        {
            if (view == null)
                return null;

            T contract = view.GetComponent<T>();
            return contract ?? view.GetComponentInChildren<T>(true);
        }

        private Transform FindOverlayViewParent()
        {
            Canvas parentCanvas = FindCanvasFromConnectedViews();
            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>(true);
            if (parentCanvas == null)
                parentCanvas = FindFirstObjectByType<Canvas>();

            if (parentCanvas != null)
                return GetOrCreateOverlayRoot(parentCanvas.rootCanvas != null ? parentCanvas.rootCanvas : parentCanvas);

            return FindInventoryViewParent();
        }

        private Canvas FindCanvasFromConnectedViews()
        {
            Canvas canvas = FindCanvas(npcConversationView);
            if (canvas != null)
                return canvas;

            canvas = FindCanvas(recipeSelectionView);
            if (canvas != null)
                return canvas;

            canvas = FindCanvas(inventoryView);
            if (canvas != null)
                return canvas;

            canvas = FindCanvas(preparationView);
            if (canvas != null)
                return canvas;

            return FindCanvas(resultView);
        }

        private static Canvas FindCanvas(GameObject view)
        {
            return view != null ? view.GetComponentInParent<Canvas>(true) : null;
        }

        private static Transform GetOrCreateOverlayRoot(Canvas canvas)
        {
            const string overlayRootName = "CookingRewardOverlayRoot";

            Transform canvasTransform = canvas.transform;
            Transform existing = canvasTransform.Find(overlayRootName);
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return existing;
            }

            GameObject rootObject = new GameObject(overlayRootName, typeof(RectTransform));
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(canvasTransform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localRotation = Quaternion.identity;
            rootRect.localScale = Vector3.one;
            rootRect.SetAsLastSibling();
            return rootRect;
        }

        private static bool IsBeforePreparation(CookingGameScreenState screen)
        {
            return screen == CookingGameScreenState.NpcConversation
                   || screen == CookingGameScreenState.RecipeSelection
                   || screen == CookingGameScreenState.Inventory;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
