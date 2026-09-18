using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using Work.UtillUI.Code;

namespace Work.Adventure.Code.UI
{
    public class GoAndStopSelectUI : MonoBehaviour
    {
        [SerializeField] private Image root;
        [SerializeField] private float time = 0.3f;

        [SerializeField] private Button goButton, stopButton;
        private Sequence _reveal;
        private bool _ready;

        public void Bind(Action go, Action stop)
        {
            goButton.onClick = new Button.ButtonClickedEvent();
            stopButton.onClick = new Button.ButtonClickedEvent();
            goButton.onClick.AddListener(() => Choose(go));
            stopButton.onClick.AddListener(() => Choose(stop));
        }

        private void Choose(Action action)
        {
            if (!_ready || GameUiInput.IsBlocked) return;
            SetInteractable(false);
            action?.Invoke();
            Disable();
        }

        public void Enable()
        {
            gameObject.SetActive(true);
            _reveal?.Kill();
            SetInteractable(false);
            GameUiInput.RequireFreshPress();
            _reveal = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Join(root.DOFade(0f, time))
                .Join(goButton.image.DOFade(1, time))
                .Join(stopButton.image.DOFade(1, time))
                .OnComplete(() => SetInteractable(true));
        }

        public void Disable()
        {
            _reveal?.Kill();
            SetInteractable(false);
            GameUiInput.ClearSelection();
            gameObject.SetActive(false);
        }

        private void SetInteractable(bool value)
        {
            _ready = value;
            goButton.interactable = stopButton.interactable = value;
            goButton.image.raycastTarget = stopButton.image.raycastTarget = value;
        }

        private void OnDisable()
        {
            _reveal?.Kill();
            SetInteractable(false);
        }
    }
}
