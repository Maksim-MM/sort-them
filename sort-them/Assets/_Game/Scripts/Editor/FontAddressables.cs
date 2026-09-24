using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace SortThem.Editor
{
    public static class FontAddressables
    {
        public const string GroupName = "Fonts-CJK";
        public const string Dir = "Assets/_Game/Fonts/SDF";
        const string OldDir = "Assets/_Game/Resources/Fonts";
        static readonly string[] Names = { "NotoSansJP SDF", "NotoSansKR SDF", "NotoSansSC SDF", "NotoSansTC SDF" };

        [MenuItem("SortThem/Loc/Fonts/Setup CJK Addressables")]
        public static void Ensure()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("FontAddressables: останови Play."); return; }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("FontAddressables: нет AddressableAssetSettings."); return; }
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Game/Fonts", "SDF");
            foreach (var name in Names)
            {
                string oldPath = OldDir + "/" + name + ".asset";
                string newPath = Dir + "/" + name + ".asset";
                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    string err = AssetDatabase.MoveAsset(oldPath, newPath);
                    if (!string.IsNullOrEmpty(err)) Debug.LogError("FontAddressables: " + err);
                }
            }
            if (AssetDatabase.IsValidFolder(OldDir) && AssetDatabase.FindAssets("", new[] { OldDir }).Length == 0) AssetDatabase.DeleteAsset(OldDir);

            var group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            }
            int count = 0;
            foreach (var name in Names)
            {
                string guid = AssetDatabase.AssetPathToGUID(Dir + "/" + name + ".asset");
                if (string.IsNullOrEmpty(guid)) { Debug.LogWarning("FontAddressables: нет " + name); continue; }
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = name;
                count++;
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log("FontAddressables: group " + GroupName + ", entries " + count);
        }

        [InitializeOnLoadMethod]
        static void RestoreAfterInterruptedBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (BuildPipeline.isBuildingPlayer) return;
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                var schema = settings != null ? settings.FindGroup(GroupName)?.GetSchema<BundledAssetGroupSchema>() : null;
                if (schema == null || schema.IncludeInBuild) return;
                SetIncludeInBuild(true);
                Debug.LogWarning("FontAddressables: группа " + GroupName + " была выключена (прерванная веб-сборка?), включена обратно.");
            };
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
