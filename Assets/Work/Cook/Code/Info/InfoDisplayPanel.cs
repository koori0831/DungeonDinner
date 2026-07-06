using System;
using System.Collections;
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
        private Action _backAction;
        private Coroutine _transitionRoutine;
        private RectTransform _transitionRoot;
        private Vector2 _transitionBasePosition;
        private int _transitionDirection;

        public virtual void InitializeDisplay(Action backAction)
        {
            _backAction = backAction;

            if (backBtn == null)
            {
                Debug.LogWarning("InfoDisplayPanel needs a back button before it can bind back navigation.", this);
            }
            else
            {
                backBtn.onClick.RemoveListener(InvokeBack);
                backBtn.onClick.AddListener(InvokeBack);
            }

            EnsureNavigationButtons();
            BindNavigationButtons();
        }

        public void SetSiblingNavigation(Action previousAction, Action nextAction, bool hasPrevious, bool hasNext)
        {
            _previousAction = previousAction;
            _nextAction = nextAction;

            EnsureNavigationButtons();
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
            gameObject.SetActive(false);
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

        private void InvokeBack()
        {
            _backAction?.Invoke();
        }

        private void EnsureNavigationButtons()
        {
            // Navigation buttons are optional serialized references now.
            // Custom encyclopedia prefabs can provide them, but this class no longer creates UI objects at runtime.
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

            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _transitionRoutine = StartCoroutine(PageTransitionRoutine(direction));
        }

        private IEnumerator PageTransitionRoutine(int direction)
        {
            direction = direction == 0 ? 1 : Math.Sign(direction);
            float elapsed = 0f;
            Vector2 from = _transitionBasePosition + new Vector2(pageTransitionSlideDistance * direction, 0f);

            transitionCanvasGroup.alpha = 0.72f;
            _transitionRoot.anchoredPosition = from;

            while (elapsed < pageTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / pageTransitionDuration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                transitionCanvasGroup.alpha = Mathf.Lerp(0.72f, 1f, eased);
                _transitionRoot.anchoredPosition = Vector2.Lerp(from, _transitionBasePosition, eased);
                yield return null;
            }

            transitionCanvasGroup.alpha = 1f;
            _transitionRoot.anchoredPosition = _transitionBasePosition;
            _transitionRoutine = null;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }
    }
}
