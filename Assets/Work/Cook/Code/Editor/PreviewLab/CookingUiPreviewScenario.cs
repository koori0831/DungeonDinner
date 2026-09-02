using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Editor.PreviewLab
{
    public enum CookingUiPreviewScreen
    {
        IngredientSelection,
        Preparation,
        MiniGame,
        Result
    }

    [Serializable]
    public sealed class CookingUiPreviewIngredientEntry
    {
        [SerializeField] private IngredientSO ingredient;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField, Min(0)] private int preparationOptionIndex;

        public IngredientSO Ingredient => ingredient;
        public int Quantity => Mathf.Max(1, quantity);
        public int PreparationOptionIndex => Mathf.Max(0, preparationOptionIndex);
    }

    /// <summary>
    /// Cooking UI Preview Lab에서만 사용하는 에디터 전용 시나리오.
    /// Editor 폴더에 있으므로 Player 빌드 어셈블리에는 포함되지 않는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CookingUiPreviewScenario",
        menuName = "Dungeon Dinner/Editor/Cooking UI Preview Scenario")]
    public sealed class CookingUiPreviewScenario : ScriptableObject
    {
        [Header("Preview Target")]
        [SerializeField] private CookingUiPreviewScreen targetScreen = CookingUiPreviewScreen.IngredientSelection;
        [SerializeField] private CookingDataCatalogSO catalogOverride;

        [Header("Ingredients")]
        [SerializeField] private List<CookingUiPreviewIngredientEntry> ingredients =
            new List<CookingUiPreviewIngredientEntry>();
        [SerializeField, Min(0)] private int minimumSelection = 1;
        [SerializeField, Min(0)] private int maximumSelection;

        [Header("Forced Mini Game Result")]
        [SerializeField] private CookingMiniGameGrade forcedGrade = CookingMiniGameGrade.Good;
        [SerializeField, Range(0f, 1f)] private float forcedScore = 0.8f;
        [SerializeField] private string forcedFeedback = "에디터 프리뷰 판정";

        public CookingUiPreviewScreen TargetScreen => targetScreen;
        public CookingDataCatalogSO CatalogOverride => catalogOverride;
        public IReadOnlyList<CookingUiPreviewIngredientEntry> Ingredients => ingredients;
        public int MinimumSelection => Mathf.Max(0, minimumSelection);
        public int MaximumSelection => Mathf.Max(0, maximumSelection);
        public CookingMiniGameGrade ForcedGrade => forcedGrade;
        public float ForcedScore => Mathf.Clamp01(forcedScore);
        public string ForcedFeedback => forcedFeedback ?? string.Empty;

        private void OnValidate()
        {
            minimumSelection = Mathf.Max(0, minimumSelection);
            maximumSelection = Mathf.Max(0, maximumSelection);
            if (maximumSelection > 0 && maximumSelection < minimumSelection)
                maximumSelection = minimumSelection;

            forcedScore = Mathf.Clamp01(forcedScore);
        }
    }
}
