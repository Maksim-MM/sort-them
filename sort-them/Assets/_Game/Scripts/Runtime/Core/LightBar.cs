using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace SortThem
{
    public static class LightBar
    {
        enum Effect { None, Flash, Blink, Hold, Rainbow }

        static readonly Color Idle = new Color(0.45f, 0.45f, 0.5f);
        static readonly Color Gold = new Color(1f, 0.72f, 0.1f);
        static readonly Color Purple = new Color(0.55f, 0.1f, 1f);
        static readonly Color Red = new Color(1f, 0.05f, 0.05f);

        static Effect _effect;
        static Color _effectColor;
        static float _start, _until;
        static Color32 _applied = new Color32(1, 2, 3, 0);
        static bool _loggedDevice;

        public static bool Supported
        {
            get
            {
#if UNITY_EDITOR
                return !Platform.IsMobile;
#elif UNITY_PS4 || UNITY_PS5 || UNITY_STANDALONE
                return !Platform.IsMobile;
#else
                return false;
#endif
            }
        }

        public static void ShelfComplete() => Play(Effect.Flash, Gold, 1.1f);
        public static void Purchase() => Play(Effect.Flash, Gold, 0.7f);
        public static void Ability(float seconds) => Play(Effect.Hold, Purple, Mathf.Max(0.5f, seconds));
        public static void SlotSpin(float seconds) => Play(Effect.Rainbow, Color.white, seconds);
        public static void SlotWin() => Play(Effect.Flash, Gold, 1.1f);
        public static void SlotBomb() => Play(Effect.Blink, Red, 0.9f);

        public static void Clear()
        {
            _effect = Effect.None;
        }

        static void Play(Effect effect, Color color, float seconds)
        {
            if (!Supported) return;
            _effect = effect;
            _effectColor = color;
            _start = Time.unscaledTime;
            _until = _start + seconds;
        }

        public static void Tick()
        {
            if (!Supported) return;
            Apply(Current());
        }

        static Color Current()
        {
            if (!Settings.LightBar) return Color.black;
            if (_effect == Effect.None) return Idle;
            float now = Time.unscaledTime;
            if (now >= _until) { _effect = Effect.None; return Idle; }
            float t = (now - _start) / (_until - _start);
            switch (_effect)
            {
                case Effect.Flash: return Color.Lerp(_effectColor, Idle, t * t);
                case Effect.Blink: return Mathf.Repeat(now - _start, 0.3f) < 0.15f ? _effectColor : Color.black;
                case Effect.Hold: return t < 0.85f ? _effectColor : Color.Lerp(_effectColor, Idle, (t - 0.85f) / 0.15f);
                case Effect.Rainbow: return Color.HSVToRGB(Mathf.Repeat((now - _start) * 1.6f, 1f), 1f, 1f);
            }
            return Idle;
        }

        static void Apply(Color color)
        {
            Color32 c = color;
            if (c.r == _applied.r && c.g == _applied.g && c.b == _applied.b) return;
            var pad = Gamepad.current;
            if (pad == null) return;
            if (!_loggedDevice)
            {
                _loggedDevice = true;
                Debug.Log("LightBar: gamepad " + pad.GetType().Name + (pad is DualShockGamepad ? " supports light bar" : " has no light bar"));
            }
            if (!(pad is DualShockGamepad ds)) return;
            try { ds.SetLightBarColor(color); }
            catch { return; }
            _applied = c;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Supported) return;
            var go = new GameObject("LightBarHost");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<LightBarHost>();
        }

        sealed class LightBarHost : MonoBehaviour
        {
            void Update() => Tick();
        }
    }
}
