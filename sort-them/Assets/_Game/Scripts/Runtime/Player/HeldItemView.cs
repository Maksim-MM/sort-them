using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public class HeldItemView : MonoBehaviour
    {
        public Inventory Inventory;
        public MeshFilter Filter;
        public MeshRenderer Renderer;
        public Shader TexturedOverlayShader;
        readonly Dictionary<Material, Material> _heldMaterials = new Dictionary<Material, Material>();
        Material _defaultHeld;

        Material[] HeldMaterials(Material[] source)
        {
            if (_defaultHeld == null) _defaultHeld = Renderer.sharedMaterial;
            var result = new Material[Mathf.Max(1, source.Length)];
            for (int i = 0; i < result.Length; i++)
            {
                var m = i < source.Length ? source[i] : null;
                if (m == null || m.shader == null || m.shader.name != "SortThem/TexturedLit" || TexturedOverlayShader == null) { result[i] = _defaultHeld; continue; }
                if (!_heldMaterials.TryGetValue(m, out var held))
                {
                    held = new Material(TexturedOverlayShader) { name = m.name + "_Held" };
                    held.SetTexture("_BaseMap", m.GetTexture("_BaseMap"));
                    held.SetColor("_BaseColor", m.GetColor("_BaseColor"));
                    _heldMaterials[m] = held;
                }
                result[i] = held;
            }
            return result;
        }
        public Vector3 RestPosition = new Vector3(0.36f, -0.26f, 0.8f);
        public Vector3 RestEuler = new Vector3(0f, 145f, 0f);
        public Vector3 EnterOffset = new Vector3(0.35f, -0.4f, 0f);
        public float Scale = 0.85f;
        public float ExitDuration = 0.08f;
        public float EnterDuration = 0.28f;

        CarItemData _shown;
        CarItemData _pending;
        CarInstance _pendingInstance;
        CarInstance _shownInstance;
        float _t;
        int _phase;
        bool _fromWorld;
        Vector3 _startPos;
        Quaternion _startRot;
        float _startScale;
        int _seenAddCount;
        int _seenRemoveCount;

        void Start()
        {
            if (Inventory == null && GameManager.I != null) Inventory = GameManager.I.Inventory;
            if (Inventory != null) Inventory.Changed += OnInventoryChanged;
            if (Filter != null) Filter.transform.localScale = Vector3.zero;
            OnInventoryChanged();
        }

        void OnDestroy()
        {
            if (Inventory != null) Inventory.Changed -= OnInventoryChanged;
        }

        void OnInventoryChanged()
        {
            var active = Inventory != null ? Inventory.Active : null;
            var data = active != null ? active.Data : null;
            bool pickedUp = active != null && active == Inventory.LastAdded && Inventory.AddCount != _seenAddCount;
            bool removed = Inventory != null && Inventory.RemoveCount != _seenRemoveCount;
            if (!pickedUp && !removed)
            {
                if (active == _shownInstance && _phase == 0) return;
                if (active == _pendingInstance && _phase != 0) return;
            }
            _pending = data;
            _pendingInstance = active;
            if (removed)
            {
                _seenRemoveCount = Inventory.RemoveCount;
                _shown = null;
                Filter.transform.localScale = Vector3.zero;
                BeginEnter();
                return;
            }
            if (_shown != null && _phase != 1)
            {
                _phase = 1;
                _t = 0f;
            }
            else if (_shown == null)
            {
                BeginEnter();
            }
        }

        void BeginEnter()
        {
            _shown = _pending;
            _shownInstance = _pendingInstance;
            if (_shown == null || _shown.Prefab == null)
            {
                _shown = null;
                _shownInstance = null;
                _phase = 0;
                Filter.transform.localScale = Vector3.zero;
                return;
            }
            var mf = _shown.Prefab.GetComponentInChildren<MeshFilter>();
            Filter.sharedMesh = mf != null ? mf.sharedMesh : null;
            var srcRend = mf != null ? mf.GetComponent<MeshRenderer>() : null;
            if (Renderer != null && srcRend != null) Renderer.sharedMaterials = HeldMaterials(srcRend.sharedMaterials);
            _phase = 2;
            _t = 0f;
            _fromWorld = Inventory != null && _pendingInstance != null && _pendingInstance == Inventory.LastAdded && Inventory.AddCount != _seenAddCount;
            if (_fromWorld) _seenAddCount = Inventory.AddCount;
            if (_fromWorld)
            {
                var cam = transform;
                _startPos = cam.InverseTransformPoint(Inventory.LastAddedPosition);
                _startRot = Quaternion.Inverse(cam.rotation) * Inventory.LastAddedRotation;
                _startScale = 1f;
                ApplyFrom(0f);
            }
            else Apply(1f, 0f);
        }

        void Update()
        {
            if (_phase == 0) return;
            _t += Time.deltaTime;
            if (_phase == 1)
            {
                float k = Mathf.Clamp01(_t / ExitDuration);
                Apply(k, 1f - k);
                if (k >= 1f) BeginEnter();
            }
            else if (_phase == 2)
            {
                float k = Mathf.Clamp01(_t / EnterDuration);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                if (_fromWorld) ApplyFrom(e);
                else Apply(1f - e, 1f);
                if (k >= 1f)
                {
                    _phase = 0;
                    _fromWorld = false;
                }
            }
        }

        public bool TryGetModelWorldPose(out Vector3 position, out Quaternion rotation)
        {
            var t = Filter.transform;
            position = t.position;
            rotation = t.rotation;
            return _shown != null && _phase != 1 && t.localScale.x > 0.01f;
        }

        void ApplyFrom(float k)
        {
            var t = Filter.transform;
            t.localPosition = Vector3.Lerp(_startPos, RestPosition, k);
            t.localRotation = Quaternion.Slerp(_startRot, Quaternion.Euler(RestEuler), k);
            t.localScale = Vector3.one * Mathf.Lerp(_startScale, Scale, k);
        }

        void Apply(float offset, float scale)
        {
            var t = Filter.transform;
            t.localPosition = RestPosition + EnterOffset * offset;
            t.localRotation = Quaternion.Euler(RestEuler + new Vector3(0f, 40f * offset, 0f));
            t.localScale = Vector3.one * (Scale * scale);
        }
    }
}
