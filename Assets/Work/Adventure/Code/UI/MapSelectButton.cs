using Assets.Work.Adventure.Code;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public class MapSelectButton : MonoBehaviour
    {
        [SerializeField] private MapInfoSO mapInfo;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private bool _isCanAdventure;
        private Action<MapInfoSO,bool> _callback;
        private Tween _fadeTween;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
        }

        public void Init(Action<MapInfoSO,bool> callback)
        {
            EnsureReferences();
            _callback = callback;
        }

        public void OpenMap()
        {
            EnsureReferences();
            KillFadeTween();
            if (button != null)
                button.interactable = true;

            if (buttonImage == null)
            {
                if (buttonText != null)
                    buttonText.alpha = 1f;
                return;
            }

            _fadeTween = buttonImage.DOFade(1f, 0.2f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    if (buttonText != null)
                        buttonText.alpha = 1f;
                });
        }

        public void CloseMap()
        {
            EnsureReferences();
            KillFadeTween();
            if (buttonText != null)
                buttonText.alpha = 0f;
            if (buttonImage != null)
            {
                _fadeTween = buttonImage.DOFade(0f, 0.2f)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            if (button != null)
                button.interactable = false;
        }

        public void ClickButton() => _callback?.Invoke(mapInfo, _isCanAdventure);

        private void OnDisable()
        {
            KillFadeTween();
        }

        private void KillFadeTween()
        {
            _fadeTween?.Kill(false);
            _fadeTween = null;
            buttonImage?.DOKill(false);
        }

        private void EnsureReferences()
        {
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();
            if (button == null)
                button = GetComponent<Button>();
            if (buttonText == null)
                buttonText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
