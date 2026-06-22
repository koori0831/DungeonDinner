using System.Collections;
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

        private Coroutine _moveRoutine;
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

            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            if (instant || isActiveAndEnabled == false)
            {
                targetCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }

            _moveRoutine = StartCoroutine(MoveCamera(pose));
        }

        private Transform ResolvePose(CookingGameScreenState state)
        {
            if (state == CookingGameScreenState.Preparation)
                return cuttingBoardPose != null ? cuttingBoardPose : conversationPose;

            return conversationPose;
        }

        private IEnumerator MoveCamera(Transform pose)
        {
            Transform cameraTransform = targetCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float eased = easing != null ? easing.Evaluate(t) : t;
                cameraTransform.position = Vector3.Lerp(startPosition, pose.position, eased);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, pose.rotation, eased);
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(pose.position, pose.rotation);
            _moveRoutine = null;
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
