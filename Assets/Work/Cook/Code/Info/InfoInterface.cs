using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoInterface { }

    public interface IInfoInterface { }

    public interface IHaveDisplayNameInfo : IInfoInterface
    {
        public string DisplayName { get; }
    }

    public interface IHaveIconInfo : IInfoInterface
    {
        public Sprite Icon { get; }
    }

    public interface IHaveDescriptionInfo : IInfoInterface
    {
        public string Description { get; }
    }

    public interface IDisplayInfo
    {

    }
}
