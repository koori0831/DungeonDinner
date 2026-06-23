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
        [SerializeField] private Transform cookingCameraPose;
        [SerializeField] private Transform cuttingBoard;
        [SerializeField] private Vector3 cuttingBoardHiddenLocalOffset = new Vector3(0f, 4f, 0f);
        [SerializeField, Min(0.01f)] private float enterDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float restoreDuration = 0.45f;
        [SerializeField] private Ease boardEnterEase = Ease.OutBounce;
        [SerializeField] private Ease boardRestoreEase = Ease.InBack;
        [SerializeField] private Ease cameraEase = Ease.InOutSine;

        private Vector3 _cameraDefaultPosition;
        private Quaternion _cameraDefaultRotation;
        private Vector3 _cuttingBoardDefaultLocalPosition;
        private bool _hasCapturedDefaults;
        private bool _isPreparationActive;
        private Sequence _activeSequence;

        private void Awake()
        {
            EnsureReferences();
            CaptureDefaultsIfNeeded();

            if (gamePanel == null || gamePanel.CurrentScreen != CookingGameScreenState.Preparation)
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
                _isPreparationActive = gamePanel.CurrentScreen == CookingGameScreenState.Preparation;
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
            if (state == CookingGameScreenState.Preparation)
            {
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

            Transform cameraTransform = targetCamera.transform;
            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(this);
            _activeSequence.Join(cuttingBoard.DOLocalMove(_cuttingBoardDefaultLocalPosition, enterDuration).SetEase(boardEnterEase));
            _activeSequence.Join(cameraTransform.DOMove(cookingCameraPose.position, enterDuration).SetEase(cameraEase));
            _activeSequence.Join(cameraTransform.DORotateQuaternion(cookingCameraPose.rotation, enterDuration).SetEase(cameraEase));
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

            Transform cameraTransform = targetCamera.transform;
            Vector3 hiddenBoardPosition = _cuttingBoardDefaultLocalPosition + cuttingBoardHiddenLocalOffset;
            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(this);
            _activeSequence.Join(cuttingBoard.DOLocalMove(hiddenBoardPosition, restoreDuration).SetEase(boardRestoreEase));
            _activeSequence.Join(cameraTransform.DOMove(_cameraDefaultPosition, restoreDuration).SetEase(cameraEase));
            _activeSequence.Join(cameraTransform.DORotateQuaternion(_cameraDefaultRotation, restoreDuration).SetEase(cameraEase));
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
        }

        private void CaptureDefaultsIfNeeded()
        {
            if (_hasCapturedDefaults == true || targetCamera == null || cuttingBoard == null)
            {
                return;
            }

            Transform cameraTransform = targetCamera.transform;
            _cameraDefaultPosition = cameraTransform.position;
            _cameraDefaultRotation = cameraTransform.rotation;
            _cuttingBoardDefaultLocalPosition = cuttingBoard.localPosition;
            _hasCapturedDefaults = true;
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
    }
}
