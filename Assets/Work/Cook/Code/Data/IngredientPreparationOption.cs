using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class IngredientPreparationOption
    {
        [SerializeField] private PreparationMethodSO method;
        [SerializeField] private string displayNameOverride;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<FoodTagSO> addTags = new List<FoodTagSO>();
        [SerializeField] private List<FoodTagSO> removeTags = new List<FoodTagSO>();
        [SerializeField] private int qualityDelta;
        [SerializeField] private bool causesDisgusting;
        [SerializeField] private bool addsPoison;
        [SerializeField] private string resultNameModifier;

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

        public bool HasFlavorChange => addTags.Count > 0
                                       || removeTags.Count > 0
                                       || string.IsNullOrWhiteSpace(resultNameModifier) == false;
    }
}
