using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SortThem.Editor
{
    public static class LightingSetup
    {
        const string RootName = "Lighting";
        const string SettingsPath = Paths.Config + "/RoomLighting.lighting";

        const float WindowIntensity = 2.5f;
        const float LampIntensity = 5.0f;
        const float LampRange = 8f;
        static readonly Color WindowColor = new Color(0.86f, 0.92f, 1f);
        static readonly Color LampColor = new Color(1f, 0.90f, 0.72f);

        public const float SunElevation = 45f;
        public const float SunAzimuth = 195f;
        const float SunIntensity = 5.0f;
        const float SkyAmbient = 0.35f;
        static readonly Color SunColor = new Color(1f, 0.94f, 0.82f);

        public static Vector3 SunDirection
        {
            get
            {
                float el = SunElevation * Mathf.Deg2Rad, az = SunAzimuth * Mathf.Deg2Rad;
                var h = new Vector3(Mathf.Sin(az), 0f, Mathf.Cos(az));
                return (h * Mathf.Cos(el) + Vector3.down * Mathf.Sin(el)).normalized;
            }
        }

        const float ArmLen = 28f, WingW = 10f;
        const float Min = -ArmLen * 0.5f, Max = ArmLen * 0.5f, Inner = Min + WingW;
        const float ProbeStep = 2.5f;
        const float SmallPropSize = 0.5f;
        static readonly float[] ProbeHeights = { 0.2f, 1.0f, 2.2f, 3.8f };

        [MenuItem("SortThem/6. Setup Lighting")]
        public static void Setup()
        {
            int fixedModels = EnsureModelLightmapUVs();

            var root = GameObject.Find(RootName);
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject(RootName);

            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.enabled = false;
            }

            BuildSun(root.transform);
            int windows = BuildWindowLights(root.transform);
            int lamps = BuildLampLights(root.transform);
            int probes = BuildLightProbes(root.transform);
            int reflections = BuildReflectionProbes(root.transform);
            int marked = MarkProps();
            TuneRenderers(out int scaled, out int excluded);
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = SkyAmbient;
            ApplySettings(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"SortThem lighting: {windows} window lights, {lamps} lamp lights, {probes} probes, " +
                      $"{reflections} reflection probes, {scaled} renderers rescaled, {excluded} excluded from GI, " +
                      $"{fixedModels} models given lightmap UVs, {marked} props marked static");
        }

        static int EnsureModelLightmapUVs()
        {
            var paths = new HashSet<string>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.sharedMesh.uv2 != null && mf.sharedMesh.uv2.Length > 0) continue;
                var path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetImporter.GetAtPath(path) is ModelImporter) paths.Add(path);
            }

            int count = 0;
            foreach (var path in paths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.generateSecondaryUV) continue;
                importer.generateSecondaryUV = true;
                importer.secondaryUVHardAngle = 88f;
                importer.secondaryUVAngleDistortion = 8f;
                importer.secondaryUVAreaDistortion = 15f;
                importer.secondaryUVPackMargin = 8f;
                importer.SaveAndReimport();
                count++;
            }
            return count;
        }

        static int MarkProps()
        {
            string[] roots = { "UpgradeTerminal", "SlotMachine", "Counter", "Doors", "Blueprints" };
            int count = 0;
            foreach (var name in roots)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                {
                    var mat = mr.sharedMaterial;
                    if (mat != null && mat.shader != null && mat.shader.name.Contains("Unlit")) continue;
                    if (mr.transform.parent != null && mr.transform.parent.name == "Frame") continue;
                    var flags = GameObjectUtility.GetStaticEditorFlags(mr.gameObject);
                    if ((flags & StaticEditorFlags.ContributeGI) != 0) continue;
                    GameObjectUtility.SetStaticEditorFlags(mr.gameObject,
                        flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
                    count++;
                }
            }
            return count;
        }

        static void BuildSun(Transform parent)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.LookRotation(SunDirection, Vector3.up);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = SunIntensity;
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.shadows = LightShadows.Soft;
            light.shadowAngle = 1.5f;
        }

        static int BuildWindowLights(Transform parent)
        {
            var group = new GameObject("WindowLights").transform;
            group.SetParent(parent, false);
            int count = 0;
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!mr.gameObject.name.StartsWith("Glass")) continue;
                var b = mr.bounds;
                var size = b.size;
                int thin = size.x < size.z ? 0 : 2;
                var normal = thin == 0 ? Vector3.right : Vector3.forward;
                float width = thin == 0 ? size.z : size.x;
                float height = size.y;

                var inward = Inside(b.center + normal * 1f) ? normal : -normal;
                if (!Inside(b.center + inward * 1f)) continue;

                var go = new GameObject("Window_" + count);
                go.transform.SetParent(group, false);
                go.transform.position = b.center + inward * 0.06f;
                go.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);

                var light = go.AddComponent<Light>();
                light.type = LightType.Rectangle;
                light.areaSize = new Vector2(Mathf.Max(0.1f, width - 0.1f), Mathf.Max(0.1f, height - 0.05f));
                light.color = WindowColor;
                light.intensity = WindowIntensity;
                light.range = 14f;
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.shadows = LightShadows.Soft;
                count++;
            }
            return count;
        }

        static int BuildLampLights(Transform parent)
        {
            var group = new GameObject("LampLights").transform;
            group.SetParent(parent, false);
            int count = 0;
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!mr.gameObject.name.StartsWith("LampGlow")) continue;
                var go = new GameObject("Lamp_" + count);
                go.transform.SetParent(group, false);
                go.transform.position = mr.bounds.center + Vector3.down * 0.04f;

                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = LampColor;
                light.intensity = LampIntensity;
                light.range = LampRange;
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.shadows = LightShadows.Soft;
                light.shadowRadius = 0.2f;
                count++;
            }
            return count;
        }

        static int BuildLightProbes(Transform parent)
        {
            var go = new GameObject("LightProbes");
            go.transform.SetParent(parent, false);
            var group = go.AddComponent<LightProbeGroup>();

            var solids = new List<Bounds>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(mr.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0) continue;
                var b = mr.bounds;
                b.Expand(0.12f);
                solids.Add(b);
            }

            var positions = new List<Vector3>();
            for (float x = Min + 0.6f; x <= Max - 0.6f; x += ProbeStep)
            for (float z = Min + 0.6f; z <= Max - 0.6f; z += ProbeStep)
            {
                if (!Inside(new Vector3(x, 1f, z))) continue;
                foreach (var y in ProbeHeights)
                {
                    var p = new Vector3(x, y, z);
                    if (IsSolid(solids, p)) continue;
                    positions.Add(p);
                }
            }
            group.probePositions = positions.ToArray();
            return positions.Count;
        }

        static bool IsSolid(List<Bounds> solids, Vector3 p)
        {
            for (int i = 0; i < solids.Count; i++)
                if (solids[i].Contains(p)) return true;
            return false;
        }

        static int BuildReflectionProbes(Transform parent)
        {
            var group = new GameObject("ReflectionProbes").transform;
            group.SetParent(parent, false);

            AddReflection(group, "Reflection_W", new Vector3((Min + Inner) * 0.5f, 1.8f, 0f), new Vector3(WingW, 5f, ArmLen));
            AddReflection(group, "Reflection_S", new Vector3((Inner + Max) * 0.5f, 1.8f, (Min + Inner) * 0.5f), new Vector3(Max - Inner, 5f, WingW));
            return 2;
        }

        static void AddReflection(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = size;
            probe.center = Vector3.zero;
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.hdr = false;
            probe.shadowDistance = 20f;
        }

        static void TuneRenderers(out int scaled, out int excluded)
        {
            scaled = 0;
            excluded = 0;
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(mr.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0) continue;

                string n = mr.gameObject.name;
                bool unlit = mr.sharedMaterial != null && mr.sharedMaterial.shader != null &&
                             mr.sharedMaterial.shader.name.Contains("Unlit");
                if (unlit || n.StartsWith("Glass") || n.StartsWith("LampGlow"))
                {
                    GameObjectUtility.SetStaticEditorFlags(mr.gameObject, flags & ~StaticEditorFlags.ContributeGI);
                    mr.lightProbeUsage = LightProbeUsage.Off;
                    excluded++;
                    continue;
                }

                if (n == "Mark")
                {
                    mr.receiveGI = ReceiveGI.Lightmaps;
                    if (SetLightmapScale(mr, 2f)) scaled++;
                    continue;
                }

                float longest = Mathf.Max(mr.bounds.size.x, Mathf.Max(mr.bounds.size.y, mr.bounds.size.z));
                mr.receiveGI = longest < SmallPropSize ? ReceiveGI.LightProbes : ReceiveGI.Lightmaps;
                if (mr.receiveGI == ReceiveGI.LightProbes) { mr.lightProbeUsage = LightProbeUsage.BlendProbes; continue; }
                float scale = longest < 1.2f ? 0.6f : 1f;
                if (n.StartsWith("Floor_")) scale = 2f;
                if (SetLightmapScale(mr, scale)) scaled++;
            }
        }

        static bool SetLightmapScale(MeshRenderer mr, float scale)
        {
            var so = new SerializedObject(mr);
            var prop = so.FindProperty("m_ScaleInLightmap");
            if (prop == null) return false;
            if (Mathf.Approximately(prop.floatValue, scale)) return false;
            prop.floatValue = scale;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static LightingSettings ApplySettings(bool final)
        {
            EditorAssets.EnsureFolder(Paths.Config);
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = "RoomLighting" };
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.directionalityMode = LightmapsMode.CombinedDirectional;
            settings.lightmapMaxSize = 1024;
            settings.lightmapPadding = 2;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.ao = true;
            settings.aoMaxDistance = 0.2f;
            settings.aoExponentDirect = 1f;
            settings.aoExponentIndirect = 1f;
            settings.albedoBoost = 2.0f;
            settings.maxBounces = 3;
            settings.lightmapResolution = final ? 12f : 4f;
            settings.directSampleCount = final ? 64 : 16;
            settings.indirectSampleCount = final ? 512 : 96;
            settings.environmentSampleCount = final ? 256 : 64;
            settings.filteringMode = LightingSettings.FilterMode.Auto;

            Lightmapping.lightingSettings = settings;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        internal static bool Inside(Vector3 p)
        {
            bool west = p.x > Min && p.x < Inner && p.z > Min && p.z < Max;
            bool south = p.z > Min && p.z < Inner && p.x > Min && p.x < Max;
            return west || south;
        }
    }
}
