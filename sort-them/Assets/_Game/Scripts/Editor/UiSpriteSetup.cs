using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class UiSpriteSetup
    {
        public const string Dir = Paths.Root + "/Art/UI";
        public static readonly string[] AbilityIcons = { Dir + "/ab_find.png", Dir + "/ab_collect.png", Dir + "/ab_rack.png" };
        public const string SlotFrame = Dir + "/frame_slot.png";
        public const string KeyFrame = Dir + "/frame_key.png";
        static readonly (UpgradeKind Kind, string Path)[] UpgradeIcons =
        {
            (UpgradeKind.Inventory, Dir + "/up_inventory.png"),
            (UpgradeKind.Range, Dir + "/up_range.png"),
            (UpgradeKind.Sprint, Dir + "/up_sprint.png"),
            (UpgradeKind.Crouch, Dir + "/up_crouch.png"),
            (UpgradeKind.ThrowPower, Dir + "/up_throw.png"),
            (UpgradeKind.AutoPlace, Dir + "/up_autoplace.png"),
            (UpgradeKind.DuplicateHighlight, AbilityIcons[0]),
            (UpgradeKind.AutoCollect, AbilityIcons[1]),
            (UpgradeKind.ShelfHighlight, AbilityIcons[2]),
        };

        [MenuItem("SortThem/3d. Import UI Sprites")]
        public static void ImportAll()
        {
            foreach (var p in AbilityIcons) Import(p, Vector4.zero);
            Import(SlotFrame, new Vector4(28f, 28f, 28f, 28f));
            Import(KeyFrame, new Vector4(24f, 24f, 24f, 24f));
            foreach (var (_, path) in UpgradeIcons) if (System.Array.IndexOf(AbilityIcons, path) < 0) Import(path, Vector4.zero);
            AssignUpgradeIcons();
            AssetDatabase.SaveAssets();
            Debug.Log("SortThem: UI sprites imported");
        }

        public static void AssignUpgradeIcons()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UpgradeData", new[] { Paths.Upgrades }))
            {
                var u = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var (kind, path) in UpgradeIcons)
                {
                    if (u.Kind != kind) continue;
                    u.Icon = Load(path);
                    EditorUtility.SetDirty(u);
                }
            }
        }

        public static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static void Import(string path, Vector4 border)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogError("SortThem: no texture at " + path); return; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spriteBorder = border;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 256;
            ti.SaveAndReimport();
        }
    }
}
