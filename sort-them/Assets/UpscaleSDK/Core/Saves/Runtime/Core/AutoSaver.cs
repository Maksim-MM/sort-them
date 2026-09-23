using System;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Represents a auto saver class.
    /// </summary>
    public class AutoSaver
    {
        /// <summary>
        /// Occurs when auto save started.
        /// </summary>
        public event Action OnAutoSaveStarted;

        private float _interval;
        private float _elapsed;
        private bool _running;

        public AutoSaver(bool startImmediately, float secondsBetweenAutosaves = 300f)
        {
            _interval = secondsBetweenAutosaves;
            if (startImmediately)
                Start();
        }

        /// <summary>
        /// Start.
        /// </summary>
        public void Start()
        {
            _running = true;
            _elapsed = 0f;
        }

        /// <summary>
        /// Stop.
        /// </summary>
        public void Stop()
        {
            _running = false;
        }

        /// <summary>
        /// Sets the interval.
        /// </summary>
        /// <param name="seconds">The seconds.</param>
        public void SetInterval(float seconds)
        {
            _interval = seconds;
        }
    
        /// <summary>
        /// Tick.
        /// </summary>
        /// <param name="deltaTime">The delta time.</param>
        public void Tick(float deltaTime)
        {
            if (!_running)
                return;

            _elapsed += deltaTime;

            if (_elapsed >= _interval)
            {
                _elapsed = 0f;
                OnAutoSaveStarted?.Invoke();
            }
        }
    }
}