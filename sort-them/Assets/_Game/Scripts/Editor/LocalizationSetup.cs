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
            LocUtil.Set("ui.collectibles", "Канистры", "Canisters");
            LocUtil.Set("ui.terminal", "Терминал улучшений", "Upgrade Terminal");
            LocUtil.Set("ui.close", "Закрыть", "Close");
            LocUtil.Set("ui.buy", "Купить", "Buy");
            LocUtil.Set("ui.max", "Макс.", "Max");
            LocUtil.Set("ui.pause", "Пауза", "Pause");
            LocUtil.Set("ui.resume", "Продолжить", "Resume");
            LocUtil.Set("ui.save", "Сохранить", "Save");
            LocUtil.Set("ui.saved", "Сохранено", "Saved");
            LocUtil.Set("ui.unstuck", "Вернуть застрявшие машинки", "Return stuck cars");
            LocUtil.Set("ui.newgame", "Сбросить прогресс", "Reset progress");
            LocUtil.Set("ui.shuffle", "Перемешать кучу", "Shuffle the pile");
            LocUtil.Set("ui.shuffling", "Перемешиваем кучу…", "Shuffling the pile…");
            LocUtil.Set("ui.settings", "Настройки", "Settings");
            LocUtil.Set("ui.music", "Музыка", "Music");
            LocUtil.Set("ui.sfx", "Эффекты", "Effects");
            LocUtil.Set("ui.sens_x", "Чувствительность по горизонтали", "Horizontal sensitivity");
            LocUtil.Set("ui.sens_y", "Чувствительность по вертикали", "Vertical sensitivity");
            LocUtil.Set("ui.vibration", "Вибрация", "Vibration");
            LocUtil.Set("ui.on", "Вкл", "On");
            LocUtil.Set("ui.off", "Выкл", "Off");
            LocUtil.Set("ui.back", "Назад", "Back");
            LocUtil.Set("ui.language", "Язык", "Language");
            LocUtil.Set("ui.controls", "Управление", "Controls");
            LocUtil.Set("ctl.move", "Передвижение", "Move");
            LocUtil.Set("ctl.look", "Обзор", "Look around");
            LocUtil.Set("ctl.jump", "Прыжок", "Jump");
            LocUtil.Set("ctl.sprint", "Бег", "Sprint");
            LocUtil.Set("ctl.crouch", "Присед", "Crouch");
            LocUtil.Set("ctl.interact", "Взять", "Take");
            LocUtil.Set("ctl.place", "Поставить на полку или бросить", "Place on a shelf or throw");
            LocUtil.Set("ctl.select", "Выбор предмета в руках", "Select item in hands");
            LocUtil.Set("ctl.wheel_key", "Колесо мыши", "Mouse wheel");
            LocUtil.Set("ctl.mouse_key", "Мышь", "Mouse");
            LocUtil.Set("ctl.ability1", "Поиск совпадений", "Find matches");
            LocUtil.Set("ctl.ability2", "Автосбор совпадений", "Auto-collect");
            LocUtil.Set("ctl.ability3", "Подсветка стеллажа", "Rack highlight");
            LocUtil.Set("ctl.pause", "Пауза", "Pause");
            LocUtil.Set("tut.walk", "{0} — походить", "{0} — walk around");
            LocUtil.Set("tut.walk_touch", "Стик слева — походить", "Left stick — walk around");
            LocUtil.Set("tut.look", "{0} — повертеть камерой", "{0} — look around");
            LocUtil.Set("tut.look_touch", "Правая половина экрана — повертеть камерой", "Right half of the screen — look around");
            LocUtil.Set("tut.take", "{0} — взять подсвеченную машинку", "{0} — pick up the highlighted car");
            LocUtil.Set("tut.take_touch", "Кнопка «взять» — подсвеченная машинка", "Take button — the highlighted car");
            LocUtil.Set("tut.place", "{0} — поставить на подсвеченный стеллаж", "{0} — place it on the highlighted rack");
            LocUtil.Set("tut.place_touch", "Кнопка «поставить» — подсвеченный стеллаж", "Place button — the highlighted rack");
            LocUtil.Set("ui.hint", "{0} взять · {1} поставить/бросить · {2} выбрать · {3} меню", "{0} take · {1} place/throw · {2} select · {3} menu");
            LocUtil.Set("msg.ability_not_ready", "Способность ещё не готова", "Ability is not ready yet");
            LocUtil.Set("msg.need_item_in_hands", "Возьми что-нибудь в руки", "Take something in your hands");
            LocUtil.Set("msg.collectible_found", "Канистра найдена: {0}/{1}", "Canister found: {0}/{1}");
            LocUtil.Set("msg.radio_track", "Радио: {0}/{1}", "Radio: {0}/{1}");
            LocUtil.Set("ui.slot", "Слот-машина", "Slot Machine");
            LocUtil.Set("ui.spin", "Крутить · ${0}", "Spin · ${0}");
            LocUtil.Set("ui.slot_remaining", "Осталось наград: {0}", "Rewards left: {0}");
            LocUtil.Set("ui.slot_empty", "Пусто. Все награды выданы", "Empty. All rewards are out");
            LocUtil.Set("ui.slot_empty_bombs", "Награды закончились. Каждый спин даёт бомбу", "Rewards are out. Every spin drops a bomb");
            LocUtil.Set("ui.slot_bomb", "Бомба!", "Bomb!");
            LocUtil.Set("msg.bomb_spawned", "Из автомата выпала бомба, ищи в куче", "A bomb dropped from the machine, find it in the pile");
            LocUtil.Set("ui.slot_idle", "Каждое вращение даёт награду", "Every spin gives a reward");
            LocUtil.Set("ui.slot_bonus_cd", "откат ×{0}", "cooldown ×{0}");
            LocUtil.Set("ui.slot_bonus_radius", "радиус ×{0}", "radius ×{0}");
            LocUtil.Set("ui.slot_bonus_duration", "действие ×{0}", "duration ×{0}");
            LocUtil.Set("msg.slot_reward", "Выпало: {0}", "You got: {0}");
            LocUtil.Set("msg.register_paid", "Касса: +${0}", "Register: +${0}");
            LocUtil.Set("msg.rotate_device", "Поверните телефон горизонтально", "Rotate your phone to landscape");
        }
    }
}
