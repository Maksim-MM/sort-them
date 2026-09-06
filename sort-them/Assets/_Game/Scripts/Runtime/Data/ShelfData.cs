using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Shelf")]
    public class ShelfData : ScriptableObject
    {
        public int Rows = 2;
        public int Columns = 5;
        public float SlotPitch = 0.45f;
        public float RowPitch = 0.5f;

        public int Capacity => Rows * Columns;
    }
}
