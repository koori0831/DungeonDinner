using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingCameraStateMover : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform conversationPose;
        [SerializeField] private Transform cuttingBoardPose;
        [SerializeField, Min(0.01f)] private float moveDuration = 0.45f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CancellationTokenSource _moveCancellationTokenSource;
        private CookingGamePanel _subscribedPanel;

        private void Reset()
        {
            targetCamera = Camera.main;
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribePanel();
            ApplyState(gamePanel != null ? gamePanel.CurrentScreen : CookingGameScreenState.None, true);
        }

        private void OnDisable()
        {
            CancelMove();
            UnsubscribePanel();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled)
                SubscribePanel();
        }

        public void ApplyState(CookingGameScreenState state, bool instant = false)
        {
            Transform pose = ResolvePose(state);
            if (pose == null || targetCamera == null)
                return;

            CancelMove();

            if (instant || isActiveAndEnabled == false)
            {
                targetCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }

            CancellationTokenSource moveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _moveCancellationTokenSource = moveCancellationTokenSource;
            MoveCameraAsync(pose, moveCancellationTokenSource).Forget();
        }

        private Transform ResolvePose(CookingGameScreenState state)
        {
            if (state == CookingGameScreenState.Preparation)
                return cuttingBoardPose != null ? cuttingBoardPose : conversationPose;

            return conversationPose;
        }

        private async UniTaskVoid MoveCameraAsync(Transform pose, CancellationTokenSource moveCancellationTokenSource)
        {
            CancellationToken cancellationToken = moveCancellationTokenSource.Token;
            Transform cameraTransform = targetCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float elapsed = 0f;

            try
            {
                while (elapsed < moveDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveDuration);
                    float eased = easing != null ? easing.Evaluate(t) : t;
                    cameraTransform.position = Vector3.Lerp(startPosition, pose.position, eased);
                    cameraTransform.rotation = Quaternion.Slerp(startRotation, pose.rotation, eased);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                cameraTransform.SetPositionAndRotation(pose.position, pose.rotation);
            }
            catch (OperationCanceledException)
            {
                // 화면 전환 또는 오브젝트 비활성화로 인한 정상 취소
            }
            finally
            {
                if (_moveCancellationTokenSource == moveCancellationTokenSource)
                {
                    _moveCancellationTokenSource = null;
                }

                moveCancellationTokenSource.Dispose();
            }
        }

        private void CancelMove()
        {
            if (_moveCancellationTokenSource == null)
                return;

            _moveCancellationTokenSource.Cancel();
            _moveCancellationTokenSource = null;
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();
            if (gamePanel == null)
                return;

            gamePanel.ScreenChanged += HandleScreenChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.ScreenChanged -= HandleScreenChanged;
            _subscribedPanel = null;
        }

        private void HandleScreenChanged(CookingGameScreenState state)
        {
            ApplyState(state);
        }
    }
}
