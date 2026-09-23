using System;
using Plugins.UpscaleSDK.PS4.Runtime.App;
using Plugins.UpscaleSDK.PS4.Runtime.DependencyManagement;
using UnityEngine;
#if UNITY_PS4
using Sony.NP;
#endif

namespace Plugins.UpscaleSDK.PS4.Runtime.Common
{
    /// <summary>
    /// Represents a main updater class.
    /// </summary>
    public class MainUpdater : MonoBehaviour
    {
#if UNITY_PS4
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Main.OnAsyncEvent += OnAsyncEventReceived;
        }

        private void OnAsyncEventReceived(NpCallbackEvent npEvent)
        {
            Debug.Log($"MainAsyncEvent: {npEvent}");
        }

        private void Update()
        {
            //var appdata = IDependency<AppData>.Instance;
            //if (appdata != null && appdata.CanUseSocial)
            {
                Main.Update();
            }
        }
#endif
    }
}