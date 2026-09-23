using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Ui.Runtime;

namespace UpscaleSDK.Core.Ui.Samples.Base
{
    public class DynamicGridDemo : MonoBehaviour
    {
        [SerializeField] private LayerNavigation _layerNavigation;
        [SerializeField] private LayerNavigator _layerNavigator;
        [SerializeField] private Layer _inventoryLayer;
        
        [SerializeField] private UIElement[] _staticElements;
        [SerializeField] private Transform _container;
        [SerializeField] private int _columns = 3;
        [SerializeField] private int _maxItems = 20;
        [SerializeField] private Vector2 _itemSize = new Vector2(100, 100);
        [SerializeField] private TMP_Text _label;
        
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _focusedColor = Color.yellow;
        [SerializeField] private float _focusScale = 1.2f;

        private List<InventoryItem> _inventoryItems = new();
        private string _startLabel;

        private void Start()
        {
            if (_container.GetComponent<GridLayoutGroup>() == null)
            {
                GridLayoutGroup grid = _container.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = _itemSize;
                grid.spacing = new Vector2(10, 10);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = _columns;
            }

            _startLabel = _label.text;
            RebuildNavigationGraph();
        }

        public void AddItem()
        {
            if (_inventoryItems.Count >= _maxItems)
            {
                return;
            }

            GameObject itemGO = new GameObject($"GridItem_{_inventoryItems.Count + 1}");
            itemGO.transform.SetParent(_container, false);

            RectTransform rectTransform = itemGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _itemSize;

            Image img = itemGO.AddComponent<Image>();
            img.color = _normalColor;

            InventoryItem uiElement = itemGO.AddComponent<InventoryItem>();
            uiElement.Initialize(_inventoryLayer, _normalColor, _focusedColor, _focusScale);

            _inventoryItems.Add(uiElement);
            _label.text = $"{_startLabel} {_inventoryItems.Count} / {_maxItems}";

            RebuildNavigationGraph();
        }

        public void RemoveItem()
        {
            if (_inventoryItems.Count == 0)
            {
                return;
            }

            InventoryItem itemToRemove = _inventoryItems[_inventoryItems.Count - 1];
            bool wasSelected = _layerNavigator.IsElementSelected(itemToRemove);
            
            _inventoryItems.RemoveAt(_inventoryItems.Count - 1);
            Destroy(itemToRemove.gameObject);
            
            _label.text = $"{_startLabel} {_inventoryItems.Count} / {_maxItems}";
            RebuildNavigationGraph();

            if (wasSelected)
            {
                if (_inventoryItems.Count > 0)
                    _layerNavigator.SelectElement(_inventoryItems[_inventoryItems.Count - 1]);
                else if (_staticElements.Length > 0)
                    _layerNavigator.SelectElement(_staticElements[0]);
            }
        }

        private void RebuildNavigationGraph()
        {
            List<Transition> transitions = new();

            if (_inventoryItems.Count > 0)
                transitions.AddRange(BuildGridTransitions());

            transitions.AddRange(BuildStaticTransitions());

            if (_inventoryItems.Count > 0)
                LinkGridToStatic(transitions);

            UIElement defaultElement = _inventoryItems.Count > 0 ? _inventoryItems[0] :
                                       _staticElements.Length > 0 ? _staticElements[0] : null;
            
            _layerNavigation.SetDefaultElement(defaultElement);
            _layerNavigation.SetTransitions(transitions.ToArray());
        }

        private List<Transition> BuildStaticTransitions()
        {
            List<Transition> transitions = new();

            for (int i = 0; i < _staticElements.Length; i++)
            {
                UIElement left = i > 0 ? _staticElements[i - 1] : null;
                UIElement right = i < _staticElements.Length - 1 ? _staticElements[i + 1] : null;
                transitions.Add(new Transition(_staticElements[i], null, null, left, right));
            }

            return transitions;
        }

        private List<Transition> BuildGridTransitions()
        {
            List<Transition> transitions = new();

            for (int i = 0; i < _inventoryItems.Count; i++)
            {
                int col = i % _columns;

                UIElement north = i - _columns >= 0 ? _inventoryItems[i - _columns] : null;
                UIElement south = i + _columns < _inventoryItems.Count ? _inventoryItems[i + _columns] : null;
                UIElement west = col > 0 ? _inventoryItems[i - 1] : null;
                UIElement east = col < _columns - 1 && i + 1 < _inventoryItems.Count ? _inventoryItems[i + 1] : null;

                transitions.Add(new Transition(_inventoryItems[i], north, south, west, east));
            }

            return transitions;
        }

        private void LinkGridToStatic(List<Transition> transitions)
        {
            if (_staticElements.Length == 0) return;

            int lastRowStart = (_inventoryItems.Count - 1) / _columns * _columns;
            int itemsInLastRow = _inventoryItems.Count - lastRowStart;

            for (int i = lastRowStart; i < _inventoryItems.Count; i++)
            {
                UpdateTransition(transitions, _inventoryItems[i], south: _staticElements[0]);
            }

            for (int i = 0; i < _staticElements.Length; i++)
            {
                int targetIndex = lastRowStart + Mathf.Min(i * itemsInLastRow / _staticElements.Length, itemsInLastRow - 1);
                UpdateTransition(transitions, _staticElements[i], north: _inventoryItems[targetIndex]);
            }
        }

        private void UpdateTransition(List<Transition> transitions, UIElement origin, 
                                     UIElement north = null, UIElement south = null)
        {
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].Origin == origin)
                {
                    Transition old = transitions[i];
                    transitions[i] = new Transition(
                        old.Origin,
                        north ?? old.North,
                        south ?? old.South,
                        old.West,
                        old.East
                    );
                    return;
                }
            }
        }
    }

    public class InventoryItem : UIElement
    {
        private Image _image;
        private Color _normalColor;
        private Color _focusedColor;
        private float _focusScale;

        public void Initialize(Layer layer, Color normalColor, Color focusedColor, float focusScale)
        {
            SetLayer(layer);
            _normalColor = normalColor;
            _focusedColor = focusedColor;
            _focusScale = focusScale;
            _image = GetComponent<Image>();
            
            OnFocusChanged += HandleFocusChanged;
        }

        private void OnDestroy()
        {
            OnFocusChanged -= HandleFocusChanged;
        }

        private void HandleFocusChanged(bool wasFocused, bool isFocused)
        {
            if (_image != null)
                _image.color = isFocused ? _focusedColor : _normalColor;

            transform.localScale = isFocused ? Vector3.one * _focusScale : Vector3.one;
        }
    }
}