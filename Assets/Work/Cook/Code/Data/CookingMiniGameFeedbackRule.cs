using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class CookingMiniGameFeedbackRule
    {
        private static readonly IReadOnlyList<FoodTagSO> EMPTY_TAGS = new List<FoodTagSO>();

        [SerializeField] private CookingMiniGameGrade grade = CookingMiniGameGrade.Normal;
        [SerializeField] private int qualityDelta;
        [SerializeField] private List<FoodTagSO> addTags = new List<FoodTagSO>();
        [SerializeField] private List<FoodTagSO> removeTags = new List<FoodTagSO>();
        [SerializeField] private string resultNameModifier;
        [SerializeField, TextArea] private string feedbackText;

        public CookingMiniGameGrade Grade => grade;
        public int QualityDelta => qualityDelta;
        public IReadOnlyList<FoodTagSO> AddTags => addTags ?? EMPTY_TAGS;
        public IReadOnlyList<FoodTagSO> RemoveTags => removeTags ?? EMPTY_TAGS;
        public string ResultNameModifier => resultNameModifier ?? string.Empty;
        public string FeedbackText => feedbackText ?? string.Empty;
    }
}
