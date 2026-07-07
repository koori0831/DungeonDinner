using Assets.Work.Adventure.Code;
using DG.Tweening;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public class MapInfoPanel : MonoBehaviour
    {
        [SerializeField] private Button startAdventureButton;
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI title, description;
        [SerializeField] private Color canNotStartAdventureButtonColor;

        [SerializeField] private float openXPos = -15.5f;
        [SerializeField] private float time = 0.5f;


        public void Open(MapInfoSO info, bool isCanAdventure)
        {
            SetText(info, isCanAdventure);
            SetStartAdventureButton(isCanAdventure);
            root.DOAnchorPos(new Vector2(openXPos, root.anchoredPosition.y), time);
        }

        public void Close()
        {
            root.DOAnchorPos(new Vector2(root.sizeDelta.x, root.anchoredPosition.y), time);
        }

        public void SetStartAdventureButton(bool isCanAdventure)
        {
            startAdventureButton.interactable = isCanAdventure;
            startAdventureButton.image.color = isCanAdventure ? Color.white : canNotStartAdventureButtonColor;
        }

        public void SetText(MapInfoSO info, bool isCanAdventure)
        {
            title.text = info.MapName;
            description.text = info.Description;

            if(isCanAdventure == false)
            {
                description.text = "<color=red>진입불가지역</color>";
            }
        }
    }
}