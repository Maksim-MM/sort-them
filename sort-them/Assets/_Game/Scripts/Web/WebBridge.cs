using Playgama;
using UnityEngine;

namespace SortThem.Web
{
    public class WebBridge : MonoBehaviour
    {
        bool _readySent, _audioOff, _paused, _gameplayOn;

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
            Ads.Services = new WebAds();
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
            if (!paused || Ads.Busy) return;
            var gm = GameManager.I;
            var ui = UiRoot.I;
            if (gm == null || !gm.Ready || ui == null || ui.AnyOpen) return;
            ui.OpenPause();
        }

        void Update()
        {
            var gm = GameManager.I;
            bool on = gm != null && gm.Ready && !gm.UiBlocking;
            if (on == _gameplayOn) return;
            _gameplayOn = on;
            Bridge.platform.SendMessage(on ? Playgama.Modules.Platform.PlatformMessage.LevelResumed : Playgama.Modules.Platform.PlatformMessage.LevelPaused);
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
