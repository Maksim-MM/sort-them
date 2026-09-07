using System;
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
            var bg = Image(rt, "Background", null, new Color(1f, 1f, 1f, 0.15f), UnityEngine.UI.Image.Type.Simple);
            bg.raycastTarget = true;
            Anchor(bg.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            var fillArea = Rect(rt, "FillArea");
            Anchor(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            var fill = Image(fillArea, "Fill", null, new Color(0.3f, 0.6f, 1f, 1f), UnityEngine.UI.Image.Type.Simple);
            Anchor(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleArea = Rect(rt, "HandleArea");
            Anchor(handleArea, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = Image(handleArea, "Handle", null, Color.white, UnityEngine.UI.Image.Type.Simple);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(20f, -16f);
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
