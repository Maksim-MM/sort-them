using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a layer navigation class.
    /// </summary>
    public class LayerNavigation : MonoBehaviour
    {
        [SerializeField] private UIElement _defaultElement;
        [SerializeField] private Transition[] _transitions;
        
        /// <summary>
        /// Sets the transitions.
        /// </summary>
        /// <param name="transitions">The transitions.</param>
        public void SetTransitions(Transition[] transitions)
        {
            _transitions = transitions;
        }
        
        /// <summary>
        /// Sets the default element.
        /// </summary>
        /// <param name="element">The element.</param>
        public void SetDefaultElement(UIElement element)
        {
            _defaultElement = element;
        }

        /// <summary>
        /// Gets the transitions.
        /// </summary>
        public Transition[] GetTransitions()
        {
            return _transitions;
        }
        
        
        /// <summary>
        /// Gets the all elements.
        /// </summary>
        public List<UIElement> GetAllElements()
        {
            var elements = new List<UIElement>();
            elements.Add(_defaultElement);
            foreach (var transition in _transitions)
            {
                if (elements.Contains(transition.Origin) == false)
                    elements.Add(transition.Origin);
                if (transition.North != null && elements.Contains(transition.North) == false)
                    elements.Add(transition.North);
                if (transition.South != null && elements.Contains(transition.South) == false)
                    elements.Add(transition.South);
                if (transition.West != null && elements.Contains(transition.West) == false)
                    elements.Add(transition.West);
                if (transition.East != null && elements.Contains(transition.East) == false)
                    elements.Add(transition.East);
            }

            return elements;
        }

        /// <summary>
        /// Gets the transition for element.
        /// </summary>
        /// <param name="element">The element.</param>
        public Transition GetTransitionForElement(UIElement element)
        {
            foreach (var transition in _transitions)
            {
                if (transition.Origin == element)
                    return transition;
            }

            return null;
        }

        /// <summary>
        /// Gets the default or first element.
        /// </summary>
        public UIElement GetDefaultOrFirstElement()
        {
            if (_defaultElement != null)
            {
                return _defaultElement;
            }

            return _transitions.FirstOrDefault()?.Origin;
        }
    }
}