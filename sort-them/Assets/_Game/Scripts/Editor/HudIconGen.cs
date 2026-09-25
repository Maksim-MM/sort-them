using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class HudIconGen
    {
        const int Size = 256;
        const float Stroke = 24f;
        const float Thin = 18f;

        struct Seg { public Vector2 A, B; public float W; }
        struct Ring { public Vector2 C; public float R, W; }
        struct Dot { public Vector2 C; public float R; }

        class Shape
        {
            public readonly List<Seg> Segs = new List<Seg>();
            public readonly List<Ring> Rings = new List<Ring>();
            public readonly List<Dot> Dots = new List<Dot>();

            public void Line(float x0, float y0, float x1, float y1, float w = Stroke) => Segs.Add(new Seg { A = new Vector2(x0, y0), B = new Vector2(x1, y1), W = w });

            public void Poly(float w, params float[] xy)
            {
                for (int i = 0; i + 3 < xy.Length; i += 2) Line(xy[i], xy[i + 1], xy[i + 2], xy[i + 3], w);
            }

            public void Circle(float cx, float cy, float r, float w = Stroke) => Rings.Add(new Ring { C = new Vector2(cx, cy), R = r, W = w });
            public void Fill(float cx, float cy, float r) => Dots.Add(new Dot { C = new Vector2(cx, cy), R = r });

            public float Distance(Vector2 p)
            {
                float d = float.MaxValue;
                foreach (var s in Segs)
                {
                    var ab = s.B - s.A;
                    float t = Mathf.Clamp01(Vector2.Dot(p - s.A, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    d = Mathf.Min(d, Vector2.Distance(p, s.A + ab * t) - s.W * 0.5f);
                }
                foreach (var r in Rings) d = Mathf.Min(d, Mathf.Abs(Vector2.Distance(p, r.C) - r.R) - r.W * 0.5f);
                foreach (var o in Dots) d = Mathf.Min(d, Vector2.Distance(p, o.C) - o.R);
                return d;
            }
        }

        [MenuItem("SortThem/3f. Generate HUD Icons")]
        public static void Generate()
        {
            Save(UiSpriteSetup.StatIcons[0], Car());
            Save(UiSpriteSetup.StatIcons[1], Shelves());
            Save(UiSpriteSetup.StatIcons[2], Crate());
            AssetDatabase.Refresh();
            UiSpriteSetup.ImportStatIcons();
            Debug.Log("SortThem: HUD icons generated");
        }

        static Shape Car()
        {
            var s = new Shape();
            s.Poly(Stroke, 24, 176, 24, 132, 64, 132, 98, 90, 162, 90, 194, 132, 232, 132, 232, 176, 206, 176);
            s.Line(24, 176, 50, 176);
            s.Line(110, 176, 146, 176);
            s.Line(130, 90, 130, 132, Thin);
            s.Circle(80, 178, 24);
            s.Circle(176, 178, 24);
            return s;
        }

        static Shape Shelves()
        {
            var s = new Shape();
            s.Line(52, 28, 52, 224);
            s.Line(204, 28, 204, 224);
            s.Line(40, 28, 216, 28);
            s.Line(40, 84, 216, 84);
            s.Line(40, 140, 216, 140);
            s.Line(40, 196, 216, 196);
            return s;
        }

        static Shape Crate()
        {
            var s = new Shape();
            s.Poly(Stroke, 44, 100, 196, 100, 196, 216, 44, 216, 44, 100);
            s.Poly(Stroke, 44, 100, 84, 60, 236, 60, 196, 100);
            s.Line(236, 60, 236, 176);
            s.Line(236, 176, 196, 216);
            s.Line(44, 100, 196, 216, Thin);
            s.Line(44, 216, 196, 100, Thin);
            return s;
        }

        static void Save(string path, Shape shape)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2(x + 0.5f, Size - (y + 0.5f));
                float a = Mathf.Clamp01(0.5f - shape.Distance(p));
                px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
