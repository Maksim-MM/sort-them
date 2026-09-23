# Порт Sort Them на консоли — лог

> Лог порта по этому проекту. Общий пайплайн — `PORTING_PIPELINE.md` в этой папке, канон SDK — `UPSCALE_SDK.md`. Журнал разработки самой игры — корневой `PROGRESS.md`, сюда только порт.

## Решения (23.09)

- **Платформы:** ПК (клавиатура+мышь, геймпад), консоли через Upscale SDK, WebGL (Яндекс Игры) остаётся целевой платформой. Точный список консолей: Switch 1 (девкит есть); PS5/PS4/Xbox — уточнить у пользователя.
- **Одна ветка, один код.** Платформенные различия — через asmdef с `includePlatforms`, `#if` только в фабрике реализаций, Build Profiles на платформу. Долгоживущих платформенных веток не заводим.
- **WebGL не выкидывается** (отклонение от пайплайна, где мобильный/веб-код удаляется). Тач-схема остаётся под `Platform.IsMobile`; плагин Яндекса (PluginYG2) пойдёт в отдельный asmdef `includePlatforms: ["WebGL"]`, на консольных сборках его кода не будет. Сетевых обращений, рекламы, покупок, аналитики в проекте нет — Stage 1 практически пустой.
- **Управление на ПК и консолях — через SDK** (`PlayerInputActionSet` поверх `InputActionsSet`, UI-слои SDK). Игра делалась под ПК и геймпад с нуля.
- **Пока не делаем:** трофеи/ачивменты, активности PS5, Light Bar (решение пользователя 23.09).
- **Глифы:** готовые наборы из `Assets/UpscaleSDK/Extras/Glyphs/`, SDK выбирает набор по платформе сам. Подтверждение/отмена в подсказках — только через семантические `GamepadAction.Apply/Cancel`, не позицией кнопки (см. `GOTCHAS.md`, Switch).
- **Лог порта** — этот файл, корневой `PROGRESS.md` не трогаем (там журнал игры).

## Что уже совпадает с требованиями SDK

- New Input System 1.19.0 (`activeInputHandler: 1`), Legacy Input не используется.
- Unity 6000.3.12f1 — в списке поддерживаемых SDK v2.1.
- Unity Localization 1.5.13 + Addressables + TMP — стадии 5.5/5.6 закрыты игрой.
- Сейвы за интерфейсом `ISaveStorage` (`Runtime/Save/`), одна реализация `PlayerPrefsSaveStorage`, подключение в `GameManager.cs:77`. Настройки (`Core/Settings.cs`, 7 ключей) пишут в `PlayerPrefs` напрямую — перевести на хранилище SDK.
- Геймпад частично сделан (22.09): привязки всех действий, `UI/ActiveDevice.cs` (активное устройство по реальному вводу), подписи кнопок в `ControlHints`, навигация в окнах.
- `Platform.cs` — только `IsMobile`.

## Особенности проекта

- UI строится кодом (`UiFactory`, `UiRoot`), не префабами: слои SDK (`Layer`, `ActionButtonElement`, `LayerNavigator`) прикручиваются в коде-генераторе; ловушки префабов из `GOTCHAS.md` не актуальны.
- Одна сцена `Main`; `Editor/BuildTool.cs` собирает по `Paths.MainScene`. Bootstrap-сцена SDK `UPSBootstrap` должна стать сценой 0 — добавить в Build Settings и в `BuildTool`. `SceneManager.LoadScene(int)` в коде игры: проверить grep'ом после добавления сцены.
- `Resources/` в `_Game` нет; локализация через Addressables.
- Под валидатор SDK: `companyName` = `DefaultCompany` → нужен ровно `Upscale Studio`; `bundleVersion` 0.1.0 подходит; заставка Unity включена (предупреждение валидатора, для консолей выключить).
- На машине пользователя установлены модули Android, WebGL, Windows, Mac. Консольные модули Unity — с порталов платформодержателей; до их появления вся интеграция проверяется на Desktop-платформе SDK.
- `Editor/BuildTool.cs` уже содержит `EnsureWebGLTarget()` (сброс кэша Addressables `[BuildTarget]`) — при появлении консольных сборок обобщить на любую целевую платформу.

## План

| # | Стадия | Что | Статус |
|---|---|---|---|
| 0 | Stage 0 | `/unity-analyze` → `ARCHITECTURE.md`, `CLAUDE.md` со станзой порта (лог порта → этот файл). Новый чат. | ⬜ |
| 1 | Импорт SDK | Newtonsoft (`com.unity.nuget.newtonsoft-json`), Core + Desktop + платформы; `Config` в `Resources/UpscaleSDK/`, `SavesSettings`, 6 наборов глифов; `UPSBootstrap` сценой 0; `companyName`; валидатор зелёный; в Play `[UPSRuntime]` без ошибок. | ⬜ пользователь ставит SDK 23.09 |
| 2 | Stage 2–5 | `PlayerInputActionSet` (геймпад + клавиатура + мышь одним действием), гейтинг геймплея при открытом окне (`Deactivate()`), слои UI, глифы вместо текстовых подписей в `ControlHints`, `ActiveDevice` → `UPSInput.GetPrimaryDevice()`. | ⬜ |
| 3 | Stage 6 | `UpscaleSaveStorage : ISaveStorage` поверх `UPSSaves.Prefs` (base64 → string), готовность через `Prefs.IsReady`/`OnReady`; `Settings` через `Prefs`; триггеры сейва централизовать. Трофеи/активности — нет. | ⬜ |
| 4 | Веб | PluginYG2 (namespace `YG`, класс `YG2`, модули с define `*_yg`, шаблон `WebGLTemplates/YandexGames`) в WebGL-only asmdef; `YandexSaveStorage`; язык платформы. Доку дочитать на этой стадии: https://max-games.ru/plugin-yg/doc/ | ⬜ |
| 5 | Stage 7 | Чеклист сертификации, Build Profiles на платформу. | ⬜ |

## Сделано

- 23.09 — папка `porting/` соседнего проекта скопирована в `Docs/porting/` (15 файлов). Прочитаны `PORTING_PIPELINE.md`, `README.md`, `UPSCALE_SDK.md`, `GOTCHAS.md`, `STAGE-0-ANALYSIS.md`; остальные стадии — по маршрутизации, когда дойдём.
- 23.09 — веб-сборка починена до порта (не часть порта, но влияет на общий код): IL2CPP `OptimizeSize` + стриппинг `Medium` для WebGL, `EnsureWebGLTarget()` в `BuildTool`; разбор в `OPTIMIZATION.md`.
