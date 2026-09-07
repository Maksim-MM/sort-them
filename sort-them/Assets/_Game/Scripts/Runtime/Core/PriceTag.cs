using TMPro;
using UnityEngine;

namespace SortThem
{
    public class PriceTag : MonoBehaviour
    {
        public ShelfController Shelf;
        public GameObject Root;
        public Renderer Plate;
        public TMP_Text NameText;
        public TMP_Text PriceText;
        public TMP_Text CountText;
        public Material WhiteMaterial;
        public Material RedMaterial;
        public Material GoldMaterial;

        void OnEnable()
        {
            if (Shelf != null) Shelf.Changed += Refresh;
            Loc.Changed += OnLocChanged;
            Refresh(Shelf);
        }

        void OnDisable()
        {
            if (Shelf != null) Shelf.Changed -= Refresh;
            Loc.Changed -= OnLocChanged;
        }

        void OnLocChanged() => Refresh(Shelf);

        public void Refresh(ShelfController shelf)
        {
            if (Root == null) return;
            if (shelf == null || shelf.IsEmpty)
            {
                if (Root.activeSelf) Root.SetActive(false);
                return;
            }
            if (!Root.activeSelf) Root.SetActive(true);
            var car = shelf.TargetCar;
            if (NameText != null) NameText.text = Loc.Get(car.DisplayName, car.DevName);
            if (PriceText != null) PriceText.text = "$" + car.DisplayPrice.ToString("0.##");
            if (CountText != null) CountText.text = shelf.Count + "/" + shelf.Capacity;
            if (Plate != null)
            {
                var mat = shelf.IsClosed ? GoldMaterial : shelf.IsValid ? WhiteMaterial : RedMaterial;
                if (mat != null) Plate.sharedMaterial = mat;
            }
            var textColor = shelf.IsClosed ? new Color(0.25f, 0.18f, 0f) : shelf.IsValid ? Color.black : Color.white;
            if (NameText != null) NameText.color = textColor;
            if (PriceText != null) PriceText.color = textColor;
            if (CountText != null) CountText.color = textColor;
        }
    }
}
