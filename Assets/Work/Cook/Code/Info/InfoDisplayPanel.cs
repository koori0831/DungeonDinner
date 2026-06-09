using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [Serializable]
    public class InfoDisplayPanel : MonoBehaviour, IDisplayInfo
    {
        [SerializeField] private ViewHaveInfoEnum viewInfo;
        [SerializeField] private Image iconImage;

        public ViewHaveInfoEnum ViewInfo => viewInfo;

        public void InitializeDisplay()
        {
            
        }

        public void Enable(DictionaryInfo displayIndfo)
        {
            //모든 정보 갱신
            gameObject.SetActive(true);
            DictionaryInfo info = displayIndfo;
            if (info != null) return;

        }

        public void Disable()
        {
            //걍 끄기
            gameObject.SetActive(false);
        }
    }
}