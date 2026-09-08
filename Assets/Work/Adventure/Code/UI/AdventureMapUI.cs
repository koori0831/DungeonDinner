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

        [SerializeField] private List<MapSelectButton> mapButtons = new List<MapSelectButton>();

        private Action _callback;
        private Sequence _visibilityTween;
        private Tween _mapPositionTween;
        private void Awake()
        {
            BindMapButtons();
        }

        public void Init(Action callback = null)
        {
            _callback = callback;
            BindMapButtons();

            mapButtons.ForEach(item =>
            {
                if (item != null)
                    item.CloseMap();
            });
        }

        private void BindMapButtons()
        {
            // Map selection must also work when preparation initialization is interrupted.
            foreach (MapSelectButton item in mapButtons)
            {
                if (item != null)
                    item.Init(OpenInfoPanel);
            }
        }

        private void OpenInfoPanel(MapInfoSO info,bool isCanAdventure)
        {
            infoPanel.Open(info, isCanAdventure);
            MoveMapImage(-300f);
        }
        
        public void CloseInfoPanel()
        {
            infoPanel.Close();
            MoveMapImage(0f);
        }


        [ContextMenu("Open")]
        public void OpenMap()
        {
            KillVisibilityTween();
            root.gameObject.SetActive(true);
            Debug.Log("OpenMap");

            _visibilityTween = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .AppendInterval(1f)
                .Append(background.DOFade(0.9f, fadeTime))
                .AppendCallback(() =>
                {
                    mapButtons.ForEach(item =>
                    {
                        if (item != null)
                            item.OpenMap();
                    });
                })
                .Append(mapImage.DOFade(1f, openTime));
        }

        [ContextMenu("Close")]
        public void CloseMap()
        {
            KillVisibilityTween();
            KillMapPositionTween();
            infoPanel.Close();
            mapButtons.ForEach(item =>
            {
                if (item != null)
                    item.CloseMap();
            });

            _visibilityTween = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Append(mapImage.DOFade(0, openTime))
                .Append(background.DOFade(0, fadeTime))
                .OnComplete(() =>
                {
                    _callback?.Invoke();
                    root.gameObject.SetActive(false);
                });
        }

        private void OnDisable()
        {
            KillVisibilityTween();
            KillMapPositionTween();
        }

        private void MoveMapImage(float targetX)
        {
            if (mapImage == null)
                return;

            KillMapPositionTween();
            RectTransform imageRect = mapImage.rectTransform;
            _mapPositionTween = imageRect
                .DOAnchorPos(new Vector2(targetX, imageRect.anchoredPosition.y), openTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void KillVisibilityTween()
        {
            _visibilityTween?.Kill(false);
            _visibilityTween = null;
            background?.DOKill(false);
            mapImage?.DOKill(false);
        }

        private void KillMapPositionTween()
        {
            _mapPositionTween?.Kill(false);
            _mapPositionTween = null;
            if (mapImage != null)
                mapImage.rectTransform.DOKill(false);
        }

        public void ClickStartAdventureButton()
        {
            _callback?.Invoke();
        }
    }
}
