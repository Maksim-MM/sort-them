using System;
using UnityEngine;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a ui element class.
    /// </summary>
    public abstract class UIElement : MonoBehaviour
    {
        [SerializeField] private Layer _layer;
        /// <summary>
        /// Occurs when interacted.
        /// </summary>
        public event Action OnInteracted;
        /// <summary>
        /// Occurs when focus changed.
        /// </summary>
        public event Action<bool, bool> OnFocusChanged;
        /// <summary>
        /// Occurs when enabled changed.
        /// </summary>
        public event Action<bool, bool> OnEnabledChanged;
        
        /// <summary>
        /// Gets or sets the is in focus.
        /// </summary>
        public bool IsInFocus => _isInFocus;
        /// <summary>
        /// Gets or sets the is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;
        
        /// <summary>
        /// Gets or sets the layer.
        /// </summary>
        public Layer Layer => _layer;
        
        private bool _isInFocus;
        private bool _isEnabled = true;
        
        /// <summary>
        /// Sets the focus.
        /// </summary>
        /// <param name="isInFocus">The is in focus.</param>
        public void SetFocus(bool isInFocus)
        {
            bool oldValue = _isInFocus;
            _isInFocus = isInFocus;
            OnFocusChanged?.Invoke(oldValue, isInFocus);
        }

        /// <summary>
        /// Sets the enabled.
        /// </summary>
        /// <param name="isEnabled">The is enabled.</param>
        public void SetEnabled(bool isEnabled)
        {
            bool oldValue = _isEnabled;
            _isEnabled = isEnabled;
            OnEnabledChanged?.Invoke(oldValue, isEnabled);
        }

        /// <summary>
        /// Sets the layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public void SetLayer(Layer layer)
        {
            _layer = layer;
        }
        
        /// <summary>
        /// Interact.
        /// </summary>
        protected virtual void Interact() => OnInteracted?.Invoke();
    }
}