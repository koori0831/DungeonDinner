using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [Serializable]
    public class InfoDisplayPanel : MonoBehaviour, IDisplayInfo
    {
        [SerializeField] protected ViewHaveInfoEnum viewInfo;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TextMeshProUGUI nameField, descriptionField;
        [SerializeField] protected Button backBtn;
        [SerializeField] protected Button previousBtn, nextBtn;
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [SerializeField, Min(0f)] private float pageTransitionDuration = 0.12f;
        [SerializeField, Min(0f)] private float pageTransitionSlideDistance = 16f;

        public ViewHaveInfoEnum ViewInfo => viewInfo;

        private Action _previousAction;
        private Action _nextAction;
        private CancellationTokenSource _transitionCancellationTokenSource;
        private RectTransform _transitionRoot;
        private Vector2 _transitionBasePosition;
        private int _transitionDirection;

        public virtual void InitializeDisplay(Action backAction)
        {
            ClearDisplayText();

            if (backBtn == null)
            {
                Debug.LogWarning("InfoDisplayPanel needs a back button before it can bind back navigation.", this);
            }
            else
                backBtn.onClick.AddListener(() => backAction?.Invoke());

            BindNavigationButtons();
        }

        public void SetSiblingNavigation(Action previousAction, Action nextAction, bool hasPrevious, bool hasNext)
        {
            _previousAction = previousAction;
            _nextAction = nextAction;

            BindNavigationButtons();

            SetButtonVisible(previousBtn, hasPrevious);
            SetButtonVisible(nextBtn, hasNext);
        }

        public virtual void Enable(InfoDictionaryEntryData displayInfo)
        {
            gameObject.SetActive(true);
            if (displayInfo == null)
                return;

            EnsureTransitionReferences();
            int direction = _transitionDirection;
            _transitionDirection = 0;

            if (iconImage != null)
                iconImage.sprite = displayInfo.Icon;

            if (nameField != null)
                nameField.text = displayInfo.DisplayName;

            if (descriptionField != null)
                descriptionField.text = displayInfo.Description;

            PlayPageTransition(direction);
        }

        public virtual void Disable()
        {
            CancelPageTransition();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelPageTransition();
        }

        private void ClearDisplayText()
        {
            if (nameField != null)
                nameField.text = string.Empty;

            if (descriptionField != null)
                descriptionField.text = string.Empty;
        }

        private void BindNavigationButtons()
        {
            if (previousBtn != null)
            {
                previousBtn.onClick.RemoveListener(InvokePrevious);
                previousBtn.onClick.AddListener(InvokePrevious);
            }

            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveListener(InvokeNext);
                nextBtn.onClick.AddListener(InvokeNext);
            }
        }

        private void InvokePrevious()
        {
            _transitionDirection = -1;
            _previousAction?.Invoke();
        }

        private void InvokeNext()
        {
            _transitionDirection = 1;
            _nextAction?.Invoke();
        }

        private void EnsureTransitionReferences()
        {
            if (transitionCanvasGroup == null)
                transitionCanvasGroup = GetComponent<CanvasGroup>();

            if (_transitionRoot == null)
            {
                if (descriptionField != null)
                    _transitionRoot = descriptionField.transform as RectTransform;
                else if (nameField != null)
                    _transitionRoot = nameField.transform as RectTransform;
                else
                    _transitionRoot = transform as RectTransform;
            }

            if (_transitionRoot != null)
                _transitionBasePosition = _transitionRoot.anchoredPosition;
        }

        private void PlayPageTransition(int direction)
        {
            if (transitionCanvasGroup == null || _transitionRoot == null || pageTransitionDuration <= 0f)
                return;

            CancelPageTransition();

            _transitionCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            PageTransitionAsync(direction, _transitionCancellationTokenSource).Forget();
        }

        private async UniTask PageTransitionAsync(int direction, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            direction = direction == 0 ? 1 : Math.Sign(direction);
            float elapsed = 0f;
            Vector2 from = _transitionBasePosition + new Vector2(pageTransitionSlideDistance * direction, 0f);

            try
            {
                transitionCanvasGroup.alpha = 0.72f;
                _transitionRoot.anchoredPosition = from;

                while (elapsed < pageTransitionDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / pageTransitionDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 2f);
                    transitionCanvasGroup.alpha = Mathf.Lerp(0.72f, 1f, eased);
                    _transitionRoot.anchoredPosition = Vector2.Lerp(from, _transitionBasePosition, eased);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transitionCanvasGroup.alpha = 1f;
                _transitionRoot.anchoredPosition = _transitionBasePosition;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (_transitionCancellationTokenSource == cancellationTokenSource)
                {
                    _transitionCancellationTokenSource.Dispose();
                    _transitionCancellationTokenSource = null;
                }
            }
        }

        private void CancelPageTransition()
        {
            if (_transitionCancellationTokenSource == null)
                return;

            _transitionCancellationTokenSource.Cancel();
            _transitionCancellationTokenSource.Dispose();
            _transitionCancellationTokenSource = null;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }
    }
}
