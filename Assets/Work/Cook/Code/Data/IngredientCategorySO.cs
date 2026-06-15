using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "IngredientCategory", menuName = "Dungeon Dinner/Cooking/Ingredient Category")]
    public sealed class IngredientCategorySO : ScriptableObject
    {
        [SerializeField] private string categoryId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        public string CategoryId => categoryId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? categoryId : displayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
