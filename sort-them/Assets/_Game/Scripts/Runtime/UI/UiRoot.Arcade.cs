using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public partial class UiRoot
    {
        struct ArcadeWindow
        {
            public RectTransform Root, Marquee, Screen, Panel;
            public TMP_Text Title;
        }

        const float ArcadeMarqueeTop = 40f, ArcadeMarqueeHeight = 84f, ArcadeGap = 18f, ArcadeSide = 48f;
        const float ArcadeBalanceHeight = 62f, ArcadePanelBottom = 36f, ArcadePanelHeight = 90f, ArcadeScreenBottom = 40f;
        const float ArcadeScreenTop = ArcadeMarqueeTop + ArcadeMarqueeHeight + ArcadeGap;
        const float ArcadeScreenTopWithBalance = ArcadeScreenTop + ArcadeBalanceHeight + ArcadeGap + 4f;
        const float ArcadeScreenBottomWithPanel = ArcadePanelBottom + ArcadePanelHeight + ArcadeGap;
        const float ArcadeButtonHeight = 62f, ArcadeButtonInset = 24f;

        readonly Dictionary<Selectable, Color> _arcadeIdle = new Dictionary<Selectable, Color>();
        readonly Dictionary<Selectable, Image> _arcadeFrames = new Dictionary<Selectable, Image>();

        static float ArcadeHeight(float screenHeight, bool balance, bool panel) =>
            (balance ? ArcadeScreenTopWithBalance : ArcadeScreenTop) + screenHeight + (panel ? ArcadeScreenBottomWithPanel : ArcadeScreenBottom);

        ArcadeWindow BuildArcadeWindow(string name, Vector2 size, string titleKey, string titleFallback, bool balance, bool panel)
        {
            var w = new ArcadeWindow();
            w.Root = UiFactory.Panel(transform, name, ArcadeBody);
            UiFactory.Anchored(w.Root, new Vector2(0.5f, 0.5f), Vector2.zero, size);

            w.Marquee = UiFactory.NeonBox(w.Root, "Marquee", ArcadeNeon, new Color(0.05f, 0.07f, 0.11f, 1f), 4f);
            var marqueeRoot = (RectTransform)w.Marquee.parent;
            UiFactory.Anchor(marqueeRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(ArcadeSide, -(ArcadeMarqueeTop + ArcadeMarqueeHeight)), new Vector2(-ArcadeSide, -ArcadeMarqueeTop));
            UiFactory.Glow(marqueeRoot, 24f, 0.5f);
            w.Title = Bind(UiFactory.Text(w.Marquee, "Title", "", 22f, TextAlignmentOptions.Center, ArcadeTitle), titleKey, titleFallback);
            w.Title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            w.Title.enableAutoSizing = true;
            w.Title.fontSizeMin = 14f;
            w.Title.fontSizeMax = 22f;
            w.Title.margin = new Vector4(16f, 0f, 16f, 0f);
            w.Title.characterSpacing = 6f;
            UiFactory.TextGlow(w.Title, new Color(ArcadeTitle.r, ArcadeTitle.g, ArcadeTitle.b, 0.85f), 0.3f, 0.6f);
            UiFactory.Anchor(w.Title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            float top = balance ? ArcadeScreenTopWithBalance : ArcadeScreenTop;
            float bottom = panel ? ArcadeScreenBottomWithPanel : ArcadeScreenBottom;
            w.Screen = UiFactory.NeonBox(w.Root, "Screen", ArcadeNeon, ArcadeScreen, 4f);
            var screenRoot = (RectTransform)w.Screen.parent;
            UiFactory.Anchor(screenRoot, Vector2.zero, Vector2.one, new Vector2(ArcadeSide, bottom), new Vector2(-ArcadeSide, -top));
            UiFactory.Glow(screenRoot, 22f, 0.4f);

            if (panel)
            {
                w.Panel = UiFactory.Rect(w.Root, "ControlPanel");
                UiFactory.Anchor(w.Panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(ArcadeSide, ArcadePanelBottom), new Vector2(-ArcadeSide, ArcadePanelBottom + ArcadePanelHeight));
            }

            w.Root.gameObject.AddComponent<UiCrtPower>();
            return w;
        }

        TMP_Text BuildBalanceBox(RectTransform root)
        {
            var box = UiFactory.NeonBox(root, "Balance", new Color(0.35f, 0.35f, 0.40f, 1f), Color.black, 3f);
            UiFactory.Anchored((RectTransform)box.parent, new Vector2(0.5f, 1f), new Vector2(0f, -ArcadeScreenTop), new Vector2(250f, ArcadeBalanceHeight));
            var label = Bind(UiFactory.Text(box, "Label", "", 16f, TextAlignmentOptions.Center, new Color(1f, 0.34f, 0.69f, 1f)), "ui.balance", "Баланс");
            label.fontStyle = FontStyles.UpperCase;
            UiFactory.TextGlow(label, new Color(1f, 0.34f, 0.69f, 0.7f), 0.2f, 0.45f);
            UiFactory.Anchor(label.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var value = UiFactory.Text(box, "Value", "$0", 34f, TextAlignmentOptions.Center, new Color(1f, 0.23f, 0.18f, 1f));
            value.fontStyle = FontStyles.Bold;
            UiFactory.TextGlow(value, new Color(1f, 0.25f, 0.15f, 0.8f), 0.25f, 0.5f);
            UiFactory.Anchor(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero);
            return value;
        }

        static void FinishArcadeScreen(RectTransform screen)
        {
            UiFactory.Vignette(screen, 0.45f);
            UiFactory.Scanlines(screen, 0.16f);
        }

        static Color ArcadeFill(Color border)
        {
            if (border == ArcadeCost) return new Color(0.14f, 0.10f, 0.04f, 1f);
            if (border == ArcadeRed) return new Color(0.16f, 0.05f, 0.04f, 1f);
            return new Color(0.06f, 0.12f, 0.16f, 1f);
        }

        Button ArcadeButton(Transform parent, string name, Action onClick, Color color, float fontSize = 18f, bool bold = false)
        {
            var b = UiFactory.NeonButton(parent, name, "", onClick, color, ArcadeFill(color), color, fontSize);
            var label = b.GetComponentInChildren<TMP_Text>();
            if (bold) label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            UiFactory.TextGlow(label, new Color(color.r, color.g, color.b, 0.6f), 0.2f, 0.5f);
            UiFactory.Glow(b.GetComponent<RectTransform>(), 16f, 0.5f);
            _arcadeIdle[b] = color;
            return b;
        }

        Button ArcadePanelButton(ArcadeWindow w, string name, Action onClick, Color color, float width, bool right, float fontSize = 18f, bool bold = false)
        {
            var b = ArcadeButton(w.Panel, name, onClick, color, fontSize, bold);
            UiFactory.Anchored(b.GetComponent<RectTransform>(), new Vector2(right ? 1f : 0f, 0.5f), new Vector2(right ? -ArcadeButtonInset : ArcadeButtonInset, 0f), new Vector2(width, ArcadeButtonHeight));
            return b;
        }

        RectTransform ArcadeRow(Transform parent, string name, string labelKey, string labelFallback, float height)
        {
            var fill = UiFactory.NeonBox(parent, name, ArcadeNeon, new Color(0.06f, 0.10f, 0.14f, 1f), 2f);
            UiFactory.Size(fill.parent as RectTransform, 0f, height);
            var label = Bind(UiFactory.Text(fill, "Label", "", 18f, TextAlignmentOptions.Left, Color.white), labelKey, labelFallback);
            label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            label.enableAutoSizing = true;
            label.fontSizeMax = 18f;
            label.fontSizeMin = 13f;
            UiFactory.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.58f, 1f), new Vector2(16f, 0f), Vector2.zero);
            return fill;
        }

        void RefreshArcadeFocus()
        {
            foreach (var kv in _arcadeIdle) Paint(kv.Key, kv.Value);
            foreach (var kv in _arcadeFrames)
            {
                var want = kv.Key == _focused ? ArcadeFocus : ArcadeNeon;
                if (kv.Value.color != want) kv.Value.color = want;
            }
        }
    }
}
