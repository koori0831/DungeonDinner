using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 조리 뷰 진입 시 페이드 전환 표시
    /// </summary>
    public sealed class CookingViewTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Image fadeImage;
        [SerializeField, Min(0f)] private float enterFadeDuration = 0.25f;
        [SerializeField] private Color fadeColor = Color.black;

        private Tween _activeTween;

        private void OnDisable()
        {
            KillTween();
        }

        public void PlayEnter(UnityAction completed = null)
        {
            EnsureReferences();
            KillTween();

            if (fadeImage != null)
                fadeImage.color = fadeColor;

            if (fadeGroup == null)
            {
                completed?.Invoke();
                return;
            }

            fadeGroup.gameObject.SetActive(true);
            fadeGroup.alpha = 1f;
            fadeGroup.interactable = true;
            fadeGroup.blocksRaycasts = true;

            if (enterFadeDuration <= 0f)
            {
                CompleteEnter(completed);
                return;
            }

            _activeTween = fadeGroup
                .DOFade(0f, enterFadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => CompleteEnter(completed));
        }

        private void CompleteEnter(UnityAction completed)
        {
            _activeTween = null;
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.interactable = false;
                fadeGroup.blocksRaycasts = false;
                fadeGroup.gameObject.SetActive(false);
            }

            completed?.Invoke();
        }

        private void EnsureReferences()
        {
            if (fadeGroup == null)
                fadeGroup = GetComponent<CanvasGroup>();
            if (fadeImage == null)
                fadeImage = GetComponent<Image>();
        }

        private void KillTween()
        {
            if (_activeTween == null)
                return;

            _activeTween.Kill();
            _activeTween = null;
        }
    }
}
