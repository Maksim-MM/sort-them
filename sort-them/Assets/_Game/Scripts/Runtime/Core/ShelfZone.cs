using UnityEngine;

namespace SortThem
{
    public class ShelfZone : MonoBehaviour
    {
        public ShelfController Shelf;

        void OnTriggerEnter(Collider other)
        {
            if (Shelf == null) return;
            var body = other.attachedRigidbody;
            if (body == null) return;
            var car = body.GetComponent<CarInstance>();
            if (car != null) Shelf.OnLooseCarEntered(car);
        }
    }
}
