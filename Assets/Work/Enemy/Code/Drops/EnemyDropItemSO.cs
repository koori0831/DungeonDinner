using UnityEngine;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 적 피격 드랍 로그에 사용할 아이템 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Enemy/Drop/Item")]
    public sealed class EnemyDropItemSO : ScriptableObject
    {
        [SerializeField]
        private string itemId;

        [SerializeField]
        private string displayName;

        /// <summary>
        /// 아이템 식별자
        /// </summary>
        public string ItemId => itemId;

        /// <summary>
        /// 표시용 아이템 이름
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) == false ? displayName : name;
    }
}
