using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryScrollViewField : MonoBehaviour
    {
        [SerializeField] private InfoSelectBtn selectBtnPrefab;
        [SerializeField] private Transform content;

        //여기에는 해당 뷰에 표시될 정보들이 들어와야함
        public void InitializeField(List<DictionaryInfo> infos)
        {
            for(int i = 0; i < infos.Count; i++)
            {
                DictionaryInfo info = infos[i];
                InfoSelectBtn btn = Instantiate(selectBtnPrefab, content);
                //btn.InitializeBtn(() => info.); //여기서 데이터 입력부분 만들어야함
            }
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        public void Disable()
        {

            gameObject.SetActive(false);

        }
    }
}