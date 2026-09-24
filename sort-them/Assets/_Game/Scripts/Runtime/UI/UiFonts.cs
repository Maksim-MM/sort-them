using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SortThem
{
    public static class Fonts
    {
        public static TMP_FontAsset Current { get; private set; }

        static readonly List<TMP_Text> Texts = new List<TMP_Text>();
        static string _code;
        static AsyncOperationHandle<TMP_FontAsset> _handle;

        public static void Register(TMP_Text t)
        {
            if (t == null) return;
            if (!Texts.Contains(t)) Texts.Add(t);
            if (Current != null) t.font = Current;
        }

        public static void Apply(string localeCode)
        {
            string code = localeCode ?? "";
            if (code == _code) return;
            _code = code;
            var prevHandle = _handle;
            var next = Load(code);
            bool changed = next != Current;
            if (changed)
            {
                Current = next;
                var target = next != null ? next : TMP_Settings.defaultFontAsset;
                for (int i = Texts.Count - 1; i >= 0; i--)
                {
                    var t = Texts[i];
                    if (t == null) { Texts.RemoveAt(i); continue; }
                    t.font = target;
                    UiFactory.ReapplyGlow(t);
                }
            }
            if (prevHandle.IsValid()) Addressables.Release(prevHandle);
            if (changed) Resources.UnloadUnusedAssets();
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
            _handle = default;
            if (name == null) return null;
            try
            {
                _handle = Addressables.LoadAssetAsync<TMP_FontAsset>(name);
                var fa = _handle.WaitForCompletion();
                if (fa == null) Debug.LogWarning("SortThem: font not found in Addressables " + name);
                return fa;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("SortThem: font load failed " + name + ": " + e.Message);
                _handle = default;
                return null;
            }
        }
    }
}
