using UnityEngine;

namespace Assets.Work.Adventure.Code
{
    [CreateAssetMenu(fileName = "MapInfoData",menuName = "SO/Adventure/MapInfoSO")]
    public class MapInfoSO : ScriptableObject
    {
        [field:SerializeField] public string MapName {  get; private set; }
        [field:SerializeField] public string Description {  get; private set; }
    }
}