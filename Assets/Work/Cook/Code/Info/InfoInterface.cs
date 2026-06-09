using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoInterface { }

    public interface IInfoInterface { }

    public interface IHaveImageInfo : IInfoInterface
    {
        public Sprite Sprite { get; }
    }

    public interface IHaveDescriptionInfo : IInfoInterface
    {
        public string Description { get; }
    }

    public interface IDisplayInfo
    {

    }
}