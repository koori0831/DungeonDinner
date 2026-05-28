using UnityEngine;
using Work.Combat.Code.Core;
using Work.Combat.Code.Runtime;
using Work.Weapon.Code.Core;

namespace Work.Weapon.Code.Runtime
{
    /// <summary>
    /// 장착 무기의 공격 데이터를 전투 공격 실행기로 전달하는 컴포넌트
    /// </summary>
    public sealed class WeaponAttackController : MonoBehaviour
    {
        [SerializeField]
        private WeaponEquipController weaponEquipController;

        [SerializeField]
        private CombatAttackExecutor combatAttackExecutor;

        [SerializeField]
        private WeaponAttackType defaultAttackType = WeaponAttackType.Normal;

        private AttackDataSO _capturedAttackData;

        private bool _hasCapturedAttackData;

        private void Awake()
        {
            if (weaponEquipController == null)
            {
                weaponEquipController = GetComponent<WeaponEquipController>();
            }

            if (combatAttackExecutor == null)
            {
                combatAttackExecutor = GetComponent<CombatAttackExecutor>();
            }

            ValidateReferences();
        }

        /// <summary>
        /// 기본 공격 타입으로 공격 데이터 캡처
        /// </summary>
        /// <returns>캡처 성공 여부</returns>
        public bool BeginAttack()
        {
            return BeginAttack(defaultAttackType);
        }

        /// <summary>
        /// 지정 공격 타입으로 공격 데이터 캡처
        /// </summary>
        /// <param name="attackType">무기 공격 입력 타입</param>
        /// <returns>캡처 성공 여부</returns>
        public bool BeginAttack(WeaponAttackType attackType)
        {
            _capturedAttackData = null;
            _hasCapturedAttackData = false;

            if (weaponEquipController == null)
            {
                LogMissingWeaponEquipController();
                return false;
            }

            if (weaponEquipController.TryGetAttackData(attackType, out AttackDataSO attackData) == false)
            {
                return false;
            }

            _capturedAttackData = attackData;
            _hasCapturedAttackData = true;
            return true;
        }

        /// <summary>
        /// 캡처된 공격 데이터로 공격 실행
        /// </summary>
        /// <returns>공격 실행 결과</returns>
        public AttackExecutionResult ExecuteCapturedAttack()
        {
            if (_hasCapturedAttackData == false || _capturedAttackData == null)
            {
                LogMissingCapturedAttackData();
                return CreateEmptyResult();
            }

            return ExecuteAttack(_capturedAttackData);
        }

        /// <summary>
        /// 현재 장착 무기의 기본 공격 타입으로 즉시 공격 실행
        /// </summary>
        /// <returns>공격 실행 결과</returns>
        public AttackExecutionResult ExecuteCurrentWeaponAttack()
        {
            return ExecuteCurrentWeaponAttack(defaultAttackType);
        }

        /// <summary>
        /// 현재 장착 무기의 지정 공격 타입으로 즉시 공격 실행
        /// </summary>
        /// <param name="attackType">무기 공격 입력 타입</param>
        /// <returns>공격 실행 결과</returns>
        public AttackExecutionResult ExecuteCurrentWeaponAttack(WeaponAttackType attackType)
        {
            if (BeginAttack(attackType) == false)
            {
                return CreateEmptyResult();
            }

            return ExecuteCapturedAttack();
        }

        private AttackExecutionResult ExecuteAttack(AttackDataSO attackData)
        {
            if (combatAttackExecutor == null)
            {
                LogMissingCombatAttackExecutor();
                return CreateEmptyResult();
            }

            return combatAttackExecutor.ExecuteAttack(attackData);
        }

        private static AttackExecutionResult CreateEmptyResult()
        {
            return new AttackExecutionResult(
                0,
                0,
                new HitResult(false, false, HitResultType.None),
                false
            );
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void ValidateReferences()
        {
            if (weaponEquipController == null)
            {
                LogMissingWeaponEquipController();
            }

            if (combatAttackExecutor == null)
            {
                LogMissingCombatAttackExecutor();
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingWeaponEquipController()
        {
            Debug.LogError($"{nameof(WeaponEquipController)} is missing.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingCombatAttackExecutor()
        {
            Debug.LogError($"{nameof(CombatAttackExecutor)} is missing.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingCapturedAttackData()
        {
            Debug.LogError($"{nameof(_capturedAttackData)} is missing. Call {nameof(BeginAttack)} before hit frame.", this);
        }
    }
}
