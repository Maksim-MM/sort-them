# Трофеи / достижения — Sort Them Cars (PS4, PS5, Xbox)

Источник правды по ID — `Assets/_Game/Scripts/Runtime/Core/Trophies.cs`, enum `Trophy` (ID = значение). Trophy ID (PSN) и ID (Xbox) совпадают с кодом один в один, менять нельзя. Платина (0) кодом не выдаётся, её даёт платформа. Список актуализирован 05.11 по DevSheet «Achievements» (14 трофеев + платина). Активность PS5 `story_01` стартует при запуске и завершается, когда закрыты все 305 полок (`GameComplete`, `Core/Activities.cs`), от трофеев не зависит.

| Object ID | Title (en-US) | Description (en-US) | Unlock Value | Hidden | PSN Trophy ID | PSN Grade | PSN Points | Xbox ID | Xbox Points |
|---|---|---|---|---|---|---|---|---|---|
| platinum_trophy | Platinum Trophy | You have unlocked all trophies! | 1 | FALSE | 0 | Platinum | 0 | --- | --- |
| first_sort | First Sort | Sort your very first car. | 1 | FALSE | 1 | Bronze | 15 | 1 | 10 |
| shelf_life | Shelf Life | Complete your first shelf. | 1 | FALSE | 2 | Bronze | 15 | 2 | 15 |
| category_closed | Category Closed | Complete all shelves in a single category. | 1 | FALSE | 3 | Silver | 30 | 3 | 15 |
| retail_therapy | Retail Therapy | Buy your first upgrade at the terminal. | 1 | FALSE | 4 | Silver | 30 | 4 | 20 |
| high_roller | High Roller | Spin the slot machine for the first time. | 1 | FALSE | 5 | Gold | 90 | 5 | 30 |
| fifty_shelves | Fifty Shelves | Complete 50 shelves. | 1 | FALSE | 6 | Gold | 90 | 6 | 40 |
| hundred_cars | Hundred Cars | Sort 100 cars in total. | 1 | FALSE | 7 | Gold | 90 | 7 | 50 |
| halfway_there | Halfway There | Complete 150 shelves. | 1 | FALSE | 8 | Gold | 90 | 8 | 60 |
| thousand_cars | Thousand Cars | Sort 1,000 cars in total. | 1 | FALSE | 9 | Gold | 90 | 9 | 70 |
| blueprint_collector | Blueprint Collector | Unlock all 15 blueprints on the wall. | 1 | FALSE | 10 | Gold | 90 | 10 | 80 |
| maxed_out | Maxed Out | Max out all 9 upgrades at the terminal. | 1 | FALSE | 11 | Gold | 90 | 11 | 90 |
| master_mechanic | Master Mechanic | Fully assemble a special car with all 10 parts. | 1 | FALSE | 12 | Gold | 90 | 12 | 100 |
| collector | Collector | Fully assemble all 5 special cars. | 1 | FALSE | 13 | Gold | 90 | 13 | 150 |
| warehouse_in_order | Warehouse in Order | Complete all 305 shelves in the store. | 1 | FALSE | 14 | Gold | 90 | 14 | 270 |

Примечания:
- Xbox: очки в сумме ровно 1000 (Gamerscore).
- Грейды и очки PSN — как в DevSheet; код на них не завязан.
- Hidden везде FALSE, скрытых условий нет.
- Убраны из старого списка: Five of a Kind, Kaboom, Treasure Hunter (код удалён).

Условия в коде (где срабатывает):

| ID | Trophy | Условие | Триггер |
|---|---|---|---|
| 1 | FirstSort | `PlacedValid` ≥ 1 | `GameManager.RecountStats` → `Trophies.Cars` |
| 2 | ShelfLife | `ClosedShelves` ≥ 1 | `GameManager.OnShelfClosed` → `Trophies.Shelves` |
| 3 | CategoryClosed | открыт ≥ 1 чертёж на стене | `BlueprintWall.Refresh` → `Trophies.Blueprints` |
| 4 | RetailTherapy | первая покупка в терминале | `UpgradeService.TryBuy` |
| 5 | HighRoller | первый спин | `UiRoot.SpinWith` |
| 6 | FiftyShelves | `ClosedShelves` ≥ 50 | `Trophies.Shelves` |
| 7 | HundredCars | `PlacedValid` ≥ 100 | `Trophies.Cars` |
| 8 | HalfwayThere | `ClosedShelves` ≥ 150 | `Trophies.Shelves` |
| 9 | ThousandCars | `PlacedValid` ≥ 1000 | `Trophies.Cars` |
| 10 | BlueprintCollector | все чертежи стены (`BlueprintWall.Categories.Length` = 15) | `BlueprintWall.Refresh` |
| 11 | MaxedOut | `UpgradeService.TerminalMaxed` (9 апгрейдов `Source == Terminal`) | `TryBuy` |
| 12 | MasterMechanic | у любой спецмашины `SpecialLevel` == `MaxLevel` (10) | `GameManager.UpgradeSpecial` → `Trophies.Specials` |
| 13 | Collector | у всех 5 спецмашин `SpecialLevel` == `MaxLevel` | там же |
| 14 | WarehouseInOrder | `ClosedShelves` ≥ `TotalShelves` (305) | `Trophies.Shelves` |

Детали:
- `PlacedValid` — счётчик «машинок на верных полках» из HUD; он пересчитывается по полкам, то есть снятая с полки машинка его уменьшает. Накопительного счётчика в сейве нет, трофей выдаётся при первом достижении порога и назад не отзывается.
- Чертежи считаются по категориям стены (15), а не по каталогу (16: в каталоге есть `cat_special`, у которого рамки на стене нет). Со старым расчётом по каталогу Blueprint Collector был недостижим.
- При загрузке сейва `Trophies.Evaluate` досдаёт полки, машинки, апгрейды и спецмашины; чертежи досдаёт `BlueprintWall.Refresh` после `StatsChanged` в конце `GameManager.Start` (платформа идемпотентна).
