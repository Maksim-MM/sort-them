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
        public BoxCollider Zone;

        public void OnLooseCarInside(CarInstance car)
        {
            if (car == null || car.State != CarState.Loose || car.Levitating || car.Body.isKinematic) return;
            if (car.Body.linearVelocity.sqrMagnitude < 0.25f) return;
            var gm = GameManager.I;
            if (gm == null || !gm.Upgrades.Has(UpgradeKind.AutoPlace)) return;
            if (Category == null || car.Data == null || car.Data.Category != Category) return;
            var target = FindAutoPlaceShelf(car.Data);
            int slot = target != null ? target.FirstFreeSlot() : -1;
            if (slot >= 0) target.StartCoroutine(target.Magnet(car, slot));
        }

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

        void OnEnable() => Loc.Changed += RefreshSign;
        void OnDisable() => Loc.Changed -= RefreshSign;

        public void RefreshSign()
        {
            if (Category == null) return;
            if (SignText != null) SignText.text = Loc.Get(Category.DisplayName, Category.DevName);
            if (SignPlate != null) SignPlate.material.color = Category.CategoryColor;
        }
    }
}
