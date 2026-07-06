using Work.Adventure.Code.UI;
using System;
using UnityEngine;
using Work.Cook.Code.Runtime;

namespace Work.Adventure.Code
{
    public class AdventureManager : MonoBehaviour
    {
        [SerializeField] private PreparationMenu preparationMenuUI;
        [SerializeField] private MainUI mainUIroot;

        public void Awake()
        {
            preparationMenuUI.Init(() => mainUIroot.HideUI(), () => mainUIroot.ShowUI());
        }

        [ContextMenu("TestEndBusiness")]
        public void EndBusiness()
        {
            preparationMenuUI.ShowUI();
        }
    }
}