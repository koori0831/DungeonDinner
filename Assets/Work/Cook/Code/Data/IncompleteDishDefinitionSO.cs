using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(menuName = "Cooking/Incomplete Dish Display")]
    public sealed class IncompleteDishDefinitionSO : ScriptableObject
    {
        public const string EntryId = "incomplete_dish";
        [SerializeField] private string displayName = "미완성 요리";
        [SerializeField, TextArea] private string description = "재료들이 제각각의 맛과 모양을 주장하고 있다. 한 접시에 담기는 했지만, 아직 하나의 요리가 되지는 못했다.";
        [SerializeField] private Sprite icon;
        private static IncompleteDishDefinitionSO _instance;
        public static IncompleteDishDefinitionSO Instance => _instance != null ? _instance : _instance = Resources.Load<IncompleteDishDefinitionSO>("IncompleteDish");
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
