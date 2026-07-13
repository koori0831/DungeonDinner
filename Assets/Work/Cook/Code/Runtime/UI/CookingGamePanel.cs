using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;
using Work.NPC.Code.Runtime;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
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
        [SerializeField] private bool autoOpenInventoryWhenNpcReady = true;
        [SerializeField] private bool keepNpcConversationVisibleBeforePreparation = true;
        [SerializeField] private bool keepNpcConversationVisibleDuringCooking = true;
        [SerializeField] private bool keepRecipeSelectionVisibleBeforePreparation = true;
        [SerializeField] private bool keepRecipeSelectionVisibleDuringInventory = true;
        [SerializeField] private bool allowRecipeConfirmation;
        [SerializeField] private TMP_FontAsset temporaryUiFontAsset;

        [Header("Rewards")]
        [SerializeField] private CookingRewardWallet rewardWallet;
        [SerializeField] private CookingRewardCalculator rewardCalculator;
        [SerializeField] private bool autoCreateRewardSystems = true;

        [Header("Business Flow")]
        [SerializeField] private CookingBusinessFlowController businessFlowController;

        [Header("Preparation Visuals")]
        [SerializeField] private CookingPreparationVisualDirector preparationVisualDirector;

        [Header("Mini Game")]
        [SerializeField] private GameObject miniGameView;

        [Header("Views")]
        [SerializeField] private GameObject npcConversationView;
        [SerializeField] private GameObject recipeSelectionView;
        [SerializeField] private GameObject inventoryView;
        [SerializeField] private GameObject preparationView;
        [SerializeField] private GameObject resultView;
        [SerializeField] private GameObject knowledgeUpdateView;
        [SerializeField] private GameObject rewardView;

        private DishResult _currentResult;
        private CookingSession _consumedIngredientSession;
        private NpcConversationRunner _subscribedNpcRunner;
        private CookingKnowledgeStore _subscribedKnowledgeStore;
        private CookingRewardWallet _subscribedRewardWallet;
        private readonly List<GameObject> _preparationHiddenViews = new List<GameObject>();
        private bool _isPreparationViewIsolated;
        private bool _isCompletingPreparationVisualSequence;
        private bool _isResultHandBlockedByPreparationVisual;
        private bool _isMiniGameActive;
        private IngredientSO _pendingMiniGameIngredient;
        private IngredientPreparationOption _pendingMiniGameOption;

        private enum MiniGameStartStatus
        {
            NotRequired,
            Started,
            Unavailable
        }

        public CookingFlowRunner FlowRunner => flowRunner;
        public NpcConversationRunner NpcRunner => npcRunner;
        public TMP_FontAsset TemporaryUiFontAsset => temporaryUiFontAsset;
        public GameObject NpcConversationView => npcConversationView;
        public GameObject RecipeSelectionView => recipeSelectionView;
        public GameObject InventoryView => inventoryView;
        public GameObject PreparationView => preparationView;
        public GameObject MiniGameView => miniGameView;
        public GameObject ResultView => resultView;
        public GameObject KnowledgeUpdateView => knowledgeUpdateView;
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
        public bool AllowRecipeConfirmation => allowRecipeConfirmation;

        private void Awake()
        {
            EnsureReferences();

            if (applyInitialScreenOnAwake == true)
                SetScreen(initialScreen);
            else
            {
                ApplyViewActiveStates();
                PublishSnapshotChanged();
            }
        }

        private void OnEnable()
        {
            SubscribeBusRequests();
        }

        private void OnDisable()
        {
            UnsubscribeBusRequests();
        }

        private void OnDestroy()
        {
            UnsubscribeStateSources();
        }

        public void SetFlowRunner(CookingFlowRunner value)
        {
            flowRunner = value;
            ResetConsumedIngredientSession();
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

        public void SetMiniGameView(GameObject value)
        {
            miniGameView = value;
            ReinitializeCookingViews();
        }

        public void SetResultView(GameObject value)
        {
            resultView = value;
            ReinitializeCookingViews();
        }

        public void SetKnowledgeUpdateView(GameObject value)
        {
            knowledgeUpdateView = value;
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
            InitializeMiniGameView(miniGameView);
            InitializeResultView(resultView);
            InitializeKnowledgeUpdateView(knowledgeUpdateView);
            InitializeRewardView(rewardView);
            ApplyViewActiveStates();
            RefreshCookingViews();
            PublishSnapshotChanged();
        }

        public void RefreshCookingViews()
        {
            if (CurrentScreen == CookingGameScreenState.RecipeSelection
                || CurrentScreen == CookingGameScreenState.NpcConversation
                || CurrentScreen == CookingGameScreenState.None)
            {
                RefreshRecipeSelectionView(recipeSelectionView);
            }

            if (CurrentScreen == CookingGameScreenState.Inventory)
                RefreshIngredientSelectionView(inventoryView);

            if (IsPreparationStageScreen(CurrentScreen) == true)
            {
                RefreshPreparationView(preparationView);
            }

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

        public void ClearStoredInfoForDebug()
        {
            if (flowRunner == null)
                flowRunner = GetComponentInChildren<CookingFlowRunner>(true);

            if (knowledgeStore == null)
                knowledgeStore = GetComponentInChildren<CookingKnowledgeStore>(true);

            if (rewardWallet == null)
                rewardWallet = GetComponentInChildren<CookingRewardWallet>(true);

            if (recipeIngredientChoiceSource == null)
                recipeIngredientChoiceSource = GetComponentInChildren<CookingRecipeIngredientChoiceSource>(true);

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            NpcEncounterDirector encounterDirector = GetComponentInChildren<NpcEncounterDirector>(true);
            if (encounterDirector == null)
                encounterDirector = FindFirstObjectByType<NpcEncounterDirector>();

            flowRunner?.ResetFlow();
            recipeIngredientChoiceSource?.Clear();
            knowledgeStore?.ClearKnowledgeForDebug();
            rewardWallet?.ClearForDebug();
            encounterDirector?.ClearEncounterHistory();
            _currentResult = null;
            ResetConsumedIngredientSession();

            CookingGameScreenState resetScreen = applyInitialScreenOnAwake == true
                ? initialScreen
                : CookingGameScreenState.None;
            SetScreen(resetScreen);
            RefreshCookingViews();
            PublishSnapshotChanged();
        }

        public void OpenRecipeSelection()
        {
            EnsureReferences();

            if (resetFlowWhenOpeningRecipeSelection == true && flowRunner != null)
                flowRunner.ResetFlow();

            _currentResult = null;
            ResetConsumedIngredientSession();
            SetScreen(CookingGameScreenState.RecipeSelection);
        }

        public bool ConfirmRecipe(RecipeSO recipe)
        {
            EnsureReferences();

            if (allowRecipeConfirmation == false)
            {
                Debug.LogWarning("Recipe confirmation is disabled. Use direct ingredient selection for cooking.", this);
                return false;
            }

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can confirm a recipe.", this);
                return false;
            }

            ResetConsumedIngredientSession();

            if (TryBeginRecipeWithIngredientChoices(recipe) == true)
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

            ResetConsumedIngredientSession();
            SetIngredientSelectionSource(null);
            SetIngredientSelectionLimits(1, 0);
            _currentResult = null;
            SetScreen(CookingGameScreenState.Inventory);
            return true;
        }

        public bool BeginCookingAfterConversation()
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

            knowledgeStore?.LearnSelectedIngredients(flowRunner.SelectedIngredients);
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

            if (_isCompletingPreparationVisualSequence == true)
            {
                return false;
            }

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

            MiniGameStartStatus miniGameStatus = TryStartMiniGame(ingredient, option);
            if (miniGameStatus == MiniGameStartStatus.Started)
                return true;
            if (miniGameStatus == MiniGameStartStatus.Unavailable)
            {
                RecoverFromUnavailableMiniGame();
                return false;
            }

            return ApplyPreparationResult(ingredient, option, null);
        }

        /// <summary>
        /// 조리 뷰의 직접 상호작용 완료 후 미니게임 또는 손질 결과 반영 진행
        /// </summary>
        /// <param name="ingredient">손질 대상 재료</param>
        /// <param name="option">적용할 손질 옵션</param>
        /// <param name="miniGameResult">선택적으로 함께 저장할 미니게임 결과</param>
        /// <returns>손질 결과 반영 성공 여부</returns>
        public bool CompletePreparationInteraction(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            CookingMiniGameResult miniGameResult)
        {
            EnsureReferences();

            if (_isCompletingPreparationVisualSequence == true)
            {
                return false;
            }

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can complete a preparation interaction.", this);
                return false;
            }

            if (ingredient == null)
            {
                Debug.LogWarning("CookingGamePanel could not complete a preparation interaction because the ingredient is missing.", this);
                return false;
            }

            if (miniGameResult == null)
            {
                MiniGameStartStatus miniGameStatus = TryStartMiniGame(ingredient, option);
                if (miniGameStatus == MiniGameStartStatus.Started)
                    return true;
                if (miniGameStatus == MiniGameStartStatus.Unavailable)
                {
                    RecoverFromUnavailableMiniGame();
                    return false;
                }
            }

            return ApplyPreparationResult(ingredient, option, miniGameResult);
        }

        public bool CompleteCooking()
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                Debug.LogWarning("CookingGamePanel needs a CookingFlowRunner before it can complete cooking.", this);
                return false;
            }

            if (flowRunner.Controller.CanCompleteCooking() == false)
            {
                Debug.LogWarning("CookingGamePanel could not complete cooking. Make sure every selected ingredient is prepared.", this);
                return false;
            }

            if (TryConsumeSelectedIngredientsForCompletion(flowRunner.Controller.CurrentSession) == false)
                return false;

            if (flowRunner.TryCompleteCooking(out DishResult result) == false)
            {
                Debug.LogWarning("CookingGamePanel could not complete cooking after ingredients were consumed.", this);
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
            Bus<CookingDishResultReadyEvent>.Raise(new CookingDishResultReadyEvent(this, result));
            return true;
        }

        public DishResult GetCurrentDishResult()
        {
            EnsureCoreReferences();
            return _currentResult ?? flowRunner?.LastResult;
        }

        public bool CanHandCurrentResultToNpc()
        {
            return GetCurrentDishResult() != null
                   && NpcRunner != null
                   && _isResultHandBlockedByPreparationVisual == false;
        }

        public bool TryBuildNpcMatchReport(DishResult result, out NpcDishMatchReport matchReport)
        {
            EnsureCoreReferences();
            return CookingNpcDishAdapter.TryBuildMatchReport(npcRunner, result, out matchReport);
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

            if (CanHandCurrentResultToNpc() == false)
            {
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

            Bus<CookingDishHandedToNpcEvent>.Raise(new CookingDishHandedToNpcEvent(this, result));
            preparationVisualDirector?.PlayDishDismissSequence();
            GrantReward(result, matchReport);

            if (resetFlowAfterHandingDish == true && flowRunner != null)
                flowRunner.ResetFlow();

            return true;
        }

        public bool AdvanceFromResult()
        {
            EnsureReferences();

            ICookingKnowledgeUpdateView updateView = GetViewContract<ICookingKnowledgeUpdateView>(knowledgeUpdateView);
            if (updateView != null && knowledgeStore != null && knowledgeStore.PendingKnowledgeUpdateCount > 0)
            {
                if (updateView.ShowPendingUpdates(() => HandResultToNpc()) == true)
                    return true;
            }

            return HandResultToNpc();
        }

        public void ReturnToNpcConversation()
        {
            SetScreen(CookingGameScreenState.NpcConversation);
        }

        public void CloseCookingViews()
        {
            SetScreen(CookingGameScreenState.None);
        }

        private bool TryBeginRecipeWithIngredientChoices(RecipeSO recipe)
        {
            if (recipe == null || flowRunner == null)
                return false;

            if (CookingRecipeIngredientChoicePlanner.TryBuild(
                    recipe,
                    flowRunner.Ingredients,
                    out CookingRecipeIngredientChoicePlan plan) == false)
            {
                return false;
            }

            if (flowRunner.BeginRecipeIngredientSelection(recipe) == false)
                return false;

            for (int i = 0; i < plan.FixedIngredients.Count; i++)
                flowRunner.AddRecipeIngredient(plan.FixedIngredients[i]);

            EnsureRecipeIngredientChoiceSource();
            if (recipeIngredientChoiceSource == null)
                return false;

            recipeIngredientChoiceSource.SetCandidates(plan.ChoiceCandidates);
            SetIngredientSelectionSource(recipeIngredientChoiceSource);
            SetIngredientSelectionLimits(
                plan.FixedIngredients.Count + plan.MinChoiceCount,
                plan.MaxChoiceCount > 0 ? plan.FixedIngredients.Count + plan.MaxChoiceCount : 0);

            _currentResult = null;
            SetScreen(CookingGameScreenState.Inventory);
            return true;
        }

        private MiniGameStartStatus TryStartMiniGame(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (option == null || option.MiniGameType == CookingMiniGameType.None)
                return MiniGameStartStatus.NotRequired;

            ICookingMiniGameView miniGame = GetMiniGameView();
            if (miniGame == null || miniGame.CanPlay(option.MiniGameType) == false)
            {
                Debug.LogError(
                    $"CookingGamePanel requires a compatible mini game view. type={option.MiniGameType}, viewMissing={miniGame == null}",
                    this);
                return MiniGameStartStatus.Unavailable;
            }

            _pendingMiniGameIngredient = ingredient;
            _pendingMiniGameOption = option;
            _isMiniGameActive = true;
            if (miniGame.StartMiniGame(ingredient, option, HandleMiniGameCompleted) == false)
            {
                ClearPendingMiniGame();
                Debug.LogError($"CookingGamePanel failed to start mini game. type={option.MiniGameType}", this);
                return MiniGameStartStatus.Unavailable;
            }

            SetScreen(CookingGameScreenState.MiniGame);
            return MiniGameStartStatus.Started;
        }

        private void HandleMiniGameCompleted(CookingMiniGameResult result)
        {
            if (_isMiniGameActive == false)
                return;

            IngredientSO ingredient = _pendingMiniGameIngredient;
            IngredientPreparationOption option = _pendingMiniGameOption;

            ClearPendingMiniGame();

            if (result == null || option == null || result.MiniGameType != option.MiniGameType)
            {
                Debug.LogError("CookingGamePanel rejected an invalid mini game result.", this);
                SetScreen(CookingGameScreenState.Preparation);
                RefreshPreparationView(preparationView);
                return;
            }

            ApplyPreparationResult(ingredient, option, result);
        }

        /// <summary>
        /// 진행 중인 미니게임을 취소하고 현재 재료 손질 선택으로 복귀
        /// </summary>
        public void CancelActiveMiniGame()
        {
            if (_isMiniGameActive == false)
                return;

            GetMiniGameView()?.CancelMiniGame();
            ClearPendingMiniGame();
            SetScreen(CookingGameScreenState.Preparation);
            RefreshPreparationView(preparationView);
        }

        private void ClearPendingMiniGame()
        {
            _pendingMiniGameIngredient = null;
            _pendingMiniGameOption = null;
            _isMiniGameActive = false;
        }

        private void RecoverFromUnavailableMiniGame()
        {
            if (CurrentScreen != CookingGameScreenState.Preparation)
                SetScreen(CookingGameScreenState.Preparation);

            RefreshPreparationView(preparationView);
            PublishSnapshotChanged();
        }

        private bool ApplyPreparationResult(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            CookingMiniGameResult miniGameResult)
        {
            if (option != null)
                knowledgeStore?.LearnPreparationEffect(ingredient, option);

            if (flowRunner.SelectPreparation(ingredient, option, miniGameResult) == false)
            {
                Debug.LogWarning("CookingGamePanel could not apply the selected preparation.", this);
                return false;
            }

            if (CurrentScreen == CookingGameScreenState.MiniGame)
                SetScreen(CookingGameScreenState.Preparation);

            if (flowRunner.GetNextUnpreparedIngredient() == null)
            {
                if (preparationVisualDirector != null)
                {
                    _isCompletingPreparationVisualSequence = true;
                    _isResultHandBlockedByPreparationVisual = true;
                    if (preparationVisualDirector.PlayCompletionSequence(
                            ingredient,
                            CompleteCookingAfterPreparationDishReplacement,
                            EnableResultHandAfterPreparationVisualSequence) == true)
                    {
                        return true;
                    }

                    _isCompletingPreparationVisualSequence = false;
                    _isResultHandBlockedByPreparationVisual = false;
                    preparationVisualDirector.SpawnPreparedIngredient(ingredient);
                }

                return CompleteCooking();
            }

            preparationVisualDirector?.SpawnPreparedIngredient(ingredient);

            RefreshPreparationView(preparationView);
            PublishSnapshotChanged();
            return true;
        }

        private void CompleteCookingAfterPreparationDishReplacement()
        {
            CompleteCooking();
        }

        private void EnableResultHandAfterPreparationVisualSequence()
        {
            _isCompletingPreparationVisualSequence = false;
            _isResultHandBlockedByPreparationVisual = false;
            PublishSnapshotChanged();
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
            EnsurePreparationVisualDirector();
            SubscribeStateSources();
        }

        private void EnsurePreparationVisualDirector()
        {
            if (preparationVisualDirector == null)
                preparationVisualDirector = GetComponentInChildren<CookingPreparationVisualDirector>(true);

            if (preparationVisualDirector == null)
                preparationVisualDirector = GetComponent<CookingPreparationVisualDirector>();
        }

        private void EnsureRecipeIngredientChoiceSource()
        {
            if (recipeIngredientChoiceSource == null)
                recipeIngredientChoiceSource = GetComponentInChildren<CookingRecipeIngredientChoiceSource>(true);

            if (recipeIngredientChoiceSource == null)
                recipeIngredientChoiceSource = GetComponent<CookingRecipeIngredientChoiceSource>();

            if (recipeIngredientChoiceSource == null)
                LogMissingViewReference(nameof(recipeIngredientChoiceSource), nameof(CookingRecipeIngredientChoiceSource));
        }

        private void EnsureReferences()
        {
            EnsureCoreReferences();
            InitializeRecipeSelectionView(recipeSelectionView);
            EnsureInventoryView();
            EnsurePreparationView();
            EnsureMiniGameView();
            EnsureResultView();
            EnsureKnowledgeUpdateView();
            EnsureRewardView();
            EnsureBusinessFlowController();
        }

        private void EnsureKnowledgeStore()
        {
            if (knowledgeStore == null)
                knowledgeStore = GetComponentInChildren<CookingKnowledgeStore>(true);

            if (knowledgeStore == null)
                knowledgeStore = GetComponent<CookingKnowledgeStore>();

            if (knowledgeStore == null)
            {
                LogMissingViewReference(nameof(knowledgeStore), nameof(CookingKnowledgeStore));
                return;
            }

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
                LogMissingViewReference(nameof(rewardWallet), nameof(CookingRewardWallet));
            else
                rewardWallet.Initialize();

            if (rewardCalculator == null)
                rewardCalculator = GetComponentInChildren<CookingRewardCalculator>(true);

            if (rewardCalculator == null)
                rewardCalculator = GetComponent<CookingRewardCalculator>();

            if (rewardCalculator == null)
                LogMissingViewReference(nameof(rewardCalculator), nameof(CookingRewardCalculator));
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

            Bus<CookingRewardGrantedEvent>.Raise(new CookingRewardGrantedEvent(this, grant));

            Debug.Log($"Cooking reward resolved: {grant.BuildDebugSummary()}", this);
            PublishSnapshotChanged();
            return grant;
        }

        private bool TryConsumeSelectedIngredientsForCompletion(CookingSession session)
        {
            if (session == null)
                return false;

            if (_consumedIngredientSession == session)
                return true;

            ICookingIngredientConsumer consumer = GetCurrentIngredientConsumer();
            if (consumer == null)
                return true;

            if (consumer.TryConsumeIngredients(session.SelectedIngredients, this, flowRunner, out string reason) == false)
            {
                Debug.LogWarning($"CookingGamePanel could not consume selected ingredients. reason={reason}", this);
                RefreshIngredientSelectionView(inventoryView);
                PublishSnapshotChanged();
                return false;
            }

            _consumedIngredientSession = session;
            RefreshIngredientSelectionView(inventoryView);
            PublishSnapshotChanged();
            return true;
        }

        private ICookingIngredientConsumer GetCurrentIngredientConsumer()
        {
            ICookingIngredientSelectionView selectionView = GetIngredientSelectionView();
            if (selectionView == null)
                return null;

            ICookingIngredientSource source = selectionView.GetCurrentIngredientSource();
            return source as ICookingIngredientConsumer;
        }

        private void ResetConsumedIngredientSession()
        {
            _consumedIngredientSession = null;
        }

        private void SetScreen(CookingGameScreenState screen)
        {
            CurrentScreen = screen;
            ApplyViewActiveStates();
            Bus<CookingGameScreenChangedEvent>.Raise(new CookingGameScreenChangedEvent(this, CurrentScreen));
            PublishSnapshotChanged();
        }

        private void ApplyViewActiveStates()
        {
            if (IsPreparationStageScreen(CurrentScreen) == true)
            {
                ApplyPreparationStageViewActiveStates();
                return;
            }

            RestorePreparationHiddenViews();

            bool beforePreparation = IsBeforePreparation(CurrentScreen);
            bool duringIngredientSelection = CurrentScreen == CookingGameScreenState.Inventory;
            bool showNpcConversation = CurrentScreen == CookingGameScreenState.NpcConversation
                                        || keepNpcConversationVisibleBeforePreparation == true && beforePreparation == true
                                        || keepNpcConversationVisibleDuringCooking == true && duringIngredientSelection == true;
            bool showRecipeSelection = CurrentScreen == CookingGameScreenState.RecipeSelection
                                        || CurrentScreen == CookingGameScreenState.Inventory
                                        || keepRecipeSelectionVisibleBeforePreparation == true
                                        && beforePreparation == true
                                        && (CurrentScreen != CookingGameScreenState.Inventory
                                            || keepRecipeSelectionVisibleDuringInventory == true);

            SetActive(npcConversationView, showNpcConversation);
            SetActive(recipeSelectionView, showRecipeSelection);
            SetActive(inventoryView, CurrentScreen == CookingGameScreenState.Inventory);

            SetActive(preparationView, CurrentScreen == CookingGameScreenState.Preparation);
            SetActive(miniGameView, false);

            SetActive(resultView, CurrentScreen == CookingGameScreenState.Result);
        }

        private void ApplyPreparationStageViewActiveStates()
        {
            HideForPreparation(npcConversationView);
            HideForPreparation(recipeSelectionView);
            HideForPreparation(inventoryView);
            HideForPreparation(resultView);
            HideForPreparation(knowledgeUpdateView);
            HideForPreparation(rewardView);
            HideDictionaryPanelsForPreparation();

            bool showMiniGame = CurrentScreen == CookingGameScreenState.MiniGame;
            SetActive(preparationView, true);
            SetActive(miniGameView, showMiniGame);

            if (showMiniGame == true)
                BringMiniGameViewToFront();

            _isPreparationViewIsolated = true;
        }

        private void BringMiniGameViewToFront()
        {
            if (miniGameView == null || miniGameView.transform.parent == null)
                return;

            miniGameView.transform.SetAsLastSibling();
        }

        private void HideDictionaryPanelsForPreparation()
        {
            Canvas canvas = FindCanvasFromConnectedViews();
            InfoDictionaryPanel[] panels = canvas != null
                ? canvas.GetComponentsInChildren<InfoDictionaryPanel>(true)
                : FindObjectsByType<InfoDictionaryPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    HideForPreparation(panels[i].gameObject);
                }
            }
        }

        private void HideForPreparation(GameObject target)
        {
            if (target == null || target == preparationView)
            {
                return;
            }

            if (target.activeSelf == true && _preparationHiddenViews.Contains(target) == false)
            {
                _preparationHiddenViews.Add(target);
            }

            SetActive(target, false);
        }

        private void RestorePreparationHiddenViews()
        {
            if (_isPreparationViewIsolated == false && _preparationHiddenViews.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _preparationHiddenViews.Count; i++)
            {
                SetActive(_preparationHiddenViews[i], true);
            }

            _preparationHiddenViews.Clear();
            _isPreparationViewIsolated = false;
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

            LogMissingViewReference(nameof(inventoryView), nameof(CookingIngredientSelectionView));
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

            selectionView.Initialize(this, flowRunner);
        }

        private void EnsurePreparationView()
        {
            if (preparationView != null)
            {
                AttachPreparationViewToOverlayRoot(preparationView);
                InitializePreparationView(preparationView);
                return;
            }

            CookingView existingCookingView = GetComponentInChildren<CookingView>(true);
            if (existingCookingView != null)
            {
                preparationView = existingCookingView.gameObject;
                AttachPreparationViewToOverlayRoot(preparationView);
                InitializePreparationView(preparationView);
                return;
            }

            CookingPreparationView existingView = GetComponentInChildren<CookingPreparationView>(true);
            if (existingView != null)
            {
                preparationView = existingView.gameObject;
                AttachPreparationViewToOverlayRoot(preparationView);
                InitializePreparationView(preparationView);
                return;
            }

            LogMissingViewReference(nameof(preparationView), nameof(CookingPreparationView));
        }

        private void AttachPreparationViewToOverlayRoot(GameObject view)
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

        private void InitializePreparationView(GameObject view)
        {
            if (view == null)
                return;

            ICookingPreparationView preparation = GetViewContract<ICookingPreparationView>(view);
            if (preparation == null)
                return;

            preparation.Initialize(this, flowRunner, temporaryUiFontAsset);
        }

        private void EnsureMiniGameView()
        {
            if (miniGameView != null)
            {
                AttachMiniGameViewToOverlayRoot(miniGameView);
                InitializeMiniGameView(miniGameView);
                return;
            }

            LogMissingViewReference(nameof(miniGameView), nameof(ICookingMiniGameView));
        }

        private void AttachMiniGameViewToOverlayRoot(GameObject view)
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

        private void InitializeMiniGameView(GameObject view)
        {
            if (view == null)
                return;

            ICookingMiniGameView miniGame = GetViewContract<ICookingMiniGameView>(view);
            if (miniGame == null)
                return;

            miniGame.Initialize(this, flowRunner, temporaryUiFontAsset);
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

            LogMissingViewReference(nameof(resultView), nameof(CookingResultView));
        }

        private void EnsureKnowledgeUpdateView()
        {
            if (knowledgeUpdateView != null)
            {
                AttachKnowledgeUpdateViewToOverlayRoot(knowledgeUpdateView);
                InitializeKnowledgeUpdateView(knowledgeUpdateView);
                return;
            }

            CookingKnowledgeUpdateView existingView = GetComponentInChildren<CookingKnowledgeUpdateView>(true);
            if (existingView != null)
            {
                knowledgeUpdateView = existingView.gameObject;
                AttachKnowledgeUpdateViewToOverlayRoot(knowledgeUpdateView);
                InitializeKnowledgeUpdateView(knowledgeUpdateView);
                return;
            }

            LogMissingViewReference(nameof(knowledgeUpdateView), nameof(CookingKnowledgeUpdateView));
        }

        private void InitializeResultView(GameObject view)
        {
            if (view == null)
                return;

            ICookingResultView result = GetViewContract<ICookingResultView>(view);
            if (result == null)
                return;

            result.Initialize(this, flowRunner);
        }

        private void AttachKnowledgeUpdateViewToOverlayRoot(GameObject view)
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

        private void InitializeKnowledgeUpdateView(GameObject view)
        {
            if (view == null)
                return;

            ICookingKnowledgeUpdateView updateView = GetViewContract<ICookingKnowledgeUpdateView>(view);
            if (updateView == null)
                return;

            updateView.Initialize(this, knowledgeStore);
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

            LogMissingViewReference(nameof(rewardView), nameof(CookingRewardToastView));
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

            rewardToast.Initialize(this, rewardWallet);
        }

        private void EnsureBusinessFlowController()
        {
            if (businessFlowController != null)
                return;

            businessFlowController = GetComponentInChildren<CookingBusinessFlowController>(true);
            if (businessFlowController != null)
            {
                businessFlowController.Initialize(this, null);
                return;
            }

            LogMissingViewReference(nameof(businessFlowController), nameof(CookingBusinessFlowController));
        }

        private void LogMissingViewReference(string fieldName, string componentName)
        {
            Debug.LogError($"CookingGamePanel missing {fieldName}. Assign a prefab/scene object with {componentName} in the inspector.", this);
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
                return ResolveOverlayRoot(parentCanvas.rootCanvas != null ? parentCanvas.rootCanvas : parentCanvas);

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

            canvas = FindCanvas(miniGameView);
            if (canvas != null)
                return canvas;

            canvas = FindCanvas(resultView);
            if (canvas != null)
                return canvas;

            return FindCanvas(knowledgeUpdateView);
        }

        private static Canvas FindCanvas(GameObject view)
        {
            return view != null ? view.GetComponentInParent<Canvas>(true) : null;
        }

        private static Transform ResolveOverlayRoot(Canvas canvas)
        {
            const string OVERLAY_ROOT_NAME = "CookingRewardOverlayRoot";

            Transform canvasTransform = canvas.transform;
            Transform existing = canvasTransform.Find(OVERLAY_ROOT_NAME);
            if (existing != null)
            {
                return existing;
            }

            return canvasTransform;
        }

        private static bool IsBeforePreparation(CookingGameScreenState screen)
        {
            return screen == CookingGameScreenState.NpcConversation
                   || screen == CookingGameScreenState.RecipeSelection
                   || screen == CookingGameScreenState.Inventory;
        }

        private static bool IsPreparationStageScreen(CookingGameScreenState screen)
        {
            return screen == CookingGameScreenState.Preparation
                   || screen == CookingGameScreenState.MiniGame;
        }

        private ICookingMiniGameView GetMiniGameView()
        {
            return GetViewContract<ICookingMiniGameView>(miniGameView);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private void SubscribeBusRequests()
        {
            Bus<CookingRecipeSelectionOpenRequestedEvent>.Events -= HandleRecipeSelectionOpenRequested;
            Bus<CookingRecipeSelectionOpenRequestedEvent>.Events += HandleRecipeSelectionOpenRequested;
            Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Events -= HandleDirectIngredientSelectionOpenRequested;
            Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Events += HandleDirectIngredientSelectionOpenRequested;
            Bus<CookingIngredientSelectionConfirmRequestedEvent>.Events -= HandleIngredientSelectionConfirmRequested;
            Bus<CookingIngredientSelectionConfirmRequestedEvent>.Events += HandleIngredientSelectionConfirmRequested;
            Bus<CookingIngredientSelectionClearRequestedEvent>.Events -= HandleIngredientSelectionClearRequested;
            Bus<CookingIngredientSelectionClearRequestedEvent>.Events += HandleIngredientSelectionClearRequested;
            Bus<CookingRecipeConfirmRequestedEvent>.Events -= HandleRecipeConfirmRequested;
            Bus<CookingRecipeConfirmRequestedEvent>.Events += HandleRecipeConfirmRequested;
            Bus<CookingIngredientSelectionToggleRequestedEvent>.Events -= HandleIngredientSelectionToggleRequested;
            Bus<CookingIngredientSelectionToggleRequestedEvent>.Events += HandleIngredientSelectionToggleRequested;
            Bus<CookingIngredientSelectionRemoveRequestedEvent>.Events -= HandleIngredientSelectionRemoveRequested;
            Bus<CookingIngredientSelectionRemoveRequestedEvent>.Events += HandleIngredientSelectionRemoveRequested;
            Bus<CookingIngredientSearchQueryChangeRequestedEvent>.Events -= HandleIngredientSearchQueryChangeRequested;
            Bus<CookingIngredientSearchQueryChangeRequestedEvent>.Events += HandleIngredientSearchQueryChangeRequested;
            Bus<CookingPreparationSelectCurrentByIndexRequestedEvent>.Events -= HandlePreparationSelectCurrentByIndexRequested;
            Bus<CookingPreparationSelectCurrentByIndexRequestedEvent>.Events += HandlePreparationSelectCurrentByIndexRequested;
            Bus<CookingPreparationSelectCurrentRequestedEvent>.Events -= HandlePreparationSelectCurrentRequested;
            Bus<CookingPreparationSelectCurrentRequestedEvent>.Events += HandlePreparationSelectCurrentRequested;
            Bus<CookingPreparationSelectRequestedEvent>.Events -= HandlePreparationSelectRequested;
            Bus<CookingPreparationSelectRequestedEvent>.Events += HandlePreparationSelectRequested;
            Bus<CookingPreparationInteractionCompleteRequestedEvent>.Events -= HandlePreparationInteractionCompleteRequested;
            Bus<CookingPreparationInteractionCompleteRequestedEvent>.Events += HandlePreparationInteractionCompleteRequested;
            Bus<CookingCompleteRequestedEvent>.Events -= HandleCookingCompleteRequested;
            Bus<CookingCompleteRequestedEvent>.Events += HandleCookingCompleteRequested;
            Bus<CookingResultAdvanceRequestedEvent>.Events -= HandleResultAdvanceRequested;
            Bus<CookingResultAdvanceRequestedEvent>.Events += HandleResultAdvanceRequested;
            Bus<CookingDishHandToNpcRequestedEvent>.Events -= HandleDishHandToNpcRequested;
            Bus<CookingDishHandToNpcRequestedEvent>.Events += HandleDishHandToNpcRequested;
            Bus<CookingNpcConversationReturnRequestedEvent>.Events -= HandleNpcConversationReturnRequested;
            Bus<CookingNpcConversationReturnRequestedEvent>.Events += HandleNpcConversationReturnRequested;
            Bus<CookingViewsCloseRequestedEvent>.Events -= HandleViewsCloseRequested;
            Bus<CookingViewsCloseRequestedEvent>.Events += HandleViewsCloseRequested;
            Bus<CookingViewsRefreshRequestedEvent>.Events -= HandleViewsRefreshRequested;
            Bus<CookingViewsRefreshRequestedEvent>.Events += HandleViewsRefreshRequested;
            Bus<CookingPreparationOpenRequestedEvent>.Events -= HandlePreparationOpenRequested;
            Bus<CookingPreparationOpenRequestedEvent>.Events += HandlePreparationOpenRequested;
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChangedEvent;
            Bus<CookingFlowStateChangedEvent>.Events += HandleFlowStateChangedEvent;
        }

        private void UnsubscribeBusRequests()
        {
            Bus<CookingRecipeSelectionOpenRequestedEvent>.Events -= HandleRecipeSelectionOpenRequested;
            Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Events -= HandleDirectIngredientSelectionOpenRequested;
            Bus<CookingIngredientSelectionConfirmRequestedEvent>.Events -= HandleIngredientSelectionConfirmRequested;
            Bus<CookingIngredientSelectionClearRequestedEvent>.Events -= HandleIngredientSelectionClearRequested;
            Bus<CookingRecipeConfirmRequestedEvent>.Events -= HandleRecipeConfirmRequested;
            Bus<CookingIngredientSelectionToggleRequestedEvent>.Events -= HandleIngredientSelectionToggleRequested;
            Bus<CookingIngredientSelectionRemoveRequestedEvent>.Events -= HandleIngredientSelectionRemoveRequested;
            Bus<CookingIngredientSearchQueryChangeRequestedEvent>.Events -= HandleIngredientSearchQueryChangeRequested;
            Bus<CookingPreparationSelectCurrentByIndexRequestedEvent>.Events -= HandlePreparationSelectCurrentByIndexRequested;
            Bus<CookingPreparationSelectCurrentRequestedEvent>.Events -= HandlePreparationSelectCurrentRequested;
            Bus<CookingPreparationSelectRequestedEvent>.Events -= HandlePreparationSelectRequested;
            Bus<CookingPreparationInteractionCompleteRequestedEvent>.Events -= HandlePreparationInteractionCompleteRequested;
            Bus<CookingCompleteRequestedEvent>.Events -= HandleCookingCompleteRequested;
            Bus<CookingResultAdvanceRequestedEvent>.Events -= HandleResultAdvanceRequested;
            Bus<CookingDishHandToNpcRequestedEvent>.Events -= HandleDishHandToNpcRequested;
            Bus<CookingNpcConversationReturnRequestedEvent>.Events -= HandleNpcConversationReturnRequested;
            Bus<CookingViewsCloseRequestedEvent>.Events -= HandleViewsCloseRequested;
            Bus<CookingViewsRefreshRequestedEvent>.Events -= HandleViewsRefreshRequested;
            Bus<CookingPreparationOpenRequestedEvent>.Events -= HandlePreparationOpenRequested;
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChangedEvent;
        }

        private bool IsRequestForThis(CookingGamePanel source)
        {
            return source == this;
        }

        private void HandleRecipeSelectionOpenRequested(CookingRecipeSelectionOpenRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                OpenRecipeSelection();
        }

        private void HandleDirectIngredientSelectionOpenRequested(CookingDirectIngredientSelectionOpenRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                OpenDirectIngredientSelection();
        }

        private void HandleIngredientSelectionConfirmRequested(CookingIngredientSelectionConfirmRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                ConfirmIngredientSelection();
        }

        private void HandleIngredientSelectionClearRequested(CookingIngredientSelectionClearRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                ClearIngredientSelection();
        }

        private void HandleRecipeConfirmRequested(CookingRecipeConfirmRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                ConfirmRecipe(gameEvent.Recipe);
        }

        private void HandleIngredientSelectionToggleRequested(CookingIngredientSelectionToggleRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                ToggleIngredientSelection(gameEvent.Ingredient);
        }

        private void HandleIngredientSelectionRemoveRequested(CookingIngredientSelectionRemoveRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                RemoveIngredientSelection(gameEvent.Ingredient);
        }

        private void HandleIngredientSearchQueryChangeRequested(CookingIngredientSearchQueryChangeRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                SetIngredientSearchQuery(gameEvent.Query);
        }

        private void HandlePreparationSelectCurrentByIndexRequested(CookingPreparationSelectCurrentByIndexRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                SelectCurrentPreparationByIndex(gameEvent.OptionIndex);
        }

        private void HandlePreparationSelectCurrentRequested(CookingPreparationSelectCurrentRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                SelectCurrentPreparation(gameEvent.Option);
        }

        private void HandlePreparationSelectRequested(CookingPreparationSelectRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                SelectPreparation(gameEvent.Ingredient, gameEvent.Option);
        }

        private void HandlePreparationInteractionCompleteRequested(CookingPreparationInteractionCompleteRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                CompletePreparationInteraction(gameEvent.Ingredient, gameEvent.Option, gameEvent.MiniGameResult);
        }

        private void HandleCookingCompleteRequested(CookingCompleteRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                CompleteCooking();
        }

        private void HandleResultAdvanceRequested(CookingResultAdvanceRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                AdvanceFromResult();
        }

        private void HandleDishHandToNpcRequested(CookingDishHandToNpcRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                HandResultToNpc();
        }

        private void HandleNpcConversationReturnRequested(CookingNpcConversationReturnRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                ReturnToNpcConversation();
        }

        private void HandleViewsCloseRequested(CookingViewsCloseRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                CloseCookingViews();
        }

        private void HandleViewsRefreshRequested(CookingViewsRefreshRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                RefreshCookingViews();
        }

        private void HandlePreparationOpenRequested(CookingPreparationOpenRequestedEvent gameEvent)
        {
            if (IsRequestForThis(gameEvent.Source) == true)
                OpenPreparation();
        }

        private void HandleFlowStateChangedEvent(CookingFlowStateChangedEvent gameEvent)
        {
            if (gameEvent.Source == flowRunner)
                HandleFlowRunnerStateChanged(gameEvent.State);
        }

        private void SubscribeStateSources()
        {
            if (_subscribedNpcRunner != npcRunner)
            {
                if (_subscribedNpcRunner != null)
                    _subscribedNpcRunner.CookingStepReady -= HandleNpcCookingStepReady;

                _subscribedNpcRunner = npcRunner;

                if (_subscribedNpcRunner != null)
                {
                    _subscribedNpcRunner.CookingStepReady += HandleNpcCookingStepReady;
                    if (_subscribedNpcRunner.IsReadyForCooking == true)
                        HandleNpcCookingStepReady();
                }
            }

            if (_subscribedKnowledgeStore != knowledgeStore)
            {
                if (_subscribedKnowledgeStore != null)
                    Bus<CookingKnowledgeChangedEvent>.Events -= HandleKnowledgeChanged;

                _subscribedKnowledgeStore = knowledgeStore;

                if (_subscribedKnowledgeStore != null)
                    Bus<CookingKnowledgeChangedEvent>.Events += HandleKnowledgeChanged;
            }

            if (_subscribedRewardWallet != rewardWallet)
            {
                if (_subscribedRewardWallet != null)
                    Bus<CookingRewardBalanceChangedEvent>.Events -= HandleRewardBalanceChanged;

                _subscribedRewardWallet = rewardWallet;

                if (_subscribedRewardWallet != null)
                    Bus<CookingRewardBalanceChangedEvent>.Events += HandleRewardBalanceChanged;
            }
        }

        private void UnsubscribeStateSources()
        {
            if (_subscribedNpcRunner != null)
                _subscribedNpcRunner.CookingStepReady -= HandleNpcCookingStepReady;

            if (_subscribedKnowledgeStore != null)
                Bus<CookingKnowledgeChangedEvent>.Events -= HandleKnowledgeChanged;

            if (_subscribedRewardWallet != null)
                Bus<CookingRewardBalanceChangedEvent>.Events -= HandleRewardBalanceChanged;

            _subscribedNpcRunner = null;
            _subscribedKnowledgeStore = null;
            _subscribedRewardWallet = null;
        }

        private void HandleFlowRunnerStateChanged(CookingFlowState state)
        {
            if (state == CookingFlowState.Idle || state == CookingFlowState.SelectingIngredients)
                ResetConsumedIngredientSession();

            RefreshCookingViews();
            PublishSnapshotChanged();
        }

        private void HandleNpcCookingStepReady()
        {
            if (autoOpenInventoryWhenNpcReady == false)
                return;

            BeginCookingAfterConversation();
        }

        private void HandleKnowledgeChanged(CookingKnowledgeChangedEvent gameEvent)
        {
            if (gameEvent.Source != knowledgeStore)
                return;

            RefreshRecipeSelectionView(recipeSelectionView);
            PublishSnapshotChanged();
        }

        private void HandleRewardBalanceChanged(CookingRewardBalanceChangedEvent gameEvent)
        {
            if (gameEvent.Source != rewardWallet)
                return;

            PublishSnapshotChanged();
        }

        private void PublishSnapshotChanged()
        {
            CookingGameSnapshot snapshot = BuildSnapshot();
            Bus<CookingGameSnapshotChangedEvent>.Raise(new CookingGameSnapshotChangedEvent(this, snapshot));
        }

    }
}
