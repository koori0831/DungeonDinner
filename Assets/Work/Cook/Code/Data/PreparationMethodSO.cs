using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "PreparationMethod", menuName = "Dungeon Dinner/Cooking/Preparation Method")]
    public sealed class PreparationMethodSO : ScriptableObject
    {
        [SerializeField] private string methodId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite iconSprite;
        [SerializeField, TextArea] private string description;

        public string MethodId => methodId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? methodId : displayName;
        public Sprite IconSprite => iconSprite;
        public string Description => description;
    }
}
