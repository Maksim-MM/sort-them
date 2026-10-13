using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SortThem.Editor
{
    public static class CarAddressables
    {
        public const string GroupName = "Cars";

        public static AssetReferenceGameObject Reference(GameObject prefab)
        {
            if (prefab == null) return null;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            if (string.IsNullOrEmpty(guid)) return null;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                var entry = settings.CreateOrMoveEntry(guid, EnsureGroup(settings), false, false);
                entry.address = prefab.name;
            }
            return new AssetReferenceGameObject(guid);
        }

        [MenuItem("SortThem/Cars: Setup Addressables")]
        public static void EnsureAll()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("CarAddressables: останови Play."); return; }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("CarAddressables: нет AddressableAssetSettings."); return; }
            int count = 0, missing = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:CarItemData", new[] { Paths.Cars }))
            {
                var data = AssetDatabase.LoadAssetAtPath<CarItemData>(AssetDatabase.GUIDToAssetPath(guid));
                var prefab = data != null && data.Prefab != null ? data.Prefab.editorAsset as GameObject : null;
                if (prefab == null) { missing++; continue; }
                data.Prefab = Reference(prefab);
                EditorUtility.SetDirty(data);
                count++;
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"CarAddressables: group {GroupName}, cars {count}, without prefab {missing}");
        }

        static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(GroupName);
            if (group != null) return group;
            group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            return group;
        }
    }
}
