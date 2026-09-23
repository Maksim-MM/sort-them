> Часть porting-пайплайна. Индекс, маршрутизация и порядок чтения — `PORTING_PIPELINE.md` в этой папке.

## Stage 0 — Project Analysis & Architecture

Первый шаг в любом проекте — составить `ARCHITECTURE.md` в корне репо. Без него каждый раз придётся заново искать сцены, скрипты и структуру UI при очистке контекста или переходе к новому этапу.

### Что документировать

- [ ] **Сцены** — список из Build Settings, назначение каждой
- [ ] **Иерархия каждой сцены** — ключевые объекты + компоненты (менеджеры, DontDestroyOnLoad, игрок, транспорт, UI canvas). Статичное окружение — одной строкой
- [ ] **Динамически создаваемые префабы** — что инстанцируется в рантайме из `Resources.Load`/Addressables и кем (см. ниже)
- [ ] **Игровой персонаж / транспорт** — иерархия объекта, скрипты с короткой ролью каждого
- [ ] **Структура UI** — Canvas → панели → ключевые кнопки и контроллеры
- [ ] **Ключевые механики** — жизненный цикл систем (поездка, AI, магазин) в виде цепочки вызовов
- [ ] **Шина событий / DI** — как системы общаются
- [ ] **Сохранение данных** — что и где хранится

### Динамически создаваемые префабы

Большая часть игрового UI и AI часто **не лежит на сценах**, а инстанцируется в рантайме через `Resources.Load(path)` + `Instantiate()` (или Addressables). На сцене стоит «пустой шелл» с менеджером, который собирает мир после `Start()`. Если этого не понять, поиск через `find_gameobjects` в активной сцене ничего не даст, а правки сцены потеряются при следующей загрузке.

**Найти все точки динамической загрузки:**

```bash
grep -rEn "Resources\.Load|Resources\.LoadAsync|Addressables\.LoadAssetAsync|AssetBundle\.LoadAsset" \
  Assets/Scripts/ 2>/dev/null

# SerializeField-поля с путями ресурсов (часто `[SerializeField] string xxxResPath`)
grep -rEn "ResPath|resPath|prefabPath|canvasPath" Assets/Scripts/ 2>/dev/null
```

**Для каждого найденного пути зафиксировать в `ARCHITECTURE.md`:**

| Колонка | Что писать |
|---|---|
| **Путь** | Resources-путь (`Gameplay/Player/Player`), или Addressables-ключ |
| **Префаб** | Реальный файл (`Assets/Resources/Gameplay/Player/Player.prefab`) |
| **Корневой компонент** | Что висит на корне префаба (`Pl_Manager`, `WorkerCanvas`, `HudCanvas` и т.п.) |
| **Кто инстанцирует** | Файл:строка вызова (`GameplayManager.cs:117` в `SetPlayer()`) |
| **Куда** | Контейнер на сцене (`UI`-объект, `AiContainer`, `null` — в root, и т.п.) |
| **Когда** | При старте сцены / по событию / в цикле спавна / по interaction |

**Подвох: дефолт `[SerializeField] string` в коде ≠ реальное значение в инспекторе**

`[SerializeField] private string canvasResPath = "Gameplay/Player/UI/ControlsCanvas";` — дефолт **в коде**. Но в инспекторе на префабе/сцене это поле может быть перебито на другой путь (`Gameplay/Player/UI/CF2-FPP-Rig`). Чтобы не ошибиться:

- При поиске «какой префаб реально грузится» — **читай YAML префаба/сцены**, не код. Через `grep -n "canvasResPath:" path.prefab` или MCP `manage_components`.
- В `ARCHITECTURE.md` пиши **реальное** значение из YAML, не дефолт из кода.
- При переименовании папок в `Resources/` — найди и обнови **и код, и YAML** (Unity не выдаст compile-error на сломанный `Resources.Load`).

**Категории динамических префабов, которые обычно находятся:**

- [ ] **Bootstrap-объекты** — Player, Environment, главные менеджеры, HUD-система (`Resources.Load` в `GameManager.Start()` или эквиваленте)
- [ ] **UI-канвасы** — каждый модальный/контекстный canvas (Shop, Settings, Worker, Delivery, DayEnd, и т.п.) — часто грузится UI-менеджером
- [ ] **AI префабы** — Customer/NPC/Worker, обычно из `Resources/.../AI/...`, с числовым суффиксом (`Customer 1..N`) и случайным spawn'ом
- [ ] **World-pickup'ы** — `GiftBox`, `Booster`, `MysteryChest`, `RewardCrate` — `Instantiate` в мире по событию или таймеру
- [ ] **Per-item canvases** — `DeliveryCanvas`, `TooltipCanvas`, `PopupCanvas` — спавнится по одному на каждую сущность
- [ ] **VFX/FX-префабы** — взрывы, портал-эффекты, particles из `Resources/Effects/...`

**Каскадные/вложенные spawn'ы** — особый случай: бутстрап-префаб (`HudManager`) сам внутри `Start()` инстанцирует дочерние префабы. Чтобы понять полную цепочку, **читай `Awake`/`Start`/`Init` методы корневых компонентов** каждого динамического префаба.

**Карта спавн-цепочки (типовая):**

```
Scene[Bootstrap] → GameManager.Start()
  └─ Instantiate(Player.prefab)
       └─ Pl_Manager.SetData()
            └─ Instantiate(ControlsCanvas.prefab)  ← вторичный спавн
  └─ Instantiate(HudManager.prefab)
       └─ HudManager.Start()
            └─ Instantiate(HudCanvas.prefab)        ← вторичный
  └─ Instantiate(DeliveryManager.prefab)
       └─ On event: Instantiate(DeliveryCanvas)    ← многократный, per-delivery
  └─ Instantiate(CustomersManager.prefab)
       └─ CorSpawning(): Instantiate(Customer N)   ← в цикле, рандомно
```

Записывай такую схему в `ARCHITECTURE.md` — она будет нужна на Stage 3 (где какой UI Layer создавать), на Stage 6 (как мигрировать на Addressables, если идёт оптимизация памяти на консоли), и на Stage 7 (что войдёт в Resources-pack и сколько весит).

> ⚠️ **На консолях `Resources.Load` — антипаттерн.** Все ассеты в `Resources/` грузятся в память при старте и не выгружаются. Для крупных проектов мигрируй на Addressables (`com.unity.addressables`) в Stage 6/7. Для маленьких — оставь как есть, но **зафиксируй полный размер `Resources/`** через Build Report Tool перед отгрузкой.

### Как делать

Запустить `/unity-analyze` — скилл автоматически обходит сцены через Unity MCP, читает скрипты на ключевых объектах и формирует `ARCHITECTURE.md`. При необходимости дополнить вручную.

Если скилл недоступен: открыть сцены через Unity MCP (`manage_scene`, `find_gameobjects`), прочитать скрипты на ключевых объектах, записать кратко.

---

