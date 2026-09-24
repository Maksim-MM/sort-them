using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SortThem.Web
{
    public static class WebLocales
    {
        static readonly HashSet<string> Codes = new HashSet<string> { "en", "ru" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (!LocalizationSettings.HasSettings) return;
            LocalizationSettings.InitializationOperation.Completed += _ => Filter();
        }

        static void Filter()
        {
            var available = LocalizationSettings.AvailableLocales;
            if (available == null) return;
            available.Locales.RemoveAll(l => l == null || !Codes.Contains(l.Identifier.Code));
            var selected = LocalizationSettings.SelectedLocale;
            if (selected != null && Codes.Contains(selected.Identifier.Code)) return;
            var lang = Application.systemLanguage;
            bool ru = lang == SystemLanguage.Russian || lang == SystemLanguage.Ukrainian || lang == SystemLanguage.Belarusian;
            Locale fallback = available.GetLocale(ru ? "ru" : "en");
            if (fallback != null) LocalizationSettings.SelectedLocale = fallback;
        }
    }
}
