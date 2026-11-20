using System.Collections.Generic;
using System.Runtime.InteropServices;
using Playgama;
using UnityEngine;

namespace SortThem.Web
{
    public class WebBridge : MonoBehaviour
    {
        public static WebBridge Instance { get; private set; }
        bool _readySent, _audioOff, _paused, _pausePending, _adOpen, _adWasOpen, _gameplayOn;
        int _adCheckFrame = -1;

        [DllImport("__Internal")]
        static extern void SortThemRestoreFocus();

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
            Analytics.Sent += SendAnalytics;
            Debug.Log($"SortThem: bridge platform {platform.id}, language {platform.language}, audio {platform.isAudioEnabled}");
        }

        void OnDestroy()
        {
            Platform.GameReady -= SendGameReady;
            Analytics.Sent -= SendAnalytics;
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
            _pausePending = paused && !_adOpen && !Ads.Busy;
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
            if (_adCheckFrame >= 0 && Time.frameCount >= _adCheckFrame)
            {
                _adCheckFrame = -1;
                var ui = UiRoot.I;
                if ((_adWasOpen || _paused) && ui != null && !ui.AnyOpen) _pausePending = true;
                _adWasOpen = false;
            }
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
            if (open) Instance._adWasOpen = true;
            else
            {
                Instance._pausePending = false;
                Instance._adCheckFrame = Time.frameCount + 3;
                SortThemRestoreFocus();
            }
            Instance.ApplyListener();
        }

        void ApplyListener()
        {
            AudioListener.pause = _audioOff || _paused || _adOpen;
        }

        void SendAnalytics(string name, Dictionary<string, object> data)
        {
            if (Bridge.analytics != null) Bridge.analytics.Send(name, data);
        }

        void SendGameReady()
        {
            if (_readySent) return;
            _readySent = true;
            Bridge.platform.SendMessage(Playgama.Modules.Platform.PlatformMessage.GameReady);
        }
    }
}
