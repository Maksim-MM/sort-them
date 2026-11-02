using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SortThem
{
    public static class ConsoleLook
    {
        public static void Apply(GameConfig cfg)
        {
            if (cfg == null) return;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional || light.bakingOutput.lightmapBakeType != LightmapBakeType.Realtime) continue;
                if (cfg.ConsoleSunIntensity <= 0f) { light.enabled = false; continue; }
                light.enabled = true;
                light.intensity = cfg.ConsoleSunIntensity;
                light.shadows = LightShadows.Soft;
                light.cullingMask = (1 << Layers.LooseItems) | (1 << Layers.StaticPlaced);
            }

            var cam = Camera.main;
            if (cam != null && cfg.ConsoleVolume != null)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) data.renderPostProcessing = true;
                var go = new GameObject("ConsoleVolume");
                var volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = cfg.ConsoleVolume;
            }

            if (cfg.ConsoleWood != null && cfg.ConsoleWood.Length > 0)
            {
                var host = new GameObject("ConsoleWood").AddComponent<WoodHost>();
                host.StartCoroutine(ApplyWood(cfg.ConsoleWood));
            }

            if (cfg.ConsoleFogDensity > 0f)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = cfg.ConsoleFogColor;
                RenderSettings.fogDensity = cfg.ConsoleFogDensity;
                RenderSettings.fog = true;
            }
        }

        sealed class WoodHost : MonoBehaviour { }

        static IEnumerator ApplyWood(ConsoleWoodMap[] maps)
        {
            var copies = new Dictionary<Material, Material>();
            foreach (var map in maps)
            {
                if (map == null || map.Material == null || map.Normal == null || map.MetallicSmooth == null) continue;
                var n = map.Normal.LoadAssetAsync<Texture2D>();
                var ms = map.MetallicSmooth.LoadAssetAsync<Texture2D>();
                yield return n;
                yield return ms;
                if (n.Result == null || ms.Result == null) { Debug.LogWarning("ConsoleLook: wood maps failed for " + map.Material.name); continue; }
                var m = new Material(map.Material) { name = map.Material.name + "_Console" };
                m.SetTexture("_BumpMap", n.Result);
                m.SetFloat("_BumpScale", map.BumpScale);
                m.SetTexture("_MetallicGlossMap", ms.Result);
                m.SetFloat("_Smoothness", map.SmoothnessScale);
                if (!map.EnvironmentReflections)
                {
                    m.SetFloat("_EnvironmentReflections", 0f);
                    m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                }
                m.SetFloat("_SmoothnessTextureChannel", 0f);
                m.EnableKeyword("_NORMALMAP");
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                copies[map.Material] = m;
            }
            if (copies.Count == 0) yield break;
            int swapped = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && copies.TryGetValue(mats[i], out var c)) { mats[i] = c; changed = true; }
                if (changed) { r.sharedMaterials = mats; swapped++; }
            }
            Debug.Log("ConsoleLook: wood maps applied to " + swapped + " renderers");
        }
    }
}
