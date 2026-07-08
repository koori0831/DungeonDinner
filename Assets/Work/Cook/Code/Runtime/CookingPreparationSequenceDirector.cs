using DG.Tweening;
using UnityEngine;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 재료 손질 단계 진입과 종료 시 카메라 및 도마 전환 연출 관리
    /// </summary>
    public sealed class CookingPreparationSequenceDirector : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraMoveTarget;
        [SerializeField] private Transform cookingCameraPose;
        [SerializeField, Min(0.01f)] private float preparationOrthographicSize = 3.54f;
        [SerializeField] private Transform cuttingBoard;
        [SerializeField] private Vector3 cuttingBoardHiddenLocalOffset = new Vector3(0f, 4f, 0f);
        [SerializeField, Min(0.01f)] private float enterDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float restoreDuration = 0.45f;
        [SerializeField] private Ease boardEnterEase = Ease.OutBounce;
        [SerializeField] private Ease boardRestoreEase = Ease.InBack;
        [SerializeField] private Ease cameraEase = Ease.InOutSine;

        private Vector3 _cameraDefaultPosition;
        private Quaternion _cameraDefaultRotation;
        private float _cameraDefaultOrthographicSize;
        private Vector3 _cuttingBoardDefaultLocalPosition;
        private bool _hasCapturedDefaults;
        private bool _isPreparationActive;
        private Sequence _activeSequence;

        private void Awake()
        {
            EnsureReferences();
            CaptureDefaultsIfNeeded();

            if (gamePanel == null || IsPreparationStageState(gamePanel.CurrentScreen) == false)
            {
                MoveBoardToHiddenPositionInstantly();
            }
        }

        private void OnEnable()
        {
            EnsureReferences();
            CaptureDefaultsIfNeeded();

            if (gamePanel != null)
            {
                gamePanel.ScreenChanged += HandleScreenChanged;
                _isPreparationActive = IsPreparationStageState(gamePanel.CurrentScreen);
            }

            if (_isPreparationActive == false)
            {
                MoveBoardToHiddenPositionInstantly();
            }
        }

        private void OnDisable()
        {
            if (gamePanel != null)
            {
                gamePanel.ScreenChanged -= HandleScreenChanged;
            }

            KillActiveSequence();
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }

        private void HandleScreenChanged(CookingGameScreenState state)
        {
            if (IsPreparationStageState(state) == true)
            {
                if (_isPreparationActive == false)
                    PlayPreparationEnterSequence();

                return;
            }

            if (_isPreparationActive == true)
            {
                PlayPreparationExitSequence();
            }
        }

        private void PlayPreparationEnterSequence()
        {
            CaptureDefaultsIfNeeded();
            if (CanPlaySequence() == false)
            {
                return;
            }

            _isPreparationActive = true;
            KillActiveSequence();
            MoveBoardToHiddenPositionInstantly();

            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(this);
            _activeSequence.Join(cuttingBoard.DOLocalMove(_cuttingBoardDefaultLocalPosition, enterDuration).SetEase(boardEnterEase));
            _activeSequence.Join(cameraMoveTarget.DOMove(cookingCameraPose.position, enterDuration).SetEase(cameraEase));
            _activeSequence.Join(cameraMoveTarget.DORotateQuaternion(cookingCameraPose.rotation, enterDuration).SetEase(cameraEase));
            _activeSequence.Join(DOTween.To(GetCameraOrthographicSize, SetCameraOrthographicSize, preparationOrthographicSize, enterDuration).SetEase(cameraEase));
        }

        private void PlayPreparationExitSequence()
        {
            CaptureDefaultsIfNeeded();
            if (CanPlaySequence() == false)
            {
                return;
            }

            _isPreparationActive = false;
            KillActiveSequence();

            Vector3 hiddenBoardPosition = _cuttingBoardDefaultLocalPosition + cuttingBoardHiddenLocalOffset;
            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(this);
            _activeSequence.Join(cuttingBoard.DOLocalMove(hiddenBoardPosition, restoreDuration).SetEase(boardRestoreEase));
            _activeSequence.Join(cameraMoveTarget.DOMove(_cameraDefaultPosition, restoreDuration).SetEase(cameraEase));
            _activeSequence.Join(cameraMoveTarget.DORotateQuaternion(_cameraDefaultRotation, restoreDuration).SetEase(cameraEase));
            _activeSequence.Join(DOTween.To(GetCameraOrthographicSize, SetCameraOrthographicSize, _cameraDefaultOrthographicSize, restoreDuration).SetEase(cameraEase));
        }

        private void MoveBoardToHiddenPositionInstantly()
        {
            if (cuttingBoard == null || _hasCapturedDefaults == false)
            {
                return;
            }

            cuttingBoard.localPosition = _cuttingBoardDefaultLocalPosition + cuttingBoardHiddenLocalOffset;
        }

        private bool CanPlaySequence()
        {
            return targetCamera != null
                   && cameraMoveTarget != null
                   && cookingCameraPose != null
                   && cuttingBoard != null
                   && _hasCapturedDefaults == true;
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
            {
                gamePanel = GetComponent<CookingGamePanel>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null && targetCamera.orthographic == false)
            {
                targetCamera.orthographic = true;
            }

            if (cameraMoveTarget == null && targetCamera != null)
            {
                cameraMoveTarget = targetCamera.transform;
            }
        }

        private void CaptureDefaultsIfNeeded()
        {
            if (_hasCapturedDefaults == true || targetCamera == null || cameraMoveTarget == null || cuttingBoard == null)
            {
                return;
            }

            _cameraDefaultPosition = cameraMoveTarget.position;
            _cameraDefaultRotation = cameraMoveTarget.rotation;
            _cameraDefaultOrthographicSize = targetCamera.orthographicSize;
            _cuttingBoardDefaultLocalPosition = cuttingBoard.localPosition;
            _hasCapturedDefaults = true;
        }

        private float GetCameraOrthographicSize()
        {
            if (targetCamera == null)
            {
                return _cameraDefaultOrthographicSize;
            }

            return targetCamera.orthographicSize;
        }

        private void SetCameraOrthographicSize(float value)
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.orthographicSize = value;
        }

        private void KillActiveSequence()
        {
            if (_activeSequence == null)
            {
                return;
            }

            _activeSequence.Kill();
            _activeSequence = null;
        }

        private static bool IsPreparationStageState(CookingGameScreenState state)
        {
            return state == CookingGameScreenState.Preparation
                   || state == CookingGameScreenState.MiniGame;
        }
    }
}
