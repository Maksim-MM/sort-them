using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem
{
    public class PlayerInteraction : MonoBehaviour
    {
        public Camera Cam;
        public Inventory Inventory;
        public MeshGhost Outline;
        public MeshGhost Ghost;

        public CarInstance HoverCar { get; private set; }
        public ShelfController HoverShelf { get; private set; }
        public int HoverSlot { get; private set; } = -1;
        public UpgradeTerminal HoverTerminal { get; private set; }
        public bool CanPlace { get; private set; }
        public float Range { get; private set; }

        InputAction _interact, _place, _next, _prev, _scroll;
        float _scrollAccum, _lastScrollInput = -10f, _lastScrollSwitch = -10f;
        HeldItemView _heldView;
        readonly RaycastHit[] _hits = new RaycastHit[24];

        void Start()
        {
            var map = GameManager.I.InputAsset.FindActionMap("Player", true);
            _interact = map.FindAction("Interact", true);
            _place = map.FindAction("PlaceOrThrow", true);
            _next = map.FindAction("NextItem", true);
            _prev = map.FindAction("PrevItem", true);
            _scroll = map.FindAction("ScrollItems", false);
            _heldView = FindFirstObjectByType<HeldItemView>();
        }

        void HandleScroll(GameConfig cfg)
        {
            if (_scroll == null) return;
            float delta = _scroll.ReadValue<float>();
            if (Time.time - _lastScrollInput > cfg.ScrollIdleReset) _scrollAccum = 0f;
            if (Mathf.Abs(delta) < 0.001f) return;
            _lastScrollInput = Time.time;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.lKey.isPressed) Debug.Log($"scroll delta {delta:F3}");
#endif
            if (Mathf.Abs(delta) >= cfg.ScrollNotchThreshold)
            {
                _scrollAccum = 0f;
                if (delta > 0f) Inventory.Next(); else Inventory.Prev();
                _lastScrollSwitch = Time.time;
                return;
            }
            _scrollAccum += delta;
            if (Mathf.Abs(_scrollAccum) < cfg.ScrollThreshold) return;
            if (Time.time - _lastScrollSwitch < cfg.ScrollMinInterval) { _scrollAccum = Mathf.Sign(_scrollAccum) * cfg.ScrollThreshold; return; }
            if (_scrollAccum > 0f) Inventory.Next(); else Inventory.Prev();
            _scrollAccum = 0f;
            _lastScrollSwitch = Time.time;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || _interact == null) return;
            if (gm.UiBlocking)
            {
                ClearHover();
                return;
            }

            Range = gm.Config.BaseInteractRange * gm.Upgrades.Value(UpgradeKind.Range, 1f);
            Scan();

            if (_next.WasPressedThisFrame()) Inventory.Next();
            if (_prev.WasPressedThisFrame()) Inventory.Prev();
            HandleScroll(gm.Config);
            if (_interact.WasPressedThisFrame()) Interact();
            if (_place.WasPressedThisFrame()) PlaceOrThrow();
        }

        void ClearHover()
        {
            HoverCar = null;
            HoverShelf = null;
            HoverSlot = -1;
            HoverTerminal = null;
            CanPlace = false;
            if (Outline != null) Outline.Hide();
            if (Ghost != null) Ghost.Hide();
        }

        void Scan()
        {
            HoverCar = null;
            HoverShelf = null;
            HoverSlot = -1;
            HoverTerminal = null;
            CanPlace = false;

            var ray = new Ray(Cam.transform.position, Cam.transform.forward);
            int n = Physics.RaycastNonAlloc(ray, _hits, Range, Layers.InteractMask, QueryTriggerInteraction.Collide);
            for (int i = 1; i < n; i++)
            {
                var h = _hits[i];
                int j = i - 1;
                while (j >= 0 && _hits[j].distance > h.distance)
                {
                    _hits[j + 1] = _hits[j];
                    j--;
                }
                _hits[j + 1] = h;
            }

            float solidDistance = float.MaxValue;
            ShelfController zoneShelf = null;
            for (int i = 0; i < n; i++)
            {
                var col = _hits[i].collider;
                if (col.isTrigger)
                {
                    if (zoneShelf == null)
                    {
                        var zone = col.GetComponent<ShelfZone>();
                        if (zone != null && zone.Shelf != null && _hits[i].distance <= solidDistance) zoneShelf = zone.Shelf;
                    }
                    continue;
                }

                if (_hits[i].distance > solidDistance) break;
                solidDistance = _hits[i].distance;

                var body = col.attachedRigidbody;
                var car = body != null ? body.GetComponent<CarInstance>() : col.GetComponent<CarInstance>();
                if (car != null)
                {
                    HoverCar = car;
                    break;
                }
                var terminal = col.GetComponentInParent<UpgradeTerminal>();
                if (terminal != null)
                {
                    HoverTerminal = terminal;
                    break;
                }
                break;
            }

            if (HoverCar != null && HoverCar.State == CarState.Placed && HoverCar.Shelf != null)
                HoverCar = HoverCar.Shelf.LastPlaced();
            else if (HoverCar == null && zoneShelf != null)
                HoverCar = zoneShelf.LastPlaced();

            var active = Inventory.Active;
            if (zoneShelf != null && active != null && zoneShelf.Accepts(active.Data))
            {
                HoverShelf = zoneShelf;
                HoverSlot = zoneShelf.FirstFreeSlot();
                CanPlace = HoverSlot >= 0;
            }
            if (!CanPlace) HoverShelf = null;

            if (Outline != null)
            {
                if (HoverCar != null && HoverCar.Filter != null)
                    Outline.Show(HoverCar.Filter.sharedMesh, HoverCar.transform.position, HoverCar.transform.rotation, HoverCar.transform.lossyScale);
                else if (HoverTerminal != null && HoverTerminal.TryGetComponent<MeshFilter>(out var termMesh))
                    Outline.Show(termMesh.sharedMesh, HoverTerminal.transform.position, HoverTerminal.transform.rotation, HoverTerminal.transform.lossyScale);
                else
                    Outline.Hide();
            }
            if (Ghost != null)
            {
                if (CanPlace && active != null && active.Filter != null)
                {
                    var slot = HoverShelf.SlotPoints[HoverSlot];
                    Ghost.Show(active.Filter.sharedMesh, active.SlotPose(slot), slot.rotation, active.transform.lossyScale);
                }
                else Ghost.Hide();
            }
        }

        void Interact()
        {
            if (HoverTerminal != null)
            {
                if (UiRoot.I != null) UiRoot.I.OpenTerminal();
                return;
            }
            if (HoverCar != null && !Inventory.IsFull) Take(HoverCar);
        }

        public void Take(CarInstance car)
        {
            if (car.State == CarState.Placed && car.Shelf != null) car.Shelf.Remove(car);
            Inventory.Add(car);
        }

        void PlaceOrThrow()
        {
            var car = Inventory.Active;
            if (car == null) return;
            if (CanPlace && HoverShelf != null && HoverShelf.Accepts(car.Data))
            {
                Vector3 fromPos = Cam.transform.position + Cam.transform.forward * 0.5f;
                Quaternion fromRot = car.transform.rotation;
                if (_heldView != null && _heldView.TryGetModelWorldPose(out var handPos, out var handRot)) { fromPos = handPos; fromRot = handRot; }
                Inventory.RemoveActive();
                if (!HoverShelf.TryPlaceAnimated(car, HoverSlot, fromPos, fromRot, GameManager.I.Config.PlaceFlightDuration)) Inventory.Add(car);
                return;
            }
            Throw(car);
        }

        void Throw(CarInstance car)
        {
            var gm = GameManager.I;
            Vector3 camPos = Cam.transform.position;
            Vector3 origin = camPos + Cam.transform.forward * 0.6f;
            Quaternion rotation = Random.rotation;
            if (_heldView != null && _heldView.TryGetModelWorldPose(out var handPos, out var handRot))
            {
                origin = handPos;
                rotation = handRot;
            }
            Vector3 toHand = origin - camPos;
            if (Physics.Raycast(camPos, toHand.normalized, out var block, toHand.magnitude + car.HalfExtents.magnitude, Layers.InteractMask, QueryTriggerInteraction.Ignore))
                origin = camPos + toHand.normalized * Mathf.Max(0.05f, block.distance - car.HalfExtents.magnitude);
            Inventory.RemoveActive();
            float distance = gm.Config.BaseThrowDistance * gm.Upgrades.Value(UpgradeKind.ThrowPower, 1f);
            float speed = Mathf.Sqrt(Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, distance));
            Vector3 dir = (Cam.transform.forward + Vector3.up * gm.Config.ThrowArc).normalized;
            car.Launch(origin, rotation, dir * speed);
        }
    }
}
