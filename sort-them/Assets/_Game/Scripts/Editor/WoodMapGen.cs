using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class WoodMapGen
    {
        public const string OutDir = "Assets/_Game/Art/Textures/Room/Console";
        public static string LastReport;

        public static readonly string[] Sources =
        {
            "Assets/_Game/Art/Textures/Room/Wood_Floor.png",
            "Assets/_Game/Art/Textures/Room/Wood_Slab.png",
            "Assets/_Game/Art/Textures/Room/Wood_Beam.png",
            "Assets/_Game/Art/Textures/Room/Wood_Panel.png",
            "Assets/_Game/Art/Textures/Room/Wood_PlanksV.png",
        };

        [MenuItem("SortThem/Console/Generate Wood Maps")]
        public static void GenerateAll() => Generate(Sources);

        public static void Generate(string[] sources, float normalStrength = 4.5f, float smoothMin = 0.3f, float smoothMax = 0.9f, float wear = 0.2f, float grain = 0.6f)
        {
            Directory.CreateDirectory(OutDir);
            var report = new System.Text.StringBuilder("WoodMapGen:\n");
            foreach (var src in sources)
            {
                if (!File.Exists(src)) { report.Append(src).Append(": missing\n"); continue; }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                tex.LoadImage(File.ReadAllBytes(src));
                int w = tex.width, h = tex.height;
                var px = tex.GetPixels32();
                Object.DestroyImmediate(tex);

                var lum = new float[w * h];
                for (int i = 0; i < px.Length; i++) lum[i] = (0.299f * px[i].r + 0.587f * px[i].g + 0.114f * px[i].b) / 255f;
                var blur = Blur(Blur(lum, w, h), w, h);
                var height = new float[w * h];
                for (int i = 0; i < height.Length; i++) height[i] = 0.7f * blur[i] + 0.3f * lum[i];

                var normal = new Color32[w * h];
                var ms = new Color32[w * h];
                float lo = 1f, hi = 0f;
                for (int i = 0; i < blur.Length; i++) { if (blur[i] < lo) lo = blur[i]; if (blur[i] > hi) hi = blur[i]; }
                float range = Mathf.Max(0.001f, hi - lo);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        float dx = (height[y * w + (x + 1) % w] - height[y * w + (x + w - 1) % w]) * normalStrength;
                        float dy = (height[((y + 1) % h) * w + x] - height[((y + h - 1) % h) * w + x]) * normalStrength;
                        var n = new Vector3(-dx, -dy, 1f).normalized;
                        normal[i] = new Color32((byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 255);

                        float t = (blur[i] - lo) / range;
                        float s = Mathf.Lerp(smoothMin, smoothMax, Mathf.Pow(t, 1.2f));
                        s += (Noise(x / 160f, y / 160f) - 0.5f) * 2f * wear;
                        s += (Noise(x / 23f, y / 23f) - 0.5f) * wear * 0.5f;
                        s += (lum[i] - blur[i]) * grain;
                        s = Mathf.Clamp01(s);
                        ms[i] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(s * 255f));
                    }
                }

                string name = Path.GetFileNameWithoutExtension(src);
                string nPath = OutDir + "/" + name + "_N.png", msPath = OutDir + "/" + name + "_MS.png";
                Write(normal, w, h, nPath);
                Write(ms, w, h, msPath);
                AssetDatabase.ImportAsset(nPath);
                AssetDatabase.ImportAsset(msPath);
                Import(nPath, true);
                Import(msPath, false);
                report.Append(name).Append(": ").Append(w).Append('x').Append(h).Append(" -> ").Append(nPath).Append(", ").Append(msPath).Append('\n');
            }
            AssetDatabase.Refresh();
            LastReport = report.ToString();
            Debug.Log(LastReport);
        }

        static void Write(Color32[] data, int w, int h, string path)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            t.SetPixels32(data);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static void Import(string path, bool normalMap)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = false;
            imp.alphaSource = normalMap ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = false;
            imp.maxTextureSize = 2048;
            imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }

        static float[] Blur(float[] src, int w, int h)
        {
            var dst = new float[src.Length];
            for (int y = 0; y < h; y++)
            {
                int y0 = ((y + h - 1) % h) * w, y1 = y * w, y2 = ((y + 1) % h) * w;
                for (int x = 0; x < w; x++)
                {
                    int x0 = (x + w - 1) % w, x2 = (x + 1) % w;
                    dst[y1 + x] = (src[y0 + x0] + src[y0 + x] + src[y0 + x2] + src[y1 + x0] + src[y1 + x] + src[y1 + x2] + src[y2 + x0] + src[y2 + x] + src[y2 + x2]) / 9f;
                }
            }
            return dst;
        }

        static float Hash(int x, int y)
        {
            uint n = (uint)(x * 374761393 + y * 668265263);
            n = (n ^ (n >> 13)) * 1274126177u;
            return ((n ^ (n >> 16)) & 0xffffff) / 16777215f;
        }

        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }
}
