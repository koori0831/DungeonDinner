using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Work.Combat.Code.Core;
using Work.Combat.Code.Runtime;

namespace Work.Combat.Code.Test
{
    /// <summary>
    /// 전투 피격 판정 범위와 실제 피격 실행을 확인하는 런타임 테스트 도구.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitboxTestTool : MonoBehaviour
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        private const float MIN_GIZMO_SIZE = 0.001f;
        private const int DEFAULT_HIT_RESULT_CAPACITY = 16;

        [Header("References")]
        [SerializeField]
        private AttackDataSO attackData;

        [SerializeField]
        private MonoBehaviour hitCasterBehaviour;

        [SerializeField]
        private CombatAttackExecutor attackExecutor;

        [SerializeField]
        private Transform attackOrigin;

        [SerializeField]
        private GameObject owner;

        [SerializeField]
        private GameObject attacker;

        [Header("Cast")]
        [SerializeField]
        private LayerMask targetLayerMask = ~0;

        [SerializeField]
        [Min(1)]
        private int maxHitResults = DEFAULT_HIT_RESULT_CAPACITY;

        [Header("Input")]
        [SerializeField]
        private bool previewEveryFrame;

        [SerializeField]
        private Key previewKey = Key.H;

        [SerializeField]
        private Key executeKey = Key.J;

        [Header("Console")]
        [SerializeField]
        private bool logResultsToConsole = true;

        [Header("Gizmos")]
        [SerializeField]
        private bool drawGizmos = true;

        [SerializeField]
        private Color castColor = new Color(0.2f, 0.8f, 1f, 0.65f);

        [SerializeField]
        private Color hitPointColor = new Color(1f, 0.85f, 0.15f, 1f);

        [SerializeField]
        [Min(MIN_GIZMO_SIZE)]
        private float hitPointRadius = 0.08f;

        [SerializeField]
        [Min(MIN_GIZMO_SIZE)]
        private float hitDirectionLength = 0.45f;

        [Header("Last Result")]
        [SerializeField]
        private int lastHitCount;

        [SerializeField]
        private bool lastWasExecution;

        [SerializeField]
        private bool lastHasAnyHit;

        [SerializeField]
        private int lastHitSuccessCount;

        [SerializeField]
        private int lastKilledCount;

        [SerializeField]
        private HitResultType lastHitResultType;

        [SerializeField]
        [TextArea(3, 8)]
        private string lastHitSummary;

        private IHitCaster _hitCaster;
        private HitCastResult[] _hitResults;
        private AttackExecutionResult _lastExecutionResult;

        /// <summary>
        /// 마지막 판정에서 감지된 대상 수.
        /// </summary>
        public int LastHitCount => lastHitCount;

        /// <summary>
        /// 마지막 판정이 실제 피격 실행까지 포함했는지 여부.
        /// </summary>
        public bool LastWasExecution => lastWasExecution;

        /// <summary>
        /// 마지막 실제 피격 실행 결과.
        /// </summary>
        public AttackExecutionResult LastExecutionResult => _lastExecutionResult;

        private void Awake()
        {
            EnsureHitResultBuffer();
            ResolveReferences();
        }

        private void Update()
        {
            if (previewEveryFrame == true)
            {
                CastOnly(false);
            }

            if (WasPressedThisFrame(previewKey) == true)
            {
                PreviewCast();
            }

            if (WasPressedThisFrame(executeKey) == true)
            {
                ExecuteHit();
            }
        }

        /// <summary>
        /// 실제 피격 처리 없이 현재 공격 판정에 들어오는 대상만 확인.
        /// </summary>
        /// <returns>감지된 피격 가능 대상 수.</returns>
        public int PreviewCast()
        {
            int hitCount = CastOnly(true);
            LogPreviewResult();
            return hitCount;
        }

        /// <summary>
        /// 현재 공격 판정을 미리 기록한 뒤 실제 피격 처리 실행.
        /// </summary>
        /// <returns>공격 실행 결과.</returns>
        public AttackExecutionResult ExecuteHit()
        {
            CastOnly(true);
            lastWasExecution = true;

            ResolveReferences();

            if (attackExecutor == null)
            {
                LogMissingAttackExecutor();
                return _lastExecutionResult;
            }

            if (attackData == null)
            {
                LogMissingAttackData();
                return _lastExecutionResult;
            }

            AttackExecutionRequest request = new AttackExecutionRequest(
                GetAttacker(),
                GetOwner(),
                attackData,
                GetAttackOriginPosition(),
                GetAttackDirection(),
                targetLayerMask
            );

            _lastExecutionResult = attackExecutor.ExecuteAttack(in request);
            lastHasAnyHit = _lastExecutionResult.HasAnyHit;
            lastHitSuccessCount = _lastExecutionResult.HitSuccessCount;
            lastKilledCount = _lastExecutionResult.KilledCount;
            lastHitResultType = _lastExecutionResult.LastHitResult.ResultType;
            AppendExecutionSummary();
            LogExecutionResult();
            return _lastExecutionResult;
        }

        [ContextMenu("Preview Cast")]
        private void PreviewCastFromContextMenu()
        {
            PreviewCast();
        }

        [ContextMenu("Execute Hit")]
        private void ExecuteHitFromContextMenu()
        {
            ExecuteHit();
        }

        /// <summary>
        /// 마지막 판정 결과 표시값 초기화.
        /// </summary>
        [ContextMenu("Clear Last Result")]
        public void ClearLastResult()
        {
            lastHitCount = 0;
            lastWasExecution = false;
            lastHasAnyHit = false;
            lastHitSuccessCount = 0;
            lastKilledCount = 0;
            lastHitResultType = HitResultType.None;
            lastHitSummary = string.Empty;
            _lastExecutionResult = CreateEmptyExecutionResult();
        }

        private int CastOnly(bool logErrors)
        {
            EnsureHitResultBuffer();
            ResolveReferences();
            ResetLastCastResult();

            if (_hitCaster == null)
            {
                if (logErrors == true)
                {
                    LogMissingHitCaster();
                }

                UpdateCastSummary();
                return lastHitCount;
            }

            if (attackData == null)
            {
                if (logErrors == true)
                {
                    LogMissingAttackData();
                }

                UpdateCastSummary();
                return lastHitCount;
            }

            HitCastRequest request = new HitCastRequest(
                GetOwner(),
                GetAttackOriginPosition(),
                GetAttackDirection(),
                attackData.Range,
                attackData.Radius,
                targetLayerMask
            );

            lastHitCount = _hitCaster.Cast(in request, _hitResults);
            UpdateCastSummary();
            return lastHitCount;
        }

        private void EnsureHitResultBuffer()
        {
            int capacity = Mathf.Max(1, maxHitResults);

            if (_hitResults != null && _hitResults.Length == capacity)
            {
                return;
            }

            _hitResults = new HitCastResult[capacity];
        }

        private void ResolveReferences()
        {
            if (hitCasterBehaviour == null)
            {
                hitCasterBehaviour = FindHitCasterBehaviourOnSelf();
            }

            _hitCaster = hitCasterBehaviour as IHitCaster;

            if (attackExecutor == null)
            {
                attackExecutor = GetComponent<CombatAttackExecutor>();
            }
        }

        private static bool WasPressedThisFrame(Key key)
        {
            if (key == Key.None)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return false;
            }

            KeyControl keyControl = keyboard[key];
            return keyControl != null && keyControl.wasPressedThisFrame == true;
        }

        private MonoBehaviour FindHitCasterBehaviourOnSelf()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];

                if (behaviour is IHitCaster)
                {
                    return behaviour;
                }
            }

            return null;
        }

        private void ResetLastCastResult()
        {
            lastHitCount = 0;
            lastWasExecution = false;
            lastHasAnyHit = false;
            lastHitSuccessCount = 0;
            lastKilledCount = 0;
            lastHitResultType = HitResultType.None;
            _lastExecutionResult = CreateEmptyExecutionResult();
        }

        private AttackExecutionResult CreateEmptyExecutionResult()
        {
            return new AttackExecutionResult(0, 0, new HitResult(false, false, HitResultType.None), false);
        }

        private GameObject GetOwner()
        {
            return owner != null ? owner : gameObject;
        }

        private GameObject GetAttacker()
        {
            return attacker != null ? attacker : gameObject;
        }

        private Vector3 GetAttackOriginPosition()
        {
            Transform originTransform = attackOrigin != null ? attackOrigin : transform;
            return originTransform.position;
        }

        private Vector3 GetAttackDirection()
        {
            Transform originTransform = attackOrigin != null ? attackOrigin : transform;
            Vector3 direction = originTransform.forward;

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return transform.forward;
            }

            return direction;
        }

        private void UpdateCastSummary()
        {
            if (lastHitCount <= 0)
            {
                lastHitSummary = "No hit";
                return;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < lastHitCount; i++)
            {
                HitCastResult hitResult = _hitResults[i];
                Collider targetCollider = hitResult.TargetCollider;
                string colliderName = targetCollider != null ? targetCollider.name : "Missing Collider";
                string hitableName = GetHitableName(hitResult.Hitable);

                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(hitableName);
                builder.Append(" / ");
                builder.Append(colliderName);
                builder.Append(" / point ");
                builder.Append(hitResult.HitPoint.ToString("F2"));
                builder.AppendLine();
            }

            lastHitSummary = builder.ToString();
        }

        private void AppendExecutionSummary()
        {
            StringBuilder builder = new StringBuilder(lastHitSummary);

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append("Executed: success=");
            builder.Append(lastHitSuccessCount);
            builder.Append(", killed=");
            builder.Append(lastKilledCount);
            builder.Append(", result=");
            builder.Append(lastHitResultType);
            builder.Append(", anyHit=");
            builder.Append(lastHasAnyHit);
            lastHitSummary = builder.ToString();
        }

        private void LogPreviewResult()
        {
            if (logResultsToConsole == false)
            {
                return;
            }

            Debug.Log(CreatePreviewLogMessage(), this);
        }

        private void LogExecutionResult()
        {
            if (logResultsToConsole == false)
            {
                return;
            }

            Debug.Log(CreateExecutionLogMessage(), this);
        }

        private string CreatePreviewLogMessage()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[CombatHitboxTestTool] Preview Cast | tester=");
            builder.Append(name);
            builder.Append(" | attack=");
            builder.Append(GetAttackDataName());
            builder.Append(" | hitCount=");
            builder.Append(lastHitCount);
            builder.AppendLine();
            builder.Append(lastHitSummary);
            return builder.ToString();
        }

        private string CreateExecutionLogMessage()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[CombatHitboxTestTool] Execute Hit | tester=");
            builder.Append(name);
            builder.Append(" | attack=");
            builder.Append(GetAttackDataName());
            builder.Append(" | detected=");
            builder.Append(lastHitCount);
            builder.Append(" | success=");
            builder.Append(lastHitSuccessCount);
            builder.Append(" | killed=");
            builder.Append(lastKilledCount);
            builder.Append(" | result=");
            builder.Append(lastHitResultType);
            builder.Append(" | anyHit=");
            builder.Append(lastHasAnyHit);
            builder.AppendLine();
            builder.Append(lastHitSummary);
            return builder.ToString();
        }

        private string GetAttackDataName()
        {
            return attackData != null ? attackData.name : "Missing AttackData";
        }

        private static string GetHitableName(IHitable hitable)
        {
            if (hitable == null)
            {
                return "Missing Hitable";
            }

            if (hitable is Component hitableComponent)
            {
                return hitableComponent.name;
            }

            return hitable.GetType().Name;
        }

        private void OnValidate()
        {
            maxHitResults = Mathf.Max(1, maxHitResults);
            hitPointRadius = Mathf.Max(MIN_GIZMO_SIZE, hitPointRadius);
            hitDirectionLength = Mathf.Max(MIN_GIZMO_SIZE, hitDirectionLength);
        }

        private void OnDrawGizmosSelected()
        {
            if (drawGizmos == false || attackData == null)
            {
                return;
            }

            DrawCastGizmo();
            DrawHitResultGizmos();
        }

        private void DrawCastGizmo()
        {
            Vector3 startPoint = GetAttackOriginPosition();
            Vector3 direction = GetAttackDirection();

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            float range = Mathf.Max(0f, attackData.Range);
            float radius = Mathf.Max(0f, attackData.Radius);
            Vector3 endPoint = startPoint + direction * range;

            Gizmos.color = castColor;
            Gizmos.DrawWireSphere(startPoint, radius);
            Gizmos.DrawWireSphere(endPoint, radius);

            Vector3 side = Vector3.Cross(direction, Vector3.up);

            if (side.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                side = Vector3.Cross(direction, Vector3.right);
            }

            side.Normalize();
            Vector3 up = Vector3.Cross(side, direction).normalized;
            DrawCapsuleSideLines(startPoint, endPoint, side * radius, up * radius);
        }

        private static void DrawCapsuleSideLines(Vector3 startPoint, Vector3 endPoint, Vector3 side, Vector3 up)
        {
            Gizmos.DrawLine(startPoint + side, endPoint + side);
            Gizmos.DrawLine(startPoint - side, endPoint - side);
            Gizmos.DrawLine(startPoint + up, endPoint + up);
            Gizmos.DrawLine(startPoint - up, endPoint - up);
        }

        private void DrawHitResultGizmos()
        {
            if (_hitResults == null)
            {
                return;
            }

            Gizmos.color = hitPointColor;

            for (int i = 0; i < lastHitCount && i < _hitResults.Length; i++)
            {
                HitCastResult hitResult = _hitResults[i];
                Vector3 hitDirection = hitResult.HitDirection;

                if (hitDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
                {
                    hitDirection = GetAttackDirection();
                }

                hitDirection.Normalize();
                Gizmos.DrawSphere(hitResult.HitPoint, hitPointRadius);
                Gizmos.DrawLine(hitResult.HitPoint, hitResult.HitPoint + hitDirection * hitDirectionLength);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingHitCaster()
        {
            Debug.LogError($"{nameof(hitCasterBehaviour)} must implement {nameof(IHitCaster)}.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingAttackExecutor()
        {
            Debug.LogError($"{nameof(CombatAttackExecutor)} is missing. Execute hit stopped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingAttackData()
        {
            Debug.LogError($"{nameof(attackData)} is missing. Hitbox test stopped.", this);
        }
    }
}
