using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SortThem
{
    public partial class UiRoot : MonoBehaviour
    {
        public static UiRoot I { get; private set; }

        public PlayerAbilities Abilities;
        public Sprite[] AbilityIcons = new Sprite[3];
        public Sprite SlotFrame, KeyFrame;
        public Sprite[] TouchIcons = new Sprite[8];
        public Sprite Circle;
        public Sprite BombIcon;

        Canvas _canvas;
        TMP_Text _carsText, _shelvesText, _collectiblesText, _balanceText, _inventoryText, _inventoryCountText, _saveText, _toastText;
        CanvasGroup _toast;
        CanvasGroup _tutorial;
        TMP_Text _fpsText;
        int _fpsFrames;
        float _fpsTime;
        TMP_Text _tutorialText;
        GameObject _tutorialTick;
        TutorialStep _tutorialShown = TutorialStep.Done;
        string _tutorialGroup;
        Coroutine _tutorialRoutine;
        UiPulse _pulseStick, _pulseTake, _pulseThrow;
        const float TutorialTickHold = 1f, TutorialFade = 0.3f;
        float _toastShownAt = -10f;
        const float ToastFadeIn = 0.15f, ToastHold = 2f, ToastFadeOut = 0.4f;
        RectTransform _terminal, _pause, _settings, _slot, _controls;
        RectTransform _controlsList;
        readonly List<Selectable> _controlsItems = new List<Selectable>();
        GameObject _controlsButton;
        string _controlsGroup;
        Button _languageButton;
        TMP_Text _languageLabel;
        CanvasGroup _fade;
        TMP_Text _fadeText;
        TMP_Text _terminalBalance, _slotBalance, _slotResult, _slotRemaining, _spinLabel;
        Image _slotIcon;
        Button _spinButton;
        readonly List<Selectable> _slotButtons = new List<Selectable>();
        UpgradeData _spinReward;
        bool _spinning, _spinBomb;
        float _spinUntil = -1f, _spinTickAt;
        const float SpinDuration = 1.55f, SpinTick = 0.07f;
        static readonly Color Gold = new Color(1f, 0.8f, 0.25f, 1f);
        static readonly Color GoldCooldown = new Color(0.6f, 0.48f, 0.15f, 1f);
        const string GoldHex = "#FFCC40";
        readonly List<UpgradeRow> _rows = new List<UpgradeRow>();
        InputAction _pauseAction, _navigateAction, _submitAction, _cancelAction;
        readonly List<(TMP_Text Text, string Key, string Fallback)> _bound = new List<(TMP_Text, string, string)>();
        readonly List<Selectable> _pauseButtons = new List<Selectable>();
        readonly List<Selectable> _settingsItems = new List<Selectable>();
        RectTransform _settingsList;
        Selectable _focused;
        Button _vibrationButton;
        GameObject _vibrationRow;
        TMP_Text _vibrationLabel;
        const float SliderStep = 0.05f;
        float _navRepeatAt;
        const float NavRepeatFirst = 0.35f, NavRepeatNext = 0.18f;
        const float PulseAmount = 0.06f, PulseHz = 1.1f;
        float _saveTextUntil;

        class AbilitySlot
        {
            public GameObject Root;
            public Image Icon, Fill;
            public TMP_Text Key;
        }

        readonly AbilitySlot[] _abilitySlots = new AbilitySlot[3];
        RectTransform _abilitiesPanel;
        GameObject _touchSprint, _touchCrouch, _rotateOverlay;
        RectTransform _safe;
        static readonly string[] AbilityActions = { "Ability1", "Ability2", "Ability3" };
        const float SlotSize = 72f, SlotGap = 12f;
        static readonly Color SlotColor = new Color(0.13f, 0.12f, 0.14f, 0.92f);
        static readonly Color FillActive = new Color(0.55f, 0.35f, 1f, 0.55f);
        static readonly Color FillCooldown = new Color(1f, 1f, 1f, 0.22f);
        static readonly Color IconCooldown = new Color(0.6f, 0.6f, 0.6f, 1f);

        class UpgradeRow
        {
            public UpgradeData Data;
            public TMP_Text Name, Desc, Level, Cost;
            public Button Buy;
            public Image Icon;
            public Image[] Pips;
            public Image Frame;
        }

        static readonly Color ArcadeBody = new Color(0.09f, 0.10f, 0.14f, 0.98f);
        static readonly Color ArcadeNeon = new Color(0.25f, 0.88f, 1f, 1f);
        static readonly Color ArcadeScreen = new Color(0.04f, 0.06f, 0.09f, 1f);
        static readonly Color ArcadeTitle = new Color(0.90f, 1f, 0.45f, 1f);
        static readonly Color ArcadeCost = new Color(1f, 0.83f, 0.30f, 1f);
        static readonly Color ArcadeRed = new Color(0.88f, 0.20f, 0.16f, 1f);
        static readonly Color ArcadeBlue = new Color(0.20f, 0.44f, 0.90f, 1f);
        static readonly Color PipOn = new Color(1f, 0.55f, 0.15f, 1f);
        static readonly Color PipOff = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color ArcadeFocus = new Color(1f, 0.82f, 0.22f, 1f);

        public bool TerminalOpen => _terminal != null && _terminal.gameObject.activeSelf;
        public bool SlotOpen => _slot != null && _slot.gameObject.activeSelf;
        public bool PauseOpen => _pause != null && _pause.gameObject.activeSelf;
        public bool SettingsOpen => _settings != null && _settings.gameObject.activeSelf;
        public bool ControlsOpen => _controls != null && _controls.gameObject.activeSelf;
        public bool Fading => _fade != null && _fade.gameObject.activeSelf;
        public bool AnyOpen => TerminalOpen || SlotOpen || PauseOpen || SettingsOpen || ControlsOpen || UpgradeOpen || ConfirmOpen || Fading;

        void Awake()
        {
            I = this;
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600, 900);
                scaler.matchWidthOrHeight = 1f;
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        void Start()
        {
            var gm = GameManager.I;
            TouchInput.Reset();
            TouchInput.Active = Platform.IsMobile;
            var existingScaler = GetComponent<CanvasScaler>();
            if (existingScaler != null) existingScaler.matchWidthOrHeight = 1f;
            _safe = UiFactory.Rect(transform, "Safe");
            UiFactory.Anchor(_safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _safe.gameObject.AddComponent<SafeAreaFitter>();
            BuildHud();
            if (TouchInput.Active) { BuildTouchControls(); BuildRotateOverlay(); }
            BuildTerminal();
            BuildSlot();
            BuildPause();
            BuildSettings();
            BuildControls();
            BuildUpgrade();
            BuildConfirm();
            BuildFade();
            gm.StatsChanged += RefreshStats;
            gm.Economy.Changed += _ => { RefreshStats(); RefreshTerminal(); RefreshSlot(); };
            gm.Upgrades.Changed += _ => { RefreshTerminal(); RefreshInventory(); RefreshSlot(); };
            gm.Inventory.Changed += RefreshInventory;
            Messages.Shown += ShowToast;
            Loc.Changed += OnLocChanged;
            gm.Save.Saved += reason => { _saveText.text = Loc.Get("ui.saved", "Сохранено"); _saveTextUntil = Time.time + 2f; };
            _pauseAction = gm.InputAsset.FindActionMap("Player", true).FindAction("Pause", true);
            var uiMap = gm.InputAsset.FindActionMap("UI", false);
            if (uiMap != null)
            {
                _navigateAction = uiMap.FindAction("Navigate", false);
                _submitAction = uiMap.FindAction("Submit", false);
                _cancelAction = uiMap.FindAction("Cancel", false);
            }
            var module = FindFirstObjectByType<InputSystemUIInputModule>();
            if (module != null) { module.move = null; module.submit = null; module.cancel = null; }
            RefreshStats();
            RefreshInventory();
            RefreshTerminal();
            RefreshSlot();
        }

        void OnDestroy()
        {
            Messages.Shown -= ShowToast;
            Loc.Changed -= OnLocChanged;
        }

        void OnLocChanged()
        {
            foreach (var b in _bound) if (b.Text != null) b.Text.text = Loc.Get(b.Key, b.Fallback);
            RefreshStats();
            RefreshInventory();
            RefreshTerminal();
            RefreshSlot();
            RefreshVibration();
            RefreshLanguage();
            RefreshUpgrade();
            if (ControlsOpen) RefreshControls();
            if (_tutorialText != null && _tutorialShown != TutorialStep.Done) _tutorialText.text = TutorialText(_tutorialShown, ActiveGroup());
        }

        TMP_Text Bind(TMP_Text text, string key, string fallback)
        {
            _bound.Add((text, key, fallback));
            text.text = Loc.Get(key, fallback);
            return text;
        }

        Button Bind(Button button, string key, string fallback)
        {
            Bind(button.GetComponentInChildren<TMP_Text>(), key, fallback);
            return button;
        }

        void ShowToast(string text)
        {
            if (_toastText == null) return;
            _toastText.text = text;
            _toastShownAt = Time.time;
        }

        void UpdateToast()
        {
            if (_toast == null) return;
            float t = Time.time - _toastShownAt;
            float a = t < ToastFadeIn ? t / ToastFadeIn : t < ToastFadeIn + ToastHold ? 1f : 1f - (t - ToastFadeIn - ToastHold) / ToastFadeOut;
            a = Mathf.Clamp01(a);
            _toast.alpha = a;
            if (_toast.gameObject.activeSelf != a > 0f) _toast.gameObject.SetActive(a > 0f);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UpdateToast();
            gm.UiBlocking = AnyOpen || !gm.Ready;
            if ((_pauseAction != null && _pauseAction.WasPressedThisFrame()) || TouchInput.Consume(TouchButton.Pause))
            {
                if (TerminalOpen) CloseTerminal();
                else if (SlotOpen) CloseSlot();
                else if (UpgradeOpen) CloseUpgrade();
                else if (ConfirmOpen) CloseConfirm();
                else if (SettingsOpen) { CloseSettings(false); }
                else if (ControlsOpen) CloseControls(false);
                else if (PauseOpen) ClosePause();
                else OpenPause();
            }
            if (_saveText != null && _saveText.gameObject.activeSelf != Time.time < _saveTextUntil) _saveText.gameObject.SetActive(Time.time < _saveTextUntil);
            RefreshAbilities();
            UpdateTutorial();
            UpdateFps();
            UpdateSpin();
            UpdateUpgrade();
            if (ControlsOpen && ActiveGroup() != _controlsGroup) RefreshControls();
            RefreshTouch();
            if (_rotateOverlay != null) { bool portrait = Screen.height > Screen.width; if (_rotateOverlay.activeSelf != portrait) _rotateOverlay.SetActive(portrait); }
            UpdateMenuFocus();
        }

        void BuildHud()
        {
            var hud = UiFactory.Rect(_safe, "HUD");
            UiFactory.Anchor(hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var stats = UiFactory.Rect(hud, "Stats");
            UiFactory.Anchored(stats, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(320f, 110f));
            UiFactory.Layout(stats, 2f, new RectOffset(12, 12, 8, 8));
            _carsText = UiFactory.Text(stats, "Cars", "", 24f, TextAlignmentOptions.Left, Color.white);
            _shelvesText = UiFactory.Text(stats, "Shelves", "", 24f, TextAlignmentOptions.Left, Color.white);
            _collectiblesText = UiFactory.Text(stats, "Collectibles", "", 24f, TextAlignmentOptions.Left, Color.white);
            var hudShadow = new Color(0f, 0f, 0f, 0.8f);
            UiFactory.TextGlow(_carsText, hudShadow, 0.35f, 0.45f);
            UiFactory.TextGlow(_shelvesText, hudShadow, 0.35f, 0.45f);
            UiFactory.TextGlow(_collectiblesText, hudShadow, 0.35f, 0.45f);

            _fpsText = UiFactory.Text(hud, "Fps", "", 24f, TextAlignmentOptions.Left, new Color(0.3f, 1f, 0.3f, 1f));
            _fpsText.fontStyle = FontStyles.Bold;
            UiFactory.Anchored(_fpsText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -134f), new Vector2(160f, 30f));

            var balancePanel = UiFactory.Rect(hud, "Balance");
            UiFactory.Anchored(balancePanel, new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(240f, 56f));
            _balanceText = UiFactory.Text(balancePanel, "Text", "$0", 32f, TextAlignmentOptions.Right, Color.white);
            _balanceText.fontStyle = FontStyles.Bold;
            UiFactory.TextGlow(_balanceText, hudShadow, 0.35f, 0.45f);
            UiFactory.Anchor(_balanceText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12f, 0f));

            var crosshair = UiFactory.Panel(hud, "Crosshair", new Color(1f, 1f, 1f, 0.9f));
            UiFactory.Anchored(crosshair, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));

            _inventoryCountText = UiFactory.Text(hud, "InventoryCount", "0/5", 30f, TextAlignmentOptions.BottomRight, Color.white);
            _inventoryCountText.fontStyle = FontStyles.Bold;
            UiFactory.Anchored(_inventoryCountText.rectTransform, new Vector2(1f, 0f), new Vector2(-28f, 20f), new Vector2(300f, 44f));
            _inventoryText = UiFactory.Text(hud, "Inventory", "", 18f, TextAlignmentOptions.BottomRight, Color.white);
            _inventoryText.richText = true;
            _inventoryText.lineSpacing = 8f;
            UiFactory.Anchored(_inventoryText.rectTransform, new Vector2(1f, 0f), new Vector2(-28f, 64f), new Vector2(500f, 500f));

            var abilities = UiFactory.Rect(hud, "Abilities");
            _abilitiesPanel = abilities;
            UiFactory.Anchored(abilities, Vector2.zero, new Vector2(24f, 24f), new Vector2(3f * SlotSize + 2f * SlotGap, SlotSize + 52f));
            for (int i = 0; i < 3; i++) _abilitySlots[i] = BuildAbilitySlot(abilities, i);

            var toast = UiFactory.Panel(hud, "Toast", new Color(0f, 0f, 0f, 0.6f));
            UiFactory.Anchored(toast, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(560f, 46f));
            _toast = toast.gameObject.AddComponent<CanvasGroup>();
            _toast.alpha = 0f;
            _toast.blocksRaycasts = false;
            _toastText = UiFactory.Text(toast, "Text", "", 22f, TextAlignmentOptions.Center, Color.white);
            UiFactory.Anchor(_toastText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            toast.gameObject.SetActive(false);

            _saveText = UiFactory.Text(hud, "Saved", "", 20f, TextAlignmentOptions.Right, new Color(0.7f, 1f, 0.7f, 1f));
            UiFactory.Anchored(_saveText.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -80f), new Vector2(240f, 30f));
            _saveText.gameObject.SetActive(false);

            BuildTutorial(hud);
        }

        void BuildTutorial(Transform hud)
        {
            var panel = UiFactory.Panel(hud, "Tutorial", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Anchored(panel, new Vector2(1f, 1f), new Vector2(-16f, -116f), new Vector2(500f, 52f));
            _tutorial = panel.gameObject.AddComponent<CanvasGroup>();
            _tutorial.alpha = 0f;
            _tutorial.blocksRaycasts = false;
            var box = UiFactory.Image(panel, "Box", SlotFrame, new Color(1f, 1f, 1f, 0.9f), Image.Type.Sliced);
            box.pixelsPerUnitMultiplier = 2.4f * 72f / 30f;
            UiFactory.Anchored(box.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(30f, 30f));
            box.rectTransform.pivot = new Vector2(0f, 0.5f);
            _tutorialTick = new GameObject("Tick", typeof(RectTransform));
            var tick = (RectTransform)_tutorialTick.transform;
            tick.SetParent(box.transform, false);
            UiFactory.Anchor(tick, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var green = new Color(0.45f, 1f, 0.5f, 1f);
            var shortBar = UiFactory.Image(tick, "Short", null, green, Image.Type.Simple);
            UiFactory.Anchored(shortBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-5.5f, -3f), new Vector2(4f, 11f));
            shortBar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var longBar = UiFactory.Image(tick, "Long", null, green, Image.Type.Simple);
            UiFactory.Anchored(longBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(3f, 0.5f), new Vector2(4f, 20f));
            longBar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            _tutorialTick.SetActive(false);
            _tutorialText = UiFactory.Text(panel, "Text", "", 22f, TextAlignmentOptions.Left, Color.white);
            _tutorialText.enableAutoSizing = true;
            _tutorialText.fontSizeMin = 15f;
            _tutorialText.fontSizeMax = 22f;
            UiFactory.Anchor(_tutorialText.rectTransform, Vector2.zero, Vector2.one, new Vector2(54f, 0f), new Vector2(-12f, 0f));
            panel.gameObject.SetActive(false);
        }

        void UpdateFps()
        {
            if (_fpsText == null) return;
            _fpsFrames++;
            _fpsTime += Time.unscaledDeltaTime;
            if (_fpsTime < 0.5f) return;
            _fpsText.text = Mathf.RoundToInt(_fpsFrames / _fpsTime) + " FPS" + (PileOcclusion.Ready && GameManager.I.Config.BuriedCulling ? " · " + PileOcclusion.HiddenCount + " hidden" : "");
            _fpsFrames = 0;
            _fpsTime = 0f;
        }

        void UpdateTutorial()
        {
            if (_tutorial == null) return;
            var t = Tutorial.I;
            var step = t != null && t.Active ? t.Step : TutorialStep.Done;
            string group = ActiveGroup();
            if (step != _tutorialShown)
            {
                bool completed = step > _tutorialShown && _tutorialShown != TutorialStep.Done;
                _tutorialShown = step;
                _tutorialGroup = group;
                if (_tutorialRoutine != null) StopCoroutine(_tutorialRoutine);
                _tutorialRoutine = StartCoroutine(TutorialTransition(step, completed));
            }
            else if (step != TutorialStep.Done && group != _tutorialGroup)
            {
                _tutorialGroup = group;
                _tutorialText.text = TutorialText(step, group);
            }
            if (TouchInput.Active) SetPulses(step);
        }

        void SetPulses(TutorialStep step)
        {
            if (_pulseStick != null && _pulseStick.enabled != (step == TutorialStep.Walk)) _pulseStick.enabled = step == TutorialStep.Walk;
            if (_pulseTake != null && _pulseTake.enabled != (step == TutorialStep.Take)) _pulseTake.enabled = step == TutorialStep.Take;
            if (_pulseThrow != null && _pulseThrow.enabled != (step == TutorialStep.Place)) _pulseThrow.enabled = step == TutorialStep.Place;
        }

        IEnumerator TutorialTransition(TutorialStep step, bool completed)
        {
            var go = _tutorial.gameObject;
            if (go.activeSelf && _tutorial.alpha > 0f)
            {
                if (completed)
                {
                    _tutorialTick.SetActive(true);
                    yield return new WaitForSecondsRealtime(TutorialTickHold);
                }
                for (float a = _tutorial.alpha; a > 0f; a -= Time.unscaledDeltaTime / TutorialFade) { _tutorial.alpha = a; yield return null; }
                _tutorial.alpha = 0f;
            }
            if (step == TutorialStep.Done) { go.SetActive(false); _tutorialRoutine = null; yield break; }
            _tutorialTick.SetActive(false);
            _tutorialText.text = TutorialText(step, _tutorialGroup);
            go.SetActive(true);
            for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / TutorialFade) { _tutorial.alpha = a; yield return null; }
            _tutorial.alpha = 1f;
            _tutorialRoutine = null;
        }

        string TutorialText(TutorialStep step, string group)
        {
            bool touch = TouchInput.Active;
            switch (step)
            {
                case TutorialStep.Walk:
                    return touch ? Loc.Get("tut.walk_touch", "Стик слева — походить")
                        : string.Format(Loc.Get("tut.walk", "{0} — походить"), ControlHints.Label(PlayerAction("Move"), group));
                case TutorialStep.Look:
                    if (touch) return Loc.Get("tut.look_touch", "Правая половина экрана — повертеть камерой");
                    string look = group == ControlHints.KeyboardGroup ? Loc.Get("ctl.mouse_key", "Мышь") : ControlHints.Label(PlayerAction("Look"), group);
                    return string.Format(Loc.Get("tut.look", "{0} — повертеть камерой"), look);
                case TutorialStep.Take:
                    return touch ? Loc.Get("tut.take_touch", "Кнопка «взять» — взять любую машинку")
                        : string.Format(Loc.Get("tut.take", "{0} — взять любую машинку"), ControlHints.Label(PlayerAction("Interact"), group));
                case TutorialStep.Place:
                    return touch ? Loc.Get("tut.place_touch", "Кнопка «поставить» — подсвеченный стеллаж")
                        : string.Format(Loc.Get("tut.place", "{0} — поставить на подсвеченный стеллаж"), ControlHints.Label(PlayerAction("PlaceOrThrow"), group));
            }
            return "";
        }

        AbilitySlot BuildAbilitySlot(Transform parent, int index)
        {
            var root = UiFactory.Rect(parent, "Ability" + (index + 1));
            UiFactory.Anchored(root, Vector2.zero, new Vector2(index * (SlotSize + SlotGap), 0f), new Vector2(SlotSize, SlotSize));
            var frame = UiFactory.Image(root, "Frame", SlotFrame, SlotColor, Image.Type.Sliced);
            frame.pixelsPerUnitMultiplier = 2.4f;
            if (TouchInput.Active)
            {
                frame.raycastTarget = true;
                var press = root.gameObject.AddComponent<TouchPressButton>();
                press.Button = index == 0 ? TouchButton.Ability1 : index == 1 ? TouchButton.Ability2 : TouchButton.Ability3;
                press.Background = frame;
            }
            UiFactory.Anchor(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var icon = UiFactory.Image(root, "Icon", index < AbilityIcons.Length ? AbilityIcons[index] : null, Color.white, Image.Type.Simple);
            icon.preserveAspect = true;
            UiFactory.Anchor(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            var fill = UiFactory.Image(root, "Fill", SlotFrame, FillCooldown, Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.fillAmount = 0f;
            UiFactory.Anchor(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var key = UiFactory.Image(root, "Key", KeyFrame, Color.white, Image.Type.Sliced);
            key.pixelsPerUnitMultiplier = 3f;
            if (TouchInput.Active) key.gameObject.SetActive(false);
            UiFactory.Anchored(key.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 38f), new Vector2(30f, 30f));
            var keyText = UiFactory.Text(key.transform, "Text", "", 18f, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.14f, 1f));
            keyText.fontStyle = FontStyles.Bold;
            UiFactory.Anchor(keyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);
            return new AbilitySlot { Root = root.gameObject, Icon = icon, Fill = fill, Key = keyText };
        }

        const float TouchBig = 120f, TouchMid = 100f, TouchSmall = 96f;

        void BuildTouchControls()
        {
            var layer = UiFactory.Rect(_safe, "Touch");
            UiFactory.Anchor(layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            layer.SetSiblingIndex(0);

            var look = UiFactory.Image(layer, "LookArea", null, new Color(0f, 0f, 0f, 0f), Image.Type.Simple);
            look.raycastTarget = true;
            UiFactory.Anchor(look.rectTransform, new Vector2(0.5f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            look.gameObject.AddComponent<TouchLookArea>();

            var stickBase = UiFactory.Image(layer, "Stick", Circle, new Color(0.13f, 0.12f, 0.14f, 0.85f), Image.Type.Simple);
            stickBase.raycastTarget = true;
            UiFactory.Anchored(stickBase.rectTransform, Vector2.zero, new Vector2(180f, 180f), new Vector2(240f, 240f));
            stickBase.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var knob = UiFactory.Image(stickBase.transform, "Knob", Circle, new Color(1f, 1f, 1f, 0.85f), Image.Type.Simple);
            UiFactory.Anchored(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var stick = stickBase.gameObject.AddComponent<TouchStick>();
            stick.Knob = knob.rectTransform;
            stick.Radius = 80f;
            _pulseStick = stickBase.gameObject.AddComponent<UiPulse>();
            _pulseStick.enabled = false;

            const float margin = 60f, gap = 24f;
            float rowBottom = margin + TouchBig * 0.5f;
            float rowTop = rowBottom + TouchBig + gap;
            float colRight = -(margin + TouchBig * 0.5f);
            float colMiddle = colRight - TouchBig * 0.5f - gap - TouchMid * 0.5f;
            float colLeft = colMiddle - TouchMid * 0.5f - gap - TouchSmall * 0.5f;
            _pulseTake = TouchButtonUi(layer, "Take", TouchButton.Interact, TouchIcon(0), new Vector2(1f, 0f), Centered(colRight, rowBottom, TouchBig), TouchBig).AddComponent<UiPulse>();
            _pulseTake.enabled = false;
            _pulseThrow = TouchButtonUi(layer, "Throw", TouchButton.Place, TouchIcon(1), new Vector2(1f, 0f), Centered(colRight, rowTop, TouchBig), TouchBig).AddComponent<UiPulse>();
            _pulseThrow.enabled = false;
            TouchButtonUi(layer, "Jump", TouchButton.Jump, TouchIcon(2), new Vector2(1f, 0f), Centered(colMiddle, rowTop, TouchMid), TouchMid);
            _touchSprint = TouchButtonUi(layer, "Sprint", TouchButton.Jump, TouchIcon(6), new Vector2(1f, 0f), Centered(colMiddle, rowBottom, TouchSmall), TouchSmall, true);
            _touchCrouch = TouchButtonUi(layer, "Crouch", TouchButton.Crouch, TouchIcon(7), new Vector2(1f, 0f), Centered(colLeft, rowBottom, TouchSmall), TouchSmall);
            TouchButtonUi(layer, "Pause", TouchButton.Pause, TouchIcon(3), new Vector2(1f, 1f), new Vector2(-280f, -16f), 60f);

            _inventoryText.gameObject.SetActive(false);
            UiFactory.Anchored(_inventoryCountText.rectTransform, new Vector2(1f, 0f), new Vector2(-28f, 340f), new Vector2(266f, 32f));
            _inventoryCountText.fontSize = 24f;
            BuildInventoryWheel(layer);
            UiFactory.Anchored(_abilitiesPanel, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(3f * SlotSize + 2f * SlotGap, SlotSize));
        }

        void BuildInventoryWheel(Transform parent)
        {
            const float rowH = 28f, wheelW = 266f;
            var wheel = UiFactory.Image(parent, "InventoryWheel", null, new Color(0f, 0f, 0f, 0.001f), Image.Type.Simple);
            wheel.raycastTarget = true;
            UiFactory.Anchored(wheel.rectTransform, new Vector2(1f, 0f), new Vector2(-28f, 380f), new Vector2(wheelW, rowH * 5f));
            wheel.gameObject.AddComponent<RectMask2D>();
            var plate = UiFactory.Image(wheel.transform, "Plate", SlotFrame, new Color(0.13f, 0.12f, 0.14f, 0.7f), Image.Type.Sliced);
            plate.pixelsPerUnitMultiplier = 2.4f;
            UiFactory.Anchored(plate.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(wheelW, rowH));
            var comp = wheel.gameObject.AddComponent<InventoryWheel>();
            comp.Counter = _inventoryCountText;
            comp.RowHeight = rowH;
            for (int k = -2; k <= 2; k++)
            {
                var row = UiFactory.Text(wheel.transform, "Row" + (k + 2), "", 13f, TextAlignmentOptions.Right, Color.white);
                row.textWrappingMode = TextWrappingModes.NoWrap;
                row.overflowMode = TextOverflowModes.Ellipsis;
                UiFactory.Anchored(row.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -k * rowH), new Vector2(wheelW - 16f, rowH));
                comp.Rows[k + 2] = row;
            }
            comp.Bind(GameManager.I.Inventory);
        }

        void BuildRotateOverlay()
        {
            var panel = UiFactory.Panel(transform, "RotateOverlay", new Color(0.08f, 0.09f, 0.12f, 0.97f));
            UiFactory.Anchor(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = UiFactory.Text(panel, "Text", "", 34f, TextAlignmentOptions.Center, Color.white);
            text.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Anchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 40f), new Vector2(-40f, -40f));
            Bind(text, "msg.rotate_device", "Поверните телефон горизонтально");
            _rotateOverlay = panel.gameObject;
            panel.gameObject.SetActive(false);
        }

        static Vector2 Centered(float cx, float cy, float size) => new Vector2(cx + size * 0.5f, cy - size * 0.5f);

        Sprite TouchIcon(int i) => TouchIcons != null && i < TouchIcons.Length ? TouchIcons[i] : null;

        GameObject TouchButtonUi(Transform parent, string name, TouchButton button, Sprite icon, Vector2 anchor, Vector2 pos, float size, bool sprintToggle = false)
        {
            var bg = UiFactory.Image(parent, name, SlotFrame, new Color(0.13f, 0.12f, 0.14f, 0.9f), Image.Type.Sliced);
            bg.pixelsPerUnitMultiplier = 2.4f * 72f / size;
            bg.raycastTarget = true;
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = UiFactory.Image(rt, "Icon", icon, Color.white, Image.Type.Simple);
            img.preserveAspect = true;
            float pad = size * 0.16f;
            UiFactory.Anchor(img.rectTransform, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));
            var press = bg.gameObject.AddComponent<TouchPressButton>();
            press.Button = button;
            press.SprintToggle = sprintToggle;
            press.Background = bg;
            return bg.gameObject;
        }

        void RefreshTouch()
        {
            if (!TouchInput.Active) return;
            var gm = GameManager.I;
            if (_touchSprint != null) { bool on = gm.Upgrades.Has(UpgradeKind.Sprint); if (_touchSprint.activeSelf != on) _touchSprint.SetActive(on); }
            if (_touchCrouch != null) { bool on = gm.Upgrades.Has(UpgradeKind.Crouch); if (_touchCrouch.activeSelf != on) _touchCrouch.SetActive(on); }
        }

        TMP_Text _terminalScore;
        Button _terminalClose;

        void BuildTerminal()
        {
            var w = BuildArcadeWindow("Terminal", new Vector2(1080f, 880f), "ui.terminal", "Терминал улучшений", true, true);
            _terminal = w.Root;
            _terminalScore = BuildBalanceBox(_terminal);

            var list = UiFactory.Rect(w.Screen, "List");
            UiFactory.Anchor(list, Vector2.zero, Vector2.one, new Vector2(12f, 14f), new Vector2(-12f, -14f));
            UiFactory.Layout(list, 3f, new RectOffset(0, 0, 0, 0));

            var gm = GameManager.I;
            foreach (var data in gm.Upgrades.All)
            {
                if (data.Source != UpgradeSource.Terminal) continue;
                var rowFill = UiFactory.NeonBox(list, "Row_" + data.UpgradeID, ArcadeNeon, new Color(0.06f, 0.10f, 0.14f, 1f), 2f);
                var rowRoot = (RectTransform)rowFill.parent;
                UiFactory.Size(rowRoot, 0f, 50f);
                var r = new UpgradeRow { Data = data, Frame = rowRoot.GetComponent<Image>() };

                var icon = UiFactory.Image(rowFill, "Icon", data.Icon, Color.white, Image.Type.Simple);
                icon.preserveAspect = true;
                icon.enabled = data.Icon != null;
                r.Icon = icon;
                UiFactory.Anchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(36f, 36f));

                r.Name = UiFactory.Text(rowFill, "Name", "", 19f, TextAlignmentOptions.Left, Color.white);
                r.Name.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                UiFactory.Anchor(r.Name.rectTransform, new Vector2(0f, 0.42f), new Vector2(0.55f, 1f), new Vector2(56f, 0f), new Vector2(0f, -4f));

                var pips = UiFactory.Rect(rowFill, "Pips");
                UiFactory.Anchor(pips, new Vector2(0f, 0f), new Vector2(0.55f, 0.42f), new Vector2(56f, 8f), new Vector2(0f, -2f));
                UiFactory.Layout(pips, 3f, new RectOffset(0, 0, 0, 0), false);
                int steps = Mathf.Clamp(data.MaxLevel, 1, 12);
                r.Pips = new Image[steps];
                for (int i = 0; i < steps; i++)
                {
                    var pip = UiFactory.Image(pips, "Pip" + i, null, PipOff, Image.Type.Simple);
                    UiFactory.Size(pip, 10f, 8f);
                    r.Pips[i] = pip;
                }

                r.Desc = UiFactory.Text(rowFill, "Desc", "", 1f, TextAlignmentOptions.Left, new Color(0f, 0f, 0f, 0f));
                r.Desc.gameObject.SetActive(false);

                r.Level = UiFactory.Text(rowFill, "Level", "", 20f, TextAlignmentOptions.Right, Color.white);
                UiFactory.Anchor(r.Level.rectTransform, new Vector2(0.55f, 0f), new Vector2(0.69f, 1f), Vector2.zero, Vector2.zero);

                r.Cost = UiFactory.Text(rowFill, "Cost", "", 22f, TextAlignmentOptions.Right, ArcadeCost);
                r.Cost.fontStyle = FontStyles.Bold;
                UiFactory.Anchor(r.Cost.rectTransform, new Vector2(0.69f, 0f), new Vector2(0.845f, 1f), Vector2.zero, new Vector2(-10f, 0f));

                var captured = data;
                r.Buy = Bind(UiFactory.NeonButton(rowFill, "Buy", "", () => { if (gm.Upgrades.TryBuy(captured)) { Sfx.PlayUi(gm.Config.PurchaseClip); gm.Save.SaveNow("purchase"); } }, ArcadeNeon, new Color(0.06f, 0.12f, 0.16f, 1f), ArcadeNeon, 17f), "ui.buy", "Купить");
                UiFactory.Anchor(r.Buy.GetComponent<RectTransform>(), new Vector2(0.85f, 0.06f), new Vector2(0.995f, 0.94f), Vector2.zero, Vector2.zero);
                _rows.Add(r);
            }

            FinishArcadeScreen(w.Screen);
            _terminalClose = Bind(ArcadePanelButton(w, "CloseBig", CloseTerminal, ArcadeNeon, 150f, true), "ui.close", "Закрыть");
            _terminal.gameObject.SetActive(false);
        }

        Button _slotClose;

        void BuildSlot()
        {
            var w = BuildArcadeWindow("SlotMachine", new Vector2(1060f, 780f), "ui.slot", "Слот-машина", true, true);
            _slot = w.Root;
            _slotBalance = BuildBalanceBox(_slot);
            var screen = w.Screen;

            var prizes = UiFactory.NeonBox(screen, "PrizeBox", ArcadeNeon, new Color(0.05f, 0.09f, 0.13f, 1f), 2f);
            UiFactory.Anchor(prizes.parent as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(14f, 14f), new Vector2(258f, -14f));
            BuildPrizeList(prizes);

            var reels = UiFactory.Rect(screen, "Reels");
            UiFactory.Anchor(reels, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(268f, 74f), new Vector2(-14f, -14f));
            BuildReels(reels);

            var payline = UiFactory.Panel(reels, "Payline", new Color(1f, 0.85f, 0.25f, 0.7f));
            UiFactory.Anchor(payline, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, -3.5f), new Vector2(6f, 3.5f));
            payline.SetAsLastSibling();

            _slotIcon = UiFactory.Image(screen, "HiddenIcon", null, Gold, Image.Type.Simple);
            _slotIcon.enabled = false;
            UiFactory.Anchored(_slotIcon.rectTransform, new Vector2(0f, 0f), Vector2.zero, Vector2.zero);

            _slotResult = UiFactory.Text(screen, "Result", "", 22f, TextAlignmentOptions.Center, Color.white);
            UiFactory.Anchor(_slotResult.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(268f, 38f), new Vector2(-14f, 70f));

            _slotRemaining = UiFactory.Text(screen, "Remaining", "", 16f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Anchor(_slotRemaining.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(268f, 10f), new Vector2(-14f, 36f));
            FinishArcadeScreen(screen);

            _spinButton = ArcadePanelButton(w, "Spin", Spin, ArcadeCost, 320f, false, 24f, true);
            _spinLabel = _spinButton.GetComponentInChildren<TMP_Text>();
            _slotClose = Bind(ArcadePanelButton(w, "Close", CloseSlot, ArcadeNeon, 150f, true), "ui.close", "Закрыть");

            _slotButtons.Add(_spinButton);
            _slotButtons.Add(_slotClose);
            _slot.gameObject.SetActive(false);
        }

        void Spin()
        {
            var gm = GameManager.I;
            if (gm == null || _spinning) return;
            int cost = gm.Config.SlotSpinCost;
            UpgradeData reward = null;
            bool bombsEnabled = gm.Config.BombPrefab != null;
            bool bomb = bombsEnabled && (gm.Upgrades.SlotRemaining == 0 || Random.value < gm.Config.BombChance);
            if (bomb)
            {
                if (!gm.Economy.TrySpend(cost)) return;
            }
            else
            {
                reward = gm.Upgrades.TrySpin(cost);
                if (reward == null) return;
            }
            _spinReward = reward;
            _spinBomb = bomb;
            _spinning = true;
            _spinUntil = Time.unscaledTime + SpinDuration;
            _spinTickAt = 0f;
            StartReels(reward != null ? reward.Icon : BombIcon);
            Sfx.PlayUi(gm.Config.SlotLeverClip);
            Sfx.PlayUi(gm.Config.SlotSpinClip != null ? gm.Config.SlotSpinClip : gm.Config.SlotReelClip);
            RefreshSlot();
        }

        void UpdateSpin()
        {
            if (!_spinning) return;
            var gm = GameManager.I;
            UpdateReels();
            if (ReelsRunning())
            {
                _slotResult.text = "";
                return;
            }
            _spinning = false;
            var reward = _spinReward;
            _spinReward = null;
            string text = reward != null ? RewardText(reward) : Loc.Get("ui.slot_bomb", "Бомба!");
            var icon = reward != null ? reward.Icon : BombIcon;
            _slotResult.text = "<color=" + GoldHex + ">" + text + "</color>";
            Sfx.PlayUi(gm.Config.SlotWinClip);
            Rumble.ShelfComplete();
            if (reward != null) Messages.Show(string.Format(Loc.Get("msg.slot_reward", "Выпало: {0}"), RewardText(reward)));
            if (reward != null && reward.Kind == UpgradeKind.PartsCrate) gm.AddCrates(1);
            if (_spinBomb) gm.SpawnBomb();
            RefreshSlot();
        }

        string RewardText(UpgradeData reward)
        {
            var gm = GameManager.I;
            return Loc.Get(reward.DisplayName, reward.DevName) + " " + gm.Upgrades.Level(reward) + "/" + reward.MaxLevel;
        }

        void RefreshSlot()
        {
            var gm = GameManager.I;
            if (gm == null || _slotBalance == null) return;
            _slotBalance.text = FormatMoney(gm.Economy.Balance);
            int left = gm.Upgrades.SlotRemaining;
            bool bombs = gm.Config.BombPrefab != null;
            _slotRemaining.text = left > 0
                ? string.Format(Loc.Get("ui.slot_remaining", "Осталось наград: {0}"), left)
                : bombs ? Loc.Get("ui.slot_empty_bombs", "Награды закончились. Каждый спин даёт бомбу") : Loc.Get("ui.slot_empty", "Пусто. Все награды выданы");
            if (!_spinning && _spinUntil < 0f)
                _slotResult.text = left > 0 ? Loc.Get("ui.slot_idle", "Каждое вращение даёт награду") : "";
            _spinLabel.text = string.Format(Loc.Get("ui.spin", "Крутить · ${0}"), gm.Config.SlotSpinCost);
            _spinButton.interactable = !_spinning && gm.Economy.CanAfford(gm.Config.SlotSpinCost) && (left > 0 || bombs);
            RefreshPrizes();
        }

        string SlotBonusText(UpgradeKind kind)
        {
            var up = GameManager.I.Upgrades;
            switch (kind)
            {
                case UpgradeKind.Inventory:
                    return up.Has(UpgradeKind.InventoryOverCap) ? "+" + up.Level(UpgradeKind.InventoryOverCap) : null;
                case UpgradeKind.DuplicateHighlight:
                case UpgradeKind.ShelfHighlight:
                case UpgradeKind.AutoCollect:
                {
                    var parts = new List<string>();
                    if (up.Has(UpgradeKind.AbilityCooldown))
                        parts.Add(string.Format(Loc.Get("ui.slot_bonus_cd", "откат ×{0}"), up.Value(UpgradeKind.AbilityCooldown, 1f).ToString("0.0#")));
                    if (kind == UpgradeKind.AutoCollect && up.Has(UpgradeKind.AutoCollectRadius))
                        parts.Add(string.Format(Loc.Get("ui.slot_bonus_radius", "радиус ×{0}"), up.Value(UpgradeKind.AutoCollectRadius, 1f).ToString("0.0#")));
                    if (kind != UpgradeKind.AutoCollect && up.Has(UpgradeKind.AbilityDuration))
                        parts.Add(string.Format(Loc.Get("ui.slot_bonus_duration", "действие ×{0}"), up.Value(UpgradeKind.AbilityDuration, 1f).ToString("0.0#")));
                    return parts.Count > 0 ? string.Join(" · ", parts) : null;
                }
            }
            return null;
        }

        void BuildPause()
        {
            const float buttonH = 56f, spacing = 14f, pad = 24f;
            int count = TouchInput.Active ? 5 : 6;
            float screenH = pad * 2f + count * buttonH + (count - 1) * spacing;
            var w = BuildArcadeWindow("Pause", new Vector2(520f, ArcadeHeight(screenH, false, false)), "ui.pause", "Пауза", false, false);
            _pause = w.Root;
            var list = UiFactory.Rect(w.Screen, "List");
            UiFactory.Anchor(list, Vector2.zero, Vector2.one, new Vector2(28f, pad), new Vector2(-28f, -pad));
            UiFactory.Layout(list, spacing, new RectOffset(0, 0, 0, 0));
            _pauseButtons.Add(Bind(ArcadeButton(list, "Resume", ClosePause, ArcadeNeon, 20f), "ui.resume", "Продолжить"));
            _pauseButtons.Add(Bind(ArcadeButton(list, "Save", () => GameManager.I.Save.SaveNow("manual"), ArcadeNeon, 20f), "ui.save", "Сохранить"));
            _pauseButtons.Add(Bind(ArcadeButton(list, "Settings", OpenSettings, ArcadeNeon, 20f), "ui.settings", "Настройки"));
            var controlsButton = Bind(ArcadeButton(list, "Controls", OpenControls, ArcadeNeon, 20f), "ui.controls", "Управление");
            _controlsButton = controlsButton.gameObject;
            _controlsButton.SetActive(!TouchInput.Active);
            _pauseButtons.Add(controlsButton);
            _pauseButtons.Add(Bind(ArcadeButton(list, "Shuffle", StartShuffle, ArcadeNeon, 20f), "ui.shuffle", "Перемешать кучу"));
            _pauseButtons.Add(Bind(ArcadeButton(list, "NewGame", OpenConfirm, ArcadeRed, 20f), "ui.newgame", "Сбросить прогресс"));
            foreach (var b in _pauseButtons) UiFactory.Size(b, 0f, buttonH);
            FinishArcadeScreen(w.Screen);
            _pause.gameObject.SetActive(false);
        }

        void BuildFade()
        {
            var panel = UiFactory.Panel(transform, "Fade", Color.black);
            UiFactory.Anchor(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _fade = panel.gameObject.AddComponent<CanvasGroup>();
            _fade.blocksRaycasts = true;
            _fadeText = UiFactory.Text(panel, "Text", "", 30f, TextAlignmentOptions.Center, Color.white);
            UiFactory.Anchor(_fadeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 40f), new Vector2(-40f, -40f));
            panel.gameObject.SetActive(false);
        }

        void StartShuffle()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Shuffling || Fading) return;
            ClosePause();
            StartCoroutine(ShuffleRoutine());
        }

        IEnumerator ShuffleRoutine()
        {
            var gm = GameManager.I;
            _fade.gameObject.SetActive(true);
            _fade.alpha = 0f;
            _fadeText.text = "";
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { _fade.alpha = t / 0.3f; yield return null; }
            _fade.alpha = 1f;
            string label = Loc.Get("ui.shuffling", "Перемешиваем кучу…");
            yield return gm.ShuffleLoose(p => _fadeText.text = label + " " + Mathf.RoundToInt(p * 100f) + "%");
            _fadeText.text = "";
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { _fade.alpha = 1f - t / 0.4f; yield return null; }
            _fade.gameObject.SetActive(false);
        }

        void BuildSettings()
        {
            const float rowH = 56f, gap = 8f, pad = 20f;
            int rows = Platform.IsMobile ? 5 : 6;
            float screenH = pad * 2f + rows * rowH + (rows - 1) * gap;
            var w = BuildArcadeWindow("Settings", new Vector2(900f, ArcadeHeight(screenH, false, true)), "ui.settings", "Настройки", false, true);
            _settings = w.Root;
            _settingsList = UiFactory.Rect(w.Screen, "List");
            UiFactory.Anchor(_settingsList, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));
            UiFactory.Layout(_settingsList, gap, new RectOffset(0, 0, 0, 0));

            _settingsItems.Add(SettingsSlider("Music", "ui.music", "Музыка", 0f, 1f, Settings.MusicVolume, Settings.SetMusicVolume, Percent));
            _settingsItems.Add(SettingsSlider("Sfx", "ui.sfx", "Эффекты", 0f, 1f, Settings.SfxVolume, Settings.SetSfxVolume, Percent));
            _settingsItems.Add(SettingsSlider("SensX", "ui.sens_x", "Чувствительность по горизонтали", Settings.SensitivityMin, Settings.SensitivityMax, Settings.SensitivityX, Settings.SetSensitivityX, Multiplier));
            _settingsItems.Add(SettingsSlider("SensY", "ui.sens_y", "Чувствительность по вертикали", Settings.SensitivityMin, Settings.SensitivityMax, Settings.SensitivityY, Settings.SetSensitivityY, Multiplier));

            var langRow = ArcadeRow(_settingsList, "Language", "ui.language", "Язык", rowH);
            _languageButton = ArcadeButton(langRow, "Cycle", () => CycleLanguage(1), ArcadeNeon, 18f);
            _languageLabel = _languageButton.GetComponentInChildren<TMP_Text>();
            UiFactory.Anchor(_languageButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, new Vector2(-10f, 0f));
            _settingsItems.Add(_languageButton);
            RefreshLanguage();

            var vibRow = ArcadeRow(_settingsList, "Vibration", "ui.vibration", "Вибрация", rowH);
            _vibrationRow = vibRow.parent.gameObject;
            _vibrationButton = ArcadeButton(vibRow, "Toggle", () => { Settings.SetVibration(!Settings.Vibration); RefreshVibration(); }, ArcadeNeon, 18f);
            _vibrationLabel = _vibrationButton.GetComponentInChildren<TMP_Text>();
            UiFactory.Anchor(_vibrationButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, new Vector2(-10f, 0f));
            _settingsItems.Add(_vibrationButton);
            RefreshVibration();

            FinishArcadeScreen(w.Screen);
            var back = Bind(ArcadePanelButton(w, "Back", () => CloseSettings(true), ArcadeNeon, 150f, true), "ui.back", "Назад");
            _settingsItems.Add(back);
            _settings.gameObject.SetActive(false);
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
        static string Multiplier(float v) => "×" + v.ToString("0.0");

        Slider SettingsSlider(string name, string labelKey, string labelFallback, float min, float max, float value, System.Action<float> apply, System.Func<float, string> format)
        {
            var row = ArcadeRow(_settingsList, name, labelKey, labelFallback, 56f);
            var slider = UiFactory.Slider(row, "Slider", min, max, value);
            UiFactory.Anchor(slider.GetComponent<RectTransform>(), new Vector2(0.54f, 0f), new Vector2(0.85f, 1f), Vector2.zero, Vector2.zero);
            var valueText = UiFactory.Text(row, "Value", format(value), 20f, TextAlignmentOptions.Right, ArcadeCost);
            valueText.fontStyle = FontStyles.Bold;
            UiFactory.Anchor(valueText.rectTransform, new Vector2(0.87f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-14f, 0f));
            slider.onValueChanged.AddListener(v => { apply(v); valueText.text = format(v); });
            _arcadeFrames[slider] = row.parent.GetComponent<Image>();
            return slider;
        }

        void CycleLanguage(int dir)
        {
            var locales = LocalizationSettings.AvailableLocales != null ? LocalizationSettings.AvailableLocales.Locales : null;
            if (locales == null || locales.Count == 0) return;
            int n = locales.Count;
            int i = locales.IndexOf(LocalizationSettings.SelectedLocale);
            i = ((i + dir) % n + n) % n;
            var locale = locales[i];
            LocalizationSettings.SelectedLocale = locale;
            Settings.SetLocale(locale.Identifier.Code);
            RefreshLanguage();
        }

        static string LocaleName(Locale locale)
        {
            if (locale == null) return "";
            var ci = locale.Identifier.CultureInfo;
            string name = ci != null ? ci.NativeName : locale.LocaleName;
            if (string.IsNullOrEmpty(name)) return locale.Identifier.Code;
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        void RefreshLanguage()
        {
            if (_languageLabel == null) return;
            _languageLabel.text = Loc.Ready ? LocaleName(LocalizationSettings.SelectedLocale) : Loc.Get("ui.language", "Язык");
        }

        static readonly (string Action, string Key, string Fallback)[] ControlRows =
        {
            ("Move", "ctl.move", "Передвижение"),
            ("Look", "ctl.look", "Обзор"),
            ("Jump", "ctl.jump", "Прыжок"),
            ("Sprint", "ctl.sprint", "Бег"),
            ("Crouch", "ctl.crouch", "Присед"),
            ("Interact", "ctl.interact", "Взять"),
            ("PlaceOrThrow", "ctl.place", "Поставить на полку или бросить"),
            (null, "ctl.select", "Выбор предмета в руках"),
            ("Ability1", "ctl.ability1", "Поиск совпадений"),
            ("Ability2", "ctl.ability2", "Автосбор совпадений"),
            ("Ability3", "ctl.ability3", "Подсветка стеллажа"),
            ("Pause", "ctl.pause", "Пауза"),
        };

        static string ActiveGroup() => GamepadActive() ? ControlHints.GamepadGroup : ControlHints.KeyboardGroup;

        static InputAction PlayerAction(string name)
        {
            var gm = GameManager.I;
            if (gm == null || gm.InputAsset == null) return null;
            var map = gm.InputAsset.FindActionMap("Player", false);
            return map != null ? map.FindAction(name, false) : null;
        }

        string SelectLabel(string group)
        {
            return group == ControlHints.GamepadGroup
                ? ControlHints.Short(PlayerAction("PrevItem"), group) + "/" + ControlHints.Short(PlayerAction("NextItem"), group)
                : Loc.Get("ctl.wheel_key", "Колесо мыши");
        }

        void BuildControls()
        {
            const float rowH = 36f, gap = 2f, pad = 20f;
            float screenH = pad * 2f + ControlRows.Length * rowH + (ControlRows.Length - 1) * gap;
            var w = BuildArcadeWindow("Controls", new Vector2(900f, ArcadeHeight(screenH, false, true)), "ui.controls", "Управление", false, true);
            _controls = w.Root;
            _controlsList = UiFactory.Rect(w.Screen, "List");
            UiFactory.Anchor(_controlsList, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));
            UiFactory.Layout(_controlsList, gap, new RectOffset(0, 0, 0, 0));
            FinishArcadeScreen(w.Screen);
            var back = Bind(ArcadePanelButton(w, "Back", () => CloseControls(true), ArcadeNeon, 150f, true), "ui.back", "Назад");
            _controlsItems.Add(back);
            _controls.gameObject.SetActive(false);
        }

        void RefreshControls()
        {
            if (_controlsList == null) return;
            var gm = GameManager.I;
            if (gm == null || gm.InputAsset == null) return;
            _controlsGroup = ActiveGroup();
            for (int i = _controlsList.childCount - 1; i >= 0; i--) Destroy(_controlsList.GetChild(i).gameObject);
            var map = gm.InputAsset.FindActionMap("Player", false);
            foreach (var row in ControlRows)
            {
                string key;
                if (row.Action == "Look" && _controlsGroup == ControlHints.KeyboardGroup) key = Loc.Get("ctl.mouse_key", "Мышь");
                else if (row.Action != null) key = ControlHints.Label(map != null ? map.FindAction(row.Action, false) : null, _controlsGroup);
                else key = SelectLabel(_controlsGroup);
                var r = UiFactory.Rect(_controlsList, "Row_" + row.Key);
                UiFactory.Size(r, 0f, 36f);
                var cap = UiFactory.NeonBox(r, "Key", ArcadeCost, new Color(0.12f, 0.09f, 0.03f, 1f), 2f);
                UiFactory.Anchor((RectTransform)cap.parent, new Vector2(0f, 0.08f), new Vector2(0.42f, 0.92f), Vector2.zero, Vector2.zero);
                var keyText = UiFactory.Text(cap, "Text", key, 18f, TextAlignmentOptions.Center, ArcadeCost);
                keyText.fontStyle = FontStyles.Bold;
                UiFactory.Anchor(keyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
                var desc = UiFactory.Text(r, "Desc", Loc.Get(row.Key, row.Fallback), 20f, TextAlignmentOptions.Left, Color.white);
                UiFactory.Anchor(desc.rectTransform, new Vector2(0.42f, 0f), Vector2.one, new Vector2(24f, 0f), Vector2.zero);
            }
        }

        void OpenControls()
        {
            _pause.gameObject.SetActive(false);
            RefreshControls();
            _controls.gameObject.SetActive(true);
            _focused = FirstCandidate(_controlsItems);
        }

        void CloseControls(bool backToPause)
        {
            _controls.gameObject.SetActive(false);
            ClearFocus();
            if (backToPause) OpenPause();
        }

        void RefreshVibration()
        {
            if (_vibrationLabel != null) _vibrationLabel.text = Settings.Vibration ? Loc.Get("ui.on", "Вкл") : Loc.Get("ui.off", "Выкл");
        }

        void OpenSettings()
        {
            _pause.gameObject.SetActive(false);
            if (_vibrationRow != null) _vibrationRow.SetActive(!Platform.IsMobile);
            _settings.gameObject.SetActive(true);
            _focused = FirstCandidate(_settingsItems);
        }

        void CloseSettings(bool backToPause)
        {
            Settings.Flush();
            _settings.gameObject.SetActive(false);
            ClearFocus();
            if (backToPause) OpenPause();
        }

        void ResetSave()
        {
            var gm = GameManager.I;
            gm.Save.ClearSave();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public void OpenTerminal()
        {
            RefreshTerminal();
            _terminal.gameObject.SetActive(true);
            _focused = FirstCandidate(TerminalCandidates());
        }

        public void CloseTerminal() { _terminal.gameObject.SetActive(false); ClearFocus(); }
        public void OpenSlot()
        {
            _spinUntil = -1f;
            RefreshSlot();
            _slot.gameObject.SetActive(true);
            _focused = FirstCandidate(_slotButtons);
        }
        public void CloseSlot() { _slot.gameObject.SetActive(false); ClearFocus(); }
        public void OpenPause() { _pause.gameObject.SetActive(true); _focused = FirstCandidate(_pauseButtons); }
        public void ClosePause() { _pause.gameObject.SetActive(false); ClearFocus(); }

        static bool GamepadActive()
        {
            var pad = Gamepad.current;
            if (pad == null) return false;
            double t = pad.lastUpdateTime;
            if (Keyboard.current != null && Keyboard.current.lastUpdateTime > t) return false;
            if (Mouse.current != null && Mouse.current.lastUpdateTime > t) return false;
            return true;
        }

        IEnumerable<Selectable> TerminalCandidates()
        {
            foreach (var r in _rows) yield return r.Buy;
            if (_terminalClose != null) yield return _terminalClose;
        }

        static bool Selectable_(Selectable b) => b != null && b.interactable && b.gameObject.activeInHierarchy;

        static Selectable FirstCandidate(IEnumerable<Selectable> buttons)
        {
            foreach (var b in buttons) if (Selectable_(b)) return b;
            return null;
        }

        void ClearFocus()
        {
            if (_focused != null) _focused.transform.localScale = Vector3.one;
            _focused = null;
        }

        void UpdateMenuFocus()
        {
            if (!AnyOpen) return;
            var gm = GameManager.I;
            var list = new List<Selectable>(TerminalOpen ? TerminalCandidates() : SlotOpen ? _slotButtons : UpgradeOpen ? _upgradeButtons : ConfirmOpen ? _confirmButtons : SettingsOpen ? _settingsItems : ControlsOpen ? _controlsItems : _pauseButtons);
            if (_focused != null && !Selectable_(_focused)) _focused = Step(list, _focused, 1) ?? FirstCandidate(list);
            if (_focused == null) _focused = FirstCandidate(list);

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                if (TerminalOpen) CloseTerminal(); else if (SlotOpen) CloseSlot(); else if (UpgradeOpen) CloseUpgrade(); else if (ConfirmOpen) CloseConfirm(); else if (SettingsOpen) CloseSettings(true); else if (ControlsOpen) CloseControls(true); else ClosePause();
                return;
            }
            if (_navigateAction != null)
            {
                var nav = _navigateAction.ReadValue<Vector2>();
                int dir = nav.y > 0.5f ? -1 : nav.y < -0.5f ? 1 : 0;
                int side = nav.x > 0.5f ? 1 : nav.x < -0.5f ? -1 : 0;
                if (dir == 0 && side == 0) _navRepeatAt = 0f;
                else if (_navRepeatAt == 0f || Time.unscaledTime >= _navRepeatAt)
                {
                    bool repeat = _navRepeatAt != 0f;
                    _navRepeatAt = Time.unscaledTime + (repeat ? NavRepeatNext : NavRepeatFirst);
                    if (dir != 0)
                    {
                        var next = Step(list, _focused, dir);
                        if (next != null && next != _focused)
                        {
                            if (_focused != null) _focused.transform.localScale = Vector3.one;
                            _focused = next;
                            Sfx.PlayUi(gm.Config.UiMoveClip);
                            Rumble.UiMove();
                        }
                    }
                    else if (_focused is Slider slider)
                        slider.value = Mathf.Clamp(slider.value + side * SliderStep * (slider.maxValue - slider.minValue), slider.minValue, slider.maxValue);
                }
            }
            if (_submitAction != null && _submitAction.WasPressedThisFrame() && _focused is Button button && Selectable_(button))
                button.onClick.Invoke();

            bool pulse = GamepadActive();
            foreach (var b in list)
            {
                float scale = pulse && b == _focused ? 1f + PulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseHz * Mathf.PI * 2f)) : 1f;
                if (b.transform.localScale.x != scale) b.transform.localScale = new Vector3(scale, scale, 1f);
            }
            if (TerminalOpen) RefreshTerminalFocus();
            RefreshArcadeFocus();
        }

        void Paint(Selectable b, Color idle)
        {
            if (b == null) return;
            var want = b == _focused ? ArcadeFocus : idle;
            var img = b.GetComponent<Image>();
            if (img != null && img.color != want) img.color = want;
            var label = b.transform.Find("Fill/Label");
            if (label != null)
            {
                var text = label.GetComponent<TMP_Text>();
                if (text != null && text.color != want) text.color = want;
            }
        }

        void RefreshTerminalFocus()
        {
            foreach (var r in _rows)
            {
                if (r.Frame == null) continue;
                bool active = r.Buy == _focused;
                var want = active ? ArcadeFocus : ArcadeNeon;
                if (r.Frame.color != want) r.Frame.color = want;
                var label = r.Buy.transform.Find("Fill/Label") as RectTransform;
                if (label != null)
                {
                    var text = label.GetComponent<TMP_Text>();
                    if (text != null && text.color != want) text.color = want;
                }
                var buyFrame = r.Buy.GetComponent<Image>();
                if (buyFrame != null && buyFrame.color != want) buyFrame.color = want;
            }
        }

        static Selectable Step(List<Selectable> list, Selectable from, int dir)
        {
            if (list.Count == 0) return null;
            int start = from != null ? list.IndexOf(from) : -1;
            for (int k = 1; k <= list.Count; k++)
            {
                int i = ((start + dir * k) % list.Count + list.Count) % list.Count;
                if (Selectable_(list[i])) return list[i];
            }
            return null;
        }

        void RefreshStats()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            _carsText.text = Loc.Get("ui.cars", "Машинки") + ": " + gm.PlacedValid + "/" + gm.TotalCars;
            _shelvesText.text = Loc.Get("ui.shelves", "Полки") + ": " + gm.ClosedShelves + "/" + gm.TotalShelves;
            _collectiblesText.text = Loc.Get("ui.crates", "Ящики запчастей") + ": " + gm.Crates;
            RefreshUpgrade();
            float b = gm.Economy.Balance;
            _balanceText.text = FormatMoney(b);
            _balanceText.color = b < 0f ? new Color(1f, 0.35f, 0.35f) : Color.white;
        }

        public static string FormatMoney(float b) => (b < 0f ? "-$" : "$") + Mathf.Abs(b).ToString("0.##");

        void RefreshInventory()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Inventory == null) return;
            var inv = gm.Inventory;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < inv.Items.Count; i++)
            {
                var d = inv.Items[i].Data;
                string name = Loc.Get(d.DisplayName, d.DevName);
                if (i == inv.ActiveIndex) sb.Append("<size=130%><color=#FFFFFF>").Append(name).Append("</color></size>");
                else sb.Append("<color=#B0B0B0>").Append(name).Append("</color>");
                if (i < inv.Items.Count - 1) sb.Append('\n');
            }
            _inventoryText.text = sb.ToString();
            _inventoryCountText.text = inv.Items.Count + "/" + inv.Capacity;
            _inventoryCountText.color = gm.Upgrades.Has(UpgradeKind.InventoryOverCap) ? Gold : Color.white;
        }

        void RefreshAbilities()
        {
            if (Abilities == null) return;
            bool pad = Gamepad.current != null && (Keyboard.current == null || Gamepad.current.lastUpdateTime > Keyboard.current.lastUpdateTime);
            for (int i = 0; i < 3; i++)
            {
                var slot = _abilitySlots[i];
                if (slot == null) continue;
                bool unlocked = Abilities.IsUnlocked(i);
                if (slot.Root.activeSelf != unlocked) slot.Root.SetActive(unlocked);
                if (!unlocked) continue;
                float active = Abilities.ActiveRemaining(i);
                float cd = Abilities.CooldownRemaining(i);
                bool boosted = Abilities.IsBoosted(i);
                if (active > 0f)
                {
                    slot.Fill.color = FillActive;
                    slot.Fill.fillAmount = active / Mathf.Max(0.01f, Abilities.ActiveTotal(i));
                    slot.Icon.color = boosted ? Gold : Color.white;
                }
                else if (cd > 0f)
                {
                    slot.Fill.color = FillCooldown;
                    slot.Fill.fillAmount = cd / Mathf.Max(0.01f, Abilities.CooldownTotal(i));
                    slot.Icon.color = boosted ? GoldCooldown : IconCooldown;
                }
                else
                {
                    slot.Fill.fillAmount = 0f;
                    slot.Icon.color = boosted ? Gold : Color.white;
                }
                string key = ControlHints.Short(PlayerAction(AbilityActions[i]), pad ? ControlHints.GamepadGroup : ControlHints.KeyboardGroup);
                if (slot.Key.text != key) slot.Key.text = key;
            }
        }

        void RefreshTerminal()
        {
            var gm = GameManager.I;
            if (gm == null || _terminalScore == null) return;
            _terminalScore.text = FormatMoney(gm.Economy.Balance);
            foreach (var r in _rows)
            {
                int level = gm.Upgrades.Level(r.Data);
                bool maxed = gm.Upgrades.IsMaxed(r.Data);
                r.Name.text = Loc.Get(r.Data.DisplayName, r.Data.DevName);
                r.Desc.text = Loc.Get(r.Data.Description, r.Data.DevDescription);
                string bonus = SlotBonusText(r.Data.Kind);
                r.Level.text = level + "/" + r.Data.MaxLevel + (bonus != null ? "\n<size=13><color=" + GoldHex + ">" + bonus + "</color></size>" : "");
                if (r.Icon != null) r.Icon.color = bonus != null ? Gold : Color.white;
                r.Cost.text = maxed ? Loc.Get("ui.max", "Макс.") : "$" + gm.Upgrades.NextCost(r.Data);
                r.Buy.interactable = gm.Upgrades.CanBuy(r.Data);
                if (r.Pips != null)
                {
                    float per = r.Data.MaxLevel / (float)r.Pips.Length;
                    for (int i = 0; i < r.Pips.Length; i++)
                        r.Pips[i].color = level >= Mathf.CeilToInt((i + 1) * per) ? PipOn : PipOff;
                }
            }
        }
    }
}
