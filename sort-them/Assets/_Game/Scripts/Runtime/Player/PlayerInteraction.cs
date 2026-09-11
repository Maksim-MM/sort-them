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
        public Collectible HoverCollectible { get; private set; }
        public Radio HoverRadio { get; private set; }
        public SlotMachine HoverSlotMachine { get; private set; }
        public CashRegister HoverRegister { get; private set; }
        public Bomb HoverBomb { get; private set; }
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

            bool tutorial = Tutorial.Running;
            if (!tutorial)
            {
                if (_next.WasPressedThisFrame() || TouchInput.Consume(TouchButton.Next)) Inventory.Next();
                if (_prev.WasPressedThisFrame() || TouchInput.Consume(TouchButton.Prev)) Inventory.Prev();
                HandleScroll(gm.Config);
            }
            if (!Tutorial.BlocksInteract && (Pressed(_interact) || TouchInput.Consume(TouchButton.Interact))) Interact();
            if (!Tutorial.BlocksPlace && (Pressed(_place) || TouchInput.Consume(TouchButton.Place))) PlaceOrThrow();
        }

        static bool Pressed(InputAction action)
        {
            if (!action.WasPressedThisFrame()) return false;
            if (TouchInput.Active && action.activeControl != null && action.activeControl.device is Mouse) return false;
            return true;
        }

        void ClearHover()
        {
            HoverCar = null;
            HoverShelf = null;
            HoverSlot = -1;
            HoverTerminal = null;
            HoverCollectible = null;
            HoverRadio = null;
            HoverSlotMachine = null;
            if (HoverRegister != null && GameManager.I != null) GameManager.I.ResetRegisterClicks();
            HoverRegister = null;
            HoverBomb = null;
            CanPlace = false;
            if (Outline != null) Outline.Hide();
            if (Ghost != null) Ghost.Hide();
        }

        void Scan()
        {
            bool hadRegister = HoverRegister != null;
            HoverCar = null;
            HoverShelf = null;
            HoverSlot = -1;
            HoverTerminal = null;
            HoverCollectible = null;
            HoverRadio = null;
            HoverSlotMachine = null;
            HoverRegister = null;
            HoverBomb = null;
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
                var bomb = col.GetComponentInParent<Bomb>();
                if (bomb != null)
                {
                    if (!bomb.Held) HoverBomb = bomb;
                    break;
                }
                var collectible = col.GetComponentInParent<Collectible>();
                if (collectible != null)
                {
                    HoverCollectible = collectible;
                    break;
                }
                var terminal = col.GetComponentInParent<UpgradeTerminal>();
                if (terminal != null)
                {
                    HoverTerminal = terminal;
                    break;
                }
                var radio = col.GetComponentInParent<Radio>();
                if (radio != null)
                {
                    HoverRadio = radio;
                    break;
                }
                var slotMachine = col.GetComponentInParent<SlotMachine>();
                if (slotMachine != null)
                {
                    HoverSlotMachine = slotMachine;
                    break;
                }
                var register = col.GetComponentInParent<CashRegister>();
                if (register != null)
                {
                    HoverRegister = register;
                    break;
                }
                break;
            }

            if (hadRegister && HoverRegister == null && GameManager.I != null) GameManager.I.ResetRegisterClicks();

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
            if (Tutorial.Running)
            {
                HoverTerminal = null;
                HoverCollectible = null;
                HoverRadio = null;
                HoverSlotMachine = null;
                HoverRegister = null;
                HoverBomb = null;
            }

            if (Outline != null)
            {
                if (HoverCar != null && HoverCar.Filter != null)
                    Outline.Show(HoverCar.Filter.sharedMesh, HoverCar.transform.position, HoverCar.transform.rotation, HoverCar.transform.lossyScale);
                else if (HoverCollectible != null && HoverCollectible.TryGetComponent<MeshFilter>(out var collMesh))
                    Outline.Show(collMesh.sharedMesh, HoverCollectible.transform.position, HoverCollectible.transform.rotation, HoverCollectible.transform.lossyScale);
                else if (HoverTerminal != null && HoverTerminal.TryGetComponent<MeshFilter>(out var termMesh))
                    Outline.Show(termMesh.sharedMesh, HoverTerminal.transform.position, HoverTerminal.transform.rotation, HoverTerminal.transform.lossyScale);
                else if (HoverRadio != null && HoverRadio.TryGetComponent<MeshFilter>(out var radioMesh))
                    Outline.Show(radioMesh.sharedMesh, HoverRadio.transform.position, HoverRadio.transform.rotation, HoverRadio.transform.lossyScale);
                else if (HoverSlotMachine != null && HoverSlotMachine.TryGetComponent<MeshFilter>(out var slotMesh))
                    Outline.Show(slotMesh.sharedMesh, HoverSlotMachine.transform.position, HoverSlotMachine.transform.rotation, HoverSlotMachine.transform.lossyScale);
                else if (HoverRegister != null && !GameManager.I.RegisterPaid && HoverRegister.TryGetComponent<MeshFilter>(out var registerMesh))
                    Outline.Show(registerMesh.sharedMesh, HoverRegister.transform.position, HoverRegister.transform.rotation, HoverRegister.transform.lossyScale);
                else if (HoverBomb != null && GameManager.I.HeldBomb == null && HoverBomb.TryGetComponent<MeshFilter>(out var bombMesh))
                    Outline.Show(bombMesh.sharedMesh, HoverBomb.transform.position, HoverBomb.transform.rotation, HoverBomb.transform.lossyScale);
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
            if (HoverBomb != null && GameManager.I.HeldBomb == null)
            {
                GameManager.I.PickBomb(HoverBomb, Cam.transform);
                return;
            }
            if (HoverCollectible != null)
            {
                GameManager.I.Collect(HoverCollectible);
                return;
            }
            if (HoverTerminal != null)
            {
                if (UiRoot.I != null) UiRoot.I.OpenTerminal();
                return;
            }
            if (HoverSlotMachine != null)
            {
                if (UiRoot.I != null) UiRoot.I.OpenSlot();
                return;
            }
            if (HoverRegister != null)
            {
                GameManager.I.ClickRegister(HoverRegister);
                return;
            }
            if (HoverRadio != null)
            {
                var music = GameManager.I.Music;
                if (music != null && music.TrackCount > 0)
                {
                    music.Next();
                    Messages.Show(string.Format(Loc.Get("msg.radio_track", "Радио: {0}/{1}"), music.Current + 1, music.TrackCount));
                }
                return;
            }
            if (HoverCar != null && !Inventory.IsFull) Take(HoverCar);
        }

        public void Take(CarInstance car)
        {
            if (car.State == CarState.Placed && car.Shelf != null) car.Shelf.Remove(car);
            Sfx.Play(GameManager.I.Config.PickupClip, car.transform.position, 1f, Random.Range(0.94f, 1.06f));
            Inventory.Add(car);
        }

        void PlaceOrThrow()
        {
            if (GameManager.I.HeldBomb != null)
            {
                ThrowBomb();
                return;
            }
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

        void ThrowBomb()
        {
            var gm = GameManager.I;
            Vector3 camPos = Cam.transform.position;
            Vector3 origin = gm.HeldBomb.transform.position;
            Vector3 toHand = origin - camPos;
            if (Physics.Raycast(camPos, toHand.normalized, out var block, toHand.magnitude + 0.05f, Layers.InteractMask, QueryTriggerInteraction.Ignore))
                origin = camPos + toHand.normalized * Mathf.Max(0.05f, block.distance - 0.05f);
            float distance = gm.Config.BaseThrowDistance * gm.Upgrades.Value(UpgradeKind.ThrowPower, 1f);
            float speed = Mathf.Sqrt(Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, distance));
            Vector3 dir = (Cam.transform.forward + Vector3.up * gm.Config.ThrowArc).normalized;
            gm.ThrowBomb(origin, Random.rotation, dir * speed);
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
            Sfx.Play(gm.Config.ThrowClip, origin);
            car.Launch(origin, rotation, dir * speed);
        }
    }
}
