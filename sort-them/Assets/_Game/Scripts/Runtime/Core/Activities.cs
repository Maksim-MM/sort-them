using System.Collections;
using UnityEngine;

namespace SortThem
{
    public static class Activities
    {
        public const string Story = "story_01";
        const string DoneKey = "activity." + Story + ".done";

        public static bool Done
        {
            get { var prefs = SavesReady.Prefs; return prefs != null && prefs.GetBool(DoneKey, false); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
#if UNITY_PS5 || UNITY_EDITOR
            var go = new GameObject("Activities");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Host>().StartCoroutine(Run());
#endif
        }

        static bool _completed, _started;

        public static void GameCompleted()
        {
            _completed = true;
            if (_started && !Done) Finish();
        }

        static IEnumerator Run()
        {
            yield return new WaitUntil(() => SavesReady.IsReady);
            if (Done) yield break;
            yield return new WaitUntil(UserRegistered);
            if (_completed) { Finish(); yield break; }
            Start();
            _started = true;
        }

        static bool UserRegistered()
        {
#if UNITY_PS5 && !UNITY_EDITOR
            return Plugins.UpscaleSDK.PS5.Runtime.Users.UserInitializer.IsUserRegistered;
#else
            return true;
#endif
        }

        static void Start()
        {
#if UNITY_PS5 && !UNITY_EDITOR
            Plugins.UpscaleSDK.PS5.Runtime.Users.Activities.Ps5Activities.StartActivity(Story);
#endif
#if UNITY_EDITOR
            Debug.Log("Activities: start " + Story);
#endif
        }

        static void Finish()
        {
#if UNITY_PS5 && !UNITY_EDITOR
            Plugins.UpscaleSDK.PS5.Runtime.Users.Activities.Ps5Activities.FinishActivity(Story, Plugins.UpscaleSDK.PS5.Runtime.Users.Activities.ActivityResult.Completed);
#endif
#if UNITY_EDITOR
            Debug.Log("Activities: finish " + Story);
#endif
            var prefs = SavesReady.Prefs;
            if (prefs != null) { prefs.SetBool(DoneKey, true); prefs.TrySave(); }
        }

        sealed class Host : MonoBehaviour { }
    }
}
