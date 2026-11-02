# Трофеи / достижения — Sort Them Cars (PS4, PS5, Xbox)

Источник правды по ID — `Assets/_Game/Scripts/Runtime/Core/Trophies.cs`, enum `Trophy` (ID = значение). Trophy ID (PSN) и ID (Xbox) совпадают с кодом один в один, менять нельзя. Платина (0) кодом не выдаётся, её даёт платформа. Активность PS5 `story_01` стартует при запуске и завершается, когда получены все 13 (`Core/Activities.cs`).

| Object ID | Title (en-US) | Description (en-US) | Unlock Value | Hidden | PSN Trophy ID | PSN Grade | PSN Points | Xbox ID | Xbox Points |
|---|---|---|---|---|---|---|---|---|---|
| platinum_trophy | Platinum Trophy | You have unlocked all trophies! | 1 | FALSE | 0 | Platinum | 0 | --- | --- |
| five_of_a_kind | Five of a Kind | Hold five identical cars at once | 1 | FALSE | 1 | Silver | 30 | 1 | 60 |
| shelf_life | Shelf Life | Complete your first shelf | 1 | FALSE | 2 | Bronze | 15 | 2 | 20 |
| fifty_shelves | Fifty Shelves | Complete 50 shelves | 1 | FALSE | 3 | Silver | 30 | 3 | 60 |
| halfway_there | Halfway There | Complete half of all shelves | 1 | FALSE | 4 | Gold | 90 | 4 | 90 |
| warehouse_in_order | Warehouse in Order | Complete every shelf in the store | 1 | FALSE | 5 | Gold | 90 | 5 | 150 |
| category_closed | Category Closed | Complete an entire category and unlock its blueprint | 1 | FALSE | 6 | Bronze | 15 | 6 | 40 |
| blueprint_collector | Blueprint Collector | Unlock all 16 blueprints | 1 | FALSE | 7 | Gold | 90 | 7 | 120 |
| retail_therapy | Retail Therapy | Buy your first upgrade at the terminal | 1 | FALSE | 8 | Bronze | 15 | 8 | 20 |
| maxed_out | Maxed Out | Max out every upgrade | 1 | FALSE | 9 | Gold | 90 | 9 | 120 |
| high_roller | High Roller | Spin the slot machine for the first time | 1 | FALSE | 10 | Bronze | 15 | 10 | 30 |
| kaboom | Kaboom | Set off a bomb | 1 | FALSE | 11 | Silver | 30 | 11 | 40 |
| treasure_hunter | Treasure Hunter | Find all 40 parts crates in the pile | 1 | FALSE | 12 | Gold | 90 | 12 | 120 |
| master_mechanic | Master Mechanic | Fully assemble a special car | 1 | FALSE | 13 | Gold | 90 | 13 | 130 |

Примечания:
- Xbox: очки в сумме ровно 1000 (Gamerscore).
- PSN: грейды и очки — предложение по сложности (4 бронзы, 3 серебра, 6 золота = 690). Минимальную сумму очков для платины сверить с правилами Sony в кабинете; грейды можно менять свободно, код на них не завязан.
- Hidden везде FALSE, скрытых условий нет.

Условия в коде (где срабатывает):

| ID | Trophy | Условие | Триггер |
|---|---|---|---|
| 1 | FiveOfAKind | в руках ≥ 5 машинок одной модели | `Inventory.Add` → `Trophies.Hands` |
| 2 | ShelfLife | `ClosedShelves` ≥ 1 | `GameManager.OnShelfClosed` |
| 3 | FiftyShelves | `ClosedShelves` ≥ 50 | там же |
| 4 | HalfwayThere | `ClosedShelves` ≥ `TotalShelves` / 2 | там же |
| 5 | WarehouseInOrder | все полки (`GameComplete`) | там же |
| 6 | CategoryClosed | первый чертёж | `GameManager.UnlockBlueprint` |
| 7 | BlueprintCollector | все чертежи (`Catalog.Categories.Length`) | там же |
| 8 | RetailTherapy | первая покупка в терминале | `UpgradeService.TryBuy` |
| 9 | MaxedOut | `UpgradeService.AllMaxed` | `TryBuy` / `TrySpin` |
| 10 | HighRoller | первый спин | `UiRoot.SpinWith` |
| 11 | Kaboom | взрыв бомбы | `GameManager.ExplodeBomb` |
| 12 | TreasureHunter | `CollectiblesFound` == число ящиков | `GameManager.Collect` |
| 13 | MasterMechanic | `SpecialLevel` == `SpecialCarData.MaxLevel` у любой спецмашины | `GameManager.UpgradeSpecial` |

При загрузке сейва `Trophies.Evaluate` досдаёт выполненные условия (платформа идемпотентна).
