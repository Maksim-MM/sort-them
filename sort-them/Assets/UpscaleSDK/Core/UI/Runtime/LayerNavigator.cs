using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a layer navigator class.
    /// </summary>
    public class LayerNavigator : MonoBehaviour
    {
        [SerializeField] private Vector2ActionDefinition _definition;
        [SerializeField] private Layer _layer;
        [SerializeField] private LayerNavigation _navigation;

        private InputAction<Vector2> _action;
        /// <summary>
        /// Occurs when select element.
        /// </summary>
        public event Action<UIElement, UIElement> OnSelectElement;

        private UIElement _currentlySelectedElement;

        private void Awake()
        {
            _action = _definition.Build();
        }

        private void Update()
        {
            TryInitialize();
            OnNavigate(_action.Read());
        }

        private void TryInitialize()
        {
            if (_currentlySelectedElement == null)
            {
                SelectElement(_navigation.GetDefaultOrFirstElement());
            }
        }
        
        /// <summary>
        /// Indicates whether element selected.
        /// </summary>
        /// <param name="element">The element.</param>
        public bool IsElementSelected(UIElement element)
        {
            return _currentlySelectedElement == element;
        }

        private void OnNavigate(Vector2 data)
        {
            if (_layer.IsEnabled == false) return;

      
            Transition transition = _navigation.GetTransitionForElement(_currentlySelectedElement);
            if (transition == null) return;
        
            if (Mathf.Abs(data.x) > Mathf.Abs(data.y))
            {
                if (data.x > 0f && transition.East != null)
                    SelectElement(transition.East);
                else if (data.x < 0f && transition.West != null)
                    SelectElement(transition.West);
            }
            else
            {
                if (data.y > 0f && transition.North != null)
                    SelectElement(transition.North);
                else if (data.y < 0f && transition.South != null)
                    SelectElement(transition.South);
            }
        }

        /// <summary>
        /// Select element.
        /// </summary>
        /// <param name="nextElement">The next element.</param>
        public void SelectElement(UIElement nextElement)
        {
            if (_navigation.GetAllElements().Contains(nextElement) == false)
            {
                UPSLogger.UiErrorLog($"Element is not part of this layer. Name: {nextElement.name}");
                return;
            }
            var previousElement = _currentlySelectedElement;
            _currentlySelectedElement?.SetFocus(false);
            _currentlySelectedElement = nextElement;
            _currentlySelectedElement.SetFocus(true);
        
            OnSelectElement?.Invoke(previousElement, _currentlySelectedElement);
        }
    }
}