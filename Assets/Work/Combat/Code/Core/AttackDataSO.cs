using UnityEngine;

namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 공격 판정에 필요한 공격 타입과 기본 범위 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Attack Data")]
    public sealed class AttackDataSO : ScriptableObject
    {
        /// <summary>
        /// 공격 식별자
        /// </summary>
        [field: SerializeField]
        public string AttackId { get; private set; }

        /// <summary>
        /// 공격 타입
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
