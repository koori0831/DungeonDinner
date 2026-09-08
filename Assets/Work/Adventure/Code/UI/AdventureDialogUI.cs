using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Work.Adventure.Code.UI
{
    public class AdventureDialogUI : MonoBehaviour
    {
        [SerializeField] private OptionUI optionUI;
        [SerializeField] private GoAndStopSelectUI selectUI;
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform dialogPanel;
        [SerializeField] private RectTransform nextObject;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private float panelMovePosY = 300;
        [SerializeField] private float time = 0.5f;
        [SerializeField] private float characterInterval = 0.05f;

        private Tween _typingTween;
        private Tween _panelTween;
        private AdventureEventSO _currentEvent;
        private Options _selectOption;
        private List<AdventrueDialogData> _currentDialogDatas;
        private int _currentDialogIndex = 0;
        private bool _isCanWriteText;
        private bool _dialogActive;
        private bool _awaitingChoice;
        private List<Options> _currentOptions;

        public void StartDialog(AdventureEventSO eventSo)
        {
            _typingTween?.Kill(false);
            optionUI.DestroyAllButton();
            selectUI.Disable();
            _dialogActive = true;
            _awaitingChoice = false;
            _isCanWriteText = false;
            _currentDialogIndex = 0;
            _selectOption = null;
            _currentEvent = eventSo;
            _currentOptions = eventSo.options;
            _currentDialogDatas = eventSo.dialogDatas;
            InitLines();
            OpenDialogPanel();
        }

        private void InitLines()
        {
            foreach (var line in _currentDialogDatas)
                foreach (var method in line.method) method.Init(root);
        }

        private void Update()
        {
            if (_isCanWriteText)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    NextDialog();
                }
            }
        }

        public void OpenDialogPanel()
        {
            KillPanelTween();
            _panelTween = dialogPanel
                .DOSizeDelta(new Vector2(dialogPanel.sizeDelta.x, panelMovePosY), time)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    _isCanWriteText = true;
                    NextDialog();
                });
        }

        public void CloseDialogPanel()
        {
            dialogText.text = " ";
            nextObject.gameObject.SetActive(false);
            KillPanelTween();
            _panelTween = dialogPanel
                .DOSizeDelta(new Vector2(dialogPanel.sizeDelta.x, 0), time)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => _isCanWriteText = false);
        }

        public void NextDialog()
        {
            if (!_dialogActive || _awaitingChoice) return;
            if (_currentDialogDatas.Count <= _currentDialogIndex)
            {
                _isCanWriteText = false;
                _currentDialogIndex = 0;
                nextObject.gameObject.SetActive(false);
                if (_currentEvent == null)
                {
                    var completed = _selectOption;
                    _selectOption = null;
                    _currentOptions = completed.followUpOptions;
                    completed.rewardMethod.ForEach(x => x.GetReward());
                    Debug.Log(completed.RewardDescription);
                }
                if (_currentOptions != null && _currentOptions.Count > 0)
                {
                    _awaitingChoice = true;
                    optionUI.Enable(_currentOptions, ResultDialog);
                }
                else
                {
                    _dialogActive = false;
                    CloseDialogPanel();
                    selectUI.Enable();
                }
                return;
            }

            AdventrueDialogData data = _currentDialogDatas[_currentDialogIndex++];

            PlayTyping(data.Context);
            data.method.ForEach(x => x.RaiseEvent());
        }

        public void ResultDialog(Options option)
        {
            if (!_dialogActive || !_awaitingChoice || !_currentOptions.Contains(option)) return;
            _awaitingChoice = false;
            _isCanWriteText = true;
            _selectOption = option;
            _currentEvent = null;
            _currentDialogDatas = _selectOption.ResultdialogDatas;
            _currentDialogIndex = 0;
            InitLines();
            NextDialog();
        }

        public void PlayTyping(string message)
        {
            _isCanWriteText = false;
            nextObject.gameObject.SetActive(false);
            _typingTween?.Kill();

            dialogText.text = message;
            dialogText.ForceMeshUpdate();
            dialogText.maxVisibleCharacters = 0;

            int count = dialogText.textInfo.characterCount;

            _typingTween = DOTween.To(
                () => dialogText.maxVisibleCharacters,
                x => dialogText.maxVisibleCharacters = x,
                count,
                count * characterInterval)
                .SetEase(Ease.Linear)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    _isCanWriteText = true;
                    nextObject.gameObject.SetActive(true);
                });
        }

        private void OnDisable()
        {
            _dialogActive = false;
            _awaitingChoice = false;
            _isCanWriteText = false;
            _typingTween?.Kill(false);
            _typingTween = null;
            KillPanelTween();
        }

        private void KillPanelTween()
        {
            _panelTween?.Kill(false);
            _panelTween = null;
            dialogPanel?.DOKill(false);
        }
    }
}
