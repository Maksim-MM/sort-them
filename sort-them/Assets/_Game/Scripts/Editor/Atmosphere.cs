using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SortThem.Editor
{
    public static class Atmosphere
    {
        const string RootName = "Atmosphere";
        const string DustName = "Dust";
        const float MinFacing = 0.35f;

        [MenuItem("SortThem/4f. Add Atmosphere")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == RootName) Object.DestroyImmediate(go);

            var root = new GameObject(RootName);
            int sunWindows = DressSunWindows();
            bool dust = BuildDust();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"SortThem atmosphere: {sunWindows} sun-lit glass panes, dust {(dust ? "attached" : "skipped (no Player)")}, sun {LightingSetup.SunDirection:F2}");
        }

        static int DressSunWindows()
        {
            var sun = LightingSetup.SunDirection;
            var sunFlat = new Vector3(sun.x, 0f, sun.z).normalized;
            string tex = RoomTextures.Folder;
            var skySun = EditorAssets.Textured("Room_WindowSkySun", tex + "/Window_SkySun.png", Color.white, 0f, true);
            var frostSun = EditorAssets.Textured("Room_WindowSun", tex + "/Window_FrostSun.png", Color.white, 0f, true);
            var sky = AssetDatabase.LoadAssetAtPath<Material>(Paths.Materials + "/Room_WindowSky.mat");
            var frost = AssetDatabase.LoadAssetAtPath<Material>(Paths.Materials + "/Room_Window.mat");

            int count = 0;
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!mr.gameObject.name.StartsWith("Glass")) continue;
                var b = mr.bounds;
                bool thinX = b.size.x < b.size.z;
                var normal = thinX ? Vector3.right : Vector3.forward;
                var inward = LightingSetup.Inside(b.center + normal) ? normal : -normal;
                if (!LightingSetup.Inside(b.center + inward)) continue;
                bool lit = Vector3.Dot(inward, sunFlat) >= MinFacing;

                var mat = mr.sharedMaterial;
                if (lit && (mat == sky || mat == skySun)) mr.sharedMaterial = skySun;
                else if (lit && (mat == frost || mat == frostSun)) mr.sharedMaterial = frostSun;
                else if (!lit && mat == skySun) mr.sharedMaterial = sky;
                else if (!lit && mat == frostSun) mr.sharedMaterial = frost;
                if (lit) count++;
            }
            return count;
        }

        static bool BuildDust()
        {
            var controller = Object.FindFirstObjectByType<PlayerController>();
            if (controller == null) return false;
            var player = controller.transform;
            var old = player.Find(DustName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var go = new GameObject(DustName);
            go.transform.SetParent(player, false);
            go.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            main.startColor = Color.white;
            main.gravityModifier = 0.0004f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 4.4f, 12f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.06f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.15f;
            noise.damping = true;
            noise.octaveCount = 1;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.12f), new GradientAlphaKey(0.45f, 0.85f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = EditorAssets.LoadOrCreateMaterial("Dust", "SortThem/Dust", new Color(1f, 0.96f, 0.88f, 1f), "_Color");
            r.sortMode = ParticleSystemSortMode.None;
            r.minParticleSize = 0f;
            r.maxParticleSize = 0.01f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return true;
        }
    }
}
