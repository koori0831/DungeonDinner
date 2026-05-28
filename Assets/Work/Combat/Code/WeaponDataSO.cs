using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 플레이어 무기의 공격 타입과 기본 공격 범위 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Weapon Data")]
    public sealed class WeaponDataSO : ScriptableObject
    {
        /// <summary>
        /// 무기 식별자
        /// </summary>
        [field: SerializeField]
        public string WeaponId { get; private set; }

        /// <summary>
        /// 무기 공격 타입
        /// </summary>
        [field: SerializeField]
        public AttackType AttackType { get; private set; }

        /// <summary>
        /// 공격 거리
        /// </summary>
        [field: SerializeField]
        public float Range { get; private set; }

        /// <summary>
        /// 공격 반지름
        /// </summary>
        [field: SerializeField]
        public float Radius { get; private set; }

        /// <summary>
        /// 넉백 강도
        /// </summary>
        [field: SerializeField]
        public float KnockbackPower { get; private set; }
    }
}
