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
        const float RoomX = 36f, RoomZ = 20f, RoomH = 5f, WallT = 0.3f;
        const float ShopGapX = 10.8f;
        const int RacksTop = 5, RacksBottom = 5, RacksLeft = 3, RacksRight = 2;
        const float BoardT = 0.04f, DividerT = 0.05f, ShelfPitch = 0.42f;
        static float RackW = 2.45f, RackD = 1.1f;
        const int ShelvesPerSection = 5, Sections = 2;
        const float BottomShelfHeight = 0.55f;
        static readonly float[] ShelfHeights = { BottomShelfHeight, BottomShelfHeight + ShelfPitch, BottomShelfHeight + ShelfPitch * 2f, BottomShelfHeight + ShelfPitch * 3f, BottomShelfHeight + ShelfPitch * 4f };
        static float RackH => ShelfHeights[ShelfHeights.Length - 1] + ShelfPitch - BoardT;
        static float RackTotalW => Sections * RackW + (Sections - 1) * DividerT;

        static Material _floor, _wall, _ceiling, _rack, _board, _podium, _terminal, _cabinet, _radio, _plateWhite, _plateRed, _plateGold, _marker, _ghost, _outline, _highlight, _levOutline, _tutOutline, _heldCars;

        [MenuItem("SortThem/4. Build Level Scene")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(Paths.Catalog);
            if (catalog == null || catalog.Categories.Length == 0)
            {
                Debug.LogError("SortThem: generate cars first");
                return;
            }
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
            gameConfig.LevelHalfExtents = new Vector3(RoomX * 0.5f + 1f, RoomH, RoomZ * 0.5f + 1f);
            EditorUtility.SetDirty(gameConfig);
            var economy = EditorAssets.LoadOrCreate<EconomyConfig>(Paths.Config + "/EconomyConfig.asset");
            var upgrades = UpgradeSetup.CreateAll(false);

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

            BuildRoom();
            var shelves = BuildRacks(catalog, shelfData);
            var podium = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podium.name = "Podium";
            podium.transform.position = new Vector3(0f, 0.3f, 0f);
            podium.transform.localScale = new Vector3(4f, 0.3f, 4f);
            podium.GetComponent<Renderer>().sharedMaterial = _podium;
            podium.isStatic = true;

            var terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terminal.name = "UpgradeTerminal";
            terminal.transform.position = new Vector3(ShopGapX - 0.57f, 0.8f, -RoomZ * 0.5f + 0.3f);
            terminal.transform.rotation = Quaternion.identity;
            terminal.transform.localScale = new Vector3(0.9f, 1.6f, 0.5f);
            terminal.GetComponent<Renderer>().sharedMaterial = _terminal;
            terminal.AddComponent<UpgradeTerminal>();
            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(terminal.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.2f, 0.52f);
            screen.transform.localScale = new Vector3(0.8f, 0.35f, 0.05f);
            screen.GetComponent<Renderer>().sharedMaterial = _marker;
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var termText = Text3D(terminal.transform, "Label", "UPGRADES", 1.2f, new Vector3(0f, 0.6f, 0.52f), Quaternion.Euler(0f, 180f, 0f), new Vector2(1.5f, 0.3f), Color.white);
            termText.transform.localScale = new Vector3(1f / 0.9f, 1f / 1.6f, 1f / 0.5f);

            BuildSlotMachine();
            BuildCashRegister();
            BuildTutorial();

            var cabinet = Block("Cabinet", null, new Vector3(-ShopGapX, 0.4f, -RoomZ * 0.5f + 0.3f), new Vector3(0.7f, 0.8f, 0.5f), _cabinet);
            var radio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radio.name = "Radio";
            radio.transform.position = new Vector3(-ShopGapX, 0.8f + 0.13f, -RoomZ * 0.5f + 0.3f);
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
            source.position = new Vector3(0f, 1.3f, 0f);
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
            var desk = Block("CashDesk", null, new Vector3(RoomX * 0.5f - WallT - 0.45f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 1.4f), _cabinet);
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
            var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slot.name = "SlotMachine";
            slot.transform.position = new Vector3(ShopGapX + 0.58f, 0.8f, -RoomZ * 0.5f + 0.3f);
            slot.transform.rotation = Quaternion.identity;
            slot.transform.localScale = new Vector3(0.8f, 1.6f, 0.5f);
            slot.GetComponent<Renderer>().sharedMaterial = _radio;
            slot.AddComponent<SlotMachine>();
            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(slot.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.15f, 0.52f);
            screen.transform.localScale = new Vector3(0.8f, 0.3f, 0.05f);
            screen.GetComponent<Renderer>().sharedMaterial = _plateGold;
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var lever = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lever.name = "Lever";
            lever.transform.SetParent(slot.transform, false);
            lever.transform.localPosition = new Vector3(0.52f, 0.2f, 0.44f);
            lever.transform.localScale = new Vector3(0.08f, 0.22f, 0.12f);
            lever.GetComponent<Renderer>().sharedMaterial = _terminal;
            Object.DestroyImmediate(lever.GetComponent<Collider>());
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Knob";
            knob.transform.SetParent(slot.transform, false);
            knob.transform.localPosition = new Vector3(0.52f, 0.45f, 0.44f);
            knob.transform.localScale = new Vector3(0.14f, 0.07f, 0.22f);
            knob.GetComponent<Renderer>().sharedMaterial = _plateRed;
            Object.DestroyImmediate(knob.GetComponent<Collider>());
            var label = Text3D(slot.transform, "Label", "CAR POT", 1.2f, new Vector3(0f, 0.6f, 0.52f), Quaternion.Euler(0f, 180f, 0f), new Vector2(1.5f, 0.3f), Color.white);
            label.transform.localScale = new Vector3(1f / 0.8f, 1f / 1.6f, 1f / 0.5f);
        }

        static void CreateMaterials()
        {
            _floor = EditorAssets.Lit("Floor", new Color(0.42f, 0.42f, 0.45f));
            _wall = EditorAssets.Lit("Wall", new Color(0.78f, 0.74f, 0.66f));
            _ceiling = EditorAssets.Lit("Ceiling", new Color(0.85f, 0.85f, 0.85f));
            _rack = EditorAssets.Lit("Rack", new Color(0.35f, 0.27f, 0.2f));
            _board = EditorAssets.Lit("ShelfBoard", new Color(0.6f, 0.48f, 0.36f));
            _podium = EditorAssets.Lit("Podium", new Color(0.5f, 0.5f, 0.55f));
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
            _levOutline.SetFloat("_Width", 0.02f);
            _levOutline.SetColor("_XRayColor", new Color(0.72f, 0.3f, 1f, 0.55f));
            _levOutline.renderQueue = 3000;
            EditorUtility.SetDirty(_levOutline);
            _tutOutline = EditorAssets.LoadOrCreateMaterial("OutlineYellow", "SortThem/OutlineXRay", new Color(1f, 0.85f, 0.2f), "_Color");
            _tutOutline.shader = Shader.Find("SortThem/OutlineXRay");
            _tutOutline.SetFloat("_Width", 0.02f);
            _tutOutline.SetColor("_XRayColor", new Color(1f, 0.85f, 0.2f, 0.6f));
            _tutOutline.renderQueue = 3000;
            EditorUtility.SetDirty(_tutOutline);
            _heldCars = EditorAssets.LoadOrCreateMaterial("CarsHeld", "SortThem/VertexColorLitOverlay", Color.white);
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

        static void BuildRoom()
        {
            var room = new GameObject("Room").transform;
            Block("Floor", room, new Vector3(0f, -0.1f, 0f), new Vector3(RoomX + 2f, 0.2f, RoomZ + 2f), _floor);
            Block("Ceiling", room, new Vector3(0f, RoomH + 0.15f, 0f), new Vector3(RoomX + 2f, 0.3f, RoomZ + 2f), _ceiling);
            Block("Wall_N", room, new Vector3(0f, RoomH * 0.5f, RoomZ * 0.5f + WallT * 0.5f), new Vector3(RoomX + WallT * 2f, RoomH, WallT), _wall);
            Block("Wall_S", room, new Vector3(0f, RoomH * 0.5f, -RoomZ * 0.5f - WallT * 0.5f), new Vector3(RoomX + WallT * 2f, RoomH, WallT), _wall);
            Block("Wall_E", room, new Vector3(RoomX * 0.5f + WallT * 0.5f, RoomH * 0.5f, 0f), new Vector3(WallT, RoomH, RoomZ), _wall);
            Block("Wall_W", room, new Vector3(-RoomX * 0.5f - WallT * 0.5f, RoomH * 0.5f, 0f), new Vector3(WallT, RoomH, RoomZ), _wall);
        }

        static List<ShelfController> BuildRacks(CarCatalog catalog, ShelfData shelfData)
        {
            var shelves = new List<ShelfController>();
            var slots = new List<(Vector3 pos, float yaw)>();
            int need = catalog.Categories.Length;
            if (need > RacksTop + RacksBottom + RacksLeft + RacksRight) Debug.LogError($"SortThem: {need} categories, only {RacksTop + RacksBottom + RacksLeft + RacksRight} rack slots");
            float sideUsable = RoomZ - 2f * RackD - 0.4f;
            for (int i = 0; i < RacksTop; i++)
            {
                float step = RoomX / RacksTop;
                slots.Add((new Vector3(-RoomX * 0.5f + step * (i + 0.5f), 0f, RoomZ * 0.5f - RackD * 0.5f), 180f));
            }
            for (int j = 0; j < RacksRight; j++)
            {
                float step = sideUsable / RacksRight;
                slots.Add((new Vector3(RoomX * 0.5f - RackD * 0.5f, 0f, -sideUsable * 0.5f + step * (j + 0.5f)), -90f));
            }
            for (int i = 0; i < RacksBottom; i++)
            {
                float step = RoomX / RacksBottom;
                slots.Add((new Vector3(RoomX * 0.5f - step * (i + 0.5f), 0f, -RoomZ * 0.5f + RackD * 0.5f), 0f));
            }
            for (int j = 0; j < RacksLeft; j++)
            {
                float step = sideUsable / RacksLeft;
                slots.Add((new Vector3(-RoomX * 0.5f + RackD * 0.5f, 0f, sideUsable * 0.5f - step * (j + 0.5f)), 90f));
            }
            if (slots.Count > need) slots.RemoveRange(need, slots.Count - need);

            EditorAssets.EnsureFolder(Paths.Racks);
            var racksRoot = new GameObject("Racks").transform;
            int shelfId = 0;
            float totalW = RackTotalW;
            for (int r = 0; r < slots.Count; r++)
            {
                var cat = catalog.Categories[r % catalog.Categories.Length];
                var rackData = EditorAssets.LoadOrCreate<RackData>(Paths.Racks + "/Rack_" + cat.CategoryID + ".asset");
                rackData.Category = cat;
                rackData.ShelfCount = ShelvesPerSection * Sections;
                rackData.Shelf = shelfData;
                EditorUtility.SetDirty(rackData);

                var rackGo = new GameObject("Rack_" + cat.CategoryID);
                rackGo.transform.SetParent(racksRoot, false);
                rackGo.transform.SetPositionAndRotation(slots[r].pos, Quaternion.Euler(0f, slots[r].yaw, 0f));
                var rack = rackGo.AddComponent<RackController>();
                rack.Category = cat;

                Block("Back", rackGo.transform, new Vector3(0f, RackH * 0.5f, -RackD * 0.5f + 0.015f), new Vector3(totalW + 0.1f, RackH, 0.03f), _rack);
                Block("Side_L", rackGo.transform, new Vector3(-totalW * 0.5f - 0.025f, RackH * 0.5f, 0f), new Vector3(0.05f, RackH, RackD), _rack);
                Block("Side_R", rackGo.transform, new Vector3(totalW * 0.5f + 0.025f, RackH * 0.5f, 0f), new Vector3(0.05f, RackH, RackD), _rack);
                for (int d = 1; d < Sections; d++)
                    Block("Divider_" + d, rackGo.transform, new Vector3(-totalW * 0.5f + d * (RackW + DividerT) - DividerT * 0.5f, RackH * 0.5f, 0f), new Vector3(DividerT, RackH, RackD), _rack);
                Block("Top", rackGo.transform, new Vector3(0f, RackH + 0.02f, 0f), new Vector3(totalW + 0.1f, BoardT, RackD), _rack);
                float plinthH = BottomShelfHeight - BoardT;
                if (plinthH > 0.02f) Block("Plinth", rackGo.transform, new Vector3(0f, plinthH * 0.5f, 0f), new Vector3(totalW + 0.1f, plinthH, RackD), _rack);

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

                        Block("Board", shelfGo.transform, new Vector3(0f, -BoardT * 0.5f, 0f), new Vector3(RackW, BoardT, RackD - 0.04f), _board);

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
            root.transform.localPosition = new Vector3(0f, -0.04f, RackD * 0.5f - 0.02f + 0.006f);
            var tag = root.AddComponent<PriceTag>();
            tag.Shelf = shelf;
            var plate = Block("Plate", root.transform, Vector3.zero, new Vector3(0.5f, 0.08f, 0.01f), _plateWhite, false);
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.layer = 0;
            tag.Plate = plate.GetComponent<Renderer>();
            tag.Root = plate;
            tag.WhiteMaterial = _plateWhite;
            tag.RedMaterial = _plateRed;
            tag.GoldMaterial = _plateGold;
            tag.NameText = Text3D(plate.transform, "Name", "", 0.32f, new Vector3(0f, 0.22f, 0.6f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.47f, 0.042f), Color.black);
            tag.PriceText = Text3D(plate.transform, "Price", "", 0.26f, new Vector3(0f, -0.26f, 0.6f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.47f, 0.034f), Color.black);
            foreach (var t in new[] { tag.NameText, tag.PriceText })
                t.transform.localScale = new Vector3(1f / 0.5f, 1f / 0.08f, 1f / 0.01f);
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
            player.transform.position = new Vector3(5f, 0.05f, -4.5f);
            player.transform.rotation = Quaternion.Euler(0f, -60f, 0f);
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
