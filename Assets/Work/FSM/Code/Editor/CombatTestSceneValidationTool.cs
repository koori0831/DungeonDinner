#if UNITY_EDITOR
using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Work.Combat.Code.Runtime;
using Work.Enemy.Code;
using Work.Entities.Code;
using Work.Players.Code;

namespace Work.FSM.Editor
{
    /// <summary>
    /// CombatTest 씬 정리 결과 검증 임시 도구.
    /// </summary>
    [InitializeOnLoad]
    public static class CombatTestSceneValidationTool
    {
        private const string SCENE_PATH = "Assets/Work/Combat/Scene/CombatTest.unity";
        private const string PREFAB_PATH = "Assets/Work/Enemy/Prefab/SlimeEnemy.prefab";
        private const string RUNNING_KEY = "CombatTestSceneValidationTool.Running";
        private const string FAILED_KEY = "CombatTestSceneValidationTool.Failed";
        private const string FAILURE_KEY = "CombatTestSceneValidationTool.Failure";
        private const string FRAME_COUNT_KEY = "CombatTestSceneValidationTool.FrameCount";
        private const string INITIAL_DISTANCE_KEY = "CombatTestSceneValidationTool.InitialDistance";
        private const string CHASE_VALIDATED_KEY = "CombatTestSceneValidationTool.ChaseValidated";
        private const int CHASE_VALIDATION_FRAME = 90;
        private const int PLAY_FRAME_COUNT = 180;
        private const float MIN_CHASE_PROGRESS = 0.05f;
        private const float ATTACK_DISTANCE_TOLERANCE = 0.35f;
        private const float NAV_MESH_SAMPLE_DISTANCE = 1f;

        static CombatTestSceneValidationTool()
        {
            if (SessionState.GetBool(RUNNING_KEY, false) == true)
            {
                Subscribe();
            }
        }

        /// <summary>
        /// CombatTest 씬 구조와 플레이모드 180프레임 검증 실행.
        /// </summary>
        public static void Run()
        {
            SessionState.SetBool(RUNNING_KEY, true);
            SessionState.SetBool(FAILED_KEY, false);
            SessionState.SetString(FAILURE_KEY, string.Empty);
            SessionState.SetInt(FRAME_COUNT_KEY, 0);
            SessionState.SetFloat(INITIAL_DISTANCE_KEY, -1f);
            SessionState.SetBool(CHASE_VALIDATED_KEY, false);

            Subscribe();

            try
            {
                EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                ValidateSceneSetup();
                Debug.Log("CombatTest scene structure validation passed.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail($"Scene validation failed. {exception}");
            }
        }

        private static void Subscribe()
        {
            Application.logMessageReceived -= HandleLogMessage;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= UpdatePlayMode;

            Application.logMessageReceived += HandleLogMessage;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update += UpdatePlayMode;
        }

        private static void Cleanup()
        {
            Application.logMessageReceived -= HandleLogMessage;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= UpdatePlayMode;

            SessionState.EraseBool(RUNNING_KEY);
            SessionState.EraseBool(FAILED_KEY);
            SessionState.EraseString(FAILURE_KEY);
            SessionState.EraseInt(FRAME_COUNT_KEY);
            SessionState.EraseFloat(INITIAL_DISTANCE_KEY);
            SessionState.EraseBool(CHASE_VALIDATED_KEY);
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (SessionState.GetBool(RUNNING_KEY, false) == false)
            {
                return;
            }

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            Fail($"Unity log {type}: {condition}\n{stackTrace}");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (SessionState.GetBool(RUNNING_KEY, false) == false || stateChange != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            bool failed = SessionState.GetBool(FAILED_KEY, false);
            int frameCount = SessionState.GetInt(FRAME_COUNT_KEY, 0);

            if (failed == false && frameCount < PLAY_FRAME_COUNT)
            {
                SessionState.SetBool(FAILED_KEY, true);
                SessionState.SetString(FAILURE_KEY, $"Play mode exited early at frame {frameCount}.");
                failed = true;
            }

            string failure = SessionState.GetString(FAILURE_KEY, string.Empty);
            Cleanup();

            if (failed == true)
            {
                Debug.Log($"CombatTest scene validation failed. {failure}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"CombatTest scene playmode validation passed for {PLAY_FRAME_COUNT} frames.");
            EditorApplication.Exit(0);
        }

        private static void UpdatePlayMode()
        {
            if (SessionState.GetBool(RUNNING_KEY, false) == false || EditorApplication.isPlaying == false)
            {
                return;
            }

            if (SessionState.GetBool(FAILED_KEY, false) == true)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            int frameCount = SessionState.GetInt(FRAME_COUNT_KEY, 0) + 1;
            SessionState.SetInt(FRAME_COUNT_KEY, frameCount);

            try
            {
                if (frameCount == 1)
                {
                    RecordInitialChaseDistance();
                }

                if (frameCount >= CHASE_VALIDATION_FRAME && SessionState.GetBool(CHASE_VALIDATED_KEY, false) == false)
                {
                    ValidateChaseProgress();
                }
            }
            catch (Exception exception)
            {
                Fail($"Runtime validation failed. {exception}");
                return;
            }

            if (frameCount >= PLAY_FRAME_COUNT)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateSceneSetup()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null)
            {
                throw new InvalidOperationException($"Prefab is missing: {PREFAB_PATH}");
            }

            if (GameObject.Find("RabbitSlime") != null || GameObject.Find("ChickSlime") != null || GameObject.Find("MinsuSlime") != null)
            {
                throw new InvalidOperationException("Old slime objects must be removed.");
            }

            if (GameObject.Find("CombatTestRunner") != null)
            {
                throw new InvalidOperationException("CombatTestRunner must be removed.");
            }

            EnemyBase[] enemies = UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (enemies.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one enemy, found {enemies.Length}.");
            }

            ValidateSlime(enemies[0]);
            ValidatePlayer();
            ValidateNavMeshSurface();
        }

        private static void ValidateSlime(EnemyBase enemy)
        {
            GameObject root = enemy.gameObject;
            RequireRootComponent<EnemyStateController>(root);
            RequireRootComponent<NavMeshAgent>(root);
            RequireRootComponent<EnemyMovementModule>(root);

            if (root.GetComponent<CharacterController>() != null)
            {
                throw new InvalidOperationException("SlimeEnemy must not use CharacterController.");
            }

            RequireChildComponent<EntityStateModule>(root, "AI");
            RequireChildComponent<EnemyTerritoryModule>(root, "AI");
            RequireChildComponent<EnemyTargetingModule>(root, "AI");
            RequireChildComponent<EnemyCombatModule>(root, "Combat");
            RequireChildComponent<CombatAttackExecutor>(root, "Combat");
            RequireChildComponent<HitCaster>(root, "Combat");
            RequireChildComponent<EnemyHitable>(root, "Hurtbox");
            RequireChildComponent<EnemyHitReaction>(root, "Hurtbox");
            RequireChildComponent<EnemyDeathHandler>(root, "Hurtbox");
            RequireChildComponent<EnemyKillConditionResolver>(root, "Hurtbox");
            RequireChildComponent<SlimeDynamicBoneAnimator>(root, "Visual");

            Transform hitbox = root.transform.Find("Hurtbox/Hitbox");

            if (hitbox == null || hitbox.GetComponent<Collider>() == null || hitbox.GetComponent<Collider>().isTrigger == false)
            {
                throw new InvalidOperationException("Hurtbox/Hitbox trigger collider is invalid.");
            }

            if (NavMesh.SamplePosition(root.transform.position, out NavMeshHit hit, NAV_MESH_SAMPLE_DISTANCE, NavMesh.AllAreas) == false)
            {
                throw new InvalidOperationException("SlimeEnemy must be placed on the NavMesh.");
            }

            _ = hit;
        }

        private static void ValidatePlayer()
        {
            GameObject player = GameObject.Find("CombatTestPlayer");

            if (player == null)
            {
                throw new InvalidOperationException("CombatTestPlayer is missing.");
            }

            RequireRootComponent<Player>(player);
            RequireRootComponent<CharacterController>(player);
            RequireRootComponent<EntityMovementModule>(player);

            if (NavMesh.SamplePosition(player.transform.position, out NavMeshHit hit, NAV_MESH_SAMPLE_DISTANCE, NavMesh.AllAreas) == false)
            {
                throw new InvalidOperationException("CombatTestPlayer must be placed on the NavMesh.");
            }

            _ = hit;
        }

        private static void ValidateNavMeshSurface()
        {
            NavMeshSurface[] surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (surfaces.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one NavMeshSurface, found {surfaces.Length}.");
            }

            if (surfaces[0].navMeshData == null)
            {
                throw new InvalidOperationException("CombatTest NavMeshSurface has no NavMeshData.");
            }
        }

        private static void RecordInitialChaseDistance()
        {
            EnemyBase enemy = FindRuntimeEnemy();
            GameObject player = FindRuntimePlayer();
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

            if (agent == null || agent.isOnNavMesh == false)
            {
                throw new InvalidOperationException("SlimeEnemy NavMeshAgent is not on the NavMesh during playmode.");
            }

            SessionState.SetFloat(INITIAL_DISTANCE_KEY, GetHorizontalDistance(enemy.transform.position, player.transform.position));
        }

        private static void ValidateChaseProgress()
        {
            EnemyBase enemy = FindRuntimeEnemy();
            GameObject player = FindRuntimePlayer();
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

            if (agent == null || agent.isOnNavMesh == false)
            {
                throw new InvalidOperationException("SlimeEnemy NavMeshAgent left the NavMesh during playmode.");
            }

            if (enemy.Target != player.transform)
            {
                throw new InvalidOperationException("SlimeEnemy did not acquire CombatTestPlayer as a target.");
            }

            if (agent.hasPath == true && agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                throw new InvalidOperationException("SlimeEnemy generated an invalid NavMesh path.");
            }

            float initialDistance = SessionState.GetFloat(INITIAL_DISTANCE_KEY, -1f);
            float currentDistance = GetHorizontalDistance(enemy.transform.position, player.transform.position);
            bool isNearAttackRange = currentDistance <= enemy.AttackDistance + ATTACK_DISTANCE_TOLERANCE;
            bool hasMovedCloser = initialDistance >= 0f && currentDistance <= initialDistance - MIN_CHASE_PROGRESS;

            if (isNearAttackRange == false && hasMovedCloser == false)
            {
                throw new InvalidOperationException($"SlimeEnemy did not chase the player. initial={initialDistance}, current={currentDistance}.");
            }

            if (isNearAttackRange == false && agent.hasPath == false)
            {
                throw new InvalidOperationException("SlimeEnemy is not in attack range and has no NavMesh path.");
            }

            SessionState.SetBool(CHASE_VALIDATED_KEY, true);
            Debug.Log($"CombatTest NavMesh chase validation result: initialDistance={initialDistance}, currentDistance={currentDistance}, targetAcquired=True.");
        }

        private static EnemyBase FindRuntimeEnemy()
        {
            EnemyBase[] enemies = UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            if (enemies.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one active enemy, found {enemies.Length}.");
            }

            return enemies[0];
        }

        private static GameObject FindRuntimePlayer()
        {
            GameObject player = GameObject.Find("CombatTestPlayer");

            if (player == null)
            {
                throw new InvalidOperationException("CombatTestPlayer is missing during playmode.");
            }

            return player;
        }

        private static float GetHorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        private static void RequireRootComponent<T>(GameObject root) where T : Component
        {
            if (root.GetComponent<T>() == null)
            {
                throw new InvalidOperationException($"Root is missing {typeof(T).Name}.");
            }
        }

        private static void RequireChildComponent<T>(GameObject root, string childName) where T : Component
        {
            Transform child = root.transform.Find(childName);

            if (child == null || child.GetComponent<T>() == null)
            {
                throw new InvalidOperationException($"{childName} is missing {typeof(T).Name}.");
            }
        }

        private static void Fail(string message)
        {
            if (SessionState.GetBool(FAILED_KEY, false) == true)
            {
                return;
            }

            SessionState.SetBool(FAILED_KEY, true);
            SessionState.SetString(FAILURE_KEY, message);
            Debug.Log($"CombatTest scene validation failure captured. {message}");

            if (EditorApplication.isPlaying == true)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Cleanup();
            EditorApplication.Exit(1);
        }
    }
}
#endif
