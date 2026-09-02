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
        [SerializeField] private Image mapImage;
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
                item.CloseMap();
            });
        }

        private void OpenInfoPanel(MapInfoSO info,bool isCanAdventure)
        {
            infoPanel.Open(info, isCanAdventure);
            mapImage.rectTransform.DOAnchorPos(new Vector2(-300, mapImage.rectTransform.anchoredPosition.y), openTime);
        }
        
        public void CloseInfoPanel()
        {
            infoPanel.Close();
            mapImage.rectTransform.DOAnchorPos(new Vector2(0, mapImage.rectTransform.anchoredPosition.y), openTime);
        }


        [ContextMenu("Open")]
        public void OpenMap()
        {
            root.gameObject.SetActive(true);
            Debug.Log("OpenMap");

            DOVirtual.DelayedCall(1, () =>
            {
                background.DOFade(0.9f, fadeTime).OnComplete(() =>
                {
                    mapImage.DOFade(1f, openTime);
                    mapButtons.ForEach(item =>
                    {
                        item.OpenMap();
                    });
                });
            });
        }

        [ContextMenu("Close")]
        public void CloseMap()
        {
            infoPanel.Close();
            mapButtons.ForEach(item =>
            {
                item.CloseMap();
            });
            mapImage.DOFade(0, openTime).OnComplete(() =>
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