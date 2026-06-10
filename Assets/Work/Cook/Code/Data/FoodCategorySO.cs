using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "FoodCategory", menuName = "Dungeon Dinner/Cooking/Food Category")]
    public sealed class FoodCategorySO : ScriptableObject
    {
        [SerializeField] private string categoryId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string description;

        public string CategoryId => categoryId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? categoryId : displayName;
        public Sprite Icon => icon;
        public string Description => description;
    }
}
