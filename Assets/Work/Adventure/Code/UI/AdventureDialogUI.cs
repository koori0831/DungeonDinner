using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Rendering.MaterialUpgrader;

namespace Work.Adventure.Code.UI
{
    public class AdventureDialogUI : MonoBehaviour
    {
        [SerializeField] private OptionUI optionUI;
        [SerializeField] private GoAndStopSelectUI selectUI;
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform dialogPanel;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private float panelMovePosY = 300;
        [SerializeField] private float time = 0.5f;
        [SerializeField] private float characterInterval = 0.05f;

        private Tween _typingTween;
        private AdventureEventSO _currentEvent;
        private Options _selectOption;
        private List<AdventrueDialogData> _currentDialogDatas;
        private int _currentDialogIndex = 0;
        private bool _isCanWriteText;

        public void StartDialog(AdventureEventSO eventSo)
        {
            _currentEvent = eventSo;
            _currentDialogDatas = eventSo.dialogDatas;
            eventSo.dialogDatas.ForEach(item =>
            {
                item.method.ForEach(method =>
                {
                    method.Init(root);
                });
            });

            eventSo.options.ForEach(item =>
            {
                item.ResultdialogDatas.ForEach(data =>
                {
                    data.method.ForEach(method =>
                    {
                        method.Init(root);
                    });
                });
            });


            OpenDialogPanel();
        }

        private void Update()
        {
            if (_isCanWriteText)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    NextDialog();
                }
            }
        }

        public void OpenDialogPanel()
        {
            dialogPanel.DOSizeDelta(new Vector2(dialogPanel.sizeDelta.x, panelMovePosY), time).OnComplete(() =>
            {
                _isCanWriteText = true;
                NextDialog();
            });
        }

        public void CloseDialogPanel()
        {
            dialogText.text = " ";
            dialogPanel.DOSizeDelta(new Vector2(dialogPanel.sizeDelta.x, 0), time).OnComplete(() => _isCanWriteText = false);
        }

        public void NextDialog()
        {
            if (_currentDialogDatas.Count <= _currentDialogIndex)
            {
                _isCanWriteText = false;
                _currentDialogIndex = 0;
                if (_currentEvent == null)
                {
                    _selectOption.rewardMethod.ForEach(x => x.GetReward());
                    Debug.Log(_selectOption.RewardDescription);
                    CloseDialogPanel();
                    selectUI.Enable();
                    //_selectOption.RewardDescription; 보상부분 띄워줄때 
                }
                else
                    optionUI.Enable(_currentEvent.options, ResultDialog);
                return;
            }

            AdventrueDialogData data = _currentDialogDatas[_currentDialogIndex++];

            PlayTyping(data.Context);
            data.method.ForEach(x => x.RaiseEvent());
        }

        public void ResultDialog(Options option)
        {
            // 이후 다이얼로그 받았고 어떤 옵션 선택했는지도 받았음
            _isCanWriteText = true;
            _selectOption = option;
            _currentEvent = null;
            _currentDialogDatas = _selectOption.ResultdialogDatas;
            NextDialog();
        }

        public void PlayTyping(string message)
        {
            _isCanWriteText = false;
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
                .OnComplete(() => _isCanWriteText = true);
        }
    }
}