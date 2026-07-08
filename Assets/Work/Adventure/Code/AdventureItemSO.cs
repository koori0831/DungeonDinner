using UnityEngine;

namespace Work.Adventure.Code
{
    [CreateAssetMenu(fileName = "AdventureItemSO", menuName = "SO/Adventure/AdventureItemSO")]
    public class AdventureItemSO : ScriptableObject
    {
        [field:SerializeField] public string ItemName { get; private set; } = "Item";
    }
}