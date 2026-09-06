using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class EditorAssets
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static Material LoadOrCreateMaterial(string name, string shaderName, Color color, string colorProp = "_BaseColor")
        {
            EnsureFolder(Paths.Materials);
            string path = Paths.Materials + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError("Shader not found: " + shaderName);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader) mat.shader = shader;
            if (mat.HasProperty(colorProp)) mat.SetColor(colorProp, color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Lit(string name, Color color) => LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit", color);
        public static Material Unlit(string name, Color color) => LoadOrCreateMaterial(name, "Universal Render Pipeline/Unlit", color);
    }
}
