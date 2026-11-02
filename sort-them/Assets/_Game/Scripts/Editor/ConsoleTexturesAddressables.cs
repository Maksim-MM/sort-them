using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SortThem.Editor
{
    public static class ConsoleTexturesAddressables
    {
        public const string GroupName = "Console-Textures";
        const string ConfigPath = "Assets/_Game/Data/Config/GameConfig.asset";

        static readonly (string material, string texture, float smoothness, bool env)[] Wood =
        {
            ("Room_WoodFloor", "Wood_Floor", 1f, true),
            ("ShelfBoard", "Wood_Slab", 0.5f, false),
            ("Rack", "Wood_Slab", 0.5f, false),
            ("Room_WoodBeam", "Wood_Beam", 0.8f, true),
            ("RackBack", "Wood_PlanksV", 0.45f, false),
            ("Room_WoodPanelV", "Wood_PlanksV", 0.7f, true),
            ("Room_WoodPanel", "Wood_Panel", 0.7f, true),
        };

        [MenuItem("SortThem/Console/Setup Wood Addressables")]
        public static void Ensure()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("ConsoleTexturesAddressables: no Addressables settings"); return; }
            var group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            }
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var list = new List<ConsoleWoodMap>();
            int entries = 0;
            foreach (var (material, texture, smoothness, env) in Wood)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/" + material + ".mat");
                string nPath = WoodMapGen.OutDir + "/" + texture + "_N.png";
                string msPath = WoodMapGen.OutDir + "/" + texture + "_MS.png";
                if (mat == null || !System.IO.File.Exists(nPath) || !System.IO.File.Exists(msPath)) { Debug.LogWarning("ConsoleTexturesAddressables: skip " + material); continue; }
                string nGuid = AssetDatabase.AssetPathToGUID(nPath), msGuid = AssetDatabase.AssetPathToGUID(msPath);
                foreach (var guid in new[] { nGuid, msGuid })
                {
                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    entry.address = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                    entries++;
                }
                list.Add(new ConsoleWoodMap { Material = mat, Normal = new AssetReferenceTexture2D(nGuid), MetallicSmooth = new AssetReferenceTexture2D(msGuid), BumpScale = 1.3f, SmoothnessScale = smoothness, EnvironmentReflections = env });
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            if (cfg != null)
            {
                cfg.ConsoleWood = list.ToArray();
                EditorUtility.SetDirty(cfg);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ConsoleTexturesAddressables: group " + GroupName + ", entries " + entries + ", config maps " + list.Count);
        }

        public static bool SetIncludeInBuild(bool include)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var schema = settings != null ? settings.FindGroup(GroupName)?.GetSchema<BundledAssetGroupSchema>() : null;
            if (schema == null) return true;
            bool prev = schema.IncludeInBuild;
            if (prev == include) return prev;
            schema.IncludeInBuild = include;
            EditorUtility.SetDirty(schema);
            AssetDatabase.SaveAssetIfDirty(schema);
            return prev;
        }
    }
}
