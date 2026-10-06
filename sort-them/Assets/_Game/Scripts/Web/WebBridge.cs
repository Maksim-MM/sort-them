using Playgama;
using UnityEngine;

namespace SortThem.Web
{
    public class WebBridge : MonoBehaviour
    {
        public static WebBridge Instance { get; private set; }
        bool _readySent, _audioOff, _paused, _pausePending, _adOpen, _gameplayOn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("WebBridge");
            DontDestroyOnLoad(go);
            go.AddComponent<WebBridge>();
        }

        void Awake()
        {
            Instance = this;
            var platform = Bridge.platform;
            Ads.Services = new WebAds();
            Premium.Refresh();
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
            _pausePending = paused;
            ApplyListener();
            TryOpenPause();
        }

        void TryOpenPause()
        {
            if (!_pausePending || Ads.Busy) return;
            var gm = GameManager.I;
            var ui = UiRoot.I;
            if (gm == null || !gm.Ready || ui == null || ui.AnyOpen) return;
            _pausePending = false;
            ui.OpenPause();
        }

        void Update()
        {
            TryOpenPause();
            var gm = GameManager.I;
            bool on = gm != null && gm.Ready && !gm.UiBlocking;
            if (on == _gameplayOn) return;
            _gameplayOn = on;
            Bridge.platform.SendMessage(on ? Playgama.Modules.Platform.PlatformMessage.LevelResumed : Playgama.Modules.Platform.PlatformMessage.LevelPaused);
        }

        public static void SetAdOpen(bool open)
        {
            if (Instance == null) return;
            Instance._adOpen = open;
            Instance.ApplyListener();
        }

        void ApplyListener()
        {
            AudioListener.pause = _audioOff || _paused || _adOpen;
        }

        void SendGameReady()
        {
            if (_readySent) return;
            _readySent = true;
            Bridge.platform.SendMessage(Playgama.Modules.Platform.PlatformMessage.GameReady);
        }
    }
}
