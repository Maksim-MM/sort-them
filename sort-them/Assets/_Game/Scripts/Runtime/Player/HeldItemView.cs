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
        public Vector3 RestPosition = new Vector3(0.22f, -0.4f, 0.7f);
        public Vector3 RestEuler = new Vector3(0f, -115f, 0f);
        public Vector3 EnterOffset = new Vector3(0.5f, -0.6f, 0f);
        public float Scale = 2f;
        public float FitHeight = 0.24f;
        public float ExitDuration = 0.08f;
        public float EnterDuration = 0.28f;
        public float SwayPerDegree = 0.000175f;
        public float SwayMax = 0.035f;
        public float SwayPerMeter = 0.0085f;
        public float SwayFrequency = 14f;
        public float SwayDamping = 0.9f;

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
        float _fit = 1f;
        Vector3 _frontLocal;
        int _seenAddCount;
        int _seenRemoveCount;
        Camera _cam;
        float _designFov;
        Vector3 _basePos;
        Quaternion _baseRot = Quaternion.identity;
        Vector3 _sway, _swayVel;
        Quaternion _prevCamRot;
        float _prevCamY;
        bool _swayInit;

        void Start()
        {
            _cam = GetComponentInParent<Camera>();
            if (_cam == null) _cam = Camera.main;
            _designFov = _cam != null ? _cam.fieldOfView : 0f;
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
            var prefab = CarAssets.Prefab(_shown);
            if (prefab == null)
            {
                _shown = null;
                _shownInstance = null;
                _phase = 0;
                Filter.transform.localScale = Vector3.zero;
                return;
            }
            var mf = prefab.GetComponentInChildren<MeshFilter>();
            Filter.sharedMesh = mf != null ? mf.sharedMesh : null;
            _fit = 1f;
            _frontLocal = Vector3.zero;
            if (Filter.sharedMesh != null)
            {
                var b = Filter.sharedMesh.bounds;
                _fit = Mathf.Min(1f, FitHeight / Mathf.Max(0.01f, b.size.y));
                _frontLocal = new Vector3(b.center.x, b.min.y, b.max.z);
            }
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

        void LateUpdate()
        {
            if (Filter == null) return;
            var cam = transform;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (!_swayInit || dt <= 0f)
            {
                _prevCamRot = cam.rotation;
                _prevCamY = cam.position.y;
                _swayInit = true;
                return;
            }
            var delta = (Quaternion.Inverse(_prevCamRot) * cam.rotation).eulerAngles;
            float yawRate = Mathf.DeltaAngle(0f, delta.y) / dt;
            float pitchRate = Mathf.DeltaAngle(0f, delta.x) / dt;
            float rise = (cam.position.y - _prevCamY) / dt;
            _prevCamRot = cam.rotation;
            _prevCamY = cam.position.y;
            var target = new Vector3(
                Mathf.Clamp(-yawRate * SwayPerDegree, -SwayMax, SwayMax),
                Mathf.Clamp(pitchRate * SwayPerDegree - rise * SwayPerMeter, -SwayMax, SwayMax),
                0f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
            float h = dt / steps;
            float w = SwayFrequency;
            for (int i = 0; i < steps; i++)
            {
                _swayVel += (w * w * (target - _sway) - 2f * SwayDamping * w * _swayVel) * h;
                _sway += _swayVel * h;
            }
            _sway = Vector3.ClampMagnitude(_sway, SwayMax * 1.5f);
            Compose();
        }

        void Compose()
        {
            var t = Filter.transform;
            float distance = Mathf.Max(0.1f, RestPosition.z);
            var lag = Quaternion.Euler(-_sway.y / distance * Mathf.Rad2Deg, _sway.x / distance * Mathf.Rad2Deg, 0f);
            t.localPosition = lag * _basePos;
            t.localRotation = lag * _baseRot;
        }

        public bool TryGetModelWorldPose(out Vector3 position, out Quaternion rotation)
        {
            var t = Filter.transform;
            position = t.position;
            rotation = t.rotation;
            return _shown != null && _phase != 1 && t.localScale.x > 0.01f;
        }

        float ViewScale()
        {
            if (_cam == null || _designFov <= 0f || PlayerController.MaxHorizontalFov <= 0f || _cam.aspect <= 0f) return 1f;
            float capped = Mathf.Min(_designFov, Camera.HorizontalToVerticalFieldOfView(PlayerController.MaxHorizontalFov, _cam.aspect));
            return Mathf.Tan(capped * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(_designFov * 0.5f * Mathf.Deg2Rad);
        }

        Vector3 RestLocal(float s)
        {
            float k = ViewScale();
            var rest = new Vector3(RestPosition.x * k, RestPosition.y * k, RestPosition.z);
            return rest - Quaternion.Euler(RestEuler) * (_frontLocal * s);
        }

        void ApplyFrom(float k)
        {
            var t = Filter.transform;
            float s = Scale * _fit * ViewScale();
            _basePos = Vector3.Lerp(_startPos, RestLocal(s), k);
            _baseRot = Quaternion.Slerp(_startRot, Quaternion.Euler(RestEuler), k);
            t.localScale = Vector3.one * Mathf.Lerp(_startScale, s, k);
            Compose();
        }

        void Apply(float offset, float scale)
        {
            var t = Filter.transform;
            float s = Scale * _fit * scale * ViewScale();
            _basePos = RestLocal(s) + EnterOffset * offset;
            _baseRot = Quaternion.Euler(RestEuler + new Vector3(0f, -40f * offset, 0f));
            t.localScale = Vector3.one * s;
            Compose();
        }
    }
}
