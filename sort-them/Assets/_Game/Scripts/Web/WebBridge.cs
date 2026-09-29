using Playgama;
using UnityEngine;

namespace SortThem.Web
{
    public class WebBridge : MonoBehaviour
    {
        bool _readySent, _audioOff, _paused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("WebBridge");
            DontDestroyOnLoad(go);
            go.AddComponent<WebBridge>();
        }

        void Awake()
        {
            var platform = Bridge.platform;
            _audioOff = !platform.isAudioEnabled;
            ApplyListener();
            platform.audioStateChanged += OnAudioState;
            platform.pauseStateChanged += OnPauseState;
            Platform.GameReady += SendGameReady;
            Debug.Log($"SortThem: bridge platform {platform.id}, language {platform.language}, audio {platform.isAudioEnabled}");
        }

        void OnDestroy()
        {
            Platform.GameReady -= SendGameReady;
            var platform = Bridge.platform;
            if (platform == null) return;
            platform.audioStateChanged -= OnAudioState;
            platform.pauseStateChanged -= OnPauseState;
        }

        void OnAudioState(bool enabled)
        {
            _audioOff = !enabled;
            ApplyListener();
        }

        void OnPauseState(bool paused)
        {
            _paused = paused;
            ApplyListener();
            if (!paused) return;
            var gm = GameManager.I;
            var ui = UiRoot.I;
            if (gm == null || !gm.Ready || ui == null || ui.AnyOpen) return;
            ui.OpenPause();
        }

        void ApplyListener()
        {
            AudioListener.pause = _audioOff || _paused;
        }

        void SendGameReady()
        {
            if (_readySent) return;
            _readySent = true;
            Bridge.platform.SendMessage(Playgama.Modules.Platform.PlatformMessage.GameReady);
        }
    }
}
