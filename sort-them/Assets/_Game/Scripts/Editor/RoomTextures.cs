using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class RoomTextures
    {
        public const string Folder = Paths.Root + "/Art/Textures/Room";
        const int Size = 512;
        const int WoodSize = 1024;

        [MenuItem("SortThem/2a. Generate Room Textures")]
        public static void Generate()
        {
            EditorAssets.EnsureFolder(Paths.Root + "/Art/Textures");
            EditorAssets.EnsureFolder(Folder);
            Write("Wood_Beam", WoodSize, WoodSize, Wood(4, new Color(0.44f, 0.26f, 0.13f), 0.30f, 11, WoodSize));
            Write("Wood_Floor", WoodSize, WoodSize, Wood(7, new Color(0.56f, 0.35f, 0.18f), 0.22f, 23, WoodSize));
            Write("Wood_Panel", WoodSize, WoodSize, Wood(5, new Color(0.50f, 0.30f, 0.16f), 0.26f, 37, WoodSize));
            Write("Plaster_Blue", Size, Size, Plaster(new Color(0.16f, 0.33f, 0.53f), 41));
            Write("Plaster_Ceiling", Size, Size, Plaster(new Color(0.86f, 0.83f, 0.76f), 53));
            Write("Rug_Check", Size, Size, Rug(new Color(0.24f, 0.24f, 0.25f), new Color(0.82f, 0.78f, 0.69f), 8, 67));
            Write("Window_Frost", Size, Size, FrostedWindow(71));
            Write("Window_Sky", Size, Size, FrostedSky(97));
            Write("Window_FrostSun", Size, Size, Glare(FrostedWindow(71), 0.5f, 1.0f, 0.9f, 0.5f, new Color(1f, 0.97f, 0.9f)));
            Write("Window_SkySun", Size, Size, Glare(FrostedSky(97), 0.5f, 0.55f, 0.6f, 0.8f, new Color(1f, 0.98f, 0.92f)));
            Write("Wood_Slab", WoodSize, WoodSize, Slab(new Color(0.52f, 0.32f, 0.17f), 0.30f, 83, WoodSize));
            Write("Wood_PlanksV", WoodSize, WoodSize, Transpose(Wood(6, new Color(0.40f, 0.24f, 0.12f), 0.26f, 91, WoodSize)));
            AssetDatabase.Refresh();
            Debug.Log("SortThem: room textures written to " + Folder);
        }

        static Func<int, int, Color> Wood(int planks, Color baseColor, float grainStrength, int seed, int size)
        {
            return (x, y) =>
            {
                float u = x / (float)size, v = y / (float)size;
                float plankF = v * planks;
                int plank = Mathf.FloorToInt(plankF);
                float inPlank = plankF - plank;

                int plankId = Mod(plank, planks);
                float tone = (Hash(plankId, 0, seed) - 0.5f) * 0.30f;
                float shift = Hash(plankId, 1, seed) * 10f;

                float grain = Tiled(u, v, 4, 24, seed + plankId * 13, 4, 0.55f);
                float rings = Mathf.Sin((inPlank * 9f + grain * 3.5f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float fibre = Tiled(u, v, 48, 96, seed + 5, 3, 0.5f);

                float k = 1f + tone;
                k *= 1f - grainStrength * (rings * 0.55f + grain * 0.45f);
                k *= 0.94f + fibre * 0.12f;

                float gloss = 0.72f + (fibre - 0.5f) * 0.35f - rings * grainStrength * 0.9f + tone * 0.6f;

                float edge = Mathf.Min(inPlank, 1f - inPlank);
                if (edge < 0.03f)
                {
                    float t = edge / 0.03f;
                    k *= Mathf.Lerp(0.45f, 1f, t);
                    gloss *= Mathf.Lerp(0.2f, 1f, t);
                }
                else if (edge < 0.06f) k *= Mathf.Lerp(1.05f, 1f, (edge - 0.03f) / 0.03f);

                float joint = Hash(plankId, 3, seed);
                float du = Mathf.Abs(Frac(u - joint) - 0.5f);
                if (du > 0.496f)
                {
                    float t = (0.5f - du) / 0.004f;
                    k *= Mathf.Lerp(0.5f, 1f, t);
                    gloss *= Mathf.Lerp(0.2f, 1f, t);
                }

                return new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, Mathf.Clamp01(gloss));
            };
        }

        static Func<int, int, Color> Transpose(Func<int, int, Color> fn) => (x, y) => fn(y, x);

        static Func<int, int, Color> Glare(Func<int, int, Color> fn, float cx, float cy, float radius, float strength, Color tint)
        {
            return (x, y) =>
            {
                float u = x / (float)Size, v = y / (float)Size;
                var c = fn(x, y);
                float d = Mathf.Abs(v - cy) / radius;
                float glow = Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f) * strength;
                float lift = 0.12f * strength;
                c = Color.Lerp(c, tint, Mathf.Clamp01(glow + lift));
                return new Color(c.r, c.g, c.b, 1f);
            };
        }

        static Func<int, int, Color> Slab(Color baseColor, float grainStrength, int seed, int size)
        {
            return (x, y) =>
            {
                float u = x / (float)size, v = y / (float)size;
                float wander = Tiled(u, v, 2, 3, seed + 2, 3, 0.6f);
                float grain = Tiled(u, v, 3, 24, seed, 4, 0.55f);
                float rings = Mathf.Sin((v * 7f + wander * 2.5f + grain * 1.8f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float fibre = Tiled(u, v, 32, 128, seed + 5, 3, 0.5f);
                float broad = Tiled(u, v, 2, 2, seed + 7, 2, 0.6f);

                float k = 1f + (broad - 0.5f) * 0.14f;
                k *= 1f - grainStrength * (rings * 0.5f + grain * 0.35f);
                k *= 0.92f + fibre * 0.17f;
                float gloss = 0.7f + (fibre - 0.5f) * 0.35f - rings * grainStrength * 0.9f + (broad - 0.5f) * 0.3f;
                return new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, Mathf.Clamp01(gloss));
            };
        }

        static Func<int, int, Color> Plaster(Color baseColor, int seed)
        {
            return (x, y) =>
            {
                float u = x / (float)Size, v = y / (float)Size;
                float broad = Fbm(u * 4f, v * 4f, 4, seed, 4, 0.6f);
                float grain = Fbm(u * 60f, v * 60f, 60, seed + 3, 2, 0.5f);
                float speck = Fbm(u * 150f, v * 150f, 150, seed + 9, 1, 0.5f);
                float k = 1f + (broad - 0.5f) * 0.16f + (grain - 0.5f) * 0.10f + (speck - 0.5f) * 0.05f;
                return new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, 1f);
            };
        }

        static Func<int, int, Color> Rug(Color dark, Color light, int cells, int seed)
        {
            var mid = Color.Lerp(dark, light, 0.45f);
            return (x, y) =>
            {
                float u = x / (float)Size, v = y / (float)Size;
                float fx = Frac(u * cells), fy = Frac(v * cells);
                bool bandX = fx < 0.5f, bandY = fy < 0.5f;
                var c = bandX && bandY ? dark : (bandX || bandY ? mid : light);

                float softX = Mathf.Min(Mathf.Abs(fx - 0.5f), Mathf.Min(fx, 1f - fx)) * cells;
                float softY = Mathf.Min(Mathf.Abs(fy - 0.5f), Mathf.Min(fy, 1f - fy)) * cells;
                float soft = Mathf.Clamp01(Mathf.Min(softX, softY) * 24f);
                c = Color.Lerp(Color.Lerp(dark, light, 0.5f), c, 0.35f + soft * 0.65f);

                float threadX = Mathf.Sin(u * Mathf.PI * 2f * 128f) * 0.5f + 0.5f;
                float threadY = Mathf.Sin(v * Mathf.PI * 2f * 128f) * 0.5f + 0.5f;
                bool overUnder = ((Mathf.FloorToInt(u * 128f) + Mathf.FloorToInt(v * 128f)) & 1) == 0;
                float weave = overUnder ? threadX : threadY;
                float noise = Fbm(u * 90f, v * 90f, 90, seed, 2, 0.5f);
                float k = 0.88f + weave * 0.16f + (noise - 0.5f) * 0.10f;
                return new Color(c.r * k, c.g * k, c.b * k, 1f);
            };
        }

        static Func<int, int, Color> FrostedWindow(int seed)
        {
            var city = Bake(Size, Size, Window(seed));
            Blur(city, Size, Size, 11, 3);
            return (x, y) =>
            {
                float u = x / (float)Size;
                var c = city[y * Size + x];
                float speck = Tiled(u, y / (float)Size, 130, 130, seed + 4, 1, 0.5f);
                float k = 0.98f + speck * 0.04f;
                float glow = Mathf.Clamp01((y / (float)Size - 0.60f) / 0.40f);
                c = Color.Lerp(c, new Color(1f, 0.98f, 0.93f), glow * 0.35f);
                return new Color(c.r * k, c.g * k, c.b * k, 1f);
            };
        }

        static Func<int, int, Color> FrostedSky(int seed)
        {
            var sky = Bake(Size, Size, Sky(seed));
            Blur(sky, Size, Size, 7, 2);
            return (x, y) =>
            {
                var c = sky[y * Size + x];
                float speck = Tiled(x / (float)Size, y / (float)Size, 120, 120, seed + 4, 1, 0.5f);
                float k = 0.985f + speck * 0.03f;
                return new Color(c.r * k, c.g * k, c.b * k, 1f);
            };
        }

        static Func<int, int, Color> Sky(int seed)
        {
            var high = new Color(0.36f, 0.58f, 0.86f);
            var low = new Color(0.80f, 0.88f, 0.95f);
            var warm = new Color(1f, 0.96f, 0.88f);
            return (x, y) =>
            {
                float u = x / (float)Size, v = y / (float)Size;
                var c = Color.Lerp(low, high, Mathf.Pow(v, 0.75f));
                c = Color.Lerp(c, warm, Mathf.Clamp01((0.28f - v) / 0.28f) * 0.55f);

                return c;
            };
        }

        static Color[] Bake(int w, int h, Func<int, int, Color> fn)
        {
            var buf = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) buf[y * w + x] = fn(x, y);
            return buf;
        }

        static void Blur(Color[] buf, int w, int h, int radius, int passes)
        {
            var tmp = new Color[buf.Length];
            for (int p = 0; p < passes; p++)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color sum = Color.clear;
                        for (int i = -radius; i <= radius; i++) sum += buf[y * w + Mod(x + i, w)];
                        tmp[y * w + x] = sum / (radius * 2 + 1);
                    }
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color sum = Color.clear;
                        for (int i = -radius; i <= radius; i++) sum += tmp[Mod(y + i, h) * w + x];
                        buf[y * w + x] = sum / (radius * 2 + 1);
                    }
            }
        }

        static Func<int, int, Color> Window(int seed)
        {
            var sky = new Color(0.62f, 0.76f, 0.92f);
            var warm = new Color(1f, 0.95f, 0.84f);
            var ground = new Color(0.62f, 0.60f, 0.57f);
            var foliage = new Color(0.42f, 0.56f, 0.33f);
            var buildings = new Color[12];
            var tops = new float[12];
            var widths = new float[12];
            float cursor = 0f;
            for (int i = 0; i < 12; i++)
            {
                float t = Hash(i, 7, seed);
                buildings[i] = Color.Lerp(new Color(0.58f, 0.50f, 0.44f), new Color(0.88f, 0.80f, 0.68f), t);
                tops[i] = 0.42f + Hash(i, 13, seed) * 0.28f;
                widths[i] = 0.06f + Hash(i, 19, seed) * 0.06f;
                cursor += widths[i];
            }
            float scale = 1f / cursor;
            for (int i = 0; i < 12; i++) widths[i] *= scale;

            return (x, y) =>
            {
                float u = x / (float)Size, v = y / (float)Size;
                var c = Color.Lerp(warm, sky, Mathf.Clamp01((v - 0.35f) / 0.65f));

                float acc = 0f;
                for (int i = 0; i < 12; i++)
                {
                    float next = acc + widths[i];
                    if (u >= acc && u < next)
                    {
                        if (v < tops[i]) c = buildings[i];
                        break;
                    }
                    acc = next;
                }
                float canopy = 0.20f + Tiled(u, 0.1f, 5, 1, seed + 11, 3, 0.6f) * 0.16f;
                if (v < canopy) c = Color.Lerp(c, foliage, Mathf.Clamp01((canopy - v) / 0.10f) * 0.85f);
                if (v < 0.10f) c = Color.Lerp(c, ground, Mathf.Clamp01((0.10f - v) / 0.10f));

                float haze = Tiled(u, v, 6, 6, seed + 2, 3, 0.6f);
                c = Color.Lerp(c, warm, 0.22f + (haze - 0.5f) * 0.16f);

                return c;
            };
        }

        static void Write(string name, int w, int h, Func<int, int, Color> fn)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = fn(x, y);
                    px[y * w + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            string path = Folder + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = true;
                imp.mipMapBias = -0.25f;
                imp.anisoLevel = 4;
                imp.maxTextureSize = Mathf.Max(w, h);
                imp.alphaIsTransparency = false;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.crunchedCompression = true;
                imp.compressionQuality = 60;
                imp.SaveAndReimport();
            }
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263 + seed * 1274126177;
                n = (n ^ (n >> 13)) * 1274126177;
                n = n ^ (n >> 16);
                return (n & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;

        static float Frac(float v) => v - Mathf.Floor(v);

        static float Value(float x, float y, int periodX, int periodY, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int xa = Mod(x0, periodX), xb = Mod(x0 + 1, periodX);
            int ya = Mod(y0, periodY), yb = Mod(y0 + 1, periodY);
            float a = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Fbm(float x, float y, int period, int seed, int octaves, float gain) => Fbm2(x, y, period, period, seed, octaves, gain);

        static float Fbm2(float x, float y, int periodX, int periodY, int seed, int octaves, float gain)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int px = periodX, py = periodY;
            float fx = x, fy = y;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(fx, fy, px, py, seed + i * 17) * amp;
                norm += amp;
                amp *= gain;
                fx *= 2f; fy *= 2f; px *= 2; py *= 2;
            }
            return sum / norm;
        }

        static float Tiled(float u, float v, int fx, int fy, int seed, int octaves, float gain) => Fbm2(u * fx, v * fy, fx, fy, seed, octaves, gain);
    }
}
