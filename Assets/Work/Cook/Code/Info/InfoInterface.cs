using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoInterface { }

    public interface IHaveImageInfo 
    {
        public Sprite Sprite { get; }
    }

    public interface IHaveDescriptionInfo
    {
        public string Description { get; }
    }
}