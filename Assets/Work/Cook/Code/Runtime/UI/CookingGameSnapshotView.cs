using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingGameSnapshotView : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private bool refreshOnEnable = true;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI screenField;
        [SerializeField] private TextMeshProUGUI flowStateField;
        [SerializeField] private TextMeshProUGUI modeField;
        [SerializeField] private TextMeshProUGUI recipeField;
        [SerializeField] private TextMeshProUGUI currentIngredientField;
        [SerializeField] private TextMeshProUGUI selectedIngredientsField;
        [SerializeField] private TextMeshProUGUI preparationProgressField;
        [SerializeField] private TextMeshProUGUI resultField;
        [SerializeField] private TextMeshProUGUI rewardBalanceField;
        [SerializeField] private TextMeshProUGUI knowledgeField;
        [SerializeField] private TextMeshProUGUI debugSummaryField;

        [Header("Optional Buttons")]
        [SerializeField] private Button openRecipeSelectionButton;
        [SerializeField] private Button openDirectSelectionButton;
        [SerializeField] private Button confirmDirectIngredientsButton;
        [SerializeField] private Button completeCookingButton;
        [SerializeField] private Button handResultToNpcButton;
        [SerializeField] private Button returnToNpcButton;
        [SerializeField] private Button closeCookingViewsButton;

        [Header("Optional Active Objects")]
        [SerializeField] private List<ScreenActiveBinding> screenObjects = new List<ScreenActiveBinding>();
        [SerializeField] private GameObject recipeModeObject;
        [SerializeField] private GameObject directModeObject;
        [SerializeField] private GameObject hasSelectionObject;
        [SerializeField] private GameObject noSelectionObject;
        [SerializeField] private GameObject preparationCompleteObject;
        [SerializeField] private GameObject resultReadyObject;

        [Header("Text")]
        [SerializeField] private string noneText = "-";
        [SerializeField] private string selectedIngredientsPrefix = "선택 재료";
        [SerializeField] private string preparationProgressPrefix = "손질 진행";
        [SerializeField] private string rewardBalancePrefix = "보유 재화";
        [SerializeField] private string knowledgePrefix = "알아낸 정보";

        private CookingGamePanel _subscribedPanel;

        private void Awake()
        {
            EnsureReferences();
            BindButtons();
        }

        private void OnEnable()
        {
            EnsureReferences();
            BindButtons();
            SubscribePanel();

            if (refreshOnEnable == true)
                Refresh();
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

            if (isActiveAndEnabled == true)
                SubscribePanel();

            Refresh();
        }

        public void Refresh()
        {
            EnsureReferences();
            ApplySnapshot(gamePanel != null ? gamePanel.CurrentSnapshot : null);
        }

        public void ApplySnapshot(CookingGameSnapshot snapshot)
        {
            SetText(screenField, snapshot != null ? snapshot.Screen.ToString() : noneText);
            SetText(flowStateField, snapshot != null ? snapshot.FlowState.ToString() : noneText);
            SetText(modeField, snapshot?.Mode != null ? snapshot.Mode.Value.ToString() : noneText);
            SetText(recipeField, GetRecipeName(snapshot));
            SetText(currentIngredientField, GetIngredientName(snapshot?.CurrentIngredient));
            SetText(selectedIngredientsField, BuildSelectedIngredientText(snapshot));
            SetText(preparationProgressField, BuildPreparationProgressText(snapshot));
            SetText(resultField, GetResultName(snapshot));
            SetText(rewardBalanceField, BuildRewardBalanceText(snapshot));
            SetText(knowledgeField, BuildKnowledgeText(snapshot));
            SetText(debugSummaryField, snapshot != null ? snapshot.BuildDebugSummary() : noneText);

            ApplyButtonStates(snapshot);
            ApplyActiveObjects(snapshot);
        }

        private void EnsureReferences()
        {
            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();

            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();

            if (gamePanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshotChangedEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            ApplySnapshot(gameEvent.Snapshot);
        }

        private void BindButtons()
        {
            BindButton(openRecipeSelectionButton, OpenRecipeSelection);
            BindButton(openDirectSelectionButton, OpenDirectSelection);
            BindButton(confirmDirectIngredientsButton, ConfirmDirectIngredients);
            BindButton(completeCookingButton, CompleteCooking);
            BindButton(handResultToNpcButton, HandResultToNpc);
            BindButton(returnToNpcButton, ReturnToNpcConversation);
            BindButton(closeCookingViewsButton, CloseCookingViews);
        }

        private void ApplyButtonStates(CookingGameSnapshot snapshot)
        {
            bool hasPanel = gamePanel != null;
            bool hasSelection = snapshot != null && snapshot.HasSelectedIngredients;
            bool canComplete = snapshot != null && snapshot.IsEveryIngredientPrepared;
            bool canHandResult = snapshot != null && snapshot.CanHandResultToNpc;

            SetInteractable(openRecipeSelectionButton, hasPanel);
            SetInteractable(openDirectSelectionButton, hasPanel);
            SetInteractable(confirmDirectIngredientsButton, hasPanel == true && hasSelection == true);
            SetInteractable(completeCookingButton, hasPanel == true && canComplete == true);
            SetInteractable(handResultToNpcButton, hasPanel == true && canHandResult == true);
            SetInteractable(returnToNpcButton, hasPanel);
            SetInteractable(closeCookingViewsButton, hasPanel);
        }

        private void ApplyActiveObjects(CookingGameSnapshot snapshot)
        {
            for (int i = 0; i < screenObjects.Count; i++)
            {
                ScreenActiveBinding binding = screenObjects[i];
                if (binding != null)
                    binding.Apply(snapshot);
            }

            SetActive(recipeModeObject, snapshot?.Mode == CookingMode.Recipe);
            SetActive(directModeObject, snapshot?.Mode == CookingMode.DirectIngredients);
            SetActive(hasSelectionObject, snapshot != null && snapshot.HasSelectedIngredients);
            SetActive(noSelectionObject, snapshot == null || snapshot.HasSelectedIngredients == false);
            SetActive(preparationCompleteObject, snapshot != null && snapshot.IsEveryIngredientPrepared);
            SetActive(resultReadyObject, snapshot != null && snapshot.HasCurrentResult);
        }

        private void OpenRecipeSelection()
        {
            if (gamePanel != null)
                Bus<CookingRecipeSelectionOpenRequestedEvent>.Raise(new CookingRecipeSelectionOpenRequestedEvent(gamePanel));
        }

        private void OpenDirectSelection()
        {
            if (gamePanel != null)
                Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Raise(new CookingDirectIngredientSelectionOpenRequestedEvent(gamePanel));
        }

        private void ConfirmDirectIngredients()
        {
            if (gamePanel != null)
                Bus<CookingIngredientSelectionConfirmRequestedEvent>.Raise(new CookingIngredientSelectionConfirmRequestedEvent(gamePanel));
        }

        private void CompleteCooking()
        {
            if (gamePanel != null)
                Bus<CookingCompleteRequestedEvent>.Raise(new CookingCompleteRequestedEvent(gamePanel));
        }

        private void HandResultToNpc()
        {
            if (gamePanel != null)
                Bus<CookingDishHandToNpcRequestedEvent>.Raise(new CookingDishHandToNpcRequestedEvent(gamePanel));
        }

        private void ReturnToNpcConversation()
        {
            if (gamePanel != null)
                Bus<CookingNpcConversationReturnRequestedEvent>.Raise(new CookingNpcConversationReturnRequestedEvent(gamePanel));
        }

        private void CloseCookingViews()
        {
            if (gamePanel != null)
                Bus<CookingViewsCloseRequestedEvent>.Raise(new CookingViewsCloseRequestedEvent(gamePanel));
        }

        private string BuildSelectedIngredientText(CookingGameSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SelectedIngredientCount == 0)
                return $"{selectedIngredientsPrefix}: {noneText}";

            return $"{selectedIngredientsPrefix} {snapshot.SelectedIngredientCount}: " +
                   BuildIngredientList(snapshot.SelectedIngredients);
        }

        private string BuildPreparationProgressText(CookingGameSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SelectedIngredientCount == 0)
                return $"{preparationProgressPrefix}: 0 / 0";

            return $"{preparationProgressPrefix}: {snapshot.PreparedIngredientCount} / {snapshot.SelectedIngredientCount}";
        }

        private string BuildRewardBalanceText(CookingGameSnapshot snapshot)
        {
            if (snapshot == null)
                return $"{rewardBalancePrefix}: 0";

            string text = $"{rewardBalancePrefix}: {snapshot.RewardBalance}";
            if (snapshot.HasCurrentResult)
                text += $" / 예상 보상: {snapshot.PreviewRewardAmount}";

            return text;
        }

        private string BuildKnowledgeText(CookingGameSnapshot snapshot)
        {
            if (snapshot == null)
                return $"{knowledgePrefix}: {noneText}";

            return $"{knowledgePrefix}: 레시피 {snapshot.KnownRecipeCount}, 손질 {snapshot.KnownPreparationEffectCount}";
        }

        private string BuildIngredientList(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return noneText;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(GetIngredientName(ingredients[i]));
            }

            return builder.ToString();
        }

        private string GetRecipeName(CookingGameSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SelectedRecipe == null)
                return noneText;

            return snapshot.SelectedRecipe.DisplayName;
        }

        private string GetResultName(CookingGameSnapshot snapshot)
        {
            if (snapshot == null || snapshot.CurrentResult == null)
                return noneText;

            return snapshot.CurrentResult.DisplayName;
        }

        private string GetIngredientName(IngredientSO ingredient)
        {
            return ingredient != null ? ingredient.DisplayName : noneText;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        [Serializable]
        private sealed class ScreenActiveBinding
        {
            [SerializeField] private CookingGameScreenState screen;
            [SerializeField] private GameObject target;
            [SerializeField] private bool activeWhenScreenMatches = true;

            public void Apply(CookingGameSnapshot snapshot)
            {
                if (target == null)
                    return;

                bool matches = snapshot != null && snapshot.Screen == screen;
                SetActive(target, activeWhenScreenMatches ? matches : matches == false);
            }
        }
    }
}
