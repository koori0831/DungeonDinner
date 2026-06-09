using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    [Serializable]
    public class ViewInfoValue
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public MarkerEnum Marker { get; private set; }
        [field: SerializeField] public ViewHaveInfoEnum HaveInfoView { get; private set; }
        [field: SerializeField] public List<DictionaryInfo> Infos { get; private set; } = new List<DictionaryInfo>();
    }


    public class InfoDictionaryPanel : MonoBehaviour
    {
        //여기서 정보들 들고와서 스크롤뷰에 정보 기입하고 옆에 북마크도 생성해야함
        //북마크랑 스크롤뷰는 1ㄷ1로 매칭되게 생성되어야함 

        [SerializeField] private float y_Offset, default_X_Value;
        [SerializeField] private Transform viewParent, bockmarkParent;
        [SerializeField] private InfoBockmarkBtn bockmarkPrefab;
        [SerializeField] private InfoDictionaryScrollViewField scrollViewPrefavb;
        [SerializeField] private List<ViewInfoValue> viewInfoList = new List<ViewInfoValue>();
        [SerializeField] private List<InfoDisplayPanel> displayPrefabs = new List<InfoDisplayPanel>();

        private List<InfoDictionaryScrollViewField> _viewList = new List<InfoDictionaryScrollViewField>();

        public void Awake()
        {
            for (int i = 0; i < viewInfoList.Count; ++i)
            {
                ViewInfoValue value = viewInfoList[i];
                InfoDisplayPanel displayPanel = Instantiate(GetDisplayPrefab(value.HaveInfoView), viewParent);
                InfoDictionaryScrollViewField view = Instantiate(scrollViewPrefavb, viewParent);
                InfoBockmarkBtn bockmark = Instantiate(bockmarkPrefab, bockmarkParent);
                _viewList.Add(view);
                view.InitializeField(value.Infos); //여기에서 이제 버튼 클릭하면 디스플레이 작동하도록 개발해야함 
                view.Disable();
                displayPanel.InitializeDisplay();
                displayPanel.Disable();
                bockmark.InitializeBtn(() => EnableScrollView(view));
                bockmark.Rect.anchoredPosition = new Vector2(default_X_Value, y_Offset * i);
            }
        }

        public void EnableScrollView(InfoDictionaryScrollViewField view)
        {
            foreach (InfoDictionaryScrollViewField item in _viewList)
            {
                item.Disable();
            }

            view.Enable();
        }

        public InfoDisplayPanel GetDisplayPrefab(ViewHaveInfoEnum viewEnum)
        {
            InfoDisplayPanel result = null;
            for (int i = 0; i < displayPrefabs.Count; i++)
            {
                result = displayPrefabs[i].ViewInfo == viewEnum ? displayPrefabs[i] : null;
            }
            return result;
        }

    }
}