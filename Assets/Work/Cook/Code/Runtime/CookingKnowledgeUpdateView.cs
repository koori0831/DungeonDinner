using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingKnowledgeUpdateView : MonoBehaviour, ICookingKnowledgeUpdateView
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;
        [SerializeField] private RectTransform pageRoot;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI bodyField;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private float typewriterInterval = 0.018f;

        private readonly List<CookingKnowledgeUpdate> _updates = new List<CookingKnowledgeUpdate>();
        private Action _completed;
        private int _index;
        private Coroutine _typewriterRoutine;
        private string _currentBody = string.Empty;
        private bool _isTyping;

        public void Initialize(CookingGamePanel owner, CookingKnowledgeStore store, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            knowledgeStore = store;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureLayout();
            BindButton();
            gameObject.SetActive(false);
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            if (titleField != null)
                titleField.font = fontAsset;
            if (bodyField != null)
                bodyField.font = fontAsset;
        }

        public bool ShowPendingUpdates(Action completed)
        {
            EnsureReferences();
            EnsureLayout();
            BindButton();

            _updates.Clear();
            if (knowledgeStore != null)
                _updates.AddRange(knowledgeStore.ConsumePendingKnowledgeUpdates());

            if (_updates.Count == 0)
                return false;

            _completed = completed;
            _index = 0;
            gameObject.SetActive(true);
            BindCurrentUpdate();
            return true;
        }

        public void ShowNext()
        {
            if (_isTyping)
            {
                CompleteTyping();
                return;
            }

            _index++;
            if (_index < _updates.Count)
            {
                BindCurrentUpdate();
                return;
            }

            Action completed = _completed;
            _completed = null;
            _updates.Clear();
            StopTyping();
            gameObject.SetActive(false);
            completed?.Invoke();
        }

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
            BindButton();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopTyping();
        }

        private void BindCurrentUpdate()
        {
            CookingKnowledgeUpdate update = _index >= 0 && _index < _updates.Count ? _updates[_index] : null;
            if (update == null)
            {
                SetText(titleField, string.Empty);
                SetText(bodyField, string.Empty);
                return;
            }

            SetText(titleField, BuildTitle(update));
            StartTyping(BuildBody(update));
        }

        private static string BuildTitle(CookingKnowledgeUpdate update)
        {
            if (update == null)
                return string.Empty;

            switch (update.UpdateType)
            {
                case CookingKnowledgeUpdateType.RecipeDiscovered:
                    return "새 레시피 기록";
                case CookingKnowledgeUpdateType.RecipeTagsRevealed:
                    return "레시피 단서 기록";
                case CookingKnowledgeUpdateType.PreparationEffectRevealed:
                    return "새 손질 기록";
                default:
                    return string.IsNullOrWhiteSpace(update.Title) ? "도감 기록" : update.Title;
            }
        }

        private static string BuildBody(CookingKnowledgeUpdate update)
        {
            if (update == null)
                return string.Empty;

            switch (update.UpdateType)
            {
                case CookingKnowledgeUpdateType.RecipeDiscovered:
                case CookingKnowledgeUpdateType.RecipeTagsRevealed:
                    return BuildRecipePageBody(update);
                case CookingKnowledgeUpdateType.PreparationEffectRevealed:
                    return BuildIngredientPageBody(update);
                default:
                    return BuildFallbackBody(update);
            }
        }

        private static string BuildRecipePageBody(CookingKnowledgeUpdate update)
        {
            RecipeSO recipe = update.Recipe;
            if (recipe == null)
                return BuildFallbackBody(update);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(recipe.DisplayName);

            if (recipe.Category != null)
                builder.AppendLine($"분류: {recipe.Category.DisplayName}");

            string description = recipe.GetKnowledgeDescription(true, true);
            if (string.IsNullOrWhiteSpace(description) == false)
            {
                builder.AppendLine();
                builder.AppendLine(description);
            }

            if (recipe.RequiredIngredients != null && recipe.RequiredIngredients.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("확인된 재료");
                for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
                    AppendRequirementLine(builder, recipe.RequiredIngredients[i]);
            }

            return builder.ToString().Trim();
        }

        private static string BuildIngredientPageBody(CookingKnowledgeUpdate update)
        {
            IngredientSO ingredient = update.Ingredient;
            IngredientPreparationOption option = ingredient != null
                ? ingredient.FindPreparationOption(update.PreparationMethod)
                : null;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(ingredient != null ? ingredient.DisplayName : "알 수 없는 재료");

            if (ingredient != null && ingredient.Category != null)
                builder.AppendLine($"분류: {ingredient.Category.DisplayName}");

            if (string.IsNullOrWhiteSpace(ingredient?.Description) == false)
            {
                builder.AppendLine();
                builder.AppendLine(ingredient.Description);
            }

            builder.AppendLine();
            builder.AppendLine($"새로 확인한 손질: {GetDisplayName(update.PreparationMethod, option)}");

            if (option != null && string.IsNullOrWhiteSpace(option.Description) == false)
                builder.AppendLine(option.Description);

            builder.AppendLine();
            builder.AppendLine("확인된 효과");
            AppendPreparationEffects(builder, option);
            return builder.ToString().Trim();
        }

        private static string BuildFallbackBody(CookingKnowledgeUpdate update)
        {
            StringBuilder builder = new StringBuilder();
            if (string.IsNullOrWhiteSpace(update.Body) == false)
                builder.AppendLine(update.Body);
            if (update.Recipe != null)
                builder.AppendLine(update.Recipe.DisplayName);
            if (update.Ingredient != null)
                builder.AppendLine(update.Ingredient.DisplayName);
            if (update.PreparationMethod != null)
                builder.AppendLine(update.PreparationMethod.DisplayName);

            return builder.ToString().Trim();
        }

        private void StartTyping(string text)
        {
            StopTyping();
            _currentBody = text ?? string.Empty;

            if (bodyField == null)
                return;

            _typewriterRoutine = StartCoroutine(TypeBodyRoutine());
        }

        private IEnumerator TypeBodyRoutine()
        {
            _isTyping = true;
            bodyField.text = string.Empty;

            for (int i = 0; i < _currentBody.Length; i++)
            {
                bodyField.text = _currentBody.Substring(0, i + 1);
                if (typewriterInterval > 0f)
                    yield return new WaitForSecondsRealtime(typewriterInterval);
                else
                    yield return null;
            }

            _isTyping = false;
            _typewriterRoutine = null;
        }

        private void CompleteTyping()
        {
            StopTyping();
            SetText(bodyField, _currentBody);
        }

        private void StopTyping()
        {
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }

            _isTyping = false;
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();
            if (knowledgeStore == null && gamePanel != null)
                knowledgeStore = gamePanel.KnowledgeStore;
            if (knowledgeStore == null)
                knowledgeStore = GetComponentInParent<CookingKnowledgeStore>();
        }

        private void EnsureLayout()
        {
            if (pageRoot != null && titleField != null && bodyField != null && nextButton != null)
                return;

            Debug.LogError("CookingKnowledgeUpdateView is missing pageRoot/titleField/bodyField/nextButton references. Assign a prefab/inspector based panel.", this);
        }

        private void BindButton()
        {
            if (nextButton == null)
                return;

            nextButton.onClick.RemoveListener(ShowNext);
            nextButton.onClick.AddListener(ShowNext);
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }

        private static void AppendRequirementLine(StringBuilder builder, RecipeIngredientRequirement requirement)
        {
            if (builder == null || requirement == null)
                return;

            List<string> targets = new List<string>();
            if (requirement.Ingredient != null)
                targets.Add(requirement.Ingredient.DisplayName);
            if (requirement.IngredientCategory != null)
                targets.Add($"{requirement.IngredientCategory.DisplayName} 재료군");
            AppendTagNames(targets, requirement.RequiredTags, "태그");
            AppendIngredientNames(targets, requirement.Alternatives, "대체");
            AppendAlternativeNames(targets, requirement.AlternativeOptions, "대체");

            if (targets.Count == 0)
                targets.Add("아무 재료");

            builder.Append("- ");
            builder.Append(string.Join(" / ", targets));
            if (requirement.HasMaxCount)
                builder.Append($" x{requirement.MinCount}-{requirement.MaxCount}");
            else if (requirement.MinCount > 1)
                builder.Append($" x{requirement.MinCount}+");
            AppendRequiredPreparationMethods(builder, requirement.RequiredPreparationMethods);
            builder.AppendLine();
        }

        private static void AppendRequiredPreparationMethods(StringBuilder builder, IReadOnlyList<PreparationMethodSO> methods)
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

        private static void AppendPreparationEffects(StringBuilder builder, IngredientPreparationOption option)
        {
            if (builder == null)
                return;

            if (option == null)
            {
                builder.AppendLine("- 아직 정리할 효과가 없습니다.");
                return;
            }

            bool hasEffect = false;
            if (option.QualityDelta != 0)
            {
                builder.AppendLine($"- 품질 {(option.QualityDelta > 0 ? "+" : string.Empty)}{option.QualityDelta}");
                hasEffect = true;
            }
            if (option.AddTags != null && option.AddTags.Count > 0)
            {
                builder.AppendLine($"- 추가 태그: {BuildTagDisplayText(option.AddTags)}");
                hasEffect = true;
            }
            if (option.RemoveTags != null && option.RemoveTags.Count > 0)
            {
                builder.AppendLine($"- 제거 태그: {BuildTagDisplayText(option.RemoveTags)}");
                hasEffect = true;
            }
            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
            {
                builder.AppendLine($"- 이름 변화: {option.ResultNameModifier}");
                hasEffect = true;
            }
            if (option.CausesDisgusting)
            {
                builder.AppendLine("- 괴식이 될 수 있음");
                hasEffect = true;
            }
            if (option.AddsPoison)
            {
                builder.AppendLine("- 독성 추가");
                hasEffect = true;
            }

            if (hasEffect == false)
                builder.AppendLine("- 특별한 변화 없이 기본 맛을 유지합니다.");
        }

        private static string GetDisplayName(PreparationMethodSO method, IngredientPreparationOption option)
        {
            if (option != null && string.IsNullOrWhiteSpace(option.DisplayName) == false)
                return option.DisplayName;

            return method != null ? method.DisplayName : "손질 없음";
        }

        private static void AppendTagNames(ICollection<string> target, IReadOnlyList<FoodTagSO> tags, string prefix)
        {
            if (target == null || tags == null || tags.Count == 0)
                return;

            string text = BuildTagDisplayText(tags);
            if (string.IsNullOrWhiteSpace(text) == false)
                target.Add($"{prefix}: {text}");
        }

        private static void AppendIngredientNames(ICollection<string> target, IReadOnlyList<IngredientSO> ingredients, string prefix)
        {
            if (target == null || ingredients == null || ingredients.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] != null)
                    names.Add(ingredients[i].DisplayName);
            }

            if (names.Count > 0)
                target.Add($"{prefix}: {string.Join(", ", names)}");
        }

        private static void AppendAlternativeNames(ICollection<string> target, IReadOnlyList<RecipeIngredientAlternative> alternatives, string prefix)
        {
            if (target == null || alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                if (alternatives[i]?.Ingredient != null)
                    names.Add(alternatives[i].Ingredient.DisplayName);
            }

            if (names.Count > 0)
                target.Add($"{prefix}: {string.Join(", ", names)}");
        }

        private static string BuildTagDisplayText(IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return string.Empty;

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    names.Add(tags[i].DisplayName);
            }

            return string.Join(", ", names);
        }
    }
}
