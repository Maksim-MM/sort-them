using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

namespace SortThem
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameConfig Config;
        public EconomyConfig EconomyConfig;
        public CarCatalog Catalog;
        public LevelLayoutData Layout;
        public UpgradeData[] UpgradeAssets = Array.Empty<UpgradeData>();
        public InputActionAsset InputAsset;
        public Transform CarsRoot;
        public PlayerController Player;
        public Inventory Inventory;
        public Scatterer Scatterer;

        public EconomyService Economy { get; private set; }
        public UpgradeService Upgrades { get; private set; }
        public SaveService Save { get; private set; }
        public MusicPlayer Music { get; private set; }

        public readonly List<CarInstance> Cars = new List<CarInstance>();
        public readonly List<ShelfController> Shelves = new List<ShelfController>();
        public readonly List<RackController> Racks = new List<RackController>();
        public readonly List<Collectible> Collectibles = new List<Collectible>();

        public int PlacedValid { get; private set; }
        public int TotalCars { get; private set; }
        public int ClosedShelves { get; private set; }
        public int TotalShelves { get; private set; }
        public int CollectiblesMask { get; private set; }
        public bool RegisterPaid { get; set; }
        int _registerClicks;
        public int CollectiblesFound
        {
            get { int n = 0, m = CollectiblesMask; while (m != 0) { n += m & 1; m >>= 1; } return n; }
        }
        public bool Ready { get; private set; }
        bool _uiBlocking;
        int _uiReleaseFrame = -1;
        public bool UiBlocking
        {
            get => _uiBlocking || Time.frameCount <= _uiReleaseFrame;
            set
            {
                if (_uiBlocking && !value) _uiReleaseFrame = Time.frameCount + 1;
                _uiBlocking = value;
            }
        }

        public event Action StatsChanged;
        public event Action<ShelfController> ShelfClosed;

        float _activationTimer;

        void Awake()
        {
            I = this;
            Economy = new EconomyService(EconomyConfig);
            Upgrades = new UpgradeService(UpgradeAssets, Economy);
            Save = new SaveService(this, new PlayerPrefsSaveStorage());
            if (InputAsset != null) InputAsset.Enable();
            Settings.Load();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        IEnumerator Start()
        {
            Shelves.Clear();
            Shelves.AddRange(FindObjectsByType<ShelfController>(FindObjectsSortMode.None));
            Shelves.Sort((a, b) => a.ShelfId.CompareTo(b.ShelfId));
            Racks.Clear();
            Racks.AddRange(FindObjectsByType<RackController>(FindObjectsSortMode.None));
            TotalShelves = Shelves.Count;

            yield return InitLocalization();
            foreach (var rack in Racks) rack.RefreshSign();
            foreach (var shelf in Shelves) if (shelf.Tag != null) shelf.Tag.Refresh(shelf);

            CarSpawner.SpawnFromLayout(Layout, CarsRoot, Cars);
            TotalCars = Cars.Count;
            SpawnCollectibles();

            Music = gameObject.AddComponent<MusicPlayer>();
            Music.Play(Config.MusicClips, Settings.MusicTrack);
            bool loaded = Save.Load();
            Debug.Log(loaded ? "SortThem: save loaded" : "SortThem: new game");
            RecountStats();
            Ready = true;
            StatsChanged?.Invoke();
        }

        IEnumerator InitLocalization()
        {
            if (!LocalizationSettings.HasSettings)
            {
                Loc.Ready = false;
                yield break;
            }
            var op = LocalizationSettings.InitializationOperation;
            float timeout = Time.realtimeSinceStartup + 10f;
            while (!op.IsDone && Time.realtimeSinceStartup < timeout) yield return null;
            Loc.Ready = op.IsDone && op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded;
            if (!Loc.Ready) Debug.LogWarning("SortThem: localization not ready, using dev names");
            else
            {
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
                LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            }
        }

        static void OnLocaleChanged(UnityEngine.Localization.Locale locale) => Loc.NotifyChanged();

        void Update()
        {
            Rumble.Tick();
            if (!Ready) return;
            _activationTimer -= Time.deltaTime;
            if (_activationTimer <= 0f)
            {
                _activationTimer = Config.ActivationUpdateInterval;
                if (Player != null) PhysicsActivation.Tick(Cars, Player.transform.position, Config.ActivationRadius);
            }
            Save.Tick(Time.deltaTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!UiBlocking && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) Economy.Add(1000f);
#endif
        }

        public void OnShelfChanged(ShelfController shelf)
        {
            RecountStats();
        }

        public void OnShelfClosed(ShelfController shelf)
        {
            ShelfClosed?.Invoke(shelf);
            if (Ready) Save.SaveNow("shelf closed");
        }

        void SpawnCollectibles()
        {
            Collectibles.Clear();
            if (Layout == null || Layout.CollectiblePrefab == null) return;
            for (int i = 0; i < Layout.Collectibles.Length && i < 32; i++)
            {
                var e = Layout.Collectibles[i];
                var go = Instantiate(Layout.CollectiblePrefab, e.Position, e.Rotation, CarsRoot);
                go.name = "Collectible_" + i;
                var c = go.GetComponent<Collectible>();
                if (c == null) c = go.AddComponent<Collectible>();
                c.Index = i;
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                Collectibles.Add(c);
            }
        }

        public void ApplyCollectiblesMask(int mask)
        {
            CollectiblesMask = mask;
            foreach (var c in Collectibles)
            {
                bool found = (mask & (1 << c.Index)) != 0;
                if (c.gameObject.activeSelf == found) c.gameObject.SetActive(!found);
            }
            StatsChanged?.Invoke();
        }

        public void Collect(Collectible c)
        {
            if (c == null || (CollectiblesMask & (1 << c.Index)) != 0) return;
            CollectiblesMask |= 1 << c.Index;
            Sfx.Play(Config.CollectibleClip, c.transform.position);
            c.gameObject.SetActive(false);
            Messages.Show(string.Format(Loc.Get("msg.collectible_found", "Канистра найдена: {0}/{1}"), CollectiblesFound, Config.CollectiblesTotal));
            StatsChanged?.Invoke();
            if (Ready) Save.SaveNow("collectible");
        }

        public void ClickRegister(CashRegister register)
        {
            if (register == null) return;
            var pos = register.transform.position;
            if (RegisterPaid) return;
            _registerClicks++;
            if (_registerClicks < Config.RegisterClicksRequired)
            {
                Sfx.Play(Config.RegisterClickClip, pos);
                return;
            }
            RegisterPaid = true;
            _registerClicks = 0;
            Economy.Add(Config.RegisterPayout);
            Sfx.Play(Config.RegisterPayClip, pos);
            Messages.Show(string.Format(Loc.Get("msg.register_paid", "Касса: +${0}"), Config.RegisterPayout));
        }

        public void ResetRegisterClicks() => _registerClicks = 0;

        public void RecountStats()
        {
            int placed = 0, closed = 0;
            foreach (var s in Shelves)
            {
                if (s.IsValid) placed += s.Count;
                if (s.IsClosed) closed++;
            }
            PlacedValid = placed;
            ClosedShelves = closed;
            StatsChanged?.Invoke();
        }

        public int UnstuckCars()
        {
            int moved = 0;
            var half = Config.LevelHalfExtents;
            foreach (var car in Cars)
            {
                if (car.State != CarState.Loose) continue;
                var p = car.transform.position;
                bool outside = p.y < Config.FloorY - 0.5f || Mathf.Abs(p.x) > half.x || Mathf.Abs(p.z) > half.z || p.y > Config.FloorY + half.y * 2f;
                if (!outside) continue;
                var target = Config.UnstuckCenter + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(-1.5f, 1.5f));
                car.SetLoose(target, UnityEngine.Random.rotation, false);
                moved++;
            }
            return moved;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && Ready) Save.SaveNow("focus lost");
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Ready) Save.SaveNow("pause");
        }
    }
}
