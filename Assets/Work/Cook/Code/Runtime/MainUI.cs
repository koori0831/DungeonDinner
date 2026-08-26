using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Work.Cook.Code.Runtime
{
    public class MainUI : MonoBehaviour
    {
        [SerializeField] private MoveLayoutUI chatUI, infoUI;

        public void ShowUI()
        {
            chatUI.ResetPos();
            infoUI.ResetPos();
        }
        public void HideUI()
        {
            chatUI.Move();
            infoUI.Move();
        }
    }
}