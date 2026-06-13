using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingGameButtonBinder : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private Button button;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private bool bindButtonOnEnable = true;
        [SerializeField] private bool refreshInteractableFromSnapshot = true;
        [SerializeField] private CookingGameButtonAction action;

        [Header("Action Data")]
        [SerializeField] private RecipeSO recipe;
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private IngredientPreparationOption preparationOption;
        [SerializeField, Min(0)] private int preparationOptionIndex;

        private CookingGamePanel _subscribedPanel;

        private void Reset()
        {
            button = GetComponent<Button>();
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void Awake()
        {
            EnsureReferences();
            BindButton();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (bindButtonOnEnable)
                BindButton();

            SubscribePanel();
            RefreshInteractable();
        }

        private void OnDisable()
        {
            UnsubscribePanel();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled)
                SubscribePanel();

            RefreshInteractable();
        }

        public void SetRecipe(RecipeSO value)
        {
            recipe = value;
            RefreshInteractable();
        }

        public void SetIngredient(IngredientSO value)
        {
            ingredient = value;
            RefreshInteractable();
        }

        public void SetPreparationOption(IngredientPreparationOption value)
        {
            preparationOption = value;
            RefreshInteractable();
        }

        public void SetPreparationOptionIndex(int value)
        {
            preparationOptionIndex = Mathf.Max(0, value);
            RefreshInteractable();
        }

        public void InvokeAction()
        {
            EnsureReferences();

            if (gamePanel == null)
                return;

            switch (action)
            {
                case CookingGameButtonAction.None:
                    break;
                case CookingGameButtonAction.OpenRecipeSelection:
                    gamePanel.OpenRecipeSelection();
                    break;
                case CookingGameButtonAction.OpenDirectIngredientSelection:
                    gamePanel.OpenDirectIngredientSelection();
                    break;
                case CookingGameButtonAction.ConfirmIngredientSelection:
                    gamePanel.ConfirmIngredientSelection();
                    break;
                case CookingGameButtonAction.ClearIngredientSelection:
                    gamePanel.ClearIngredientSelection();
                    break;
                case CookingGameButtonAction.ConfirmRecipe:
                    gamePanel.ConfirmRecipe(recipe);
                    break;
                case CookingGameButtonAction.ToggleIngredientSelection:
                    gamePanel.ToggleIngredientSelection(ingredient);
                    break;
                case CookingGameButtonAction.RemoveIngredientSelection:
                    gamePanel.RemoveIngredientSelection(ingredient);
                    break;
                case CookingGameButtonAction.SelectCurrentPreparationByIndex:
                    gamePanel.SelectCurrentPreparationByIndex(preparationOptionIndex);
                    break;
                case CookingGameButtonAction.SelectCurrentPreparation:
                    gamePanel.SelectCurrentPreparation(preparationOption);
                    break;
                case CookingGameButtonAction.SelectPreparation:
                    gamePanel.SelectPreparation(ingredient, preparationOption);
                    break;
                case CookingGameButtonAction.CompleteCooking:
                    gamePanel.CompleteCooking();
                    break;
                case CookingGameButtonAction.HandResultToNpc:
                    gamePanel.HandResultToNpc();
                    break;
                case CookingGameButtonAction.ReturnToNpcConversation:
                    gamePanel.ReturnToNpcConversation();
                    break;
                case CookingGameButtonAction.CloseCookingViews:
                    gamePanel.CloseCookingViews();
                    break;
                case CookingGameButtonAction.RefreshCookingViews:
                    gamePanel.RefreshCookingViews();
                    break;
            }
        }

        public void RefreshInteractable()
        {
            if (button == null || refreshInteractableFromSnapshot == false)
                return;

            CookingGameSnapshot snapshot = gamePanel != null ? gamePanel.CurrentSnapshot : null;
            button.interactable = CanInvoke(snapshot);
        }

        private bool CanInvoke(CookingGameSnapshot snapshot)
        {
            if (gamePanel == null)
                return false;

            switch (action)
            {
                case CookingGameButtonAction.None:
                    return false;
                case CookingGameButtonAction.ConfirmIngredientSelection:
                    return snapshot != null && snapshot.HasSelectedIngredients;
                case CookingGameButtonAction.ConfirmRecipe:
                    return recipe != null;
                case CookingGameButtonAction.ToggleIngredientSelection:
                case CookingGameButtonAction.RemoveIngredientSelection:
                    return ingredient != null;
                case CookingGameButtonAction.SelectCurrentPreparationByIndex:
                case CookingGameButtonAction.SelectCurrentPreparation:
                    return snapshot != null && snapshot.HasCurrentIngredient;
                case CookingGameButtonAction.SelectPreparation:
                    return ingredient != null;
                case CookingGameButtonAction.CompleteCooking:
                    return snapshot != null && snapshot.IsEveryIngredientPrepared;
                case CookingGameButtonAction.HandResultToNpc:
                    return snapshot != null && snapshot.CanHandResultToNpc;
                default:
                    return true;
            }
        }

        private void EnsureReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void BindButton()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(InvokeAction);
            button.onClick.AddListener(InvokeAction);
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();

            if (gamePanel == null)
                return;

            gamePanel.SnapshotChanged += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.SnapshotChanged -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshot snapshot)
        {
            RefreshInteractable();
        }
    }

    public enum CookingGameButtonAction
    {
        None,
        OpenRecipeSelection,
        OpenDirectIngredientSelection,
        ConfirmIngredientSelection,
        ClearIngredientSelection,
        ConfirmRecipe,
        ToggleIngredientSelection,
        RemoveIngredientSelection,
        SelectCurrentPreparationByIndex,
        SelectCurrentPreparation,
        SelectPreparation,
        CompleteCooking,
        HandResultToNpc,
        ReturnToNpcConversation,
        CloseCookingViews,
        RefreshCookingViews
    }
}
