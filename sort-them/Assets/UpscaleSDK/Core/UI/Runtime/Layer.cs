using UnityEngine;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a layer class.
    /// </summary>
    public class Layer : MonoBehaviour
    {
        [SerializeField] private string _key;
        private bool _isEnabled = true;
        
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key => _key;
        /// <summary>
        /// Gets or sets the is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;
        
        /// <summary>
        /// Sets the enabled.
        /// </summary>
        /// <param name="isEnabled">The is enabled.</param>
        public void SetEnabled(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        private void Start()
        {
            UILayersManager.RegisterLayer(this);
        }

        private void OnDestroy()
        {
            UILayersManager.UnregisterLayer(this);
        }
    }
}