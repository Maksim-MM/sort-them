> Часть porting-пайплайна. Индекс, маршрутизация и порядок чтения — `PORTING_PIPELINE.md` в этой папке.

## Stage 1 — SDK Audit & Cleanup

> Не все плагины ниже присутствуют в каждом проекте. Порядок работы: сначала найди что есть → прими решение по каждому → убери/замени.

### 1.1 Аудит — найди что установлено

Перед удалением — точная карта того, что физически в проекте.

**Физическое присутствие SDK (готовые команды):**
 
```bash
# Папки с именами SDK
find Assets -maxdepth 5 -type d \( -iname "*admob*" -o -iname "*applovin*" \
  -o -iname "*ironsource*" -o -iname "*unityads*" -o -iname "*firebase*" \
  -o -iname "*facebook*" -o -iname "*adjust*" -o -iname "*appsflyer*" \
  -o -iname "*gameanalytics*" -o -iname "*revenuecat*" -o -iname "*MaxSdk*" \
  -o -iname "*Vungle*" -o -iname "*UnityPurchasing*" -o -iname "*GoogleMobileAds*" \
  -o -iname "*purchasing*" -o -iname "*ExternalDependency*" -o -iname "*EDM*" \
  -o -iname "*PlayServicesResolver*" \) 2>/dev/null

# Нативные плагины
ls Assets/Plugins/Android/ Assets/Plugins/iOS/ 2>/dev/null

# Конфиги SDK
find Assets -maxdepth 6 -type f \( -iname "google-services.json" \
  -o -iname "GoogleService-Info.plist" -o -iname "AdMob*Settings*" \
  -o -iname "AppLovin*Settings*" -o -iname "MaxSdk*" \
  -o -iname "*.aar" -o -iname "*.androidlib" -o -iname "*.framework" \) 2>/dev/null
```

- [ ] **Реклама**: AdMob / MAX / IronSource / Unity Ads / Vungle / Chartboost / Pangle
- [ ] **Аналитика**: Firebase Analytics / Adjust / AppsFlyer / GameAnalytics / Singular / Branch
- [ ] **IAP**: Unity IAP / RevenueCat / собственный биллинг
- [ ] **Прочие мобильные/веб SDK**: CrazyGames, Yandex Games, Kongregate, FB Instant Games
- [ ] **Vendor-папки с чужими event bus / SDK-фреймворками** — могут использоваться другим кодом, удалять осторожно

**`Packages/manifest.json` — пакеты:**

```bash
grep -E '"com\.' Packages/manifest.json | grep -v "modules\."
grep -A2 "scopedRegistries" Packages/manifest.json  # OpenUPM / частные registries
```

- [ ] `com.unity.purchasing` — IAP
- [ ] `com.unity.analytics` / `com.unity.modules.unityanalytics` — Unity Analytics
- [ ] `com.google.firebase.*` / `com.google.ads.*` — Firebase / AdMob
- [ ] `com.appsflyer.*` / `com.adjust.*` / `com.gameanalytics.*` — аналитика

**Заглушенный SDK без физического присутствия (типичная ловушка):**

Часто кто-то удалил SDK-пакет раньше, но **код-обвязку и серилизованные ключи на префабах оставил**. Симптомы: класс `*AdsManager`/`*IAPManager` есть, методы `Initialize*`/`Show*` — пустые тела или закомментированы; на каком-то GameObject в Splash/Init-сцене заполнены реальные `MaxSdkKey`/`AdUnitId`/`AppKey` поля. Это **активные продакшн-ключи**, которые нельзя оставлять в репозитории портируемой версии.

- [ ] Грепнуть YAML префабов и сцен на признаки ad-ключей:
```bash
grep -rE "MaxSdkKey|AdUnitId|AppKey|GameKey|API_KEY|app_id|ENTER_.*_HERE" \
  Assets/Resources/ Assets/Scenes/ Assets/Prefabs/ 2>/dev/null
```
- [ ] Если найдено — удалить как часть удаления самого класса в 1.4.

**Карта rewarded-точек (закомментированная и активная аналитика):**

Даже если аналитический SDK физически отсутствует, в коде часто остаются **закомментированные точки трекинга** (`// FBAnalytics.ins?.EventCallBack("xxx")`). Это **готовая карта** событий, которые продуктовая команда хотела отслеживать. Сохрани её — если на консоли понадобится платформенная телеметрия или CRM, эти точки можно переподключить без бизнес-анализа.

```bash
grep -rE "// *(FBAnalytics|Firebase|Adjust|AppsFlyer|GameAnalytics|UAnalytics)\." \
  Assets/Scripts/ 2>/dev/null
```

**Sanity: что должно быть результатом 1.1**

- Полный список SDK с пометкой `физически есть` / `только заглушка` / `только закомментированные вызовы`
- Список ad-ключей, которые надо вычистить из инспекторов
- Список rewarded/IAP-точек в коде (см. 1.2 и 1.3)
- Запись в `PROGRESS.md` — какие SDK финально удалить, какие оставить

### 1.2 Реклама — убрать полностью

- [ ] Удалить SDK (`.aar`, `.gradle`, скрипты, вендорные папки)
- [ ] Убрать все вызовы: Show/Hide баннера, ShowInterstitial, ShowRewardedVideo
- [ ] Найти и удалить **авто-интерстишн таймеры** (типовой паттерн `*AdsManager*.timer` + `Update()` каждые N секунд → `ShowInterstitial`). Часто завязано на `OnPickup`/`OnAction` — сбрасывает timer и показывает рекламу через N сек. Грепни: `AdsManager.*timer`, `Time.deltaTime` рядом с `Show*Ad`.

**Rewarded Ad контент — обязательно обсудить с клиентом до изменений:**

Rewarded ad может закрывать не только предметы магазина, но и:
- разблокировку геймплейных функций или способностей
- доступ к режимам / уровням
- валюту / ресурсы
- кастомизацию

> Перед тем как что-то менять — составь полный список всего что закрыто за рекламу (код + сцена) и согласуй с клиентом механику разблокировки для каждого типа.

#### Типовая карта rewarded-точек в mobile-играх

Используй как чеклист — каждую категорию проверь, даже если кажется что её в проекте нет. Чаще всего эти 7 встречаются в любых тайкунах/симуляторах/казуалках:

| Категория | Где обычно сидит | Типовое имя UI-кнопки/скрипта |
|---|---|---|
| **Cash/Currency boost** | Bank-вкладка в магазине, Top Bar `+`-кнопка рядом с балансом | `BankUI`, `Plus_icon`, `WatchAd_Btn`, `FreeCash*` |
| **Energy / Stamina refill** | то же место что и Cash | `EnergyBank`, `EnergyRefill`, `Plus_icon` на energy bar |
| **Continue / Extra life** | Game Over экран | `Continue_Btn`, `ReviveBtn`, `WatchAd_Continue` |
| **2x Speed / Time boost** | плавающая кнопка `2X` где-то в углу HUD | `2X*Btn`, `SpeedBtns`, `DoubleSpeed*` |
| **Instant completion** (доставка/строительство/таймер) | рядом с таймером объекта | `WatchAd_Btn`, `SkipBtn`, `InstantComplete*`, `*timeComplete` |
| **Daily bonus / Free tax / Income multiplier** | Day-End / Day-Start экран | `FreeTaxBtn`, `IncBalanceBtn`, `DailyReward*`, `Bonus*` |
| **Mystery box / Gift / Free chest** | 3D-пикап в мире или иконка в HUD | `GiftBox`, `PickGift_Btn`, `MysteryBox*`, `Chest*` |
| **Skip ad / Continue Ad** | модальный диалог "посмотри рекламу или подожди X сек" | `SkipWaitBtn`, `ContinueAdBtn` |

#### Варианты замены (типовая таблица)

| Категория | Чаще всего делают |
|---|---|
| Cash/Currency boost | **Удалить кнопку**. Тайкуны на консоли зарабатывают по геймплею. UI-узел Plus_icon скрыть, Button.enabled = false (чтобы курсор не реагировал). |
| Energy/Stamina refill | **Удалить кнопку** или сделать таймер регенерации (1 stamina / N сек). |
| Continue / Extra life | **Сделать бесплатным** (одно нажатие → continue) или удалить совсем (game over без альтернативы). Зависит от жанра. |
| 2x Speed / Time boost | **За энергию** (списать N энергии). Сохраняет геймплейный баланс. Кост в инспекторе. |
| Instant completion | **За энергию** (часто такая кнопка уже есть параллельно, см. SpeedUp + WatchAd дубликат). |
| Daily bonus / Free tax / Income mult | **Удалить кнопки целиком**. Либо «всегда доступно бесплатно» — но тогда баланс игры просядет. |
| Mystery box / Gift | **Сделать бесплатным** (Pickup → Reward напрямую). Таймер кулдауна (1 в N минут) уже обычно встроен в spawn-логику. |
| Skip ad / Continue Ad | Удалить, оставить только «подожди». |

> ⚠️ **Обсудить с клиентом** **обязательно**. Удаление каждой rewarded-точки меняет баланс. Решение по каждой записывай в `PROGRESS.md` со ссылкой на конкретные файлы/префабы.

#### Поиск ad-кнопок и индикаторов в UI

**В коде:**
```bash
grep -rEn "ShowRVideo|ShowRewardedVideo|ShowInterstitial|ShowBanner|RewardedAd|rewarded_ad" Assets/Scripts/
grep -rEn "OnRewardedClosed|OnAdLoaded|OnAdShown|RewardedAdCallback" Assets/Scripts/
grep -rEn "RewardType\.|UserReward|GiveReward|OnAdReward" Assets/Scripts/
```

**В иерархии префабов** — ищи GameObject'ы с этими именами:
- `*WatchAd*`, `*Watch_Ad*`, `*AdBtn*`, `*Ad_Btn*`, `*RewardedBtn*`, `*RewardBtn*`
- `*Plus_icon*` (на Currency/Energy bar — IAP пополнение)
- `*Ad Icon*`, `*AdIcon*`, `*ADTxt*`, `*Ad_Txt*` (под другими кнопками — текст "AD" или иконка рекламы)
- `*Free*Btn` (часто кнопка с подписью "Free" — за рекламу)
- `*Continue*Ad*`, `*Continue_Btn*`
- `*SkipAd*`, `*SkipBtn*`
- `*DailyBonus*`, `*FreeTax*`, `*IncBalance*`

Через MCP:
```
find_gameobjects search_term="WatchAd" search_method=by_name include_inactive=true
find_gameobjects search_term="Plus_icon" search_method=by_name include_inactive=true
find_gameobjects search_term="AdIcon" search_method=by_name include_inactive=true
```

**На 3D-объектах в мире** (World-Space Canvas) — см. подводный камень ниже.

#### Чеклист правок

- [ ] Найти в коде все флаги/поля типа `purchasableViaRewardedAd`, `canUnlockThroughAd`, `isRewardedReady` и аналоги
- [ ] **Найти на сцене** все кнопки "Watch Ad" / "Rewarded" через Unity MCP (`find_gameobjects`) — см. список имён выше
- [ ] **Найти ad-индикаторы на 3D-объектах в мире** (см. подводный камень ниже) — не только в UI Canvas, но и над пикапами, бустерами, NPC, машинами и т.п.
- [ ] **Найти "AD"/иконки рекламы под обычными кнопками** — отдельные дочерние Text/Image на кнопках Close/Save/Continue/Apply, которые сейчас бессмысленны после удаления rewarded
- [ ] **Обсудить с клиентом** механику замены для каждого типа контента
- [ ] Заменить флаги и задать цены / условия разблокировки
- [ ] После замены — **проверить полный флоу покупки** в Unity: кнопка видна, активна (interactable), покупка проходит, состояние сохраняется

> ⚠️ Типичный баг при замене ad → cash: UI-кнопка становится видимой (`SetActive(true)`), но `interactable` не инициализируется — кнопка выглядит активной но не нажимается. Убедись что `interactable` пересчитывается при каждом показе кнопки на основе актуального баланса.

#### Подводный камень: реклама на 3D-объектах через World-Space Canvas

Иконки/тексты «Watch Ad» бывают не только на UI-кнопках в Canvas-Overlay. Часто на **мировых пикапах** (бустеры, подарки, бесплатные сундуки, временные бонусы) висит небольшой World-Space Canvas со стрелкой/иконкой видео-плеера и таймером жизни. Когда вырезаешь логику рекламы для такой механики, сама `Interact()`-кнопка может остаться рабочей, но **визуальный индикатор остаётся прежним — игрок видит знак «смотри рекламу»**, хотя кнопка теперь даёт награду бесплатно.

**Типовая структура такого пикапа (пример из supermarket-simulator, `Assets/Prefabs/Boxes/BoxMain/Gift Box.prefab`):**

```
Gift Box [Pickup-скрипт, Timer-скрипт, Collider, Rigidbody]
├── Mesh [HighlightEffect]
│   └── VFX (Epic Toon FX портал/glow)
├── HUD Element (часто inactive — альт. путь через Sickscore HUD Navigation)
├── ToucInputObject (inactive) — мобильный ItemTouchInput
└── Canvas (World-Space)
    └── LookAtPivot [LookAtPlayer — поворачивается к камере]
        └── Canvas
            └── Arrow [Image, Animator] ← ad-иконка «watch video»
                └── TimerText [Text] ← живой таймер от Timer-скрипта
```

**Что проверить и что сделать:**

- [ ] Грепни префабы пикапов на компоненты-таймеры (`*Timer.cs`) и `LookAtPlayer`/`LookAtCamera`. Каждый такой пикап — кандидат: посмотри его World-Space Canvas-дерево.
- [ ] У каждого пикапа найди узлы с `UI.Image` под `Canvas/LookAtPivot/Canvas/...` — там может быть ad-icon.
- [ ] Если ad-icon является **единственным визуалом над объектом** — заменить sprite на нейтральный (стрелка, восклицательный знак, иконка типа подарка). Не удалять GameObject — у него обычно есть Animator (пульсация/поворот) и дочерний Text (таймер).
- [ ] Если есть отдельный `Animator.controller` со специфически рекламной анимацией (видеокамера, кнопка play) — заменить или удалить триггер.
- [ ] Если над пикапом несколько индикаторов (например HUD Element + World-Space Canvas одновременно) — проверить, что после изменения один из них не повторяет ad-иконку.

**Где обычно лежат такие пикапы:** префабы в `Resources/` подпапках типа `Gameplay/Boxes`, `Pickups`, `Items`, `Powerups`. Если в проекте есть `*GiftBox*`, `*Pickup*`, `*Booster*`, `*Reward*` — каждый префаб с этим именем проверять отдельно.

**Обсудить с клиентом:** какую иконку поставить вместо «watch video» (стрелка / знак подарка / question mark) — это влияет на читаемость UI. Не оставляй ad-иконку.

### 1.3 IAP — решить по ситуации

Варианты (выбери один для каждого проекта):
- **Вырезать** — контент становится бесплатным или за игровую валюту
- **Конвертировать** — покупка за внутриигровую валюту вместо реальных денег (подходит если монетизации на консоли нет)
- **Заменить** — интеграция с консольным IAP через платформенный SDK (Stage 6)

- [ ] Найти все точки покупки в коде (`IAPManager`, `BuyButton`, `InAppUI`, `IAPOffers` и т.п.)
- [ ] Найти на сцене UI-кнопки покупок через Unity MCP — **обсудить с клиентом** что оставить, что убрать, что конвертировать
- [ ] Принять решение выше и реализовать
- [ ] Убрать сам IAP SDK если не заменяем на консольный

#### Подводный камень: параллельные системы (rewarded + IAP) на одну механику

Часто на одну категорию (Cash boost, Energy refill) висят **две параллельные кнопки**: одна за rewarded video, вторая за реальные деньги (IAP). После удаления SDK логика может быть такой:
- **Rewarded-кнопка**: SDK заглушка, callback всегда возвращает success → награда раздаётся **бесплатно** (sic).
- **IAP-кнопка**: класс `*IAPManager`/`InAppUI` остался, но без подключённого SDK → `Application.RequestPurchase` либо никогда не вызывается, либо метод-обёртка просто прибавляет ресурс **без проверки оплаты**. Это **dev-cheat**, который случайно остался в проде.

**Проверь** обе системы перед удалением:
```bash
grep -rEn "IAPManager|InAppUI|BuyProduct|RequestPurchase|RestorePurchases" Assets/Scripts/
grep -rEn "DB\.iap_|DB\.IsFreeAdsMode|PlayerPrefs.*RemoveAds|PlayerPrefs.*Premium" Assets/Scripts/
```

- [ ] **IAP-флаги в `PlayerPrefs`** (`iap_AdsPurchased`, `RemoveAds`, `iap_UnlockAllLevels`, `iap_UnlockEverything`, `PremiumUser`) — если ничего нигде их не ставит в `1` (нет UI покупки), это мёртвые проверки. Найти все `if (DB.iap_X == 1)` / `PlayerPrefs.GetInt("RemoveAds")`, понять что блокируется, и либо удалить условие, либо принять решение «всегда премиум».
- [ ] Если IAP-кнопка раздаёт ресурс **без оплаты** (нет вызова `Application.RequestPurchase` или эквивалента) — это случайный cheat, не оставлять. Удалить класс целиком.

### 1.3.5 Privacy Policy / Rate Us / GDPR Consent — отдельная категория

Не реклама и не IAP, но в ту же кучу. Часто эти три механики тянутся за SDK и должны быть убраны:

- [ ] **Кнопка Privacy Policy** в Settings — типовая реализация `Application.OpenURL("https://...")`. На консоли `OpenURL` либо не работает (Switch), либо открывает встроенный браузер с предупреждением (PS/Xbox). Удалить кнопку и метод. Если требуется по сертификации — заменить на platform-overlay из консольного SDK (Stage 7).
- [ ] **GDPR Consent** (`UserConsent.cs`, `GDPR.cs`, объект `Consent` на Splash) — GDPR на консолях обрабатывается на уровне ОС (PS/Xbox PII consent). Удалить класс, объект на сцене, поле `DB.UserConsent`.
- [ ] **Rate Us** / `Application.RequestRating` — на консолях нет такой механики. Удалить кнопку из Settings.
- [ ] **Share / Social** — `Application.OpenURL` на соц-сети. Удалить или заменить на консольный share overlay.

```bash
grep -rEn "Application\.OpenURL|Application\.RequestRating|RequestStoreReview|FB\.Share" Assets/Scripts/
grep -rEn "ConsentForm|UMP\.|TagForUnderAgeOfConsent|ConsentInformation|CCPA" Assets/Scripts/
```

### 1.4 Финал

- [ ] Удалить Android/iOS-специфичные плагины (`Assets/Plugins/Android/`, `Assets/Plugins/iOS/`) — всё что осталось после удаления SDK
- [ ] Проверить компиляцию, исправить зависимости удалённых SDK
- [ ] Убедиться что ничего игрового не сломалось
- [ ] **Прогнать sweep broken-script компонентов** (см. ниже) — обязательно
- [ ] Удалить **серилизованные ad-ключи** на префабах сцен (которые остались как «заглушенный SDK» из 1.1)
- [ ] Удалить корневые GameObject'ы из Splash-сцены, которые были host'ами рекламы (`Economy`, `Consent` и подобные). Если Splash после этого становится пустым шеллом — это нормально.
- [ ] Если на Splash-сцене был **единственный `DontDestroyOnLoad`-объект** (часто `AdsManager` с DDOL) — проверь, что ничего другого не полагалось на его пережитие при переходе в Gameplay
- [ ] Обновить `ARCHITECTURE.md` — убрать разделы про удалённые SDK и рекламу, отразить изменения в механике покупок

#### Обязательный sweep broken-script компонентов

После удаления классов рекламы/IAP/аналитики в проекте остаются **тысячи «висящих» компонентов** на префабах: GameObject'ы, у которых `MonoScript` указывал на теперь несуществующий класс. Unity терпимо относится к этому в Play-режиме (просто null component slot), но **категорически отказывается сохранять префаб** с такими компонентами — `Error while saving Prefab: ... script does not derive from MonoBehaviour`.

**Это блокирует все последующие правки префабов**, поэтому sweep — обязательный шаг, **до точечных правок UI**. Иначе будешь натыкаться на блокировку на каждой кнопке.

**Три типа битых компонентов:**

| Тип | Признак | Как чистить |
|---|---|---|
| **Missing script** | `m_Script: {fileID: 0}` (полностью пустой) или GUID не находится в `.meta` ни одного `.cs` | `GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go)` |
| **Null-component slot** | `m_Component[i].component.objectReferenceValue == null` (slot есть, объект-компонент пустой) | `SerializedObject(go).FindProperty("m_Component").DeleteArrayElementAtIndex(i)` |
| **Script-not-MonoBehaviour** | `.cs` существует, GUID валиден, но класс не загружается (`MonoScript.GetClass() == null` — например файл закомментирован, или класс не наследует MonoBehaviour) | Найти `.cs` через `AssetDatabase.GUIDToAssetPath(guid)`, удалить `.cs+.meta` целиком → после этого slot становится «missing» и подхватывается первым типом. См. подводный камень в начале файла. |

**Готовый snippet под `execute_code` (MCP):**

```csharp
var sb = new System.Text.StringBuilder();
int grandMissing = 0, grandNull = 0;
foreach (var p in UnityEditor.AssetDatabase.GetAllAssetPaths()) {
    if (!p.StartsWith("Assets/") || !p.EndsWith(".prefab")) continue;
    UnityEngine.GameObject root = null;
    try { root = UnityEditor.PrefabUtility.LoadPrefabContents(p); } catch { continue; }
    if (root == null) continue;
    int missing = 0, nulls = 0;
    var transforms = new System.Collections.Generic.List<UnityEngine.Transform>(
        root.GetComponentsInChildren<UnityEngine.Transform>(true));
    // Тип 1: missing script
    foreach (var t in transforms) if (t != null)
        missing += UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
    // Тип 2: null component slot (включая Тип 3 после удаления .cs)
    foreach (var t in transforms) {
        if (t == null) continue;
        var comps = t.GetComponents<UnityEngine.Component>();
        var nullIdx = new System.Collections.Generic.List<int>();
        for (int i = 0; i < comps.Length; i++) if (comps[i] == null) nullIdx.Add(i);
        if (nullIdx.Count == 0) continue;
        var so = new UnityEditor.SerializedObject(t.gameObject);
        var prop = so.FindProperty("m_Component");
        if (prop == null) continue;
        nullIdx.Sort();
        for (int i = nullIdx.Count - 1; i >= 0; i--) {
            int idx = nullIdx[i];
            if (idx < prop.arraySize) { prop.DeleteArrayElementAtIndex(idx); nulls++; }
        }
        so.ApplyModifiedProperties();
    }
    if (missing + nulls > 0) {
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, p);
        sb.AppendLine(p + ": -" + missing + " missing / -" + nulls + " null");
        grandMissing += missing; grandNull += nulls;
    }
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
UnityEditor.AssetDatabase.Refresh();
return "Cleaned " + grandMissing + " missing scripts and " + grandNull + " null slots.\n" + sb;
```

**Порядок:**
1. Запустить sweep после удаления `.cs` классов SDK (1.4 первые шаги) — он не уберёт «script-not-MonoBehaviour» (см. Тип 3).
2. Для каждой ошибки `script '', which does not derive from MonoBehaviour` — диагностировать GUID через сниппет из подводного камня в начале файла, удалить виновный `.cs+.meta`.
3. Запустить sweep ещё раз — теперь подхватит остатки как Тип 1.

#### Stage 1 sanity checklist (перед закрытием стадии)

- [ ] Компиляция чистая (`read_console types=error` — ничего, кроме YAML-warning'ов про `.meta`)
- [ ] Sweep broken-script компонентов отработал и **на повторном запуске возвращает 0**
- [ ] Изменённые префабы реально сохранились — `stat -f "%Sm" path.prefab` показывает свежий mtime (а не дату ассета из репозитория)
- [ ] Нет упоминаний удалённых классов в коде: `grep -rE "AdsManager|RewardedVideo|IAPManager|FBAnalytics|GDPR" Assets/Scripts/` пусто
- [ ] Нет ad-ключей в YAML префабов/сцен: `grep -rE "MaxSdkKey|AdUnitId|app_id" Assets/Resources/ Assets/Scenes/ Assets/Prefabs/` пусто
- [ ] **Сверка «обнулённая ссылка ↔ безусловное обращение в коде»** (см. ниже — без неё чистка оставляет тихие NRE)
- [ ] **Запуск игры**: Splash → Gameplay проходит, не падает на missing reference в инспекторе менеджеров
- [ ] **Базовый игровой флоу** (главная механика игры) проходит без новых ошибок в Console
- [ ] `PROGRESS.md` обновлён: что удалено, что осталось «унаследовано» для следующих стадий (например, `RewardPanel.cs` нигде не вызывается, но компонент висит на сценном GameObject — удалить в Stage 7)

#### Обязательная сверка: обнулённая ссылка в префабе ↔ безусловное обращение в коде

Самый частый остаток после чистки — **не мусорный класс, а разорванная ссылка**: ad/IAP-кнопку удалили из иерархии префаба, поле в компоненте стало `{fileID: 0}`, а код продолжает безусловно к нему обращаться (`removeAdsButton.onClick.AddListener(...)`). Это даёт `NullReferenceException` в `Start()`/`Awake()`, который **не рушит игру и потому не находится плейтестом**: подписки выше уже навесились, UI работает и показывает данные, а оборвавшийся хвост метода часто дублируется каким-нибудь событием, так что симптома нет вообще. В `TopBarCanvas` такой NRE прожил от Stage 1 до Stage 6 (лечение — `if (removeAdsButton)`), и вместе с ним не вызывалась инициализация состояния кнопок скорости.

Проверять механически по каждому живому префабу, который правили:

```bash
# 1. найти обнулённые ссылки в компонентах (кроме служебных m_CorrespondingSourceObject/m_PrefabInstance/m_PrefabAsset/m_Icon)
grep -n ": {fileID: 0}" Assets/Resources/**/*.prefab | grep -vE "m_CorrespondingSourceObject|m_PrefabInstance|m_PrefabAsset|m_Icon|m_Sprite|m_TargetTexture|m_VolumeTrigger"

# 2. по каждому найденному полю — есть ли обращение БЕЗ проверки на null
grep -rn "<имяПоля>\." Assets/Scripts/
```

Ссылка пустая — приемлемо, если все обращения к полю либо под `if (field)`, либо закомментированы (в `TopBarCanvas` так и оказалось с `leaderBoardButton` и `offersButton`). Любое безусловное обращение — либо guard, либо выпилить поле вместе с логикой.

- [ ] `read_console types=error` **со стеком** после первого запуска: любой `NullReferenceException` из `Start`/`Awake`/`OnEnable` UI-менеджеров разобрать, а не списать на шум. В консоли постоянно висят `Compatibility Mode` и GUID-warning'и — одна строка NRE среди них теряется.

