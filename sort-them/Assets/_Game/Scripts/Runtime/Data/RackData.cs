using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Rack")]
    public class RackData : ScriptableObject
    {
        public CategoryData Category;
        public int ShelfCount = 5;
        public ShelfData Shelf;

        public string CategoryID => Category != null ? Category.CategoryID : null;
    }
}
