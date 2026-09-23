# LOCALIZATION_TERMS.md — терминология и TRM-требования для перевода

Рабочий документ для перевода таблицы `Game_Text` (Unity Localization, 19 локалей).
Термины из таблиц ниже — **обязательная терминология платформодержателя**: это общая
справка, не всё из неё встречается в игре. Правило: **если** такое слово встречается
в игровом тексте — перевод берётся **дословно** из этого документа (диакритику
сохранять; регистр можно адаптировать под стиль UI). Документ пополняется по мере поступления требований.

**Список языков не железный** — перед запуском перевода уточнить актуальный список
у продюсера; базово — те 19 локалей, что уже заведены в проекте.

## TRM: направленная терминология

Программа использует directional terminology для следующих терминов:

| Термин | ID требования | Таблица переводов |
|---|---|---|
| Light Bar | 350 | есть (ниже) |
| Vibration | 378 | есть (ниже) |
| Controller | 191 | есть (ниже) |
| Left Stick | — | не нужна: в текстах слово избегаем, показываем глиф стика под каждую платформу |

## Обязательные переводы терминов

Колонка «Локаль» — код локали проекта (`Assets/Localization/Locales/`).

| Страна | Язык | Локаль | Light Bar (350) | Vibration (378) | Controller (191) |
|---|---|---|---|---|---|
| China | Chinese (Simplified) — 简体中文 | zh | 光条 | 震动 | 控制器 |
| China | Chinese (Traditional) — 繁體中文 | zh-Hant | 光條 | 震動 | 控制器 |
| Denmark | Dansk | da | lysstribe | vibration | controller |
| Netherlands | Dutch | nl | lichtbalk | trilling | controller |
| Finland | Finnish / Suomi | fi | valopalkki | värinä | ohjain |
| France | Français | fr | barre lumineuse | vibration | manette |
| Germany | Deutsch | de | Leuchtleiste | Vibration | Controller |
| Italy | Italiano | it | barra luminosa | vibrazione | controller |
| Japan | Japanese | ja | ライトバー | 振動 and バイブレーション | コントローラー |
| Korea | 한국어 | ko | 라이트 바 | 진동 | 컨트롤러 |
| Norway | Norsk | no | lyslist | vibrering | kontroller |
| Poland | Polski | pl | pasek świetlny | wibracje | kontroler |
| Brazil | Português (BR) | pt-BR | barra de luz | vibração | controle |
| Russia | Русский | ru | световая панель | вибрация | контроллер |
| Spain | Español | es | barra luminosa | vibración | mando |
| Sweden | Svenska | sv | lampskena | vibration | handkontroll |
| Turkey | Türkçe | tr | ışıklı çubuk | titreşim | kontrol cihazı |
| Ukraine | Українська | uk | світлова панель | вібрація | контролер |
| England / USA | English | en | light bar | vibration | controller |

Примечания:
- **Японский Vibration** — в требовании два допустимых варианта: 振動 и バイブレーション.
- Колонка «Auto-installation» в исходных данных пуста — заполнится при поступлении.

## Ожидается

- Прочая терминология / TRM-требования (докидываются порциями).
