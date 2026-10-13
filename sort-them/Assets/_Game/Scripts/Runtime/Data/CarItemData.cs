using UnityEngine;
using UnityEngine.AddressableAssets;
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
        public AssetReferenceGameObject Prefab;
        public float DisplayPrice;

        public string CategoryID => Category != null ? Category.CategoryID : null;
    }
}
