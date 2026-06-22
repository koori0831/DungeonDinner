using System.Collections.Generic;
using UnityEngine;
using Work.Core.EventBus;
using Work.Enemy.Code;
using Work.Players.Code;

namespace Work.Combat.Code.Test
{
    /// <summary>
    /// CombatTest 씬에서 Player 등록 이후 Enemy를 생성하는 테스트 전용 스포너
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatTestSceneSpawner : MonoBehaviour
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        private const float MIN_GIZMO_RADIUS = 0.01f;

        [Header("Enemy")]
        [SerializeField]
        private EnemyBase enemyPrefab;

        [SerializeField]
        private Transform[] enemySpawnPoints;

        [SerializeField]
        private Transform enemyRoot;

        [Header("Options")]
        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField]
        private bool spawnOnPlayerRegistered = true;

        [SerializeField]
        private bool respawnOnPlayerRegistered;

        [SerializeField]
        private bool clearBeforeSpawn = true;

        [SerializeField]
        private bool facePlayerOnSpawn = true;

        [SerializeField]
        private bool requireRegisteredPlayer = true;

        [SerializeField]
        private bool logSpawns = true;

        [Header("Gizmos")]
        [SerializeField]
        private Color spawnPointColor = new Color(1f, 0.45f, 0.1f, 0.85f);

        [SerializeField]
        [Min(MIN_GIZMO_RADIUS)]
        private float spawnPointGizmoRadius = 0.35f;

        [Header("Last Spawn")]
        [SerializeField]
        private int lastSpawnCount;

        private readonly List<EnemyBase> SPAWNED_ENEMIES = new List<EnemyBase>();
        private Transform _playerTarget;
        private bool _hasSpawned;

        /// <summary>
        /// 마지막 생성 Enemy 수
        /// </summary>
        public int LastSpawnCount => lastSpawnCount;

        private void OnEnable()
        {
            Bus<PlayerTargetChangedEvent>.Events += HandlePlayerTargetChanged;
        }

        private void OnDisable()
        {
            Bus<PlayerTargetChangedEvent>.Events -= HandlePlayerTargetChanged;
        }

        private void Start()
        {
            if (spawnOnStart == false)
            {
                return;
            }

            TrySpawnEnemiesWhenReady(false);
        }

        /// <summary>
        /// 테스트 Enemy 생성
        /// </summary>
        /// <returns>생성된 Enemy 수</returns>
        public int SpawnEnemies()
        {
            lastSpawnCount = 0;
            _hasSpawned = false;

            if (clearBeforeSpawn == true)
            {
                ClearSpawnedEnemies();
            }

            if (enemyPrefab == null)
            {
                LogMissingEnemyPrefab();
                return lastSpawnCount;
            }

            Transform playerTarget = GetPlayerTarget();

            if (playerTarget == null && requireRegisteredPlayer == true)
            {
                LogMissingPlayerTarget();
                return lastSpawnCount;
            }

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                SpawnEnemyAt(transform.position, GetSpawnRotation(transform.position, transform.rotation, playerTarget), playerTarget);
                LogSpawnResult();
                _hasSpawned = lastSpawnCount > 0;
                return lastSpawnCount;
            }

            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                Transform spawnPoint = enemySpawnPoints[i];

                if (spawnPoint == null)
                {
                    continue;
                }

                Quaternion spawnRotation = GetSpawnRotation(spawnPoint.position, spawnPoint.rotation, playerTarget);
                SpawnEnemyAt(spawnPoint.position, spawnRotation, playerTarget);
            }

            LogSpawnResult();
            _hasSpawned = lastSpawnCount > 0;
            return lastSpawnCount;
        }

        [ContextMenu("Spawn Enemies")]
        private void SpawnEnemiesFromContextMenu()
        {
            SpawnEnemies();
        }

        /// <summary>
        /// 테스트 스포너가 생성한 Enemy 제거
        /// </summary>
        [ContextMenu("Clear Spawned Enemies")]
        public void ClearSpawnedEnemies()
        {
            for (int i = SPAWNED_ENEMIES.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = SPAWNED_ENEMIES[i];

                if (enemy == null)
                {
                    continue;
                }

                DestroyEnemy(enemy.gameObject);
            }

            SPAWNED_ENEMIES.Clear();
            lastSpawnCount = 0;
            _hasSpawned = false;
        }

        private void TrySpawnEnemiesWhenReady(bool forceRespawn)
        {
            if (_hasSpawned == true && forceRespawn == false)
            {
                return;
            }

            Transform playerTarget = GetPlayerTarget();

            if (playerTarget == null && requireRegisteredPlayer == true)
            {
                return;
            }

            SpawnEnemies();
        }

        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent evt)
        {
            if (evt.IsRegistered == false)
            {
                if (_playerTarget == evt.Target)
                {
                    _playerTarget = null;
                    ApplyPlayerTargetToSpawnedEnemies(null);
                }

                return;
            }

            if (IsValidPlayerTarget(evt.Target) == false)
            {
                return;
            }

            _playerTarget = evt.Target;
            ApplyPlayerTargetToSpawnedEnemies(_playerTarget);

            if (spawnOnPlayerRegistered == false)
            {
                return;
            }

            TrySpawnEnemiesWhenReady(respawnOnPlayerRegistered);
        }

        private void SpawnEnemyAt(Vector3 position, Quaternion rotation, Transform playerTarget)
        {
            EnemyBase enemy = Instantiate(enemyPrefab, position, rotation, enemyRoot);

            if (enemy == null)
            {
                return;
            }

            ApplyPlayerTargetToEnemy(enemy, playerTarget);
            SPAWNED_ENEMIES.Add(enemy);
            lastSpawnCount++;
        }

        private Transform GetPlayerTarget()
        {
            if (IsValidPlayerTarget(_playerTarget) == false)
            {
                _playerTarget = null;
                return null;
            }

            return _playerTarget;
        }

        private void ApplyPlayerTargetToSpawnedEnemies(Transform playerTarget)
        {
            for (int i = 0; i < SPAWNED_ENEMIES.Count; i++)
            {
                EnemyBase enemy = SPAWNED_ENEMIES[i];

                if (enemy == null)
                {
                    continue;
                }

                ApplyPlayerTargetToEnemy(enemy, playerTarget);
            }
        }

        private void ApplyPlayerTargetToEnemy(EnemyBase enemy, Transform playerTarget)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyTargetingModule targetingModule = null;

            if (enemy.TryGetModule<EnemyTargetingModule>(out targetingModule, true) == false)
            {
                targetingModule = enemy.GetComponentInChildren<EnemyTargetingModule>(true);
            }

            if (targetingModule == null)
            {
                LogMissingEnemyTargetingModule(enemy);
                return;
            }

            targetingModule.SetKnownTarget(playerTarget);
        }

        private static bool IsValidPlayerTarget(Transform playerTarget)
        {
            return playerTarget != null && playerTarget.gameObject.activeInHierarchy == true;
        }

        private Quaternion GetSpawnRotation(Vector3 spawnPosition, Quaternion fallbackRotation, Transform playerTarget)
        {
            if (facePlayerOnSpawn == false || playerTarget == null)
            {
                return fallbackRotation;
            }

            Vector3 direction = playerTarget.position - spawnPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return fallbackRotation;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void DestroyEnemy(GameObject enemyObject)
        {
            if (enemyObject == null)
            {
                return;
            }

            if (Application.isPlaying == true)
            {
                Destroy(enemyObject);
                return;
            }

            DestroyImmediate(enemyObject);
        }

        private void OnValidate()
        {
            spawnPointGizmoRadius = Mathf.Max(MIN_GIZMO_RADIUS, spawnPointGizmoRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = spawnPointColor;

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                Gizmos.DrawWireSphere(transform.position, spawnPointGizmoRadius);
                return;
            }

            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                Transform spawnPoint = enemySpawnPoints[i];

                if (spawnPoint == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(spawnPoint.position, spawnPointGizmoRadius);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogSpawnResult()
        {
            if (logSpawns == false)
            {
                return;
            }

            Debug.Log($"{nameof(CombatTestSceneSpawner)} spawned enemies. count={lastSpawnCount}", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingEnemyPrefab()
        {
            Debug.LogError($"{nameof(enemyPrefab)} is missing. Enemy spawn stopped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingPlayerTarget()
        {
            Debug.LogError($"{nameof(PlayerTargetChangedEvent)} has no registered target. Enemy spawn stopped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingEnemyTargetingModule(EnemyBase enemy)
        {
            Debug.LogError($"{nameof(EnemyTargetingModule)} is missing on spawned enemy.", enemy);
        }
    }
}
