using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryScrollViewField : MonoBehaviour
    {
        [SerializeField] private InfoSelectBtn selectBtnPrefab;
        [SerializeField] private Transform content;

        //여기에는 해당 뷰에 표시될 정보들이 들어와야함
        public void InitializeField(int testCount)
        {
            for(int i = 0; i < testCount; i++)
            {
                Instantiate(selectBtnPrefab, content);
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