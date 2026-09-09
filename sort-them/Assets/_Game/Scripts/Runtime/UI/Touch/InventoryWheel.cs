using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SortThem
{
    public class InventoryWheel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Inventory Inventory;
        public TMP_Text Counter;
        public TMP_Text[] Rows = new TMP_Text[5];
        public float RowHeight = 40f;
        public float SnapSpeed = 400f;

        const float TapThreshold = 12f;
        float _offset;
        bool _dragging;
        float _dragDistance;
        Canvas _canvas;
        RectTransform _rect;

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void Bind(Inventory inventory)
        {
            if (Inventory != null) Inventory.Changed -= Refresh;
            Inventory = inventory;
            if (Inventory != null && isActiveAndEnabled) Inventory.Changed += Refresh;
            Refresh();
        }

        void OnEnable()
        {
            if (Inventory != null) Inventory.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (Inventory != null) Inventory.Changed -= Refresh;
        }

        void Update()
        {
            if (_dragging || _offset == 0f) return;
            _offset = Mathf.MoveTowards(_offset, 0f, SnapSpeed * Time.unscaledDeltaTime);
            Layout();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _dragging = true;
            _dragDistance = 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (Inventory == null || Inventory.Items.Count < 2) return;
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            float dy = e.delta.y / Mathf.Max(0.01f, scale);
            _dragDistance += Mathf.Abs(dy);
            _offset += dy;
            while (_offset >= RowHeight * 0.5f) { _offset -= RowHeight; Step(1); }
            while (_offset <= -RowHeight * 0.5f) { _offset += RowHeight; Step(-1); }
            Layout();
        }

        public void OnEndDrag(PointerEventData e) => _dragging = false;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging || _dragDistance > TapThreshold || Inventory == null || Inventory.Items.Count < 2) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, e.position, e.pressEventCamera, out var local)) return;
            int k = Mathf.Clamp(Mathf.RoundToInt(-(local.y - _rect.rect.center.y) / RowHeight), -2, 2);
            if (k != 0 && Visible(k, Inventory.Items.Count)) Step(k);
        }

        void Step(int delta)
        {
            int n = Inventory.Items.Count;
            if (n == 0) return;
            Inventory.ActiveIndex = ((Inventory.ActiveIndex + delta) % n + n) % n;
            var gm = GameManager.I;
            if (gm != null) Sfx.PlayUi(gm.Config.UiMoveClip);
            Inventory.NotifyChanged();
        }

        static bool Visible(int k, int n)
        {
            if (n >= 5) return true;
            switch (n)
            {
                case 1: return k == 0;
                case 2: return k == 0 || k == 1;
                case 3: return k >= -1 && k <= 1;
                case 4: return k >= -1 && k <= 2;
                default: return false;
            }
        }

        public void Refresh()
        {
            if (Inventory == null) return;
            int n = Inventory.Items.Count;
            if (Counter != null) Counter.text = n + "/" + Inventory.Capacity;
            for (int k = -2; k <= 2; k++)
            {
                var row = Rows[k + 2];
                if (row == null) continue;
                bool visible = n > 0 && Visible(k, n);
                if (row.gameObject.activeSelf != visible) row.gameObject.SetActive(visible);
                if (!visible) continue;
                int index = ((Inventory.ActiveIndex + k) % n + n) % n;
                var d = Inventory.Items[index].Data;
                row.text = Loc.Get(d.DisplayName, d.DevName);
                bool center = k == 0;
                row.fontSize = center ? 17f : 13f;
                row.color = center ? Color.white : new Color(0.75f, 0.75f, 0.8f, Mathf.Abs(k) == 1 ? 0.9f : 0.55f);
            }
            Layout();
        }

        void Layout()
        {
            for (int k = -2; k <= 2; k++)
            {
                var row = Rows[k + 2];
                if (row == null) continue;
                row.rectTransform.anchoredPosition = new Vector2(0f, -k * RowHeight + _offset);
            }
        }
    }
}
