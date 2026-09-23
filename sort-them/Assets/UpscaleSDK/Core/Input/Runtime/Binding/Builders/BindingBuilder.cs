using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Processors.Abstractions;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Binding.Builders
{
    /// <summary>
    /// Represents a binding builder class.
    /// </summary>
    public sealed class BindingBuilder<T>
    {
        private readonly ActionBinding<T> _binding;
        private readonly IInputProvider _inputProvider;
        private readonly IGlyphProvider _glyphProvider;
        private readonly BindingsRegistry _registry;

        internal BindingBuilder(ActionBinding<T> binding, IInputProvider inputProvider, IGlyphProvider glyphProvider, BindingsRegistry registry)
        {
            _binding = binding;
            _inputProvider = inputProvider;
            _glyphProvider = glyphProvider;
            _registry = registry;
        }

        /*public BindingBuilder<T> ForPlayer(int player)
    {
        _binding.PlayerId = player;
        return this;
    }*/
        /// <summary>
        /// With source.
        /// </summary>
        /// <param name="source">The source.</param>
        public BindingBuilder<T> WithSource(IInputSource<T> source)
        {
            _binding.RegisterSource(source);
            return this;
        }
        
        /// <summary>
        /// With processor.
        /// </summary>
        /// <param name="processor">The processor.</param>
        /// <param name="clone">The clone.</param>
        public BindingBuilder<T> WithProcessor(IProcessor<T> processor, bool clone = false)
        {
            var processorToAdd = clone ? processor.Clone() : processor;
            _binding.RegisterProcessor(processorToAdd);
            return this;
        }
        
        /// <summary>
        /// Complete.
        /// </summary>
        public InputAction<T> Complete() 
        {
            //return UPSInput.Register(_binding);
            
            _registry.AddBinding(_binding);
            var action = new InputAction<T>(_binding, _inputProvider, _glyphProvider);
            _registry.AddProxy(_binding.Name, _binding.PlayerId, action);
            
            return action;
        }
    }
}