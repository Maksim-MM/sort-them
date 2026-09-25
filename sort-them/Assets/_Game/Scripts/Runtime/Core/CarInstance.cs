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
        public Mesh[] Lods;
        [System.NonSerialized] public int Lod;
        [System.NonSerialized] public bool Levitating;
        [System.NonSerialized] public float CalmSince = -1f;
        [System.NonSerialized] public Vector3 LitPosition = new Vector3(float.MaxValue, 0f, 0f);
        public bool Hidden { get; private set; }

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

        public float DisplayScale { get; private set; } = 1f;

        public Vector3 HalfExtents => BaseHalfExtents * DisplayScale;

        Vector3 BaseHalfExtents
        {
            get
            {
                if (!_halfExtentsReady)
                {
                    if (Col is BoxCollider box) _halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
                    else if (Col != null) _halfExtents = Col.bounds.extents;
                    else _halfExtents = Vector3.one * 0.1f;
                    _halfExtents /= DisplayScale;
                    _halfExtentsReady = true;
                }
                return _halfExtents;
            }
        }

        public void SetDisplayScale(float scale)
        {
            if (scale <= 0f) scale = 1f;
            if (Mathf.Approximately(DisplayScale, scale)) return;
            DisplayScale = scale;
            transform.localScale = Vector3.one * scale;
        }

        public bool IsSleepingOrKinematic => Body.isKinematic || Body.IsSleeping();

        public void SetLoose(Vector3 position, Quaternion rotation, bool kinematic)
        {
            State = CarState.Loose;
            SetDisplayScale(1f);
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
            SetVisible(true);
            PileOcclusion.MarkDirty(this);
        }

        public void Launch(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            State = CarState.Loose;
            SetDisplayScale(1f);
            Shelf = null;
            SlotIndex = -1;
            gameObject.layer = Layers.LooseItems;
            transform.SetPositionAndRotation(position, rotation);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Body.isKinematic = false;
            Body.WakeUp();
            Body.linearVelocity = velocity;
            Body.angularVelocity = Random.insideUnitSphere * 3f;
            SetVisible(true);
        }

        public void SetHeld()
        {
            PileOcclusion.MarkDirty(this);
            State = CarState.Held;
            SetDisplayScale(1f);
            Shelf = null;
            SlotIndex = -1;
            Body.isKinematic = true;
            gameObject.SetActive(false);
        }

        public void SetPlaced(ShelfController shelf, int slot, Transform slotPoint)
        {
            State = CarState.Placed;
            SetDisplayScale(shelf != null && shelf.Data != null ? shelf.Data.DisplayScale : 1f);
            Shelf = shelf;
            SlotIndex = slot;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Body.isKinematic = true;
            gameObject.layer = Layers.StaticPlaced;
            transform.SetPositionAndRotation(SlotPose(slotPoint), slotPoint.rotation);
            SetVisible(true);
        }

        public Vector3 SlotPose(Transform slotPoint) => SlotPose(slotPoint, DisplayScale);

        public Vector3 SlotPose(Transform slotPoint, float scale) => slotPoint.position + slotPoint.up * (BaseHalfExtents.y * scale);

        public void Freeze()
        {
            if (!Body.isKinematic) Body.isKinematic = true;
            PileOcclusion.MarkDirty(this);
        }

        public void Unfreeze()
        {
            if (Body.isKinematic)
            {
                Body.isKinematic = false;
                Body.WakeUp();
                SetVisible(true);
                PileOcclusion.MarkDirty(this);
            }
        }

        public void SetVisible(bool visible)
        {
            if (Hidden != !visible) Hidden = !visible;
            var r = Rend;
            if (r != null && r.enabled != visible) r.enabled = visible;
        }
    }
}
