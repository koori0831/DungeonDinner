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


        public void Init(Action<MapInfoSO,bool> callback)
        {
            _callback = callback;
        }

        public void OpenMap()
        {
            buttonImage.DOFade(1f, 0.2f).OnComplete(() => buttonText.alpha = 1f);
            button.enabled = true;
        }

        public void CloseMap()
        {
            buttonText.alpha = 0f;
            buttonImage.DOFade(0f, 0.2f);
            button.enabled = false;
        }

        public void ClickButton() => _callback?.Invoke(mapInfo, _isCanAdventure);
    }
}