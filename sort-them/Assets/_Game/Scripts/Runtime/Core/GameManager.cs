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
        public readonly List<Bomb> Bombs = new List<Bomb>();
        public Bomb HeldBomb { get; private set; }
        public int PendingBombs => Bombs.Count;

        public int PlacedValid { get; private set; }
        public int TotalCars { get; private set; }
        public int ClosedShelves { get; private set; }
        public int TotalShelves { get; private set; }
        public int CollectiblesMask { get; private set; }
        public bool RegisterPaid { get; set; }
        public bool TutorialDone { get; set; }
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
            PileOcclusion.Shutdown();
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
            PileOcclusion.Init(Cars, Config);
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
                Settings.EnsureLoaded();
                if (!string.IsNullOrEmpty(Settings.Locale))
                {
                    var saved = LocalizationSettings.AvailableLocales.GetLocale(Settings.Locale);
                    if (saved != null && LocalizationSettings.SelectedLocale != saved) LocalizationSettings.SelectedLocale = saved;
                }
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
                if (Player != null) PhysicsActivation.Tick(Cars, Player.transform.position, Config.ActivationRadius, Config.FreezeSpeed, Config.FreezeDelay, 3, Time.frameCount);
            }
            Save.Tick(Time.deltaTime);
            PileOcclusion.Tick();
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

        public Bomb SpawnBomb(bool announce = true)
        {
            if (Config.BombPrefab == null) return null;
            Vector3 pos = Config.UnstuckCenter;
            var candidates = new List<CarInstance>();
            foreach (var c in Cars)
                if (c.State == CarState.Loose && c.gameObject.activeSelf && c.transform.position.y < Config.FloorY + 1.5f) candidates.Add(c);
            if (candidates.Count > 0) pos = candidates[UnityEngine.Random.Range(0, candidates.Count)].transform.position + Vector3.up * 1.5f;
            var go = Instantiate(Config.BombPrefab, pos, UnityEngine.Random.rotation, CarsRoot);
            go.name = "Bomb";
            var bomb = go.GetComponent<Bomb>();
            if (bomb == null) bomb = go.AddComponent<Bomb>();
            Bombs.Add(bomb);
            if (announce) Messages.Show(Loc.Get("msg.bomb_spawned", "Из автомата выпала бомба, ищи в куче"));
            return bomb;
        }

        public void SpawnBombs(int count)
        {
            for (int i = 0; i < count; i++) SpawnBomb(false);
        }

        public void PickBomb(Bomb bomb, Transform hand)
        {
            if (bomb == null || bomb.Held || HeldBomb != null) return;
            HeldBomb = bomb;
            bomb.Hold(hand, Config.BombHandPosition, Quaternion.Euler(Config.BombHandEuler));
            bomb.Ignite(Config.BombFuseTime, Config.BombFuseClip);
            Sfx.Play(Config.PickupClip, hand.position);
        }

        public void ThrowBomb(Vector3 origin, Quaternion rotation, Vector3 velocity)
        {
            if (HeldBomb == null) return;
            var bomb = HeldBomb;
            HeldBomb = null;
            bomb.Release(CarsRoot, origin, rotation, velocity);
            Sfx.Play(Config.ThrowClip, origin);
        }

        public void ExplodeBomb(Bomb bomb)
        {
            if (bomb == null) return;
            Vector3 pos = bomb.transform.position;
            float mult = bomb.Held ? Config.BombHandMultiplier : 1f;
            if (bomb == HeldBomb) HeldBomb = null;
            Bombs.Remove(bomb);
            float radius = Config.BombRadius, r2 = radius * radius;
            foreach (var car in Cars)
            {
                if (car.State != CarState.Loose || !car.gameObject.activeSelf) continue;
                if ((car.transform.position - pos).sqrMagnitude > r2) continue;
                car.Unfreeze();
                car.Body.AddExplosionForce(Config.BombForce * mult, pos, radius, Config.BombUpwardModifier, ForceMode.VelocityChange);
            }
            Sfx.Play(Config.BombExplodeClip, pos);
            StartCoroutine(BombFlash(pos, radius * mult));
            Destroy(bomb.gameObject);
        }

        IEnumerator BombFlash(Vector3 pos, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BombFlash";
            Destroy(go.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 0.85f, 0.45f, 1f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            const float duration = 0.2f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, radius * 2f, t / duration);
                yield return null;
            }
            Destroy(mat);
            Destroy(go);
        }

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

        public bool Shuffling { get; private set; }

        public IEnumerator ShuffleLoose(Action<float> progress = null)
        {
            if (Shuffling || !Ready || Scatterer == null) yield break;
            Shuffling = true;
            var loose = new List<CarInstance>();
            foreach (var car in Cars)
                if (car.State == CarState.Loose && car.gameObject.activeSelf && !car.Levitating) loose.Add(car);
            var bodies = new List<Rigidbody>();
            foreach (var bomb in Bombs) if (bomb != null && !bomb.Held) bodies.Add(bomb.Body);
            var rng = new System.Random(Environment.TickCount);
            for (int i = loose.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (loose[i], loose[j]) = (loose[j], loose[i]); }

            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                float dt = Time.fixedDeltaTime;
                int perStep = Mathf.Max(1, Config.ShuffleCarsPerStep), perFrame = Mathf.Max(1, Config.ShuffleStepsPerFrame), maxSteps = Mathf.Max(1, Config.ShuffleMaxSteps);
                int next = 0, steps = 0;
                foreach (var body in bodies) Scatterer.LaunchBody(body, rng);
                while (steps < maxSteps)
                {
                    for (int f = 0; f < perFrame && steps < maxSteps; f++)
                    {
                        for (int k = 0; k < perStep && next < loose.Count; k++, next++) Scatterer.LaunchCar(loose[next], rng);
                        Physics.Simulate(dt);
                        steps++;
                    }
                    progress?.Invoke(next < loose.Count ? 0.6f * next / Mathf.Max(1, loose.Count) : 0.6f + 0.4f * Mathf.Clamp01((steps - loose.Count / (float)perStep) / 600f));
                    if (next >= loose.Count && steps % 25 == 0 && AllSleeping(loose)) break;
                    yield return null;
                }
                var half = Config.LevelHalfExtents;
                foreach (var car in loose)
                {
                    var p = car.transform.position;
                    if (p.y < Config.FloorY - 0.2f || Mathf.Abs(p.x) > half.x || Mathf.Abs(p.z) > half.z)
                        car.SetLoose(Config.UnstuckCenter + new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble(), (float)rng.NextDouble() * 2f - 1f), UnityEngine.Random.rotation, true);
                    else car.Freeze();
                }
            }
            finally
            {
                Physics.simulationMode = prevMode;
                Shuffling = false;
            }
        }

        static bool AllSleeping(List<CarInstance> cars)
        {
            foreach (var car in cars) if (!car.Body.isKinematic && !car.Body.IsSleeping()) return false;
            return true;
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
