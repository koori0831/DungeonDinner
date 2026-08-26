using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
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

            if (isActiveAndEnabled == true)
                SubscribePanel();
        }

        public void ApplyState(CookingGameScreenState state, bool instant = false)
        {
            Transform pose = ResolvePose(state);
            if (pose == null || targetCamera == null)
                return;

            CancelMove();

            if (instant == true || isActiveAndEnabled == false)
            {
                targetCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }

            _moveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            MoveCameraAsync(pose, _moveCancellationTokenSource).Forget();
        }

        private Transform ResolvePose(CookingGameScreenState state)
        {
            if (state == CookingGameScreenState.Preparation || state == CookingGameScreenState.MiniGame)
                return cuttingBoardPose != null ? cuttingBoardPose : conversationPose;

            return conversationPose;
        }

        private async UniTask MoveCameraAsync(Transform pose, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
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
                return;
            }
            finally
            {
                if (_moveCancellationTokenSource == cancellationTokenSource)
                {
                    _moveCancellationTokenSource.Dispose();
                    _moveCancellationTokenSource = null;
                }
            }
        }

        private void CancelMove()
        {
            if (_moveCancellationTokenSource == null)
                return;

            _moveCancellationTokenSource.Cancel();
            _moveCancellationTokenSource.Dispose();
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

            Bus<CookingGameScreenChangedEvent>.Events += HandleScreenChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            Bus<CookingGameScreenChangedEvent>.Events -= HandleScreenChanged;
            _subscribedPanel = null;
        }

        private void HandleScreenChanged(CookingGameScreenChangedEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            ApplyState(gameEvent.Screen);
        }
    }
}
