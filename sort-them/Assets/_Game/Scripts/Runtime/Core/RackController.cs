using TMPro;
using UnityEngine;

namespace SortThem
{
    public class RackController : MonoBehaviour
    {
        public CategoryData Category;
        public ShelfController[] Shelves;
        public TMP_Text SignText;
        public Renderer SignPlate;
        public GameObject HighlightFrame;

        public ShelfController FindAutoPlaceShelf(CarItemData car)
        {
            if (car == null || Shelves == null) return null;
            ShelfController empty = null;
            foreach (var shelf in Shelves)
            {
                if (shelf == null) continue;
                if (shelf.TargetCar == car && shelf.Count < shelf.Capacity) return shelf;
                if (empty == null && shelf.IsEmpty) empty = shelf;
            }
            return empty;
        }

        public void SetHighlight(bool on)
        {
            if (HighlightFrame != null && HighlightFrame.activeSelf != on) HighlightFrame.SetActive(on);
        }

        public void RefreshSign()
        {
            if (Category == null) return;
            if (SignText != null) SignText.text = Loc.Get(Category.DisplayName, Category.DevName);
            if (SignPlate != null) SignPlate.material.color = Category.CategoryColor;
        }
    }
}
