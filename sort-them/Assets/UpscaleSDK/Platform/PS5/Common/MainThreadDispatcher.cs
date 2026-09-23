using System;
using System.Collections.Concurrent;
#if UNITY_PS5
#endif
using UnityEngine;

namespace Plugins.UpscaleSDK.PS5.Runtime.Common
{
    /// <summary>
    /// Represents a main thread dispatcher class.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _queue = new();

        /// <summary>
        /// Enqueue.
        /// </summary>
        /// <param name="action">The action.</param>
        public static void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }

        private void Update()
        {
            while (_queue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}