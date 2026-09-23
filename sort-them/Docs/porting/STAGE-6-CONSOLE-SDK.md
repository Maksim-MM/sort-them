> Часть porting-пайплайна. Индекс, маршрутизация и порядок чтения — `PORTING_PIPELINE.md` в этой папке.

## Stage 6 — Console SDK Integration

Большинство интеграций делается через `Assets/UpscaleSDK/` — см. `UPSCALE_SDK.md` для API: сейвы (`UPSSaves.Prefs`), вибрация (`UPSInput.SetGamepadVibration`), трофеи/активности (`Ps5Trophies`/`Ps4Trophies`/`XboxAchievements`/`Ps5Activities`), лог (`UPSLogger`).

- [ ] Проверить платформенные модули SDK под цель (с v2.1 PS4/PS5/Switch/Xbox в комплекте `Assets/UpscaleSDK/Platform/`; в v2 ставились отдельными `.unitypackage`) + установить **Unity-пакеты платформ** (`com.unity.*.ps5`, GDK и т.п.) на сборочной машине — типы активны под build-target define (`UNITY_PS5`/`UNITY_PS4`/`UNITY_GAMECORE_XBOXSERIES`); namespace в пакетах **разный по подсистемам** — сверять grep'ом (см. `UPSCALE_SDK.md` → Platform)
- [ ] Achievements / Trophies — `Ps5Trophies.Unlock(id)` / `Ps4Trophies.Unlock(id)` / `XboxAchievements.Unlock(id)` (своя абстракция не нужна — SDK уже разделяет по платформам). **Pеализовано** в `SocialService.cs`
- [ ] Сохранения — заменить `DataManager`/`PlayerPrefs` на `UPSSaves.Prefs` (key/value, JSON). **Captive: выполнено в Фазе 1 (2026-06-20)** — все настройки на `UPSSaves.Prefs` (см. раздел «Паттерн миграции сохранений»). На консольной фазе остаётся настроить платформенные поля `SavesSettings` (PS4/PS5 `SaveSize`, `XboxContainerName`)
- [ ] Вибрация (рамбл) — через SDK `UPSInput.SetGamepadVibration(low, high)` / `StopGamepadVibration()` (консоле-безопасно). **Pеализовано** в `VibrationService.cs`. Адаптивные триггеры PS5 — в вендор-доке v2 не описаны, добрать из живого SDK на девките
- [ ] Сплэш-экран и инициализация — сцена `UPSBootstrap` + `UPSInitializeAwaiter` (дожидается `UPSSaves.Prefs.IsReady` перед загрузкой MENU). Уже подключено в Фазе 1
- [ ] Platform-specific overlays (Profile, Store) — нативные SDK вне Upscale
- [ ] Проверить: сохранение/трофеи/активности работают на каждой платформе (на девките)

### Ачивки и трофеи — generic-паттерн

Единый сервис-синглтон ловит игровые события и выдаёт трофеи. Шаги для нового проекта:

1. **Получить от продюсера/PM список трофеев**: для каждого `ID` (число) — название — условие разблокировки. **ID одинаковые на всех платформах** (один и тот же int уходит в `Ps5Trophies.Unlock(id)`/`Ps4Trophies.Unlock(id)` и `XboxAchievements.Unlock(id)`). ID **должны совпадать** с конфигом trophy pack (Sony) и achievement-конфигом (Xbox SCID) — это вне кода.
2. **Сервис** (один `MonoBehaviour`-синглтон, `DontDestroyOnLoad`, на стартовой/бутстрап-сцене). `enum TrophyType { None=0, ... = ID }`, диспетчер под `#if`:
   ```csharp
   #if UNITY_PS5
       Ps5Trophies.Unlock(id);
   #elif UNITY_PS4
       Ps4Trophies.Unlock(id);
   #elif UNITY_GAMECORE_XBOXSERIES
       XboxAchievements.Unlock(id);
   #endif
   #if UNITY_EDITOR
       Debug.Log($"[SocialService] Trophy unlocked id={id}");
   #endif
   ```
   В Editor — `Debug.Log` (реальные вызовы под define), так логику видно без devkit. SDK сам инициализируется (`RuntimeInitializeOnLoadMethod`), отдельный init не нужен. **`using`-директивы тоже под `#if`** (namespace типов разный по платформам и существует только под своим build-target). Сверено с живым SDK v2.1 (2026-07-30):

   | Платформа | Дефайн(ы) | Тип | Namespace |
   |---|---|---|---|
   | PS5 | `UNITY_PS5` | `Ps5Trophies`, `Ps5Activities` | `Plugins.UpscaleSDK.PS5.Runtime.Users.{Trophies,Activities}` |
   | PS4 | `UNITY_PS4` | `Ps4Trophies` | `Plugins.UpscaleSDK.PS4.Runtime.TrophiesManagement` |
   | Xbox | `UNITY_GAMECORE_XBOXSERIES` **или** `UNITY_GAMECORE_XBOXONE` | `XboxAchievements` | `UpscaleSDK.Platform.Xbox` |

   🔴 **Два подвоха, на которых легко потерять платформу:**
   - **Xbox: нужны ОБА дефайна.** Сама обёртка собирается под `#if UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE`, так что если в диспетчере сервиса указать только Series, на Xbox One ачивки молча не выдаются.
   - **Xbox-ветке нужен свой `using`.** В донорских сервисах `using` часто есть только для PS — тогда Xbox-сборка падает с `CS0246: XboxAchievements not found`. В Editor это невидимо (ветка неактивна), вылезает только на сборочной машине.
3. **Идемпотентность.** 🔴 **Гард `Saves["Trophy_<id>"]==1` ПЕРЕД выдачей — ловушка, не повторять.** Обёртки SDK молча выходят, если соцфункции недоступны (`Ps5Trophies.Unlock`: `if (_appData.CanUseSocial == false) return;`, `XboxAchievements.Unlock`: `if (_instance == null) return;`), а сервис при этом уже записал «выдан» → у игрока без входа в PSN/Xbox Live трофей теряется **навсегда**.
   Правильно: **выдавать всегда, платформа сама идемпотентна** — `Ps5Trophies.Unlock` проверяет `AllTrophies[id].Data.Unlocked`, `XboxAchievements.UnlockAchievement` — `ProgressState == Achieved`. От спама в рамках сессии достаточно `HashSet<int>` в памяти. Ключ `Saves["Trophy_<id>"]` при этом всё равно полезен, но в другой роли — «условие когда-либо выполнялось» (нужен, чтобы понять, что собраны все N для завершения активности PS5).
   **Счётчики** прогресса («продать 100», «убрать 20 раз») — отдельные ключи `Saves["Count_*"]`, инкремент в обработчике события. Для условий «купить всё из набора» удобны ключи-флаги на элемент (`Saves["Furn_<typeId>"]=1`), иначе не отследить покупки, растянутые на много сессий.
4. **Маппинг условие → триггер** (главная работа): для каждого условия найти точку в коде. Сначала искать **готовые** события в шине проекта (`grep` по `EventsManager.cs`); подписки ставить в `OnEnable`/снимать в `OnDisable`. Где готового сигнала нет — **добавить новое событие** в профильный `EventsManager` и фаер в точке механики (а не лепить вызовы сервиса по всему коду). Состояние-условия (уровень, дневной доход, «все куплено») читать из БД/контроллеров в обработчике.
5. **Типы условий**: one-shot (первое действие — гард-флаг), счётчик (≥N), «всё из набора» (сравнить количество купленного с полным списком), пороги прогрессии (`>=` в обработчике апдейта). Условие с **позицией игрока** (напр. «не выходя из магазина») — отдельный триггер-объём (`BoxCollider isTrigger`) + статический флаг `PlayerInside`, детект игрока по тегу/контроллер-компоненту; учесть, что у игрока должен быть коллайдер+`Rigidbody`/`CharacterController`, иначе триггер не сработает.
6. **Активность PS5** (`Ps5Activities.StartActivity(id)` / `Ps5Activities.FinishActivity(id, ActivityResult.{Completed|Failed|Abandoned})`) — только PS5, через UDS, отдельно от трофеев. Запускать на старте, завершать по чёткому **числовому/детектируемому** условию. Если условие от PM расплывчатое — уточнять, не выдумывать; можно отложить, трофеи от этого не зависят.
   - 🔴 **Если активность «по умолчанию» не стартует (в Control Center остаётся Not Started, трофеи при этом работают) — это почти наверняка гонка ниже, применяй фикс `IsUserRegistered` сразу.** ⚠️ **Проверено на SDK v2.1 (2026-07-30): флаг штатно НЕ приехал** — в `Platform/PS5/Users/UserInitializer.cs` есть только инстансный `IsInitialized`. Патч накладывается вручную, 3 строки: `public static bool IsUserRegistered { get; private set; }` рядом с `IsInitialized`; `IsUserRegistered = true;` в success-ветке `RegisterUserSession` (сразу после `SessionsManager.RegisterUserSessionEvent`); `IsUserRegistered = false;` первой строкой `UnregisterUserSession`. Остальные файлы соц-части (`Ps5Trophies.cs`, `Ps5Activities.cs`) патчить не нужно — они не менялись.
   - 🔴 **`activityStart` нельзя постить до регистрации юзера в UDS.** Юзер добавляется в UDS асинхронно (`OnUserServiceEvent(Login)` → `AddUserRequest`, на девките ~0.5–1 c после бута). Если сервис активности живёт на сцене 0 и стартует из `Awake`/`Start`, PostEvent уходит раньше и UDS его отбрасывает: `Request API result warning! Warning (0x0): User not registered with UDS`. Активность в Control Center остаётся «Not Started». Фикс: публичный флаг `IsUserRegistered` в Social-модуле SDK (выставлять в success-ветке `RegisterUserSession`, сбрасывать при логауте) + в сервисе `yield return new WaitUntil(() => Social.IsUserRegistered)` перед `StartActivity`. Ждать `Social.IsInitialized` бесполезно — он выставляется синхронно в `Initialize()` ещё до сцен.
   - 🔴 **«User not registered with UDS» приходит как Warning, а `CheckRequestSuccess` SDK считает Warning успехом** — success-колбэк `StartActivity` срабатывает, в логе «Activity started», хотя событие реально отброшено. Ретрай по success-колбэку не спасает. Диагностика только по жёлтой warning-строке в логе.
   - 🔴 **Трофеи эту гонку маскируют**: они анлочатся минутами позже, когда юзер давно зарегистрирован, поэтому «трофеи работают, активность нет» — типичная картина именно этой гонки.
   - **Диагностика на девките**: колбэки PSN-запросов (`ContinueWith`) выполняются на worker-потоках — экранный лог-оверлей должен подписываться на `Application.logMessageReceivedThreaded` (не `logMessageReceived`, тот ловит только main thread), буфер под `lock`, время через `Stopwatch` (Unity `Time.*` вне main thread кидает). Исключения внутри PSN-запросов печатаются только под `DEBUG` → диагностический билд собирать с **Development Build**, иначе цепочка умирает молча.

📍 _Пример (supermarket-simulator):_ `SocialService` (на `Launcher.unity`), 16 трофеев, гард `Trophy_{id}`, счётчики `Count_ProductsSold`/`Count_CleanMess`; новые события `OnFurniturePlaced/OnDirtCleaned/OnFireExtinguisherUsed/OnServiceOrdered/OnThiefKicked/OnBoxThrownInTrash`; `StoreZone`-триггер для Perfect Aim; активность `story_01` стартует после `WaitUntil(IsUserRegistered)`, завершается при получении всех 16 трофеев.

### Паттерн миграции сохранений

При переносе `DataManager` (JSON-файл) и `PlayerPrefs`-систем на `UPSSaves.Prefs`:

**1. JSON-прогресс через строку:**
```csharp
// Загрузка
var json = UPSSaves.Prefs.GetString("savedata", "");
data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);

// Сохранение (только dict, без I/O)
UPSSaves.Prefs.SetString("savedata", JsonUtility.ToJson(data));

// Реальный flush — только в lifecycle-коллбэках
private void OnApplicationQuit() { SaveData(); UPSSaves.Prefs.TrySave(); }
```

**2. PlayerPrefs → Prefs (прямая замена):**
`PlayerPrefs.GetInt/SetInt/HasKey/DeleteKey/Save` → `UPSSaves.Prefs.*` (+ `GetBool/SetBool` для bool, `TrySave()` вместо `Save()`). API близок к `PlayerPrefs`.

**2.5. Инвентарь: грепать по ВСЕМУ `Assets/`, а не по папке своих скриптов.** Живые игровые модули часто лежат вне `Assets/Scripts` (донорские паки дейли-наград, кастомизации и т.п.) и пишут прогресс в `PlayerPrefs` сами. Разметить три группы: (а) живые системы — мигрировать; (б) мёртвые мобильные остатки (0 ссылок в коде **и** в сценах/префабах по GUID) — удалять, не мигрировать; (в) editor-утилиты и сэмплы — не трогать. Помнить, что после миграции **PlayerPrefs-вьюеры перестают показывать игровые данные** (они в JSON-файле SDK) — либо снести утилиту, либо завести просмотрщик SDK-сейва.
```bash
grep -rn "PlayerPrefs\." Assets --include="*.cs" | grep -v UpscaleSDK | grep -v "3rd Party" | grep -v "Assets/Plugins/"
```

**2.6. Сейвы SDK поднимаются САМИ — бутстрап-сцена для них не нужна.** `RuntimeBootstrap.Boot()` помечен `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` и создаёт `UPSSaves` + зовёт `Initialize()` в любой сцене. Значит миграцию можно делать до внедрения `UPSBootstrap.unity`; бутстрап-сцена нужна ради лого издателя и **гейта готовности**, а не ради доступа к `Prefs`. Обратная сторона: `UPSSaves.Prefs` — это `_instance._prefs`, и до `Initialize()` обращение даёт **NRE**, а не дефолт (поэтому ранние обращения — только через хелпер `SavesReady`).

**2.7. Механика, из-за которой ранняя запись исчезает** (чтобы не спорить с инвариантом ниже): `Saver` по завершении чтения файла делает `_currentSaveState = save.Data` — **заменяет словарь целиком**, а не мержит. Всё, что успели записать `Set*` до `LoadCompleted`, стирается молча.

**2.8. Гейт готовности ставить в уже существующую точку ожидания.** Не нужно строить свой экран: в игре почти всегда есть загрузочный экран, «Press any button» или сплэш — достаточно не пускать дальше, пока `SavesReady.IsReady` не встанет. Это одной правкой снимает необходимость гардов во всём геймплейном коде — он по определению стартует после готовности. Гарды остаются нужны только тем, кто живёт **до** гейта (`RuntimeInitializeOnLoadMethod`, сплэш-меню настроек, менеджер локали) — им `SavesReady.Run(...)`.

**2.9. Триггер сейва централизовать в один объект.** Типовая мобильная схема — каждая система сохранения сама держит `OnApplicationQuit/Pause/Focus`. На консоли это N синхронных записей файла на один выход/оверлей. Правильно: один `DontDestroyOnLoad`-объект ловит lifecycle-коллбэки → фаерит игровое событие сохранения (`OnSaveGame`) → делает **один** `TrySave()`; из систем свои коллбэки убрать, подписку на событие оставить.
- Если старый триггер был платформенным (`WebGLSaveManager` с JS-interop `RegisterBeforeUnload`), переименовать класс+файл, **сохранив GUID в `.meta`** — тогда ссылка в сцене/префабе не рвётся (имя объекта в YAML поправить отдельно, `m_Name`).
- Платформенный запрос сохранения от системы (`Platform.OnContextRequestSave`) SDK обрабатывает сам (`Prefs.TrySave()`), но **до игрового кода не доводит**: `_platform` в `UPSSaves` приватный. Состояние сцены в этот момент может быть не сериализовано — если нужно, просить у вендора публичный хук.

**2.10. Ключи настроек сверить между UI и сервисами.** Классический рассинхрон при переносе заготовок из другого проекта: панель настроек пишет `Setting_Vibration` как `int`, а `VibrationService`/`LightBarService` читают `Settings.Vibration` через `GetBool`. Тумблер работает «на вид», а сервис его не видит. Держать одну константу (`public const string KEY` в сервисе, UI ссылается на неё).

**2.11. `XboxContainerName` — это имя файла сейва на ВСЕХ платформах**, не только на Xbox (`UPSPlayerPrefs.TrySave()`/`LoadSave()` передают его как `saveName`). Дефолт из SDK — `UpscaleContainer`; переименование под тайтл = сброс уже созданных тестовых сейвов, поэтому делать до тестовых прогонов.

**3. Запись `Save()` синхронна, клиентского rate-limit нет:**
`Saver.Save()` (`Assets/UpscaleSDK/Core/Saves/Runtime/Core/Saver.cs`) пишет `_fileSystem.Write(...)` синхронно, без троттлинга — на Desktop каждый `UPSSaves.Prefs.TrySave()` пишет файл сразу. Тем не менее совет ниже («не вызывать `Save()` на каждое изменение, батчить») — хорошая гигиена и страховка на случай, если консольные `FileSystem` (PS/Xbox/Switch) окажутся троттлящими: проверять на девките. Инвариант «не сохраняй до `IsReady`» встроен: `UPSPlayerPrefs.TrySave()` начинается с гарда `if (IsReady == false) return;`.

**Правило:** `Set*` вызывать свободно (только in-memory, никакого I/O). `Save()` — только в:
- lifecycle-коллбэках (`OnApplicationQuit/Pause/Focus`) — гарантируют финальный flush
- критических одиночных точках (например, покупка за реальные деньги, если есть)
- автосейв SDK (каждые 300 сек по умолчанию) берёт остальное

**Системы с частыми изменениями** (геймплейные очки, деньги, рейтинг) — только `Set*`, без `Save()`.  
**Системы с редкими изменениями** (покупка, разблокировка) — `Set*` + `Save()` в точке события, т.к. это критичные транзакции.

**4. 🔴 Два инварианта против затирания сейва «пустотой»** (словлено на практике: на консоли периодически пропадала вся купленная мебель):
- **«Не сохраняй, пока не загрузился»**: системы, сериализующие СОСТОЯНИЕ СЦЕНЫ (мебель/коробки/доставки) и сохраняющие в `OnApplicationQuit/Pause(true)/Focus(false)` — на консоли эти коллбэки прилетают при системном оверлее/закрытии игры В ЛЮБОЙ момент, в т.ч. ДО восстановления мира из сейва. Сейв в этот момент сериализует пустую сцену и затирает ключ. Фикс: флаг `_loaded` (ставится в конце Load-метода, в ОБЕИХ ветках), все Save-методы начинаются с `if (!_loaded) return;`.
- **«Не загружайся из пустоты»**: на консоли сейв-данные грузятся АСИНХРОННО (`UPSSaves.Prefs.IsReady` встаёт позже первых `Start()`); `HasKey` до готовности вернёт false → система решит «новая игра». Фикс: `yield return new WaitUntil(() => UPSSaves.Prefs.IsReady);` перед Load (в Captive — через хелпер `SavesReady.IsReady`/`SavesReady.Run`, см. раздел «Паттерн миграции сохранений»). В Editor загрузка синхронная — поэтому баг в редакторе невоспроизводим, ловится только на деките.
- Туда же: НИКАКИХ `UPSSaves.Prefs.Get*/Set*/TrySave()` в `Awake` бутстрап-сцены (до `IsReady` флаги недостоверны, а ранний `TrySave()` пишет почти пустой словарь поверх реального файла — в v2 `TrySave()` до `IsReady` сам no-op, но раннее чтение всё равно вернёт дефолт).

📍 **Captive: ✅ выполнено в Фазе 1 (2026-06-20).** Реальных игровых сейвов нет — персистятся 5 ключей настроек: `Settings.Volume`/`Settings.Sensitivity` (`float`), `Settings.Vibration`/`Settings.LightBar` (`bool`), `Settings.LocaleCode` (`string`). Как сделано:
- **Хелпер `SavesReady`** (`Assets/Standard Assets/SavesReady.cs`, static): `IsReady` (try/catch-гард против NRE до создания `UPSSaves`) и `Run(Action)` — выполнить сразу, если `UPSSaves.Prefs.IsReady`, иначе по `UPSSaves.Prefs.OnReady` (а если инстанса ещё нет — сперва по статическому `UPSSaves.IsReady`, затем перенаправить на `Prefs.OnReady`). В firstpass, т.к. потребитель `SensitivitySettings` тоже там (см. «`.asmdef` изоляция»).
- `GameSettings.cs` — load в `OnEnable`, save + `UPSSaves.Prefs.TrySave()` в `OnDisable`. Открывается из MENU/паузы, т.е. **после** `IsReady` (`UPSInitializeAwaiter` не пускает MENU, пока `UPSSaves.Prefs.IsReady != true`) → прямой `UPSSaves.Prefs.Get*/Set*` безопасен. Vibration/LightBar переведены на `Get/SetBool`.
- `LocaleManager.cs` — `UPSSaves.Prefs.GetString/SetString` + `TrySave()`; чтение в корутине ждёт `WaitUntil(() => SavesReady.IsReady)` (стартует на `BeforeSceneLoad`).
- `SensitivitySettings.Boot()` и `VibrationService.Awake()` — оба на `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, т.е. **до** `IsReady` (прямой `UPSSaves.Prefs.Get*` тут вернёт дефолт и может кинуть NRE). Чтение отложено через `SavesReady.Run(...)`. Дефолты (`0.5` / `true`) держатся до callback; значения потребляются только в геймплее (после MENU) — безопасно.
- Старые `PlayerPrefs`-значения не переносились (другое хранилище) — для предрелиза это чистый сброс к дефолтам, импорт не делали.
- **Попап ошибок сейвов (добавлено в Фазе 1 по рекомендации разработчика SDK).** ⚠️ **Устарело с SDK v2.1 — в новых проектах НЕ повторять.** Паттерн эпохи v2: в бут-сцену `UPSBootstrap` добавлялся объект `[ErrorPopupHandler]` со скриптом `SaveErrorPopupHandler` (копия сэмплового `PopupErrorHandler`; синглтон + `DontDestroyOnLoad`, подписка на `ErrorHandler.OnError`) + свой префаб `ErrorPopup.prefab` — ошибки поднимались через `UpscaleSDK.Core.Saves.ErrorHandling.ErrorHandler.RaiseError(msg)`.
  **Как в v2.1:** `PS4FileSystem`/`PS5FileSystem` при storage-ошибках сами открывают **системный диалог** платформы (`Unity.SaveData.*.Dialog`: NoSpace/Corrupted) — кастомный канвас не нужен, а подписанный на `ErrorHandler.OnError` попап показал бы **дубль** (PS-код после диалога всё ещё зовёт `RaiseError`). На Xbox/Switch/Desktop в v2.1 `RaiseError` не вызывается вообще — канал спит везде. Оговорка вендора: «посмотрим, что скажет сертификация» — если серт потребует свой UX, вернуться к сэмпл-эталону `Assets/UpscaleSDK/Samples/Saves/` (`PopupErrorHandler.cs`/`ErrorPopup.cs`/`ErrorPopupPrefab.prefab`), но вешать его **только не-PS платформам** или после удаления дублирующего `RaiseError`.

### Подводный камень: `.asmdef` изоляция

Если скрипт находится в папке с файлом `*.asmdef` — он компилируется в отдельную сборку, которая по умолчанию **не видит Assembly-CSharp**. Сам `UpscaleSDK.Core` имеет свой `*.asmdef` с `autoReferenced:true` → виден предопределённым сборкам (Assembly-CSharp и firstpass) автоматически. Но если асмдеф `noAutoReferenced`/изолирован — обращение к `UPSSaves.Prefs.*` даст `CS0103: The name 'UpscaleSDK' does not exist`; добавить UpscaleSDK в `references`.

Проверить наличие `.asmdef` в папках с мигрируемыми скриптами:
```bash
find Assets/Scripts -name "*.asmdef"
```

Если `.asmdef` пустой (только `{"name": "..."}`, без `references`) — удалить его вместе с `.meta`. Скрипт перейдёт в Assembly-CSharp и увидит UpscaleSDK. Если `.asmdef` нужен по другой причине (ссылки на сторонние сборки) — добавить в `references` сборку UpscaleSDK (если у него есть свой `.asmdef`) или убрать из отдельной сборки.

📍 **Captive (словлено на миграции сейвов):** `Assets/Standard Assets/` без своего `.asmdef` идёт в **`Assembly-CSharp-firstpass`** (компилируется ДО `Assembly-CSharp`). Скрипт оттуда (`SensitivitySettings`, `MouseLook`) видит `UpscaleSDK.Core` (autoReferenced), но **НЕ видит обычные скрипты из `Assets/Scripts`** (это `Assembly-CSharp`, компилируется позже). Поэтому общий хелпер `SavesReady`, нужный и firstpass-, и обычным скриптам, положен в **`Assets/Standard Assets/`** (firstpass) — оттуда его видят обе сборки.

