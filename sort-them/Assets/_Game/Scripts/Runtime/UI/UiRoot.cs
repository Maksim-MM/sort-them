using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SortThem
{
    public class UiRoot : MonoBehaviour
    {
        public static UiRoot I { get; private set; }

        public PlayerAbilities Abilities;
        public Sprite[] AbilityIcons = new Sprite[3];
        public Sprite SlotFrame, KeyFrame;

        Canvas _canvas;
        TMP_Text _carsText, _shelvesText, _collectiblesText, _balanceText, _inventoryText, _inventoryCountText, _hintText, _saveText, _toastText;
        CanvasGroup _toast;
        float _toastShownAt = -10f;
        const float ToastFadeIn = 0.15f, ToastHold = 2f, ToastFadeOut = 0.4f;
        RectTransform _terminal, _pause, _settings;
        TMP_Text _terminalBalance;
        readonly List<UpgradeRow> _rows = new List<UpgradeRow>();
        InputAction _pauseAction, _navigateAction, _submitAction, _cancelAction;
        readonly List<(TMP_Text Text, string Key, string Fallback)> _bound = new List<(TMP_Text, string, string)>();
        readonly List<Selectable> _pauseButtons = new List<Selectable>();
        readonly List<Selectable> _settingsItems = new List<Selectable>();
        Selectable _focused;
        Button _vibrationButton;
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
        static readonly string[] KeyboardKeys = { "1", "2", "3" };
        static readonly string[] GamepadKeys = { "\u2191", "\u2190", "\u2192" };
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
        }

        public bool TerminalOpen => _terminal != null && _terminal.gameObject.activeSelf;
        public bool PauseOpen => _pause != null && _pause.gameObject.activeSelf;
        public bool SettingsOpen => _settings != null && _settings.gameObject.activeSelf;
        public bool AnyOpen => TerminalOpen || PauseOpen || SettingsOpen;

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
                scaler.matchWidthOrHeight = 0.5f;
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        void Start()
        {
            var gm = GameManager.I;
            BuildHud();
            BuildTerminal();
            BuildPause();
            BuildSettings();
            gm.StatsChanged += RefreshStats;
            gm.Economy.Changed += _ => { RefreshStats(); RefreshTerminal(); };
            gm.Upgrades.Changed += _ => { RefreshTerminal(); RefreshInventory(); };
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
            RefreshVibration();
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
            if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
            {
                if (TerminalOpen) CloseTerminal();
                else if (SettingsOpen) { CloseSettings(false); }
                else if (PauseOpen) ClosePause();
                else OpenPause();
            }
            if (_saveText != null && _saveText.gameObject.activeSelf != Time.time < _saveTextUntil) _saveText.gameObject.SetActive(Time.time < _saveTextUntil);
            RefreshAbilities();
            UpdateMenuFocus();
        }

        void BuildHud()
        {
            var hud = UiFactory.Rect(transform, "HUD");
            UiFactory.Anchor(hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var stats = UiFactory.Panel(hud, "Stats", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Anchored(stats, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(320f, 110f));
            UiFactory.Layout(stats, 2f, new RectOffset(12, 12, 8, 8));
            _carsText = UiFactory.Text(stats, "Cars", "", 24f, TextAlignmentOptions.Left, Color.white);
            _shelvesText = UiFactory.Text(stats, "Shelves", "", 24f, TextAlignmentOptions.Left, Color.white);
            _collectiblesText = UiFactory.Text(stats, "Collectibles", "", 24f, TextAlignmentOptions.Left, Color.white);

            var balancePanel = UiFactory.Panel(hud, "Balance", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Anchored(balancePanel, new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(240f, 56f));
            _balanceText = UiFactory.Text(balancePanel, "Text", "$0", 32f, TextAlignmentOptions.Center, Color.white);
            UiFactory.Anchor(_balanceText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

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
            UiFactory.Anchored(abilities, Vector2.zero, new Vector2(24f, 24f), new Vector2(3f * SlotSize + 2f * SlotGap, SlotSize + 52f));
            for (int i = 0; i < 3; i++) _abilitySlots[i] = BuildAbilitySlot(abilities, i);

            _hintText = Bind(UiFactory.Text(hud, "Hint", "", 18f, TextAlignmentOptions.Top, new Color(1f, 1f, 1f, 0.6f)), "ui.hint", "ЛКМ взять · ПКМ поставить/бросить · колесо выбрать · Esc меню");
            UiFactory.Anchored(_hintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(900f, 30f));

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
        }

        AbilitySlot BuildAbilitySlot(Transform parent, int index)
        {
            var root = UiFactory.Rect(parent, "Ability" + (index + 1));
            UiFactory.Anchored(root, Vector2.zero, new Vector2(index * (SlotSize + SlotGap), 0f), new Vector2(SlotSize, SlotSize));
            var frame = UiFactory.Image(root, "Frame", SlotFrame, SlotColor, Image.Type.Sliced);
            frame.pixelsPerUnitMultiplier = 2.4f;
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
            UiFactory.Anchored(key.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 38f), new Vector2(30f, 30f));
            var keyText = UiFactory.Text(key.transform, "Text", KeyboardKeys[index], 18f, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.14f, 1f));
            keyText.fontStyle = FontStyles.Bold;
            UiFactory.Anchor(keyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);
            return new AbilitySlot { Root = root.gameObject, Icon = icon, Fill = fill, Key = keyText };
        }

        void BuildTerminal()
        {
            _terminal = UiFactory.Panel(transform, "Terminal", new Color(0.08f, 0.09f, 0.12f, 0.96f));
            UiFactory.Anchored(_terminal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));
            UiFactory.Layout(_terminal, 6f, new RectOffset(20, 20, 16, 16));

            var header = UiFactory.Rect(_terminal, "Header");
            UiFactory.Size(header, 0f, 48f);
            var title = Bind(UiFactory.Text(header, "Title", "", 34f, TextAlignmentOptions.Left, Color.white), "ui.terminal", "Терминал улучшений");
            UiFactory.Anchor(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _terminalBalance = UiFactory.Text(header, "Balance", "", 30f, TextAlignmentOptions.Right, Color.white);
            UiFactory.Anchor(_terminalBalance.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-160f, 0f));
            var close = Bind(UiFactory.Button(header, "Close", "", CloseTerminal, 20f), "ui.close", "Закрыть");
            UiFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-140f, 4f), new Vector2(0f, -4f));

            var gm = GameManager.I;
            foreach (var data in gm.Upgrades.All)
            {
                var row = UiFactory.Panel(_terminal, "Row_" + data.UpgradeID, new Color(1f, 1f, 1f, 0.06f));
                UiFactory.Size(row, 0f, 66f);
                var r = new UpgradeRow { Data = data };
                var icon = UiFactory.Image(row, "Icon", data.Icon, Color.white, Image.Type.Simple);
                icon.preserveAspect = true;
                icon.enabled = data.Icon != null;
                UiFactory.Anchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(52f, 52f));
                r.Name = UiFactory.Text(row, "Name", "", 24f, TextAlignmentOptions.Left, Color.white);
                UiFactory.Anchor(r.Name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f), new Vector2(72f, 0f), new Vector2(0f, -4f));
                r.Desc = UiFactory.Text(row, "Desc", "", 17f, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f, 1f));
                r.Desc.textWrappingMode = TextWrappingModes.Normal;
                UiFactory.Anchor(r.Desc.rectTransform, new Vector2(0f, 0f), new Vector2(0.62f, 0.5f), new Vector2(72f, 4f), new Vector2(0f, 0f));
                r.Level = UiFactory.Text(row, "Level", "", 22f, TextAlignmentOptions.Center, Color.white);
                UiFactory.Anchor(r.Level.rectTransform, new Vector2(0.62f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero);
                r.Cost = UiFactory.Text(row, "Cost", "", 24f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.5f, 1f));
                UiFactory.Anchor(r.Cost.rectTransform, new Vector2(0.74f, 0f), new Vector2(0.86f, 1f), Vector2.zero, Vector2.zero);
                var captured = data;
                r.Buy = Bind(UiFactory.Button(row, "Buy", "", () => { if (gm.Upgrades.TryBuy(captured)) { Sfx.PlayUi(gm.Config.PurchaseClip); gm.Save.SaveNow("purchase"); } }, 20f), "ui.buy", "Купить");
                UiFactory.Anchor(r.Buy.GetComponent<RectTransform>(), new Vector2(0.87f, 0.15f), new Vector2(0.99f, 0.85f), Vector2.zero, Vector2.zero);
                _rows.Add(r);
            }
            _terminal.gameObject.SetActive(false);
        }

        void BuildPause()
        {
            _pause = UiFactory.Panel(transform, "Pause", new Color(0.08f, 0.09f, 0.12f, 0.96f));
            UiFactory.Anchored(_pause, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 428f));
            UiFactory.Layout(_pause, 12f, new RectOffset(24, 24, 20, 20));
            var title = Bind(UiFactory.Text(_pause, "Title", "", 34f, TextAlignmentOptions.Center, Color.white), "ui.pause", "Пауза");
            UiFactory.Size(title, 0f, 50f);
            _pauseButtons.Add(Bind(UiFactory.Button(_pause, "Resume", "", ClosePause), "ui.resume", "Продолжить"));
            _pauseButtons.Add(Bind(UiFactory.Button(_pause, "Save", "", () => GameManager.I.Save.SaveNow("manual")), "ui.save", "Сохранить"));
            _pauseButtons.Add(Bind(UiFactory.Button(_pause, "Settings", "", OpenSettings), "ui.settings", "Настройки"));
            _pauseButtons.Add(Bind(UiFactory.Button(_pause, "Unstuck", "", () => GameManager.I.UnstuckCars()), "ui.unstuck", "Вернуть застрявшие машинки"));
            _pauseButtons.Add(Bind(UiFactory.Button(_pause, "NewGame", "", ResetSave), "ui.newgame", "Сбросить прогресс"));
            foreach (var b in _pauseButtons) UiFactory.Size(b, 0f, 56f);
            _pause.gameObject.SetActive(false);
        }

        void BuildSettings()
        {
            _settings = UiFactory.Panel(transform, "Settings", new Color(0.08f, 0.09f, 0.12f, 0.96f));
            UiFactory.Anchored(_settings, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 510f));
            UiFactory.Layout(_settings, 10f, new RectOffset(24, 24, 20, 20));
            var title = Bind(UiFactory.Text(_settings, "Title", "", 34f, TextAlignmentOptions.Center, Color.white), "ui.settings", "Настройки");
            UiFactory.Size(title, 0f, 50f);

            _settingsItems.Add(SettingsSlider("Music", "ui.music", "Музыка", 0f, 1f, Settings.MusicVolume, Settings.SetMusicVolume, Percent));
            _settingsItems.Add(SettingsSlider("Sfx", "ui.sfx", "Эффекты", 0f, 1f, Settings.SfxVolume, Settings.SetSfxVolume, Percent));
            _settingsItems.Add(SettingsSlider("SensX", "ui.sens_x", "Чувствительность по горизонтали", Settings.SensitivityMin, Settings.SensitivityMax, Settings.SensitivityX, Settings.SetSensitivityX, Multiplier));
            _settingsItems.Add(SettingsSlider("SensY", "ui.sens_y", "Чувствительность по вертикали", Settings.SensitivityMin, Settings.SensitivityMax, Settings.SensitivityY, Settings.SetSensitivityY, Multiplier));

            var vibRow = UiFactory.Rect(_settings, "Vibration");
            UiFactory.Size(vibRow, 0f, 52f);
            var vibLabel = Bind(UiFactory.Text(vibRow, "Label", "", 21f, TextAlignmentOptions.Left, Color.white), "ui.vibration", "Вибрация");
            UiFactory.Anchor(vibLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(8f, 0f), Vector2.zero);
            _vibrationButton = UiFactory.Button(vibRow, "Toggle", "", () => { Settings.SetVibration(!Settings.Vibration); RefreshVibration(); }, 20f);
            _vibrationLabel = _vibrationButton.GetComponentInChildren<TMP_Text>();
            UiFactory.Anchor(_vibrationButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.1f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
            _settingsItems.Add(_vibrationButton);
            RefreshVibration();

            var back = Bind(UiFactory.Button(_settings, "Back", "", () => CloseSettings(true)), "ui.back", "Назад");
            UiFactory.Size(back, 0f, 56f);
            _settingsItems.Add(back);
            _settings.gameObject.SetActive(false);
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
        static string Multiplier(float v) => "×" + v.ToString("0.0");

        Slider SettingsSlider(string name, string labelKey, string labelFallback, float min, float max, float value, System.Action<float> apply, System.Func<float, string> format)
        {
            var row = UiFactory.Rect(_settings, name);
            UiFactory.Size(row, 0f, 52f);
            var text = Bind(UiFactory.Text(row, "Label", "", 21f, TextAlignmentOptions.Left, Color.white), labelKey, labelFallback);
            UiFactory.Anchor(text.rectTransform, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(8f, 0f), Vector2.zero);
            var slider = UiFactory.Slider(row, "Slider", min, max, value);
            UiFactory.Anchor(slider.GetComponent<RectTransform>(), new Vector2(0.54f, 0f), new Vector2(0.85f, 1f), Vector2.zero, Vector2.zero);
            var valueText = UiFactory.Text(row, "Value", format(value), 20f, TextAlignmentOptions.Right, new Color(0.8f, 0.8f, 0.85f, 1f));
            UiFactory.Anchor(valueText.rectTransform, new Vector2(0.87f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-8f, 0f));
            slider.onValueChanged.AddListener(v => { apply(v); valueText.text = format(v); });
            return slider;
        }

        void RefreshVibration()
        {
            if (_vibrationLabel != null) _vibrationLabel.text = Settings.Vibration ? Loc.Get("ui.on", "Вкл") : Loc.Get("ui.off", "Выкл");
        }

        void OpenSettings()
        {
            _pause.gameObject.SetActive(false);
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
        }

        static Selectable FirstCandidate(IEnumerable<Selectable> buttons)
        {
            foreach (var b in buttons) if (b.interactable) return b;
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
            var list = new List<Selectable>(TerminalOpen ? TerminalCandidates() : SettingsOpen ? _settingsItems : _pauseButtons);
            if (_focused != null && !_focused.interactable) _focused = Step(list, _focused, 1) ?? FirstCandidate(list);
            if (_focused == null) _focused = FirstCandidate(list);

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                if (TerminalOpen) CloseTerminal(); else if (SettingsOpen) CloseSettings(true); else ClosePause();
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
                        }
                    }
                    else if (_focused is Slider slider)
                        slider.value = Mathf.Clamp(slider.value + side * SliderStep * (slider.maxValue - slider.minValue), slider.minValue, slider.maxValue);
                    else if (_focused == _vibrationButton && !repeat) _vibrationButton.onClick.Invoke();
                }
            }
            if (_submitAction != null && _submitAction.WasPressedThisFrame() && _focused is Button button && button.interactable)
                button.onClick.Invoke();

            bool pulse = GamepadActive();
            foreach (var b in list)
            {
                float scale = pulse && b == _focused ? 1f + PulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseHz * Mathf.PI * 2f)) : 1f;
                if (b.transform.localScale.x != scale) b.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        static Selectable Step(List<Selectable> list, Selectable from, int dir)
        {
            if (list.Count == 0) return null;
            int start = from != null ? list.IndexOf(from) : -1;
            for (int k = 1; k <= list.Count; k++)
            {
                int i = ((start + dir * k) % list.Count + list.Count) % list.Count;
                if (list[i].interactable) return list[i];
            }
            return null;
        }

        void RefreshStats()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            _carsText.text = Loc.Get("ui.cars", "Машинки") + ": " + gm.PlacedValid + "/" + gm.TotalCars;
            _shelvesText.text = Loc.Get("ui.shelves", "Полки") + ": " + gm.ClosedShelves + "/" + gm.TotalShelves;
            _collectiblesText.text = Loc.Get("ui.collectibles", "Канистры") + ": " + gm.CollectiblesFound + "/" + gm.Config.CollectiblesTotal;
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
                if (active > 0f)
                {
                    slot.Fill.color = FillActive;
                    slot.Fill.fillAmount = active / Mathf.Max(0.01f, Abilities.ActiveTotal(i));
                    slot.Icon.color = Color.white;
                }
                else if (cd > 0f)
                {
                    slot.Fill.color = FillCooldown;
                    slot.Fill.fillAmount = cd / Mathf.Max(0.01f, Abilities.CooldownTotal(i));
                    slot.Icon.color = IconCooldown;
                }
                else
                {
                    slot.Fill.fillAmount = 0f;
                    slot.Icon.color = Color.white;
                }
                string key = pad ? GamepadKeys[i] : KeyboardKeys[i];
                if (slot.Key.text != key) slot.Key.text = key;
            }
        }

        void RefreshTerminal()
        {
            var gm = GameManager.I;
            if (gm == null || _terminalBalance == null) return;
            _terminalBalance.text = FormatMoney(gm.Economy.Balance);
            foreach (var r in _rows)
            {
                int level = gm.Upgrades.Level(r.Data);
                bool maxed = gm.Upgrades.IsMaxed(r.Data);
                r.Name.text = Loc.Get(r.Data.DisplayName, r.Data.DevName);
                r.Desc.text = Loc.Get(r.Data.Description, r.Data.DevDescription);
                r.Level.text = level + "/" + r.Data.MaxLevel;
                r.Cost.text = maxed ? Loc.Get("ui.max", "Макс.") : "$" + gm.Upgrades.NextCost(r.Data);
                r.Buy.interactable = gm.Upgrades.CanBuy(r.Data);
            }
        }
    }
}
