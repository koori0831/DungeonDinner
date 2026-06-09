#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Work.Combat.Code.Runtime;
using Work.Enemy.Code;
using Work.Entities.Code;

namespace Work.FSM.Editor
{
    /// <summary>
    /// CombatTest 씬 정리 결과 검증 임시 도구.
    /// </summary>
    public static class CombatTestSceneValidationTool
    {
        private const string SCENE_PATH = "Assets/Work/Combat/Scene/CombatTest.unity";
        private const string PREFAB_PATH = "Assets/Work/Enemy/Prefab/SlimeEnemy.prefab";
        private const string RUNNING_KEY = "CombatTestSceneValidationTool.Running";
        private const string FAILED_KEY = "CombatTestSceneValidationTool.Failed";
        private const string FAILURE_KEY = "CombatTestSceneValidationTool.Failure";
        private const string FRAME_COUNT_KEY = "CombatTestSceneValidationTool.FrameCount";
        private const string ATTACK_RUN_KEY = "CombatTestSceneValidationTool.AttackRun";
        private const int ATTACK_FRAME = 5;
        private const int PLAY_FRAME_COUNT = 180;

        /// <summary>
        /// CombatTest 씬 구조와 플레이모드 180프레임 검증 실행.
        /// </summary>
        public static void Run()
        {
            SessionState.SetBool(RUNNING_KEY, true);
            SessionState.SetBool(FAILED_KEY, false);
            SessionState.SetString(FAILURE_KEY, string.Empty);
            SessionState.SetInt(FRAME_COUNT_KEY, 0);
            SessionState.SetBool(ATTACK_RUN_KEY, false);

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
            SessionState.EraseBool(ATTACK_RUN_KEY);
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
                if (frameCount == ATTACK_FRAME)
                {
                    RunPlayerAttackValidation();
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
        }

        private static void ValidateSlime(EnemyBase enemy)
        {
            GameObject root = enemy.gameObject;
            RequireRootComponent<EnemyStateController>(root);
            RequireRootComponent<CharacterController>(root);
            RequireRootComponent<EnemyMovementModule>(root);
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
        }

        private static void RunPlayerAttackValidation()
        {
            if (SessionState.GetBool(ATTACK_RUN_KEY, false) == true)
            {
                return;
            }

            SessionState.SetBool(ATTACK_RUN_KEY, true);
            GameObject player = GameObject.Find("CombatTestPlayer");

            if (player == null)
            {
                throw new InvalidOperationException("CombatTestPlayer is missing.");
            }

            CombatAttackExecutor executor = player.GetComponent<CombatAttackExecutor>();

            if (executor == null)
            {
                throw new InvalidOperationException("CombatTestPlayer attack executor is missing.");
            }

            executor.ExecuteAttack();
            Debug.Log($"CombatTest player attack validation result: hits={executor.LastHitSuccessCount}, kills={executor.LastKilledCount}.");
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
