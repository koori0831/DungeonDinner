using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public enum CookingDataValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public sealed class CookingDataValidationIssue
    {
        public CookingDataValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public UnityEngine.Object Asset { get; }

        public CookingDataValidationIssue(
            CookingDataValidationSeverity severity,
            string code,
            string message,
            UnityEngine.Object asset)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Asset = asset;
        }

        public override string ToString()
        {
            return $"[{Severity}] {Code}: {Message}";
        }
    }

    public sealed class CookingDataValidationReport
    {
        public IReadOnlyList<CookingDataValidationIssue> Issues { get; }
        public bool HasErrors { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }

        public CookingDataValidationReport(IReadOnlyList<CookingDataValidationIssue> issues)
        {
            Issues = issues ?? Array.Empty<CookingDataValidationIssue>();
            for (int i = 0; i < Issues.Count; i++)
            {
                if (Issues[i].Severity == CookingDataValidationSeverity.Error)
                    ErrorCount++;
                else if (Issues[i].Severity == CookingDataValidationSeverity.Warning)
                    WarningCount++;
            }
            HasErrors = ErrorCount > 0;
        }
    }

    public sealed class CookingDataValidationService
    {
        public const int RepresentativeCombinationLimit = 4096;
        private static readonly Regex ID_PATTERN = new Regex("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly List<CookingDataValidationIssue> _issues = new List<CookingDataValidationIssue>();
        private readonly HashSet<string> _issueKeys = new HashSet<string>(StringComparer.Ordinal);

        public CookingDataValidationReport ValidateCatalog(CookingDataCatalogSO catalog)
        {
            _issues.Clear();
            _issueKeys.Clear();
            if (catalog == null)
            {
                Add(CookingDataValidationSeverity.Error, "CATALOG_MISSING", "Cooking data catalog is missing.", null);
                return new CookingDataValidationReport(_issues);
            }

            ValidateMajorIds(catalog);
            ValidateIngredients(catalog);
            ValidateRecipes(catalog);
            ValidateRepresentativeMatches(catalog);
            return new CookingDataValidationReport(new List<CookingDataValidationIssue>(_issues));
        }

        public CookingDataValidationReport ValidateRecipe(CookingDataCatalogSO catalog, RecipeSO recipe)
        {
            CookingDataValidationReport report = ValidateCatalog(catalog);
            if (recipe == null)
                return report;
            List<CookingDataValidationIssue> filtered = new List<CookingDataValidationIssue>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                CookingDataValidationIssue issue = report.Issues[i];
                if (issue.Asset == recipe || issue.Asset == catalog)
                    filtered.Add(issue);
            }
            return new CookingDataValidationReport(filtered);
        }

        public static bool IsValidId(string value)
        {
            return string.IsNullOrWhiteSpace(value) == false && ID_PATTERN.IsMatch(value);
        }

        private void ValidateMajorIds(CookingDataCatalogSO catalog)
        {
            ValidateIds(catalog.Categories, value => value?.CategoryId, "CATEGORY", "food category");
            ValidateIds(catalog.IngredientCategories, value => value?.CategoryId, "INGREDIENT_CATEGORY", "ingredient category");
            ValidateIds(catalog.Tags, value => value?.TagId, "TAG", "tag");
            ValidateIds(catalog.PreparationMethods, value => value?.MethodId, "PREPARATION_METHOD", "preparation method");
            ValidateIds(catalog.Ingredients, value => value?.IngredientId, "INGREDIENT", "ingredient");
            ValidateIds(catalog.Recipes, value => value?.RecipeId, "RECIPE", "recipe");
        }

        private void ValidateIds<T>(
            IReadOnlyList<T> values,
            Func<T, string> getId,
            string codePrefix,
            string label) where T : UnityEngine.Object
        {
            Dictionary<string, T> seen = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < values.Count; i++)
            {
                T asset = values[i];
                if (asset == null)
                {
                    Add(CookingDataValidationSeverity.Error, codePrefix + "_NULL", $"Catalog contains a null {label} reference.", null);
                    continue;
                }
                string id = getId(asset);
                if (string.IsNullOrWhiteSpace(id))
                {
                    Add(CookingDataValidationSeverity.Error, codePrefix + "_ID_MISSING", $"{asset.name} has no {label} ID.", asset);
                    continue;
                }
                if (IsValidId(id) == false)
                    Add(CookingDataValidationSeverity.Error, codePrefix + "_ID_FORMAT", $"{label} ID '{id}' contains unsupported characters.", asset);
                if (seen.ContainsKey(id))
                    Add(CookingDataValidationSeverity.Error, codePrefix + "_ID_DUPLICATE", $"Duplicate {label} ID '{id}'.", asset);
                else
                    seen.Add(id, asset);
            }
        }

        private void ValidateIngredients(CookingDataCatalogSO catalog)
        {
            Dictionary<string, string> effectSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int ingredientIndex = 0; ingredientIndex < catalog.Ingredients.Count; ingredientIndex++)
            {
                IngredientSO ingredient = catalog.Ingredients[ingredientIndex];
                if (ingredient == null)
                    continue;
                if (ingredient.Category != null && Contains(catalog.IngredientCategories, ingredient.Category) == false)
                    Add(CookingDataValidationSeverity.Error, "INGREDIENT_CATEGORY_REFERENCE", $"{ingredient.name} references an ingredient category outside the catalog.", ingredient);
                ValidateTagReferences(catalog, ingredient.BaseTags, ingredient, "INGREDIENT_TAG_REFERENCE");

                HashSet<string> optionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int optionIndex = 0; optionIndex < ingredient.PreparationOptions.Count; optionIndex++)
                {
                    IngredientPreparationOption option = ingredient.PreparationOptions[optionIndex];
                    if (option == null)
                    {
                        Add(CookingDataValidationSeverity.Error, "PREPARATION_OPTION_NULL", $"{ingredient.name} has a null preparation option.", ingredient);
                        continue;
                    }
                    ValidateId(option.PreparationOptionId, "PREPARATION_OPTION", $"{ingredient.name} option {optionIndex + 1}", ingredient);
                    if (string.IsNullOrWhiteSpace(option.PreparationOptionId) == false && optionIds.Add(option.PreparationOptionId) == false)
                        Add(CookingDataValidationSeverity.Error, "PREPARATION_OPTION_ID_DUPLICATE", $"{ingredient.name} repeats option ID '{option.PreparationOptionId}'.", ingredient);
                    if (option.Method == null || Contains(catalog.PreparationMethods, option.Method) == false)
                        Add(CookingDataValidationSeverity.Error, "PREPARATION_METHOD_REFERENCE", $"{ingredient.name} option '{option.PreparationOptionId}' references a missing method.", ingredient);
                    ValidateTagReferences(catalog, option.AddTags, ingredient, "PREPARATION_ADD_TAG_REFERENCE");
                    ValidateTagReferences(catalog, option.RemoveTags, ingredient, "PREPARATION_REMOVE_TAG_REFERENCE");
                    ValidateFeedbackRules(catalog, ingredient, option, effectSignatures);
                }
            }
        }

        private void ValidateFeedbackRules(
            CookingDataCatalogSO catalog,
            IngredientSO ingredient,
            IngredientPreparationOption option,
            IDictionary<string, string> effectSignatures)
        {
            for (int i = 0; i < option.MiniGameFeedbackRules.Count; i++)
            {
                CookingMiniGameFeedbackRule rule = option.MiniGameFeedbackRules[i];
                if (rule == null)
                {
                    Add(CookingDataValidationSeverity.Error, "FEEDBACK_RULE_NULL", $"{ingredient.name}/{option.PreparationOptionId} contains a null feedback rule.", ingredient);
                    continue;
                }
                ValidateTagReferences(catalog, rule.AddTags, ingredient, "FEEDBACK_ADD_TAG_REFERENCE");
                ValidateTagReferences(catalog, rule.RemoveTags, ingredient, "FEEDBACK_REMOVE_TAG_REFERENCE");
                if (rule.HasIdentityEffect)
                {
                    ValidateId(rule.VariantEffectId, "VARIANT_EFFECT", $"{ingredient.name}/{option.PreparationOptionId}/{rule.Grade}", ingredient);
                    if (string.IsNullOrWhiteSpace(rule.VariantEffectId))
                        continue;
                    string signature = BuildEffectSignature(rule);
                    if (effectSignatures.TryGetValue(rule.VariantEffectId, out string previous)
                        && string.Equals(previous, signature, StringComparison.Ordinal) == false)
                    {
                        Add(CookingDataValidationSeverity.Error, "VARIANT_EFFECT_CONFLICT", $"Variant effect ID '{rule.VariantEffectId}' points to different tag/name effects.", ingredient);
                    }
                    else
                        effectSignatures[rule.VariantEffectId] = signature;
                }
                else if (string.IsNullOrWhiteSpace(rule.VariantEffectId) == false)
                {
                    Add(CookingDataValidationSeverity.Warning, "VARIANT_EFFECT_QUALITY_ONLY", $"{ingredient.name}/{option.PreparationOptionId}/{rule.Grade} only changes quality but has variant effect ID '{rule.VariantEffectId}'.", ingredient);
                }
            }
        }

        private void ValidateRecipes(CookingDataCatalogSO catalog)
        {
            HashSet<string> globalSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int recipeIndex = 0; recipeIndex < catalog.Recipes.Count; recipeIndex++)
            {
                RecipeSO recipe = catalog.Recipes[recipeIndex];
                if (recipe == null)
                    continue;
                if (recipe.Category == null || Contains(catalog.Categories, recipe.Category) == false)
                    Add(CookingDataValidationSeverity.Error, "RECIPE_CATEGORY_REFERENCE", $"{recipe.name} references a missing food category.", recipe);
                ValidateTagReferences(catalog, recipe.BaseTags, recipe, "RECIPE_TAG_REFERENCE");
                if (recipe.RequiredIngredients.Count == 0)
                    Add(CookingDataValidationSeverity.Error, "RECIPE_SLOT_EMPTY", $"{recipe.name} has no ingredient slots.", recipe);

                bool hasDefining = false;
                HashSet<string> localSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int requirementIndex = 0; requirementIndex < recipe.RequiredIngredients.Count; requirementIndex++)
                {
                    RecipeIngredientRequirement requirement = recipe.RequiredIngredients[requirementIndex];
                    if (requirement == null)
                    {
                        Add(CookingDataValidationSeverity.Error, "RECIPE_SLOT_NULL", $"{recipe.name} slot {requirementIndex + 1} is null.", recipe);
                        continue;
                    }
                    if (requirement.RecipeDefining)
                        hasDefining = true;
                    ValidateId(requirement.RequirementId, "REQUIREMENT", $"{recipe.name} slot {requirementIndex + 1}", recipe);
                    if (string.IsNullOrWhiteSpace(requirement.RequirementId) == false)
                    {
                        if (localSlotIds.Add(requirement.RequirementId) == false)
                            Add(CookingDataValidationSeverity.Error, "REQUIREMENT_ID_DUPLICATE", $"{recipe.name} repeats slot ID '{requirement.RequirementId}'.", recipe);
                        if (globalSlotIds.Add(requirement.RequirementId) == false)
                            Add(CookingDataValidationSeverity.Warning, "REQUIREMENT_ID_GLOBAL_DUPLICATE", $"Slot ID '{requirement.RequirementId}' is reused by another recipe.", recipe);
                    }
                    ValidateRequirement(catalog, recipe, requirement, requirementIndex);
                }
                if (hasDefining == false)
                    Add(CookingDataValidationSeverity.Error, "RECIPE_NO_DEFINING_SLOT", $"{recipe.name} has no recipe-defining slot.", recipe);
            }
        }

        private void ValidateRequirement(
            CookingDataCatalogSO catalog,
            RecipeSO recipe,
            RecipeIngredientRequirement requirement,
            int index)
        {
            string label = $"{recipe.name} slot {index + 1}";
            bool empty = requirement.Ingredient == null
                         && requirement.IngredientCategory == null
                         && requirement.RequiredTags.Count == 0
                         && requirement.Alternatives.Count == 0
                         && requirement.AlternativeOptions.Count == 0;
            if (empty)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_EMPTY", $"{label} has no ingredient/category/tag/alternative condition.", recipe);
            if (requirement.HasMaxCount && requirement.MinCount > requirement.MaxCount)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_COUNT_RANGE", $"{label} has minCount greater than maxCount.", recipe);
            if (requirement.Ingredient != null && Contains(catalog.Ingredients, requirement.Ingredient) == false)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_INGREDIENT_REFERENCE", $"{label} references an ingredient outside the catalog.", recipe);
            if (requirement.IngredientCategory != null && Contains(catalog.IngredientCategories, requirement.IngredientCategory) == false)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_CATEGORY_REFERENCE", $"{label} references an ingredient category outside the catalog.", recipe);
            ValidateTagReferences(catalog, requirement.RequiredTags, recipe, "REQUIREMENT_TAG_REFERENCE");
            ValidateMethodReferences(catalog, requirement.RequiredPreparationMethods, recipe, label);

            HashSet<IngredientSO> alternatives = new HashSet<IngredientSO>();
            for (int i = 0; i < requirement.Alternatives.Count; i++)
                ValidateAlternative(catalog, recipe, requirement, requirement.Alternatives[i], alternatives, label);
            for (int i = 0; i < requirement.AlternativeOptions.Count; i++)
                ValidateAlternative(catalog, recipe, requirement, requirement.AlternativeOptions[i]?.Ingredient, alternatives, label);
            if (requirement.Ingredient != null && requirement.IsMatchedBy(requirement.Ingredient) == false)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_CONTRADICTION", $"{label} canonical ingredient contradicts its category/tag conditions.", recipe);
        }

        private void ValidateAlternative(
            CookingDataCatalogSO catalog,
            RecipeSO recipe,
            RecipeIngredientRequirement requirement,
            IngredientSO alternative,
            ISet<IngredientSO> seen,
            string label)
        {
            if (alternative == null || Contains(catalog.Ingredients, alternative) == false)
            {
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_ALTERNATIVE_REFERENCE", $"{label} has a missing alternative ingredient.", recipe);
                return;
            }
            if (alternative == requirement.Ingredient || seen.Add(alternative) == false)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_ALTERNATIVE_DUPLICATE", $"{label} repeats alternative '{alternative.DisplayName}'.", recipe);
            if (requirement.IsMatchedBy(alternative) == false)
                Add(CookingDataValidationSeverity.Error, "REQUIREMENT_ALTERNATIVE_CONTRADICTION", $"{label} alternative '{alternative.DisplayName}' contradicts category/tag conditions.", recipe);
        }

        private void ValidateRepresentativeMatches(CookingDataCatalogSO catalog)
        {
            for (int recipeIndex = 0; recipeIndex < catalog.Recipes.Count; recipeIndex++)
            {
                RecipeSO recipe = catalog.Recipes[recipeIndex];
                if (recipe == null)
                    continue;
                List<List<PreparedIngredientState>> dimensions = BuildRepresentativeDimensions(catalog, recipe);
                if (dimensions == null || dimensions.Count == 0)
                    continue;
                long total = 1;
                for (int i = 0; i < dimensions.Count; i++)
                {
                    int count = Math.Max(1, dimensions[i].Count);
                    if (total > RepresentativeCombinationLimit / (long)count)
                    {
                        total = RepresentativeCombinationLimit + 1L;
                        break;
                    }
                    total *= count;
                }
                if (total > RepresentativeCombinationLimit)
                    Add(CookingDataValidationSeverity.Warning, "REPRESENTATIVE_LIMIT", $"{recipe.name} exceeds {RepresentativeCombinationLimit} representative combinations; only the first {RepresentativeCombinationLimit} are validated.", recipe);

                int inspected = 0;
                EnumerateCombinations(dimensions, 0, new List<PreparedIngredientState>(), combination =>
                {
                    if (inspected++ >= RepresentativeCombinationLimit)
                        return false;
                    ValidateRepresentativeCombination(catalog, recipe, combination);
                    return true;
                });
            }
        }

        private List<List<PreparedIngredientState>> BuildRepresentativeDimensions(
            CookingDataCatalogSO catalog,
            RecipeSO recipe)
        {
            List<List<PreparedIngredientState>> dimensions = new List<List<PreparedIngredientState>>();
            for (int requirementIndex = 0; requirementIndex < recipe.RequiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[requirementIndex];
                if (requirement == null)
                    continue;
                List<PreparedIngredientState> choices = new List<PreparedIngredientState>();
                for (int ingredientIndex = 0; ingredientIndex < catalog.Ingredients.Count; ingredientIndex++)
                {
                    IngredientSO ingredient = catalog.Ingredients[ingredientIndex];
                    if (requirement.IsMatchedBy(ingredient) == false)
                        continue;
                    if (ingredient.PreparationOptions.Count == 0 && requirement.HasRequiredPreparationMethods == false)
                        choices.Add(new PreparedIngredientState(ingredient, null));
                    for (int optionIndex = 0; optionIndex < ingredient.PreparationOptions.Count; optionIndex++)
                    {
                        IngredientPreparationOption option = ingredient.PreparationOptions[optionIndex];
                        if (option != null && requirement.IsPreparationMethodAllowed(option.Method))
                            choices.Add(new PreparedIngredientState(ingredient, option));
                    }
                }
                if (choices.Count == 0)
                {
                    Add(CookingDataValidationSeverity.Error, "RECIPE_NO_REPRESENTATIVE", $"{recipe.name} slot '{requirement.RequirementId}' has no valid ingredient/preparation combination.", recipe);
                    return null;
                }

                if (requirement.RecipeDefining == false)
                {
                    // Optional authored slots must be checked both absent and
                    // present, otherwise conflicts caused by a narrow garnish or
                    // additional ingredient remain invisible to the validator.
                    choices.Insert(0, null);
                    dimensions.Add(choices);
                    continue;
                }

                for (int occurrence = 0; occurrence < Math.Max(1, requirement.MinCount); occurrence++)
                    dimensions.Add(choices);
            }
            return dimensions;
        }

        private void ValidateRepresentativeCombination(
            CookingDataCatalogSO catalog,
            RecipeSO owner,
            IReadOnlyList<PreparedIngredientState> combination)
        {
            RecipePreparedMatchResult ownMatch = owner.MatchPreparedIngredients(combination);
            if (ownMatch.Status == RecipeMatchStatus.NoMatch)
            {
                Add(CookingDataValidationSeverity.Error, "RECIPE_SELF_NO_MATCH", $"{owner.name} does not match one of its representative authored combinations.", owner);
                return;
            }
            if (ownMatch.Status == RecipeMatchStatus.Ambiguous)
                Add(CookingDataValidationSeverity.Error, "RECIPE_SLOT_AMBIGUOUS", $"{owner.name} has a representative combination with multiple meaningful slot bindings.", owner);

            int bestScore = int.MinValue;
            int topCount = 0;
            int matchedCount = 0;
            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                RecipeSO recipe = catalog.Recipes[i];
                if (recipe == null)
                    continue;
                RecipePreparedMatchResult match = recipe.MatchPreparedIngredients(combination);
                if (match.Status == RecipeMatchStatus.NoMatch)
                    continue;
                matchedCount++;
                int score = recipe.CalculateMatchSpecificityScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    topCount = 1;
                }
                else if (score == bestScore)
                    topCount++;
            }
            if (topCount > 1)
                Add(CookingDataValidationSeverity.Error, "RECIPE_TOP_SCORE_TIE", $"{owner.name} has a representative combination tied at the highest recipe score.", owner);
            else if (matchedCount > 1)
                Add(CookingDataValidationSeverity.Warning, "RECIPE_PRIORITY_RESOLUTION", $"{owner.name} overlaps another recipe and is resolved only by priority/specificity.", owner);
        }

        private static bool EnumerateCombinations(
            IReadOnlyList<List<PreparedIngredientState>> dimensions,
            int depth,
            List<PreparedIngredientState> current,
            Func<IReadOnlyList<PreparedIngredientState>, bool> visit)
        {
            if (depth >= dimensions.Count)
                return visit(new List<PreparedIngredientState>(current));

            for (int i = 0; i < dimensions[depth].Count; i++)
            {
                current.Add(dimensions[depth][i]);
                bool keepGoing = depth + 1 >= dimensions.Count
                    ? visit(new List<PreparedIngredientState>(current))
                    : EnumerateCombinations(dimensions, depth + 1, current, visit);
                current.RemoveAt(current.Count - 1);
                if (keepGoing == false)
                    return false;
            }

            return true;
        }

        private void ValidateId(string id, string codePrefix, string label, UnityEngine.Object asset)
        {
            if (string.IsNullOrWhiteSpace(id))
                Add(CookingDataValidationSeverity.Error, codePrefix + "_ID_MISSING", $"{label} has no stable ID.", asset);
            else if (IsValidId(id) == false)
                Add(CookingDataValidationSeverity.Error, codePrefix + "_ID_FORMAT", $"{label} ID '{id}' contains unsupported characters.", asset);
        }

        private void ValidateTagReferences(
            CookingDataCatalogSO catalog,
            IReadOnlyList<FoodTagSO> tags,
            UnityEngine.Object asset,
            string code)
        {
            if (tags == null)
                return;
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == null || Contains(catalog.Tags, tags[i]) == false)
                    Add(CookingDataValidationSeverity.Error, code, $"{asset.name} references a tag outside the catalog.", asset);
            }
        }

        private void ValidateMethodReferences(
            CookingDataCatalogSO catalog,
            IReadOnlyList<PreparationMethodSO> methods,
            UnityEngine.Object asset,
            string label)
        {
            if (methods == null)
                return;
            for (int i = 0; i < methods.Count; i++)
            {
                if (methods[i] == null || Contains(catalog.PreparationMethods, methods[i]) == false)
                    Add(CookingDataValidationSeverity.Error, "REQUIREMENT_METHOD_REFERENCE", $"{label} references a preparation method outside the catalog.", asset);
            }
        }

        private static string BuildEffectSignature(CookingMiniGameFeedbackRule rule)
        {
            List<string> add = BuildSortedTagIds(rule.AddTags);
            List<string> remove = BuildSortedTagIds(rule.RemoveTags);
            return string.Join(",", add) + "|" + string.Join(",", remove) + "|" + rule.ResultNameModifier.Trim();
        }

        private static List<string> BuildSortedTagIds(IReadOnlyList<FoodTagSO> tags)
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    ids.Add(tags[i].TagId ?? string.Empty);
            }
            ids.Sort(StringComparer.OrdinalIgnoreCase);
            return ids;
        }

        private static bool Contains<T>(IReadOnlyList<T> values, T target)
        {
            if (values == null)
                return false;
            for (int i = 0; i < values.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(values[i], target))
                    return true;
            }
            return false;
        }

        private void Add(
            CookingDataValidationSeverity severity,
            string code,
            string message,
            UnityEngine.Object asset)
        {
            string key = severity + "|" + code + "|" + message + "|" + (asset != null ? asset.GetInstanceID() : 0);
            if (_issueKeys.Add(key))
                _issues.Add(new CookingDataValidationIssue(severity, code, message, asset));
        }
    }
}
