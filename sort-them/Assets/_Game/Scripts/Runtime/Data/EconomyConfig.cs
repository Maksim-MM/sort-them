using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        public float RewardPerCar = 1f;
        public float ShelfCompleteBonus = 0f;
        public bool AllowNegativeBalance = true;
    }
}
