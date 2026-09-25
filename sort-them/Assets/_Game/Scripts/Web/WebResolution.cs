using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SortThem.Web
{
    public class WebResolution : MonoBehaviour
    {
        const int DefaultMaxShortSide = 590;
        const int DefaultFrameRate = 30;
        const float MinScale = 0.3f;

        int _maxShortSide;
        float _baseScale = -1f;
        int _w, _h;
        float _nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!Application.isMobilePlatform) return;
            Application.targetFrameRate = ReadParam("fps", DefaultFrameRate);
            var go = new GameObject("[WebResolution]");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<WebResolution>()._maxShortSide = ReadParam("maxh", DefaultMaxShortSide);
        }

        static int ReadParam(string name, int fallback)
        {
            string url = Application.absoluteURL ?? "";
            int q = url.IndexOf('?');
            if (q < 0) return fallback;
            foreach (var part in url.Substring(q + 1).Split('&'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0] == name && int.TryParse(kv[1], out int v)) return v;
            }
            return fallback;
        }

        void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;
            if (Screen.width == _w && Screen.height == _h) return;
            _w = Screen.width;
            _h = Screen.height;
            var urp = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (urp == null) return;
            if (_baseScale < 0f) _baseScale = urp.renderScale;
            int shortSide = Mathf.Min(_w, _h);
            float scale = _baseScale;
            if (_maxShortSide > 0 && shortSide > 0) scale = Mathf.Clamp(_maxShortSide / (float)shortSide, MinScale, _baseScale);
            urp.renderScale = scale;
            Debug.Log("SortThem: web render scale " + scale.ToString("0.00") + " (screen " + _w + "x" + _h + ", max short side " + _maxShortSide + ", base " + _baseScale.ToString("0.00") + ", asset " + urp.name + ")");
        }
    }
}
