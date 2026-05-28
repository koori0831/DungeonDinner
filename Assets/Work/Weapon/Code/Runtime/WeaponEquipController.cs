using UnityEngine;
using Work.Combat.Code.Core;
using Work.Weapon.Code.Core;

namespace Work.Weapon.Code.Runtime
{
    /// <summary>
    /// 현재 장착 무기 상태와 무기 교체 처리 컴포넌트
    /// </summary>
    public sealed class WeaponEquipController : MonoBehaviour
    {
        [SerializeField]
        private WeaponDataSO defaultWeapon;

        private WeaponDataSO _currentWeapon;

        /// <summary>
        /// 현재 장착 무기
        /// </summary>
        public WeaponDataSO CurrentWeapon => _currentWeapon;

        /// <summary>
        /// 현재 장착 무기의 일반 공격 데이터
        /// </summary>
        public AttackDataSO CurrentAttackData => _currentWeapon != null ? _currentWeapon.NormalAttackData : null;

        /// <summary>
        /// 무기 장착 여부
        /// </summary>
        public bool HasWeapon => _currentWeapon != null;

        private void Awake()
        {
            if (defaultWeapon != null)
            {
                EquipWeapon(defaultWeapon);
            }
        }

        /// <summary>
        /// 지정 무기 장착
        /// </summary>
        /// <param name="weaponData">장착할 무기 데이터</param>
        /// <returns>장착 성공 여부</returns>
        public bool EquipWeapon(WeaponDataSO weaponData)
        {
            if (weaponData == null)
            {
                LogMissingWeaponData();
                return false;
            }

            _currentWeapon = weaponData;
            return true;
        }

        /// <summary>
        /// 현재 장착 무기 해제
        /// </summary>
        public void ClearWeapon()
        {
            _currentWeapon = null;
        }

        /// <summary>
        /// 현재 장착 무기에서 공격 데이터 조회
        /// </summary>
        /// <param name="attackType">무기 공격 입력 타입</param>
        /// <param name="attackData">조회된 공격 데이터</param>
        /// <returns>조회 성공 여부</returns>
        public bool TryGetAttackData(WeaponAttackType attackType, out AttackDataSO attackData)
        {
            attackData = null;

            if (_currentWeapon == null)
            {
                LogMissingCurrentWeapon();
                return false;
            }

            attackData = _currentWeapon.GetAttackData(attackType);

            if (attackData == null)
            {
                LogMissingAttackData(_currentWeapon, attackType);
                return false;
            }

            return true;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingWeaponData()
        {
            Debug.LogError($"{nameof(WeaponDataSO)} is missing. Equip failed.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingCurrentWeapon()
        {
            Debug.LogError($"{nameof(CurrentWeapon)} is missing.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingAttackData(WeaponDataSO weaponData, WeaponAttackType attackType)
        {
            Debug.LogError($"{weaponData.name} has no {attackType} attack data.", this);
        }
    }
}
