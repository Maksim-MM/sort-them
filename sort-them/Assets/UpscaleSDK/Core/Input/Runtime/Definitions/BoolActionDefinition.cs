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
    /// Represents a bool action definition class.
    /// </summary>
    [CreateAssetMenu(fileName = "New Bool Action", menuName = "Upscale SDK/Input/Actions/Bool Action")]
    public class BoolActionDefinition : InputActionDefinition<bool>
    {
        [SerializeReference] private List<IInputSource<bool>> sources = new();
        [SerializeReference] private List<IProcessor<bool>> processors = new();
        
        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        public List<IInputSource<bool>> Sources => sources;
        /// <summary>
        /// Gets or sets the processors.
        /// </summary>
        public List<IProcessor<bool>> Processors => processors;
        
        /// <summary>
        /// Build.
        /// </summary>
        public override InputAction<bool> Build()
        {
            BindingBuilder<bool> builder = UPSInput.BindAsBool(Guid.NewGuid().ToString());

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


    /// <summary>
    /// Represents a input action definition class.
    /// </summary>
    public abstract class InputActionDefinition : ScriptableObject
    {
        //[SerializeField] protected string actionName = "NewAction";
        //[SerializeField] protected int playerId = 0;
        
        //public string ActionName => actionName;
       // public int PlayerId => playerId;
        
        /// <summary>
        /// Builds the runtime InputAction from this definition.
        /// Called at runtime to instantiate the actual input action.
        /// </summary>
        public abstract object BuildAction();
    }
    
    /// <summary>
    /// Generic base for type-safe action building
    /// </summary>
    public abstract class InputActionDefinition<T> : InputActionDefinition
    {
        /// <summary>
        /// Build.
        /// </summary>
        public abstract InputAction<T> Build();
        
        /// <summary>
        /// Build action.
        /// </summary>
        public override object BuildAction()
        {
            return Build();
        }
    }
}