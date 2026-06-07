using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "FoodTag", menuName = "Dungeon Dinner/Cooking/Food Tag")]
    public sealed class FoodTagSO : ScriptableObject
    {
        [SerializeField] private string tagId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        public string TagId => tagId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? tagId : displayName;
        public string Description => description;
    }
}
