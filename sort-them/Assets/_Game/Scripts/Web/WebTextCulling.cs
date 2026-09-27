using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SortThem.Web
{
    public class WebTextCulling : MonoBehaviour
    {
        const int DefaultDistance = 6;
        const float SmallTextHeight = 0.2f;
        const float Interval = 0.2f;

        readonly List<Renderer> _small = new List<Renderer>();
        float _distance2;
        float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!Application.isMobilePlatform) return;
            var go = new GameObject("[WebTextCulling]");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            var c = go.AddComponent<WebTextCulling>();
            int d = WebResolution.ReadParam("tagd", DefaultDistance);
            c._distance2 = d * d;
            SceneManager.sceneLoaded += (s, m) => c.Collect();
            c.Collect();
        }

        void Collect()
        {
            _small.Clear();
            foreach (var t in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var rt = t.rectTransform;
                if (_distance2 > 0f && rt.rect.height * rt.lossyScale.y < SmallTextHeight) _small.Add(r);
            }
            Debug.Log("SortThem: web text culling " + _small.Count + " small texts, distance " + Mathf.Sqrt(_distance2));
        }

        void Update()
        {
            if (_small.Count == 0 || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Interval;
            var cam = Camera.main;
            if (cam == null) return;
            var p = cam.transform.position;
            for (int i = 0; i < _small.Count; i++)
            {
                var r = _small[i];
                if (r == null) continue;
                bool on = (r.transform.position - p).sqrMagnitude < _distance2;
                if (r.enabled != on) r.enabled = on;
            }
        }
    }
}
