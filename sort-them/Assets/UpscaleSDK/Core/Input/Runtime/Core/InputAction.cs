using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Binding;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Provider.Abstractions;

namespace UpscaleSDK.Core.Input.Core
{
    /// <summary>
    /// Represents a input action class.
    /// </summary>
    public class InputAction<T> : IDisposable
    {
        private ActionBinding<T> _binding;
        private IInputProvider _inputProvider;
        private IGlyphProvider _glyphProvider;
        
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => _binding.Name;

        public event Action<T> Performed
        {
            add    => _binding.Performed += value;
            remove => _binding.Performed -= value;
        }

        public InputAction(ActionBinding<T> binding, IInputProvider inputProvider, IGlyphProvider glyphProvider)
        {
            _binding = binding;
            _inputProvider = inputProvider;
            _glyphProvider = glyphProvider;
        }

        /// <summary>
        /// Read.
        /// </summary>
        public T Read()
        {
            return _binding.LastValue;
            // return _binding.Read(_inputProvider);
        }

        /// <summary>
        /// Attempts to get glyph.
        /// </summary>
        public Sprite TryGetGlyph()
        {
            return _glyphProvider.TryProvide(_binding.Sources);
        }

        public void Dispose()
        {
            _binding = null;
            _inputProvider = null;
            _glyphProvider = null;
        }
    }
}