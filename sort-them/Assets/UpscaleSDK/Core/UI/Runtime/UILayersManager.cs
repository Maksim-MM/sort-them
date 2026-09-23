using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a ui layers manager class.
    /// </summary>
    public static class UILayersManager
    {
        private static List<Layer> _layers = new();

        private static Layer _currentActiveLayer;
        /// <summary>
        /// Gets or sets the current active layer.
        /// </summary>
        public static Layer CurrentActiveLayer => _currentActiveLayer;
        
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _currentActiveLayer = null;
            _layers = new();
        }
#endif
        
        /// <summary>
        /// Register layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public static void RegisterLayer(Layer layer)
        {
            var existingLayer = _layers.Find((l) => l.Key == layer.Key);
            if (existingLayer != null)
            {
                _layers.Remove(existingLayer);
                UPSLogger.UiDevLog($"Layer with key {layer.Key} is already registered. Replacing it.");
            }
            
            _layers.Add(layer);
        }
        
        /// <summary>
        /// Activate layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public static void ActivateLayer(Layer layer)
        {
            if (_currentActiveLayer != null)
            {
                _currentActiveLayer?.SetEnabled(false);
            }

            _currentActiveLayer = layer;
            _currentActiveLayer?.SetEnabled(true);
        }
        
        /// <summary>
        /// Unregister layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public static void UnregisterLayer(Layer layer)
        {
            if (layer == null)
                return;
            
            if (_currentActiveLayer == layer)
            {
                _currentActiveLayer.SetEnabled(false);
                _currentActiveLayer = null;
            }
            
            _layers.Remove(layer);
        }
        
        /// <summary>
        /// Activate layer.
        /// </summary>
        /// <param name="layerKey">The layer key.</param>
        public static void ActivateLayer(string layerKey)
        {
            var layer = _layers.Find((l) => l.Key == layerKey);
            if (layer == null)
            {
                UPSLogger.UiInfoLog($"Layer with key {layerKey} is not registered");
                return;
            }

            ActivateLayer(layer);
        }
    }
}