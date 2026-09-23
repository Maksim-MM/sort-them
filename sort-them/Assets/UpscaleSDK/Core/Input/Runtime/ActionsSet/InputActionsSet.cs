using System;
using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Input.ActionsSet.Builders;
using UpscaleSDK.Core.Input.Core;

namespace UpscaleSDK.Core.Input.ActionsSet
{
    /// <summary>
    /// Represents a input actions set class.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class InputActionsSet<T> : IDisposable where T : InputActionsSet<T>, new()
    {
        private string _id;
        /// <summary>
        /// Gets or sets the is active.
        /// </summary>
        public bool IsActive { get; private set; } = true;
        
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }
       
        protected InputActionsSet()
        {
            _id = typeof(T).Name + '/';
            Initialize(new InputSetBuilder(_id));
        }
    
        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected abstract void Initialize(InputSetBuilder builder);
    
        /// <summary>
        /// Deactivate.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            SetActiveAllBindings(IsActive);
        }
        /// <summary>
        /// Activate.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            SetActiveAllBindings(IsActive);
        }

        public void Dispose()
        {
            IsActive = false;
            DestroyAllActions();
        }

        private void SetActiveAllBindings(bool active)
        {
            foreach (var binding in UPSInput.GetAllBindings())
            {
                if (binding.Name.StartsWith(_id))
                {
                    binding.SetActive(active);
                }
            }
        }
    
        private void DestroyAllActions()
        {
            var toRemove = new List<string>();

            foreach (var binding in UPSInput.GetAllBindings())
            {
                if (binding.Name.StartsWith(_id))
                {
                    toRemove.Add(binding.Name);
                }
            }

            foreach (var key in toRemove)
            {
                UPSInput.Unbind(key);
            }
        }
    }
}