using System;
using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Processors.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Definitions
{
    /// <summary>
    /// Represents a float action definition class.
    /// </summary>
    [CreateAssetMenu(fileName = "New float Action", menuName = "Upscale SDK/Input/Actions/Float Action")]
    public class FloatActionDefinition : InputActionDefinition<float>
    {
        [SerializeReference] private List<IInputSource<float>> sources = new();
        [SerializeReference] private List<IProcessor<float>> processors = new();
        
        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        public List<IInputSource<float>> Sources => sources;
        /// <summary>
        /// Gets or sets the processors.
        /// </summary>
        public List<IProcessor<float>> Processors => processors;
        
        /// <summary>
        /// Build.
        /// </summary>
        public override InputAction<float> Build()
        {
            BindingBuilder<float> builder = UPSInput.BindAsFloat(Guid.NewGuid().ToString());

            foreach (var source in Sources)
            {
                builder.WithSource(source);
            }
            
            foreach (var processor in Processors)
            {
                builder.WithProcessor(processor, true);
            }

            return builder.Complete();
        }
    }
}