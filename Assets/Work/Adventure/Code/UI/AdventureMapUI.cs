using Assets.Work.Adventure.Code;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public class AdventureMapUI : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private MapInfoPanel infoPanel;
        [SerializeField] private RectTransform mapRoot;
        [SerializeField] private RectTransform root;
        [SerializeField] private float fadeTime = 0.3f;
        [SerializeField] private float openTime = 0.6f;
        [SerializeField] private float openSizeWidth = 1030f;

        [SerializeField] private List<MapSelectButton> mapButtons = new List<MapSelectButton>();

        private Action _callback;
        public void Init(Action callback = null)
        {
            _callback = callback;

            mapButtons.ForEach(item =>
            {
                item.Init(OpenInfoPanel);
            });
        }

        private void OpenInfoPanel(MapInfoSO info,bool isCanAdventure)
        {
            infoPanel.Open(info, isCanAdventure);
        }

        

        [ContextMenu("Open")]
        public void OpenMap()
        {
            root.gameObject.SetActive(true);

            DOVirtual.DelayedCall(1, () =>
            {
                background.DOFade(0.9f, fadeTime).OnComplete(() =>
                {
                    mapRoot.DOSizeDelta(new Vector2(openSizeWidth, mapRoot.sizeDelta.y), openTime);
                });
            });
        }

        [ContextMenu("Close")]
        public void CloseMap()
        {
            infoPanel.Close();
            mapRoot.DOSizeDelta(new Vector2(0, mapRoot.sizeDelta.y), openTime).OnComplete(() =>
            {
                background.DOFade(0, fadeTime).OnComplete(() =>
                {
                    _callback?.Invoke();
                    root.gameObject.SetActive(false);
                });
            });
        }

        public void ClickStartAdventureButton()
        {
            _callback?.Invoke();
        }
    }
}