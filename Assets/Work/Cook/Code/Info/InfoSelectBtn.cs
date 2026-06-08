using System;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [RequireComponent(typeof(Button))]
    public class InfoSelectBtn : MonoBehaviour
    {
        private Button _button;
        private DictionaryInfo _info;

        [SerializeField] private Image image;

        public void InitializeBtn(Action action)
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => action.Invoke());

        }


    }
}