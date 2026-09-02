using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class IngredientPreparationOption
    {
        [SerializeField] private string preparationOptionId;
        [SerializeField] private PreparationMethodSO method;
        [SerializeField] private string displayNameOverride;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<FoodTagSO> addTags = new List<FoodTagSO>();
        [SerializeField] private List<FoodTagSO> removeTags = new List<FoodTagSO>();
        [SerializeField] private int qualityDelta;
        [SerializeField] private bool causesDisgusting;
        [SerializeField] private bool addsPoison;
        [SerializeField] private string resultNameModifier;
        [SerializeField] private List<CookingMiniGameFeedbackRule> miniGameFeedbackRules =
            new List<CookingMiniGameFeedbackRule>();

        public string PreparationOptionId => preparationOptionId ?? string.Empty;
        public PreparationMethodSO Method => method;
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(displayNameOverride) == false)
                    return displayNameOverride;

                return method != null ? method.DisplayName : string.Empty;
            }
        }

        public string Description => description;
        public IReadOnlyList<FoodTagSO> AddTags => addTags;
        public IReadOnlyList<FoodTagSO> RemoveTags => removeTags;
        public int QualityDelta => qualityDelta;
        public bool CausesDisgusting => causesDisgusting;
        public bool AddsPoison => addsPoison;
        public string ResultNameModifier => resultNameModifier;
        public CookingMiniGameType MiniGameType => method != null ? method.MiniGameType : CookingMiniGameType.None;
        public IReadOnlyList<CookingMiniGameFeedbackRule> MiniGameFeedbackRules => miniGameFeedbackRules;

        public bool HasFlavorChange => addTags.Count > 0
                                       || removeTags.Count > 0
                                       || string.IsNullOrWhiteSpace(resultNameModifier) == false;
        public bool HasIdentityEffect => HasFlavorChange || causesDisgusting || addsPoison;

        public CookingMiniGameFeedbackRule FindMiniGameFeedbackRule(CookingMiniGameGrade grade)
        {
            if (miniGameFeedbackRules == null)
                return null;

            for (int i = 0; i < miniGameFeedbackRules.Count; i++)
            {
                CookingMiniGameFeedbackRule rule = miniGameFeedbackRules[i];
                if (rule != null && rule.Grade == grade)
                    return rule;
            }

            return null;
        }

#if UNITY_EDITOR
        public void EditorSetPreparationOptionId(string value)
        {
            preparationOptionId = value ?? string.Empty;
        }
#endif
    }
}
