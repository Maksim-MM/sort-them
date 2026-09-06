using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SortThem
{
    public class UiRoot : MonoBehaviour
    {
        public static UiRoot I { get; private set; }

        public PlayerAbilities Abilities;

        Canvas _canvas;
        TMP_Text _carsText, _shelvesText, _collectiblesText, _balanceText, _inventoryText, _inventoryCountText, _abilitiesText, _hintText, _saveText, _toastText;
        CanvasGroup _toast;
        float _toastShownAt = -10f;
        const float ToastFadeIn = 0.15f, ToastHold = 2f, ToastFadeOut = 0.4f;
        RectTransform _terminal, _pause;
        TMP_Text _terminalBalance;
        readonly List<UpgradeRow> _rows = new List<UpgradeRow>();
        InputAction _pauseAction;
        float _saveTextUntil;

        class UpgradeRow
        {
            public UpgradeData Data;
            public TMP_Text Name, Desc, Level, Cost;
            public Button Buy;
        }

        public bool TerminalOpen => _terminal != null && _terminal.gameObject.activeSelf;
        public bool PauseOpen => _pause != null && _pause.gameObject.activeSelf;
        public bool AnyOpen => TerminalOpen || PauseOpen;

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
            gm.StatsChanged += RefreshStats;
            gm.Economy.Changed += _ => { RefreshStats(); RefreshTerminal(); };
            gm.Upgrades.Changed += _ => { RefreshTerminal(); RefreshInventory(); };
            gm.Inventory.Changed += RefreshInventory;
            Messages.Shown += ShowToast;
            gm.Save.Saved += reason => { _saveText.text = Loc.Get("ui.saved", "Сохранено"); _saveTextUntil = Time.time + 2f; };
            _pauseAction = gm.InputAsset.FindActionMap("Player", true).FindAction("Pause", true);
            RefreshStats();
            RefreshInventory();
            RefreshTerminal();
        }

        void OnDestroy()
        {
            Messages.Shown -= ShowToast;
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
                else if (PauseOpen) ClosePause();
                else OpenPause();
            }
            if (_saveText != null && _saveText.gameObject.activeSelf != Time.time < _saveTextUntil) _saveText.gameObject.SetActive(Time.time < _saveTextUntil);
            RefreshAbilities();
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

            _abilitiesText = UiFactory.Text(hud, "Abilities", "", 20f, TextAlignmentOptions.BottomLeft, Color.white);
            UiFactory.Anchored(_abilitiesText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 70f), new Vector2(600f, 80f));

            _hintText = UiFactory.Text(hud, "Hint", Loc.Get("ui.hint", "ЛКМ взять · ПКМ поставить/бросить · колесо выбрать · Esc меню"), 18f, TextAlignmentOptions.Top, new Color(1f, 1f, 1f, 0.6f));
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

        void BuildTerminal()
        {
            _terminal = UiFactory.Panel(transform, "Terminal", new Color(0.08f, 0.09f, 0.12f, 0.96f));
            UiFactory.Anchored(_terminal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));
            UiFactory.Layout(_terminal, 6f, new RectOffset(20, 20, 16, 16));

            var header = UiFactory.Rect(_terminal, "Header");
            UiFactory.Size(header, 0f, 48f);
            var title = UiFactory.Text(header, "Title", Loc.Get("ui.terminal", "Терминал улучшений"), 34f, TextAlignmentOptions.Left, Color.white);
            UiFactory.Anchor(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _terminalBalance = UiFactory.Text(header, "Balance", "", 30f, TextAlignmentOptions.Right, Color.white);
            UiFactory.Anchor(_terminalBalance.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-160f, 0f));
            var close = UiFactory.Button(header, "Close", Loc.Get("ui.close", "Закрыть"), CloseTerminal, 20f);
            UiFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-140f, 4f), new Vector2(0f, -4f));

            var gm = GameManager.I;
            foreach (var data in gm.Upgrades.All)
            {
                var row = UiFactory.Panel(_terminal, "Row_" + data.UpgradeID, new Color(1f, 1f, 1f, 0.06f));
                UiFactory.Size(row, 0f, 66f);
                var r = new UpgradeRow { Data = data };
                r.Name = UiFactory.Text(row, "Name", "", 24f, TextAlignmentOptions.Left, Color.white);
                UiFactory.Anchor(r.Name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f), new Vector2(12f, 0f), new Vector2(0f, -4f));
                r.Desc = UiFactory.Text(row, "Desc", "", 17f, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f, 1f));
                r.Desc.textWrappingMode = TextWrappingModes.Normal;
                UiFactory.Anchor(r.Desc.rectTransform, new Vector2(0f, 0f), new Vector2(0.62f, 0.5f), new Vector2(12f, 4f), new Vector2(0f, 0f));
                r.Level = UiFactory.Text(row, "Level", "", 22f, TextAlignmentOptions.Center, Color.white);
                UiFactory.Anchor(r.Level.rectTransform, new Vector2(0.62f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero);
                r.Cost = UiFactory.Text(row, "Cost", "", 24f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.5f, 1f));
                UiFactory.Anchor(r.Cost.rectTransform, new Vector2(0.74f, 0f), new Vector2(0.86f, 1f), Vector2.zero, Vector2.zero);
                var captured = data;
                r.Buy = UiFactory.Button(row, "Buy", Loc.Get("ui.buy", "Купить"), () => { if (gm.Upgrades.TryBuy(captured)) gm.Save.SaveNow("purchase"); }, 20f);
                UiFactory.Anchor(r.Buy.GetComponent<RectTransform>(), new Vector2(0.87f, 0.15f), new Vector2(0.99f, 0.85f), Vector2.zero, Vector2.zero);
                _rows.Add(r);
            }
            _terminal.gameObject.SetActive(false);
        }

        void BuildPause()
        {
            _pause = UiFactory.Panel(transform, "Pause", new Color(0.08f, 0.09f, 0.12f, 0.96f));
            UiFactory.Anchored(_pause, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 360f));
            UiFactory.Layout(_pause, 12f, new RectOffset(24, 24, 20, 20));
            var title = UiFactory.Text(_pause, "Title", Loc.Get("ui.pause", "Пауза"), 34f, TextAlignmentOptions.Center, Color.white);
            UiFactory.Size(title, 0f, 50f);
            UiFactory.Size(UiFactory.Button(_pause, "Resume", Loc.Get("ui.resume", "Продолжить"), ClosePause), 0f, 56f);
            UiFactory.Size(UiFactory.Button(_pause, "Save", Loc.Get("ui.save", "Сохранить"), () => GameManager.I.Save.SaveNow("manual")), 0f, 56f);
            UiFactory.Size(UiFactory.Button(_pause, "Unstuck", Loc.Get("ui.unstuck", "Вернуть застрявшие машинки"), () => GameManager.I.UnstuckCars()), 0f, 56f);
            UiFactory.Size(UiFactory.Button(_pause, "NewGame", Loc.Get("ui.newgame", "Сбросить сохранение"), ResetSave), 0f, 56f);
            _pause.gameObject.SetActive(false);
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
        }

        public void CloseTerminal() => _terminal.gameObject.SetActive(false);
        public void OpenPause() => _pause.gameObject.SetActive(true);
        public void ClosePause() => _pause.gameObject.SetActive(false);

        void RefreshStats()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            _carsText.text = Loc.Get("ui.cars", "Машинки") + ": " + gm.PlacedValid + "/" + gm.TotalCars;
            _shelvesText.text = Loc.Get("ui.shelves", "Полки") + ": " + gm.ClosedShelves + "/" + gm.TotalShelves;
            _collectiblesText.text = Loc.Get("ui.collectibles", "Коллекция") + ": " + gm.CollectiblesFound + "/" + gm.Config.CollectiblesTotal;
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
            if (Abilities == null || _abilitiesText == null) return;
            var sb = new System.Text.StringBuilder();
            string[] names = { Loc.Get("ui.ab1", "Поиск"), Loc.Get("ui.ab2", "Автосбор"), Loc.Get("ui.ab3", "Стеллаж") };
            for (int i = 0; i < 3; i++)
            {
                if (!Abilities.IsUnlocked(i)) continue;
                float cd = Abilities.CooldownRemaining(i);
                float active = Abilities.ActiveRemaining(i);
                sb.Append(i + 1).Append(": ").Append(names[i]);
                if (active > 0f) sb.Append(" <color=#7FFF7F>").Append(Mathf.CeilToInt(active)).Append("s</color>");
                else if (cd > 0f) sb.Append(" (").Append(Mathf.CeilToInt(cd)).Append(')');
                sb.Append('\n');
            }
            _abilitiesText.text = sb.ToString();
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
