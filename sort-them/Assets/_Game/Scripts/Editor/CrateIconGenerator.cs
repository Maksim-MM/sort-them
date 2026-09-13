using System;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class CrateIconGenerator
    {
        const int Size = 256;
        const float Stroke = 7.5f;

        [MenuItem("SortThem/3e. Generate Crate Icon")]
        public static void Generate()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Shape(p);
                    float a = Mathf.Clamp01(0.5f - d);
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            string path = UiSpriteSetup.Dir + "/up_crate.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 256;
                ti.SaveAndReimport();
            }
            Debug.Log("SortThem: crate icon written to " + path);
        }

        static float Shape(Vector2 p)
        {
            float box = RoundedBox(p - new Vector2(128f, 88f), new Vector2(84f, 54f), 10f);
            float d = Mathf.Abs(box) - Stroke;

            d = Mathf.Min(d, Segment(p, new Vector2(46f, 122f), new Vector2(210f, 122f)) - Stroke);
            d = Mathf.Min(d, Segment(p, new Vector2(68f, 50f), new Vector2(188f, 110f)) - Stroke * 0.8f);
            d = Mathf.Min(d, Segment(p, new Vector2(68f, 110f), new Vector2(188f, 50f)) - Stroke * 0.8f);

            var gearCentre = new Vector2(162f, 186f);
            float gear = Mathf.Abs(Circle(p, gearCentre, 32f)) - Stroke;
            var v = p - gearCentre;
            float ang = Mathf.Atan2(v.y, v.x);
            const int teeth = 6;
            float step = Mathf.PI * 2f / teeth;
            float snapped = Mathf.Round(ang / step) * step;
            var dir = new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped));
            gear = Mathf.Min(gear, Segment(p, gearCentre + dir * 28f, gearCentre + dir * 48f) - Stroke * 0.9f);
            d = Mathf.Min(d, gear);

            var boltCentre = new Vector2(86f, 176f);
            float bolt = Mathf.Abs(Circle(p, boltCentre, 20f)) - Stroke * 0.95f;
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f + 0.3f;
                var dd = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bolt = Mathf.Min(bolt, Segment(p, boltCentre + dd * 17f, boltCentre + dd * 31f) - Stroke * 0.8f);
            }
            d = Mathf.Min(d, bolt);
            return d;
        }

        static float RoundedBox(Vector2 p, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x) - half.x + r, Mathf.Abs(p.y) - half.y + r);
            return Mathf.Min(Mathf.Max(q.x, q.y), 0f) + new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude - r;
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
    }
}
