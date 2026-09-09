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
        public Sprite[] TouchIcons = new Sprite[8];
        public Sprite Circle;

        Canvas _canvas;
        TMP_Text _carsText, _shelvesText, _collectiblesText, _balanceText, _inventoryText, _inventoryCountText, _hintText, _saveText, _toastText;
        CanvasGroup _toast;
        float _toastShownAt = -10f;
        const float ToastFadeIn = 0.15f, ToastHold = 2f, ToastFadeOut = 0.4f;
        RectTransform _terminal, _pause, _settings, _slot;
        TMP_Text _terminalBalance, _slotBalance, _slotResult, _slotRemaining, _spinLabel;
        Image _slotIcon;
        Button _spinButton;
        readonly List<Selectable> _slotButtons = new List<Selectable>();
        UpgradeData _spinReward;
        float _spinUntil = -1f, _spinTickAt;
        const float SpinDuration = 1.2f, SpinTick = 0.07f;
        static readonly Color Gold = new Color(1f, 0.8f, 0.25f, 1f);
        static readonly Color GoldCooldown = new Color(0.6f, 0.48f, 0.15f, 1f);
        const string GoldHex = "#FFCC40";
        readonly List<UpgradeRow> _rows = new List<UpgradeRow>();
        InputAction _pauseAction, _navigateAction, _submitAction, _cancelAction;
        readonly List<(TMP_Text Text, string Key, string Fallback)> _bound = new List<(TMP_Text, string, string)>();
        readonly List<Selectable> _pauseButtons = new List<Selectable>();
        readonly List<Selectable> _settingsItems = new List<Selectable>();
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
            public Image Icon;
        }

        public bool TerminalOpen => _terminal != null && _terminal.gameObject.activeSelf;
        public bool SlotOpen => _slot != null && _slot.gameObject.activeSelf;
        public bool PauseOpen => _pause != null && _pause.gameObject.activeSelf;
        public bool SettingsOpen => _settings != null && _settings.gameObject.activeSelf;
        public bool AnyOpen => TerminalOpen || SlotOpen || PauseOpen || SettingsOpen;

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
                else if (SettingsOpen) { CloseSettings(false); }
                else if (PauseOpen) ClosePause();
                else OpenPause();
            }
            if (_saveText != null && _saveText.gameObject.activeSelf != Time.time < _saveTextUntil) _saveText.gameObject.SetActive(Time.time < _saveTextUntil);
            RefreshAbilities();
            UpdateSpin();
            RefreshTouch();
            if (_rotateOverlay != null) { bool portrait = Screen.height > Screen.width; if (_rotateOverlay.activeSelf != portrait) _rotateOverlay.SetActive(portrait); }
            UpdateMenuFocus();
        }

        void BuildHud()
        {
            var hud = UiFactory.Rect(_safe, "HUD");
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
            _abilitiesPanel = abilities;
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
            var keyText = UiFactory.Text(key.transform, "Text", KeyboardKeys[index], 18f, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.14f, 1f));
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
            UiFactory.Anchored(stickBase.rectTransform, Vector2.zero, new Vector2(60f, 60f), new Vector2(240f, 240f));
            var knob = UiFactory.Image(stickBase.transform, "Knob", Circle, new Color(1f, 1f, 1f, 0.85f), Image.Type.Simple);
            UiFactory.Anchored(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var stick = stickBase.gameObject.AddComponent<TouchStick>();
            stick.Knob = knob.rectTransform;
            stick.Radius = 80f;

            const float margin = 60f, gap = 24f;
            float rowBottom = margin + TouchBig * 0.5f;
            float rowTop = rowBottom + TouchBig + gap;
            float colRight = -(margin + TouchBig * 0.5f);
            float colMiddle = colRight - TouchBig * 0.5f - gap - TouchMid * 0.5f;
            float colLeft = colMiddle - TouchMid * 0.5f - gap - TouchSmall * 0.5f;
            TouchButtonUi(layer, "Take", TouchButton.Interact, TouchIcon(0), new Vector2(1f, 0f), Centered(colRight, rowBottom, TouchBig), TouchBig);
            TouchButtonUi(layer, "Throw", TouchButton.Place, TouchIcon(1), new Vector2(1f, 0f), Centered(colRight, rowTop, TouchBig), TouchBig);
            TouchButtonUi(layer, "Jump", TouchButton.Jump, TouchIcon(2), new Vector2(1f, 0f), Centered(colMiddle, rowTop, TouchMid), TouchMid);
            _touchSprint = TouchButtonUi(layer, "Sprint", TouchButton.Jump, TouchIcon(6), new Vector2(1f, 0f), Centered(colMiddle, rowBottom, TouchSmall), TouchSmall, true);
            _touchCrouch = TouchButtonUi(layer, "Crouch", TouchButton.Crouch, TouchIcon(7), new Vector2(1f, 0f), Centered(colLeft, rowBottom, TouchSmall), TouchSmall);
            TouchButtonUi(layer, "Pause", TouchButton.Pause, TouchIcon(3), new Vector2(1f, 1f), new Vector2(-280f, -16f), 60f);

            _inventoryText.gameObject.SetActive(false);
            UiFactory.Anchored(_inventoryCountText.rectTransform, new Vector2(1f, 0f), new Vector2(-28f, 340f), new Vector2(266f, 32f));
            _inventoryCountText.fontSize = 24f;
            BuildInventoryWheel(layer);
            UiFactory.Anchored(_abilitiesPanel, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(3f * SlotSize + 2f * SlotGap, SlotSize));
            if (_hintText != null) _hintText.gameObject.SetActive(false);
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
                if (data.Source != UpgradeSource.Terminal) continue;
                var row = UiFactory.Panel(_terminal, "Row_" + data.UpgradeID, new Color(1f, 1f, 1f, 0.06f));
                UiFactory.Size(row, 0f, 66f);
                var r = new UpgradeRow { Data = data };
                var icon = UiFactory.Image(row, "Icon", data.Icon, Color.white, Image.Type.Simple);
                icon.preserveAspect = true;
                icon.enabled = data.Icon != null;
                r.Icon = icon;
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

        void BuildSlot()
        {
            _slot = UiFactory.Panel(transform, "SlotMachine", new Color(0.08f, 0.09f, 0.12f, 0.96f));
            UiFactory.Anchored(_slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
            UiFactory.Layout(_slot, 12f, new RectOffset(24, 24, 16, 20));

            var header = UiFactory.Rect(_slot, "Header");
            UiFactory.Size(header, 0f, 48f);
            var title = Bind(UiFactory.Text(header, "Title", "", 34f, TextAlignmentOptions.Left, Color.white), "ui.slot", "Слот-машина");
            UiFactory.Anchor(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _slotBalance = UiFactory.Text(header, "Balance", "", 30f, TextAlignmentOptions.Right, Color.white);
            UiFactory.Anchor(_slotBalance.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-160f, 0f));
            var close = Bind(UiFactory.Button(header, "Close", "", CloseSlot, 20f), "ui.close", "Закрыть");
            UiFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-140f, 4f), new Vector2(0f, -4f));

            var drum = UiFactory.Panel(_slot, "Drum", new Color(1f, 1f, 1f, 0.06f));
            UiFactory.Size(drum, 0f, 150f);
            _slotIcon = UiFactory.Image(drum, "Icon", null, Gold, Image.Type.Simple);
            _slotIcon.preserveAspect = true;
            _slotIcon.enabled = false;
            UiFactory.Anchored(_slotIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(27f, 0f), new Vector2(96f, 96f));
            _slotResult = UiFactory.Text(drum, "Result", "", 28f, TextAlignmentOptions.Left, Color.white);
            _slotResult.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Anchor(_slotResult.rectTransform, Vector2.zero, Vector2.one, new Vector2(150f, 10f), new Vector2(-20f, -10f));

            _slotRemaining = UiFactory.Text(_slot, "Remaining", "", 20f, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f, 1f));
            UiFactory.Size(_slotRemaining, 0f, 30f);

            _spinButton = UiFactory.Button(_slot, "Spin", "", Spin, 26f);
            _spinLabel = _spinButton.GetComponentInChildren<TMP_Text>();
            UiFactory.Size(_spinButton, 0f, 64f);
            _slotButtons.Add(_spinButton);
            _slot.gameObject.SetActive(false);
        }

        void Spin()
        {
            var gm = GameManager.I;
            if (gm == null || Time.unscaledTime < _spinUntil) return;
            var reward = gm.Upgrades.TrySpin(gm.Config.SlotSpinCost);
            if (reward == null) return;
            _spinReward = reward;
            _spinUntil = Time.unscaledTime + SpinDuration;
            _spinTickAt = 0f;
            Sfx.PlayUi(gm.Config.SlotLeverClip);
            Sfx.PlayUi(gm.Config.SlotReelClip);
            RefreshSlot();
        }

        void UpdateSpin()
        {
            if (_spinReward == null) return;
            var gm = GameManager.I;
            if (Time.unscaledTime < _spinUntil)
            {
                if (Time.unscaledTime < _spinTickAt) return;
                _spinTickAt = Time.unscaledTime + SpinTick;
                var pool = new List<UpgradeData>();
                foreach (var u in gm.Upgrades.All) if (u.Source == UpgradeSource.Slot) pool.Add(u);
                if (pool.Count == 0) return;
                var pick = pool[Random.Range(0, pool.Count)];
                _slotResult.text = "<color=#A0A0A8>" + Loc.Get(pick.DisplayName, pick.DevName) + "</color>";
                _slotIcon.sprite = pick.Icon;
                _slotIcon.enabled = pick.Icon != null;
                _slotIcon.color = new Color(0.63f, 0.63f, 0.66f, 1f);
                return;
            }
            var reward = _spinReward;
            _spinReward = null;
            string text = RewardText(reward);
            _slotResult.text = "<color=" + GoldHex + ">" + text + "</color>";
            _slotIcon.sprite = reward.Icon;
            _slotIcon.enabled = reward.Icon != null;
            _slotIcon.color = Gold;
            Sfx.PlayUi(gm.Config.SlotWinClip);
            Rumble.ShelfComplete();
            Messages.Show(string.Format(Loc.Get("msg.slot_reward", "Выпало: {0}"), text));
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
            bool spinning = _spinReward != null;
            _slotRemaining.text = left > 0
                ? string.Format(Loc.Get("ui.slot_remaining", "Осталось наград: {0}"), left)
                : Loc.Get("ui.slot_empty", "Пусто. Все награды выданы");
            if (!spinning && _spinUntil < 0f)
                _slotResult.text = left > 0 ? Loc.Get("ui.slot_idle", "Каждое вращение даёт награду") : "";
            _spinLabel.text = string.Format(Loc.Get("ui.spin", "Крутить · ${0}"), gm.Config.SlotSpinCost);
            _spinButton.interactable = !spinning && gm.Upgrades.CanSpin(gm.Config.SlotSpinCost);
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
                    return parts.Count > 0 ? string.Join("\n", parts) : null;
                }
            }
            return null;
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
            _vibrationRow = vibRow.gameObject;
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
            var list = new List<Selectable>(TerminalOpen ? TerminalCandidates() : SlotOpen ? _slotButtons : SettingsOpen ? _settingsItems : _pauseButtons);
            if (_focused != null && !Selectable_(_focused)) _focused = Step(list, _focused, 1) ?? FirstCandidate(list);
            if (_focused == null) _focused = FirstCandidate(list);

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                if (TerminalOpen) CloseTerminal(); else if (SlotOpen) CloseSlot(); else if (SettingsOpen) CloseSettings(true); else ClosePause();
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
                    else if (_focused == _vibrationButton && !repeat) _vibrationButton.onClick.Invoke();
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
                string bonus = SlotBonusText(r.Data.Kind);
                r.Level.text = level + "/" + r.Data.MaxLevel + (bonus != null ? "\n<size=15><color=" + GoldHex + ">" + bonus + "</color></size>" : "");
                if (r.Icon != null) r.Icon.color = bonus != null ? Gold : Color.white;
                r.Cost.text = maxed ? Loc.Get("ui.max", "Макс.") : "$" + gm.Upgrades.NextCost(r.Data);
                r.Buy.interactable = gm.Upgrades.CanBuy(r.Data);
            }
        }
    }
}
