using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class IconAtlasSlicer
    {
        const int Gap = 28;
        const float Pad = 0.10f;

        public static List<RectInt> FindIcons(Texture2D tex, int minPixels = 200)
        {
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            var seen = new bool[w * h];
            var boxes = new List<RectInt>();
            var stack = new Stack<int>();
            for (int i = 0; i < px.Length; i++)
            {
                if (seen[i] || px[i].a < 8) continue;
                int minX = i % w, maxX = minX, minY = i / w, maxY = minY, count = 0;
                stack.Clear();
                stack.Push(i);
                seen[i] = true;
                while (stack.Count > 0)
                {
                    int p = stack.Pop();
                    int x = p % w, y = p / w;
                    count++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            int np = ny * w + nx;
                            if (seen[np] || px[np].a < 8) continue;
                            seen[np] = true;
                            stack.Push(np);
                        }
                }
                if (count >= minPixels) boxes.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }
            return Merge(boxes);
        }

        static List<RectInt> Merge(List<RectInt> boxes)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < boxes.Count && !changed; i++)
                    for (int j = i + 1; j < boxes.Count && !changed; j++)
                    {
                        var a = boxes[i];
                        var b = boxes[j];
                        var ea = new RectInt(a.x - Gap, a.y - Gap, a.width + Gap * 2, a.height + Gap * 2);
                        if (!ea.Overlaps(b)) continue;
                        int x0 = Mathf.Min(a.x, b.x), y0 = Mathf.Min(a.y, b.y);
                        int x1 = Mathf.Max(a.xMax, b.xMax), y1 = Mathf.Max(a.yMax, b.yMax);
                        boxes[i] = new RectInt(x0, y0, x1 - x0, y1 - y0);
                        boxes.RemoveAt(j);
                        changed = true;
                    }
            }
            boxes.Sort((a, b) => a.y != b.y ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));
            return boxes;
        }

        public static void Cut(Texture2D src, RectInt box, string path, int size = 256)
        {
            int side = Mathf.RoundToInt(Mathf.Max(box.width, box.height) * (1f + Pad * 2f));
            int cx = box.x + box.width / 2, cy = box.y + box.height / 2;
            int x0 = cx - side / 2, y0 = cy - side / 2;
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var out32 = new Color32[size * size];
            var srcPx = src.GetPixels32();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * side + x0;
                    float v = (y + 0.5f) / size * side + y0;
                    out32[y * size + x] = Sample(srcPx, src.width, src.height, u, v);
                }
            dst.SetPixels32(out32);
            dst.Apply();
            System.IO.File.WriteAllBytes(path, dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
        }

        static Color32 Sample(Color32[] px, int w, int h, float u, float v)
        {
            int x0 = Mathf.FloorToInt(u - 0.5f), y0 = Mathf.FloorToInt(v - 0.5f);
            float fx = u - 0.5f - x0, fy = v - 0.5f - y0;
            Color acc = Color.clear;
            for (int j = 0; j <= 1; j++)
                for (int i = 0; i <= 1; i++)
                {
                    int x = Mathf.Clamp(x0 + i, 0, w - 1), y = Mathf.Clamp(y0 + j, 0, h - 1);
                    float weight = (i == 0 ? 1f - fx : fx) * (j == 0 ? 1f - fy : fy);
                    if (x0 + i < 0 || y0 + j < 0 || x0 + i >= w || y0 + j >= h) continue;
                    Color c = px[y * w + x];
                    acc += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a) * weight;
                }
            if (acc.a > 0.001f) return new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a);
            return new Color(1f, 1f, 1f, 0f);
        }

        public static Texture2D LoadReadable(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            imp.textureType = TextureImporterType.Default;
            imp.isReadable = true;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
