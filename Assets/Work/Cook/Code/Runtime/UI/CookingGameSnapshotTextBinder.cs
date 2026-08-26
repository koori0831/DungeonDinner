using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingGameSnapshotTextBinder : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private TextMeshProUGUI textField;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private CookingGameSnapshotTextKind textKind;
        [SerializeField] private string prefix;
        [SerializeField] private string noneText = "-";
        [SerializeField] private string separator = ", ";

        private CookingGamePanel _subscribedPanel;

        private void Reset()
        {
            textField = GetComponent<TextMeshProUGUI>();
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribePanel();
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
            ApplySnapshot(gamePanel != null ? gamePanel.CurrentSnapshot : null);
        }

        public void ApplySnapshot(CookingGameSnapshot snapshot)
        {
            if (textField == null)
                return;

            string value = BuildText(snapshot);
            textField.text = string.IsNullOrWhiteSpace(prefix) ? value : $"{prefix}{value}";
        }

        private string BuildText(CookingGameSnapshot snapshot)
        {
            if (snapshot == null)
                return noneText;

            switch (textKind)
            {
                case CookingGameSnapshotTextKind.Screen:
                    return snapshot.Screen.ToString();
                case CookingGameSnapshotTextKind.FlowState:
                    return snapshot.FlowState.ToString();
                case CookingGameSnapshotTextKind.Mode:
                    return snapshot.Mode?.ToString() ?? noneText;
                case CookingGameSnapshotTextKind.SelectedRecipeName:
                    return snapshot.SelectedRecipe != null ? snapshot.SelectedRecipe.DisplayName : noneText;
                case CookingGameSnapshotTextKind.CurrentIngredientName:
                    return snapshot.CurrentIngredient != null ? snapshot.CurrentIngredient.DisplayName : noneText;
                case CookingGameSnapshotTextKind.CurrentIngredientDescription:
                    return snapshot.CurrentIngredient != null ? snapshot.CurrentIngredient.Description : noneText;
                case CookingGameSnapshotTextKind.SelectedIngredientCount:
                    return snapshot.SelectedIngredientCount.ToString();
                case CookingGameSnapshotTextKind.SelectedIngredientNames:
                    return BuildIngredientNames(snapshot.SelectedIngredients);
                case CookingGameSnapshotTextKind.PreparedIngredientCount:
                    return snapshot.PreparedIngredientCount.ToString();
                case CookingGameSnapshotTextKind.PreparationProgress:
                    return $"{snapshot.PreparedIngredientCount} / {snapshot.SelectedIngredientCount}";
                case CookingGameSnapshotTextKind.ResultName:
                    return snapshot.CurrentResult != null ? snapshot.CurrentResult.DisplayName : noneText;
                case CookingGameSnapshotTextKind.ResultQuality:
                    return snapshot.CurrentResult != null ? snapshot.CurrentResult.Quality.ToString() : noneText;
                case CookingGameSnapshotTextKind.ResultTags:
                    return snapshot.CurrentResult != null ? snapshot.CurrentResult.BuildTagText(',') : noneText;
                case CookingGameSnapshotTextKind.ResultSummary:
                    return BuildResultSummary(snapshot.CurrentResult);
                case CookingGameSnapshotTextKind.RewardBalance:
                    return snapshot.RewardBalance.ToString();
                case CookingGameSnapshotTextKind.PreviewRewardAmount:
                    return snapshot.PreviewRewardAmount.ToString();
                case CookingGameSnapshotTextKind.RewardSummary:
                    return snapshot.HasCurrentResult
                        ? $"{snapshot.RewardBalance} (+{snapshot.PreviewRewardAmount})"
                        : snapshot.RewardBalance.ToString();
                case CookingGameSnapshotTextKind.NpcExpectedResult:
                    return BuildNpcResultText(snapshot.CurrentNpcMatchReport);
                case CookingGameSnapshotTextKind.NpcMatchRatio:
                    return BuildNpcMatchRatio(snapshot.CurrentNpcMatchReport);
                case CookingGameSnapshotTextKind.NpcMatchSummary:
                    return BuildNpcMatchSummary(snapshot.CurrentNpcMatchReport);
                case CookingGameSnapshotTextKind.KnowledgeSummary:
                    return $"Recipes {snapshot.KnownRecipeCount} / Prep {snapshot.KnownPreparationEffectCount}";
                case CookingGameSnapshotTextKind.DebugSummary:
                    return snapshot.BuildDebugSummary();
                default:
                    return noneText;
            }
        }

        private string BuildIngredientNames(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return noneText;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(separator);

                builder.Append(ingredients[i].DisplayName);
            }

            return builder.Length > 0 ? builder.ToString() : noneText;
        }

        private string BuildResultSummary(DishResult result)
        {
            if (result == null)
                return noneText;

            string recipe = result.BaseRecipe != null ? result.BaseRecipe.DisplayName : noneText;
            string category = result.Category != null ? result.Category.DisplayName : noneText;
            return $"{result.DisplayName} / {result.Quality} / {recipe} / {category}";
        }

        private string BuildNpcResultText(NpcDishMatchReport report)
        {
            if (report == null)
                return noneText;

            NpcConversationResult result = report.Evaluation?.Result ?? NpcConversationResult.Wrong;
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return "Perfect";
                case NpcConversationResult.Correct:
                    return "Correct";
                case NpcConversationResult.Similar:
                    return "Similar";
                case NpcConversationResult.Disgusting:
                case NpcConversationResult.Wrong:
                default:
                    return "Wrong";
            }
        }

        private string BuildNpcMatchRatio(NpcDishMatchReport report)
        {
            if (report == null)
                return noneText;

            int percent = Mathf.RoundToInt(report.MatchRatio * 100f);
            return $"{report.MatchScore}/{report.MaxMatchScore} ({percent}%)";
        }

        private string BuildNpcMatchSummary(NpcDishMatchReport report)
        {
            if (report == null)
                return noneText;

            return $"{BuildNpcResultText(report)} / {BuildNpcMatchRatio(report)}";
        }

        private void EnsureReferences()
        {
            if (textField == null)
                textField = GetComponent<TextMeshProUGUI>();

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
    }

    public enum CookingGameSnapshotTextKind
    {
        Screen,
        FlowState,
        Mode,
        SelectedRecipeName,
        CurrentIngredientName,
        CurrentIngredientDescription,
        SelectedIngredientCount,
        SelectedIngredientNames,
        PreparedIngredientCount,
        PreparationProgress,
        ResultName,
        ResultQuality,
        ResultTags,
        ResultSummary,
        RewardBalance,
        PreviewRewardAmount,
        RewardSummary,
        NpcExpectedResult,
        NpcMatchRatio,
        NpcMatchSummary,
        KnowledgeSummary,
        DebugSummary
    }
}
