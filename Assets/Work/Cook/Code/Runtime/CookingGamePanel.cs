using System;
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

    public sealed class CookingGamePanel : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private CookingGameScreenState initialScreen = CookingGameScreenState.None;
        [SerializeField] private bool applyInitialScreenOnAwake = true;
        [SerializeField] private bool resetFlowWhenOpeningRecipeSelection = true;
        [SerializeField] private bool resetFlowAfterHandingDish = true;
        [SerializeField] private bool keepNpcConversationVisibleBeforePreparation = true;
        [SerializeField] private bool keepRecipeSelectionVisibleBeforePreparation = true;
        [SerializeField] private bool keepRecipeSelectionVisibleDuringInventory;
        [SerializeField] private bool autoCreateTemporaryInventoryView = true;
        [SerializeField] private bool autoCreateTemporaryPreparationView = true;
        [SerializeField] private TMP_FontAsset temporaryUiFontAsset;

        [Header("Views")]
        [SerializeField] private GameObject npcConversationView;
        [SerializeField] private GameObject recipeSelectionView;
        [SerializeField] private GameObject inventoryView;
        [SerializeField] private GameObject preparationView;
        [SerializeField] private GameObject resultView;

        [Header("Events")]
        [SerializeField] private CookingGameScreenChangedEvent screenChanged = new CookingGameScreenChangedEvent();
        [SerializeField] private CookingGameDishResultEvent resultReady = new CookingGameDishResultEvent();
        [SerializeField] private CookingGameDishResultEvent dishHandedToNpc = new CookingGameDishResultEvent();

        private DishResult _currentResult;

        public event Action<CookingGameScreenState> ScreenChanged;
        public event Action<DishResult> ResultReady;
        public event Action<DishResult> DishHandedToNpc;

        public CookingFlowRunner FlowRunner => flowRunner;
        public NpcConversationRunner NpcRunner => npcRunner;
        public CookingGameScreenState CurrentScreen { get; private set; } = CookingGameScreenState.None;
        public DishResult CurrentResult => _currentResult;

        private void Awake()
        {
            EnsureReferences();

            if (applyInitialScreenOnAwake)
                SetScreen(initialScreen);
            else
                ApplyViewActiveStates();
        }

        public void SetFlowRunner(CookingFlowRunner value)
        {
            flowRunner = value;
        }

        public void SetNpcRunner(NpcConversationRunner value)
        {
            npcRunner = value;
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

            _currentResult = null;
            SetScreen(CookingGameScreenState.Inventory);
            return true;
        }

        public bool OpenInventory()
        {
            return OpenDirectIngredientSelection();
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

            SetScreen(CookingGameScreenState.Preparation);
            return true;
        }

        public void OpenPreparation()
        {
            SetScreen(CookingGameScreenState.Preparation);
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
            SetScreen(CookingGameScreenState.Result);
            ResultReady?.Invoke(result);
            resultReady.Invoke(result);
            return true;
        }

        public bool HandResultToNpc()
        {
            EnsureReferences();

            DishResult result = _currentResult ?? flowRunner?.LastResult;
            if (result == null)
            {
                Debug.LogWarning("CookingGamePanel cannot hand a dish to the NPC because no result is ready.", this);
                return false;
            }

            if (CookingNpcDishAdapter.SubmitToNpc(npcRunner, result) == false)
            {
                Debug.LogWarning("CookingGamePanel could not submit the dish. Check that an active NpcConversationRunner is connected.", this);
                return false;
            }

            DishHandedToNpc?.Invoke(result);
            dishHandedToNpc.Invoke(result);

            if (resetFlowAfterHandingDish && flowRunner != null)
                flowRunner.ResetFlow();

            ReturnToNpcConversation();
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

        private void EnsureReferences()
        {
            if (flowRunner == null)
                flowRunner = GetComponentInChildren<CookingFlowRunner>(true);

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            EnsureInventoryView();
            EnsurePreparationView();
        }

        private void SetScreen(CookingGameScreenState screen)
        {
            CurrentScreen = screen;
            ApplyViewActiveStates();
            ScreenChanged?.Invoke(CurrentScreen);
            screenChanged.Invoke(CurrentScreen);
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

        private void InitializeIngredientSelectionView(GameObject view)
        {
            if (view == null)
                return;

            CookingIngredientSelectionView selectionView = view.GetComponent<CookingIngredientSelectionView>();
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

            CookingPreparationView preparation = view.GetComponent<CookingPreparationView>();
            if (preparation == null)
                return;

            preparation.Initialize(this, flowRunner, temporaryUiFontAsset);
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
