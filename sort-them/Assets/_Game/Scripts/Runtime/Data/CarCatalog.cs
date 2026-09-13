using System;
using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Car Catalog")]
    public class CarCatalog : ScriptableObject
    {
        public CategoryData[] Categories = Array.Empty<CategoryData>();
        public CarItemData[] Cars = Array.Empty<CarItemData>();
        public CategoryData SpecialCategory;
        public SpecialCarData[] Specials = Array.Empty<SpecialCarData>();

        public int IndexOf(CarItemData car) => Array.IndexOf(Cars, car);
        public bool IsSpecial(CarItemData car) => car != null && SpecialCategory != null && car.Category == SpecialCategory;
        public bool IsSpecial(CategoryData cat) => cat != null && cat == SpecialCategory;

        public SpecialCarData SpecialFor(CarItemData car)
        {
            if (car == null || Specials == null) return null;
            foreach (var s in Specials) if (s != null && s.Car == car) return s;
            return null;
        }
    }
}
