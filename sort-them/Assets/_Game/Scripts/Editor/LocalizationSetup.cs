using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;

namespace SortThem.Editor
{
    public static class LocalizationSetup
    {
        [MenuItem("SortThem/1. Localization Setup")]
        public static void Setup()
        {
            EditorAssets.EnsureFolder(Paths.Localization);

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "LocalizationSettings";
                AssetDatabase.CreateAsset(settings, Paths.Localization + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            EnsureLocale(SystemLanguage.English, "en");
            EnsureLocale(SystemLanguage.Russian, "ru");

            var selectors = settings.GetStartupLocaleSelectors();
            if (selectors.Count == 0)
            {
                selectors.Add(new SystemLocaleSelector());
                selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("en") });
            }
            if (settings.GetAvailableLocales() == null) settings.SetAvailableLocales(new LocalesProvider());
            if (settings.GetStringDatabase() == null) settings.SetStringDatabase(new LocalizedStringDatabase());
            if (settings.GetAssetDatabase() == null) settings.SetAssetDatabase(new LocalizedAssetDatabase());
            EditorUtility.SetDirty(settings);

            var coll = LocalizationEditorSettings.GetStringTableCollection(LocUtil.TableName);
            if (coll == null)
            {
                EditorAssets.EnsureFolder(Paths.Localization + "/Tables");
                coll = LocalizationEditorSettings.CreateStringTableCollection(LocUtil.TableName, Paths.Localization + "/Tables");
            }
            if (!coll.SharedData.Metadata.HasMetadata<PreloadAssetTableMetadata>())
            {
                coll.SharedData.Metadata.AddMetadata(new PreloadAssetTableMetadata { Behaviour = PreloadAssetTableMetadata.PreloadBehaviour.PreloadAll });
                EditorUtility.SetDirty(coll.SharedData);
            }

            SetUiStrings();

            var aa = AddressableAssetSettingsDefaultObject.Settings;
            if (aa != null)
            {
                aa.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
                EditorUtility.SetDirty(aa);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("SortThem: localization ready");
        }

        static void EnsureLocale(SystemLanguage language, string code)
        {
            if (LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code)) != null) return;
            var locale = Locale.CreateLocale(language);
            AssetDatabase.CreateAsset(locale, Paths.Localization + "/Locale_" + code + ".asset");
            LocalizationEditorSettings.AddLocale(locale);
        }

        static void SetUiStrings()
        {
            LocUtil.Set("ui.cars", "Машинки", "Cars");
            LocUtil.Set("ui.shelves", "Полки", "Shelves");
            LocUtil.Set("ui.collectibles", "Коллекция", "Collectibles");
            LocUtil.Set("ui.terminal", "Терминал улучшений", "Upgrade Terminal");
            LocUtil.Set("ui.close", "Закрыть", "Close");
            LocUtil.Set("ui.buy", "Купить", "Buy");
            LocUtil.Set("ui.max", "Макс.", "Max");
            LocUtil.Set("ui.pause", "Пауза", "Pause");
            LocUtil.Set("ui.resume", "Продолжить", "Resume");
            LocUtil.Set("ui.save", "Сохранить", "Save");
            LocUtil.Set("ui.saved", "Сохранено", "Saved");
            LocUtil.Set("ui.unstuck", "Вернуть застрявшие машинки", "Return stuck cars");
            LocUtil.Set("ui.newgame", "Сбросить сохранение", "Reset save");
            LocUtil.Set("ui.settings", "Настройки", "Settings");
            LocUtil.Set("ui.music", "Музыка", "Music");
            LocUtil.Set("ui.sfx", "Эффекты", "Effects");
            LocUtil.Set("ui.sens_x", "Чувствительность по горизонтали", "Horizontal sensitivity");
            LocUtil.Set("ui.sens_y", "Чувствительность по вертикали", "Vertical sensitivity");
            LocUtil.Set("ui.vibration", "Вибрация", "Vibration");
            LocUtil.Set("ui.on", "Вкл", "On");
            LocUtil.Set("ui.off", "Выкл", "Off");
            LocUtil.Set("ui.back", "Назад", "Back");
            LocUtil.Set("ui.hint", "ЛКМ взять · ПКМ поставить/бросить · колесо выбрать · Esc меню", "LMB take · RMB place/throw · wheel select · Esc menu");
            LocUtil.Set("msg.ability_not_ready", "Способность ещё не готова", "Ability is not ready yet");
            LocUtil.Set("msg.need_item_in_hands", "Возьми что-нибудь в руки", "Take something in your hands");
        }
    }
}
