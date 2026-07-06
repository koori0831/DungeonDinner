using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "PreparationMethod", menuName = "Dungeon Dinner/Cooking/Preparation Method")]
    public sealed class PreparationMethodSO : ScriptableObject
    {
        [SerializeField] private string methodId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        public string MethodId => methodId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? methodId : displayName;
        public string Description => description;
    }
}
