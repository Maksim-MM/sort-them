using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Processors.Abstractions;

namespace UpscaleSDK.Core.Input.Processors
{
    /// <summary>
    /// Represents a delay vector2 processor class.
    /// </summary>
    [Serializable]
    public sealed class DelayVector2Processor : IProcessor<Vector2>
    {
        [SerializeField] private float initialDelay = 0.4f;
        [SerializeField] private float repeatDelay = 0.15f;
        [SerializeField] private float deadzone = 0.1f;

        private bool _hasInput;
        private double _lastTime;
        private float _currentDelay;

        /// <summary>
        /// Process.
        /// </summary>
        /// <param name="value">The value.</param>
        public Vector2 Process(Vector2 value)
        {
            if (value.sqrMagnitude < deadzone * deadzone)
            {
                _hasInput = false;
                return Vector2.zero;
            }

            double now = Time.unscaledTimeAsDouble;

            if (!_hasInput)
            {
                _hasInput = true;
                _currentDelay = initialDelay;
                _lastTime = now;

                return NormalizeToAxis(value);
            }

            if (now - _lastTime < _currentDelay)
                return Vector2.zero;

            _currentDelay = repeatDelay;
            _lastTime = now;

            return NormalizeToAxis(value);
        }

        /// <summary>
        /// Clone.
        /// </summary>
        public IProcessor<Vector2> Clone()
        {
            return (IProcessor<Vector2>)MemberwiseClone();
        }

        private static Vector2 NormalizeToAxis(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
                return new Vector2(Mathf.Sign(v.x), 0f);

            return new Vector2(0f, Mathf.Sign(v.y));
        }
    }
}