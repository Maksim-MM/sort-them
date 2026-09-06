using UnityEngine;

namespace SortThem
{
    public enum CarState
    {
        Loose,
        Held,
        Placed
    }

    [RequireComponent(typeof(Rigidbody))]
    public class CarInstance : MonoBehaviour
    {
        public int InstanceId = -1;
        public CarItemData Data;
        public float PaidAmount;
        public CarState State = CarState.Loose;
        public ShelfController Shelf;
        public int SlotIndex = -1;
        [System.NonSerialized] public bool Levitating;

        Rigidbody _body;
        Collider _col;
        MeshRenderer _rend;
        MeshFilter _filter;
        Vector3 _halfExtents;
        bool _halfExtentsReady;

        public Rigidbody Body { get { if (_body == null) _body = GetComponent<Rigidbody>(); return _body; } }
        public Collider Col { get { if (_col == null) _col = GetComponent<Collider>(); return _col; } }
        public MeshRenderer Rend { get { if (_rend == null) _rend = GetComponentInChildren<MeshRenderer>(); return _rend; } }
        public MeshFilter Filter { get { if (_filter == null) _filter = GetComponentInChildren<MeshFilter>(); return _filter; } }

        public Vector3 HalfExtents
        {
            get
            {
                if (!_halfExtentsReady)
                {
                    if (Col is BoxCollider box) _halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
                    else if (Col != null) _halfExtents = Col.bounds.extents;
                    else _halfExtents = Vector3.one * 0.1f;
                    _halfExtentsReady = true;
                }
                return _halfExtents;
            }
        }

        public bool IsSleepingOrKinematic => Body.isKinematic || Body.IsSleeping();

        public void SetLoose(Vector3 position, Quaternion rotation, bool kinematic)
        {
            State = CarState.Loose;
            Shelf = null;
            SlotIndex = -1;
            gameObject.layer = Layers.LooseItems;
            transform.SetPositionAndRotation(position, rotation);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Body.isKinematic = kinematic;
            if (!kinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
        }

        public void Launch(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            State = CarState.Loose;
            Shelf = null;
            SlotIndex = -1;
            gameObject.layer = Layers.LooseItems;
            transform.SetPositionAndRotation(position, rotation);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Body.isKinematic = false;
            Body.WakeUp();
            Body.linearVelocity = velocity;
            Body.angularVelocity = Random.insideUnitSphere * 3f;
        }

        public void SetHeld()
        {
            State = CarState.Held;
            Shelf = null;
            SlotIndex = -1;
            Body.isKinematic = true;
            gameObject.SetActive(false);
        }

        public void SetPlaced(ShelfController shelf, int slot, Transform slotPoint)
        {
            State = CarState.Placed;
            Shelf = shelf;
            SlotIndex = slot;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Body.isKinematic = true;
            gameObject.layer = Layers.StaticPlaced;
            transform.SetPositionAndRotation(SlotPose(slotPoint), slotPoint.rotation);
        }

        public Vector3 SlotPose(Transform slotPoint) => slotPoint.position + slotPoint.up * HalfExtents.y;

        public void Freeze()
        {
            if (!Body.isKinematic) Body.isKinematic = true;
        }

        public void Unfreeze()
        {
            if (Body.isKinematic)
            {
                Body.isKinematic = false;
                Body.WakeUp();
            }
        }
    }
}
