# Upscale SDK v2.1 — Документация

> ✅ **Baseline: v2.1** (нумерация наша, внутренняя — вендор версии не нумерует; v2 = переписанное поколение SDK, v2.1 = его апдейт 2026-07). Написано по вендорской доке v2 (сырьё — `UPSCALE_SDK_V2_SOURCE.md`, источник https://upscaleweevil.github.io/ups-docs/ — **на момент v2.1 сайт НЕ обновлён**), сверено с живым SDK: v2 — Сессия 1 (2026-06-15), v2.1 — 2026-07-14 (подифный аудит v2→v2.1 по git).
>
> **Дельта v2 → v2.1:**
> - **Breaking:** `BaseInputActionSet` переименован в `BaseGamepadActionSet` (состав действий идентичен, чистый rename).
> - **Клавиатура+мышь:** наборы `BaseKeyboardActionSet`/`BaseMouseInputActionSet`; определения-ассеты перешли на источники (`[SerializeReference] List<IInputSource<T>>` — одно действие слушает геймпад+клаву+мышь); builder: `.ToKey()/.ToMouseButton()/.ToWASD()/.ToArrows()/.ToMouseAxis()`; `DeviceType.Mouse`; `UPSInput.GetPrimaryDevice()`.
> - **Глифы device-aware:** `KeyboardGlyph`/`MouseGlyph`, `KeyboardIconsSet`/`MouseIconsSet`, `IGlyphProvider.TryProvide(sources)` и `action.TryGetGlyph()` сами выбирают спрайт под активное устройство.
> - **UI:** новые `ActionToggleElement`, `ActionScrollViewElement`, `ActionScrollbarElement`; дефолтные определения `UiButtonBase`/`UiButtonExtra`.
> - **Конфиг:** `_psSet`→`_ps5Set` + новые `_keyboardSet`/`_mouseSet`; наборы иконок разложены по папкам `PlayStation4`/`PlayStation5`/`Desktop` + Colored-варианты (см. «Конфигурация»).
> - **Сейвы:** на PS4/PS5 системные диалоги ошибок показываются автоматически (см. Saves); ядро `UPSSaves`/`UPSPlayerPrefs`/`AutoSaver` не менялось.
> - **Платформы:** модули PS4/PS5/Switch (+NintendoSDKPlugin)/Xbox теперь в комплекте пакета.
> - ⚠️ Вендор предупредил: на тесты v2.1 было меньше времени, чем обычно — при странностях сначала подозревать SDK и писать разработчику SDK.
>
> **Подтверждено живым кодом:** `UPSInput` (Subscribe/Unsubscribe, BindAsBool/Vector2/Float, SetActionState, Unbind, GetGamepadType, SetGamepadVibration/StopGamepadVibration, GetConnectedDevices, GetPrimaryDevice, TryGetGlyph, OnDeviceAdded/Removed); `BaseGamepadActionSet` (Apply/Cancel/Base/Extra/South/West/North/East/Select/Start/shoulders/stick-buttons/LeftStick/RightStick/X/Y/DPad+dirs/triggers); `BaseKeyboardActionSet` (WASD/Arrows Vector2 + все клавиши как `ButtonActionGroup`); `BaseMouseInputActionSet` (LeftClick/RightClick/MiddleClick, Scroll±, MouseDelta/MousePosition + поосно); `ButtonActionGroup.Press/Hold/Release`; `InputAction<T>.Read()/Performed/TryGetGlyph()/Dispose()`; `UPSSaves.Prefs/Saver/IsReady`; `UPSPlayerPrefs` (Set/Get Int/String/Float/Bool, HasKey, DeleteKey, TrySave, IsReady, OnReady); UI: `UILayersManager`(static)/`Layer`/`LayerNavigation`/`LayerNavigator`/`UIElement`/`ActionButtonElement`/`ActionSliderElement`/`ActionToggleElement`/`ActionScrollViewElement`/`ActionScrollbarElement`/`ActivateLayerTrigger`; `ConfigurationProvider` грузит `Resources.Load("UpscaleSDK/Config")`; `RuntimeBootstrap` инициализирует Core через `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`.
>
> **Уточнения по живому коду (не из вендор-доки):**
> - **Namespace UI — `UpscaleSDK.Core.Ui.*`** (именно `Ui`, не `UI`). Дерево: `UpscaleSDK.Core.{Input,Saves,Ui,Utils,Configuration,Validator}`, `UpscaleSDK.Platform.Desktop`, `UpscaleSDK.BootstrapScene`.
> - **Опечатка «Platfrom» — во ВСЕХ платформенных asmdef v2.1:** `UpscaleSDK.Platfrom.{Desktop,PS4,PS5,Switch,Xbox}`; namespace при этом корректные (`UpscaleSDK.Platform.Desktop`, `Plugins.UpscaleSDK.*` — см. Platform). Core-asmdef — `UpscaleSDK.Core`.
> - Доп. API, которых нет в таблице ниже: `UPSInput.Get<T>(...)` → `ActionResult<T>` (и перегрузка от `InputAction<T>`); `UPSInput.TryGetGlyphProvider()` → `IGlyphProvider`.
> - Стартовый переход: сцена `UPSBootstrap` через `UPSInitializeAwaiter` делает `SceneManager.LoadSceneAsync(1)` после `UPSSaves.Prefs.IsReady` + сплэш-анимации. Без ассета `Config` по жёсткому пути `Resources/UpscaleSDK/Config` `ConfigurationProvider.Initialize()` вернёт false и игра **зависнет в bootstrap-сцене** (детали и поля конфига — раздел «Конфигурация (ассеты SDK)»).
> - Валидатор требует ровно `Application.companyName == "Upscale Studio"` и версию по маске `X.Y.Z`.
>
> **На Stage 6** ещё сверить с живым кодом: define-символы, адаптивные триггеры PS5 (в API v2.1 не видны), трофеи/активности в деле. Платформенные модули SDK с v2.1 в комплекте (`Assets/UpscaleSDK/Platform/`), но Unity-пакеты платформ (`com.unity.*.ps5` и т.п.) по-прежнему ставятся отдельно на сборочной машине.

Внутренний SDK Upscale Studio: абстрагирует платформо-специфику для шиппинга на Desktop (Win/macOS/Linux), Xbox (Series/One), PlayStation (PS4/PS5), Nintendo Switch. Путь в проекте: `Assets/UpscaleSDK/`.

Корневой namespace — `UpscaleSDK`. Архитектура v2: модули **Core** (Input, Saves, UI, Configuration, Utils, Validator) + **Platform** (Desktop/Xbox/PS4/PS5/Switch). Bootstrap через `RuntimeBootstrap` + сцена `UPSBootstrap`.

---

## ⚠️ Главное для порта (читать первым)

1. **v2 построен на Unity New Input System** — это требование, валидатор его проверяет. Legacy Input Manager не поддерживается. Если проект на Legacy `Input.GetKey` — миграция на New Input System обязательна (Stage 2).
2. **Зависимость Newtonsoft.Json** (`gjrf`) — для сериализации сейвов.
3. **Поддерживаемые Unity:** 6000.3.12f и 6000.2.10f (на 6000.x статус 🧪 — works in limited testing). 2023/2022 — экспериментально.
4. **Bootstrap-сцена `UPSBootstrap` должна быть первой** в Build Settings.
5. Ввод/UI: action sets + bindings (`UPSInput`/`BaseGamepadActionSet`; с v2.1 также `BaseKeyboardActionSet`/`BaseMouseInputActionSet`) и UI-слои (`Layer`/`UILayersManager` + `ActionButtonElement` поверх UGUI Button) — кнопки не подписываются на ввод вручную, действие идёт через `BoolActionDefinition`.

## Краткий справочник API v2 по областям

| Область | v2 |
|---|---|
| Ввод (чтение, геймпад) | `BaseGamepadActionSet.Instance.RightStick.Read()`, `…Apply.Press.Performed += …` / `UPSInput.Subscribe<T>` |
| Ввод (чтение, клавиатура) | `BaseKeyboardActionSet.Instance.KeyE.Press.Read()`, `.WASD.Read()`, `.Arrows.Read()` |
| Ввод (чтение, мышь) | `BaseMouseInputActionSet.Instance.LeftClick.Hold.Read()`, `.Scroll.Read()`, `.MouseDelta.Read()` |
| Ввод (модель) | action sets (`InputActionsSet<T>`), bindings (`BindAsBool/Float/Vector2 → To… → Complete`); определения-ассеты = список источников `IInputSource<T>` (геймпад+клава+мышь в одном действии), `InputAction<T>.Performed` |
| Кнопки enum | `GamepadButton` (ButtonSouth/West/North/East) + `GamepadAction` (Apply/Cancel/Base/Extra) + `ButtonTrigger` (Press/Hold/Release); kb/mouse: `KeyboardKey`, `MouseButton`, `Mouse1DAxis`/`Mouse2DAxis`, `Keyboard2DAxis` |
| Тип геймпада | `UPSInput.GetGamepadType()` → `GamepadType` (None/Nintendo/PlayStation/Xbox) |
| Активное устройство | `UPSInput.GetPrimaryDevice()` → `DeviceType` (Gamepad/Keyboard/Mouse); на консольном билде всегда Gamepad, на ПК приоритет геймпад→клава→мышь |
| Ремап | пересборка binding'а (builder), `UPSInput.Unbind` + новый bind |
| Глифы | `GamepadIconsSet`/`KeyboardIconsSet`/`MouseIconsSet` (в `UpscaleSDKConfig`), `GlyphProvider`; **device-aware:** `action.TryGetGlyph()` / `TryGetGlyphProvider().TryProvide(sources)` сами выбирают спрайт под `GetPrimaryDevice()`; точечно — `UPSInput.TryGetGlyph(GamepadGlyph)` |
| Вибрация | `UPSInput.SetGamepadVibration(low, high)` / `UPSInput.StopGamepadVibration()` |
| Light Bar / LED PS-пада | API в SDK нет → напрямую Unity Input System `DualShockGamepad.SetLightBarColor(Color)` / `DualSenseGamepadHID` (заготовка `LightBarService`, см. `STAGE-2-5`) |
| Адаптивные триггеры PS5 | в вендор-доке v2 не описано (добрать из API Reference / живого SDK на Stage 6) |
| Сейвы | `UPSSaves.Prefs.SetFloat/GetInt/…`, `UPSSaves.Prefs.TrySave()`, `UPSSaves.IsReady`/`Prefs.IsReady`, `SavesSettings` |
| Слоты сейвов | `UPSSaves.Saver.Save("Slot")/Load("Slot")` (advanced, async) |
| Автосейв | `AutoSaver` (интервал в `SavesSettings`) |
| UI-кнопка | `ActionButtonElement` (поверх UGUI Button) + `BoolActionDefinition` |
| UI-тогл | `ActionToggleElement` (поверх UGUI Toggle, **с v2.1 штатный**) + `BoolActionDefinition`; флипает `isOn` |
| UI-скролл | `ActionScrollViewElement` (ScrollRect) / `ActionScrollbarElement` (Scrollbar) + `Vector2ActionDefinition` + `_stepAmount` (v2.1) |
| UI-слой | `Layer` (Key) + `UILayersManager.ActivateLayer(...)`; стека/приоритетов нет |
| UI-навигация | `LayerNavigation` (граф `Transition`) + `LayerNavigator` |
| Слайдер | `ActionSliderElement` + `Vector2ActionDefinition` |
| Активация слоя | `ActivateLayerTrigger` (TriggerType) или `UILayersManager.ActivateLayer` |
| Lifecycle/Bootstrap | `RuntimeBootstrap` + сцена `UPSBootstrap`; `PlatformProvider`/`Platform` |
| Конфиг | один `UpscaleSDKConfig` (`Resources/UpscaleSDK/Config`) — поля, пути, выбор глифов: см. «Конфигурация (ассеты SDK)» |
| Трофеи/ачивки | `Ps4Trophies.Unlock(id)`, `Ps5Trophies.Unlock(id)`, `XboxAchievements.Unlock(id)` |
| Активности PS5 | `Ps5Activities.StartActivity(id)` / `FinishActivity(id, ActivityResult)` |
| Лог | `UPSLogger` (Utils) |
| Define-символы | платформа выбирается build-config'ом + платформенными пакетами; явные define не задокументированы (сверить на Stage 6) |

---

## Установка и bootstrap

1. Newtonsoft.Json: Package Manager → Add package by name → `com.unity.nuget.newtonsoft-json`.
2. Импорт `UpscaleSDK-Core.unitypackage` (Bootstrap-сцена + Core-модули + Desktop). Затем платформенные `.unitypackage` под цели. Опционально `Samples`/`Extras`.
3. Создать/найти `UpscaleSDKConfig` с именем **`Config`** в `Resources/UpscaleSDK`. Заполнить: ссылку на `SavesSettings`, 4 набора глифов `GamepadIconsSet` под платформы + `KeyboardIconsSet`/`MouseIconsSet` (v2.1). Подробно (поля, пути, готовые наборы) — раздел «Конфигурация (ассеты SDK)» ниже.
4. Добавить `Assets/UpscaleSDK/BootstrapScene` (`UPSBootstrap`) **первой** сценой в Build Settings.
5. Tools → UpscaleSDK → Validate перед сборкой.

Инициализация Core (Saves и т.д.) — автоматически через `RuntimeBootstrap` после готовности `Platform`.

## Конфигурация (ассеты SDK)

SDK конфигурируется тремя `ScriptableObject`-ассетами. **Перед Stage 2 (геймпад) проверить их наличие и спросить разработчика, если чего-то нет** — см. пре-флайт в `STAGE-2-5-GAMEPAD-UI.md`.

**Процедура (ИИ выполняет САМ, как только разработчик импортировал SDK — не дожидаясь Stage 2; конфиги без действий не появляются):**
1. **Корневой конфиг.** Проверить `UpscaleSDKConfig` по жёсткому пути `Resources/UpscaleSDK/Config.asset` (имя ровно `Config`). Нет — создать. Программно: `ScriptableObject.CreateInstance(<тип>)` + `AssetDatabase.CreateAsset`; поля заполнять через `SerializedObject.FindProperty(...)` — они приватные `[SerializeField]`. (Ассет можно класть в любую `Resources/UpscaleSDK/` — напр. `Assets/UpscaleSDK/Resources/UpscaleSDK/Config.asset`, чтобы не мусорить в игровые `Resources`.)
2. **Конфиг сохранений.** Найти/создать `SavesSettings` (если нет — `Create → Upscale SDK/Saves/Settings` с дефолтами из таблицы) и привязать в `_savesSettings`. Готовый обычно лежит в `Assets/UpscaleSDK/Extras/SaveSettings.asset`.
3. **Наборы глифов.** Привязать 6 наборов: геймпадные `_ps5Set`, `_ps4Set`, `_xboxSet`, `_switchSet` (`GamepadIconsSet`) + `_keyboardSet` (`KeyboardIconsSet`) и `_mouseSet` (`MouseIconsSet`). Готовые — `Assets/UpscaleSDK/Extras/Glyphs/`: `PlayStation5/Ps5 Outline IconsSet`, `PlayStation4/Ps4 Outline IconsSet`, `Xbox/Xbox Outline IconsSet`, `Switch/Switch Outline IconsSet`, `Desktop/White/{KeyboardIconsSet, MouseIconsSet}`; у консольных есть и `Colored`-варианты (Outline/Colored — выбор стиля, любой валиден). **⚠️ PS4 и PS5 — разные наборы** (с v2.1 в разных папках).
   > ⚠️ **Миграция конфига v2 → v2.1:** поле `_psSet` переименовано в `_ps5Set` — сериализованное значение старого конфига молча теряется; PS4-набор в v2.1 — **новый ассет с новым GUID** (старая ссылка станет Missing). После обновления SDK открыть `Config` и перепривязать оба поля + заполнить `_keyboardSet`/`_mouseSet`.
4. **Если какого-то ассета/набора не хватает** (нет набора под платформу, нет `SavesSettings`, нет спрайтов) — **НЕ выдумывать и не дублировать чужой набор**: спросить разработчика, какой ассет использовать / где его взять / нужна ли вообще эта платформа.
5. **Проверить в Play:** ушла ли `[UPS-CORE] Can't load upscale configuration`; на объекте `[UPSRuntime]` появились `ConfigurationProvider` + `Platform` (напр. `DesktopPlatform`) + `UPSInput` + `UPSSaves` без новых ошибок.

> ⚠️ Создание конфига **запускает** дальнейший bootstrap (`UPSInput`/`UPSSaves`/`Platform`) — это фактически старт интеграции SDK. Без конфига `RuntimeBootstrap` лишь логирует `[UPS-CORE] Can't load upscale configuration` и **аккуратно останавливается** (безвредно, но SDK не поднимается).

| Ассет | Класс / где исходник | Где лежит (ожидаемый путь) | Create-меню | Ключевые поля | Нужен для |
|---|---|---|---|---|---|
| **Корневой конфиг** | `UpscaleSDKConfig` (`Core/Configuration/UpscaleSDKConfig.cs`) | `Assets/Resources/UpscaleSDK/Config.asset` (имя ровно `Config`) | `Create → Upscale SDK/Config` | `_savesSettings` + `_ps5Set/_ps4Set/_xboxSet/_switchSet` (4× `GamepadIconsSet`) + `_keyboardSet`/`_mouseSet` (v2.1) | bootstrap, сейвы, глифы |
| **Настройки сейвов** | `SavesSettings` (`Core/Saves/Runtime/Settings/SavesSettings.cs`) | привязан в `Config` (обычно `Resources/UpscaleSDK/SaveSettings.asset`) | `Create → Upscale SDK/Saves/Settings` | `EnableAutosaves`(true), `SecondsBetweenAutosaves`(300), `SaveFileExtension`("txt"), `ProjectFolderName`("UpscaleSaves"), `InitializeSystemOnAwake`(true), `SaveSize`(PS, МБ), `XboxContainerName` | Stage 6 (сейвы) |
| **Набор глифов геймпада** (×4, по платформе) | `GamepadIconsSet` (`Core/Input/Runtime/Glyphs/GamepadIconsSet.cs`, эдитор в `Core/Input/Editor/Glyphs/`) | привязаны в `Config`; готовые — `Assets/UpscaleSDK/Extras/Glyphs/{PlayStation4,PlayStation5,Xbox,Switch}/…` (Outline + Colored) | `Create → Upscale SDK/Gamepad Icons Set` | `List<{GamepadGlyph Glyph, Sprite Sprite}>`, доступ `GetGlyph(GamepadGlyph)` | Stage 5 (глифы) |
| **Наборы клавиатуры/мыши** (v2.1) | `KeyboardIconsSet` / `MouseIconsSet` (`Core/Input/Runtime/Glyphs/`) | привязаны в `Config`; готовые — `Assets/UpscaleSDK/Extras/Glyphs/Desktop/White/` | `Create → Upscale SDK/…` | аналогично, ключи `KeyboardGlyph` (вкл. композиты WASD/Arrows) / `MouseGlyph` | Stage 5 (kb/mouse-подсказки) |

**⚠️ Путь Config зашит жёстко.** `ConfigurationProvider.LoadConfiguration()` делает `Resources.Load<UpscaleSDKConfig>("UpscaleSDK/Config")` — имя обязано быть `Config`, лежать в `Resources/UpscaleSDK/`. Не нашёл → `ConfigurationProvider.Initialize()` вернёт false → игра **зависнет в bootstrap-сцене**. `GetConfiguration()` в эдиторе грузит ассет напрямую даже без живого инстанса.

**Выбор набора глифов — `UpscaleSDKConfig.ProvideGamepadSet(GamepadType)`:** на консольной сборке по define-ам (`#if UNITY_PS5 / UNITY_PS4 / UNITY_SWITCH / UNITY_GAMECORE_XBOXSERIES / UNITY_GAMECORE_XBOXONE && !UNITY_EDITOR`); иначе (редактор/ПК) — switch по `UPSInput.GetGamepadType()`: `PlayStation→_ps5Set`, `Nintendo→_switchSet`, `Xbox→_xboxSet`, дефолт `→_ps5Set`. Клавиатура/мышь — `ProvideKeyboardSet()`/`ProvideMouseSet()` без платформенной логики. (В исходнике закомментирован `_simulateGamepadType` — хук для форс-симуляции типа пада в эдиторе, при необходимости можно раскомментировать.)

**Готовые наборы глифов не рисовать с нуля** — в `Assets/UpscaleSDK/Extras/Glyphs/` уже лежат заполненные наборы: Outline и Colored для PS4/PS5/Xbox/Switch (~23 глифа каждый) + White-наборы клавиатуры и мыши в `Desktop/White/`. Их достаточно привязать в `Config`.

> 📍 Пример (этот проект, v2.1): `Assets/UpscaleSDK/Resources/UpscaleSDK/Config.asset` (любая `Resources/UpscaleSDK/` подходит — `Resources.Load` ищет во всех). При апдейте SDK до v2.1 (2026-07-14) конфиг перепривязан: `_ps5Set`=`Ps5 Outline IconsSet` (бывший `Ps Outline`, GUID тот же — но поле новое), `_ps4Set`=`Ps4 Outline IconsSet` (новый GUID!), `_xboxSet`/`_switchSet` — прежние Outline, `_keyboardSet`/`_mouseSet` = `Desktop/White/`. Проверено в Play: `[UPSRuntime]` поднял DesktopPlatform/Input/Saves без ошибок.

> ⚠️ **Подводный камень: вставка `UPSBootstrap` сценой 0 сдвигает все индексы билда на +1.** Любой захардкоженный `SceneManager.LoadScene(int)` / `LoadManager.LoadScene(int)` в игре теперь грузит не ту сцену (Play Button → не та сцена, перезагрузка может прийтись на саму себя и дать `MissingReferenceException` на уничтоженных scene-объектах в `DontDestroyOnLoad`-менеджерах). После добавления bootstrap — грепни `LoadScene(` / `buildIndex` по `Assets/Scripts` и поправь абсолютные индексы (+1). Относительные (`GetActiveScene().buildIndex`) не трогать. Bootstrap-`Awaiter` сам грузит **индекс 1** — первая игровая сцена обязана остаться на 1.
> 📍 Пример: `MainMenu` 1→2, `EndingManager` 2→3, `PauseMenu` 0→1.

## Input

Strongly-typed, action-based поверх Unity New Input System.

- `InputAction<T>` — именованный ввод (T = bool/float/Vector2), событие `Performed`, метод `Read()`, `Dispose()`.
- `ButtonActionGroup` — `.Press` / `.Hold` / `.Release` (каждый — `InputAction<bool>`).
- `InputActionsSet<T>` — набор; `.Instance`, `.Activate()`, `.Deactivate()`, `.IsActive`, `.Dispose()`.
- `BaseGamepadActionSet.Instance` — готовый набор (до v2.1 назывался `BaseInputActionSet`): face raw `South/West/North/East`, mapped `Apply/Cancel/Base/Extra`, `Start/Select`, `LeftShoulder/RightShoulder/LeftStickButton/RightStickButton`, `DpadUp/Down/Left/Right`, `LeftTrigger/RightTrigger` (float), `LeftStickX/Y`,`RightStickX/Y` (float), `LeftStick/RightStick/DPad` (Vector2).
- `BaseKeyboardActionSet.Instance` (v2.1) — готовый клавиатурный набор: композиты `WASD`/`Arrows` (Vector2) + каждая клавиша как `ButtonActionGroup` (`KeyE`, `Space`, `Escape`, модификаторы, стрелки, цифры, F1–F12, символы).
- `BaseMouseInputActionSet.Instance` (v2.1) — готовый мышиный набор: `LeftClick/RightClick/MiddleClick` (`ButtonActionGroup`), `Scroll/ScrollUp/ScrollDown` (float), `MouseDelta/MousePosition` (Vector2, есть поосные float-варианты).

Чтение:
```csharp
bool held   = BaseGamepadActionSet.Instance.South.Hold.Read();
Vector2 mv  = BaseGamepadActionSet.Instance.LeftStick.Read();
float trig  = BaseGamepadActionSet.Instance.RightTrigger.Read();
// клавиатура/мышь (v2.1):
bool e      = BaseKeyboardActionSet.Instance.KeyE.Press.Read();
Vector2 wasd = BaseKeyboardActionSet.Instance.WASD.Read();
bool lmb    = BaseMouseInputActionSet.Instance.LeftClick.Press.Read();
// событие:
BaseGamepadActionSet.Instance.Apply.Press.Performed += OnApply;   // -= в OnDisable
```

Кастомный набор (рекоменд. способ для геймплея):
```csharp
public sealed class PlayerInputActionSet : InputActionsSet<PlayerInputActionSet>
{
    public InputAction<bool> Interact { get; private set; }
    public InputAction<Vector2> Move { get; private set; }
    protected override void Initialize(InputSetBuilder builder)
    {
        Interact = builder.BindAsBool("Interact").ToAction(GamepadAction.Apply, ButtonTrigger.Press).Complete();
        Move = builder.BindAsVector2("Move").ToLeftStick().WithDeadzone(0.15f).Complete();
    }
}
PlayerInputActionSet.Instance.Activate();   // Deactivate()/Dispose() по необходимости
```

Builder: `BindAsBool|Float|Vector2(name) → To…(…) → WithProcessor/WithDeadzone/WithInvert… → Complete()`. **Несколько `To…` подряд = несколько источников одного действия** (например геймпад-кнопка + клавиша: сработает любой).
- Bool: `.ToButton(GamepadButton, ButtonTrigger)`, `.ToAction(GamepadAction, ButtonTrigger)`; v2.1: `.ToKey(KeyboardKey, ButtonTrigger)`, `.ToMouseButton(MouseButton, ButtonTrigger)`.
- Float: `.ToLeftTrigger()/.ToRightTrigger()/.ToXLeftStick()/…`, `.WithInvert()`; v2.1: `.ToMouseAxis(Mouse1DAxis)` (Scroll/Delta/Position поосно). Клавиатурных float-осей нет — только 2D-композиты.
- Vector2: `.ToLeftStick()/.ToRightStick()/.ToDpad()`, `.WithDeadzone(f)/.WithInvertX()/.WithInvertY()`; v2.1: `.ToWASD()/.ToArrows()/.ToMouseAxis(Mouse2DAxis)` (Delta/Position).

Static API:
```csharp
UPSInput.BindAsBool("Jump").ToButton(GamepadButton.ButtonSouth, ButtonTrigger.Press).Complete();
UPSInput.Subscribe<bool>("Jump", OnJump);  UPSInput.Unsubscribe<bool>("Jump", OnJump);
UPSInput.SetActionState("Jump", active:false);  UPSInput.Unbind("Jump");
GamepadType t = UPSInput.GetGamepadType();  DeviceType[] d = UPSInput.GetConnectedDevices();
DeviceType primary = UPSInput.GetPrimaryDevice();   // v2.1: консоль → всегда Gamepad; ПК: геймпад > клава > мышь
UPSInput.SetGamepadVibration(0.4f, 0.7f);  UPSInput.StopGamepadVibration();
UPSInput.OnDeviceAdded += dev => {};  UPSInput.OnDeviceRemoved += dev => {};
```

Enums: `GamepadButton` (ButtonSouth/West/North/East), `GamepadAction` (Apply/Cancel/Base/Extra), `ButtonTrigger` (Press/Hold/Release), `GamepadType`, `DeviceType` (Keyboard/Gamepad/**Mouse** — v2.1), `GamepadGlyph`; v2.1: `KeyboardKey`, `MouseButton`, `Mouse1DAxis`/`Mouse2DAxis`, `Keyboard2DAxis`, `KeyboardGlyph`/`MouseGlyph`.

> ⚠️ **Рамбл покрыт SDK (`UPSInput.SetGamepadVibration`), а Light Bar / LED PS-пада — НЕТ.** В `UPSInput` нет API подсветки. Держать вне SDK: напрямую Unity Input System `DualShockGamepad.SetLightBarColor(Color)` / `DualSenseGamepadHID` (под `#if UNITY_STANDALONE_WIN || UNITY_EDITOR` + PS-алиасы). Готовая заготовка-сервис и подводные камни — в `STAGE-2-5-GAMEPAD-UI.md` (`LightBarService`).

Глифы: `action.Press.TryGetGlyph()` → `Sprite` — **с v2.1 device-aware**: провайдер идёт по источникам биндинга и отдаёт спрайт источника, чей девайс = `GetPrimaryDevice()` (геймпад-глиф при паде, клавишу — на клавиатуре; фоллбэк — любой источник с глифом). Точечно по enum: `UPSInput.TryGetGlyph(GamepadGlyph.East)`; для kb/mouse — через `UPSInput.TryGetGlyphProvider().Provide(KeyboardGlyph.E / MouseGlyph.Left)`. Наборы: Create → Upscale SDK → … Icons Set, привязать в `UpscaleSDKConfig`.

Asset-based действия: `BoolActionDefinition`/`FloatActionDefinition`/`Vector2ActionDefinition` (ScriptableObject) → `.Build()` → `InputAction<T>` (не забыть `.Dispose()`). **С v2.1 определение = список источников** (`[SerializeReference] List<IInputSource<T>>` + processors, правится в инспекторе): в один ассет можно положить геймпад-кнопку И клавишу И кнопку мыши — действие сработает от любого, а глиф выберется по устройству. Источники аккумулируются при чтении (для bool — OR).

Маппинг Apply/Cancel/Base/Extra по платформам — см. таблицу в `UPSCALE_SDK_V2_SOURCE.md`.

## Глифы — система подсказок (Stage 5)

> Сверено с живым SDK v2.1 (2026-07) — устаревшего относительно доки не найдено; ниже уточнения из живого кода (внутренности `TryGetGlyph`, device-aware нюанс, сэмпл-виджеты).

**Слои данных.** `UpscaleSDKConfig` (`Resources/UpscaleSDK/Config`) → 6 наборов-`ScriptableObject`:
- геймпад: `_ps5Set` / `_ps4Set` / `_xboxSet` / `_switchSet` = `GamepadIconsSet`
- ПК: `_keyboardSet` = `KeyboardIconsSet`, `_mouseSet` = `MouseIconsSet`

Каждый набор = `List<{Glyph, Sprite}>` + `GetGlyph(glyph)` (линейный `FirstOrDefault`; в ассете `icons: - Glyph: <int>  Sprite: {…}`).

**Enum'ы глифов** (`Core/Input/Runtime/Glyphs/Enums/`, порядок = int в ассете):
- **`GamepadGlyph` (23):** North, East, South, West, StickLeft, StickRight, TriggerLeft, TriggerRight, LeftStickButton, RightStickButton, ShoulderLeft, ShoulderRight, Dpad, DPadUp, DPadDown, DPadLeft, DPadRight, Select, Start, StickLeftX, StickLeftY, StickRightX, StickRightY.
- **`KeyboardGlyph`:** A–Z, Digit0–9, F1–F12, Space/Enter/Escape/Tab/Backspace/Delete/Insert/Home/End, модификаторы, стрелки, пунктуация + композиты **WASD**, **Arrows**.
- **`MouseGlyph`:** Left/Right/Middle, Scroll/ScrollUp/ScrollDown, Move/MoveX/MoveY.

**Провайдер** `GlyphProvider : IGlyphProvider` (доступ: `UPSInput.TryGetGlyphProvider()`):
- `Provide(GamepadGlyph)` → `Config.ProvideGamepadSet(UPSInput.GetGamepadType()).GetGlyph(glyph)` (выбор платформенного набора — см. «Конфигурация», раздел `ProvideGamepadSet`).
- `Provide(KeyboardGlyph)` / `Provide(MouseGlyph)` → соответствующий набор.
- `TryProvide(sources)` → берёт источник, чей `Device == UPSInput.GetPrimaryDevice()`; если нет — первый `IGlyphSource` (fallback). Это и есть device-aware выбор.

**`InputAction<T>.TryGetGlyph()`** (`Core/Input/Runtime/Core/InputAction.cs`):
```csharp
public Sprite TryGetGlyph() => _glyphProvider.TryProvide(_binding.Sources);
```
> ⚠️ **Device-aware только при мульти-источниковом действии.** Готовые сеты девайс-специфичны: `BaseGamepadActionSet.Instance.Apply.Press.TryGetGlyph()` → **всегда геймпад-глиф** (fallback), `BaseKeyboardActionSet…` → клавиатурный. Авто-переключение пад↔клава в ОДНОМ значке требует определения (`BoolActionDefinition`) с источниками обоих устройств (→ `ActionButtonElement`).

**Готовые способы вывести подсказку:**
1. **`ActionButtonElement`** (`Core/Ui/Runtime`, `[RequireComponent(Button)]`) — полноценный device-aware prompt: `BoolActionDefinition` + `Image` (поля `showIcon`/`corner`/`offset`/`scale`). Спрайт = `glyphProvider.TryProvide(_definition.Sources)`, обновляется на `UPSInput.OnDeviceAdded/OnDeviceRemoved`, прячется если у действия нет источника под подключённое устройство; при срабатывании экшена дёргает `button.onClick` (подсказка + «нажми = клик»).
2. **Сэмплы `Samples/Input/Glyphs/`** — девайс-специфичные виджеты «один глиф» (эталон для лёгких подсказок):
   - `Gamepad/`: `GlyphForGamepadButton` (enum `GamepadButton`), `GlyphForGamepadAction` (`GamepadAction`), `GlyphForGamepad1DAxis`, `GlyphForGamepad2DAxis` + сцена `GamepadGlyphs.unity`.
   - `MouseAndKeyboard/`: `GlyphForKeyboardButton` / `GlyphForKeyboard2DAxis`, `GlyphForMouseButton` / `GlyphForMouse1DAxis` / `GlyphForMouse2DAxis` + `DesktopGlyphs.unity`.
   - Паттерн: `Image` = `transform.GetChild(0)`, опц. `TMP_Text`; в `Start` резолвят группу из `BaseGamepadActionSet`/`BaseKeyboardActionSet`, `image.sprite = group.Press.TryGetGlyph()`, подписка `Press/Release.Performed` → scale `0.8↔1` (фидбэк нажатия); axis-варианты печатают live `Read()`.

**Стили** (`Extras/Glyphs/`): PS4/PS5 — **Outline + Colored**; Xbox/Switch — Outline; `Desktop/White/` — keyboard+mouse. В нашем `Config` подключён **Outline** (+ White); Colored для PS доступен, если захотим сменить.

**Для наших подсказок (Stage 5):** навигаторы (`*GamepadNav`) читают `BaseGamepadActionSet` напрямую → проще всего лёгкий виджет а-ля `GlyphFor*`: `Image` + выбранная группа → `group.Press.TryGetGlyph()`, обновление на `OnDeviceAdded/Removed`. Кросс-девайс (пад↔клава) → `ActionButtonElement` + мульти-источниковый `BoolActionDefinition`. Оба требуют поднятого `UPSInput`/бутстрапа SDK (в код игры ещё не интегрирован → Stage 6 либо частичный запуск ради подсказок). Переключение устройства для подсказок брать из SDK (`GetPrimaryDevice()`/`OnDeviceAdded/Removed`), а не из нашего `InputDeviceTracker`.

> ⚠️ **ПРАВИЛО: на консолях глифы ВСЕГДА платформенные (геймпадные).** Клавиатурный/мышиный глиф (`E`, ЛКМ и т.п.) на консоли показываться НЕ должен — ни при холодном старте, ни при отключении/переподключении пада. Причина: наш `InputDeviceTracker.GamepadActive` — это латч по «последнему устройству» (пад→`true`, мышь→`false`), и он (а) стартует в `false` (до первого ввода мелькнул бы ПК-глиф), (б) не имеет обработчика дисконнекта. Поэтому в `InputDeviceTracker.Poll()` стоит гейт: под `UNITY_PS4 || UNITY_PS5 || UNITY_GAMECORE || UNITY_XBOXONE || UNITY_SWITCH` он безусловно выставляет `GamepadActive = true` и выходит (латч мышь/пад — только редактор/ПК, ветка `#else`). Сам выбор платформенного набора (PS4/PS5/Xbox/Switch) делает `UpscaleSDKConfig.ProvideGamepadSet(...)` по консольным define-ам — см. «Конфигурация», `ProvideGamepadSet`. На Stage 6 (SDK интегрирован) переключение устройства для подсказок брать из SDK (`GetPrimaryDevice()`), но правило «на консоли — только геймпадные глифы» остаётся.

### Как встраивать глифы в текст — наша реализация подсказок (обязательно к прочтению)

Своя система подсказок в `Assets/Scripts/Input/Prompts/` — device-aware «слово + глиф», не зависит от бутстрапа `UPSInput` (глифы берём из `Config` напрямую). Ниже — как она устроена и **как класть глиф в текст**, чтобы на новом проекте сразу понять паттерн.

**Компоненты:**
- **`GlyphResolver`** — отдаёт `Sprite` под устройство/платформу из `UpscaleSDKConfig` (`Resources.Load("UpscaleSDK/Config")`): `GetGamepadGlyph(GamepadGlyph)` (платформа через Unity InputSystem: DualShock→PS / XInput→Xbox / SwitchProController→Nintendo), `GetKeyboardGlyph(KeyboardGlyph)`, `GetMouseGlyph(MouseGlyph)`. Работает БЕЗ поднятого `UPSInput`.
- **`InputDeviceTracker.GamepadActive`** — есть подключённый пад → геймпадные глифы всегда; нет пада → клава/мышь; консоль (`#if`) → всегда пад (см. правило «на консолях глифы всегда платформенные»).
- **`PromptDef`** (`PromptDefs.cs`) — данные подсказки: `Id, Word, Kb, Pad, Mouse, PreferMouse, Pad2, HasPad2`. Конструкторы: `(id,word,kb,pad)`; `(id,word,mouse,pad)` — ПК-глиф мышь (`PreferMouse`); `(id,word,kb,pad,pad2)` — **два глифа** (пара L1/R1, ↑/↓, L2/R2). Все дефы — статические `readonly` в `PromptDefs`.
- **`PromptRow`** — рендер одной строки `[слово] + [Image глиф] (+ Image glyph2)`, глиф выбирается по `InputDeviceTracker` (пад/клава/мышь) и обновляется на смене устройства. `SetDim(bool)` — приглушение (alpha) для тутор-подсветки.
- **`PromptManager`** — `Show/Hide(def)` (правый контейнер), `ShowLeft/HideLeft` (левый), + расширенные строки (`ShowDelivery`/`ShowSpeed`, цена+иконка+время).

**Главный принцип: глиф — это отдельный `Image` (из `GlyphResolver`), а НЕ inline-`<sprite>` в TMP.**

**Два способа «глиф в тексте»:**
1. **Слово + глиф (угловые подсказки, бары):** префаб `PromptRow` = `HorizontalLayoutGroup [слово][глиф]`; инстанцируем и зовём `Set(def)`. Так сделаны все контекстные подсказки и бары (`ShopPromptBar`, `CustomizationPromptBar`, `ShirtCustomizationPromptBar`).
2. **Глиф ВНУТРИ фразы** (тутор «Use [глиф] to Look Around»): мини-ряд `[TMP «Use»][Image глиф][TMP «to Look Around»]` в `HorizontalLayoutGroup`; глиф device-aware через `GlyphResolver`, обновляется каждый кадр. Ряд кладётся поверх/вместо исходного текста и **повторяет его масштаб/шрифт** (пример: `TutorialManager.InstructionGlyphRow` — `localScale 0.175`, шрифт `Rubik SemiBold`, чтобы совпасть с `TutorialInfo_txt`). Текст пузыря на этом шаге очищаем (`instructionInfoText.text=""`), т.к. `SetActive` перебивает Animator.

**Почему НЕ TMP inline `<sprite>`:** геймпадные глифы разные под платформы (PS/Xbox/Switch) — статические теги спрайтов не переключаются по платформе, пришлось бы печь все варианты в TMP Sprite Asset (атлас) и выбирать тег по платформе (много возни). Наши `Image`-глифы приходят прямо из наборов SDK → корректны под платформу бесплатно. **Компромисс:** `HorizontalLayoutGroup` — одна строка, фраза с глифом **не переносится** посередине. Если нужен именно переносящийся текст с иконкой внутри — тогда оправдан TMP Sprite Asset + `<sprite>` (с печью per-platform).

**Два глифа (пары):** `PromptDef` 5-арг конструктор (`kb, pad, pad2`); `PromptRow` рисует `glyph2` — важно гасить сам `GameObject` глифа (не только `Image.enabled`), иначе его `LayoutElement` занимает место в раскладке.

**Мышиный глиф (ЛКМ / движение мыши):** `MouseGlyph` + конструктор `PreferMouse`; на ПК `PromptRow` берёт мышь вместо клавиши.

**Пульсация/подсветка (тутор):** пульс — `localScale = 1 + Mathf.Sin(Time.unscaledTime*speed)*amp` на трансформе строки; неактивные строки — `SetDim(true)` (alpha). Примеры: `ShopPromptBar.ApplyTutorialHighlight`, `DeliveryPromptRow.SetPulse`.

**Подводный камень — тайминг лейаута:** TMP на первом кадре ещё не сгенерил меш → `ContentSizeFitter`/`HorizontalLayoutGroup` меряют 0 → глифы наезжают. Фикс: `LayoutRebuilder.ForceRebuildLayoutImmediate(container)` при построении + ещё пару кадров (счётчик `_relayout`). См. `ShopPromptBar`.

**Чек-лист для похожей задачи на новом проекте:**
1. В `UpscaleSDKConfig` привязаны наборы глифов (4× `GamepadIconsSet` по платформам + `Keyboard`/`Mouse`) — см. «Конфигурация».
2. Скопировать `GlyphResolver`, `InputDeviceTracker`, `PromptDef`/`PromptDefs`, `PromptRow`, `PromptManager` (+ `PromptGate` для гейтинга слоёв).
3. Добавить `PromptDef` для нового действия (слово + нужные глифы: `kb`/`pad`/`mouse`/`pad2`).
4. Угловая подсказка → инстанс `PromptRow`; глиф в предложении → мини-ряд `[TMP][Image][TMP]` в масштабе/шрифте целевого текста + обновление глифа каждый кадр (смена устройства) + `ForceRebuildLayoutImmediate` (тайминг TMP).
5. На консоли — всегда платформенный глиф (гейт в `InputDeviceTracker`).

**Файлы:** `Assets/Scripts/Input/Prompts/{GlyphResolver, PromptDefs, PromptRow, PromptManager, PromptGate, DeliveryPromptRow}.cs`, `Assets/Scripts/Input/InputDeviceTracker.cs`; префабы `Assets/Prefabs/UI/{PromptRow, PromptRowLeft, DeliveryPromptRow}.prefab`; материал обводки текста `Assets/UI/Fonts/TmPro/Bebas Ui Prompt Outline.mat`.

## Saves

`UPSSaves.Prefs` (key/value, основной путь) и `UPSSaves.Saver` (слоты, advanced/async). Типы: int/float/string/bool. JSON (Newtonsoft).
```csharp
if (UPSSaves.Prefs.IsReady) {}      // или UPSSaves.Prefs.OnReady += () => {};
UPSSaves.Prefs.SetInt("level", 7);  UPSSaves.Prefs.SetBool("seen", true);
int lvl = UPSSaves.Prefs.GetInt("level", 1);
UPSSaves.Prefs.HasKey("level");  UPSSaves.Prefs.DeleteKey("level");
UPSSaves.Prefs.TrySave();           // в чекпоинтах; молча no-op если не готово
```
Явный сейв после каждого изменения не нужен — `AutoSaver` делает периодический flush (интервал в `SavesSettings`, по `Time.unscaledDeltaTime`). `Saver` (advanced): `Save("Slot")`, `Load("Slot")` (async), `StartSavesSearch()`, `CurrentSaveData`, события `ISaverEvents`. Проверять `UPSSaves.IsReady` перед `Saver`.

`SavesSettings` (Create → Upscale SDK → Saves → Settings): EnableAutosaves, SecondsBetweenAutosaves(300), SaveFileExtension("txt"), ProjectFolderName("UpscaleSaves"), SaveSize(PS4/PS5, МБ), XboxContainerName. FileSystem на платформу: Desktop/Xbox/PS4/PS5/Switch (см. Platform).

**Ошибки сейвов — с v2.1 на PS обрабатываются автоматически.** `PS4FileSystem`/`PS5FileSystem` при storage-ошибках сами открывают **системный диалог** платформы (`Unity.SaveData.*.Dialog`: NoSpaceContinuable — нет места, Corrupted — битые данные) — кастомный канвас не нужен (вендор: «посмотрим, что скажет сертификация»). Статический канал `UpscaleSDK.Core.Saves.ErrorHandling.ErrorHandler` (`OnError += msg=>…` / `RaiseError(string)`) остался, и PS-файловые системы **всё ещё зовут `RaiseError` после системного диалога** → подписанный кастомный попап покажет сообщение **вторым, дублем** — на PS его не вешать. На Xbox/Switch в v2.1 `RaiseError` не вызывается вообще, в Core/Desktop — тоже: канал фактически мёртв; сэмпл `Samples/Saves/PopupErrorHandler` (+`ErrorPopupPrefab`) остался в пакете, но нужен только если захочется своего UX поверх ошибок, поднимаемых собственным кодом. _(Паттерн Captive «своя копия `SaveErrorPopupHandler` в `UPSBootstrap`» — эпоха v2, в v2.1 не переносить.)_

## UI

Каждый экран = `Layer` (UGUI Canvas + компонент `Layer` с `Key`). **Стека слоёв нет** — `ActivateLayer` деактивирует предыдущий.
```csharp
UILayersManager.ActivateLayer("MainMenu");          // или .ActivateLayer(layerRef)
Layer cur = UILayersManager.CurrentActiveLayer;
```
- `UIElement` (база): `IsInFocus`, `IsEnabled`, `SetFocus/SetEnabled`, события `OnInteracted`, `OnFocusChanged(old,new)`, `OnEnabledChanged`.
- `ActionButtonElement` — поверх UGUI `Button`: `BoolActionDefinition` (обычно `UiButtonApply`) → по input дёргает `onClick`, опц. глиф.
- `ActionSliderElement` — поверх UGUI `Slider`: `Vector2ActionDefinition` (`UiSlider`) + `Step amount`.
- `ActionToggleElement` (v2.1) — поверх UGUI `Toggle`: `BoolActionDefinition` → по input флипает `isOn` (стандартные гарды `Layer.IsEnabled`/`IsEnabled`/фокус/`interactable`). До v2.1 такого элемента не было и в проектах писался свой с тем же именем — **при апдейте старого проекта свой класс удалить/переименовать, иначе конфликт типов**.
- `ActionScrollViewElement` (v2.1) — поверх UGUI `ScrollRect`: `Vector2ActionDefinition` + `_stepAmount` (шаг normalizedPosition по осям, clamp 0..1).
- `ActionScrollbarElement` (v2.1) — поверх UGUI `Scrollbar`: `Vector2ActionDefinition` + `_stepAmount`, учитывает направление скроллбара.
- `LayerNavigation` — граф: `SetDefaultElement(el)` + `SetTransitions(new[]{ new Transition(el, north, south, west, east), … })`.
- `LayerNavigator` — читает направленный ввод (`Vector2ActionDefinition`, default `UiLayerNavigation.asset`), авто-фокус дефолтного элемента, событие `OnSelectElement(prev,next)`.
- `ActivateLayerTrigger` — `TriggerType` (Awake/Start/OnEnable/OnDisable/OnDestroy/Manual), `FindType` (Key/Reference), `ExecuteTrigger()`.

Дефолтные определения в `Assets/UpscaleSDK/Core/UI/Runtime/DefaultDefinitions/`: `UiLayerNavigation`, `UiButtonApply`, `UiButtonCancel`, `UiSlider` + v2.1: `UiButtonBase`, `UiButtonExtra`.

> ⚠️ **Дефолтные UI-определения содержат ТОЛЬКО геймпад-источники** (проверено по YAML v2.1: `UiButtonApply` = `GamepadActionSource(Apply)`, `UiLayerNavigation` = 2× `Gamepad2DAxisSource`). Клавиатура/мышь в UI из коробки НЕ работают: чтобы Enter жал кнопку или стрелки двигали фокус — добавить kb-источники (`KeyboardButtonSource`/`Keyboard2DAxisSource`) в ассеты определений (или завести свои копии определений и не трогать SDK-ассеты — при обновлении SDK правки в его папке затрутся).

Фокус-фидбэк — самому подписаться на `UIElement.OnFocusChanged` (модуль ничего не рисует).

### Слои UI и разграничение с геймплеем (контексты ввода) — КРИТИЧНО

Типовая боль порта: открыто UI-окно, а на фоне ходит персонаж / нажатие Apply «протекает» в мир. Как это устроено в v2:

**Модель слоёв — один активный за раз (не стек приоритетов).** `UILayersManager.ActivateLayer(x)` ставит предыдущему активному слою `IsEnabled=false`, новому `true`. Гейтинг — внутри самих UI-компонентов: `ActionButtonElement.OnPerformed` и `LayerNavigator.OnNavigate` первой строкой проверяют `if (Layer.IsEnabled == false) return;`.

- **UI↔UI разводится САМО — НО только если базовый слой реально «активирован».** Открыл инвентарь → `ActivateLayer(inventory)` → слой меню/паузы под ним `IsEnabled=false` → его кнопки на Apply возвращают early. Ручных проверок приоритета писать не надо.
- **⚠️ Подводный камень: `ActivateLayer` гасит ТОЛЬКО `CurrentActiveLayer`, а `Layer._isEnabled` по умолчанию `true`.** Если базовый экран (меню/HUD) свой слой через `UILayersManager` НИКОГДА не активировал, он `IsEnabled=true`, но `CurrentActiveLayer=null`. Тогда открытие нового окна `ActivateLayer(settings)` **не гасит** базовый слой (нечего гасить) → ввод течёт в ОБА слоя: навигация и Apply работают и в окне, и в фоне (Apply жмёт и контрол окна, и сфокусированную кнопку фона). **Правило: каждый базовый слой экрана обязан активироваться через `UILayersManager` — повесь `ActivateLayerTrigger` (Start, Reference) на его Canvas (📍 в этом проекте — на `Canvas` сцены MENU для слоя `MainMenu`).** Тогда `CurrentActiveLayer` корректен, открытие окна гасит фон, а закрытие через `ActivateLayer(_prevLayer)` возвращает его.
- **Геймплей↔UI система слоёв НЕ закрывает.** Слои рулят только UI-компонентами (`ActionButtonElement`/`LayerNavigator`). Код, читающий ввод напрямую (`BaseGamepadActionSet.Instance.X.Read()` в контроллере персонажа, raycast-Interact и т.п.), **вне слоёв** — `ActivateLayer` на него не влияет. Персонаж продолжит ходить, а `Interact` ловить тот же физический Apply, что и UI-кнопка → двойной фаер. Это **не баг слоёв, а отсутствие гейтинга геймплейного чтения**.

**Чистый рычаг v2 для геймплея — `InputActionsSet.Deactivate()` / `Activate()`.** `Deactivate()` → `SetActiveAllBindings(false)`, а `ActionBinding.Read()` при `!IsActive` возвращает `default` → **один вызов глушит ВСЕ чтения набора** (не нужно ходить по скриптам с проверками слоя). Биндинги именованы по набору (`_id = typeof(T).Name + "/"`), а UI-определения (`UiButtonApply`/`UiLayerNavigation`) строятся под GUID-именами → `gameplaySet.Deactivate()` **не трогает** UI-навигацию/кнопки.

**Идиома (правило):** геймплей = отдельный `InputActionsSet` (контекст), UI = слои.
- Открытие игрового UI: `UILayersManager.ActivateLayer(uiLayer)` **И** `gameplaySet.Deactivate()`.
- Закрытие: `gameplaySet.Activate()` (и вернуть активный слой).
- Рекомендуется завести **выделенный** `PlayerInputActionSet` под геймплей и гасить именно его, а `BaseGamepadActionSet` оставить под общие действия. Если геймплей читает `BaseGamepadActionSet` напрямую — можно гасить его, но это глобально.
- Заведи один helper, делающий обе вещи (ActivateLayer + Deactivate/Activate набора) — чтобы не забывать при каждом окне.

Замечание: Legacy kb/mouse-чтение (`Input.*`) SDK не гасит — за него отвечает существующая пауза игры (отключение контроллера/курсора). А вот kb/mouse-ввод, заведённый **через SDK** (v2.1: `BaseKeyboardActionSet`/`BaseMouseInputActionSet`/kb-источники в кастомном наборе), гасится тем же `InputActionsSet.Deactivate()` наравне с геймпадом — ещё один довод заводить новый ввод через SDK, а не через `Input.*`.

## Validator

Tools → UpscaleSDK → Validate (ручной) / `UpscaleSDKBuildValidator` (пред-сборочный хук). Обязательно: company name; версия `X.Y.Z`; **Unity Input System активна**; `UpscaleSDKConfig`(`Config`) в `Resources/UpscaleSDK/`; ≥1 сцена (warning если нет `UPSBootstrap`). Предупреждения: Incremental GC вкл., splash screen выкл.

## Platform

> ⚠️ **`UpscaleSDK.Platform.<PLAT>` — это имя сборки (`.asmdef`), а НЕ C#-namespace.** Писать `using UpscaleSDK.Platform.PS5.Users.Trophies;` нельзя — не скомпилируется (`CS0234`, словлено на PS5-сборке Фазы 1). Реальные namespace в установленном PS5-пакете (сверено по `Assets/UpscaleSDK/Platform/PS5`):
> - Трофеи: `Plugins.UpscaleSDK.PS5.Runtime.Users.Trophies` (`Ps5Trophies`)
> - Активности: `Plugins.UpscaleSDK.PS5.Runtime.Users.Activities` (`Ps5Activities`, `ActivityResult`)
> - Сейвы: `Plugins.UpscaleSDK.Saves.Runtime.PS5` (`PS5FileSystem`)
> - App/User: `Plugins.UpscaleSDK.PS5.Runtime.App` / `Plugins.UpscaleSDK.PS5.Runtime.Users`
>
> Внутри одного пакета корень namespace **разный по подсистемам** — НЕ угадывать. **Перед любым `using` сверяй фактический namespace в установленном пакете:**
> ```bash
> grep -rhnE "^namespace " Assets/UpscaleSDK/Platform/<PLAT> | sort -u
> ```
> Эти namespace существуют только под своим build-target (`#if UNITY_PS5` и т.п.). С v2.1 платформенные модули SDK едут в комплекте пакета (`Assets/UpscaleSDK/Platform/{PS4,PS5,Switch,XBOX}` + `NintendoSDKPlugin`), но **Unity-пакеты платформ** (`com.unity.*.ps5` и т.п.) по-прежнему ставятся отдельно. На ноуте (Standalone) блоки выключены `#if`-ом — ошибок нет; вылезут только на сборке под консоль. Доставка пакетов на сборочный сервер — в `GOTCHAS.md`.

- **Desktop:** без доп. пакетов, `System.IO`. Сейвы → `Application.persistentDataPath/<ProjectFolderName>` через `DesktopFileSystem`.
- **PS5:** пакеты `com.unity.*.ps5` (commondialog/inputsystem/playgo/psn/render-pipelines/savedata/share); сборка (asmdef) `UpscaleSDK.Platfrom.PS5` (опечатка «Platfrom» — в самом SDK). Реальные namespace типов — см. ⚠️-блок выше. Трофеи `Ps5Trophies.Unlock(id)`. Активности `Ps5Activities.StartActivity(id)`/`FinishActivity(id, ActivityResult.{Completed|Failed|Abandoned})`. Юзер: `AppInitializer`/`UserInitializer`/`UserData`. Сейвы `PS5FileSystem` + `SaveSize`.
- **PS4:** пакеты `com.unity.*.ps4` (+ `com.unity.savedata.ps4-1.0.6`); сборка (asmdef) `UpscaleSDK.Platfrom.PS4` (опечатка «Platfrom» с v2.1 у всех платформ). Трофеи `Ps4Trophies.Unlock(id)` (ID из .trp). Сейвы `PS4FileSystem` + `SaveSize`. Namespace (сверено по v2.1): `Plugins.UpscaleSDK.PS4.Runtime.{App,Common,TrophiesManagement,User,DependencyManagement}`, сейвы `Plugins.UpscaleSDK.Saves.Runtime.PS4`.
- **Xbox:** пакеты Microsoft GDK (`com.unity.microsoft.gdk*`, `com.unity.inputsystem.gxdk`, `com.unity.render-pipelines.gamecore`); сборка (asmdef) `UpscaleSDK.Platfrom.Xbox`. Ачивки `XboxAchievements.Unlock(id)` (ID из Partner Center). Сейвы — connected-storage, `XboxContainerName`. Namespace (сверено по v2.1): единый `UpscaleSDK.Platform.Xbox`.
- **Switch:** сейвы `SwitchFileSystem` (mount Nintendo save-data); сборка (asmdef) `UpscaleSDK.Platfrom.Switch` + отдельный `NintendoSDK` (обёртки `nn.*` в комплекте `Platform/Switch/NintendoSDKPlugin`). Namespace (сверено по v2.1): `Plugins.UpscaleSDK.Switch.Runtime.Initialization`, сейвы `Plugins.UpscaleSDK.Saves.Runtime.Switch`, плагин — `nn.{account,fs,hid,…}`. Детали в вендор-доке скудные — добирать из живого SDK.

## Samples

`Assets/UpscaleSDK/Samples` — самодостаточные сцены: **Input** (live tester + глиф-демо геймпада `Input/Glyphs/Gamepad/GamepadGlyphs.unity` с префабами `GlyphForGamepadButton`/`GlyphForGamepadAction`/`GlyphForGamepad1DAxis`/`GlyphForGamepad2DAxis`) + **демо клавиатуры/мыши `Input/Glyphs/MouseAndKeyboard/DesktopGlyphs.unity`** (`GlyphForKeyboardButton`/`GlyphForKeyboard2DAxis`/`GlyphForMouseButton`/`GlyphForMouse1DAxis`/`GlyphForMouse2DAxis`) — разбор паттерна в разделе «Глифы — система подсказок», **Saves** (CRUD типов + error popup), **UI** (layers/transitions/focus; в v2.1 демо обновлено под новые компоненты — `UIExampleScene.unity`). Копировать как стартовую точку.

## Не добрано (есть на сайте)

Поклассовые страницы API Reference с точными сигнатурами (Core/Platform/PlatformProvider/RuntimeBootstrap/Configuration, Input/Saves/UI поклассово, Validator, Platform-саб-классы, PS5 Activities/Trophies, адаптивные триггеры PS5, define-символы). Добрать на Stage 3/6 при необходимости и сверить с живым SDK. Полное сырьё — `UPSCALE_SDK_V2_SOURCE.md` (⚠️ соответствует v2.0: сайт вендора на момент v2.1 не обновлён — при расхождении канон живой SDK и этот файл).
