using UnityEngine;
using UnityEngine.Localization;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Category")]
    public class CategoryData : ScriptableObject
    {
        public string CategoryID;
        public string DevName;
        public LocalizedString DisplayName;
        public Color CategoryColor = Color.white;
    }
}
