using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Adventure.Code
{
    [Serializable]
    public abstract class AdventureReward 
    {
        public abstract void GetReward();
    }

    [Serializable]
    public abstract class AdventrueDialogEvent
    {
        public abstract void Init(RectTransform root);

        public abstract void RaiseEvent();
    }

    [Serializable]
    public class Options
    {
        [field:SerializeField] public string OptionName {  get; protected set; }
        [field:SerializeField] public string OptionTooltip {  get; protected set; }
        [field:SerializeField] public string RewardDescription { get; protected set; }
        [SerializeReference] public List<AdventureReward> rewardMethod = new List<AdventureReward>();
        [field: SerializeField] public List<AdventrueDialogData> ResultdialogDatas { get; private set; } = new List<AdventrueDialogData>();
    }
    [Serializable]
    public class LockedOption : Options
    {
        [field:SerializeField] public AdventureItemSO KeyItem { get; protected set; } // 해당 아이템을 가지고 있으면 선택지 해금
    } 

    [Serializable] 
    public class AdventrueDialogData // 대화 한줄 한줄이고 해당 줄에 어떤 이미지가 나와야한다. 뭐 다른게 작동해야 한다. 그러면 AdventrueEventDialogEvent를 구현한 클래스를 넣어두면 알아서 실행
    {
        [field: SerializeField] public string Context { get; private set;  }
        [SerializeReference] public List<AdventrueDialogEvent> method = new List<AdventrueDialogEvent>();
    }

    [CreateAssetMenu(fileName = "AdventureEventSO", menuName = "SO/Adventure/AdventureEventSO")]
    public class AdventureEventSO : ScriptableObject
    {
        [field:SerializeField] public List<AdventrueDialogData> dialogDatas { get; private set; } = new List<AdventrueDialogData>();
        [SerializeReference] public List<Options> options = new List<Options>();
    }
}