using UnityEngine;

namespace Work.Items.Code
{
    /// <summary>
    /// 아이템 대분류와 시스템 역할 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Category")]
    public sealed class ItemCategorySO : ScriptableObject
    {
        [SerializeField]
        private string categoryId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        [TextArea]
        private string description;

        [SerializeField]
        private ItemCategoryRole role;

        /// <summary>
        /// 아이템 카테고리 식별자
        /// </summary>
        public string CategoryId => categoryId;

        /// <summary>
        /// 표시용 아이템 카테고리 이름
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) == false ? displayName : name;

        /// <summary>
        /// 표시용 아이템 카테고리 설명
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 아이템 카테고리의 시스템 역할
        /// </summary>
        public ItemCategoryRole Role => role;

        /// <summary>
        /// 지정 역할의 카테고리인지 반환
        /// </summary>
        /// <param name="targetRole">비교할 카테고리 역할</param>
        /// <returns>동일 역할 여부</returns>
        public bool HasRole(ItemCategoryRole targetRole)
        {
            return role == targetRole;
        }
    }
}
