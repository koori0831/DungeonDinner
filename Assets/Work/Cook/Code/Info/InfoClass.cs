using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoClass { }

    public class DefaultInfo
    {
        public string Name { get; private set; }
    }

    /// <summary>
    /// 사전의 기본적인 정보
    /// </summary>
    public class DictionaryInfo : DefaultInfo, IHaveImageInfo, IHaveDescriptionInfo
    {
        public Sprite Sprite { get; private set; }
        public string Description {  get; private set; }
    }

    /// <summary>
    /// 나라에 대한 정보
    /// </summary>
    public class CountryInfo : DictionaryInfo
    {

    }

    /// <summary>
    /// 종족에 대한 정보
    /// </summary>
    public class TribeInfo : DictionaryInfo
    {

    }

    public class GroomingMethodInfo
    {

    }
}
