> Часть porting-пайплайна. Индекс, маршрутизация и порядок чтения — `PORTING_PIPELINE.md` в этой папке.

> **Покрывает стадии 2–5** (один файл — работа над вводом и UI неразделима): Stage 2 — Input Assessment · Stage 3 — Gamepad · Stage 4 — UI Navigation · Stage 5 — Button Prompts.

> ✅ **Актуализировано под Upscale SDK v2.1** (ввод/навигация/слои/глифы; дельта v2→v2.1 — шапка `UPSCALE_SDK.md`). **Канон API — `UPSCALE_SDK.md`** (Input/UI/Saves); здесь — «что делаем и зачем», без дублирования сигнатур. Часть подводных камней **версионно-независима** (UX/архитектура) — помечены «(версионно-независимо)». Имена конкретного проекта — только в строках `📍 Пример (этот проект):`; правила формулируются обобщённо (дока кочует из проекта в проект).
>
> **Базовая модель v2.1** (детали — в `UPSCALE_SDK.md`): ввод — strongly-typed action sets (`BaseGamepadActionSet.Instance.<Action>.Press.Read()/.Performed` — до v2.1 `BaseInputActionSet`; клавиатура/мышь: `BaseKeyboardActionSet`/`BaseMouseInputActionSet`; кастомные `InputActionsSet<T>`); UI — `Layer` + `UILayersManager.ActivateLayer` (один активный слой, **не** стек приоритетов) поверх UGUI, элементы `ActionButtonElement`/`ActionSliderElement`/`ActionToggleElement`/`ActionScroll…` + навигация `LayerNavigator`+`LayerNavigation`(`Transition`-граф); глифы — наборы в `UpscaleSDKConfig` + `action.TryGetGlyph()` (**device-aware в v2.1**: спрайт сам меняется под активное устройство).

## Stage 2 — Пре-флайт: наличие SDK-ассетов (делать ПЕРВЫМ)

**До любой геймпад-работы проверь, что SDK и его ассеты на месте.** Без них глифы не резолвятся, а при неверном пути `Config` — зависание bootstrap-сцены. Проверять присутствие:

- [ ] **UpscaleSDK Core импортирован** — папка `Assets/UpscaleSDK/Core` (+ `BootstrapScene`, `Platform/Desktop`).
- [ ] **`Config.asset`** — по пути `Resources/UpscaleSDK/Config.asset` (имя ровно `Config`; подойдёт **любая** `Resources/UpscaleSDK/` папка, `Resources.Load("UpscaleSDK/Config")` зашит жёстко; см. `UPSCALE_SDK.md` → «Конфигурация»).
- [ ] **`SavesSettings`-ассет** создан и привязан в `Config`.
- [ ] **Наборы глифов** — лежат в `Assets/UpscaleSDK/Extras/Glyphs/{PlayStation4,PlayStation5,Xbox,Switch,Desktop}/...` и привязаны в `Config`: геймпадные `_ps5Set/_ps4Set/_xboxSet/_switchSet` + kb/mouse `_keyboardSet/_mouseSet` (v2.1).
- [ ] **После обновления SDK поверх старого** — проверить конфиг на молчаливые потери: v2→v2.1 переименовал поле `_psSet`→`_ps5Set` (значение теряется) и заменил PS4-ассет (новый GUID → Missing-ссылка). См. «Миграция конфига» в `UPSCALE_SDK.md`.

**⚠️ Правило (что делать по результату проверки):**
- **SDK Core импортирован, но конфигов нет** → ИИ **создаёт их сам, не дожидаясь Stage 2**: `Config` + `SavesSettings`, привязывает готовые наборы из `Extras/Glyphs` — 4 геймпадных (PS4 и PS5 — **разные** наборы!) + kb/mouse из `Desktop/White` (v2.1). Пошаговая процедура — `UPSCALE_SDK.md` → «Конфигурация (ассеты SDK)».
- **SDK Core НЕ импортирован** (нет `Assets/UpscaleSDK/Core`) → остановиться и спросить: «Импортировал ли ты Upscale SDK Core и ресурсы с глифами?»
- **Не хватает конкретного набора/ассета** (нет набора под нужную платформу, нет `SavesSettings`, нет спрайтов в `Extras`) → **НЕ выдумывать и не дублировать чужой набор**: спросить разработчика, какой ассет использовать / где взять / нужна ли эта платформа.

> **Отделяй «ввод работает» от «глифы/bootstrap готовы».** Само чтение `BaseGamepadActionSet.Instance.<…>.Read()` от `Config` НЕ зависит (набор само-инициализируется, `IsActive=true`) — геймплейный ввод заработает даже без `Config`. `Config` нужен для **глифов** (Stage 5) и **bootstrap-сцены** (`ConfigurationProvider.Initialize`). Поэтому пре-флайт обязателен именно перед глифами/подсказками; чистый игровой слой можно делать и раньше, но проверку всё равно проведи и зафиксируй пробелы.

## Stage 2 — Input System Assessment

- [ ] Определить тип Input System: Legacy (`Input.GetKeyDown`) / New (`InputSystem`) / Hybrid
- [ ] Проверить наличие `*.inputactions` с геймпад-биндингами
- [ ] Найти весь touch-специфичный код (`Input.touchCount`, `Touch.*`, on-screen джойстики)
- [ ] Найти весь мышиный/клавиатурный код в UI-скриптах
- [ ] Задокументировать: какие actions нужны (Move, Look, Attack, Interact, UI Navigate, UI Submit, UI Cancel)

> **v2 требует New Input System** (валидатор проверяет; см. `UPSCALE_SDK.md`). Но это **не** значит, что Legacy kb/mouse надо переписывать.
>
> **Стратегия «гибрид» (рекомендуется, если игра уже работает на Legacy `Input.*`):** не трогаем существующее клавиатуро-мышиное чтение, а **добавляем** геймпад через SDK **OR-чтением в той же точке**: `if (Input.GetKeyDown(KEY) || BaseGamepadActionSet.Instance.<Action>.Press.Read())`. Так одно действие обрабатывается одним кодом для обоих устройств. Авто-переключение «kb↔геймпад» для подсказок/курсора — по детекту устройства (v2.1: `UPSInput.GetPrimaryDevice()` — на консоли всегда Gamepad, на ПК геймпад приоритетнее клавы/мыши; события `OnDeviceAdded/Removed`; при желании — тонкая обёртка-синглтон с `bool GamepadActive` + событием). 📍 Пример (этот проект): обёртка `InputDeviceManager` (`GamepadActive`, `OnInputDeviceChanged`), самобутстрап через `[RuntimeInitializeOnLoadMethod]` — написана до v2.1, внутренности можно упростить до `GetPrimaryDevice()`.
>
> **Альтернатива с v2.1 — «kb/mouse тоже через SDK»:** клавиатура и мышь теперь первоклассные источники (`BaseKeyboardActionSet`/`BaseMouseInputActionSet`, `.ToKey()`/`.ToMouseButton()` в кастомном наборе, kb-источники в `*ActionDefinition`). Плюсы: одно действие = один биндинг на все устройства (без OR-паттерна), глиф device-aware сам, `InputActionsSet.Deactivate()` глушит kb/mouse наравне с геймпадом (Legacy `Input.*` он не глушит). Минус: переписывание существующего Legacy-чтения — объём и риск регрессий. **Правило: существующий Legacy-код не трогаем (гибрид), но НОВЫЙ ввод порта (хоткеи, кастомные наборы, UI-биндинги) заводим через SDK-источники.** Финальное решение — с разработчиком на старте Stage 2.
>
> Чтобы геймпадные оси не давали двойной ввод поверх Legacy (joystick на тех же осях `Horizontal/Vertical`) — нейтрализовать joystick-вариант осей в `ProjectSettings/InputManager.asset` (см. gotcha про legacy EventSystem в 3.3).

## Stage 3 — Gamepad Implementation

Реализуется **послойно**: каждый UI/механика — отдельный слой. Начинать с главного игрового слоя (управление персонажем/транспортом), потом UI-слои (меню, магазины, навигаторы и т.д.).

### 3.1 Анализ и план

- [ ] Проанализировать главную игровую сцену: как происходит управление персонажем/транспортом, как устроен игровой UI
- [ ] Найти все мобильные on-screen кнопки и touch-обработчики (`IPointerDownHandler/IDragHandler/IPointerUpHandler`, `Input.touchCount`)
- [ ] Найти все point-зависимые от мобильной платформы проверки (`Application.isMobilePlatform`) — оценить, какие нужно заменить на консольную проверку, какие удалить
- [ ] Составить список механик/слоёв, где нужно добавить геймпад-управление (геймплей + каждый UI экран отдельно)
- [ ] **Уточнить у разработчика маппинг кнопок для каждого слоя** — на разных слоях одна и та же физическая кнопка может означать разное (например, бамперы = переключение радиостанций в геймплее и переключение вкладок в магазине)

### 3.2 Главный игровой слой

- [ ] Камера: правый стик → вращение. `BaseGamepadActionSet.Instance.RightStick.Read()` в `Update()` (+ дедзона); при гибриде — поверх существующего mouse-look (см. `UPSCALE_SDK.md` → Input).
- [ ] Движение: левый стик — `BaseGamepadActionSet.Instance.LeftStick.Read()` (OR с Legacy-осями, если гибрид).
- [ ] Триггеры/кнопки → действия игрока через гибрид-OR: `Input.GetMouseButton/GetKeyDown(...) || BaseGamepadActionSet.Instance.<Action>.Press.Read()` (или `.RightTrigger.Read() > 0.5f` и т.п.). Раскладку взять из обсуждения с разработчиком.
- [ ] Мобильные on-screen кнопки → **переиспользовать как подсказки** (не скрывать): внутри кнопки виден либо keyboard-хинт, либо glyph геймпад-кнопки.
- [ ] Touch-обработчики удалить, если игра не пойдёт на мобильные платформы.
- [ ] UI-кнопки действия (HUD/меню) — через `ActionButtonElement` поверх UGUI Button (см. «Паттерн UI-кнопки v2» ниже).

#### Карта механизмов ввода — один механизм на действие

**Главная причина «плавающих» багов ввода на порте — в проекте сосуществует несколько независимых механизмов обработки нажатий, и одно действие/кнопка случайно проходит через два из них.** Перед написанием кода составь карту и держись одного механизма на действие.

Механизмы v2:

| Механизм | Что это | Когда уместен |
|---|---|---|
| **Прямое гибрид-чтение** (`Input.X \|\| BaseGamepadActionSet.Instance.<Action>.Read()` в `Update`) | одно действие на оба устройства; слои НЕ гейтят — гасить вручную (флаг/`Deactivate`) | геймплей, мировое взаимодействие по прицелу, hold |
| **`ActionButtonElement`** (поверх UGUI Button, в `Layer`) | по input дёргает `_button.onClick`; гейтится `Layer.IsEnabled` + фокус | HUD/модальные кнопки с `onClick` |
| **`ActionSliderElement` / `ActionToggleElement`** (поверх UGUI Slider/Toggle, в `Layer`) | DPad L/R меняет слайдер; Apply флипает тогл; гейтится слоем+фокусом | строки настроек |
| **`LayerNavigator` + `LayerNavigation`(`Transition`-граф)** | перемещение фокуса по элементам слоя (DPad/стик) | навигация по списку/меню |

> `ActionToggleElement` **с v2.1 — штатный SDK-компонент** (в v2 его не было и писался свой; см. предупреждение о конфликте имён в «Паттерн UI-кнопки»). Там же в v2.1 добавлены `ActionScrollViewElement`/`ActionScrollbarElement` для прокрутки.

**Правила-доктрина:**

1. **На Stage 3.1 инвентаризируй все пути ввода** — grep: `BaseGamepadActionSet`, `.Press.Read()`, `.Performed`, `Input.GetKeyDown`, `Input.GetMouseButton`, `onClick.AddListener`, `ActionButtonElement`, `UILayersManager.ActivateLayer`. Составь карту «**физическая кнопка × контекст → действие × механизм**».
2. **Одно действие = один механизм.** Не давай одной кнопке и прямое чтение, и `ActionButtonElement`-хоткей на одно действие — двойной фаер (см. «двойное срабатывание»).
3. **Одна физическая кнопка в одном контексте = одно активное действие.** В v2 нет индексов/приоритетов слоёв — конфликт решается тем, что **активен лишь один `Layer`** (`UILayersManager`), а внутри слоя — фокусом (`_checkIsFocused`).
4. **Выбор механизма по типу действия:**

| Тип действия | Механизм |
|---|---|
| Мировое взаимодействие/геймплей | прямое гибрид-чтение `Input.X \|\| BaseGamepadActionSet.<Action>.Read()`; гейтить флагом/`Deactivate()` при открытом UI |
| HUD/модальная кнопка с `onClick` | `ActionButtonElement` (Apply/Cancel) на UGUI Button, base `_layer` = слой этого экрана |
| Слайдер/тогл в панели | `ActionSliderElement` / `ActionToggleElement` |
| Прокрутка списка/окна | `ActionScrollViewElement` (ScrollRect) / `ActionScrollbarElement` (v2.1) |
| Перемещение фокуса по элементам | `LayerNavigator` + граф `Transition` (не свой обработчик DPad) |

Нарушение правил 2/3 проявляется как gotcha-кейсы ниже («прямое чтение обходит слой», «двойное срабатывание», «взаимоисключение видимости»).

#### Паттерн UI-кнопки v2 (`ActionButtonElement`)

UI-кнопка, которую должен дёргать геймпад — это **UGUI `Button` + компонент `ActionButtonElement`** (поверх него). По нужному действию (`BoolActionDefinition`) SDK сам вызывает `_button.onClick.Invoke()` — тот же `onClick`, что и мышь. Глиф рисуется тем же компонентом. Кнопку не нужно вручную подписывать на ввод — действие идёт через `BoolActionDefinition`.

**Поля `ActionButtonElement`** (`[RequireComponent(typeof(Button))]`):

| Поле | Значение | Заметка |
|---|---|---|
| `_definition` | `BoolActionDefinition` — обычно `UiButtonApply` / `UiButtonCancel` (из `Core/UI/Runtime/DefaultDefinitions/`) | по этому действию дёргается `onClick` |
| `_button` | UGUI `Button` на том же объекте | без него компонент сам себя `enabled=false` |
| `_layer` (база `UIElement`) | `Layer`-инстанс **своего** экрана | `OnPerformed` первой строкой проверяет `Layer.IsEnabled` |
| `_checkIsFocused` | `true` для кнопок в навигируемом списке; `false` для «глобальных» в слое (Close по Cancel и т.п.) | при `true` срабатывает только когда элемент в фокусе |
| `_checkIsInteractable` | `true` | уважать `Button.interactable` |
| `_showIcon` | `true` чтобы показывать глиф | иначе иконки нет |
| `_iconImage` | дочерний `Image` под глиф | сюда кладётся спрайт глифа |
| `_corner` / `_offset` / `_scale` | позиция/размер глифа (enum `Corner` — **последовательный** 0..9: Free, UpperLeft…LowerRight) | `ApplyIconPosition()` ставит rect глифа по этим полям в `OnValidate` |

**Эдиторное превью глифа бесплатно:** при `_showIcon=true` `ActionButtonElement.OnValidate` дёргает glyph-provider и ставит спрайт в `_iconImage` прямо в эдиторе (в редакторе без устройства `GetGamepadType` отдаёт Nintendo). В рантайме обновляется под подключённую платформу. Это **единственный** SDK-компонент с живым превью — для статичных подсказок-не-кнопок превью ставим вручную (см. «правило эдиторного превью»).

**Тогл — с v2.1 штатный `ActionToggleElement`** (`UpscaleSDK.Core.Ui.Runtime`, `[RequireComponent(Toggle)]`): `_toggle` + `BoolActionDefinition` (обычно `UiButtonApply`); в `OnPerformed` стандартные гарды (`Layer.IsEnabled`/`IsEnabled`/`IsInFocus`/`_toggle.interactable`) → `_toggle.isOn = !_toggle.isOn`. Свой писать больше не надо.
> ⚠️ **Конфликт имён при апдейте старого v2-проекта:** если в проекте уже есть самописный `ActionToggleElement` (этот паттерн рекомендовался до v2.1), после импорта v2.1 появится второй класс с тем же именем. Самописный без namespace с SDK-классом (он в `UpscaleSDK.Core.Ui.Runtime`) не столкнётся компилятором, но останется путаницей — мигрировать префабы на SDK-компонент и удалить свой (или как минимум переименовать).

**Фокус-фидбэк** (scale + показ глифа только у сфокусированной кнопки при геймпаде) модуль не рисует сам — это отдельный компонент на `UIElement`, подписанный на `OnFocusChanged` + детект устройства. 📍 `FocusScale` (scale 1.1, тоггл дочернего `glyph`).

**Запреты:**
- Не вешать на одну кнопку и `ActionButtonElement`, и отдельный ручной обработчик ввода на тот же `onClick` — двойной фаер.
- `_definition`/`_button`/`_layer` обязательны — без `_layer` `OnPerformed` упадёт на `Layer.IsEnabled` (NRE), без `_definition`/`_button` компонент выключит себя.

#### Паттерн: добавление глифа + хоткея на существующую UI-кнопку

У кнопки уже есть `UnityEngine.UI.Button` (+ опц. PC-хинт). Нужно: (а) глиф геймпад-кнопки, (б) хоткей с геймпада, (в) автопереключение kb↔gamepad.

**1. Добавить `ActionButtonElement` на тот же GameObject** (см. таблицу полей выше). `_definition` = `UiButtonApply` (или нужное действие), `_button` = этот Button, `_layer` = слой экрана, `_checkIsFocused` по ситуации. Хоткей готов — по действию дёргается существующий `onClick`.

**2. Глиф.** Создать дочерний `Image` под глиф (raycastTarget off), указать его в `_iconImage`, `_showIcon=true`, выставить `_corner`/`_offset`/`_scale`. `ActionButtonElement.ApplyIconPosition()` сам позиционирует глиф по этим полям (в т.ч. в эдиторе через `OnValidate`) — **не двигай RectTransform глифа вручную**, его перезатрёт на ближайшем OnValidate; меняй `_corner`/`_offset`.

**3. Превью в эдиторе.** При `_showIcon=true` глиф виден сразу в эдиторе (provider в `OnValidate`). Дополнительно можно положить статичный превью-спрайт в `_iconImage.sprite` (см. «правило эдиторного превью»).

**4. Переключение kb↔gamepad.** С v2.1 глиф `ActionButtonElement` device-aware сам: показывается, когда подключено устройство одного из источников биндинга, спрайт выбирается по `GetPrimaryDevice()` и обновляется на `OnDeviceAdded`/`OnDeviceRemoved`. Если в `BoolActionDefinition` добавлен и kb-источник — вместо ручного «PC-хинт ↔ геймпад-глиф» кнопка сама покажет клавишу без геймпада и глиф пада с ним (нужен привязанный `_keyboardSet`). Ручная схема «два под-объекта + детект устройства» остаётся для случаев, где ввод kb остался на Legacy `Input.*` (SDK о клавише не знает) или где PC-хинт — текст, а не спрайт. 📍 В этом проекте — `FocusScale` тогглит дочерний `glyph` (показывает только при фокусе+геймпаде через `InputDeviceManager.GamepadActive`/`OnInputDeviceChanged`); PC-хинт — отдельный под-объект, активный когда геймпада нет.

> Lifecycle подписки на детект устройства — `OnEnable`/`OnDisable` (при `SetActive(false)` отписка авто; при активации сразу пересчитать состояние). Видимостью **самой** кнопки (показать/скрыть целиком) занимается другой код — это отдельная ответственность от «kb↔gamepad внутри кнопки».

---

#### Чеклист: добавление новой HUD-кнопки с подсказкой PC + контроллер (v2)

Применяется к любой UI-кнопке с `UnityEngine.UI.Button`.

1. **`ActionButtonElement` на GameObject кнопки** — `_definition` (`UiButtonApply`/нужное действие), `_button`, base `_layer` = слой этого HUD/экрана, `_checkIsFocused` по ситуации.
2. **Глиф** — дочерний `Image` (raycastTarget off) → в `_iconImage`, `_showIcon=true`, выставить `_corner`/`_offset`/`_scale`. Статичный превью-спрайт в `_iconImage.sprite` (см. «правило эдиторного превью»).
3. **PC-хинт** (если нужен текст/спрайт клавиши) — отдельный под-объект; переключение видимости kb↔gamepad по детекту устройства (📍 `FocusScale`/обёртка над `InputDeviceManager`).
4. **Слой экрана активируется через `UILayersManager`** (см. 3.3) — иначе `ActionButtonElement` не гейтится корректно.
5. **Verify** — Play без геймпада: PC-хинт. Подключить геймпад: появляется глиф, Apply дёргает `onClick`. Отключить — обратно.

#### Паттерн: контекстная подсказка, которая НЕ кнопка (in-world prompt)

Не каждая подсказка «нажми X» висит на `UI.Button`. Часто это просто строка в HUD/world-space, появляющаяся по триггеру/состоянию (*«press X to jump / climb / open»*) — без клика, фокуса и Layer. Для неё **не нужны** ни `ActionButtonElement`, ни глиф-машинерия кнопки, ни навигация. Минимальный рецепт (v2):

- **Два объекта в сцене, оба inactive:** kb-вариант (текст с `[KEY]`) и геймпад-вариант (строка `текст + Image-глиф`, напр. через `HorizontalLayoutGroup`, чтобы глиф встал по центру строки). PC-объект не трогаем — это и есть «оставить подсказку для клавы/мыши».
- **Выбор по детектору устройства:** показывать геймпад-вариант при `GamepadActive` (📍 в этом проекте — `InputDeviceManager.GamepadActive`), иначе kb-вариант. Хелпер `ShowHelp(bool)` гасит оба при скрытии и включает нужный при показе.
- **Спрайт глифа — прямо из экшена**, без Button/`_style`: `Image.sprite = BaseGamepadActionSet.Instance.<Action>.Press.TryGetGlyph()` (в try/catch). Глиф сам соответствует платформе (PS/Xbox/Switch), а с v2.1 — и устройству: у действия с kb-источником `TryGetGlyph()` на клавиатуре отдаст спрайт клавиши (`GetPrimaryDevice()`).
- **Переключение на лету:** если подсказка уже показывается из тикающего места (`Update`/`OnTriggerStay`), выбирай нужный объект **каждый кадр** — подписка на событие смены устройства не нужна. Подписываться на `OnInputDeviceChanged` стоит только когда показ разовый (по событию, без пер-кадрового тика).
- **Ввод оставить гибридным:** `if (Input.GetKeyDown(KEY) || BaseGamepadActionSet.Instance.<Action>.Press.Read())` — обработка одна на оба устройства, подсказка лишь меняет вид.
- **⚠️ Превью глифа в эдиторе (правило проекта):** `Image`-подсказка, чей `sprite` проставляется только в рантайме (`TryGetGlyph`/`RefreshHints`), в эдиторе рисуется **белым квадратом** — визуал не оценить. **Всегда проставляй в префабе статичный превью-`sprite`** одной платформы (📍 в этом проекте — из `Assets/Resources/UpscaleSDK/Glyphs/XboxIconsSet.asset` через `GamepadIconsSet.GetGlyph(GamepadGlyph)`; для Cancel — `East`). Рантайм всё равно перезапишет его глифом подключённой платформы. Сэмплы `Samples/Input/Glyphs/Gamepad/*` для этого НЕ годятся — они тоже ставят спрайт в `Start()` (рантайм), эдиторного превью не дают. Единственный SDK-компонент с живым превью в эдиторе — `ActionButtonElement` (`OnValidate`→glyph-provider), но он для интерактивных кнопок (`_showIcon`), не для статичных подсказок.

> 📍 Пример (этот проект): `EndingManager` (прыжок в проём в концовке) — поля `helpText` (kb) / `helpTextGamepad` + `jumpGlyph` (Image), `ShowHelp(bool)` выбирает по `InputDeviceManager.GamepadActive` каждый кадр в `OnTriggerStay`, глиф = `Apply.Press.TryGetGlyph()`. Ввод `Space || Apply.Press.Read()` был гибридным изначально — добавлен только визуал.

#### Правило изоляции кнопок между слоями (v2)

Одна и та же физическая кнопка может означать разное на разных экранах (геймплей, меню, магазин, диалог). Нажатия **не должны протекать между слоями**.

**Модель v2 — один активный слой (`UILayersManager`), без индексов/приоритетов:**

- Каждый экран = **свой** `Layer` (с `Key`); каждый `UIElement` (`ActionButtonElement`/слайдер/тогл) ссылается на `_layer` **своего** экрана.
- `UILayersManager.ActivateLayer(x)` гасит предыдущий активный слой (`IsEnabled=false`) и включает новый. `ActionButtonElement.OnPerformed`/`LayerNavigator.OnNavigate` первой строкой проверяют `Layer.IsEnabled` → элементы неактивного слоя молча игнорируют ввод. Ручных индексов/приоритетов слоёв **нет**.
- ⚠️ **Базовый слой обязан быть активирован через менеджер.** `Layer.IsEnabled` по умолчанию `true`; если базовый экран свой слой ни разу не `ActivateLayer`-нул, он `IsEnabled=true`, но не `CurrentActiveLayer` — тогда открытие нового окна его **не погасит**, и ввод пойдёт в оба слоя. Вешай `ActivateLayerTrigger` (Start, Reference) на корень базового экрана. (Канон и пример — `UPSCALE_SDK.md` → «Слои UI».)
- **Геймплейное чтение слои НЕ гейтят** (см. «прямое чтение обходит слой»): код, читающий `BaseGamepadActionSet`/`InputAction` напрямую — гасить флагом или `InputActionsSet.Deactivate()` при открытии UI.

### 3.3 UI-слои (магазины, меню, диалоги)

Для каждого слоя:
- [ ] Повесить компонент `Layer` (с уникальным `Key`) на корневой Canvas/Panel экрана.
- [ ] Все интерактивные элементы слоя — `ActionButtonElement`/`ActionSliderElement`/`ActionToggleElement` с `_layer` = этот `Layer`.
- [ ] Навигация по элементам — `LayerNavigator` (читает `UiLayerNavigation`) + `LayerNavigation` (граф `Transition`). **Не** Unity `Selectable.Navigation`.
- [ ] Открытие экрана: `UILayersManager.ActivateLayer(thisLayer)` (гасит предыдущий) **+** заглушить геймплейное чтение (флаг/`Deactivate()`). Закрытие: вернуть предыдущий слой + снять заглушку.

#### Паттерн: открытие/закрытие слоя и возврат предыдущего

Слой активируется **через менеджер**, а не «само по `SetActive`». В v2 `SetActive(true)` на панели сам слой в `UILayersManager` НЕ активирует — нужен явный вызов (или `ActivateLayerTrigger`).

```csharp
// На открытии экрана (OnEnable панели / по кнопке):
_prevLayer = UILayersManager.CurrentActiveLayer;   // запомнить, чтобы вернуть
UILayersManager.ActivateLayer(myLayer);
// + заглушить геймплей: gameplaySet.Deactivate()  ИЛИ свой флаг GameplayInputBlocked=true

// На закрытии:
UILayersManager.ActivateLayer(_prevLayer);          // null → активного слоя нет (ОК)
// + gameplaySet.Activate() / GameplayInputBlocked=false
```

- **Базовый/постоянно присутствующий экран** (главное меню, World-Space HUD) активирует свой слой на старте через `ActivateLayerTrigger` (TriggerType=Start, FindType=Reference) — иначе сработает gotcha «базовый слой не активирован» (см. «Правило изоляции», `UPSCALE_SDK.md`).
- **Стек модалок** (окно поверх окна): паттерн `_prevLayer` сам собой возвращает нижний слой при закрытии верхнего. Геймплейный гейт при стеке — **save/restore** (запомнить прежнее значение флага и вернуть его, а не безусловно снимать), иначе закрытие верхней модалки снимет гейт, пока нижняя ещё открыта.
- **Панель через `SetActive`**: активацию слоя удобно делать в `OnEnable`/`OnDisable` панели. **Панель через CanvasGroup alpha** (GameObject остаётся активным) — её элементы ловят ввод, пока слой явно не погашен: на старте `Layer.SetEnabled(false)`, активировать только при открытии.

#### Паттерн: навигация фокусом внутри UI-слоя

**Базовый случай — штатный `LayerNavigator` + `LayerNavigation` (граф `Transition`).** Перемещение фокуса по элементам слоя (DPad/левый стик) делает `LayerNavigator` (читает `UiLayerNavigation` с авто-повтором `DelayVector2Processor`), а граф направлений (north/south/west/east на каждый элемент) задаётся в `LayerNavigation`. Граф можно:
- **забить в инспекторе** (`SetTransitions`-массив + `_defaultElement`) — для фикс-набора элементов;
- **построить кодом** в `OnEnable` (`SetDefaultElement(first)` + `SetTransitions(...)`) — когда набор **условный/динамический** (например, платформенная строка скрыта `#if`, и её надо исключить из wrap-around). 📍 У нас граф настроек строится кодом из активных строк.

Фокус сбрасывать на дефолт при открытии: `navigator.SelectElement(defaultElement)`.

**Когда нужен свой обработчик** (циклические табы по бамперам, нестандартная логика выбора, hold-to-repeat по контенту) — читать ввод **напрямую** из `BaseGamepadActionSet.Instance`:

```csharp
private void Update()
{
    if (!_panelRoot.activeInHierarchy) return;          // guard: только когда панель открыта
    var a = BaseGamepadActionSet.Instance;
    if (a.DpadUp.Press.Read())        _controller.NavigatePrev();
    else if (a.DpadDown.Press.Read()) _controller.NavigateNext();
    // бамперы для табов: a.LeftShoulder.Press.Read() / a.RightShoulder.Press.Read()
}
```

> ⚠️ Прямое чтение `BaseGamepadActionSet` слоями **не гейтится** (см. gotcha «прямое чтение обходит слой») — обязателен guard (панель открыта / `Layer.IsEnabled` / флаг), иначе обработчик сработает и в геймплее, и под открытой модалкой.

**Расширения паттерна (навигация по сетке/списку карточек) — версионно-независимая UX-механика:**
- **Hold-to-repeat:** для перемещения **фокуса** авто-повтор уже встроен в `UiLayerNavigation`/`LayerNavigator` (`DelayVector2Processor`) — ручной поллинг не нужен. Если листаешь **контент** своим обработчиком, читай `BaseGamepadActionSet.Instance.DpadDown.Hold.Read()` с таймером (`initialDelay`→`repeatInterval`). Одну и ту же кнопку не обрабатывай и в разовом, и в hold-пути (двойной шаг).
- **Фокус** обычно `transform.localScale *= ~1.05–1.1`. В `GridLayoutGroup` scale не ломает раскладку (grid задаёт size/pos, не scale).
- **Scroll-into-view** для `ScrollRect`: `Canvas.ForceUpdateCanvases()`, перевести corner'ы карты в локаль `viewport`, сравнить с `viewport.rect` верх/низ и сдвинуть `content.anchoredPosition`.
- **Per-card глифы**: положи `ActionButtonElement`(`_showIcon`) + `FocusScale` (или статичный глиф-`Image`) в сам card-prefab детьми кнопок (`raycastTarget=off`, inactive) — позиции трекают кнопки при ресайзе. Показывай только у карты в фокусе.
- **Кнопки карты ищи по имени** (`Minus_Btn/Plus_Btn/AddToCart_Btn`) и зови `button.onClick.Invoke()` — если у разных типов карт имена совпадают, один generic-скрипт работает на всех.
- **Заблокированный элемент**: фокус ОСТАВИТЬ (игрок видит контент), но скрыть глифы и заблокировать действия (`if (IsLocked) return;`), а не выкидывать из навигации. Статус блокировки проверяй в момент фокуса/нажатия, не кэшируй.

**Гетерогенные карты без общего интерфейса (реальный кейс — Manage: `ShopManageCardGamepadNav`, 5 разных типов карт License/Growth/Service/Item/Design, каждый со своим скриптом, без общей базы):**
- Не пиши per-type детект состояния. Ищи **единый рантайм-сигнал**. Здесь: кнопка действия у всех названа одинаково (`Purchase_Btn`) И прячется (`SetActive(false)`) когда пункт «израсходован»/куплен. → `actionBtn.activeInHierarchy` = «доступно/навигируемо»; скрытая → пропуск. Это заменяет 5 разных `IsPurchased()`-проверок одним условием.
- Действие зови по имени (`Press("Purchase_Btn")`) — попадаешь в штатный `onClick`-обработчик карты со всеми его проверками уровня/денег (warn при недоступности). Нет общего «lock-флага» → не пытайся скрывать глиф по доступности, покажи на всех «не-израсходованных», пусть обработчик сам warn'ит.
- **Advance-on-consume**: после действия, которое убирает карту из навигируемых (покупка прячет `Purchase_Btn`), переведи фокус на следующую навигируемую (по sibling-порядку), иначе фокус «застрянет» на неактивной.
- **Цикл вкладок**: 2 вкладки → фикс-таргеты (`ShoulderLeft`=A, `ShoulderRight`=B); N вкладок (5 в Manage) → относительный prev/next с `current` + `Mathf.Clamp`. Кнопки бери `[SerializeField] Button[] tabs` в порядке ряда.
- **Ось скролла не предполагай**: один `ScrollRect` в списке оказался горизонтальным — `ScrollIntoView` делай по `scrollRect.vertical`/`scrollRect.horizontal` (обе ветки), а не только по Y.

#### Паттерн: модальная Settings/Pause-панель

Меню настроек, паузы, диалоги — модальные панели, блокирующие ввод нижних слоёв (часто замораживают игру). Типовой набор, повторяется почти в каждом проекте:

- Параметры: громкости (Music/SFX), чувствительность стика, тоглы вибрации/Light Bar (PS), субтитры, язык.
- Открывается по `Start`/паузе, закрывается по `Cancel` (и/или своей кнопкой Close).
- Если игра замораживается — `Time.timeScale = 0` + пауза зацикленных SFX (см. п.3–4, версионно-независимо).
- DPad/левый стик — навигация по строкам (фокус-скейл); DPad L/R меняет слайдер; Apply флипает тогл.
- Сохранение — `UPSSaves.Prefs` (Upscale SDK v2), batch-flush на закрытии (`TrySave()`).

**1. Структура (v2):**

```
SettingsPanelRoot            ← контроллер; показ через SetActive ИЛИ CanvasGroup alpha
  Panel
    Layer (Key="Settings")   ← + LayerNavigator (UiLayerNavigation) + LayerNavigation
    SoundRow:        Slider + ActionSliderElement(UiSlider)        + FocusScale
    SensitivityRow:  Slider + ActionSliderElement(UiSlider)        + FocusScale
    VibrationRow:    Toggle + ActionToggleElement(UiButtonApply)   + FocusScale
    LightBarRow:     Toggle + ActionToggleElement(UiButtonApply)   + FocusScale   ← скрыт вне PS/редактора
    LanguageButton:  Button + ActionButtonElement(UiButtonApply)   + FocusScale   ← открывает под-попап
    CloseButton:     Button + ActionButtonElement(UiButtonCancel, _checkIsFocused=false)  ← не в графе навигации
    Hints            ← глифы DPad/Cancel, видны при геймпаде, со статичным превью-спрайтом
```

Каждый `UIElement._layer` = этот `Layer`. Слой включается через `UILayersManager.ActivateLayer` на открытии (в v2 «само от SetActive» слой в менеджере не активируется).

**2. Контроллер (v2) — порядок открытия/закрытия:**

```csharp
// Открытие:
LoadValues();                                  // UPSSaves.Prefs → значения в контролы
ConfigurePlatformRows();                        // #if UNITY_PS4||UNITY_PS5||UNITY_EDITOR — иначе LightBarRow.SetActive(false)
BuildNavGraph();                                // граф из АКТИВНЫХ строк (скрытую LightBarRow исключить из wrap-around)
_prevLayer = UILayersManager.CurrentActiveLayer;
UILayersManager.ActivateLayer(settingsLayer);
_prevBlocked = GameplayInputBlocked; GameplayInputBlocked = true;   // или gameplaySet.Deactivate() — save/restore при стеке!
navigator.SelectElement(firstRow);              // сброс фокуса на верх

// Закрытие (Close по Cancel / своей кнопке):
SaveValues(); UPSSaves.Prefs.TrySave();         // batch flush
UILayersManager.ActivateLayer(_prevLayer);
GameplayInputBlocked = _prevBlocked;            // вернуть прежнее (а не безусловно false) — иначе снимет гейт нижней модалки
```

- **Apply/Cancel/слайдеры/тоглы вручную НЕ читаем** — их обрабатывают `ActionButtonElement`/`ActionSliderElement`/`ActionToggleElement` (гейтятся слоем+фокусом). Свой `Update` нужен максимум для Cancel под-попапа.
- **Граф навигации с условными строками — строй кодом** (`SetDefaultElement`+`SetTransitions` из активных строк). 📍 У нас `GameSettings.BuildNavigationGraph` собирает `Transition[]` из `navOrder.Where(activeInHierarchy)` с wrap-around.
- **Под-попап (выбор языка):** свой `Layer`; открытие — `ActivateLayer(popupLayer)` (гасит слой настроек), закрытие — `ActivateLayer(settingsLayer)`. Гонки «Apply открыл и тут же закрыл попап» / «Cancel закрыл попап и заодно окно» закрывай **frame-guard** (`if (Time.frameCount == _toggleFrame) return;` симметрично на open/close — см. «двойное срабатывание»).

**3. Анимация открытия и пауза игры:**

```csharp
private void AnimatePanelOpen()
{
    settingsPanel.SetActive(true);
    _panelCanvasGroup.alpha = 0f;
    _panelRect.localScale = new Vector3(0.85f, 0.85f, 1f);
    Time.timeScale = 0f;
    _panelCanvasGroup.DOFade(1f, panelAnimDuration).SetUpdate(true);   // SetUpdate(true) = unscaled time
    _panelRect.DOScale(Vector3.one, panelAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
}

private void AnimatePanelClose()
{
    DOTween.Sequence()
        .Join(_panelCanvasGroup.DOFade(0f, panelAnimDuration))
        .Join(_panelRect.DOScale(new Vector3(0.85f, 0.85f, 1f), panelAnimDuration))
        .SetUpdate(true)
        .OnComplete(() =>
        {
            settingsPanel.SetActive(false);
            Time.timeScale = 1f;
            SoundController.Instance?.ResumeLoopingSfx();
        });
}
```

**Критично**: все DOTween в модальной панели — с `.SetUpdate(true)` (unscaled time). Без этого `Time.timeScale = 0` остановит и сами анимации, и панель не откроется.

**4. Pause/Resume зацикленных SFX:**

`Time.timeScale = 0` останавливает игровое время, но `AudioSource` продолжает играть в реальном времени. Зацикленные SFX (двигатель машины, ambience с loop) при паузе звучат жутко: фоновая музыка продолжает идти на полную, и игрок физически слышит, что игра «не на паузе».

Решение — в звуковом менеджере (`SoundController` или аналог) хранить словарь активных loop-источников и явно ставить их на паузу:

```csharp
private readonly Dictionary<SoundID, AudioSource> _loopSources = new();

public void PauseLoopingSfx()
{
    foreach (var src in _loopSources.Values)
        if (src != null && src.isPlaying) src.Pause();
}

public void ResumeLoopingSfx()
{
    if (IsSfxMuted) return;
    foreach (var src in _loopSources.Values)
        if (src != null) src.UnPause();
}
```

Вызвать `PauseLoopingSfx()` в `OpenSettings()`, `ResumeLoopingSfx()` — в `OnComplete` закрывающей анимации.

**5. Навигация по строкам (v2):** делает `LayerNavigator` + граф `Transition` (DPad/стик перемещают фокус), значения меняют сами `ActionSliderElement`/`ActionToggleElement` (DPad L/R — слайдер, Apply — тогл). Ручной `MoveFocus`/`AdjustFocused` **не пишем**. Что остаётся на контроллере:
- **Условные строки:** `#if UNITY_PS4 || UNITY_PS5 || UNITY_EDITOR` показать/скрыть платформенную строку (Light Bar), и **исключить скрытую из графа** (граф строй кодом из активных строк).
- **Фокус-фидбэк:** `FocusScale` на каждой строке (scale ~1.1 при фокусе+геймпаде; отменяй предыдущий твин перед новым — `LeanTween.cancel`/`DOKill`).

**6. Batch-save:** хендлеры контролов пишут значения только в память; один flush на закрытии — `UPSSaves.Prefs.TrySave()` (или `PlayerPrefs.Save()`). Не сохранять после каждого изменения слайдера (на консолях упрёшься в rate limit `Save()` — см. Stage 6).

**7. Связь с существующими сервисами:**

Не плодить логику внутри `SettingsView` — он только UI-слой. Сами значения хранят и применяют сервисы:

- `SoundController.SetMusicVolume(float)` / `SetSfxVolume(float)` — раздельные ключи `MusicVolume`/`SfxVolume`
- `VibrationService.SetEnabled(bool)` — глобальная вкл/выкл вибрации (заготовка-сервис, см. ниже)
- Light Bar / LED PS-контроллера — `LightBarService.SetEnabled(bool)` (заготовка-сервис, см. ниже). **Делается в фазе геймпада, НЕ на Stage 6:** в SDK v2 API подсветки нет → напрямую через Unity Input System (`SetLightBarColor`), работает в Editor/Win, платформенный PS-пакет для этого не нужен (PS-ветки за `#if`)
- Чувствительность обзора — static-сервис со множителем, читается контроллером камеры (📍 Captive: `SensitivitySettings.Multiplier` в `MouseLook`)

В `Awake` контроллера панели (или в bootstrap) вызвать `XxxService.Load()` чтобы поднять значения из сейвов (`UPSSaves.Prefs`/`PlayerPrefs`) в память до первого открытия.

> **Сервис без потребителя — мёртвая настройка (версионно-независимо).** Static-сервис вида `XxxService.Value` + сохранение в `UPSSaves.Prefs`/`PlayerPrefs` сам по себе ничего не делает. На стороне геймплея нужен **явный consumer**, который читает `XxxService.Value` каждый кадр (или подписан на событие изменения) и применяет коэффициент к нужной формуле. Типовая ошибка: подключить slider в Settings, проверить что значение сохраняется, и забыть подключить потребителя — настройка «работает» на бумаге, но игрок не чувствует разницы.
>
> Для непрерывных параметров (sensitivity, brightness, FOV) consumer обычно делает: `final = baseValue * XxxService.Value * scale`. Множитель `scale` подбирается так, чтобы **дефолтное значение** slider'а давало текущее (до фичи) поведение — например, slider в [0..1] с дефолтом 0.5 → `scale = 2f` (0.5 × 2 = 1.0 → нет изменений относительно прежней игры).
>
> **Минимум slider'а > 0.** Для параметров, при которых ноль ломает UX (sensitivity камеры, скорость UI-навигации, громкость диалогов), не давай игроку выставить ровно `0`. Поставь `minValue` в инспекторе `0.1`–`0.2`. Иначе пользователь случайно «обнулит» параметр и не поймёт, почему функция перестала работать.

#### Заготовка: вибрация (`VibrationService`) + UI-хаптика (`UIHaptics`)

Готовые скрипты-заготовки, **переносятся из проекта в проект и адаптируются** (события/интенсивности — под игру). Не изобретать с нуля; сами `.cs` живут в проекте (копируются), не в `porting/` — здесь зафиксировано, что заготовка есть и как с ней работать.

**`VibrationService`** — self-bootstrap singleton (как `InputDeviceManager`: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` создаёт GameObject + `DontDestroyOnLoad`, без префаба на сцене).
- Рамбл **только** через SDK `UPSInput.SetGamepadVibration(low, high)` / `StopGamepadVibration()` — консоле-безопасно (PS5 и т.п. внутри SDK). **Не** сырой `Gamepad.current.SetMotorSpeeds` (Desktop-only, платформы не покрывает). API сверять с `UPSCALE_SDK.md` → Input.
- Enable-флаг из сейв-стора (`PlayerPrefs`/`UPSSaves.Prefs`, ключ настройки вибрации) в `Awake` — применяется с запуска, не только после открытия настроек.
- API: `Vibrate(low, high, duration)` + пресеты `VibrateLight/Medium/Heavy`, `StopVibration()`. Авто-стоп корутиной на `WaitForSecondsRealtime` (работает при `Time.timeScale = 0`). `StopVibration` на `SceneManager.sceneLoaded` и `OnApplicationQuit` (не тащить рамбл через перезагрузку/смерть).
- Гейт внутри: `if (!_enabled || !<геймпад подключён>) return;` → мышь/без геймпада не трясёт, тогл уважается.
- **Адаптация под игру:** (1) тогл настроек → `SetEnabled(bool)`; (2) звать `Vibrate*`/пресеты в игровых событиях; (3) подкрутить интенсивности пресетов. Для «только маньяк/конкретный триггер» среди общих смертельных триггеров — `[SerializeField] bool`-флаг на компоненте-триггере, включённый лишь на нужном инстансе/префабе.
- 📍 Пример (Captive): `Assets/Scripts/VibrationService.cs`; ключ `Settings.Vibration`; триггеры — поимка маньяком (`KillTrigger.vibrateOnCatch` → `VibrateHeavy`), звук рации (`NPCSpawner` → `VibrateMedium`).

**`UIHaptics`** — лёгкая вибрация на UI. `[RequireComponent(UIElement)]`; подписка на `UIElement.OnInteracted` (нажатие кнопки / шаг `ActionSliderElement` / флип тогла) и `OnFocusChanged` (получение фокуса) → `VibrationService.VibrateLight()`. Вешается на каждый навигируемый `UIElement` (меню/настройки/пауза). Гамепад-only автоматически: `OnInteracted` приходит только с геймпада (через `ActionButtonElement/Slider/Toggle.OnPerformed`), плюс гейт сервиса. 📍 Пример (Captive): `Assets/Scripts/UIHaptics.cs`.

**`LightBarService`** — подсветка световой панели DualSense/DualShock. Self-bootstrap singleton (тот же паттерн, что `VibrationService`/`InputDeviceManager`: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` + `DontDestroyOnLoad`, без префаба).
- **⚠️ Ключевое отличие от вибрации: в SDK API подсветки НЕТ (проверено и на v2.1)** (рамбл есть — `UPSInput.SetGamepadVibration`; light bar — нет). Цвет ставится **напрямую через Unity Input System**: `DualShockGamepad.SetLightBarColor(Color)` / `DualSenseGamepadHID` под `#if UNITY_STANDALONE_WIN || UNITY_EDITOR`, плюс алиасы под `#if UNITY_PS4`/`UNITY_PS5`. Классы есть в `com.unity.inputsystem` → компилируется и работает в Editor/Win; PS-ветки за define-ами (платформенные пакеты ещё не импортированы — сборку не ломают). Каст к типу сам no-op-ит на Xbox/клавиатуре.
- State-machine в `Update()` на `Time.unscaledTime` (эффекты с приоритетом → целевой цвет); `SetLightBarColor` зовётся **только при смене цвета** (заодно снижает коллизию с рамблом на PC, см. gotcha). По умолчанию белый; OFF → чёрный.
- Enable-флаг из сейв-стора (`UPSSaves.Prefs.GetBool`, ключ настройки) в `Awake` через `SavesReady.Run` (как `VibrationService`). Тогл настроек → `SetEnabled(bool)`.
- **Перенос из другого проекта:** если берёшь готовый `LightBarService`/`DualSenseLightService` — тогл читать через `UPSSaves.Prefs.GetBool(...)`. Сама Update-машина и `ApplyColor` (платформенные ветки `SetLightBarColor`) переносятся почти как есть.
- **Подводный камень (PC HID):** на DualSense по HID `SetLightBarColor` и `SetMotorSpeeds` шлются одним output-репортом (FIXME в Input System) → одновременная работа с `VibrationService` может давать мерцание/срыв рамбла **только на PC**; на нативных PS4/PS5 API разделены. Митигировано отправкой цвета лишь при смене.
- **Эдиторная оговорка:** срабатывает только когда `Gamepad.current` — DualSense/DualShock (нужно хоть раз тронуть геймпад). Физически надёжно по USB; по Bluetooth на macOS запись output-репорта нестабильна — логику в редакторе проверять логами, цвет — на железе/USB.
- 📍 Пример (Captive): `Assets/Scripts/LightBarService.cs`; ключ `Settings.LightBar`; эффекты `CaughtFlash` (красное мигание, переживает перезагрузку сцены), `RadioAlert` (сплошной красный), `ToolSuccess` (зелёное мигание), `PickupFlash` (жёлтая огибающая); триггеры — рация (`NPCSpawner`), поимка (`KillTrigger`), использование инструментов (отвёртка/монтировка/вилка/ключ/молоток/кувалда/код сейфа), подбор предмета (`Pickable.PickUpObject`). Тогл `lightBarToggle` в `Game Settings.prefab` → `GameSettings.LightBarToggle`, строка скрыта вне Editor/PS (`ConfigurePlatformVisibility`).

#### Подводный камень: внутриигровой регулятор громкости перезаписывает master из Settings

Master Music/Sfx Volume из Settings хранится в `UPSSaves.Prefs` и читается при старте звукового менеджера. Если в игровом мире есть **второй источник управления той же громкостью** (физическая ручка радио в машине, регулятор на пульте, ползунок на NPC и т.п.), и он применяет своё значение через тот же метод, что и slider Settings — он будет молча затирать пользовательскую настройку в Saves.

**Типовая ошибка:**

```csharp
// SoundManager — один метод и для Settings, и для in-game регулятора
public void SetMusicVolume(float v)
{
    _musicVolume = v;
    UPSSaves.Prefs.SetFloat("MusicVolume", v);   // ← пишет в Saves всегда
    musicAudioSource.volume = entry.volume * _musicVolume;
}

// In-game регулятор при старте сцены: «радио выключено по умолчанию»
private void Start()
{
    SoundManager.Instance.SetMusicVolume(0f);   // ← затирает master из Settings нулём
}
```

Симптом: slider в Settings работает «в моменте» (двигаешь — громкость меняется), но не сохраняется между запусками. После рестарта slider всегда на той величине, что выставил последним внутриигровой регулятор (обычно 0).

**Правило:** master volume из Settings и runtime-источники громкости — две **независимые** переменные. Метод записи в Saves вызывается **только** из Settings.

```csharp
private float _masterMusicVolume = 1f;   // из Settings, сохраняется
private float _runtimeMusicVolume = 1f;  // от in-game регулятора (или нескольких), не сохраняется

public void SetMasterMusicVolume(float v)   // вызывается из Settings slider
{
    _masterMusicVolume = Mathf.Clamp01(v);
    UPSSaves.Prefs.SetFloat("MusicVolume", _masterMusicVolume);
    ApplyVolume();
}

public void SetRuntimeMusicVolume(float v)  // вызывается из in-game регулятора
{
    _runtimeMusicVolume = Mathf.Clamp01(v);
    ApplyVolume();   // НЕ пишет в Saves
}

private void ApplyVolume()
{
    musicAudioSource.volume = entry.volume * _masterMusicVolume * _runtimeMusicVolume;
}
```

Семантика: master из Settings — «потолок», runtime-регулятор работает в диапазоне 0..master. Если runtime-источников несколько (радио + регулятор у пассажира), формула расширяется произведением — каждый коэффициент в [0, 1].

Если в проекте уже есть один общий метод, который используют оба пути (Settings и in-game), и нужно сохранить совместимость — добавь второй метод (`SetRuntime*`), который **не** пишет в Saves, и переведи на него все вызовы из in-game источников. Метод из Settings остаётся как есть.

**Диагностика «значение не сохраняется» через MCP:**

Чтобы быстро понять, проблема в записи или в перезаписи — достать содержимое сохранения напрямую:

```csharp
// execute_code в Unity MCP
string raw = UnityEngine.PlayerPrefs.GetString("SaveData", "EMPTY");
byte[] bytes = System.Convert.FromBase64String(raw);
using var stream = new System.IO.MemoryStream(bytes);
var dict = (System.Collections.Generic.Dictionary<string, object>)
    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Deserialize(stream);
return string.Join(" | ", dict.Select(kv => $"{kv.Key}={kv.Value}"));
```

Если ключ `MusicVolume` есть и в нём `0` — значит запись работает, но кто-то её затирает (ищи второй источник вызовов `Set*` для этого ключа). Если ключа нет вовсе — проблема в самой записи (`Save()` не вызывается, упёрся в rate limit, или платформенный модуль не активирован).

#### Подводный камень: сохранённые настройки применяются только при открытии панели настроек

Очень частый паттерн: панель настроек применяет сохранённые значения (громкость, чувствительность, вибрация) в своём `Start()`/`OnEnable()`. Но если панель инстанцируется **неактивной** (`SetActive(false)` каскадом, типично для модалок) — её `Start()` **не выполняется** до первого открытия меню. Значит на старте игры настройки молча **не применены**: источники работают на дефолтах префаба.

**Симптом:** после рестарта дня/игры громкость/чувствительность не соответствуют сохранённым (слышно музыку, хотя выкручена в 0), пока не откроешь и не закроешь настройки.

**Правило:** применяй сохранённые настройки из компонента, **активного на старте сцены** (или из bootstrap), независимо от панели настроек. Панель настроек только **редактирует** значения; **применение при буте** — отдельная ответственность.

> 📍 Пример (этот проект): `SettingCanvas` создаётся `SetActive(false)` → `SettingsUI.Start()` не идёт на буте. Применение громкостей из `DB` перенесено в `SFX.Start()` (объект `SFX` на сцене, активен с запуска).

#### Подводный камень: звук в обход слайдера — нужен AudioMixer, а не громкость-в-аргументе

Если «громкость SFX» реализована как аргумент каждого воспроизведения (`source.PlayOneShot(clip, sfxVolume)`) или как `source.volume` на отдельных источниках, то любой источник, играющий **в обход** центрального хаба, слайдеру не подчиняется: шаги, ambient, аларм, VFX-звуки (огонь), звук техники. Слайдер «работает», но «не на всё».

**Правило:** для громкости из Settings используй **AudioMixer с группами** (Music / SFX / …) и выведи **каждый** `AudioSource` в группу. Слайдер пишет в exposed-параметр группы, а не в источник.

- **Аудит:** найди все источники — grep компонентов `AudioSource` по префабам/сценам **и** рантайм-создаваемые (`gameObject.AddComponent<AudioSource>()`), плюс `PlayOnAwake`. Источники без `outputAudioMixerGroup` идут мимо микшера (сразу в AudioListener).
- **Маршрутизация:** `audioSource.outputAudioMixerGroup = <группа>` (в префабах через `LoadPrefabContents`+`SaveAsPrefabAsset`).
- **Слайдер → микшер:** `mixer.SetFloat("SFXVolume", LinearToDb(value))`, где `LinearToDb(v) = v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f` (слайдер линейный 0..1, громкость в dB).
- **Применение при старте:** `mixer.SetFloat(...)` из сейва в `Start()` активного объекта (см. подводный камень выше). `AudioMixer.SetFloat` работает только в Play mode — в edit-time `GetFloat` вернёт значение снапшота, это нормально.
- Не множь дважды: если источник в группе SFX, не передавай ещё и `sfxVolume` в `PlayOneShot` — будет двойное затухание.

**Программное создание AudioMixer-ассета** (internal editor API, через `execute_code`):
```csharp
var t = typeof(UnityEditor.AssetDatabase).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
var ctrl = t.GetMethod("CreateMixerControllerAtPath").Invoke(null, new object[]{ "Assets/Audio/GameAudioMixer.mixer" });
// CreateNewGroup(name,false) → AddChildToParent(group, masterGroup) → AddExposedParameter(new AudioGroupParameterPath(group, group.GetGUIDForVolume()))
// переименовать exposed в ctrl.exposedParameters (поля name/guid) на "MusicVolume"/"SFXVolume"
```
`AddGroupToCurrentView` может кинуть `IndexOutOfRange` — это косметика редакторного окна, на рантайм/маршрутизацию не влияет. Фоллбэк, если internal API подведёт — создать миксер вручную в редакторе (Create → Audio Mixer; группы; правый клик по Volume → Expose; переименовать).

> 📍 Пример (этот проект): `Assets/Audio/GameAudioMixer.mixer` (Master→Music,SFX). В Music — `Bg_Source`(ambience), `ShopMusic`; в SFX — `Clips_Source`, шаги Player, аларм TopBar, огнетушитель, грузовик, fire-VFX. `SettingsUI`/`SFX` ссылаются на миксер; `SFX.LinearToDb` — общий хелпер.

#### Подводный камень: прямое чтение ввода слои НЕ гейтят (модалки/геймплей текут)

В v2 слои (`UILayersManager`/`Layer.IsEnabled`) гейтят **только** SDK-UI-компоненты (`ActionButtonElement`/`ActionSliderElement`/`LayerNavigator` — они сами проверяют `Layer.IsEnabled`). Любой код, читающий ввод **напрямую** (`BaseGamepadActionSet.Instance.X.Read()`/`.Performed`, кастомный `InputActionsSet`, raycast-Interact на Apply) — **вне слоёв**: сработает и в геймплее, и под открытой модалкой.

Симптом: открыта модальная панель, а DPad/кнопка в фоне всё равно листает магазин/двигает игрока; или Apply одновременно жмёт кнопку окна и делает мировое взаимодействие.

**Правила:**
- **Геймплейный ввод** заглушать на время UI: либо `gameplaySet.Deactivate()` (один вызов гасит весь набор — рекоменд., см. `UPSCALE_SDK.md` → «Слои UI»), либо общий флаг (`📍 GameController.GameplayInputBlocked`), который геймплейные читатели проверяют первой строкой.
- **Свой UI-обработчик** (если пишешь прямое чтение вместо `LayerNavigator`) — ранний guard: `if (!_panelRoot.activeInHierarchy) return;` или `if (!myLayer.IsEnabled) return;`. Если скрипт на корне слоя, включающегося/выключающегося — хватит `OnEnable`/`OnDisable`.
- **Панель через CanvasGroup alpha** (GameObject активен всегда) — её слой на старте `SetEnabled(false)`, иначе её `ActionButtonElement` ловят Apply прямо в геймплее.
- **Каждый экран — свой `Layer` (`Key`)**, активируемый через `UILayersManager`; не переиспользовать чужой `Layer` для своих элементов.

#### Подводный камень: World Space Canvas и z-порядок

(версионно-независимо) В **World Space Canvas** элементы рисуются в порядке иерархии (painter's algorithm). Если UI-глиф геймпад-кнопки (`Image`/иконка `ActionButtonElement`) находится в объекте, который стоит **раньше** в иерархии чем другой крупный элемент (карта, изображение на весь экран), он будет перекрыт.

**Решение:** добавить компонент `Canvas` на GameObject с глифом:
- `Override Sorting = true`
- `Sorting Order = 1` (или выше чем у перекрывающего элемента)

Вложенный Canvas выходит из общего порядка рендера и рисуется поверх. `GraphicRaycaster` не нужен — мышиный клик идёт через родительский Canvas, геймпад — через Layer SDK.

#### Подводный камень: одно событие — несколько UI-состояний

Если одно событие может принимать одно значение для **нескольких разных UI-состояний**, простой инверсии `!isActive` недостаточно.

**Пример:** `CameraSwitchController.OnCameraChanged(bool isPlayerActive)` стреляет `false` и при переходе в Dashboard, и при переходе в AcSystem. Паттерн `SetLayerActive(!isPlayerActive)` в `DashboardInputLayer` активировал Layer 1 (дашборд) в обоих случаях — кнопка R1, назначенная на закрытие дашборда, перехватывала нажатие и в режиме AcSystem, не давая переключать радиостанции.

**Правило:** каждый `XxxInputLayer` должен явно проверять нужное состояние из источника, а не полагаться на одно булево значение события:

```csharp
// ПЛОХО: активируется для всех не-gameplay состояний
private void OnStateChanged(bool isGameplayActive)
{
    _layer.SetActive(!isGameplayActive);
}

// ХОРОШО: явная проверка нужного состояния
private bool IsThisLayerState()
{
    return !_system.IsGameplayActive && !_system.IsOtherUIActive;
}

private void OnStateChanged(bool _) => _layer.SetActive(IsThisLayerState());
private void Start()               => _layer.SetActive(IsThisLayerState());
```

Это же касается code-based input-обработчиков (без Layer): они тоже должны проверять конкретное состояние системы, а не просто `!isPlayerActive`.

#### Подводный камень: хардкод `transform.GetChild(N)` ломается при структурных правках префаба

Часто старый код берёт дочерние объекты по индексу: `someBtn.transform.GetChild(0).GetComponent<Text>().text = "..."`. Это работает пока структура префаба не меняется. При выносе фона/спрайта в отдельный дочерний GameObject с `SetSiblingIndex(0)` (типичная правка при добавлении gamepad-глифа) **первым ребёнком становится Sprite без Text-компонента** → `GetComponent<Text>()` возвращает `null` → NRE при `.text = ...`.

NRE в глобальном event-обработчике обрывает оставшихся подписчиков (см. ниже), и это маскирует первопричину — выглядит как баг совсем в другой системе.

**Правило:** перед любой правкой `SiblingIndex` или порядка детей префаба — grep весь код на `GetChild(`, `transform.GetChild`, `m_Children[`. В исходном коде заменить на устойчивые формы:

```csharp
// ПЛОХО — хрупкий хардкод:
someBtn.transform.GetChild(0).GetComponent<Text>().text = label;

// ХОРОШО — поиск по типу:
var t = someBtn.GetComponentInChildren<Text>(true);
if (t != null) t.text = label;

// ИЛИ поиск по имени:
var t = someBtn.transform.Find("Label")?.GetComponent<Text>();
```

#### Подводный камень: исключение в одном подписчике обрывает весь event chain

Стандартный `Action.Invoke()` в C# вызывает подписчиков синхронно в порядке регистрации. Если один из них кидает exception — **остальные подписчики после него не вызываются**. На глобальной шине событий (типа `OnItemInteract`, `OnStateChanged`) это даёт каскадный сбой: один сломанный обработчик глушит обновление UI/состояния, видимое как баг в системе которая просто не получила event.

**Правило:** обработчики broadcast-событий обязаны быть устойчивы к null-references на любых сериализованных полях префаба (которые могли быть удалены/перемещены/не назначены). Защита — `if (ref == null) return;` на входе и null-safe доступ через `?.` для всех Unity-объектов из инспектора.

При диагностике "событие не доходит" — проверять Console на любые exception во время воспроизведения, не только Error по своей фиче. Один тихий NRE в чужом подписчике маскирует кучу симптомов.

#### Подводный камень: synchronous event chain и race condition при изменении state в обработчике

Если handler меняет global state (например `canInteract = false`) в ответ на событие, и в **той же** `Action.Invoke()` есть другой подписчик, который читает это state — порядок подписки определяет поведение. Хуже того, если игровое действие (нажатие кнопки) триггерит две вещи параллельно:
1. Закрытие UI окна → сброс state в "разрешено"
2. Гейтированный обработчик действия → должен видеть "запрещено"

Подписчик №2 в той же frame может увидеть state ПОСЛЕ сброса от №1 и сработать "лишний раз".

**Правило:** изменения state, которое должно видеть **только следующее** событие (а не подписчиков текущего) — откладывать на 1 кадр:

```csharp
private Coroutine _restoreCoroutine;
private void OnCursorShow(bool show)
{
    if (_restoreCoroutine != null) { StopCoroutine(_restoreCoroutine); _restoreCoroutine = null; }
    if (show) flag = false;          // блокирующий state — мгновенно
    else _restoreCoroutine = StartCoroutine(RestoreNextFrame()); // разрешающий — через кадр
}
private IEnumerator RestoreNextFrame() { yield return null; flag = true; _restoreCoroutine = null; }
```

Это даёт другим подписчикам того же event'а время отработать с старым state, прежде чем разрешать действия снова.

#### Подводный камень: Animator.enabled без явного управления "застревает"

При использовании Animator для условной анимации (мигание, pulse, attention-blink в туториале) `Animator.enabled` нужно устанавливать как **функцию текущего состояния**, а не как изменение по событию:

```csharp
// ПЛОХО — оставляет enabled в произвольном состоянии вне условия:
if (state == SomeState) animator.enabled = shouldAnimate;

// ХОРОШО — каждый вызов явно решает текущее значение:
animator.enabled = (state == SomeState) && shouldAnimate;
```

Иначе Animator остаётся "забытым" во включённом состоянии после прохождения целевого state — и продолжает крутить анимацию (или замораживает свойство в произвольном кадре после `enabled=false`, что тоже визуальный артефакт).

Дополнительное правило: если анимация меняет `RectTransform.localScale` / `CanvasGroup.alpha` / другие визуальные свойства — выключение `Animator.enabled` посреди клипа **замораживает** их в текущем значении (не сбрасывает к исходному). Чтобы вернуть к default — `animator.Rebind()` после `enabled = false`.

> **Прямое чтение ввода слои НЕ гейтят** — см. одноимённый подводный камень выше: UI-действия делай через `ActionButtonElement`/`LayerNavigator` (гейтятся `Layer.IsEnabled`), а прямое чтение `BaseGamepadActionSet`/raycast-Interact заглушай флагом/`Deactivate()`.

#### Подводный камень: двойное срабатывание на toggle-интерактивах (raycast Interact + UI-кнопка в одном кадре)

(версионно-независимо) У интерактивного объекта в мире часто есть **два** пути активации на одно и то же нажатие E/Apply:
- (A) raycast по объекту в `Update` → `interactionObject.Interact()`
- (B) контекстная UI-кнопка (`ActionButtonElement` на той же Apply, или клавиатурный хинт на той же клавише) → `onClick` → обработчик на объекте

Оба пути читают ввод в **одном кадре** (гибрид `Input.GetKeyDown(E) || Apply.Press.Read()`); `ActionButtonElement.OnPerformed` приходит тем же кадром. Если оба выполняют **одно действие**, оно срабатывает дважды. (Та же гонка — между вложенными модалками: Apply открыл попап и тут же закрыл; Cancel закрыл попап и заодно окно.)

**Симптом:** для **toggle**-семантики (открыть/закрыть, вкл/выкл) объект «не реагирует» или меняется через раз — чётное число тоглов даёт нулевой эффект. Недетерминированность добавляет 1-кадровый лаг между raycast-детектом и активацией UI-кнопки (на пограничном кадре активен только один путь → одиночный тогл → состояние всё-таки меняется). Для **идемпотентных** действий (вход за стойку, открыть панель) двойной вызов визуально незаметен, но всё равно нежелателен (рестарт корутин, двойной фаер событий).

**ПЛОХО** — два независимых метода с собственным toggle:
```csharp
public override void Interact()      { isOpen = !isOpen; Apply(); } // raycast
private void OnButtonClickEvent()    { isOpen = !isOpen; Apply(); } // UI-кнопка
```

**ХОРОШО** — единый метод с guard по кадру (оба входа остаются рабочими — нельзя терять ввод на пограничных кадрах):
```csharp
private int lastToggleFrame = -1;
public override void Interact()   => Toggle();
private void OnButtonClickEvent() => Toggle();
private void Toggle()
{
    if (Time.frameCount == lastToggleFrame) return; // второй вызов в том же кадре — игнор
    lastToggleFrame = Time.frameCount;
    isOpen = !isOpen; Apply();
}
```

Намеренный быстрый double-press (два разных кадра) при этом по-прежнему даёт два тогла — корректно. Пример в проекте: `OpClShopItem` (вывеска Open/Close). Тот же дубль-паттерн есть у `CheckoutItem` (`Interact()` + `EnterCounter()`), но там действие идемпотентно.

#### Подводный камень: два `ActionButtonElement` на одном действии в одном слое

Индексов слоёв и приоритетов **нет**. Конфликт **между** экранами решается тем, что активен лишь один `Layer` (`UILayersManager`). Конфликт **внутри** слоя — если два `ActionButtonElement` слушают одно действие (например оба на Apply) и оба проходят гарды — оба дёрнут свой `onClick`.

**Как разводить в v2:**
- Только **один** элемент в слое должен быть «доступен» для действия в данный момент. Используй `_checkIsFocused=true` + фокус-навигацию (`LayerNavigator`): срабатывает только сфокусированный. «Глобальной» кнопке слоя (Close по Cancel) ставь `_checkIsFocused=false`, но тогда она единственная на это действие в слое.
- Если две кнопки реально конкурируют за одно действие у одной цели — это ошибка дизайна: разведи их видимостью (см. «взаимоисключение видимости») или разными действиями.

#### Подводный камень: несколько контекстных действий на одной физической кнопке — взаимоисключение видимости

Отдельно от двойного фаера *одного* действия (см. выше): когда **разные** экранные кнопки-действия мапятся на **одну** физическую кнопку и могут быть релевантны у одной цели, нельзя оставлять их видимыми одновременно — игрок видит несколько глифов одной кнопки, а сработает произвольное.

**Правило:**
- Гарантируй, что у любой цели/контекста активна (`SetActive(true)`) **только одна** кнопка на данную физическую кнопку.
- Централизуй show/hide в **одном** месте (single source of truth) и пересчитывай **от факта состояния**, а не от порядка событий: подписчики broadcast-события (`OnItemInteract` и т.п.) срабатывают в **неопределённом** порядке, поэтому «спрятать A, когда показал B» по событию — ненадёжно. Считай видимость от `activeSelf`/состояния.

```csharp
// order-independent: видимость A пересчитывается от факта, активны ли конфликтующие B/C
void RefreshA() {
    bool conflict = bBtn.activeSelf || cBtn.activeSelf;
    aBtn.SetActive(ShouldShowA() && !conflict);
}
// вызывать RefreshA() в конце КАЖДОГО метода, меняющего видимость B или C
```

> 📍 Пример (этот проект): `Open/Close`, `Place`, `Set Price` — все на `Apply`. `RefreshOpenCloseBtn()` пересчитывает видимость Open/Close от `placeButton.activeSelf || plSetPriceButton.activeSelf` и зовётся из `EnablePlaceBtn`/`SetPriceLabelBtn` — порядок подписчиков не важен.

#### Подводный камень: legacy EventSystem «Submit»/«Cancel» на геймпаде повторно жмёт выбранную мышью кнопку

При `Active Input Handling = Both` сцена с `StandaloneInputModule` параллельно с SDK слушает legacy-оси `Submit`/`Cancel` (Project Settings → Input Manager), которые по умолчанию привязаны к `joystick button 0`/`1`. Любой клик мышью по `UnityEngine.UI.Button` делает его `EventSystem.currentSelectedGameObject`. Когда игрок жмёт геймпад-кнопку (через свою SDK-логику), `StandaloneInputModule` **в тот же момент** ловит legacy «Submit» и шлёт `ExecuteEvents.submitHandler` в выбранную кнопку → её `onClick` срабатывает **повторно/паразитно**.

**Симптом (реальный):** нажатие `Buy` (SDK, `ButtonWest`) покупает корзину И заодно повторно дёргает `Add to Cart` последней кликнутой мышью карточки; при пустой корзине повторный `Buy` снова что-то добавляет. На **macOS** физический индекс кнопки в legacy-Input отличается от Windows, поэтому даже `ButtonWest` может совпасть с `joystick button 0`.

**Диагностика:** `ActionButtonElement` зовёт `_button.onClick.Invoke()` напрямую (без EventSystem) — значит лишний вызов идёт НЕ от SDK, а от `StandaloneInputModule`. Цель лишнего вызова = `currentSelectedGameObject` (последняя кликнутая кнопка).

**Фикс:** если весь геймпад-UI идёт через UpscaleSDK (а не через EventSystem-навигацию), убери геймпад-привязки у legacy-осей в `ProjectSettings/InputManager.asset`: у `Submit` и `Cancel` очисти `altPositiveButton: joystick button 0`/`1` (клавиатурные `return`/`enter`/`space`/`escape` оставь). Это глобально гасит вредный legacy gamepad-submit, ничего из SDK не ломая. Альтернатива (точечно) — `EventSystem.current.SetSelectedGameObject(null)` после клика, но это надо вешать на каждую кнопку.

#### Hold-to-repeat для слайдеров/навигации — в v2 встроено

В v2 ручной поллинг для авто-повтора обычно **не нужен**: `ActionSliderElement` (`UiSlider`) и `LayerNavigator` (`UiLayerNavigation`) читают `Vector2ActionDefinition` с `DelayVector2Processor` — удержание DPad/стика само повторяется (initial delay → repeat). Поэтому слайдер, на котором висит `ActionSliderElement`, листается удержанием «из коробки».

Своё hold-чтение пиши только для нестандартного контента (не слайдер/не фокус-навигация). Тогда читай `BaseGamepadActionSet.Instance.<Action>.Hold.Read()` (или float-ось) в `Update` с таймером (`initialDelay`→`repeatInterval`), под guard'ом видимости панели. Параметры тайминга — в инспектор.

> Мышь по `UnityEngine.UI.Button.onClick` всё так же даёт один тик на клик — для hold по слайдеру мышью нужен свой `IPointerDown/Up` поллинг (если требуется); геймпад этим не затронут.

#### Подводный камень: `AssetDatabase.LoadAssetAtPath` молча возвращает `null` при миграции ассета

Префабы хранят ссылки на ассеты через GUID (в `.meta`), и при перемещении ассета по структуре `Assets/` существующие GUID-references остаются валидны. Но `AssetDatabase.LoadAssetAtPath<T>("Assets/old/path.asset")` в editor-скриптах-настройщиках **не следует за переездом** — возвращает `null` если путь больше не существует.

Без `if (asset == null) return;` скрипт молча присваивает `null` в сериализованное поле компонента — баг проявится визуально (нет иконки/спрайта/звука), но Console чист.

**Правила для editor-скриптов:**

```csharp
var asset = AssetDatabase.LoadAssetAtPath<MyType>(path);
if (asset == null)
{
    return "ERR: " + path + " not loaded (moved? deleted?)";
}
// ... use asset ...

// В финальном debug-выводе всегда печатать .name присвоенного ассета:
return "OK: ... assigned=" + (so.FindProperty("_definition").objectReferenceValue?.name ?? "NULL!");
```

Fallback на случай переезда — `AssetDatabase.FindAssets("Имя t:Type")` находит по имени независимо от пути.

### 3.4 Проверка platform-зависимого UI

Не использовать `Application.isMobilePlatform` как «единственный признак». На Xbox/PS/Switch `isMobilePlatform = false`, но там нет клавиатурных подсказок и UI должен выглядеть как на гейм-консоли.

Generic-паттерн для определения консольной платформы:

```csharp
private static bool IsConsolePlatform()
    {
        RuntimePlatform currentPlatform = Application.platform;
        return currentPlatform == RuntimePlatform.GameCoreXboxSeries
            || currentPlatform == RuntimePlatform.XboxOne
            || currentPlatform == RuntimePlatform.PS4
            || currentPlatform == RuntimePlatform.PS5
            || currentPlatform == RuntimePlatform.Switch;
    }

// Логика видимости hints для подсказок в UI:
bool showKeyboard = !isConsole && !hasGamepad;
bool showGamepad  = isConsole || hasGamepad;
// Консоль: gamepad hints всегда, keyboard скрыты
// PC без геймпада: keyboard hints
// PC с геймпадом:  gamepad hints
```

Если игра не идёт на мобильные — `isMobilePlatform` для UI-логики можно удалить.

**Детект «есть ли геймпад»** (для авто-переключения kb↔gamepad-подсказок): с v2.1 — `UPSInput.GetPrimaryDevice()` (на консоли всегда `Gamepad`; на ПК `Gamepad`, если подключён, иначе `Keyboard`/`Mouse`) + события `UPSInput.OnDeviceAdded/OnDeviceRemoved`. Низкоуровнево — `GetConnectedDevices()` (массив `DeviceType`). Уже-подключённое устройство события не дают — опрашивать (например в `Update` или при открытии экрана). 📍 В этом проекте обёртка `InputDeviceManager` (`GamepadActive` + `OnInputDeviceChanged`) инкапсулирует это (написана до v2.1 — внутри можно заменить на `GetPrimaryDevice()`). Платформенные строки (Light Bar) гейтить через `#if UNITY_PS4 || UNITY_PS5 || UNITY_EDITOR`.

### 3.5 Финал

- [ ] Все механики из 3.1 покрыты геймпадом
- [ ] Проверить `SimpleInput` / другие input-абстракции — совместимость с New Input System
- [ ] Протестировать каждый слой: движение, камера, действия, переходы между UI, hot-plug геймпада
- [ ] Зафиксировать выполненную работу в `PROGRESS.md` (раздел Stage 3)

> ⚠️ В v2 семантику Apply/Cancel/Base/Extra несут **action-определения** (`GamepadAction` в `BoolActionDefinition`), а не raw-событие. UI-действия — через `ActionButtonElement`/`ActionSliderElement` (см. выше), raw-чтение (DPad/стик/триггеры) — через `BaseGamepadActionSet.Instance`. Канон — `UPSCALE_SDK.md` → Input/UI.

## Stage 4 — UI Navigation (v2)

- [ ] Навигация UI — через `LayerNavigator` + `LayerNavigation` (граф `Transition`, default-элемент), **не** Unity `EventSystem`/`Selectable.Navigation`/`First Selected`. Фокус сбрасывать на дефолт при открытии (`navigator.SelectElement(default)`).
- [ ] Элементы экрана — `ActionButtonElement`/`ActionSliderElement`/`ActionToggleElement`, все на `_layer` этого экрана.
- [ ] При открытии меню: `UILayersManager.ActivateLayer(thisLayer)` **+** заглушить геймплей (`gameplaySet.Deactivate()` / флаг). При закрытии — вернуть `_prevLayer` + снять заглушку (при стеке модалок — save/restore).
- [ ] **Логика навигации в каждом UI** — выяснить раскладку (одно меню → DPad, вкладки → бамперы). Обсудить маппинг с разработчиком; кнопки самому не назначать.
- [ ] **Что блокировать при открытии UI** — не только движение/камера, но и все геймплейные читатели ввода (через флаг/`Deactivate`), и нижние UI-слои (автоматически — single-active).
- [ ] Закрытие — по `Cancel` (`ActionButtonElement` с `UiButtonCancel`, `_checkIsFocused=false`) или штатной кнопкой. Вложенные меню: открыть → закрыть → фокус возвращается через `_prevLayer`.

## Stage 5 — Button Prompts (v2)

- [ ] Глифы кнопок — через `GamepadIconsSet` (по набору на платформу PS/Xbox/Switch), привязанные в `UpscaleSDKConfig`. Спрайт берётся `UPSInput.TryGetGlyph(GamepadGlyph)` или `action.TryGetGlyph()`, либо автоматически иконкой `ActionButtonElement` (`_showIcon`). Платформа-адаптация — внутри provider'а по `UPSInput.GetGamepadType()`.
- [ ] **Правило эдиторного превью:** глифу-`Image`, чей спрайт ставится в рантайме, проставляй статичный превью-`sprite` в префабе (из `GamepadIconsSet.GetGlyph(...)`) — иначе в эдиторе белый квадрат. `ActionButtonElement` (`_showIcon`) даёт превью сам через `OnValidate`.
- [ ] keyboard↔gamepad подсказки переключать по детекту устройства (`UPSInput.GetConnectedDevices()`/`OnDeviceAdded/Removed`; 📍 обёртка `InputDeviceManager.GamepadActive`/`OnInputDeviceChanged`).
- [ ] Заполнить наборы глифов: PS (Cross/Circle/Square/Triangle…), Xbox (A/B/X/Y…), Switch (A/B/X/Y…) — каждый `GamepadGlyph` → свой спрайт.

