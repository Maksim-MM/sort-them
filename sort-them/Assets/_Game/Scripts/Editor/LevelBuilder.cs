using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SortThem.Editor
{
    public static class LevelBuilder
    {
        const float ArmLen = 28f, WingW = 10f, RoomH = 5f, WallT = 0.3f;
        const float Min = -ArmLen * 0.5f, Max = ArmLen * 0.5f, Inner = Min + WingW;
        const float WestX = (Min + Inner) * 0.5f, SouthZ = (Min + Inner) * 0.5f;
        static readonly Vector3 SpecialRackPos = new Vector3(WestX, 0f, SouthZ);
        static readonly Vector3 FountainPos = SpecialRackPos + new Vector3(0f, 3.5f, 0f);
        const float ShopX = Max - 0.3f;
        const float BoardT = 0.04f, DividerT = 0.05f, ShelfPitch = 0.34f;
        static float RackW = 2.45f, RackD = 1.1f;
        const int ShelvesPerSection = 5, Sections = 4;
        const float BottomShelfHeight = 0.4f;
        const float SpecialDisplayScale = 2f;
        static readonly float[] ShelfHeights = { BottomShelfHeight, BottomShelfHeight + ShelfPitch, BottomShelfHeight + ShelfPitch * 2f, BottomShelfHeight + ShelfPitch * 3f, BottomShelfHeight + ShelfPitch * 4f };
        static float RackH => ShelfHeights[ShelfHeights.Length - 1] + ShelfPitch - BoardT;
        static float RackTotalW => Sections * RackW + (Sections - 1) * DividerT;

        static Material _floor, _wall, _ceiling, _rack, _board, _podium, _terminal, _cabinet, _radio, _plateWhite, _plateRed, _plateGold, _marker, _ghost, _outline, _highlight, _levOutline, _tutOutline, _heldCars;
        static Material _woodBeam, _woodPanel, _woodPanelV, _woodFloor, _plaster, _ceilingPlaster, _glass, _sky, _rug, _lampGlow, _rackBack;

        [MenuItem("SortThem/4. Build Level Scene")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(Paths.Catalog);
            if (catalog == null || catalog.Categories.Length == 0)
            {
                Debug.LogError("SortThem: generate cars first");
                return;
            }
            EditorAssets.EnsureFolder(Paths.Scenes);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = Camera.main;
            if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);
            var light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity = 1.2f;
                light.shadows = LightShadows.None;
            }
            catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(Paths.Catalog);
            _roomLayout = RoomLayoutCapture.Load();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Paths.InputAsset);
            if (inputAsset == null) inputAsset = InputSetup.Create();
            if (inputAsset == null)
            {
                AssetDatabase.ImportAsset(Paths.InputAsset, ImportAssetOptions.ForceUpdate);
                inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Paths.InputAsset);
            }

            CreateMaterials();
            var shelfData = EditorAssets.LoadOrCreate<ShelfData>(Paths.Data + "/Shelf_Standard.asset");
            RackW = shelfData.Columns * shelfData.SlotPitch + 0.2f;
            RackD = shelfData.Rows * shelfData.RowPitch + 0.1f;
            EditorUtility.SetDirty(shelfData);

            var gameConfig = EditorAssets.LoadOrCreate<GameConfig>(Paths.Config + "/GameConfig.asset");
            gameConfig.LevelHalfExtents = new Vector3(Max + 1f, RoomH, Max + 1f);
            gameConfig.UnstuckCenter = SpecialRackPos + new Vector3(0f, 2.5f, 2.5f);
            EditorUtility.SetDirty(gameConfig);
            var economy = EditorAssets.LoadOrCreate<EconomyConfig>(Paths.Config + "/EconomyConfig.asset");
            var upgrades = UpgradeSetup.CreateAll(false);

            var placements = ComputeRacks(catalog);
            BuildRoom(placements);
            var shelves = BuildRacks(catalog, shelfData, placements);
            if (catalog.SpecialCategory != null && catalog.Specials != null && catalog.Specials.Length > 0) BuildSpecialRack(catalog);
            var terminal = BuildArcadeCabinet("UpgradeTerminal", new Vector3(ArcadeX, 0f, SouthZ + 1.75f), 0f);
            terminal.AddComponent<UpgradeTerminal>();

            BuildSlotMachine();
            BuildCashRegister();
            BuildTutorial();

            var cabinet = Block("Cabinet", null, new Vector3(ShopX, 0.4f, SouthZ + 4.2f), new Vector3(0.7f, 0.8f, 0.5f), _cabinet);
            cabinet.transform.rotation = ShopRot;
            var radio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radio.name = "Radio";
            radio.transform.position = new Vector3(ShopX, 0.8f + 0.13f, SouthZ + 4.2f);
            radio.transform.rotation = ShopRot;
            radio.transform.localScale = new Vector3(0.44f, 0.26f, 0.18f);
            radio.GetComponent<Renderer>().sharedMaterial = _radio;
            radio.AddComponent<Radio>();
            var grille = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grille.name = "Grille";
            grille.transform.SetParent(radio.transform, false);
            grille.transform.localPosition = new Vector3(-0.18f, 0f, 0.52f);
            grille.transform.localScale = new Vector3(0.5f, 0.7f, 0.06f);
            grille.GetComponent<Renderer>().sharedMaterial = _terminal;
            Object.DestroyImmediate(grille.GetComponent<Collider>());
            var dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dial.name = "Dial";
            dial.transform.SetParent(radio.transform, false);
            dial.transform.localPosition = new Vector3(0.25f, 0f, 0.52f);
            dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dial.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            dial.GetComponent<Renderer>().sharedMaterial = _marker;
            Object.DestroyImmediate(dial.GetComponent<Collider>());
            var antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "Antenna";
            antenna.transform.SetParent(radio.transform, false);
            antenna.transform.localPosition = new Vector3(0.35f, 1.1f, 0f);
            antenna.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            antenna.transform.localScale = new Vector3(0.04f, 0.7f, 0.1f);
            antenna.GetComponent<Renderer>().sharedMaterial = _terminal;
            Object.DestroyImmediate(antenna.GetComponent<Collider>());

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.Config = gameConfig;
            gm.EconomyConfig = economy;
            gm.Catalog = catalog;
            gm.UpgradeAssets = upgrades;
            gm.InputAsset = inputAsset;
            var carsRoot = new GameObject("Cars").transform;
            gm.CarsRoot = carsRoot;
            var scatterer = gmGo.AddComponent<Scatterer>();
            var source = new GameObject("ScatterSource").transform;
            source.SetParent(gmGo.transform, false);
            source.position = FountainPos;
            scatterer.Source = source;
            scatterer.MinSpeed = 5f;
            scatterer.MaxSpeed = 9f;
            gm.Scatterer = scatterer;
            var layout = AssetDatabase.LoadAssetAtPath<LevelLayoutData>(Paths.Layout);
            gm.Layout = layout;

            var player = BuildPlayer(gm);
            gm.Player = player.GetComponent<PlayerController>();
            gm.Inventory = player.GetComponent<Inventory>();

            var uiGo = new GameObject("UI");
            var canvas = uiGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = uiGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 1f;
            uiGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ui = uiGo.AddComponent<UiRoot>();
            ui.Abilities = player.GetComponent<PlayerAbilities>();
            ui.AbilityIcons = new[] { UiSpriteSetup.Load(UiSpriteSetup.AbilityIcons[0]), UiSpriteSetup.Load(UiSpriteSetup.AbilityIcons[1]), UiSpriteSetup.Load(UiSpriteSetup.AbilityIcons[2]) };
            ui.SlotFrame = UiSpriteSetup.Load(UiSpriteSetup.SlotFrame);
            ui.KeyFrame = UiSpriteSetup.Load(UiSpriteSetup.KeyFrame);
            ui.TouchIcons = System.Array.ConvertAll(UiSpriteSetup.TouchIcons, UiSpriteSetup.Load);
            ui.Circle = UiSpriteSetup.Load(UiSpriteSetup.Circle);
            ui.BombIcon = UiSpriteSetup.Load(UiSpriteSetup.BombIcon);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            EditorSceneManager.SaveScene(scene, Paths.MainScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Paths.MainScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("SortThem: level built, shelves=" + shelves.Count);
        }

        [MenuItem("SortThem/4b. Add Slot Machine")]
        public static void AddSlotMachine()
        {
            CreateMaterials();
            BuildSlotMachine();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("SortThem/4c. Add Cash Register")]
        public static void AddCashRegister()
        {
            CreateMaterials();
            BuildCashRegister();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("SortThem/4e. Add Special Rack")]
        public static void AddSpecialRack()
        {
            CreateMaterials();
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(Paths.Catalog);
            _roomLayout = RoomLayoutCapture.Load();
            if (catalog == null || catalog.SpecialCategory == null)
            {
                Debug.LogError("SortThem: import special cars first (menu 3h)");
                return;
            }
            BuildSpecialRack(catalog);
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.UpgradeAssets = UpgradeSetup.CreateAll(false);
                EditorUtility.SetDirty(gm);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        static void BuildSpecialRack(CarCatalog catalog)
        {
            const float tableRadius = 1.1f, tableHeight = 1.2f, slotRadius = 0.55f;
            var cat = catalog.SpecialCategory;
            var shelfData = EditorAssets.LoadOrCreate<ShelfData>(Paths.Data + "/Shelf_Special.asset");
            shelfData.Rows = 1;
            shelfData.Columns = 1;
            shelfData.SlotYaw = 0f;
            shelfData.Locked = true;
            shelfData.DisplayScale = SpecialDisplayScale;
            EditorUtility.SetDirty(shelfData);
            EditorAssets.EnsureFolder(Paths.Racks);
            var rackData = EditorAssets.LoadOrCreate<RackData>(Paths.Racks + "/Rack_" + cat.CategoryID + ".asset");
            rackData.Category = cat;
            rackData.ShelfCount = catalog.Specials.Length;
            rackData.Shelf = shelfData;
            EditorUtility.SetDirty(rackData);

            var existing = GameObject.Find("Rack_" + cat.CategoryID);
            if (existing != null) Object.DestroyImmediate(existing);
            int shelfId = 0;
            foreach (var s in Object.FindObjectsByType<ShelfController>(FindObjectsSortMode.None)) shelfId = Mathf.Max(shelfId, s.ShelfId + 1);

            var racksRoot = GameObject.Find("Racks");
            var rackGo = new GameObject("Rack_" + cat.CategoryID);
            if (racksRoot != null) rackGo.transform.SetParent(racksRoot.transform, false);
            var podiumPos = SpecialRackPos;
            float podiumYaw = 0f;
            if (_roomLayout != null && _roomLayout.TryGetRack(cat.CategoryID, out var savedPodium, out var savedPodiumYaw)) { podiumPos = savedPodium; podiumYaw = savedPodiumYaw; }
            rackGo.transform.SetPositionAndRotation(podiumPos, Quaternion.Euler(0f, podiumYaw, 0f));
            var rack = rackGo.AddComponent<RackController>();
            rack.Category = cat;

            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "Table";
            table.transform.SetParent(rackGo.transform, false);
            table.transform.localPosition = new Vector3(0f, tableHeight * 0.5f, 0f);
            table.transform.localScale = new Vector3(tableRadius * 2f, tableHeight * 0.5f, tableRadius * 2f);
            table.GetComponent<Renderer>().sharedMaterial = _podium;
            table.isStatic = true;
            Object.DestroyImmediate(table.GetComponent<Collider>());
            var tableCollider = table.AddComponent<MeshCollider>();
            tableCollider.sharedMesh = table.GetComponent<MeshFilter>().sharedMesh;
            tableCollider.convex = true;
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(rackGo.transform, false);
            post.transform.localPosition = new Vector3(0f, tableHeight + 0.6f, 0f);
            post.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
            post.GetComponent<Renderer>().sharedMaterial = _rack;
            Object.DestroyImmediate(post.GetComponent<Collider>());
            var sign = Block("Sign", rackGo.transform, new Vector3(0f, tableHeight + 1.45f, 0f), new Vector3(1.4f, 0.5f, 0.04f), EditorAssets.Unlit("Sign_" + cat.CategoryID, cat.CategoryColor));
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            sign.isStatic = false;
            rack.SignPlate = sign.GetComponent<Renderer>();
            rack.SignText = Text3D(rackGo.transform, "SignText", cat.DevName, 2.2f, new Vector3(0f, tableHeight + 1.45f, -0.03f), Quaternion.identity, new Vector2(1.3f, 0.45f), Color.white);
            var backText = Text3D(rackGo.transform, "SignTextBack", cat.DevName, 2.2f, new Vector3(0f, tableHeight + 1.45f, 0.03f), Quaternion.Euler(0f, 180f, 0f), new Vector2(1.3f, 0.45f), Color.white);
            backText.text = cat.DevName;

            var rackZoneGo = new GameObject("RackZone");
            rackZoneGo.transform.SetParent(rackGo.transform, false);
            rackZoneGo.transform.localPosition = new Vector3(0f, tableHeight * 0.5f + 0.3f, 0f);
            var rackZone = rackZoneGo.AddComponent<BoxCollider>();
            rackZone.isTrigger = true;
            rackZone.size = new Vector3(tableRadius * 2f + 0.4f, tableHeight + 0.8f, tableRadius * 2f + 0.4f);
            rackZoneGo.AddComponent<RackZone>().Rack = rack;
            rack.Zone = rackZone;
            rack.HighlightFrame = BuildFrame(rackGo.transform, new Vector3(0f, tableHeight * 0.5f + 0.25f, 0f), new Vector3(tableRadius * 2f + 0.2f, tableHeight + 0.7f, tableRadius * 2f + 0.2f));

            int count = Mathf.Max(1, catalog.Specials.Length);
            var shelves = new ShelfController[count];
            for (int i = 0; i < count; i++)
            {
                float yaw = 180f + i * 360f / count;
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var shelfGo = new GameObject("Shelf_" + i);
                shelfGo.transform.SetParent(rackGo.transform, false);
                shelfGo.transform.localPosition = dir * slotRadius + Vector3.up * tableHeight;
                shelfGo.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                var shelf = shelfGo.AddComponent<ShelfController>();
                shelf.ShelfId = shelfId++;
                shelf.Data = shelfData;
                shelf.Rack = rack;

                var zoneGo = new GameObject("Zone");
                zoneGo.transform.SetParent(shelfGo.transform, false);
                zoneGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                var zone = zoneGo.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.size = new Vector3(0.5f, 0.4f, 0.55f);
                zoneGo.AddComponent<ShelfZone>().Shelf = shelf;
                shelf.Zone = zone;

                var pt = new GameObject("Slot_0").transform;
                pt.SetParent(shelfGo.transform, false);
                pt.localPosition = Vector3.zero;
                pt.localRotation = Quaternion.identity;
                shelf.SlotPoints = new[] { pt };

                shelf.Tag = BuildPriceTag(shelf, shelfGo.transform);
                shelf.Tag.transform.localPosition = new Vector3(0f, -0.02f, tableRadius - slotRadius + 0.045f);
                shelf.Tag.Root.transform.localScale = new Vector3(0.45f, 0.08f, 0.01f);
                foreach (var txt in new[] { shelf.Tag.NameText, shelf.Tag.PriceText })
                    txt.transform.localScale = new Vector3(1f / 0.5f * (0.8f / 0.45f), 1f / 0.05f, 1f / 0.01f);
                shelves[i] = shelf;
            }
            rack.Shelves = shelves;
        }

        [MenuItem("SortThem/4d. Add Tutorial")]
        public static void AddTutorial()
        {
            CreateMaterials();
            BuildTutorial();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        static void BuildTutorial()
        {
            var existing = GameObject.Find("Tutorial");
            if (existing != null) Object.DestroyImmediate(existing);
            var go = new GameObject("Tutorial");
            var tutorial = go.AddComponent<Tutorial>();
            var outline = Ghost("TutorialOutline", _tutOutline);
            outline.transform.SetParent(go.transform, false);
            tutorial.Outline = outline;
        }

        static void BuildCashRegister()
        {
            var existing = GameObject.Find("CashDesk");
            if (existing != null) Object.DestroyImmediate(existing);
            var desk = Block("CashDesk", null, new Vector3(Max - 0.45f, 0.45f, SouthZ - 2.6f), new Vector3(0.6f, 0.9f, 1.4f), _cabinet);
            var register = GameObject.CreatePrimitive(PrimitiveType.Cube);
            register.name = "CashRegister";
            register.transform.SetParent(desk.transform, false);
            register.transform.localPosition = new Vector3(-0.05f, 0.5f + 0.19f, 0f);
            register.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            register.transform.localScale = new Vector3(0.45f / 1.4f, 0.34f / 0.9f, 0.36f / 0.6f);
            register.GetComponent<Renderer>().sharedMaterial = _terminal;
            register.AddComponent<CashRegister>();
            var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = "Display";
            display.transform.SetParent(register.transform, false);
            display.transform.localPosition = new Vector3(0f, 0.25f, 0.52f);
            display.transform.localScale = new Vector3(0.7f, 0.3f, 0.05f);
            display.GetComponent<Renderer>().sharedMaterial = _marker;
            Object.DestroyImmediate(display.GetComponent<Collider>());
            var keys = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keys.name = "Keys";
            keys.transform.SetParent(register.transform, false);
            keys.transform.localPosition = new Vector3(0f, -0.2f, 0.52f);
            keys.transform.localScale = new Vector3(0.8f, 0.4f, 0.05f);
            keys.GetComponent<Renderer>().sharedMaterial = _plateWhite;
            Object.DestroyImmediate(keys.GetComponent<Collider>());
        }

        static void BuildSlotMachine()
        {
            var existing = GameObject.Find("SlotMachine");
            if (existing != null) Object.DestroyImmediate(existing);

            var floorPos = new Vector3(ArcadeX, 0f, SouthZ + 0.35f);
            var root = new GameObject("SlotMachine");
            root.transform.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, SlotYaw, 0f));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotMachinePrefab);
            if (prefab == null)
            {
                Debug.LogError("SortThem: slot machine model not found at " + SlotMachinePrefab);
                Panel("Stand", root.transform, new Vector3(0f, SlotStandH * 0.5f, 0f), new Vector3(SlotStandW, SlotStandH, SlotStandD), _rack, SlabTile);
                root.AddComponent<SlotMachine>();
                return;
            }

            var machine = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            machine.name = "Machine";
            machine.transform.SetParent(root.transform, false);
            machine.transform.localRotation = Quaternion.identity;
            machine.transform.localPosition = Vector3.zero;

            var body = machine.transform.Find("SlotMachine_Body");
            var bodyRenderer = body != null ? body.GetComponent<MeshRenderer>() : null;
            var local = LocalBoundsIn(root.transform, bodyRenderer != null ? bodyRenderer.gameObject : machine);
            machine.transform.localScale *= SlotStandW / local.size.x;

            local = LocalBoundsIn(root.transform, bodyRenderer != null ? bodyRenderer.gameObject : machine);
            float standW = local.size.x;
            float standD = local.size.z + 0.04f;

            Panel("Stand", root.transform, new Vector3(local.center.x, SlotStandH * 0.5f, local.center.z), new Vector3(standW, SlotStandH, standD), _rack, SlabTile);
            Panel("StandTop", root.transform, new Vector3(local.center.x, SlotStandH + BoardT * 0.5f, local.center.z),
                new Vector3(standW + CapOverhang * 2f, BoardT, standD + CapOverhang * 2f), _board, SlabTile);

            var all = LocalBoundsIn(root.transform, machine);
            machine.transform.localPosition += new Vector3(0f, SlotStandH + BoardT - all.min.y, 0f);

            foreach (var t in machine.GetComponentsInChildren<Transform>())
                t.gameObject.isStatic = !(t.name.Contains("Reel") || t.name.Contains("Lever"));

            root.AddComponent<SlotMachine>();
        }

        static Bounds LocalBoundsIn(Transform space, GameObject go)
        {
            var toLocal = space.worldToLocalMatrix;
            var b = new Bounds();
            bool first = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = toLocal * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var pt = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(pt, Vector3.zero); first = false; }
                    else b.Encapsulate(pt);
                }
            }
            return b;
        }

        static void CreateMaterials()
        {
            string tex = RoomTextures.Folder;
            _woodBeam = EditorAssets.Textured("Room_WoodBeam", tex + "/Wood_Beam.png", Color.white, 0.18f);
            _woodPanel = EditorAssets.Textured("Room_WoodPanel", tex + "/Wood_Panel.png", Color.white, 0.15f);
            _woodPanelV = EditorAssets.Textured("Room_WoodPanelV", tex + "/Wood_PlanksV.png", Color.white, 0.15f);
            _woodFloor = EditorAssets.Textured("Room_WoodFloor", tex + "/Wood_Floor.png", Color.white, 0.22f);
            _plaster = EditorAssets.Textured("Room_Plaster", tex + "/Plaster_Blue.png", Color.white, 0.05f);
            _ceilingPlaster = EditorAssets.Textured("Room_Ceiling", tex + "/Plaster_Ceiling.png", Color.white, 0.05f);
            _glass = EditorAssets.Textured("Room_Window", tex + "/Window_Frost.png", Color.white, 0f, true);
            _sky = EditorAssets.Textured("Room_WindowSky", tex + "/Window_Sky.png", Color.white, 0f, true);
            _rug = EditorAssets.Textured("Room_Rug", tex + "/Rug_Check.png", Color.white, 0.04f);
            _lampGlow = EditorAssets.Unlit("Room_LampGlow", new Color(1f, 0.93f, 0.75f));
            _floor = EditorAssets.Lit("Floor", new Color(0.42f, 0.42f, 0.45f));
            _wall = EditorAssets.Lit("Wall", new Color(0.78f, 0.74f, 0.66f));
            _ceiling = EditorAssets.Lit("Ceiling", new Color(0.85f, 0.85f, 0.85f));
            _rack = EditorAssets.Textured("Rack", tex + "/Wood_Slab.png", Color.white, 0.16f);
            _board = EditorAssets.Textured("ShelfBoard", tex + "/Wood_Slab.png", new Color(1.05f, 1.02f, 0.98f), 0.18f);
            _rackBack = EditorAssets.Textured("RackBack", tex + "/Wood_PlanksV.png", Color.white, 0.12f);
            _podium = EditorAssets.Textured("Podium", tex + "/Wood_Panel.png", new Color(0.82f, 0.78f, 0.74f), 0.2f);
            _terminal = EditorAssets.Lit("Terminal", new Color(0.15f, 0.15f, 0.18f));
            _cabinet = EditorAssets.Lit("Cabinet", new Color(0.42f, 0.28f, 0.16f));
            _radio = EditorAssets.Lit("Radio", new Color(0.75f, 0.55f, 0.3f));
            _plateWhite = EditorAssets.Unlit("PlateWhite", Color.white);
            _plateRed = EditorAssets.Unlit("PlateRed", new Color(0.85f, 0.12f, 0.12f));
            _plateGold = EditorAssets.Unlit("PlateGold", new Color(1f, 0.78f, 0.2f));
            _marker = EditorAssets.Unlit("HighlightMarker", new Color(0.2f, 1f, 0.35f));
            _ghost = EditorAssets.LoadOrCreateMaterial("Ghost", "SortThem/Ghost", new Color(0.2f, 1f, 0.3f, 0.55f), "_Color");
            _outline = EditorAssets.LoadOrCreateMaterial("Outline", "SortThem/Outline", Color.white, "_Color");
            _highlight = EditorAssets.LoadOrCreateMaterial("HighlightThroughWalls", "SortThem/HighlightThroughWalls", new Color(1f, 0.85f, 0.2f, 0.85f), "_Color");
            _levOutline = EditorAssets.LoadOrCreateMaterial("OutlinePurple", "SortThem/OutlineXRay", new Color(0.72f, 0.3f, 1f), "_Color");
            _levOutline.shader = Shader.Find("SortThem/OutlineXRay");
            _levOutline.SetFloat("_Width", 7f);
            _levOutline.renderQueue = 3000;
            EditorUtility.SetDirty(_levOutline);
            _tutOutline = EditorAssets.LoadOrCreateMaterial("OutlineYellow", "SortThem/OutlineXRay", new Color(1f, 0.85f, 0.2f), "_Color");
            _tutOutline.shader = Shader.Find("SortThem/OutlineXRay");
            _tutOutline.SetFloat("_Width", 7f);
            _tutOutline.renderQueue = 3000;
            EditorUtility.SetDirty(_tutOutline);
            _heldCars = EditorAssets.LoadOrCreateMaterial("CarsHeld", "SortThem/VertexColorLitOverlay", Color.white);
        }

        const float SlabTile = 1.0f;
        const float PairGap = 0.12f;
        const float CapOverhang = 0.02f;
        const float GlassTile = 3.0f;
        static RoomLayoutData _roomLayout;

        static GameObject Panel(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float metersPerTile)
        {
            var go = RoomMesh.Box(name, parent, center, size, mat, metersPerTile);
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        static GameObject Block(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool isStatic = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = isStatic;
            return go;
        }

        static readonly Quaternion ShopRot = Quaternion.Euler(0f, -90f, 0f);

        const float SocleH = 1.10f, WinBottom = 2.40f, WinTop = 3.90f, CorniceH = 0.12f, SillH = 0.10f;
        const float PanoBottom = 0.90f, Module = 2.0f;
        const float TrimDepth = 0.06f, MullionW = 0.05f;
        const float BeamH = 0.30f, BeamT = 0.22f;
        const float PostW = BeamT, PostDepth = BeamH;

        struct WallRun
        {
            public Vector3 Start;
            public Vector3 Along;
            public Vector3 In;
            public float Length;
            public int PanoramaFrom;
            public WallRun(Vector3 start, Vector3 along, Vector3 inward, float length, int panoramaFrom = -1)
            {
                Start = start; Along = along; In = inward; Length = length; PanoramaFrom = panoramaFrom;
            }
        }

        static void BuildRoom(List<RackPlacement> racks)
        {
            var room = new GameObject("Room").transform;
            float armLen = Max - Inner;

            float slabW = WingW + 2f;
            float westEdge = WestX + slabW * 0.5f;
            float southW = Max + 1f - westEdge;
            float southCx = westEdge + southW * 0.5f;
            BuildSlab("Floor_W", room, new Vector3(WestX, -0.1f, 0f), new Vector3(slabW, 0.2f, ArmLen + 2f), _woodFloor, 1.2f);
            BuildSlab("Floor_S", room, new Vector3(southCx, -0.1f, SouthZ), new Vector3(southW, 0.2f, slabW), _woodFloor, 1.2f);
            BuildSlab("Ceiling_W", room, new Vector3(WestX, RoomH + 0.15f, 0f), new Vector3(slabW, 0.3f, ArmLen + 2f), _ceilingPlaster, 2.5f);
            BuildSlab("Ceiling_S", room, new Vector3(southCx, RoomH + 0.15f, SouthZ), new Vector3(southW, 0.3f, slabW), _ceilingPlaster, 2.5f);

            BuildWallCollider("Wall_W", room, new Vector3(Min - WallT * 0.5f, RoomH * 0.5f, 0f), new Vector3(WallT, RoomH, ArmLen + WallT * 2f));
            BuildWallCollider("Wall_N", room, new Vector3(WestX, RoomH * 0.5f, Max + WallT * 0.5f), new Vector3(WingW + WallT * 2f, RoomH, WallT));
            BuildWallCollider("Wall_IE", room, new Vector3(Inner + WallT * 0.5f, RoomH * 0.5f, (Inner + Max) * 0.5f + WallT * 0.5f), new Vector3(WallT, RoomH, armLen + WallT));
            BuildWallCollider("Wall_IN", room, new Vector3((Inner + Max) * 0.5f + WallT * 0.5f, RoomH * 0.5f, Inner + WallT * 0.5f), new Vector3(armLen + WallT, RoomH, WallT));
            BuildWallCollider("Wall_E", room, new Vector3(Max + WallT * 0.5f, RoomH * 0.5f, SouthZ), new Vector3(WallT, RoomH, WingW + WallT * 2f));
            BuildWallCollider("Wall_S", room, new Vector3(0f, RoomH * 0.5f, Min - WallT * 0.5f), new Vector3(ArmLen + WallT * 2f, RoomH, WallT));

            var runs = new[]
            {
                new WallRun(new Vector3(Min, 0f, Min), Vector3.forward, Vector3.right, ArmLen),
                new WallRun(new Vector3(Min, 0f, Max), Vector3.right, Vector3.back, WingW, 0),
                new WallRun(new Vector3(Inner, 0f, Max), Vector3.back, Vector3.left, armLen),
                new WallRun(new Vector3(Inner, 0f, Inner), Vector3.right, Vector3.back, armLen, 4),
                new WallRun(new Vector3(Max, 0f, Inner), Vector3.back, Vector3.left, WingW),
                new WallRun(new Vector3(Max, 0f, Min), Vector3.left, Vector3.forward, ArmLen)
            };
            var walls = new GameObject("Walls").transform;
            walls.SetParent(room, false);
            for (int i = 0; i < runs.Length; i++) BuildWallRun(walls, runs[i], i, racks);

            BuildCeilingBeams(room);
            BuildRugs(room);
        }

        static void BuildSlab(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float metersPerTile)
        {
            var go = RoomMesh.Box(name, parent, center, size, mat, metersPerTile);
            go.AddComponent<BoxCollider>().size = size;
        }

        static void BuildWallCollider(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
        }

        static void BuildWallRun(Transform parent, WallRun run, int index, List<RackPlacement> racks)
        {
            var root = new GameObject("Run_" + index).transform;
            root.SetParent(parent, false);

            var spans = RacksOnRun(run, racks);
            var posts = new List<float> { 0f };
            for (int i = 0; i + 1 < spans.Count; i++) posts.Add((spans[i].end + spans[i + 1].start) * 0.5f);
            float panoStart = run.PanoramaFrom >= 0 ? run.PanoramaFrom * Module : -1f;
            if (panoStart > 0.5f && panoStart < run.Length - 0.5f) posts.Add(panoStart);
            posts.Sort();

            var bounds = new List<float>(posts) { run.Length };
            for (int i = 0; i + 1 < bounds.Count; i++)
            {
                float a = bounds[i] + PostW * 0.5f;
                float b = bounds[i + 1] - (i + 2 < bounds.Count ? PostW * 0.5f : 0f);
                float len = b - a;
                if (len < 0.1f) continue;
                float centerT = (a + b) * 0.5f;
                var mid = run.Start + run.Along * centerT;
                bool pano = panoStart >= 0f && centerT >= panoStart;
                bool behindRack = IsBehindRack(spans, a, b);
                float bottom = pano ? PanoBottom : WinBottom;

                if (behindRack)
                    Span(root, "WallLow_" + i, mid, run, len, 0f, bottom - SillH, _plaster, 2.5f, 0.02f);
                else
                {
                    float socleTop = Mathf.Min(SocleH, bottom - SillH);
                    Span(root, "Socle_" + i, mid, run, len, 0f, socleTop, _woodPanelV, 0.9f, TrimDepth, false);
                    Span(root, "Plaster_" + i, mid, run, len, socleTop, bottom - SillH, _plaster, 2.5f, 0.02f);
                }

                Span(root, "Sill_" + i, mid, run, len, bottom - SillH, bottom, _woodBeam, 1f, TrimDepth * 1.6f);
                float rowMid = (bottom + WinTop) * 0.5f;
                if (pano)
                {
                    GlassSpan(root, "GlassLow_" + i, mid, run, len, bottom, rowMid, _glass);
                    GlassSpan(root, "GlassHigh_" + i, mid, run, len, rowMid, WinTop, _sky);
                }
                else GlassSpan(root, "Glass_" + i, mid, run, len, bottom, WinTop, _sky);

                int panes = Mathf.Max(2, Mathf.RoundToInt(len / Module));
                for (int k = 1; k < panes; k++)
                {
                    float t = a + len * k / panes;
                    AddBar(root, "Mullion_" + i + "_" + k, run.Start + run.Along * t, run, MullionW, bottom, WinTop, _woodBeam);
                }
                float midY = (bottom + WinTop) * 0.5f;
                Span(root, "Transom_" + i, mid, run, len, midY - MullionW * 0.5f, midY + MullionW * 0.5f, _woodBeam, 1f, TrimDepth * 0.9f);

                Span(root, "Cornice_" + i, mid, run, len, WinTop, WinTop + CorniceH, _woodBeam, 1f, TrimDepth * 1.6f);
                Span(root, "Upper_" + i, mid, run, len, WinTop + CorniceH, RoomH - 0.30f, _plaster, 2.5f, 0.02f);
                Span(root, "Header_" + i, mid, run, len, RoomH - 0.30f, RoomH, _woodBeam, 1f, TrimDepth * 1.4f);
            }

            for (int i = 0; i < posts.Count; i++)
                AddPost(root, "Post_" + i, run.Start + run.Along * posts[i], run);
        }

        static List<(float start, float end)> RacksOnRun(WallRun run, List<RackPlacement> racks)
        {
            var spans = new List<(float start, float end)>();
            if (racks == null) return spans;
            float half = RackTotalW * 0.5f + 0.05f;
            foreach (var r in racks)
            {
                var v = r.Pos - run.Start;
                float depth = Vector3.Dot(v, run.In);
                if (depth < 0.05f || depth > RackD * 0.9f) continue;
                var facing = Quaternion.Euler(0f, r.Yaw, 0f) * Vector3.forward;
                if (Vector3.Dot(facing, run.In) < 0.7f) continue;
                float t = Vector3.Dot(v, run.Along);
                float s0 = Mathf.Clamp(t - half, 0f, run.Length);
                float s1 = Mathf.Clamp(t + half, 0f, run.Length);
                if (s1 - s0 > 0.2f) spans.Add((s0, s1));
            }
            spans.Sort((x, y) => x.start.CompareTo(y.start));
            return spans;
        }

        static bool IsBehindRack(List<(float start, float end)> spans, float a, float b)
        {
            float covered = 0f;
            foreach (var s in spans) covered += Mathf.Max(0f, Mathf.Min(b, s.end) - Mathf.Max(a, s.start));
            return covered > (b - a) * 0.5f;
        }

        static void AddPost(Transform parent, string name, Vector3 at, WallRun run)
        {
            var along = new Vector3(Mathf.Abs(run.Along.x), 0f, Mathf.Abs(run.Along.z));
            var thick = new Vector3(Mathf.Abs(run.In.x), 0f, Mathf.Abs(run.In.z));
            var size = along * PostW + thick * PostDepth + Vector3.up * RoomH;
            var center = at + Vector3.up * (RoomH * 0.5f) + run.In * (PostDepth * 0.5f);
            RoomMesh.Box(name, parent, center, size, _woodBeam, 1f);
        }

        static void GlassSpan(Transform parent, string name, Vector3 mid, WallRun run, float width, float y0, float y1, Material mat)
        {
            float h = y1 - y0;
            if (h <= 0.001f || width <= 0.001f) return;
            var along = new Vector3(Mathf.Abs(run.Along.x), 0f, Mathf.Abs(run.Along.z));
            var thick = new Vector3(Mathf.Abs(run.In.x), 0f, Mathf.Abs(run.In.z));
            var size = along * width + thick * 0.012f + Vector3.up * h;
            var center = mid + Vector3.up * ((y0 + y1) * 0.5f) + run.In * 0.006f;
            RoomMesh.Box(name, parent, center, size, mat, new Vector2(GlassTile, h), false);
        }

        static void Span(Transform parent, string name, Vector3 mid, WallRun run, float width, float y0, float y1, Material mat, float metersPerTile, float depth, bool grainAlongLongest = true)
        {
            if (y1 - y0 <= 0.001f || width <= 0.001f) return;
            var along = new Vector3(Mathf.Abs(run.Along.x), 0f, Mathf.Abs(run.Along.z));
            var thick = new Vector3(Mathf.Abs(run.In.x), 0f, Mathf.Abs(run.In.z));
            var size = along * width + thick * depth + Vector3.up * (y1 - y0);
            var center = mid + Vector3.up * ((y0 + y1) * 0.5f) + run.In * (depth * 0.5f);
            RoomMesh.Box(name, parent, center, size, mat, metersPerTile, grainAlongLongest);
        }

        static void AddBar(Transform parent, string name, Vector3 at, WallRun run, float width, float y0, float y1, Material mat)
        {
            var along = new Vector3(Mathf.Abs(run.Along.x), 0f, Mathf.Abs(run.Along.z));
            var thick = new Vector3(Mathf.Abs(run.In.x), 0f, Mathf.Abs(run.In.z));
            float depth = TrimDepth * 1.8f;
            var size = along * width + thick * depth + Vector3.up * (y1 - y0);
            var center = at + Vector3.up * ((y0 + y1) * 0.5f) + run.In * (depth * 0.5f);
            RoomMesh.Box(name, parent, center, size, mat, 1f);
        }

        static void BuildCeilingBeams(Transform room)
        {
            var beams = new GameObject("Beams").transform;
            beams.SetParent(room, false);
            float y = RoomH - 0.18f;
            var beamSize = new Vector3(WingW, BeamH, BeamT);
            int n = Mathf.RoundToInt(ArmLen / Module);
            for (int i = 0; i <= n; i++)
            {
                float z = Min + i * (ArmLen / n);
                RoomMesh.Box("Beam_W_" + i, beams, new Vector3(WestX, y, z), beamSize, _woodBeam, 1f);
                if (i % 2 == 0) AddLamps(beams, new Vector3(WestX, y, z), Vector3.right, WingW, "W" + i);
            }
            float armLen = Max - Inner;
            int m = Mathf.RoundToInt(armLen / Module);
            var beamSizeS = new Vector3(BeamT, BeamH, WingW);
            for (int i = 0; i <= m; i++)
            {
                float x = Inner + i * (armLen / m);
                RoomMesh.Box("Beam_S_" + i, beams, new Vector3(x, y, SouthZ), beamSizeS, _woodBeam, 1f);
                if (i % 2 == 0) AddLamps(beams, new Vector3(x, y, SouthZ), Vector3.forward, WingW, "S" + i);
            }
        }

        static void AddLamps(Transform parent, Vector3 beamCenter, Vector3 along, float span, string tag)
        {
            for (int k = -1; k <= 1; k += 2)
            {
                var at = beamCenter + along * (span * 0.25f * k) + Vector3.down * 0.2f;
                RoomMesh.Box("LampBody_" + tag + "_" + k, parent, at, new Vector3(0.12f, 0.14f, 0.12f), _woodBeam, 1f);
                RoomMesh.Box("LampGlow_" + tag + "_" + k, parent, at + Vector3.down * 0.09f, new Vector3(0.14f, 0.03f, 0.14f), _lampGlow, 1f);
            }
        }

        const string ArcadePrefab = "Assets/Arcade machine/fbx.fbx";
        const float ArcadeHeight = 1.75f;
        const string SlotMachinePrefab = "Assets/_Game/Prefabs/SlotMachine.prefab";
        const float SlotStandH = 0.9f, SlotStandW = 0.62f, SlotStandD = 0.52f;
        const float SlotMachineH = 0.62f;
        const float SlotYaw = 270f;
        const float ArcadeX = Max - 0.62f;

        static GameObject BuildArcadeCabinet(string name, Vector3 floorPos, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArcadePrefab);
            if (prefab == null)
            {
                Debug.LogError("SortThem: arcade cabinet model not found at " + ArcadePrefab);
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = name;
                fallback.transform.SetPositionAndRotation(floorPos + Vector3.up * 0.8f, Quaternion.Euler(0f, yaw, 0f));
                fallback.transform.localScale = new Vector3(0.9f, 1.6f, 0.5f);
                fallback.GetComponent<Renderer>().sharedMaterial = _terminal;
                return fallback;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
            go.transform.position = floorPos;

            var world = WorldBounds(go);
            float scale = ArcadeHeight / world.size.y;
            go.transform.localScale *= scale;
            world = WorldBounds(go);
            go.transform.position += new Vector3(floorPos.x - world.center.x, floorPos.y - world.min.y, floorPos.z - world.center.z);

            var local = LocalBounds(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = local.size;
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return go;
        }

        static Bounds LocalBounds(GameObject go)
        {
            var toLocal = go.transform.worldToLocalMatrix;
            var b = new Bounds();
            bool first = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = toLocal * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var p = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var b = new Bounds();
            bool first = true;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static void BuildRugs(Transform room)
        {
            var rugs = new GameObject("Rugs").transform;
            rugs.SetParent(room, false);
            if (_roomLayout != null && _roomLayout.Rugs != null && _roomLayout.Rugs.Length > 0)
            {
                foreach (var e in _roomLayout.Rugs)
                {
                    var size = new Vector3(e.Size.x, Mathf.Max(0.01f, e.Size.y), e.Size.z);
                    RoomMesh.Box(e.Name, rugs, e.Position, size, _rug, 2f);
                }
                return;
            }
            RoomMesh.Box("Rug_Shop", rugs, new Vector3(Max - 2.6f, 0.005f, SouthZ), new Vector3(3.6f, 0.01f, 6.0f), _rug, 2f);
            RoomMesh.Box("Rug_WestAisle", rugs, new Vector3(WestX, 0.005f, Max - 4.5f), new Vector3(4.4f, 0.01f, 5.0f), _rug, 2f);
        }

        struct RackPlacement
        {
            public CategoryData Cat;
            public Vector3 Pos;
            public float Yaw;
        }

        static List<RackPlacement> ComputeRacks(CarCatalog catalog)
        {
            var slots = new List<(Vector3 pos, float yaw)>();
            var categories = new List<CategoryData>();
            foreach (var c in catalog.Categories) if (c != null && !catalog.IsSpecial(c)) categories.Add(c);
            int need = categories.Count;
            float wallOff = RackD * 0.5f + 0.05f;
            float pairOff = (RackD + PairGap) * 0.5f;
            float c1 = Max - 0.5f - RackTotalW * 0.5f, c3 = Min + 1.2f + RackTotalW * 0.5f;
            float[] wallCenters = { c1, (c1 + c3) * 0.5f, c3 };
            float[] wingCenters = { Inner + 0.2f + RackTotalW * 0.5f, Max - 1.2f - RackTotalW * 0.5f };
            foreach (float z in wallCenters) slots.Add((new Vector3(Min + wallOff, 0f, z), 90f));
            foreach (float z in wingCenters)
            {
                slots.Add((new Vector3(WestX + pairOff, 0f, z), 90f));
                slots.Add((new Vector3(WestX - pairOff, 0f, z), -90f));
                slots.Add((new Vector3(Inner - wallOff, 0f, z), -90f));
            }
            slots.Add((new Vector3(wingCenters[0], 0f, Inner - wallOff), 180f));
            slots.Add((new Vector3(wingCenters[0], 0f, SouthZ + pairOff), 0f));
            slots.Add((new Vector3(wingCenters[0], 0f, SouthZ - pairOff), 180f));
            foreach (float x in wallCenters) slots.Add((new Vector3(-x, 0f, Min + wallOff), 0f));
            if (slots.Count < need) Debug.LogError($"SortThem: {need} categories, only {slots.Count} rack slots");
            if (slots.Count > need) slots.RemoveRange(need, slots.Count - need);

            var list = new List<RackPlacement>(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                var cat = categories[i];
                var pos = slots[i].pos;
                float yaw = slots[i].yaw;
                if (_roomLayout != null && _roomLayout.TryGetRack(cat.CategoryID, out var savedPos, out var savedYaw)) { pos = savedPos; yaw = savedYaw; }
                list.Add(new RackPlacement { Cat = cat, Pos = pos, Yaw = yaw });
            }
            return list;
        }

        static List<ShelfController> BuildRacks(CarCatalog catalog, ShelfData shelfData, List<RackPlacement> placements)
        {
            var shelves = new List<ShelfController>();
            var slots = new List<(Vector3 pos, float yaw)>();
            var categories = new List<CategoryData>();
            foreach (var c in catalog.Categories) if (c != null && !catalog.IsSpecial(c)) categories.Add(c);
            int need = categories.Count;
            float wallOff = RackD * 0.5f + 0.05f;
            float pairOff = (RackD + PairGap) * 0.5f;
            float c1 = Max - 0.5f - RackTotalW * 0.5f, c3 = Min + 1.2f + RackTotalW * 0.5f;
            float[] wallCenters = { c1, (c1 + c3) * 0.5f, c3 };
            float[] wingCenters = { Inner + 0.2f + RackTotalW * 0.5f, Max - 1.2f - RackTotalW * 0.5f };
            foreach (float z in wallCenters) slots.Add((new Vector3(Min + wallOff, 0f, z), 90f));
            foreach (float z in wingCenters)
            {
                slots.Add((new Vector3(WestX + pairOff, 0f, z), 90f));
                slots.Add((new Vector3(WestX - pairOff, 0f, z), -90f));
                slots.Add((new Vector3(Inner - wallOff, 0f, z), -90f));
            }
            slots.Add((new Vector3(wingCenters[0], 0f, Inner - wallOff), 180f));
            slots.Add((new Vector3(wingCenters[0], 0f, SouthZ + pairOff), 0f));
            slots.Add((new Vector3(wingCenters[0], 0f, SouthZ - pairOff), 180f));
            foreach (float x in wallCenters) slots.Add((new Vector3(-x, 0f, Min + wallOff), 0f));
            if (slots.Count < need) Debug.LogError($"SortThem: {need} categories, only {slots.Count} rack slots");
            if (slots.Count > need) slots.RemoveRange(need, slots.Count - need);

            EditorAssets.EnsureFolder(Paths.Racks);
            var racksRoot = new GameObject("Racks").transform;
            int shelfId = 0;
            float totalW = RackTotalW;
            for (int r = 0; r < placements.Count; r++)
            {
                var cat = placements[r].Cat;
                var rackData = EditorAssets.LoadOrCreate<RackData>(Paths.Racks + "/Rack_" + cat.CategoryID + ".asset");
                rackData.Category = cat;
                rackData.ShelfCount = ShelvesPerSection * Sections;
                rackData.Shelf = shelfData;
                EditorUtility.SetDirty(rackData);

                var rackGo = new GameObject("Rack_" + cat.CategoryID);
                rackGo.transform.SetParent(racksRoot, false);
                rackGo.transform.SetPositionAndRotation(placements[r].Pos, Quaternion.Euler(0f, placements[r].Yaw, 0f));
                var rack = rackGo.AddComponent<RackController>();
                rack.Category = cat;

                float plinthH = BottomShelfHeight - BoardT;
                float bodyBottom = plinthH > 0.02f ? plinthH : 0f;
                float bodyH = RackH - bodyBottom;
                float bodyMidY = bodyBottom + bodyH * 0.5f;

                Panel("Back", rackGo.transform, new Vector3(0f, bodyMidY, -RackD * 0.5f + 0.015f), new Vector3(totalW, bodyH, 0.03f), _rackBack, 1f);
                Panel("Side_L", rackGo.transform, new Vector3(-totalW * 0.5f - 0.025f, bodyMidY, 0f), new Vector3(0.05f, bodyH, RackD), _rack, SlabTile);
                Panel("Side_R", rackGo.transform, new Vector3(totalW * 0.5f + 0.025f, bodyMidY, 0f), new Vector3(0.05f, bodyH, RackD), _rack, SlabTile);
                for (int d = 1; d < Sections; d++)
                    Panel("Divider_" + d, rackGo.transform, new Vector3(-totalW * 0.5f + d * (RackW + DividerT) - DividerT * 0.5f, bodyMidY, 0f), new Vector3(DividerT, bodyH, RackD), _rack, SlabTile);
                Panel("Top", rackGo.transform, new Vector3(0f, RackH + 0.02f, 0f), new Vector3(totalW + 0.1f + CapOverhang * 2f, BoardT, RackD + CapOverhang * 2f), _rack, SlabTile);
                if (plinthH > 0.02f) Panel("Plinth", rackGo.transform, new Vector3(0f, plinthH * 0.5f, 0f), new Vector3(totalW + 0.1f + CapOverhang * 2f, plinthH, RackD + CapOverhang * 2f), _rack, SlabTile);

                var sign = Block("Sign", rackGo.transform, new Vector3(0f, RackH + 0.45f, 0.05f), new Vector3(Mathf.Min(totalW - 0.2f, 3f), 0.55f, 0.04f), EditorAssets.Unlit("Sign_" + cat.CategoryID, cat.CategoryColor));
                Object.DestroyImmediate(sign.GetComponent<Collider>());
                sign.isStatic = false;
                rack.SignPlate = sign.GetComponent<Renderer>();
                rack.SignText = Text3D(rackGo.transform, "SignText", cat.DevName, 2.2f, new Vector3(0f, RackH + 0.45f, 0.08f), Quaternion.Euler(0f, 180f, 0f), new Vector2(Mathf.Min(totalW - 0.3f, 2.9f), 0.5f), Color.white);
                var rackZoneGo = new GameObject("RackZone");
                rackZoneGo.transform.SetParent(rackGo.transform, false);
                rackZoneGo.transform.localPosition = new Vector3(0f, RackH * 0.5f + 0.15f, 0f);
                var rackZone = rackZoneGo.AddComponent<BoxCollider>();
                rackZone.isTrigger = true;
                rackZone.size = new Vector3(totalW + 0.3f, RackH + 0.5f, RackD + 0.3f);
                rackZoneGo.AddComponent<RackZone>().Rack = rack;
                rack.Zone = rackZone;
                rack.HighlightFrame = BuildFrame(rackGo.transform, new Vector3(0f, RackH * 0.5f + 0.02f, 0f), new Vector3(totalW + 0.16f, RackH + 0.1f, RackD + 0.1f));

                var rackShelves = new ShelfController[ShelvesPerSection * Sections];
                for (int sec = 0; sec < Sections; sec++)
                {
                    float xOff = -totalW * 0.5f + RackW * 0.5f + sec * (RackW + DividerT);
                    for (int s = 0; s < ShelvesPerSection; s++)
                    {
                        float y = ShelfHeights[s];
                        var shelfGo = new GameObject("Shelf_" + sec + "_" + s);
                        shelfGo.transform.SetParent(rackGo.transform, false);
                        shelfGo.transform.localPosition = new Vector3(xOff, y, 0f);
                        var shelf = shelfGo.AddComponent<ShelfController>();
                        shelf.ShelfId = shelfId++;
                        shelf.Data = shelfData;
                        shelf.Rack = rack;

                        Panel("Board", shelfGo.transform, new Vector3(0f, -BoardT * 0.5f, 0f), new Vector3(RackW, BoardT, RackD - 0.04f), _board, SlabTile);

                        float pitchY = ShelfPitch;
                        float zoneH = pitchY - BoardT - 0.02f;
                        var zoneGo = new GameObject("Zone");
                        zoneGo.transform.SetParent(shelfGo.transform, false);
                        zoneGo.transform.localPosition = new Vector3(0f, zoneH * 0.5f + 0.005f, 0f);
                        var zone = zoneGo.AddComponent<BoxCollider>();
                        zone.isTrigger = true;
                        zone.size = new Vector3(RackW, zoneH, RackD - 0.04f);
                        zoneGo.AddComponent<ShelfZone>().Shelf = shelf;
                        shelf.Zone = zone;

                        var points = new Transform[shelfData.Capacity];
                        float pitch = shelfData.SlotPitch;
                        float x0 = -(shelfData.Columns - 1) * pitch * 0.5f;
                        float z0 = -(shelfData.Rows - 1) * shelfData.RowPitch * 0.5f;
                        for (int k = 0; k < shelfData.Capacity; k++)
                        {
                            int row = k / shelfData.Columns;
                            int col = k % shelfData.Columns;
                            var pt = new GameObject("Slot_" + k).transform;
                            pt.SetParent(shelfGo.transform, false);
                            pt.localPosition = new Vector3(x0 + col * pitch, 0f, z0 + row * shelfData.RowPitch);
                            pt.localRotation = Quaternion.Euler(0f, shelfData.SlotYaw, 0f);
                            points[k] = pt;
                        }
                        shelf.SlotPoints = points;

                        shelf.Tag = BuildPriceTag(shelf, shelfGo.transform);
                        rackShelves[sec * ShelvesPerSection + s] = shelf;
                        shelves.Add(shelf);
                    }
                }
                rack.Shelves = rackShelves;
            }
            return shelves;
        }

        static GameObject BuildFrame(Transform parent, Vector3 center, Vector3 size)
        {
            var frame = new GameObject("HighlightFrame");
            frame.transform.SetParent(parent, false);
            frame.transform.localPosition = center;
            const float t = 0.06f;
            var hx = size.x * 0.5f; var hy = size.y * 0.5f; var hz = size.z * 0.5f;
            void Edge(Vector3 c, Vector3 s)
            {
                var b = Block("Edge", frame.transform, c, s, _highlight, false);
                Object.DestroyImmediate(b.GetComponent<Collider>());
            }
            foreach (float sy in new[] { -hy, hy })
            {
                foreach (float sz in new[] { -hz, hz }) Edge(new Vector3(0f, sy, sz), new Vector3(size.x, t, t));
                foreach (float sx in new[] { -hx, hx }) Edge(new Vector3(sx, sy, 0f), new Vector3(t, t, size.z));
            }
            foreach (float sx in new[] { -hx, hx })
                foreach (float sz in new[] { -hz, hz }) Edge(new Vector3(sx, 0f, sz), new Vector3(t, size.y, t));
            frame.SetActive(false);
            return frame;
        }

        static PriceTag BuildPriceTag(ShelfController shelf, Transform parent)
        {
            var root = new GameObject("PriceTag");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, -0.02f, RackD * 0.5f - 0.02f + 0.006f);
            var tag = root.AddComponent<PriceTag>();
            tag.Shelf = shelf;
            var plate = Block("Plate", root.transform, Vector3.zero, new Vector3(0.5f, 0.05f, 0.01f), _plateWhite, false);
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.layer = 0;
            tag.Plate = plate.GetComponent<Renderer>();
            tag.Root = plate;
            tag.WhiteMaterial = _plateWhite;
            tag.RedMaterial = _plateRed;
            tag.GoldMaterial = _plateGold;
            tag.NameText = Text3D(plate.transform, "Name", "", 0.32f, new Vector3(0f, 0.22f, 0.6f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.47f, 0.026f), Color.black);
            tag.PriceText = Text3D(plate.transform, "Price", "", 0.26f, new Vector3(0f, -0.26f, 0.6f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.47f, 0.021f), Color.black);
            foreach (var t in new[] { tag.NameText, tag.PriceText })
                t.transform.localScale = new Vector3(1f / 0.5f, 1f / 0.05f, 1f / 0.01f);
            return tag;
        }

        static TextMeshPro Text3D(Transform parent, string name, string text, float fontSize, Vector3 localPos, Quaternion localRot, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.05f;
            tmp.fontSizeMax = fontSize;
            tmp.rectTransform.sizeDelta = size;
            return tmp;
        }

        static GameObject BuildPlayer(GameManager gm)
        {
            var player = new GameObject("Player");
            player.layer = LayerMask.NameToLayer("Player");
            player.transform.position = new Vector3(Max - 3.5f, 0.05f, SouthZ);
            player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 1.65f, 0f);
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 150f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            var controller = player.AddComponent<PlayerController>();
            controller.CameraPivot = pivot;
            var inventory = player.AddComponent<Inventory>();
            var interaction = player.AddComponent<PlayerInteraction>();
            interaction.Cam = cam;
            interaction.Inventory = inventory;
            interaction.Outline = Ghost("HoverOutline", _outline);
            interaction.Ghost = Ghost("PlacementGhost", _ghost);
            var heldRoot = new GameObject("HeldItem");
            heldRoot.transform.SetParent(camGo.transform, false);
            var heldModel = new GameObject("Model");
            heldModel.transform.SetParent(heldRoot.transform, false);
            var heldFilter = heldModel.AddComponent<MeshFilter>();
            var heldRenderer = heldModel.AddComponent<MeshRenderer>();
            heldRenderer.sharedMaterial = _heldCars;
            heldRenderer.shadowCastingMode = ShadowCastingMode.Off;
            heldRenderer.receiveShadows = false;
            var heldView = heldRoot.AddComponent<HeldItemView>();
            heldView.Inventory = inventory;
            heldView.Filter = heldFilter;
            heldView.Renderer = heldRenderer;
            heldView.TexturedOverlayShader = AssetDatabase.LoadAssetAtPath<Shader>(Paths.Root + "/Art/Shaders/TexturedLitOverlay.shader");
            heldView.Scale = 2f;
            heldView.RestPosition = new Vector3(0.22f, -0.4f, 0.7f);
            heldView.RestEuler = new Vector3(0f, -115f, 0f);
            heldView.FitHeight = 0.24f;
            heldView.EnterOffset = new Vector3(0.5f, -0.6f, 0f);
            var abilities = player.AddComponent<PlayerAbilities>();
            abilities.Inventory = inventory;
            abilities.LevitateOutlineMaterial = _levOutline;
            return player;
        }

        static MeshGhost Ghost(string name, Material mat)
        {
            var go = new GameObject(name);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var ghost = go.AddComponent<MeshGhost>();
            ghost.Filter = mf;
            ghost.Renderer = mr;
            go.SetActive(false);
            return ghost;
        }
    }
}
