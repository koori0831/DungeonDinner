using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRecipeDisplayPanel : InfoDisplayPanel
    {
        [Header("Recipe Fields")]
        [SerializeField] private TextMeshProUGUI requiredIngredientsField;
        [SerializeField] private TextMeshProUGUI knownEffectiveTagsField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel;
        [SerializeField] private string confirmRecipeText = "레시피 확정";
        [SerializeField] private string directSelectionText = "재료 직접 선택";

        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private bool showConfirmButtonForDirectSelection = true;

        private CookingRecipeEntryData _currentEntry;

        public void SetGamePanel(CookingGamePanel value)
        {
            gamePanel = value;
        }

        public override void InitializeDisplay(Action backAction)
        {
            base.InitializeDisplay(backAction);
            SetText(requiredIngredientsField, string.Empty);
            SetText(knownEffectiveTagsField, string.Empty);
            SetConfirmButton(false, string.Empty);

            if (confirmButton == null)
            {
                Debug.LogWarning("CookingRecipeDisplayPanel needs a confirm button before it can confirm recipes.", this);
                return;
            }

            confirmButton.onClick.AddListener(ConfirmCurrentEntry);
        }

        public override void Enable(InfoDictionaryEntryData displayInfo)
        {
            base.Enable(displayInfo);

            _currentEntry = displayInfo as CookingRecipeEntryData;
            BindRecipeFields();
        }

        private void BindRecipeFields()
        {
            if (_currentEntry == null)
            {
                SetText(requiredIngredientsField, string.Empty);
                SetText(knownEffectiveTagsField, string.Empty);
                SetConfirmButton(false, confirmRecipeText);
                return;
            }

            SetText(requiredIngredientsField, BuildRequiredIngredientText(_currentEntry));
            SetText(knownEffectiveTagsField, BuildKnownEffectiveTagText(_currentEntry));
            bool canConfirm = _currentEntry.IsDirectIngredientSelection
                ? showConfirmButtonForDirectSelection
                : gamePanel != null && gamePanel.AllowRecipeConfirmation;
            SetConfirmButton(canConfirm, _currentEntry.IsDirectIngredientSelection ? directSelectionText : confirmRecipeText);
        }

        private void ConfirmCurrentEntry()
        {
            if (_currentEntry == null)
                return;

            EnsureGamePanel();
            if (gamePanel == null)
            {
                Debug.LogWarning("CookingRecipeDisplayPanel needs a CookingGamePanel before it can confirm a selection.", this);
                return;
            }

            if (_currentEntry.IsDirectIngredientSelection)
            {
                Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Raise(
                    new CookingDirectIngredientSelectionOpenRequestedEvent(gamePanel));
                return;
            }

            if (gamePanel.AllowRecipeConfirmation == false)
                return;

            Bus<CookingRecipeConfirmRequestedEvent>.Raise(
                new CookingRecipeConfirmRequestedEvent(gamePanel, _currentEntry.Recipe));
        }

        private void EnsureGamePanel()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void SetConfirmButton(bool interactable, string label)
        {
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(interactable);
                confirmButton.interactable = interactable;
            }

            if (confirmButtonLabel != null)
                confirmButtonLabel.text = label;
        }

        private static string BuildRequiredIngredientText(CookingRecipeEntryData entry)
        {
            if (entry.IsDirectIngredientSelection)
                return "가방에서 사용할 재료를 직접 고릅니다.";

            RecipeSO recipe = entry.Recipe;
            if (recipe == null || recipe.RequiredIngredients.Count == 0)
                return "필요 재료: 없음";

            if (entry.IsDiscovered == false)
                return entry.HasAttempted
                    ? "아직 정확한 재료와 손질법은 정리되지 않았습니다. 이번에 시도한 조합은 도감에 기록됩니다."
                    : "아직 정확한 재료와 손질법을 알 수 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("필요 재료");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                string requirementText = BuildRequirementText(requirement);
                if (string.IsNullOrWhiteSpace(requirementText))
                    continue;

                builder.Append("- ");
                builder.Append(requirementText);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildRequirementText(RecipeIngredientRequirement requirement)
        {
            if (requirement == null)
                return string.Empty;

            List<string> targets = new List<string>();

            if (requirement.Ingredient != null)
                targets.Add(requirement.Ingredient.DisplayName);

            if (requirement.IngredientCategory != null)
                targets.Add($"{requirement.IngredientCategory.DisplayName} 재료군");

            AppendTagTargets(targets, requirement.RequiredTags);
            AppendSimpleAlternativeTargets(targets, requirement.Alternatives);
            AppendAlternativeOptionTargets(targets, requirement.AlternativeOptions);

            if (targets.Count == 0)
                targets.Add("아무 재료");

            StringBuilder builder = new StringBuilder();
            builder.Append(string.Join(" / ", targets));
            AppendCountText(builder, requirement);
            AppendPreparationText(builder, requirement.RequiredPreparationMethods);
            return builder.ToString();
        }

        private static void AppendTagTargets(ICollection<string> targets, IReadOnlyList<FoodTagSO> tags)
        {
            if (targets == null || tags == null || tags.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag != null)
                    names.Add(tag.DisplayName);
            }

            if (names.Count > 0)
                targets.Add($"태그: {string.Join(", ", names)}");
        }

        private static void AppendSimpleAlternativeTargets(
            ICollection<string> targets,
            IReadOnlyList<IngredientSO> alternatives)
        {
            if (targets == null || alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                IngredientSO ingredient = alternatives[i];
                if (ingredient != null)
                    names.Add(ingredient.DisplayName);
            }

            if (names.Count > 0)
                targets.Add($"대체: {string.Join(", ", names)}");
        }

        private static void AppendAlternativeOptionTargets(
            ICollection<string> targets,
            IReadOnlyList<RecipeIngredientAlternative> alternatives)
        {
            if (targets == null || alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                RecipeIngredientAlternative alternative = alternatives[i];
                if (alternative != null && alternative.Ingredient != null)
                    names.Add(alternative.Ingredient.DisplayName);
            }

            if (names.Count > 0)
                targets.Add($"대체: {string.Join(", ", names)}");
        }

        private static void AppendCountText(StringBuilder builder, RecipeIngredientRequirement requirement)
        {
            if (builder == null || requirement == null)
                return;

            if (requirement.MinCount <= 1 && requirement.HasMaxCount && requirement.MaxCount <= 1)
                return;

            if (requirement.HasMaxCount)
                builder.Append($" x{requirement.MinCount}-{requirement.MaxCount}");
            else
                builder.Append($" x{requirement.MinCount}+");
        }

        private static void AppendPreparationText(StringBuilder builder, IReadOnlyList<PreparationMethodSO> methods)
        {
            if (builder == null || methods == null || methods.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < methods.Count; i++)
            {
                PreparationMethodSO method = methods[i];
                if (method != null)
                    names.Add(method.DisplayName);
            }

            if (names.Count > 0)
                builder.Append($" ({string.Join(" / ", names)})");
        }

        private static void AppendAlternativeText(
            StringBuilder builder,
            IReadOnlyList<RecipeIngredientAlternative> alternatives)
        {
            if (alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                RecipeIngredientAlternative alternative = alternatives[i];
                if (alternative != null && alternative.Ingredient != null)
                    names.Add(alternative.Ingredient.DisplayName);
            }

            if (names.Count > 0)
                builder.Append($" (대체: {string.Join(", ", names)})");
        }

        private static string BuildKnownEffectiveTagText(CookingRecipeEntryData entry)
        {
            if (entry.IsDirectIngredientSelection)
                return string.Empty;

            if (entry.KnownEffectiveTags == null || entry.KnownEffectiveTags.Count == 0)
                return "유효 태그: 아직 알아낸 정보 없음";

            List<string> tags = new List<string>();
            for (int i = 0; i < entry.KnownEffectiveTags.Count; i++)
            {
                FoodTagSO tag = entry.KnownEffectiveTags[i];
                if (tag != null)
                    tags.Add(tag.DisplayName);
            }

            return tags.Count > 0
                ? $"유효 태그: {string.Join(", ", tags)}"
                : "유효 태그: 아직 알아낸 정보 없음";
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }
    }
}
