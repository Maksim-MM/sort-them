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
    /// Represents a vector2 action definition class.
    /// </summary>
    [CreateAssetMenu(fileName = "New Vector2 Action", menuName = "Upscale SDK/Input/Actions/Vector2 Action")]
    public class Vector2ActionDefinition : InputActionDefinition<Vector2>
    {
        [SerializeReference] private List<IInputSource<Vector2>> sources = new();
        [SerializeReference] private List<IProcessor<Vector2>> processors = new();
        
        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        public List<IInputSource<Vector2>> Sources => sources;
        /// <summary>
        /// Gets or sets the processors.
        /// </summary>
        public List<IProcessor<Vector2>> Processors => processors;
        
        /// <summary>
        /// Build.
        /// </summary>
        public override InputAction<Vector2> Build()
        {
            BindingBuilder<Vector2> builder = UPSInput.BindAsVector2(Guid.NewGuid().ToString());

            foreach (var source in Sources)
            {
                builder.WithSource(source);
            }
            
            foreach (var processor in Processors)
            {
                builder.WithProcessor(processor, true);
               // var clonedProcessor = CloneProcessor(processor);
                //builder.WithProcessor(clonedProcessor);
            }

            return builder.Complete();
        }
        
        private IProcessor<Vector2> CloneProcessor(IProcessor<Vector2> original)
        {
            string json = JsonUtility.ToJson(original);
            var clone = (IProcessor<Vector2>)JsonUtility.FromJson(json, original.GetType());
            return clone;
        }
        
        /*public IProcessor<T> CloneProcessor(IProcessor<T> original)
        {
            string json = JsonUtility.ToJson(original);
            return (IProcessor<T>)JsonUtility.FromJson(json, original.GetType());
        }*/
    }
}