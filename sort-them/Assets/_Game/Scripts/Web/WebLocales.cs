using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SortThem.Web
{
    public static class WebLocales
    {
        static readonly HashSet<string> Codes = new HashSet<string> { "en", "ru", "es", "de", "fr", "it", "pt-BR", "tr", "pl" };

        class PlatformSelector : IStartupLocaleSelector
        {
            public Locale GetStartupLocale(ILocalesProvider availableLocales)
            {
                string code = PlatformCode();
                Locale locale = code != null ? availableLocales.GetLocale(code) : null;
                if (locale != null) return locale;
                var lang = Application.systemLanguage;
                bool ru = lang == SystemLanguage.Russian || lang == SystemLanguage.Ukrainian || lang == SystemLanguage.Belarusian;
                return availableLocales.GetLocale(ru ? "ru" : "en");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (!LocalizationSettings.HasSettings) return;
            LocalizationSettings.StartupLocaleSelectors.Insert(0, new PlatformSelector());
            LocalizationSettings.InitializationOperation.Completed += _ => Filter();
        }

        static void Filter()
        {
            var available = LocalizationSettings.AvailableLocales;
            if (available == null) return;
            available.Locales.RemoveAll(l => l == null || !Codes.Contains(l.Identifier.Code));
        }

        static string PlatformCode()
        {
            string lang;
            try { lang = Playgama.Bridge.platform.language; }
            catch { return null; }
            if (string.IsNullOrEmpty(lang)) return null;
            int cut = lang.IndexOfAny(new[] { '-', '_' });
            string code = (cut > 0 ? lang.Substring(0, cut) : lang).Trim().ToLowerInvariant();
            if (code == "pt") return "pt-BR";
            if (code == "uk" || code == "be") return "ru";
            return Codes.Contains(code) ? code : null;
        }
    }
}
