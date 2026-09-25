using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SortThem
{
    public class TitleScreen : MonoBehaviour
    {
        public Texture Background;
        public AudioClip[] Music = System.Array.Empty<AudioClip>();
        public AudioClip Click;
        public string NextScene = "Main";

        const string KeyButton = "title.press_button";
        const string KeyKey = "title.press_key";
        const string KeyTouch = "title.press_touch";
        const float FadeTime = 0.45f;

        string _textButton = "Press Any Button";
        string _textKey = "Press Any Key";
        string _textTouch = "Tap to Start";
        TMP_Text _label;
        Image _fade;
        AudioSource _music;
        AsyncOperation _load;
        bool _ready, _started, _sawKbm, _sawTouch;
        float _shownAt;

        IEnumerator Start()
        {
            BuildUi();
            float timeout = Time.realtimeSinceStartup + 10f;
            while (!SavesReady.IsReady && Time.realtimeSinceStartup < timeout) yield return null;
            Settings.EnsureLoaded();
            PlayMusic();
            yield return InitLocalization();
            _load = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode.Single);
            if (_load != null) _load.allowSceneActivation = false;
            _ready = true;
            _shownAt = Time.unscaledTime;
        }

        void Update()
        {
            TrackDevice();
            if (_label != null && !_started)
            {
                _label.text = LabelText();
                if (_ready)
                {
                    float t = Time.unscaledTime - _shownAt;
                    _label.alpha = Mathf.Clamp01(t / 0.6f);
                    float scale = 1f + 0.06f * (0.5f - 0.5f * Mathf.Cos(t * 3.2f));
                    _label.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            if (_ready && !_started && AnyPress(out bool fromPad)) StartCoroutine(Go(fromPad));
            Rumble.Tick();
        }

        IEnumerator Go(bool fromPad)
        {
            _started = true;
            Sfx.PlayUi(Click);
            if (fromPad) Rumble.UiClick();
            float startLabel = _label.alpha;
            float startMusic = _music != null ? _music.volume : 0f;
            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                float k = t / FadeTime;
                _fade.color = new Color(0f, 0f, 0f, k);
                _label.alpha = Mathf.Lerp(startLabel, 0f, k);
                if (_music != null) _music.volume = Mathf.Lerp(startMusic, 0f, k);
                yield return null;
            }
            _fade.color = Color.black;
            Rumble.Stop();
            if (_load == null) { SceneManager.LoadScene(NextScene); yield break; }
            while (_load.progress < 0.9f) yield return null;
            _load.allowSceneActivation = true;
        }

        void TrackDevice()
        {
            ActiveDevice.Poll();
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.anyKey.isPressed || mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.5f) _sawKbm = true;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) _sawTouch = true;
        }

        bool ShowPad() => ActiveDevice.Console || ActiveDevice.Gamepad || Gamepad.current != null && !_sawKbm;

        string LabelText()
        {
            bool touch = !ActiveDevice.Console && (_sawTouch || Platform.IsMobile && !ActiveDevice.Gamepad);
            if (touch) return _textTouch;
            return ShowPad() ? _textButton : _textKey;
        }

        static bool AnyPress(out bool fromPad)
        {
            fromPad = true;
            foreach (var pad in Gamepad.all)
            {
                if (Hit(pad.buttonSouth) || Hit(pad.buttonEast) || Hit(pad.buttonWest) || Hit(pad.buttonNorth)
                    || Hit(pad.leftShoulder) || Hit(pad.rightShoulder) || Hit(pad.leftTrigger) || Hit(pad.rightTrigger)
                    || Hit(pad.leftStickButton) || Hit(pad.rightStickButton) || Hit(pad.selectButton)
                    || Hit(pad.dpad.up) || Hit(pad.dpad.down) || Hit(pad.dpad.left) || Hit(pad.dpad.right))
                    return true;
            }
            fromPad = false;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame && !kb.escapeKey.isPressed) return true;
            var mouse = Mouse.current;
            return mouse != null && (Hit(mouse.leftButton) || Hit(mouse.rightButton) || Hit(mouse.middleButton));
        }

        static bool Hit(ButtonControl b) => b != null && b.wasPressedThisFrame;

        void PlayMusic()
        {
            if (Music == null || Music.Length == 0) return;
            var clip = Music[Random.Range(0, Music.Length)];
            if (clip == null) return;
            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = clip;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.playOnAwake = false;
            _music.volume = Settings.MusicVolume;
            _music.Play();
        }

        IEnumerator InitLocalization()
        {
            if (!LocalizationSettings.HasSettings) yield break;
            var op = LocalizationSettings.InitializationOperation;
            float timeout = Time.realtimeSinceStartup + 10f;
            while (!op.IsDone && Time.realtimeSinceStartup < timeout) yield return null;
            if (!op.IsDone || op.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded) yield break;
            if (!string.IsNullOrEmpty(Settings.Locale))
            {
                var saved = LocalizationSettings.AvailableLocales.GetLocale(Settings.Locale);
                if (saved != null && LocalizationSettings.SelectedLocale != saved) LocalizationSettings.SelectedLocale = saved;
            }
            var table = LocalizationSettings.StringDatabase.GetTableAsync(Loc.Table);
            timeout = Time.realtimeSinceStartup + 5f;
            while (!table.IsDone && Time.realtimeSinceStartup < timeout) yield return null;
            Fonts.Apply(LocalizationSettings.SelectedLocale?.Identifier.Code);
            _textButton = Localized(KeyButton, _textButton);
            _textKey = Localized(KeyKey, _textKey);
            _textTouch = Localized(KeyTouch, _textTouch);
        }

        static string Localized(string key, string fallback)
        {
            try
            {
                var s = LocalizationSettings.StringDatabase.GetLocalizedString(Loc.Table, key);
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch
            {
                return fallback;
            }
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;

            var bgRect = UiFactory.Rect(root, "Background");
            var bg = bgRect.gameObject.AddComponent<RawImage>();
            bg.texture = Background;
            bg.raycastTarget = false;
            var fitter = bgRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = Background != null ? Background.width / (float)Background.height : 16f / 9f;

            _label = UiFactory.Text(root, "Press", _textButton, 44f, TextAlignmentOptions.Center, Color.white);
            _label.fontStyle = FontStyles.Bold;
            _label.alpha = 0f;
            UiFactory.Anchored(_label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1500f, 70f));
            _label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _label.rectTransform.anchoredPosition = new Vector2(0f, 115f);
            UiFactory.TextGlow(_label, new Color(0f, 0f, 0f, 0.9f), 0.4f, 0.5f);

            _fade = UiFactory.Panel(root, "Fade", new Color(0f, 0f, 0f, 0f)).GetComponent<Image>();
            _fade.raycastTarget = false;
            UiFactory.Anchor(_fade.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
    }
}
