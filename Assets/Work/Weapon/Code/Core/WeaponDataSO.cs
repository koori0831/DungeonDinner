using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Weapon.Code.Core
{
    /// <summary>
    /// 장착 가능한 무기의 기본 정보와 공격 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Weapon/Weapon Data")]
    public sealed class WeaponDataSO : ScriptableObject
    {
        /// <summary>
        /// 무기 식별자
        /// </summary>
        [field: SerializeField]
        public string WeaponId { get; private set; }

        /// <summary>
        /// 표시용 무기 이름
        /// </summary>
        [field: SerializeField]
        public string DisplayName { get; private set; }

        /// <summary>
        /// 일반 공격 데이터
        /// </summary>
        [field: SerializeField]
        public AttackDataSO NormalAttackData { get; private set; }

        // TODO: 언젠가 강공이나 스킬 추가되었을 떄 만들기
        
        //[field: SerializeField]
        //public AttackDataSO HeavyAttackData { get; private set; }
        //[field: SerializeField]
        //public AttackDataSO SkillAttackData { get; private set; }

        /// <summary>
        /// 공격 타입에 맞는 공격 데이터 반환
        /// </summary>
        /// <param name="attackType">무기 공격 입력 타입</param>
        /// <returns>공격 데이터</returns>
        public AttackDataSO GetAttackData(WeaponAttackType attackType)
        {
            // if (attackType == WeaponAttackType.Heavy)
            // {
            //     return HeavyAttackData != null ? HeavyAttackData : NormalAttackData;
            // }
            //
            // if (attackType == WeaponAttackType.Skill)
            // {
            //     return SkillAttackData != null ? SkillAttackData : NormalAttackData;
            // }

            return NormalAttackData;
        }
    }
}
