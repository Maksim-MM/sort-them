using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public static class UiFactory
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Anchored(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, Image.Type type)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.raycastTarget = false;
            return img;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick, float fontSize = 22f)
        {
            var rt = Panel(parent, name, new Color(0.2f, 0.45f, 0.8f, 1f));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.6f, 1f, 1f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.6f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            btn.colors = colors;
            var t = Text(rt, "Label", label, fontSize, TextAlignmentOptions.Center, Color.white);
            Anchor(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btn.onClick.AddListener(() => { var gm = GameManager.I; if (gm != null) Sfx.PlayUi(gm.Config.UiClickClip); Rumble.UiClick(); });
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Slider Slider(Transform parent, string name, float min, float max, float value)
        {
            var rt = Rect(parent, name);
            var bg = Image(rt, "Background", null, new Color(0.25f, 0.88f, 1f, 0.18f), UnityEngine.UI.Image.Type.Simple);
            bg.raycastTarget = true;
            Anchor(bg.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            var fillArea = Rect(rt, "FillArea");
            Anchor(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            var fill = Image(fillArea, "Fill", null, new Color(0.25f, 0.88f, 1f, 1f), UnityEngine.UI.Image.Type.Simple);
            Anchor(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleArea = Rect(rt, "HandleArea");
            Anchor(handleArea, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = Image(handleArea, "Handle", null, new Color(0.93f, 0.97f, 1f, 1f), UnityEngine.UI.Image.Type.Simple);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(14f, -20f);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            return slider;
        }

        public static RectTransform NeonBox(Transform parent, string name, Color border, Color fill, float thickness = 3f)
        {
            var outer = Panel(parent, name, border);
            var inner = Panel(outer, "Fill", fill);
            Anchor(inner, Vector2.zero, Vector2.one, new Vector2(thickness, thickness), new Vector2(-thickness, -thickness));
            return inner;
        }

        public static Button NeonButton(Transform parent, string name, string label, Action onClick, Color border, Color fill, Color textColor, float fontSize = 20f)
        {
            var outer = Panel(parent, name, border);
            var btn = outer.gameObject.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            var inner = Panel(outer, "Fill", fill);
            Anchor(inner, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            inner.GetComponent<Image>().raycastTarget = false;
            var t = Text(inner, "Label", label, fontSize, TextAlignmentOptions.Center, textColor);
            Anchor(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => { var gm = GameManager.I; if (gm != null) Sfx.PlayUi(gm.Config.UiClickClip); Rumble.UiClick(); });
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        static readonly Dictionary<(Material, Color), Material> GlowMaterials = new Dictionary<(Material, Color), Material>();
        static Sprite _glowSprite, _vignetteSprite;
        static Texture2D _scanTexture;

        public static void TextGlow(TMP_Text t, Color glow, float dilate = 0.25f, float softness = 0.55f)
        {
            var key = (t.fontSharedMaterial, glow);
            if (!GlowMaterials.TryGetValue(key, out var mat))
            {
                mat = new Material(t.fontSharedMaterial) { name = t.fontSharedMaterial.name + " Glow" };
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, glow);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
                GlowMaterials.Add(key, mat);
            }
            t.fontSharedMaterial = mat;
        }

        static Sprite GlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            const int size = 64, border = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "UiGlow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, border - x, x - (size - 1 - border)) / border;
                float dy = Mathf.Max(0f, border - y, y - (size - 1 - border)) / border;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a *= a;
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _glowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _glowSprite;
        }

        public static Image Glow(RectTransform target, float spread = 18f, float alpha = 0.55f)
        {
            var img = Image(target.parent, target.name + "Glow", GlowSprite(), Color.white, UnityEngine.UI.Image.Type.Sliced);
            img.pixelsPerUnitMultiplier = 24f / spread;
            img.rectTransform.SetSiblingIndex(target.GetSiblingIndex());
            img.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var glow = img.gameObject.AddComponent<UiGlow>();
            glow.Source = target.GetComponent<Image>();
            glow.Target = target;
            glow.Spread = spread;
            glow.Alpha = alpha;
            glow.Follow();
            return img;
        }

        public static RawImage Scanlines(RectTransform area, float alpha = 0.18f, float period = 3f)
        {
            if (_scanTexture == null)
            {
                _scanTexture = new Texture2D(1, 3, TextureFormat.RGBA32, false) { name = "UiScanlines", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                _scanTexture.SetPixels32(new[] { new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0) });
                _scanTexture.Apply();
            }
            var rt = Rect(area, "Scanlines");
            Anchor(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = _scanTexture;
            raw.color = new Color(0f, 0f, 0f, alpha);
            raw.raycastTarget = false;
            rt.gameObject.AddComponent<UiScanlines>().Period = period;
            rt.SetAsLastSibling();
            return raw;
        }

        public static Image Vignette(RectTransform area, float alpha = 0.5f)
        {
            if (_vignetteSprite == null)
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "UiVignette", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v) / 1.41421f;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, r));
                    px[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply();
                _vignetteSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            var img = Image(area, "Vignette", _vignetteSprite, new Color(1f, 1f, 1f, alpha), UnityEngine.UI.Image.Type.Simple);
            Anchor(img.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            img.rectTransform.SetAsLastSibling();
            return img;
        }

        public static void Layout(RectTransform rt, float spacing, RectOffset padding, bool vertical = true)
        {
            HorizontalOrVerticalLayoutGroup g = vertical ? rt.gameObject.AddComponent<VerticalLayoutGroup>() : rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding;
            g.childForceExpandHeight = !vertical;
            g.childForceExpandWidth = vertical;
            g.childControlHeight = true;
            g.childControlWidth = true;
        }

        public static LayoutElement Size(Component c, float minWidth, float minHeight, float flex = 0f)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minWidth;
            le.minHeight = minHeight;
            le.preferredHeight = minHeight;
            le.flexibleWidth = flex;
            return le;
        }
    }
}
