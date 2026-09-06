using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SortThem
{
    public class PlayerAbilities : MonoBehaviour
    {
        public Inventory Inventory;
        public Material LevitateOutlineMaterial;

        InputAction[] _actions;
        readonly float[] _cooldown = new float[3];
        float _findUntil, _rackUntil, _collectUntil, _collectNextPull;
        bool _rackActive;
        CarItemData _collectModel;
        int _collectBudget;
        Camera _cam;
        readonly List<CarInstance> _pulls = new List<CarInstance>();
        readonly List<Vector3> _pullFrom = new List<Vector3>();
        readonly List<float> _pullStart = new List<float>();
        readonly List<CarInstance> _levitating = new List<CarInstance>();
        readonly List<float> _levitateY = new List<float>();
        readonly List<MeshGhost> _outlines = new List<MeshGhost>();

        static readonly UpgradeKind[] Kinds = { UpgradeKind.DuplicateHighlight, UpgradeKind.AutoCollect, UpgradeKind.ShelfHighlight };

        public float CooldownRemaining(int index) => index >= 0 && index < 3 ? Mathf.Max(0f, _cooldown[index]) : 0f;
        public bool IsUnlocked(int index) => GameManager.I != null && GameManager.I.Upgrades.Has(Kinds[index]);
        public float ActiveRemaining(int index)
        {
            float until = index == 0 ? _findUntil : index == 1 ? _collectUntil : _rackUntil;
            return Mathf.Max(0f, until - Time.time);
        }

        void Start()
        {
            var map = GameManager.I.InputAsset.FindActionMap("Player", true);
            _actions = new[] { map.FindAction("Ability1", true), map.FindAction("Ability2", true), map.FindAction("Ability3", true) };
            _cam = Camera.main;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || _actions == null) return;
            for (int i = 0; i < 3; i++) _cooldown[i] -= Time.deltaTime;

            if (!gm.UiBlocking)
            {
                if (_actions[0].WasPressedThisFrame()) TryFindMatches(gm);
                if (_actions[1].WasPressedThisFrame()) TryAutoCollect(gm);
                if (_actions[2].WasPressedThisFrame()) TryRackHighlight(gm);
            }

            UpdateLevitation(gm);
            UpdateAutoCollect(gm);
            UpdateRackHighlight(gm);
        }

        static void NotReady() => Messages.Show(Loc.Get("msg.ability_not_ready", "Способность ещё не готова"));
        static void NeedItem() => Messages.Show(Loc.Get("msg.need_item_in_hands", "Возьми что-нибудь в руки"));

        void TryFindMatches(GameManager gm)
        {
            if (!gm.Upgrades.Has(UpgradeKind.DuplicateHighlight)) return;
            if (_cooldown[0] > 0f) { NotReady(); return; }
            var held = Inventory.Active;
            if (held == null) { NeedItem(); return; }
            EndLevitation();
            float targetY = transform.position.y + gm.Config.LevitateHeight;
            foreach (var car in gm.Cars)
            {
                if (car.State != CarState.Loose || car.Levitating || car.Data != held.Data) continue;
                car.Levitating = true;
                car.Body.isKinematic = true;
                float y = targetY;
                if (Physics.Raycast(car.transform.position, Vector3.up, out var hit, targetY - car.transform.position.y + car.HalfExtents.y, Layers.InteractMask, QueryTriggerInteraction.Ignore))
                    y = Mathf.Max(car.transform.position.y, hit.point.y - car.HalfExtents.y - 0.05f);
                _levitating.Add(car);
                _levitateY.Add(y);
            }
            if (_levitating.Count == 0) return;
            _findUntil = Time.time + gm.Config.AbilityDuration;
            _cooldown[0] = gm.Config.AbilityCooldown;
        }

        void UpdateLevitation(GameManager gm)
        {
            if (_levitating.Count == 0) return;
            if (Time.time >= _findUntil) { EndLevitation(); return; }
            float step = gm.Config.LevitateSpeed * Time.deltaTime;
            int shown = 0;
            for (int i = _levitating.Count - 1; i >= 0; i--)
            {
                var car = _levitating[i];
                if (car == null || car.State != CarState.Loose)
                {
                    if (car != null) car.Levitating = false;
                    _levitating.RemoveAt(i);
                    _levitateY.RemoveAt(i);
                    continue;
                }
                var t = car.transform;
                var pos = t.position;
                pos.y = Mathf.MoveTowards(pos.y, _levitateY[i], step);
                t.position = pos;
                t.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
                Outline(shown++).Show(car.Filter.sharedMesh, t.position, t.rotation, t.lossyScale);
            }
            for (int i = shown; i < _outlines.Count; i++) _outlines[i].Hide();
        }

        void EndLevitation()
        {
            foreach (var car in _levitating)
            {
                if (car == null) continue;
                car.Levitating = false;
                if (car.State == CarState.Loose) car.Unfreeze();
            }
            _levitating.Clear();
            _levitateY.Clear();
            foreach (var o in _outlines) o.Hide();
        }

        MeshGhost Outline(int index)
        {
            while (_outlines.Count <= index)
            {
                var go = new GameObject("LevitateOutline");
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = LevitateOutlineMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                var ghost = go.AddComponent<MeshGhost>();
                ghost.Filter = mf;
                ghost.Renderer = mr;
                go.SetActive(false);
                _outlines.Add(ghost);
            }
            return _outlines[index];
        }

        void TryAutoCollect(GameManager gm)
        {
            if (!gm.Upgrades.Has(UpgradeKind.AutoCollect)) return;
            if (_cooldown[1] > 0f) { NotReady(); return; }
            var held = Inventory.Active;
            if (held == null) { NeedItem(); return; }
            if (Inventory.IsFull) return;
            _collectModel = held.Data;
            _collectBudget = Inventory.Capacity - Inventory.Items.Count;
            _collectUntil = Time.time + gm.Config.AutoCollectDuration;
            _collectNextPull = Time.time;
            _cooldown[1] = gm.Config.AutoCollectCooldown;
        }

        Vector3 HandTarget()
        {
            var c = _cam != null ? _cam.transform : transform;
            return c.position + c.forward * 0.5f + c.right * 0.25f - c.up * 0.2f;
        }

        void UpdateAutoCollect(GameManager gm)
        {
            float flight = Mathf.Max(0.05f, gm.Config.AutoCollectFlightTime);
            Vector3 hand = HandTarget();
            for (int i = _pulls.Count - 1; i >= 0; i--)
            {
                var car = _pulls[i];
                if (car == null || car.State != CarState.Loose)
                {
                    if (car != null) car.Levitating = false;
                    RemovePull(i);
                    continue;
                }
                float t = (Time.time - _pullStart[i]) / flight;
                if (t >= 1f)
                {
                    car.Levitating = false;
                    if (!Inventory.Add(car)) car.Unfreeze();
                    RemovePull(i);
                    continue;
                }
                var pos = Vector3.Lerp(_pullFrom[i], hand, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 0.4f;
                car.transform.position = pos;
                car.transform.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
            }

            if (Time.time >= _collectUntil || Time.time < _collectNextPull) return;
            if (_collectBudget <= 0 || Inventory.Capacity - Inventory.Items.Count - _pulls.Count <= 0) { _collectUntil = 0f; return; }

            float r2 = gm.Config.AutoCollectRadius * gm.Config.AutoCollectRadius;
            Vector3 p = transform.position;
            CarInstance best = null; float bestD = float.MaxValue;
            foreach (var car in gm.Cars)
            {
                if (car.State != CarState.Loose || car.Levitating || car.Data != _collectModel) continue;
                float d = (car.transform.position - p).sqrMagnitude;
                if (d <= r2 && d < bestD) { bestD = d; best = car; }
            }
            if (best == null) { _collectNextPull = Time.time + 0.5f; return; }
            best.Levitating = true;
            best.Body.isKinematic = true;
            _pulls.Add(best);
            _pullFrom.Add(best.transform.position);
            _pullStart.Add(Time.time);
            _collectBudget--;
            _collectNextPull = Time.time + gm.Config.AutoCollectInterval;
        }

        void RemovePull(int i)
        {
            _pulls.RemoveAt(i);
            _pullFrom.RemoveAt(i);
            _pullStart.RemoveAt(i);
        }

        void TryRackHighlight(GameManager gm)
        {
            if (!gm.Upgrades.Has(UpgradeKind.ShelfHighlight)) return;
            if (_cooldown[2] > 0f) { NotReady(); return; }
            if (Inventory.Active == null) { NeedItem(); return; }
            _rackUntil = Time.time + gm.Config.AbilityDuration;
            _cooldown[2] = gm.Config.AbilityCooldown;
        }

        void UpdateRackHighlight(GameManager gm)
        {
            bool active = Time.time < _rackUntil;
            if (!active && !_rackActive) return;
            var held = Inventory.Active;
            foreach (var rack in gm.Racks)
                rack.SetHighlight(active && held != null && rack.Category == held.Data.Category);
            _rackActive = active;
        }
    }
}
