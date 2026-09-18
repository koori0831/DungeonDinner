using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Editor
{
    public sealed partial class CookingDataEditorWindow
    {
        private sealed class RecipeDraft
        {
            public string RecipeId;
            public string DisplayName;
            public Sprite IconSprite;
            public string Description;
            public FoodCategorySO Category;
            public int Priority;
            public List<FoodTagSO> BaseTags = new List<FoodTagSO>();
            public List<IngredientRequirementDraft> RequiredIngredients = new List<IngredientRequirementDraft>();
            public List<PerfectRuleDraft> PerfectRules = new List<PerfectRuleDraft>();

            public static RecipeDraft From(RecipeSO recipe)
            {
                SerializedObject serialized = new SerializedObject(recipe);
                RecipeDraft draft = new RecipeDraft
                {
                    RecipeId = ReadString(serialized, "recipeId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    IconSprite = ReadObject<Sprite>(serialized, "iconSprite"),
                    Description = ReadString(serialized, "description"),
                    Category = ReadObject<FoodCategorySO>(serialized, "category"),
                    Priority = ReadInt(serialized, "priority"),
                    BaseTags = ReadObjectArray<FoodTagSO>(serialized, "baseTags")
                };

                for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
                {
                    RecipeIngredientRequirement source = recipe.RequiredIngredients[i];
                    IngredientRequirementDraft requirement = new IngredientRequirementDraft();
                    if (source != null)
                    {
                        requirement.RequirementId = source.RequirementId;
                        requirement.Ingredient = source.Ingredient;
                        requirement.IngredientCategory = source.IngredientCategory;
                        requirement.RequiredTags = new List<FoodTagSO>(source.RequiredTags);
                        requirement.SimpleAlternatives = new List<IngredientSO>(source.Alternatives);
                        requirement.RequiredPreparationMethods = new List<PreparationMethodSO>(source.RequiredPreparationMethods);
                        if (requirement.RequiredPreparationMethods.Count == 0 && source.RequiredPreparationMethod != null)
                            requirement.RequiredPreparationMethods.Add(source.RequiredPreparationMethod);
                        requirement.MinCount = source.MinCount;
                        requirement.MaxCount = source.MaxCount;
                        requirement.RecipeDefining = source.RecipeDefining;
                        requirement.RequireManualPreparation = source.RequireManualPreparation;
                        requirement.UsePreparationResultNameModifier = source.UsePreparationResultNameModifier;

                        for (int alternativeIndex = 0; alternativeIndex < source.AlternativeOptions.Count; alternativeIndex++)
                        {
                            RecipeIngredientAlternative alternative = source.AlternativeOptions[alternativeIndex];
                            if (alternative != null)
                            {
                                requirement.Alternatives.Add(new IngredientAlternativeDraft
                                {
                                    Ingredient = alternative.Ingredient,
                                    ResultNameModifier = alternative.ResultNameModifier
                                });
                            }
                        }

                    }

                    draft.RequiredIngredients.Add(requirement);
                }

                for (int i = 0; i < recipe.PerfectPreparationRules.Count; i++)
                {
                    RecipePreparationRule source = recipe.PerfectPreparationRules[i];
                    PerfectRuleDraft rule = new PerfectRuleDraft();
                    if (source != null)
                    {
                        rule.Ingredient = source.Ingredient;
                        rule.PerfectMethod = source.PerfectMethod;
                    }

                    draft.PerfectRules.Add(rule);
                }

                return draft;
            }
        }

        private sealed class CategoryDraft
        {
            public string CategoryId;
            public string DisplayName;
            public Sprite Icon;
            public string Description;

            public static CategoryDraft From(FoodCategorySO category)
            {
                SerializedObject serialized = new SerializedObject(category);
                return new CategoryDraft
                {
                    CategoryId = ReadString(serialized, "categoryId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Icon = ReadObject<Sprite>(serialized, "icon"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class IngredientCategoryDraft
        {
            public string CategoryId;
            public string DisplayName;
            public Sprite Icon;
            public string Description;

            public static IngredientCategoryDraft From(IngredientCategorySO category)
            {
                SerializedObject serialized = new SerializedObject(category);
                return new IngredientCategoryDraft
                {
                    CategoryId = ReadString(serialized, "categoryId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Icon = ReadObject<Sprite>(serialized, "icon"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class TagDraft
        {
            public string TagId;
            public string DisplayName;
            public string Description;

            public static TagDraft From(FoodTagSO tag)
            {
                SerializedObject serialized = new SerializedObject(tag);
                return new TagDraft
                {
                    TagId = ReadString(serialized, "tagId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class MethodDraft
        {
            public string MethodId;
            public string DisplayName;
            public Sprite IconSprite;
            public string Description;

            public static MethodDraft From(PreparationMethodSO method)
            {
                SerializedObject serialized = new SerializedObject(method);
                return new MethodDraft
                {
                    MethodId = ReadString(serialized, "methodId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    IconSprite = ReadObject<Sprite>(serialized, "iconSprite"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class IngredientDraft
        {
            public string IngredientId;
            public string DisplayName;
            public Sprite IconSprite;
            public string Description;
            public IngredientCategorySO Category;
            public GameObject ModelPrefab;
            public List<FoodTagSO> BaseTags = new List<FoodTagSO>();
            public List<PreparationOptionDraft> PreparationOptions = new List<PreparationOptionDraft>();

            public static IngredientDraft From(IngredientSO ingredient)
            {
                SerializedObject serialized = new SerializedObject(ingredient);
                IngredientDraft draft = new IngredientDraft
                {
                    IngredientId = ReadString(serialized, "ingredientId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    IconSprite = ReadObject<Sprite>(serialized, "iconSprite"),
                    Description = ReadString(serialized, "description"),
                    Category = ReadObject<IngredientCategorySO>(serialized, "category"),
                    ModelPrefab = ReadObject<GameObject>(serialized, "modelPrefab"),
                    BaseTags = ReadObjectArray<FoodTagSO>(serialized, "baseTags")
                };

                SerializedProperty options = serialized.FindProperty("preparationOptions");
                if (options != null && options.isArray)
                {
                    for (int i = 0; i < options.arraySize; i++)
                    {
                        SerializedProperty element = options.GetArrayElementAtIndex(i);
                        draft.PreparationOptions.Add(new PreparationOptionDraft
                        {
                            PreparationOptionId = ReadRelativeString(element, "preparationOptionId"),
                            Method = ReadRelativeObject<PreparationMethodSO>(element, "method"),
                            DisplayNameOverride = ReadRelativeString(element, "displayNameOverride"),
                            Description = ReadRelativeString(element, "description"),
                            AddTags = ReadObjectArray<FoodTagSO>(element.FindPropertyRelative("addTags")),
                            RemoveTags = ReadObjectArray<FoodTagSO>(element.FindPropertyRelative("removeTags")),
                            QualityDelta = ReadRelativeInt(element, "qualityDelta"),
                            CausesDisgusting = ReadRelativeBool(element, "causesDisgusting"),
                            AddsPoison = ReadRelativeBool(element, "addsPoison"),
                            ResultNameModifier = ReadRelativeString(element, "resultNameModifier"),
                            MiniGameFeedbackRules = ReadFeedbackRules(element.FindPropertyRelative("miniGameFeedbackRules"))
                        });
                    }
                }

                return draft;
            }
        }

        private sealed class IngredientRequirementDraft
        {
            public string RequirementId;
            public IngredientSO Ingredient;
            public IngredientCategorySO IngredientCategory;
            public List<FoodTagSO> RequiredTags = new List<FoodTagSO>();
            public List<IngredientSO> SimpleAlternatives = new List<IngredientSO>();
            public List<IngredientAlternativeDraft> Alternatives = new List<IngredientAlternativeDraft>();
            public List<PreparationMethodSO> RequiredPreparationMethods = new List<PreparationMethodSO>();
            public int MinCount = 1;
            public int MaxCount = 1;
            public bool RecipeDefining = true;
            public bool RequireManualPreparation;
            public bool UsePreparationResultNameModifier = true;
        }

        private sealed class IngredientAlternativeDraft
        {
            public IngredientSO Ingredient;
            public string ResultNameModifier;
        }

        private sealed class PerfectRuleDraft
        {
            public IngredientSO Ingredient;
            public PreparationMethodSO PerfectMethod;
        }

        private sealed class PreparationOptionDraft
        {
            public string PreparationOptionId;
            public PreparationMethodSO Method;
            public string DisplayNameOverride;
            public string Description;
            public List<FoodTagSO> AddTags = new List<FoodTagSO>();
            public List<FoodTagSO> RemoveTags = new List<FoodTagSO>();
            public int QualityDelta;
            public bool CausesDisgusting;
            public bool AddsPoison;
            public string ResultNameModifier;
            public List<MiniGameFeedbackRuleDraft> MiniGameFeedbackRules = new List<MiniGameFeedbackRuleDraft>();
        }

        private sealed class MiniGameFeedbackRuleDraft
        {
            public CookingMiniGameGrade Grade;
            public string VariantEffectId;
            public int QualityDelta;
            public List<FoodTagSO> AddTags = new List<FoodTagSO>();
            public List<FoodTagSO> RemoveTags = new List<FoodTagSO>();
            public string ResultNameModifier;
            public string FeedbackText;
        }

        private static List<MiniGameFeedbackRuleDraft> ReadFeedbackRules(SerializedProperty property)
        {
            List<MiniGameFeedbackRuleDraft> rules = new List<MiniGameFeedbackRuleDraft>();
            if (property == null || property.isArray == false)
                return rules;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                rules.Add(new MiniGameFeedbackRuleDraft
                {
                    Grade = (CookingMiniGameGrade)element.FindPropertyRelative("grade").enumValueIndex,
                    VariantEffectId = ReadRelativeString(element, "variantEffectId"),
                    QualityDelta = ReadRelativeInt(element, "qualityDelta"),
                    AddTags = ReadObjectArray<FoodTagSO>(element.FindPropertyRelative("addTags")),
                    RemoveTags = ReadObjectArray<FoodTagSO>(element.FindPropertyRelative("removeTags")),
                    ResultNameModifier = ReadRelativeString(element, "resultNameModifier"),
                    FeedbackText = ReadRelativeString(element, "feedbackText")
                });
            }
            return rules;
        }
    }
}
