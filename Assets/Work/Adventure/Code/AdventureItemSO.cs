using UnityEngine;

namespace Work.Adventure.Code
{
    [CreateAssetMenu(fileName = "AdventureItemSO", menuName = "SO/Adventure/AdventureItemSO")]
    public class AdventureItemSO : ScriptableObject
    {
        [SerializeField] private string discoveryEntryId;
        public string DiscoveryEntryId => discoveryEntryId;
        [field:SerializeField] public string ItemName { get; private set; } = "Item";
        [field:SerializeField] public Sprite ItemIcon { get; private set; }
    }
}
