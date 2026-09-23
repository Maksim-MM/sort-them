using System;
using System.Collections.Generic;
using System.Linq;
using UpscaleSDK.Core.Input.Binding;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Input.Core
{
    /// <summary>
    /// Represents a bindings registry class.
    /// </summary>
    public class BindingsRegistry
    {
        private Dictionary<(string, int), ActionBinding> _bindings = new();
        private Dictionary<(string, int), IDisposable> _activeProxies = new();

        /// <summary>
        /// Add binding.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="playerId">The player id.</param>
        public void AddBinding(ActionBinding binding, int playerId = 0)
        {
            var key = (binding.Name, playerId);
            if (_bindings.ContainsKey(key))
            {
                UPSLogger.InputDevLog($"[INPUT] Overwriting binding for '{binding.Name}'");
            }
            _bindings[key] = binding;
        }

        /// <summary>
        /// Add proxy.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="playerId">The player id.</param>
        /// <param name="proxy">The proxy.</param>
        public void AddProxy(string name, int playerId, IDisposable proxy)
        {
            _activeProxies[(name, playerId)] = proxy;
        }

        /// <summary>
        /// Attempts to get binding.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="playerId">The player id.</param>
        /// <param name="binding">The binding.</param>
        public bool TryGetBinding(string name, int playerId, out ActionBinding binding)
        {
            return _bindings.TryGetValue((name, playerId), out binding);
        }

        /// <summary>
        /// Attempts to get proxy.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="playerId">The player id.</param>
        /// <param name="proxy">The proxy.</param>
        public bool TryGetProxy<T>(string name, int playerId, out T proxy)
        {
            if (_activeProxies.TryGetValue((name, playerId), out var obj) && obj is T typedProxy)
            {
                proxy = typedProxy;
                return true;
            }
            proxy = default;
            return false;
        }

        /// <summary>
        /// Remove.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="playerId">The player id.</param>
        public void Remove(string name, int playerId)
        {
            var key = (name, playerId);
            if (_activeProxies.TryGetValue(key, out var proxy))
            {
                proxy.Dispose();
                _activeProxies.Remove(key);
            }
            _bindings.Remove(key);
        }

        /// <summary>
        /// Update all.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public void UpdateAll(IInputProvider provider)
        {
            foreach (var binding in _bindings.Values.ToArray())
            {
                binding.Refresh(provider);
            }
        }

        /// <summary>
        /// Gets the all.
        /// </summary>
        public IEnumerable<ActionBinding> GetAll() => _bindings.Values;
    }
}