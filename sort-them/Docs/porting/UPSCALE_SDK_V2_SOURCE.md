# Upscale SDK v2 — сырьё вендорской документации

> Источник: https://upscaleweevil.github.io/ups-docs/ (репозиторий github.com/UpscaleWeevil/ups-docs, обновлено 2026-05-28).
> Это первоисточник, по которому написан `UPSCALE_SDK.md`. Не редактировать вручную под проект — для project-специфики используй `PROGRESS.md` / пометки в стадиях. Снято: 2026-06-15.
> ⚠️ Снято через web-фетч (конвертация в markdown); часть кода/деталей могла потеряться. Каноном считать живой SDK после импорта (Stage 3) и сам сайт.
> ⚠️ **Файл соответствует SDK v2.0.** На момент апдейта SDK до v2.1 (2026-07-14) сайт вендора не обновлён — здесь НЕТ: rename `BaseInputActionSet`→`BaseGamepadActionSet`, клавиатуры/мыши, device-aware глифов, новых UI-компонентов, автодиалогов ошибок сейвов PS. Дельта и канон v2.1 — `UPSCALE_SDK.md`. Когда сайт обновится — переснять.

## Структура навигации сайта

- Overview: What Is Upscale SDK? · Unity Compatibility · Roadmap
- Manual: Getting Started · Core (Input, Saves, UI, Validator) · Platform (Desktop, Xbox, PS4, PS5, Switch)
- Samples: Overview · Input · Saves · UI
- API Reference: Core (Platform, PlatformProvider, RuntimeBootstrap, Configuration/{ConfigurationProvider, UpscaleSDKConfig}, Input/*, Saves/*, UI/*, Utils/UPSLogger, Validator/*) · Platform (Desktop, PS4, PS5, Switch, XBOX)

## Overview

UpscaleSDK — Unity SDK, абстрагирующий платформо-специфичные фичи для шиппинга консольных и десктопных игр. Платформы: Desktop (Windows/macOS/Linux), Xbox (Series X/S, One), PlayStation (PS4, PS5), Nintendo Switch.

Рекомендуемый порядок изучения: Getting Started → Manual/Core → Manual/Platform → API Reference.

## Unity Compatibility

- **Поддерживаемые:** Unity 6000.3.12f, 6000.2.10f.
- **Экспериментальные:** 2023.1.22f1, 2022.3.62f3.
- Легенда: ✅ проверено · 🧪 works in limited testing, use with caution (все платформы на 6000.x) · ⚠️ ожидается, не валидировано (2023/2022) · ❌ не поддерживается.

## Getting Started

**Зависимости:** Newtonsoft.Json — Window → Package Manager → + → Add package by name → `com.unity.nuget.newtonsoft-json`.

**Импорт пакетов:**
1. `UpscaleSDK-Core.unitypackage` — Bootstrap-сцена, кроссплатформенные модули (Input, Saves, UI, Configuration, Utils, Validator), Desktop-платформа.
2. Платформенные: `UpscaleSDK-Xbox`, `UpscaleSDK-PS4`, `UpscaleSDK-PS5`, `UpscaleSDK-Switch`.unitypackage.
3. Опциональные: `UpscaleSDK-Samples`, `UpscaleSDK-Extras`.unitypackage.

**Конфигурация:** создать/найти `UpscaleSDKConfig` в `Resources/UpscaleSDK` с именем `Config`; заполнить Saves Settings (интервал автосейва, расширение файла, размеры) и ссылки `GamepadIconsSet` на платформенные наборы глифов.

**Bootstrap-сцена:** добавить `Assets/UpscaleSDK/BootstrapScene` (она же `UPSBootstrap`) **первой** в Build Settings → Scenes In Build.

**Валидация:** Tools → UpscaleSDK → Validate Project перед сборкой.

## Manual/Core — Input

Strongly-typed, action-based абстракция поверх **Unity New Input System**, одинаково на Xbox/PS/Switch.

**Компоненты:**
- `InputAction<T>` — именованный ввод со значением типа T и событием `Performed`.
- `ButtonActionGroup` — кнопка в трёх вариантах триггера: `Press` (раз при нажатии), `Hold` (каждый кадр пока зажата), `Release` (раз при отпускании).
- `InputActionsSet<T>` — коллекция действий, активируется/деактивируется как единое целое.
- `InputActionDefinition<T>` — ScriptableObject-ассет для авторинга действий в инспекторе.

**Enums / static:**
- `GamepadButton`: ButtonSouth, ButtonWest, ButtonNorth, ButtonEast.
- `GamepadAction`: Apply, Cancel, Base, Extra.
- `ButtonTrigger`: Press, Hold, Release.
- `GamepadType`: None, Nintendo, PlayStation, Xbox.
- `DeviceType`: Keyboard, Gamepad.
- `GamepadGlyph`: иконки контроллера (face buttons, D-pad, sticks, shoulders, triggers, Start, Select).
- `Gamepad1DAxis` / `Gamepad2DAxis`: оси стиков/триггеров.

**Ключевые классы:**
- `UPSInput` — статический API: биндинг действий, запросы устройств, вибрация, события hot-plug.
- `BaseInputActionSet` — готовый набор стандартного гейпад-ввода (через `BaseInputActionSet.Instance`):
  - Face raw: South, West, North, East; mapped: Apply, Cancel, Base, Extra.
  - System: Start, Select. Shoulders/clicks: LeftShoulder, RightShoulder, LeftStickButton, RightStickButton.
  - DPad: DpadUp/Down/Left/Right. Triggers (float): LeftTrigger, RightTrigger.
  - Stick axes (float): LeftStickX/Y, RightStickX/Y. Vector2: LeftStick, RightStick, DPad.
- `GlyphProvider` — получение платформо-специфичных иконок. `GamepadIconsSet` — ScriptableObject со спрайтами под каждый `GamepadGlyph`.

**Чтение (polling):**
```csharp
bool jumpHeld = BaseInputActionSet.Instance.South.Hold.Read();
Vector2 move  = BaseInputActionSet.Instance.LeftStick.Read();
float trigger = BaseInputActionSet.Instance.RightTrigger.Read();
```

**Подписка на событие:**
```csharp
private void OnEnable()  { BaseInputActionSet.Instance.Apply.Press.Performed += OnApply; }
private void OnDisable() { BaseInputActionSet.Instance.Apply.Press.Performed -= OnApply; }
private void OnApply(bool pressed) { Debug.Log($"Apply pressed: {pressed}"); }
```

**Кастомный action set:**
```csharp
public sealed class PlayerInputActionSet : InputActionsSet<PlayerInputActionSet>
{
    public InputAction<bool> Interact { get; private set; }
    public InputAction<Vector2> Move { get; private set; }
    public InputAction<float> Aim { get; private set; }

    protected override void Initialize(InputSetBuilder builder)
    {
        Interact = builder.BindAsBool("Interact").ToAction(GamepadAction.Apply, ButtonTrigger.Press).Complete();
        Move = builder.BindAsVector2("Move").ToLeftStick().WithDeadzone(0.15f).Complete();
        Aim = builder.BindAsFloat("Aim").ToRightTrigger().Complete();
    }
}
// Lifecycle:
PlayerInputActionSet.Instance.Activate();
PlayerInputActionSet.Instance.Deactivate();
bool isActive = PlayerInputActionSet.Instance.IsActive;
PlayerInputActionSet.Instance.Dispose();
```

**Binding builder:** `BindAs...(name) → To...(…) → WithProcessor(...) → Complete()`.
- Bool: `UPSInput.BindAsBool("Jump").ToButton(GamepadButton.ButtonSouth, ButtonTrigger.Press).Complete();`
- Float: `.ToXLeftStick()`, `.ToYLeftStick()`, `.ToXRightStick()`, `.ToYRightStick()`, `.ToLeftTrigger()`, `.ToRightTrigger()`, `.WithInvert()`.
- Vector2: `.ToLeftStick()`, `.ToRightStick()`, `.ToDpad()`, `.WithDeadzone(float)`, `.WithInvertX()`, `.WithInvertY()`.

**Processors (встроенные):** InvertBoolProcessor, InvertFloatProcessor, InvertXVector2Processor, InvertYVector2Processor, DeadzoneVector2Processor, DelayVector2Processor. Кастомный через `IProcessor<T>` (методы `Process(value)`, `Clone()`), применяется `.WithProcessor(new …)`.

**Static API (UPSInput):**
```csharp
UPSInput.BindAsBool("Interact").ToButton(GamepadButton.ButtonSouth, ButtonTrigger.Press).Complete();
UPSInput.Subscribe<bool>("Interact", OnInteract);
UPSInput.Unsubscribe<bool>("Interact", OnInteract);
UPSInput.SetActionState("Interact", active: false);
UPSInput.Unbind("Interact");

GamepadType pad = UPSInput.GetGamepadType();
DeviceType[] connected = UPSInput.GetConnectedDevices();

UPSInput.SetGamepadVibration(lowFrequency: 0.4f, highFrequency: 0.7f);
UPSInput.StopGamepadVibration();

UPSInput.OnDeviceAdded   += device => { };
UPSInput.OnDeviceRemoved += device => { };
```

**Глифы:**
```csharp
Sprite icon = BaseInputActionSet.Instance.Apply.Press.TryGetGlyph();
Sprite east = UPSInput.TryGetGlyph(GamepadGlyph.East);
```
Создание набора: right-click → Create → Upscale SDK → Gamepad Icons Set → заполнить спрайты под каждый `GamepadGlyph` → привязать в `UpscaleSDKConfig` (Resources/UpscaleSDK/Config).

**Asset-based actions:**
```csharp
[SerializeField] private BoolActionDefinition _interactDefinition;
[SerializeField] private Vector2ActionDefinition _moveDefinition;
_interact = _interactDefinition.Build();   // в OnEnable
_move = _moveDefinition.Build();
// _interact.Performed += ...; в OnDisable: -= ...; затем _interact.Dispose();
```

**Маппинг кнопок по платформам:**
| Action | PS4 | PS5 | Switch | Xbox |
|---|---|---|---|---|
| Apply | X / O | X | A | A |
| Cancel | O / X | O | B | B |
| Base | Square | Square | X | Y |
| Extra | Triangle | Triangle | Y | X |

## Manual/Core — Saves

Платформо-независимое key/value + слотовое сохранение. **Требует Newtonsoft.Json.**

| Компонент | Роль |
|---|---|
| `UPSSaves` | Фасад: доступ к `.Prefs` и `.Saver`, сигнал `IsReady` |
| `UPSPlayerPrefs` (`UPSSaves.Prefs`) | High-level key/value (аналог Unity PlayerPrefs) |
| `Saver` (`UPSSaves.Saver`) | Low-level: Dictionary<string,string> в памяти + I/O (advanced, в разработке) |
| `AutoSaver` | Таймер периодического flush (`Tick(Time.unscaledDeltaTime)`, не зависит от паузы) |
| `SavesSettings` | ScriptableObject-конфиг |

**Инициализация:** автоматически через `RuntimeBootstrap` после готовности Platform. Нужны: ассет `UpscaleSDKConfig` (`Config`) в `Resources/UpscaleSDK/`, ссылка на `SavesSettings`, сцена `UPSBootstrap` первой в Build Settings.

**Prefs API:**
```csharp
bool ready = UPSSaves.Prefs.IsReady;
UPSSaves.Prefs.OnReady += () => { };

UPSSaves.Prefs.SetInt("level", 7);
UPSSaves.Prefs.SetFloat("health", 0.85f);
UPSSaves.Prefs.SetString("playerName", "Hero");
UPSSaves.Prefs.SetBool("hardMode", true);

int level   = UPSSaves.Prefs.GetInt("level", defaultValue: 1);
float hp    = UPSSaves.Prefs.GetFloat("health", defaultValue: 1f);
string name = UPSSaves.Prefs.GetString("playerName", defaultValue: "Player");
bool hard   = UPSSaves.Prefs.GetBool("hardMode", defaultValue: false);

bool exists = UPSSaves.Prefs.HasKey("level");
UPSSaves.Prefs.DeleteKey("level");
UPSSaves.Prefs.TrySave();   // безопасно в любой момент; молча no-op если система не готова
```
Явный сейв после каждого изменения не нужен — AutoSaver делает периодический flush. Поддерживаемые типы: int, float, string, bool.

**Saver (advanced):**
```csharp
if (UPSSaves.IsReady) { /* safe to use Saver */ }
void SetSaveData(string key, string data) => UPSSaves.Saver.CurrentSaveData[key] = data;
UPSSaves.Saver.Save();           // авто-имя Save_yyyyMMdd_HHmmss
UPSSaves.Saver.Save("Slot1");    // именованный слот
UPSSaves.Saver.OnLoadCompleted += success => { };
UPSSaves.Saver.Load("Slot1");    // асинхронно (cloud storage на консолях)
UPSSaves.Saver.OnSavesFound += entries => { foreach (FileEntry e in entries) {} };
UPSSaves.Saver.StartSavesSearch();
Dictionary<string,string> state = UPSSaves.Saver.CurrentSaveData;
```
**ISaverEvents:** OnSaveStarted(Dictionary), OnSaveCompleted(string), OnLoadStarted(), OnLoadCompleted(bool), OnSavesFound(FileEntry[]), OnReadError(int,string), OnWriteError(int,string).

**SavesSettings** (Create → Upscale SDK → Saves → Settings):
| Поле | Тип | Default | Назначение |
|---|---|---|---|
| EnableAutosaves | bool | true | Вкл. AutoSaver |
| SecondsBetweenAutosaves | int | 300 | Интервал flush |
| SaveFileExtension | string | "txt" | Расширение |
| ProjectFolderName | string | "UpscaleSaves" | Папка в persistentDataPath |
| InitializeSystemOnAwake | bool | true | (не используется) |
| SaveSize | UInt64 | — | PS4/PS5 объём (МБ) |
| XboxContainerName | string | "UpscaleContainer" | Xbox-контейнер (alnum, _ / . -) |

**Ошибки:** `ErrorHandler.OnError += msg => …;` глобальный диспетчер, `ErrorHandler.RaiseError(string)`.
**Сериализация:** JSON (Newtonsoft), полиморфизм `TypeNameHandling.All`, `JsonSerializator`/`SerializatorFactory`.
**FileSystem на платформу:** Desktop `DesktopFileSystem` (System.IO), Xbox `XboxFileSystem` (XGameSave), PS4 `PS4FileSystem`, PS5 `PS5FileSystem`, Switch `SwitchFileSystem`. Кастом: реализовать `IFileSystem` + переопределить `Platform.ProvideFileSystem()`.

## Manual/Core — UI

Controller-ориентированная навигация меню. Каждый экран = `Layer`; слой содержит `UIElement`-ы (кнопки, слайдеры) + граф навигации. Модуль не рисует/не анимирует/не владеет префабами; работает поверх обычных UGUI Canvas/Button/Slider. **Стека слоёв НЕТ** — `ActivateLayer` просто деактивирует предыдущий.

- `Layer` (MonoBehaviour, авто-регистрация в UILayersManager): `Key`, `IsEnabled` (r/o), `SetEnabled(bool)`.
- `UILayersManager` (static):
```csharp
UILayersManager.ActivateLayer("MainMenu");
UILayersManager.ActivateLayer(settingsLayer);
Layer current = UILayersManager.CurrentActiveLayer;
```
- `LayerNavigation` — граф переходов: `DefaultElement` + массив `Transition` (North/South/West/East):
```csharp
navigation.SetDefaultElement(playButton);
navigation.SetTransitions(new[] {
    new Transition(playButton, north: null, south: quitElement, west: null, east: null),
    new Transition(quitElement, north: playButton, south: volumeElement, west: null, east: null),
});
```
- `LayerNavigator` — читает направленный ввод, управляет фокусом; требует `Vector2ActionDefinition` (default `UiLayerNavigation.asset`); авто-фокус начального элемента при активации слоя; событие `OnSelectElement(prev, next)`.
- `UIElement` (база): `IsInFocus`, `IsEnabled`, `SetFocus(bool)`, `SetEnabled(bool)`; события `OnInteracted`, `OnFocusChanged(old, current)`, `OnEnabledChanged(old, current)`.
- `ActionButtonElement` — оборачивает UGUI Button: `Action definition` = `BoolActionDefinition` (обычно `UiButtonApply`), `Button` = целевой Button; по input вызывает `onClick`; опционально показывает глиф.
- `ActionSliderElement` — оборачивает UGUI Slider: `Action definition` = `Vector2ActionDefinition` (обычно `UiSlider`), `Step amount`; управляется направленным вводом.
- `ActivateLayerTrigger` — декларативная активация: `TriggerType` (Awake/Start/OnEnable/OnDisable/OnDestroy/Manual), `FindType` (Key/Reference); метод `ExecuteTrigger()`.

**Дефолтные определения** (в `Assets/UpscaleSDK/Core/UI/Runtime/DefaultDefinitions/`): LayerNavigator→`UiLayerNavigation.asset`, Apply→`UiButtonApply.asset`, Cancel→`UiButtonCancel.asset`, Slider→`UiSlider.asset`.

**Пример визуального фидбэка фокуса:**
```csharp
private void OnEnable()  => uiElement.OnFocusChanged += OnFocusChanged;
private void OnDisable() => uiElement.OnFocusChanged -= OnFocusChanged;
private void OnFocusChanged(bool oldV, bool newV) => graphic.color = newV ? highlightColor : originalColor;
```

## Manual/Core — Validator

Проверяет корректность настройки под активную платформу. Режимы: ручной (Tools → UpscaleSDK → Validate, отчёт в Console) и авто (`UpscaleSDKBuildValidator` — пред-сборочный хук). Делит на ошибки и предупреждения.

**Обязательные проверки (все платформы):** установлено название компании; версия `X.Y.Z`; **Unity Input System установлена и активирована**; ассет `UpscaleSDKConfig` (`Config`) в `Resources/UpscaleSDK/`; ≥1 сцена в Build Settings (предупреждение, если нет `UPSBootstrap`).
**Предупреждения:** включить Incremental GC; отключить splash screen.

## Manual/Platform

**Desktop:** доп. пакеты/middleware не нужны, слой использует `System.IO` напрямую. Сейвы → `Application.persistentDataPath/<SavesSettings.ProjectFolderName>` через `DesktopFileSystem`. Системы: Windows/macOS/Linux.

**PS5:** пакеты — com.unity.{commondialog, inputsystem, playgo, psn, render-pipelines, savedata, share}.ps5. Проверить build config `UpscaleSDK.Platform.PS5`.
- Трофеи: `Ps5Trophies.Unlock(1);`
- Активности: `Ps5Activities.StartActivity("activity_main_story_chapter_1");` / `Ps5Activities.FinishActivity("...", ActivityResult.Completed);` — `ActivityResult`: Completed/Failed/Abandoned.
- Сейвы: `PS5FileSystem`, `SavesSettings.SaveSize` (МБ), интеграция с сервисом сохранений Sony.
- Юзер: `AppInitializer`, `UserInitializer`, `UserData`.

**PS4:** пакеты — com.unity.{commondialog, inputsystem, nptoolkit2, render-pipelines}.ps4, com.unity.savedata.ps4-1.0.6. Build config `UpscaleSDK.Platform.PS4`.
- Трофеи: `Ps4Trophies.Unlock(1);` (ID из .trp проекта).
- Сейвы: `PS4FileSystem`, `SavesSettings.SaveSize`; классы `ReadFileRequest/Response`, `WriteFileRequest/Response`.

**Xbox (Series X/S, One):** пакеты — com.unity.microsoft.gdk(+tools, +tools.xbox), com.unity.inputsystem.gxdk, com.unity.render-pipelines.gamecore. Версии должны быть совместимы с установленным Microsoft GDK.
- Ачивки: `XboxAchievements.Unlock(1);` (ID из Partner Center).
- Сейвы: connected-storage контейнеры, `SavesSettings.XboxContainerName`; Xbox-специфичный код не требуется. Классы: XboxPlatform, XboxData, XBOXFileSystem, XboxValidator.

**Switch:** сейвы через `IFileSystem` на базе Nintendo SDK; `SwitchFileSystem` маршрутизирует через монтирование save-data. Классы: SwitchPlatform, SwitchValidator. (На сайте детали/define-символы не приведены.)

## Samples

В `Assets/UpscaleSDK/Samples`, каждый — отдельная самодостаточная сцена:
1. **Input** — работа с геймпадом, live tester + per-platform controller glyph prefabs.
2. **Saves** — чтение/запись int/float/string/bool с обработкой ошибок через popup.
3. **UI** — controller-friendly навигация меню: layers, transitions, focus feedback.

## Не снято детально (есть на сайте, добрать при необходимости)

- Roadmap.
- API Reference поклассовые страницы (точные сигнатуры): Core/{Platform, PlatformProvider, RuntimeBootstrap, Configuration/*}, Input/Saves/UI поклассово, Validator/*, Platform/* (PS5 Activities/Trophies, Xbox/PS4 саб-классы). Добрать на Stage 3/6 при необходимости и сверить с живым SDK.
