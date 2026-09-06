using System;
using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Car Catalog")]
    public class CarCatalog : ScriptableObject
    {
        public CategoryData[] Categories = Array.Empty<CategoryData>();
        public CarItemData[] Cars = Array.Empty<CarItemData>();

        public int IndexOf(CarItemData car) => Array.IndexOf(Cars, car);
    }
}
