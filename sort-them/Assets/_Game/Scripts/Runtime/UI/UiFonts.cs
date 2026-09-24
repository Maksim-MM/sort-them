using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SortThem
{
    public static class Fonts
    {
        public static TMP_FontAsset Current { get; private set; }

        static readonly List<TMP_Text> Texts = new List<TMP_Text>();
        static string _code;

        public static void Register(TMP_Text t)
        {
            Texts.Add(t);
            if (Current != null) t.font = Current;
        }

        public static void Apply(string localeCode)
        {
            string code = localeCode ?? "";
            if (code == _code) return;
            _code = code;
            var next = Load(code);
            if (next == Current) return;
            Current = next;
            var target = next != null ? next : TMP_Settings.defaultFontAsset;
            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                var t = Texts[i];
                if (t == null) { Texts.RemoveAt(i); continue; }
                t.font = target;
                UiFactory.ReapplyGlow(t);
            }
            Resources.UnloadUnusedAssets();
        }

        static TMP_FontAsset Load(string code)
        {
            string name = code switch
            {
                "ja" => "NotoSansJP SDF",
                "ko" => "NotoSansKR SDF",
                "zh" => "NotoSansSC SDF",
                "zh-Hant" => "NotoSansTC SDF",
                _ => null,
            };
            if (name == null) return null;
            var fa = Resources.Load<TMP_FontAsset>("Fonts/" + name);
            if (fa == null) Debug.LogWarning("SortThem: font not found Resources/Fonts/" + name);
            return fa;
        }
    }
}
