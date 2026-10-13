using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SortThem.Editor
{
    public static class MusicAddressables
    {
        public const string GroupName = "Music";

        public static AssetReferenceT<AudioClip>[] Ensure(AudioClip[] clips)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("MusicAddressables: нет AddressableAssetSettings."); return System.Array.Empty<AssetReferenceT<AudioClip>>(); }
            var group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.Uncompressed;
            }
            var refs = new List<AssetReferenceT<AudioClip>>();
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
                if (string.IsNullOrEmpty(guid)) continue;
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = clip.name;
                refs.Add(new AssetReferenceT<AudioClip>(guid));
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"MusicAddressables: group {GroupName}, entries {refs.Count}");
            return refs.ToArray();
        }
    }
}
