using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryPanel : MonoBehaviour
    {
        //여기서 정보들 들고와서 스크롤뷰에 정보 기입하고 옆에 북마크도 생성해야함
        //북마크랑 스크롤뷰는 1ㄷ1로 매칭되게 생성되어야함 

        #region Test
        [SerializeField] private int testCount;
        #endregion
        [SerializeField] private Transform viewParent, bockmarkParent;
        [SerializeField] private InfoDictionaryScrollViewField scrollViewPrefavb;
        [SerializeField] private InfoBockmarkBtn bockmarkPrefab;

        private List<InfoDictionaryScrollViewField> viewList = new List<InfoDictionaryScrollViewField>();

        public void Awake()
        {
            for (int i = 0; i < testCount; ++i)
            {
                InfoDictionaryScrollViewField view = Instantiate(scrollViewPrefavb, viewParent);
                InfoBockmarkBtn bockmark = Instantiate(bockmarkPrefab, bockmarkParent);
                viewList.Add(view);
                view.InitializeField(i);
                bockmark.InitializeBtn(() => EnableScrollView(view));
            }
        }

        public void EnableScrollView(InfoDictionaryScrollViewField view)
        {
            foreach(InfoDictionaryScrollViewField item in viewList)
            {
                item.Disable();
            }

            view.Enable();
        }

    }
}