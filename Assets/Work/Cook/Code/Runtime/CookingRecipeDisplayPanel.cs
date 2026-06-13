using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;

namespace Work.Cook.Code.Runtime
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

        private CookingRecipeEntryData _currentEntry;

        public void SetGamePanel(CookingGamePanel value)
        {
            gamePanel = value;
        }

        public override void InitializeDisplay(Action backAction)
        {
            base.InitializeDisplay(backAction);

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
            SetConfirmButton(true, _currentEntry.IsDirectIngredientSelection ? directSelectionText : confirmRecipeText);
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
                gamePanel.OpenDirectIngredientSelection();
                return;
            }

            gamePanel.ConfirmRecipe(_currentEntry.Recipe);
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
                confirmButton.interactable = interactable;

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

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("필요 재료");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                IngredientSO ingredient = requirement != null ? requirement.Ingredient : null;
                if (ingredient == null)
                    continue;

                builder.Append("- ");
                builder.Append(ingredient.DisplayName);
                AppendAlternativeText(builder, requirement.AlternativeOptions);
                builder.AppendLine();
            }

            return builder.ToString();
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
