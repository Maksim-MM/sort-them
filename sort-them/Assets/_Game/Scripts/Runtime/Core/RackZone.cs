using UnityEngine;

namespace SortThem
{
    public class RackZone : MonoBehaviour
    {
        public RackController Rack;

        void OnTriggerStay(Collider other)
        {
            if (Rack == null) return;
            var body = other.attachedRigidbody;
            if (body == null) return;
            var car = body.GetComponent<CarInstance>();
            if (car != null) Rack.OnLooseCarInside(car);
        }
    }
}
