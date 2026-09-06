using UnityEngine;
using UnityEngine.Localization;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Car")]
    public class CarItemData : ScriptableObject
    {
        public string CarID;
        public string DevName;
        public LocalizedString DisplayName;
        public CategoryData Category;
        public GameObject Prefab;
        public float DisplayPrice;

        public string CategoryID => Category != null ? Category.CategoryID : null;
    }
}
