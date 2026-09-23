using System;
using System.Collections.Generic;
using UpscaleSDK.Core.Input.Processors.Abstractions;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Binding
{
    /// <summary>
    /// Represents a action binding class.
    /// </summary>
    public abstract class ActionBinding<T> : ActionBinding
    {
        /// <summary>
        /// The player id field.
        /// </summary>
        public int PlayerId;
        /// <summary>
        /// Gets or sets the is active.
        /// </summary>
        public bool IsActive { get; private set; } = true;
        
        /// <summary>
        /// Gets or sets the last value.
        /// </summary>
        public T LastValue => _latestValue;

        /// <summary>
        /// Occurs when performed.
        /// </summary>
        public event Action<T> Performed;

        private readonly List<IInputSource<T>> _sources = new();
        private readonly List<IProcessor<T>> _processors = new();
        
        private T _latestValue;
        
        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        public List<IInputSource<T>> Sources => _sources;

        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public T Read(IInputProvider provider)
        {
            if (!IsActive) 
                return default;
        
            T value = default;

            foreach (var source in _sources)
                value = source.Accumulate(value, provider, PlayerId);

            foreach (var processor in _processors)
                value = processor.Process(value);
            
            _latestValue = value;
            return value;
        }

        /// <summary>
        /// Register processor.
        /// </summary>
        /// <param name="processor">The processor.</param>
        public void RegisterProcessor(IProcessor<T> processor)
        {
            _processors.Add(processor);
        }

        /// <summary>
        /// Register source.
        /// </summary>
        /// <param name="source">The source.</param>
        public void RegisterSource(IInputSource<T> source)
        {
            _sources.Add(source);
        }
    
        /// <summary>
        /// Invoke performed.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void InvokePerformed(T value)
        {
            if (!IsActive) 
                return;
        
            Performed?.Invoke(value);
        }

        /// <summary>
        /// Sets the active.
        /// </summary>
        /// <param name="value">The value.</param>
        public override void SetActive(bool value)
        {
            IsActive = value;
            _latestValue = default;
        }
    }

    /// <summary>
    /// Represents a action binding class.
    /// </summary>
    public abstract class ActionBinding
    {
        /// <summary>
        /// The name field.
        /// </summary>
        public string Name;
        /// <summary>
        /// Refresh.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public abstract void Refresh(IInputProvider provider);
        /// <summary>
        /// Sets the active.
        /// </summary>
        /// <param name="value">The value.</param>
        public abstract void SetActive(bool value);
    }
}