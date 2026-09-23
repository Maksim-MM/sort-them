using UnityEngine;
using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Core;

namespace UpscaleSDK.Core.Input.ActionsSet.Builders
{
    /// <summary>
    /// Represents a input set builder class.
    /// </summary>
    public class InputSetBuilder
    {
        private readonly string _setId;
    
        private static int _nextActionId = 1;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetId()
        {
            _nextActionId = 1;
        }
#endif

        private static int GetNextId()
        {
            //Debug.Log(_nextActionId);
            return _nextActionId++;
        }
    
        public InputSetBuilder(string setId)
        {
            _setId = setId;
        }

        /// <summary>
        /// Bind as bool.
        /// </summary>
        /// <param name="action">The action.</param>
        public BindingBuilder<bool> BindAsBool(string action = null)
        {
            return UPSInput.BindAsBool(Scoped(action));
        }

        /// <summary>
        /// Bind as vector2.
        /// </summary>
        /// <param name="action">The action.</param>
        public BindingBuilder<Vector2> BindAsVector2(string action = null)
        {
            return UPSInput.BindAsVector2(Scoped(action));
        }

        /// <summary>
        /// Bind as float.
        /// </summary>
        /// <param name="action">The action.</param>
        public BindingBuilder<float> BindAsFloat(string action = null)
        {
            return UPSInput.BindAsFloat(Scoped(action));
        }
    
        private string Scoped(string action)
        {
            if (string.IsNullOrEmpty(action))
                action = GetNextId().ToString();

            return $"{_setId}{action}";
        }
    }
}